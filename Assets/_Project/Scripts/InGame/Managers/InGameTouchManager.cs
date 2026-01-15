using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using CharacterController = CookApps.BattleSystem.CharacterController;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;
using PrimeTween;
using UnityEngine.Pool;
using static CookApps.AutoBattler.TutorialActionCharacterPlacement;

public class InGameTouchManager : SingletonMonoBehaviour<InGameTouchManager>
{
    private bool _touchLocked = false;

    private CharacterController _selectedCharacterController = null;
    private InGameTileView _selectedTileView = null;
    private List<InGameTile> _attackRangeTileList = new List<InGameTile>();
    private InGameTileView _selectedFirstTileView = null;
    private Vector3 _offset;
    private bool _isMoveEndAnimation;

    private float _initialFingersDistance;
    private float _initialCameraSize;
    private readonly float _cameraMinSize = 5.0f;
    private readonly float _cameraMaxSize = 9.0f;

    private Vector3 _initialFingersPosition;
    private Vector3 _initialCameraPosition;

    private readonly float _zoomCooldown = 0.1f;
    private float _zoomCooldownTimer = 0f;

    private int _selectedFirstTileID = -1;
    public int SelectedFirstTileID { get => _selectedFirstTileID; set => _selectedFirstTileID = value; }

    /////////////////////////////////////////////////////////////
    // protected

