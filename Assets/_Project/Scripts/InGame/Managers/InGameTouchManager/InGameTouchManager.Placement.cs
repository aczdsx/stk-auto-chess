using System;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// [Placement] UI → 보드 캐릭터 배치
/// - 고스트 캐릭터 생성 · 드래그 프리뷰 · 배치 확정/취소
/// - 추천 행(CharacterPositionType 기반) VFX 표시/숨기기
/// </summary>
public partial class InGameTouchManager
{
    // 추천 행 VFX (캐싱)
    private InGameVfx _recommendedRowVfx;
    
    /// <summary>
    /// UI에서 드래그하여 보드에 캐릭터를 배치할 때 호출 (고스트 캐릭터 생성)
    /// </summary>
    public async UniTask<bool> StartDragFromUI(CharacterStatData statData, InGameTileView tileView, Action<CharacterStatData> onPlacementConfirmed = null)
    {
        if (_ghostCharacterController != null || _isMoveEndAnimation)
            return false;

        _isDragFromUI = true;  // UI에서 드래그 시작

        // 타일 유효성 체크
        if (tileView == null || tileView.AllianceType != AllianceType.Player)
            return false;

        var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
        if (inGameTile == null)
            return false;

        // 이미 캐릭터가 있는 타일이면 빈 타일 찾기
        if (inGameTile.OccupiedCharacter != null)
        {
            var emptyTile = InGameObjectManager.Instance.InGameGrid.GetRecommandedTile(statData.Spec);
            if (emptyTile == null)
                return false;

            inGameTile = emptyTile;
            tileView = emptyTile.View as InGameTileView;
        }

        // 고스트 캐릭터 생성 (Synergy 미등록, 타일 점유 안 함)
        _ghostCharacterController = await InGameObjectManager.Instance.CreateGhostCharacter(statData, inGameTile);
        if (_ghostCharacterController == null)
            return false;

        // 고스트용 홀로그램 Material 적용 및 아군 방향 설정
        var ghostView = _ghostCharacterController.GetCharacterView();
        ghostView?.SetHologramShader();
        ghostView?.SetFirstDirection(AllianceType.Player);

        _ghostStatData = statData;
        _onPlacementConfirmed = onPlacementConfirmed;

        // 튜토리얼 중이면 고스트 캐릭터에 TutorialTarget 등록
        if (TutorialManager.Instance.HasTutorialStage)
        {
            var characterView = _ghostCharacterController.GetCharacterView();
            if (characterView != null)
            {
                var tutorialTarget = characterView.gameObject.GetComponent<TutorialTarget>();
                if (tutorialTarget == null)
                {
                    tutorialTarget = characterView.gameObject.AddComponent<TutorialTarget>();
                }
                tutorialTarget.SetTargetId(_ghostCharacterController.CharacterId.ToString());
            }
        }

        // 고스트 상태로 전환 (기존 selected 로직 활용)
        _selectedTileView = tileView;
        _selectedFirstTileView = tileView;
        _selectedCharacterController = _ghostCharacterController;
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_position_pick);
        _selectedTileView.SetActiveObj(true);

        // 추천 행 VFX 표시
        ShowRecommendedRowVfx(statData.Spec.character_position_type);

