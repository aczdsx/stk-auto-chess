using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using LitMotion;
using System.Linq;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;

/// <summary>
/// [FieldControl] 보드 위 캐릭터 조작
/// - 캐릭터 선택(터치) · 드래그 이동 · 타일 스왑
/// - 배틀아이템 드래그 적용
/// - BottomUI 드롭으로 캐릭터 반환
/// </summary>
public partial class InGameTouchManager
{
    private void CheckSelectedCharacter(Vector3 touchPosition)
    {
        if (_isMoveEndAnimation)
            return;

        if (_selectedCharacterController != null)
            return;

        // 튜토리얼 캐릭터 배치 UI 중에는 보드 위 캐릭터 선택(드래그) 차단
        if (TutorialActionCharacterPlacementUI.IsActive)
            return;

        _isDragFromUI = false;  // 보드에서 직접 선택

        // RaycastAll로 Slot 우선 검색
        if (!TryGetPrioritizedHit(touchPosition, out RaycastHit hit, out string hitTag))
            return;

        if (hitTag != "Slot")
            return;

        InGameTileView inGameTileView = hit.transform.GetComponent<InGameTileView>();
        InGameTile tile = InGameObjectManager.Instance.GetInGameTile(inGameTileView.ID);

        // Enemy 보스 몬스터 클릭 시 스킬 툴팁 표시
        if (tile.OccupiedCharacter != null &&
            tile.OccupiedCharacter.AllianceType == AllianceType.Enemy)
        {
            var specMonster = tile.OccupiedCharacter.GetCharacterStat()?.Spec as MonsterInfo;
            if (specMonster != null && specMonster.character_type == CharacterType.BOSS)
            {
                InGameMain.GetInGameMain().ShowEnemySkillTooltip(specMonster);
                return;
            }
        }

        if (InGameMain.GetInGameMain().IsCheckTouchTile(tile) || CheckCanTouchTile(tile, inGameTileView))
        {
            // 튜토리얼 오브젝트 이동 중일 때 Source 오브젝트만 선택 가능
            if (TutorialActionMoveObject.IsActive)
            {
                int targetId = inGameTileView.ID;
                if (!TutorialActionMoveObject.CanSelectFromTile(targetId))
                {
                    return;
                }
            }


            _selectedTileView = inGameTileView;
            SetSelectedCharacter(tile.OccupiedCharacter);
        }
    }
    private bool CheckCanTouchTile(InGameTile tile, InGameTileView inGameTileView)
    {
        if (tile.OccupiedCharacter == null)
            return false;

        if (tile.OccupiedCharacter.AllianceType != AllianceType.Wall
            && tile.OccupiedCharacter.SpecCharacter.character_type == CharacterType.BATTLEITEM)
        {
            return true;
        }

        return false;
    }