    protected void Update()
    {
        bool isPointerOverUI = false;
        if (Application.isEditor)
        {
            if (EventSystem.current != null)
            {
                isPointerOverUI = EventSystem.current.currentSelectedGameObject != null;
                if (EventSystem.current.IsPointerOverGameObject())
                    isPointerOverUI = true;
            }
        }
        else
        {
            if (Input.touchCount > 0)
            {
                if (EventSystem.current != null)
                {
                    isPointerOverUI = EventSystem.current.currentSelectedGameObject != null;
                    if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
                        isPointerOverUI = true;
                    if (IsPointerOverUIObject())
                        isPointerOverUI = true;

                    if (_selectedCharacterController == null)
                        CameraMove(isPointerOverUI);
                }
            }
        }

        if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateReadyBase))
            return;

        if (Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleTouch(Input.mousePosition, TouchPhase.Began, isPointerOverUI);
            }

            if (Input.GetMouseButton(0))
            {
                HandleTouch(Input.mousePosition, TouchPhase.Moved, isPointerOverUI);
            }

            if (Input.GetMouseButtonUp(0))
            {
                HandleTouch(Input.mousePosition, TouchPhase.Ended, isPointerOverUI);
            }
        }
        else
        {
            if (Input.touchCount > 0)
            {
                HandleTouch(Input.touches[0].position, Input.touches[0].phase, isPointerOverUI);
            }
        }
    }

    /////////////////////////////////////////////////////////////
    // private

    private void HandleTouch(Vector3 touchPosition, TouchPhase touchPhase, bool isPointerOverUI)
    {
        if (_touchLocked) return;
        touchPosition.z = 0;

        switch (touchPhase)
        {
            case TouchPhase.Began:
                // case TouchPhase.Stationary:
                CheckSelectedCharacter(touchPosition);
                break;
            case TouchPhase.Canceled:
                Debug.LogColor("Canceled");
                CancelMoveCharacter();
                break;
            case TouchPhase.Ended:
                EndedMoveCharacter(touchPosition);
                break;
            case TouchPhase.Moved:
                MoveCharacter(touchPosition);
                break;
        }
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        _ = ListPool<RaycastResult>.Get(out var results);
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    private void CheckSelectedCharacter(Vector3 touchPosition)
    {
        if (_isMoveEndAnimation)
            return;

        if (_selectedCharacterController != null)
            return;

        Ray ray = MainCameraHolder.MainCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.transform.CompareTag("Slot"))
        {
            InGameTileView inGameTileView = hit.transform.GetComponent<InGameTileView>();
            InGameTile tile =
                InGameObjectManager.Instance.GetInGameTile(inGameTileView.ID);

            if (InGameMain.GetInGameMain().IsCheckTouchTile(tile) || CheckCanTouchTile(tile, inGameTileView))
            {
                // 튜토리얼 캐릭터 배치 중일 때 특정 캐릭터만 선택 가능
                if (tile.OccupiedCharacter != null &&
                    !TutorialActionCharacterPlacement.CanSelectCharacter(tile.OccupiedCharacter.CharacterId))
                {
                    return;
                }

                _selectedTileView = inGameTileView;
                SetSelectedCharacter(tile.OccupiedCharacter);
            }
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

        Ray ray = MainCameraHolder.MainCamera.ScreenPointToRay(touchPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        // BattleItem의 경우 Slot 태그 체크 없이 진행, 일반 캐릭터는 Slot 태그 필요
        bool isBattleItem = _selectedCharacterController.AllianceType == AllianceType.BattleItem;
        if (!isBattleItem && !hit.transform.CompareTag("Slot"))
            return;

        InGameTileView ingameTileView = hit.transform.GetComponent<InGameTileView>();
        if (ingameTileView == null)
            return;

        // 타일 변경 조건 체크
        bool canMoveToTile = CanMoveToTile(ingameTileView, isBattleItem);
        if (!canMoveToTile)
            return;

        UpdateSelectedTileAndAnimate(ingameTileView);
    }

    /// <summary>
    /// 선택된 캐릭터가 해당 타일로 이동할 수 있는지 확인합니다.
    /// </summary>
    private bool CanMoveToTile(InGameTileView targetTileView, bool isBattleItem)
    {
        // 같은 타일이면 이동 불가
        if (targetTileView.ID == _selectedTileView.ID)
            return false;

        // 튜토리얼 캐릭터 배치 중일 때 특정 타일로만 이동 가능
        if (IsActive && TargetTileId >= 0)
        {
            if (!CanPlaceOnTile(targetTileView.ID))
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

        Tween.Custom(
            _selectedCharacterController.Position3D,
            targetPosition,
            duration,
            (Vector3 value) =>
            {
                if (_selectedCharacterController != null)
                {
                    _selectedCharacterController.Position3D = value;
                    _selectedCharacterController.GetCharacterView().CachedTr.localPosition = value;
                }
            });
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
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        _ = ListPool<RaycastResult>.Get(out var results);
        EventSystem.current.RaycastAll(pointerData, results);

        var returnObjResult = results.FirstOrDefault(r => r.gameObject != null && r.gameObject.CompareTag("ReturnObj"));

        if (returnObjResult.gameObject != null &&
        _selectedCharacterController.SpecCharacter.character_type != CharacterType.BATTLEITEM)
        {
            var inGameMain = InGameMain.GetInGameMain();
            CharacterController deleteCharacterController = _selectedCharacterController;
            ReleaseSelectedHero(true);

            deleteCharacterController.CurrentTile.SetUnoccupied();
            InGameObjectManager.Instance.RemoveCharacterFromField(deleteCharacterController);
            inGameMain.ReturnCharacterUI(deleteCharacterController);
        }
        else
        {
            InGameTile tile = InGameObjectManager.Instance.GetInGameTile(_selectedTileView.ID);
            HandleCharacterTileChange(tile, _selectedTileView);
        }
    }

    private void HandleCharacterTileChange(InGameTile tile, InGameTileView ingameTileView)
    {
        // 튜토리얼 UI 캐릭터 배치 - 타겟 타일이 아니면 배치 취소
        if (CookApps.AutoBattler.TutorialActionCharacterPlacementUI.IsActive)
        {
            if (!CookApps.AutoBattler.TutorialActionCharacterPlacementUI.CanPlaceOnTile(tile.View.ID))
            {
                // 캐릭터 제거 및 UI로 복귀
                CharacterController characterToRemove = _selectedCharacterController;
                ReleaseSelectedHero();
                characterToRemove.CurrentTile.SetUnoccupied();
                InGameObjectManager.Instance.RemoveCharacterFromField(characterToRemove);
                InGameMain.GetInGameMain().ReturnCharacterUI(characterToRemove);
                // 드래그 취소 처리 (마스크 홀 복원)
                CookApps.AutoBattler.TutorialActionCharacterPlacementUI.OnDragCancel();
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

        Tween.Custom(
            startPosition1,
            targetPosition1,
            duration,
            (Vector3 value) =>
            {
                if (character1 != null)
                {
                    character1.Position3D = value;
                    character1.GetCharacterView().CachedTr.localPosition = value;
                }
            }).OnComplete(() =>
        {
            completedTweens++;
            if (completedTweens >= 2)
            {
                CompleteSwap(character1, character2, targetPosition1);
            }
        });

        Tween.Custom(
            startPosition2,
            targetPosition2,
            duration,
            (Vector3 value) =>
            {
                character2.Position3D = value;
                character2.GetCharacterView().CachedTr.localPosition = value;
            }).OnComplete(() =>
        {
            completedTweens++;
            if (completedTweens >= 2)
            {
                CompleteSwap(character1, character2, targetPosition1);
            }
        });
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

        Tween.Custom(
            startPosition,
            targetPosition,
            duration,
            (Vector3 value) => { character.Position3D = value; }).OnComplete(onComplete);
    }

    private void SetSelectedCharacter(CharacterController character)
    {
        _selectedCharacterController = character;
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
    private void ReleaseSelectedHero(bool isDropFx = false)
    {
        if (_selectedCharacterController != null)
        {

            // 튜토리얼 캐릭터 배치 완료 알림 (타일이 변경되었을 때만)
            bool tileChanged = _selectedFirstTileView != null && _selectedTileView != null &&
                               _selectedFirstTileView.ID != _selectedTileView.ID;
            int placedCharacterId = _selectedCharacterController.CharacterId;

            _selectedCharacterController.SetSelectedCharacter(false);
            _selectedTileView.SetActiveObj(false);
            _selectedFirstTileID = _selectedFirstTileView.ID;
            InActiveAttackTile();
            _attackRangeTileList.Clear();
            _selectedCharacterController = null;
            _selectedFirstTileView = null;
            InGameMain.GetInGameMain().UnSetFocusSlotUI(isDropFx);
            InGameMain.GetInGameMain().CloseSkillTooltip();
            _isMoveEndAnimation = false;

            // 튜토리얼 캐릭터 배치 완료 콜백 호출 (CHARACTER_PLACEMENT)
            if (tileChanged && IsActive && CanSelectCharacter(placedCharacterId))
            {
                NotifyPlacementCompleted();
            }

            // 튜토리얼 UI 캐릭터 배치 완료 콜백 호출 (CHARACTER_PLACEMENT_UI)
            if (tileChanged && CookApps.AutoBattler.TutorialActionCharacterPlacementUI.IsActive)
            {
                CookApps.AutoBattler.TutorialActionCharacterPlacementUI.NotifyPlacementCompleted();
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

    private void CameraMove(bool isPointerOverUI)
    {
        Vector3 initialPosition = Vector3.zero;
        Vector3 currentPosition = Vector3.zero;
        bool isInputBegan = false;
        bool isInputMoved = false;
        float distanceFactor = -0.01f;
        // 줌 후 쿨다운 타이머 감소
        if (_zoomCooldownTimer > 0)
        {
            _zoomCooldownTimer -= Time.deltaTime;
        }

        if (!Application.isEditor)
        {
            if (Input.touchCount == 2)
            {
                HandleZoom(Input.GetTouch(0), Input.GetTouch(1));
                _zoomCooldownTimer = _zoomCooldown;
                return;
            }
            else if (Input.touchCount == 1 && _zoomCooldownTimer <= 0)
            {
                Touch touch = Input.GetTouch(0);
                initialPosition = touch.position;
                currentPosition = touch.position;
                isInputBegan = touch.phase == TouchPhase.Began;
                isInputMoved = touch.phase == TouchPhase.Moved;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                initialPosition = Input.mousePosition;
                isInputBegan = true;
            }

            if (Input.GetMouseButton(0))
            {
                currentPosition = Input.mousePosition;
                isInputMoved = true;
            }
        }

        if (_zoomCooldownTimer <= 0)
        {
            if (isInputBegan)
            {
                _initialFingersPosition = initialPosition;
                _initialCameraPosition = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).GetCameraTransform().position;
            }
            else if (isInputMoved && !isPointerOverUI)
            {
                Vector2 direction = (currentPosition - _initialFingersPosition).normalized;
                float cameraSize = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).GetCameraSize();
                float normalizedSize = (2.0f - (cameraSize - _cameraMinSize) / (_cameraMaxSize - _cameraMinSize)) * 0.3f;
                float distance = Vector2.Distance(currentPosition, _initialFingersPosition) * distanceFactor *
                                 normalizedSize;

                Vector2 distancePosition;
                distancePosition.x = direction.x * distance;
                distancePosition.y = direction.y * distance;

                Vector3 newCameraPosition = new Vector3(
                    Mathf.Clamp(_initialCameraPosition.x + distancePosition.x, -2, 2),
                    Mathf.Clamp(_initialCameraPosition.y + distancePosition.y, -2, 4),
                    Mathf.Clamp(_initialCameraPosition.z - distancePosition.x, -12, -8)
                );

                ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPosition(newCameraPosition);
            }
        }
    }

    private void HandleZoom(Touch touch1, Touch touch2)
    {
        if (touch2.phase == TouchPhase.Began)
        {
            _initialFingersDistance = Vector2.Distance(touch1.position, touch2.position);
            _initialCameraSize = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).GetCameraSize();
        }
        else if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
        {
            var currentFingersDistance = Vector2.Distance(touch1.position, touch2.position);
            var scaleFactor = _initialFingersDistance / currentFingersDistance;

            float size = _initialCameraSize * scaleFactor;
            size = Mathf.Clamp(size, _cameraMinSize, _cameraMaxSize);
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(size);
        }
    }

    /////////////////////////////////////////////////////////////
    // public - UI 드래그 연동

    /// <summary>
    /// UI에서 드래그하여 보드에 캐릭터를 배치할 때 호출
    /// </summary>
    public async UniTask<bool> StartDragFromUI(CharacterStatData statData, InGameTileView tileView)
    {
        if (_selectedCharacterController != null || _isMoveEndAnimation)
            return false;

        // 타일 유효성 체크
        if (tileView == null || tileView.AllianceType != AllianceType.Player)
            return false;

        var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
        if (inGameTile == null)
            return false;

        // 이미 캐릭터가 있는 타일이면 스왑 대상으로 처리
        if (inGameTile.OccupiedCharacter != null)
        {
            // 빈 타일 찾기
            var emptyTile = InGameObjectManager.Instance.InGameGrid.GetRecommandedTile(statData.Spec);
            if (emptyTile == null)
                return false;

            inGameTile = emptyTile;
            tileView = emptyTile.View as InGameTileView;
        }

        // 캐릭터 생성
        int2 pos = new int2(inGameTile.X, inGameTile.Y);
        await InGameObjectManager.Instance.AddCharacterToField(
            statData, pos, AllianceType.Player,
            typeof(CharacterStateReady), true, HpBarType.Synergy);

        // 생성된 캐릭터 가져오기
        var character = inGameTile.OccupiedCharacter;
        if (character == null)
            return false;

        // selected 상태로 전환
        _selectedTileView = tileView;
        SetSelectedCharacter(character);

        return true;
    }

    /// <summary>
    /// UI 드래그 중 터치 위치 업데이트 (MoveCharacter 대리 호출)
    /// </summary>
    public void UpdateDragPosition(Vector3 screenPosition)
    {
        if (_selectedCharacterController == null)
            return;

        MoveCharacter(screenPosition);
    }

    /// <summary>
    /// UI 드래그 종료 (EndedMoveCharacter 대리 호출)
    /// </summary>
    public void EndDragFromUI(Vector3 screenPosition)
    {
        if (_selectedCharacterController == null)
            return;

        EndedMoveCharacter(screenPosition);
    }

    /// <summary>
    /// UI 드래그 취소 - 캐릭터 제거 및 UI로 복귀
    /// </summary>
    public void CancelDragFromUI()
    {
        if (_selectedCharacterController == null)
            return;

        CharacterController characterToRemove = _selectedCharacterController;
        ReleaseSelectedHero();

        // 캐릭터 제거
        characterToRemove.CurrentTile.SetUnoccupied();
        InGameObjectManager.Instance.RemoveCharacterFromField(characterToRemove);
    }

    /// <summary>
    /// 스크린 좌표에서 타일뷰 가져오기
    /// </summary>
    public InGameTileView GetTileViewFromScreenPosition(Vector3 screenPosition)
    {
        Ray ray = MainCameraHolder.MainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.CompareTag("Slot"))
        {
            return hit.transform.GetComponent<InGameTileView>();
        }
        return null;
    }
}