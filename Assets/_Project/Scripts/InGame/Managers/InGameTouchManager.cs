using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.EventSystems;
using CharacterController = CookApps.BattleSystem.CharacterController;
using PrimeTween;
using UnityEngine.Pool;

public class InGameTouchManager : SingletonMonoBehaviour<InGameTouchManager>
{
    private bool _touchLocked = false;

    private CharacterController _selectedCharacterController = null;
    private InGameTileView _selectedTileView = null;
    private InGameTileView _selectedFirstTileView = null;

    private Vector3 _offset;
    private bool _isMoveEndAnimation;

    private float _initialFingersDistance;
    private float _initialCameraSize;
    private readonly float _cameraMinSize = 5.0f;
    private readonly float _cameraMaxSize = 10.0f;

    private Vector2 _initialFingersPosition;
    private Vector3 _initialCameraPosition;
    private Vector2 _minCameraPosition = new Vector2(-10, -10);
    private Vector2 _maxCameraPosition = new Vector2(10, 10);


    /////////////////////////////////////////////////////////////
    // protected

    protected void Update()
    {
        if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageReady))
            return;

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch2.phase == TouchPhase.Began)
            {
                _initialFingersDistance = Vector2.Distance(touch1.position, touch2.position);
                _initialCameraSize = InGameCommanderManager.Instance.InGameCamera.GetCameraSize();

                _initialFingersPosition = (touch1.position + touch2.position) / 2;
                _initialCameraPosition = InGameCommanderManager.Instance.InGameCamera.GetCameraTransform().position;
            }
            else if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                // 핀치 줌 처리
                var currentFingersDistance = Vector2.Distance(touch1.position, touch2.position);
                var scaleFactor = _initialFingersDistance / currentFingersDistance;

                float size = _initialCameraSize * scaleFactor;
                size = Mathf.Clamp(Camera.main.orthographicSize, _cameraMinSize, _cameraMaxSize);
                InGameCommanderManager.Instance.InGameCamera.SetCameraSize(size);

                // 스와이프 처리
                Vector2 currentFingersPosition = (touch1.position + touch2.position) / 2;
                Vector2 positionDelta = _initialFingersPosition - currentFingersPosition;

                Vector3 newCameraPosition = new Vector3(
                    _initialCameraPosition.x + positionDelta.x,
                    _initialCameraPosition.y + positionDelta.y,
                    _initialCameraPosition.z
                );

                newCameraPosition.x = Mathf.Clamp(newCameraPosition.x, _minCameraPosition.x, _maxCameraPosition.x);
                newCameraPosition.y = Mathf.Clamp(newCameraPosition.y, _minCameraPosition.y, _maxCameraPosition.y);

                InGameCommanderManager.Instance.InGameCamera.SetCameraPosition(newCameraPosition);
            }
        }

        bool isPointerOverUI = false;
        if (Application.isEditor)
        {
            if (EventSystem.current != null)
            {
                isPointerOverUI = EventSystem.current.currentSelectedGameObject != null;
                if (EventSystem.current.IsPointerOverGameObject())
                    isPointerOverUI = true;
            }

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
                if (EventSystem.current != null)
                {
                    isPointerOverUI = EventSystem.current.currentSelectedGameObject != null;
                    if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
                        isPointerOverUI = true;
                    if (IsPointerOverUIObject())
                        isPointerOverUI = true;
                }

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

        Ray ray = CameraManager.Main.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.transform.CompareTag("Slot"))
        {
            InGameTileView inGameTileView = hit.transform.GetComponent<InGameTileView>();
            InGameTile tile =
                InGameObjectManager.Instance.GetInGameTile(inGameTileView.ID);
            if (tile.IsOccupied() && tile.OccupiedCharacter.AllianceType == AllianceType.Player)
            {
                _selectedTileView = inGameTileView;
                SetSelectedCharacter(tile.OccupiedCharacter);
            }
        }
    }

    private void MoveCharacter(Vector3 touchPosition)
    {
        if (_isMoveEndAnimation)
            return;

        if (_selectedCharacterController != null)
        {
            Ray ray = CameraManager.Main.ScreenPointToRay(touchPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) && hit.transform.CompareTag("Slot"))
            {
                InGameTileView ingameTileView = hit.transform.GetComponent<InGameTileView>();
                if (ingameTileView.ID != _selectedTileView.ID &&
                    ingameTileView.AllianceType == AllianceType.Player)
                {
                    _selectedTileView.SetActiveObj(false);
                    _selectedTileView = ingameTileView;
                    _selectedTileView.SetActiveObj(true);

                    Vector3 targetPosition = ingameTileView.CachedTr.transform.position;
                    float duration = 0.15f;

                    Tween.Custom(
                        _selectedCharacterController.Position3D,
                        targetPosition,
                        duration,
                        (Vector3 value) =>
                        {
                            if (_selectedCharacterController != null)
                            {
                                // Debug.LogColor($"position : {_selectedCharacterController.Position3D} / target : {targetPosition}");
                                _selectedCharacterController.Position3D = value;
                            }
                        });
                }
            }
        }
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

        if (returnObjResult.gameObject != null)
        {
            var inGameMain = InGameMain.GetInGameMain();
            CharacterController deleteCharacterController = _selectedCharacterController;
            ReleaseSelectedHero(true);
            inGameMain.ReturnCharacter(deleteCharacterController);
        }
        else
        {
            InGameTile tile = InGameObjectManager.Instance.GetInGameTile(_selectedTileView.ID);
            HandleCharacterTileChange(tile, _selectedTileView);
        }
    }

    private void HandleCharacterTileChange(InGameTile tile, InGameTileView ingameTileView)
    {
        if (tile.OccupiedCharacter != null)
        {
            if (tile.OccupiedCharacter.AllianceType == AllianceType.Wall || tile.OccupiedCharacter.AllianceType == AllianceType.Neutral)
            {
                var inGameTile = InGameObjectManager.Instance.GetInGameTile(_selectedFirstTileView.ID);

                AnimateCharacterMove(_selectedCharacterController, inGameTile.View.Position, () =>
                {
                    CancelMoveToFirstTile();
                });
            }
            else
            {
                CharacterController targetCharacter = tile.OccupiedCharacter;
                if (_selectedCharacterController == targetCharacter)
                {
                    CancelMoveCharacter();
                }
                else
                {
                    AnimateCharacterSwap(_selectedCharacterController, targetCharacter);
                }
            }
        }
        else
        {
            Vector3 targetPosition = ingameTileView.CachedTr.transform.position;
            AnimateCharacterMove(_selectedCharacterController, targetPosition, () =>
            {
                _selectedCharacterController.ChangeOccupiedTile(tile);
                CancelMoveCharacter();
            });
        }
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
            (Vector3 value) =>
            {
                character.Position3D = value;
            }).OnComplete(onComplete);
    }

    private void SetSelectedCharacter(CharacterController character)
    {
        _selectedCharacterController = character;
        _selectedTileView.SetActiveObj(true);
        _selectedFirstTileView = _selectedTileView;
        _selectedCharacterController.SetSelectedCharacter(true);
        InGameMain.GetInGameMain().ReturnObjectActive(true);
        InGameMain.GetInGameMain().SetFocusSlot(character.GetCharacterStat().Spec);

        InGameMain.GetInGameMain().ShowSKillTooltip(_selectedCharacterController.GetCharacterStat());
    }

    private void ReleaseSelectedHero(bool isDropFx = false)
    {
        if (_selectedCharacterController != null)
        {
            _selectedCharacterController.SetSelectedCharacter(false);
            InGameMain.GetInGameMain().ReturnObjectActive(false);
            _selectedTileView.SetActiveObj(false);
            _selectedCharacterController = null;
            _selectedFirstTileView = null;
            InGameMain.GetInGameMain().UnSetFocusSlot(isDropFx);

            InGameMain.GetInGameMain().CloseSkillTooltip();
            _isMoveEndAnimation = false;
        }
    }
}