    private void MoveCharacter(Vector3 touchPosition)
    {
        if (_isMoveEndAnimation || _selectedCharacterController == null)
            return;

        // 드래그 중 BottomUI 영역 하이라이트 체크 (BattleItem 제외)
        UpdateDropHighlight(touchPosition);

        // RaycastAll로 Slot 우선 검색
        if (!TryGetPrioritizedHit(touchPosition, out RaycastHit hit, out string hitTag))
            return;

        bool isBattleItem = _selectedCharacterController.AllianceType == AllianceType.BattleItem;

        // 1. 타일 위인 경우 → 기존 로직 (타일에 스냅)
        if (hitTag == "Slot")
        {
            InGameTileView ingameTileView = hit.transform.GetComponent<InGameTileView>();
            if (ingameTileView == null)
                return;

            var ecc = _selectedCharacterController.GetEffectCodeContainer();
            if (ecc != null)
            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCharacterDragging);
                EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCharacterDraggingLambda);
            }

            // 타일 변경 조건 체크
            bool canMoveToTile = CanMoveToTile(ingameTileView, isBattleItem);
            if (!canMoveToTile)
                return;

            UpdateSelectedTileAndAnimate(ingameTileView);
        }
        // 2. 배경(Playground) 위인 경우 → 자유 이동
        else if (hitTag == "Playground")
        {
            // 타일 하이라이트 해제
            InActiveAttackTile();
            _selectedTileView?.SetActiveObj(false);

            // hit.point의 Y값을 캐릭터 높이로 보정
            Vector3 targetPos = hit.point;
            targetPos.y = _selectedCharacterController.Position3D.y;

            _selectedCharacterController.Position3D = targetPos;
            _selectedCharacterController.GetCharacterView().CachedTr.localPosition = targetPos;
        }
    }

    /// <summary>
    /// 드래그 중 BottomUI 영역 하이라이트 업데이트
    /// </summary>
    private void UpdateDropHighlight(Vector3 touchPosition)
    {
        if (_selectedCharacterController == null) return;
        if (_selectedCharacterController.SpecCharacter.character_type == CharacterType.BATTLEITEM) return;

        var inGameMain = InGameMain.GetInGameMain();
        bool isInBottomUI = inGameMain.IsPointInBottomScrollRect(touchPosition);
        inGameMain.SetDropHighlight(isInBottomUI);
    }

    /// <summary>
    /// 선택된 캐릭터가 해당 타일로 이동할 수 있는지 확인합니다.
    /// </summary>
    private bool CanMoveToTile(InGameTileView targetTileView, bool isBattleItem)
    {
        // 같은 타일이면 이동 불가
        if (targetTileView.ID == _selectedTileView.ID)
            return false;

        // 튜토리얼 오브젝트 이동 중일 때 Destination 타일로만 이동 가능
        if (TutorialActionMoveObject.IsActive)
        {
            if (!TutorialActionMoveObject.CanMoveToTile(targetTileView.ID))
                return false;
        }

        if (isBattleItem)
        {
            //배틀아이템은 타일에 효과코드가 있으면 이동 불가
            var TileEcc = InGameObjectManager.Instance.InGameGrid.GetTile(targetTileView.ID).EffectCodeContainer;
            if (TileEcc != null)
            {
                if (TileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_TRAP) is not null
                || TileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_FIRE) is not null
                || TileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_ICE) is not null
                || TileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_LANDMINE) is not null
                || TileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_SANDSTORM) is not null
                || TileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_RANDOM_MOVE) is not null
                )
                {
                    return false;
                }
            }

        }

        // BattleItem은 적 타일이 아닌 곳으로 이동 가능
        // 일반 캐릭터는 플레이어 타일로만 이동 가능
        return isBattleItem
            ? targetTileView.AllianceType != AllianceType.Enemy
            : targetTileView.AllianceType == AllianceType.Player;
    }

    /// <summary>
    /// 선택된 타일을 업데이트하고 캐릭터 이동 애니메이션을 실행합니다.
    /// </summary>
    private void UpdateSelectedTileAndAnimate(InGameTileView targetTileView)
    {
        // 이전 타일 비활성화
        _selectedTileView.SetActiveObj(false);
        InActiveAttackTile();

        // 새 타일 선택
        _selectedTileView = targetTileView;
        var inGameTile = InGameObjectManager.Instance.GetInGameTile(_selectedTileView.ID);
        _selectedTileView.SetActiveObj(true);

        // 공격 범위 타일 활성화
        if (_selectedCharacterController.GetCharacterStat() != null)
        {
            ActiveAttackTile(inGameTile, _selectedCharacterController.AttackRange);
        }

        // 캐릭터 이동 애니메이션
        AnimateCharacterPosition(targetTileView.CachedTr.transform.position);
    }

    /// <summary>
    /// 캐릭터 위치를 애니메이션으로 이동시킵니다.
    /// </summary>
    private void AnimateCharacterPosition(Vector3 targetPosition)
    {
        const float duration = 0.15f;

        LMotion.Create(
            _selectedCharacterController.Position3D,
            targetPosition,
            duration)
            .Bind(value =>
            {
                if (_selectedCharacterController != null)
                {
                    _selectedCharacterController.Position3D = value;
                    _selectedCharacterController.GetCharacterView().CachedTr.localPosition = value;
                }
            })
            .AddTo(this);
    }

    private void CancelMoveCharacter(bool isSameHero = false)
    {
        Debug.LogColor("CancelMoveCharacter");
        if (_selectedCharacterController != null)
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_selectedTileView.ID);

            _selectedCharacterController.ChangeOccupiedTile(inGameTile);
            ReleaseSelectedHero();
        }
    }

    private void CancelMoveToFirstTile()
    {
        Debug.LogColor("CancelMoveCharacter");
        if (_selectedCharacterController != null)
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_selectedFirstTileView.ID);

            _selectedCharacterController.ChangeOccupiedTile(inGameTile);
            ReleaseSelectedHero();
        }
    }

    private void EndedMoveCharacter(Vector3 touchPosition)
    {
        if (_isMoveEndAnimation)
            return;
        if (_selectedCharacterController == null)
            return;

        _isMoveEndAnimation = true;

        // BottomUI ScrollRect 영역 체크로 반환 판정
        var inGameMain = InGameMain.GetInGameMain();
        bool isInBottomUI = inGameMain.IsPointInBottomScrollRect(touchPosition);

        // 드래그 종료 시 하이라이트 해제
        inGameMain.SetDropHighlight(false);

        if (isInBottomUI &&
            _selectedCharacterController.SpecCharacter.character_type != CharacterType.BATTLEITEM)
        {
            // BottomUI 영역에 드롭 - 캐릭터 반환
            ReturnCharacterToUI();
        }
        else
        {
            // RaycastAll로 Slot 우선 검색
            if (TryGetPrioritizedHit(touchPosition, out RaycastHit hit, out string hitTag))
            {
                if (hitTag == "Slot")
                {
                    // 타일 위에 드롭 - 기존 로직
                    InGameTile tile = InGameObjectManager.Instance.GetInGameTile(_selectedTileView.ID);
                    HandleCharacterTileChange(tile, _selectedTileView);
                }
                else if (hitTag == "Playground")
                {
                    // 배경 위에 드롭
                    HandlePlaygroundDrop();
                }
                else
                {
                    // 기타 영역 - 원래 타일로 복귀
                    ReturnToOriginalTile();
                }
            }
            else
            {
                // Raycast 실패 - 원래 타일로 복귀
                ReturnToOriginalTile();
            }
        }
    }

    /// <summary>
    /// 캐릭터를 BottomUI로 반환
    /// </summary>
    private void ReturnCharacterToUI()
    {
        CharacterController characterToReturn = _selectedCharacterController;
        ReleaseSelectedHero(isDropFx: true, skipTutorialCheck: true);
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_position_back);

        characterToReturn.CurrentTile.SetUnoccupied();
        InGameObjectManager.Instance.RemoveCharacterFromField(characterToReturn);
        InGameMain.GetInGameMain().ReturnCharacterUI(characterToReturn);
    }

    /// <summary>
    /// 배경(Playground)에 드롭 시 처리
    /// UI에서 올린 캐릭터면 UI로 반환, 보드 내 이동이면 원래 타일로 복귀
    /// </summary>
    private void HandlePlaygroundDrop()
    {
        if (_isDragFromUI)
        {
            // UI에서 올린 캐릭터 → UI로 반환
            ReturnCharacterToUI();
        }
        else
        {
            // 보드 내 이동 → 원래 타일로 복귀
            ReturnToOriginalTile();
        }
    }

    private void HandleCharacterTileChange(InGameTile tile, InGameTileView ingameTileView)
    {
        // 튜토리얼 UI 캐릭터 배치 - 타겟 타일이 아니면 배치 취소
        if (TutorialActionCharacterPlacementUI.IsActive)
        {
            if (!TutorialActionCharacterPlacementUI.CanPlaceOnTile(tile.View.ID))
            {
                // 캐릭터 제거 및 UI로 복귀
                CharacterController characterToRemove = _selectedCharacterController;
                ReleaseSelectedHero();
                characterToRemove.CurrentTile.SetUnoccupied();
                InGameObjectManager.Instance.RemoveCharacterFromField(characterToRemove);
                InGameMain.GetInGameMain().ReturnCharacterUI(characterToRemove);
                return;
            }
        }

        if (tile.OccupiedCharacter == null)
        {
            HandleEmptyTileMove(tile, ingameTileView);
            return;
        }

        CharacterController tileOccupiedCharacter = tile.OccupiedCharacter;

        // 1. 아이템 적용 가능 여부를 먼저 체크
        if (CanApplyItemToTarget(tileOccupiedCharacter, out CharacterController itemObj, out CharacterController targetObj))
        {
            if (TryApplyItem(itemObj, targetObj))
            {
                // 아이템 적용 성공 시 이동 완료
                return;
            }
            // 아이템 적용 실패 시 원래 위치로 복귀
            ReturnToOriginalTile();
            return;
        }

        // 2. 아이템 적용 불가능한 경우 플레이어 타일인지 체크
        bool isPlayerTile = InGameMain.GetInGameMain().IsCheckTouchTile(tile);
        if (isPlayerTile)
        {
            HandlePlayerTileInteraction(tile);
        }
        else
        {
            // 플레이어 타일이 아닌 경우 원래 위치로 복귀
            ReturnToOriginalTile();
        }
    }

    private void HandleEmptyTileMove(InGameTile tile, InGameTileView ingameTileView)
    {
        Vector3 targetPosition = ingameTileView.CachedTr.transform.position;
        AnimateCharacterMove(_selectedCharacterController, targetPosition, () =>
        {
            _selectedCharacterController.ChangeOccupiedTile(tile);
            CancelMoveCharacter();
        });
    }

    private void HandlePlayerTileInteraction(InGameTile tile)
    {
        CharacterController targetCharacter = tile.OccupiedCharacter;

        if (_selectedCharacterController == targetCharacter)
        {
            // 같은 캐릭터라면 이동 취소
            CancelMoveCharacter();
        }
        else
        {
            // 다른 캐릭터라면 위치 스왑
            AnimateCharacterSwap(_selectedCharacterController, targetCharacter);
        }
    }

    private bool CanApplyItemToTarget(CharacterController targetCharacter, out CharacterController itemObj, out CharacterController targetObj)
    {
        itemObj = null;
        targetObj = null;
        // 선택된 캐릭터가 아이템이고, 타겟 캐릭터가 벽이 아닌 경우
        if (_selectedCharacterController == null || targetCharacter == null)
        {
            return false;
        }

        if (_selectedCharacterController == targetCharacter)
        {
            return false;
        }

        if (InGameSynergyManager.Instance.IsDragAndDropBattleItem(_selectedCharacterController))
        {
            itemObj = _selectedCharacterController;
            targetObj = targetCharacter;
        }
        else if (InGameSynergyManager.Instance.IsDragAndDropBattleItem(targetCharacter))
        {
            itemObj = targetCharacter;
            targetObj = _selectedCharacterController;
        }

        if (itemObj == null || targetObj == null)
        {
            return false;
        }

        bool isTargetValid = targetObj.AllianceType != AllianceType.Wall
            && targetObj.SpecCharacter != null;

        return isTargetValid;
    }

    private bool TryApplyItem(CharacterController itemObj, CharacterController targetObj)
    {
        return ApplyItem(itemObj, targetObj);
    }

    private void ReturnToOriginalTile()
    {
        var inGameTile = InGameObjectManager.Instance.GetInGameTile(_selectedFirstTileView.ID);
        AnimateCharacterMove(_selectedCharacterController, inGameTile.View.Position,
            () => { CancelMoveToFirstTile(); });
    }

    private void AnimateCharacterSwap(CharacterController character1, CharacterController character2)
    {
        if (character1 == null || character2 == null)
            return;

        Vector3 startPosition1 = character1.Position3D;
        Vector3 targetPosition1 = character2.Position3D;

        Vector3 startPosition2 = character2.Position3D;
        Vector3 targetPosition2 = character1.CurrentTile.View.Position;

        float duration = 0.15f;
        int completedTweens = 0;

        LMotion.Create(
            startPosition1,
            targetPosition1,
            duration)
            .WithOnComplete(() =>
            {
                completedTweens++;
                if (completedTweens >= 2)
                {
                    CompleteSwap(character1, character2, targetPosition1);
                }
            })
            .Bind(value =>
            {
                if (character1 != null)
                {
                    character1.Position3D = value;
                    character1.GetCharacterView().CachedTr.localPosition = value;
                }
            })
            .AddTo(this);

        LMotion.Create(
            startPosition2,
            targetPosition2,
            duration)
            .WithOnComplete(() =>
            {
                completedTweens++;
                if (completedTweens >= 2)
                {
                    CompleteSwap(character1, character2, targetPosition1);
                }
            })
            .Bind(value =>
            {
                character2.Position3D = value;
                character2.GetCharacterView().CachedTr.localPosition = value;
            })
            .AddTo(this);
    }

    private void CompleteSwap(CharacterController character1, CharacterController character2, Vector3 targetPosition1)
    {
        if (character1 != null && character2 != null)
            InGameObjectManager.Instance.ChangeTileCharacterToCharacter(character1, character2);

        CancelMoveCharacter();
    }

    private void AnimateCharacterMove(CharacterController character, Vector3 targetPosition, Action onComplete)
    {
        Vector3 startPosition = character.Position3D;
        float duration = 0.15f;

        LMotion.Create(
            startPosition,
            targetPosition,
            duration)
            .WithOnComplete(onComplete)
            .Bind(value => { character.Position3D = value; })
            .AddTo(this);
    }

    private void SetSelectedCharacter(CharacterController character)
    {
        _selectedCharacterController = character;
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_position_pick);

        _selectedFirstTileView = _selectedTileView;
        _selectedTileView.SetActiveObj(true);
        if (character.GetCharacterStat() != null)
        {
            ActiveAttackTile(character.CurrentTile, character.AttackRange);
            _selectedCharacterController.SetSelectedCharacter(true);
            InGameMain.GetInGameMain().SetFocusSlotUI(character.GetCharacterStat().Spec as CharacterInfo);
            InGameMain.GetInGameMain().ShowSKillTooltip(_selectedCharacterController.GetCharacterStat());
        }
    }

    private bool ApplyItem(CharacterController itemObj, CharacterController targetObj)
    {
        if (!InGameSynergyManager.Instance.ApplyBattleItem(itemObj, targetObj))
        {
            return false;
        }

        var inGameTile = InGameObjectManager.Instance.GetInGameTile(_selectedTileView.ID);
        itemObj.ChangeOccupiedTile(inGameTile);
        ReleaseSelectedHero();
        itemObj.CurrentTile.SetOccupied(null);
        targetObj.ChangeOccupiedTile(inGameTile);
        return true;
    }
    private void ReleaseSelectedHero(bool isDropFx = false, bool skipTutorialCheck = false)
    {
        if (_selectedCharacterController != null)
        {
            // 드래그 종료 콜백 호출
            var ecc = _selectedCharacterController.GetEffectCodeContainer();
            if (ecc != null && ecc.EffectCodes is not null)
            {

                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCharacterDraggingEnd);
                EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCharacterDraggingEndLambda);
            }

            // 튜토리얼 캐릭터 배치 완료 알림 (타일이 변경되었을 때만)
            bool tileChanged = _selectedFirstTileView != null && _selectedTileView != null &&
                               _selectedFirstTileView.ID != _selectedTileView.ID;
            int placedCharacterId = _selectedCharacterController.CharacterId;
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_position_drop);

            _selectedCharacterController.SetSelectedCharacter(false, isDropFx: _isDragFromUI && tileChanged);
            _selectedTileView.SetActiveObj(false);
            _selectedFirstTileID = _selectedFirstTileView.ID;
            InActiveAttackTile();
            _attackRangeTileList.Clear();
            _selectedCharacterController = null;
            _selectedFirstTileView = null;
            InGameMain.GetInGameMain().UnSetFocusSlotUI(isDropFx);
            InGameMain.GetInGameMain().CloseSkillTooltip();
            _isMoveEndAnimation = false;
            _isDragFromUI = false;  // 플래그 초기화

            // 튜토리얼 체크 스킵 (UI로 반환하는 경우)
            if (skipTutorialCheck) return;

            // 튜토리얼 UI 캐릭터 배치 완료 콜백 호출 (CHARACTER_PLACEMENT_UI)
            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                Debug.LogColor($"[InGameTouchManager] NotifyPlacementCompleted: {placedCharacterId}", "green");

                if (TutorialActionCharacterPlacementUI.CanPlaceOnTile(_selectedTileView.ID))
                {
                    TutorialActionCharacterPlacementUI.NotifyPlacementCompleted();

                    TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.CHARACTER_PLACEMENT, placedCharacterId.ToString());
                }
            }

            // 튜토리얼 오브젝트 이동 완료 콜백 호출 (MOVE_OBJECT)
            if (tileChanged && TutorialActionMoveObject.IsActive)
            {
                // Source에서 Destination으로 이동했는지 확인
                var destTargetId = TutorialActionMoveObject.DestTileId;
                if (destTargetId != 0 && _selectedTileView != null)
                {
                    if (_selectedTileView.ID == destTargetId)
                    {
                        TutorialActionMoveObject.NotifyMoveCompleted();

                        // MOVE_OBJECT_AFTER 트리거 (오브젝트 이동 완료 후)
                        TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.MOVE_OBJECT_AFTER, _selectedTileView.ID.ToString());
                    }
                }
            }

            // InGameObjectManager.Instance.DrawPlayerLine(true);
            // InGameObjectManager.Instance.DrawPlayerLine(false);
        }
    }

    private void InActiveAttackTile()
    {
        foreach (var tile in _attackRangeTileList)
        {
            tile.View.SetAttackActiveObj(false);
        }

        _attackRangeTileList.Clear();
    }

    private void ActiveAttackTile(InGameTile pivot, int range)
    {
        var tiles = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(pivot, range);
        _attackRangeTileList.AddRange(tiles.ToList());
        _attackRangeTileList.Remove(pivot);
        foreach (var tile in _attackRangeTileList)
        {
            tile.View.SetAttackActiveObj(true);
        }
    }
}