        return true;
    }

    /// <summary>
    /// UI 드래그 중 터치 위치 업데이트 (MoveCharacter 대리 호출)
    /// </summary>
    public void UpdateDragPosition(Vector3 screenPosition)
    {
        if (_ghostCharacterController == null)
            return;

        MoveGhostCharacter(screenPosition);
    }

    /// <summary>
    /// 고스트 캐릭터 이동 처리 (MoveCharacter 기반)
    /// </summary>
    private void MoveGhostCharacter(Vector3 touchPosition)
    {
        if (_ghostCharacterController == null)
            return;

        // 드래그 중 BottomUI 영역 하이라이트 체크
        var inGameMain = InGameMain.GetInGameMain();
        bool isInBottomUI = inGameMain.IsPointInBottomScrollRect(touchPosition);
        inGameMain.SetDropHighlight(isInBottomUI);

        // RaycastAll로 Slot 우선 검색
        if (!TryGetPrioritizedHit(touchPosition, out RaycastHit hit, out string hitTag))
            return;

        // 1. 타일 위인 경우 → 타일에 스냅
        if (hitTag == "Slot")
        {
            InGameTileView ingameTileView = hit.transform.GetComponent<InGameTileView>();
            if (ingameTileView == null)
                return;

            // Player 타일만 허용
            if (ingameTileView.AllianceType != AllianceType.Player)
                return;

            // 같은 타일이면 스킵
            if (_selectedTileView != null && ingameTileView.ID == _selectedTileView.ID)
                return;

            // 이전 타일 비활성화
            _selectedTileView?.SetActiveObj(false);

            // 새 타일 선택
            _selectedTileView = ingameTileView;
            _selectedTileView.SetActiveObj(true);

            // 고스트 캐릭터 이동
            Vector3 targetPos = ingameTileView.CachedTr.transform.position;
            _ghostCharacterController.Position3D = targetPos;
            _ghostCharacterController.GetCharacterView().CachedTr.localPosition = targetPos;
            _ghostCharacterController.GetCharacterView()?.SetFirstDirection(AllianceType.Player);
        }
        // 2. 배경(Playground) 위인 경우 → 자유 이동
        else if (hitTag == "Playground")
        {
            // 타일 하이라이트 해제
            _selectedTileView?.SetActiveObj(false);

            // hit.point의 Y값을 캐릭터 높이로 보정
            Vector3 targetPos = hit.point;
            targetPos.y = _ghostCharacterController.Position3D.y;

            _ghostCharacterController.Position3D = targetPos;
            _ghostCharacterController.GetCharacterView().CachedTr.localPosition = targetPos;
            _ghostCharacterController.GetCharacterView()?.SetFirstDirection(AllianceType.Player);
        }
    }

    /// <summary>
    /// UI 드래그 종료 - 고스트 제거 후 실제 캐릭터 배치 또는 취소
    /// </summary>
    public void EndDragFromUI(Vector3 screenPosition)
    {
        if (_ghostCharacterController == null)
            return;

        EndGhostDrag(screenPosition).Forget();
    }

    /// <summary>
    /// 고스트 드래그 종료 처리 (비동기)
    /// </summary>
    private async UniTaskVoid EndGhostDrag(Vector3 screenPosition)
    {
        var inGameMain = InGameMain.GetInGameMain();
        bool isInBottomUI = inGameMain.IsPointInBottomScrollRect(screenPosition);

        // 드래그 종료 시 하이라이트 해제
        inGameMain.SetDropHighlight(false);

        if (isInBottomUI)
        {
            // BottomUI 영역에 드롭 - 고스트만 제거, UI에서 제거 안 함
            CancelGhostDrag();
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_position_back);
        }
        else
        {
            // RaycastAll로 Slot 우선 검색
            if (TryGetPrioritizedHit(screenPosition, out RaycastHit hit, out string hitTag))
            {
                if (hitTag == "Slot")
                {
                    InGameTileView tileView = hit.transform.GetComponent<InGameTileView>();
                    if (tileView != null && tileView.AllianceType == AllianceType.Player)
                    {
                        // 타일 위에 드롭 - 실제 캐릭터 배치
                        await ConfirmGhostPlacement(tileView);
                        return;
                    }
                }
            }

            // 배치 불가 - 고스트 제거
            CancelGhostDrag();
        }
    }

    /// <summary>
    /// 고스트 배치 확정 - 실제 캐릭터 생성
    /// </summary>
    private async UniTask ConfirmGhostPlacement(InGameTileView tileView)
    {
        if (_ghostCharacterController == null || _ghostStatData == null)
            return;

        var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
        if (inGameTile == null)
        {
            CancelGhostDrag();
            return;
        }

        // 튜토리얼 UI 캐릭터 배치 - 타겟 타일이 아니면 배치 취소 (기존 캐릭터 반환 전에 체크)
        if (TutorialActionCharacterPlacementUI.IsActive)
        {
            if (!TutorialActionCharacterPlacementUI.CanPlaceOnTile(tileView.ID))
            {
                Debug.LogColor($"[InGameTouchManager] 튜토리얼 타겟 타일이 아님. 배치 취소. tileId={tileView.ID}, targetId={TutorialActionCharacterPlacementUI.GetTargetTileId()}", "yellow");
                CancelGhostDrag();
                return;
            }
        }

        // 기존 캐릭터가 있으면 UI로 반환하고 그 자리에 배치
        if (inGameTile.OccupiedCharacter != null)
        {
            var existingCharacter = inGameTile.OccupiedCharacter;
            if(existingCharacter.SpecCharacter.character_type == CharacterType.BATTLEITEM)
            {
                CancelGhostDrag();
                return;
            }

            // 기존 캐릭터를 타일에서 제거하고 UI로 반환
            existingCharacter.CurrentTile.SetUnoccupied();
            InGameObjectManager.Instance.RemoveCharacterFromField(existingCharacter);
            InGameMain.GetInGameMain().ReturnCharacterUI(existingCharacter);
        }

        //유저 카운트 체크
        var userKnightCount = SpecDataManager.Instance.GetUserKnightCountByNestCount().maximum_character_count;
        if (userKnightCount <= InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count
        && InGameMainFlowManager.Instance.CurrentFlowState is not FlowStateInGameTestReady)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_OVER_INT_CHARACTER");
            CancelGhostDrag();
            return;
        }

        // 고스트 위치 저장
        Vector3 placementPosition = _ghostCharacterController.Position3D;
        CharacterStatData statData = _ghostStatData;
        Action<CharacterStatData> onConfirmed = _onPlacementConfirmed;

        // 고스트 제거
        InGameObjectManager.Instance.RemoveGhostCharacter(_ghostCharacterController);
        _selectedTileView?.SetActiveObj(false);
        HideRecommendedRowVfx();
        ClearGhostState();

        //



        //

        // 실제 캐릭터 생성 (Synergy 등록됨)
        int2 pos = new int2(inGameTile.X, inGameTile.Y);
        var realCharacter = await InGameObjectManager.Instance.AddCharacterToField(
            statData, pos, AllianceType.Player,
            typeof(CharacterStateReady), true, HpBarType.Synergy, isSummonFx: false);

        if (realCharacter != null)
        {
            // 배치 확정 콜백 호출 (UI 리스트에서 제거)
            onConfirmed?.Invoke(statData);

            // 튜토리얼 중이면 TutorialTarget 등록
            if (TutorialManager.Instance.HasTutorialStage)
            {
                var characterView = realCharacter.GetCharacterView();
                if (characterView != null)
                {
                    var tutorialTarget = characterView.gameObject.GetComponent<TutorialTarget>();
                    if (tutorialTarget == null)
                    {
                        tutorialTarget = characterView.gameObject.AddComponent<TutorialTarget>();
                    }
                    tutorialTarget.SetTargetId(realCharacter.CharacterId.ToString());
                }
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_position_drop);

            // 튜토리얼 처리
            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                if (TutorialActionCharacterPlacementUI.CanPlaceOnTile(tileView.ID))
                {
                    TutorialActionCharacterPlacementUI.NotifyPlacementCompleted();
                    TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.CHARACTER_PLACEMENT, realCharacter.CharacterId.ToString());
                }
            }
        }

        _isMoveEndAnimation = false;
    }

    /// <summary>
    /// 고스트 드래그 취소 - 고스트만 제거
    /// </summary>
    private void CancelGhostDrag()
    {
        if (_ghostCharacterController == null)
            return;

        InGameObjectManager.Instance.RemoveGhostCharacter(_ghostCharacterController);
        _selectedTileView?.SetActiveObj(false);
        HideRecommendedRowVfx();
        ClearGhostState();

        // 튜토리얼 취소 알림
        if (TutorialActionCharacterPlacementUI.IsActive)
        {
            TutorialActionCharacterPlacementUI.OnDragCancel();
        }
    }

    /// <summary>
    /// 고스트 상태 초기화
    /// </summary>
    private void ClearGhostState()
    {
        _ghostCharacterController = null;
        _ghostStatData = null;
        _onPlacementConfirmed = null;
        _selectedCharacterController = null;
        _selectedTileView = null;
        _selectedFirstTileView = null;
        _isDragFromUI = false;
        _isMoveEndAnimation = false;
    }

    /////////////////////////////////////////////////////////////
    // 추천 행 VFX

    /// <summary>
    /// CharacterPositionType에 따른 추천 Y 위치 반환
    /// (InGameGrid.GetRecommandedTile과 동일한 기준)
    /// </summary>
    private static int GetRecommendedYPosition(CharacterPositionType posType)
    {
        switch (posType)
        {
            case CharacterPositionType.GUARDIAN:
            case CharacterPositionType.STRIKER:
                return 2;
            case CharacterPositionType.ESPER:
            case CharacterPositionType.ORACLE:
                return 1;
            case CharacterPositionType.SHARPSHOOTER:
            case CharacterPositionType.GHOST:
                return 0;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 추천 행 VFX 표시 - 해당 Y행의 Player 타일 중심에 배치
    /// </summary>
    private void ShowRecommendedRowVfx(CharacterPositionType posType)
    {
        HideRecommendedRowVfx();

        var grid = InGameObjectManager.Instance.InGameGrid;
        int targetY = GetRecommendedYPosition(posType);

        // 해당 Y행의 Player 타일들 조회
        var playerTilesInRow = grid.GetAllTiles()
            .Where(t => t.Y == targetY && t.View.AllianceType == AllianceType.Player)
            .OrderBy(t => t.X)
            .ToList();

        if (playerTilesInRow.Count == 0)
            return;

        // 첫 타일과 마지막 타일 Position의 중심점 계산
        Vector3 firstPos = playerTilesInRow.First().View.CachedTr.position;
        Vector3 lastPos = playerTilesInRow.Last().View.CachedTr.position;
        Vector3 centerPos = (firstPos + lastPos) * 0.5f;

        // VFX 생성 및 배치
        _recommendedRowVfx = InGameVfxManager.Instance.AddInGameVfx(
            InGameVfxNameType.fx_common_area_position, centerPos);
    }

    /// <summary>
    /// 추천 행 VFX 숨기기
    /// </summary>
    private void HideRecommendedRowVfx()
    {
        if (_recommendedRowVfx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_recommendedRowVfx);
            _recommendedRowVfx = null;
        }
    }
}
