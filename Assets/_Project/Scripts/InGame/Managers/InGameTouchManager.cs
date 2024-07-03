using System;
using System.Collections;
using System.Collections.Generic;
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

    /////////////////////////////////////////////////////////////
    // protected

    protected void Update()
    {
        if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageReady))
            return;

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
                CheckSelectedCharacter(touchPosition, isPointerOverUI);
                break;
            case TouchPhase.Canceled:
                Debug.LogColor("Canceled");
                CancelMoveCharacter();
                break;
            case TouchPhase.Ended:
                EndedMoveCharacter(touchPosition, isPointerOverUI);
                break;
            case TouchPhase.Moved:
                MoveCharacter(touchPosition, isPointerOverUI);
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

    private void CheckSelectedCharacter(Vector3 touchPosition, bool isPointerOverUI)
    {
        if (_selectedCharacterController != null)
            return;

        if (isPointerOverUI)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            _ = ListPool<RaycastResult>.Get(out var results);
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                GameObject topUIObject = results[0].gameObject;
                if (topUIObject != null && topUIObject.CompareTag("ReturnObj"))
                {
                    if (_selectedCharacterController != null)
                    {
                        CharacterController deleteCharacterController = _selectedCharacterController;
                        ReleaseSelectedHero();
                        InGameMain.GetInGameMain().ReturnCharacter(deleteCharacterController);
                    }
                }
            }
        }

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

    private void MoveCharacter(Vector3 touchPosition, bool isPointerOverUI)
    {
        if (_selectedCharacterController != null)
        {
            if (isPointerOverUI)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                _ = ListPool<RaycastResult>.Get(out var results);
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    GameObject topUIObject = results[0].gameObject;
                    InGameMain.GetInGameMain().ReturnObjectColorChange(topUIObject != null && topUIObject.CompareTag("ReturnObj"));
                }
                else
                {
                    InGameMain.GetInGameMain().ReturnObjectColorChange(false);
                }
            }
            else
            {
                InGameMain.GetInGameMain().ReturnObjectColorChange(false);
            }

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

    private void EndedMoveCharacter(Vector3 touchPosition, bool isPointerOverUI)
    {
        if (_selectedCharacterController != null)
        {
            Ray ray = CameraManager.Main.ScreenPointToRay(touchPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);

            InGameTileView ingameTileView = _selectedTileView;

            if (isPointerOverUI)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                _ = ListPool<RaycastResult>.Get(out var results);
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    GameObject topUIObject = results[0].gameObject;
                    if (topUIObject != null && topUIObject.CompareTag("ReturnObj"))
                    {
                        CharacterController deleteCharacterController = _selectedCharacterController;
                        ReleaseSelectedHero(true);
                        InGameMain.GetInGameMain().ReturnCharacter(deleteCharacterController);
                    }
                }
            }

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.tag.Equals("Slot"))
                {
                    InGameTileView hitTileView = hit.transform.GetComponent<InGameTileView>();
                    if (hitTileView.AllianceType == AllianceType.Player)
                    {
                        ingameTileView = hitTileView;
                        break;
                    }
                }
            }

            InGameTile tile = InGameObjectManager.Instance.GetInGameTile(ingameTileView.ID);
            HandleCharacterTileChange(tile, ingameTileView);
        }
    }

    private void HandleCharacterTileChange(InGameTile tile, InGameTileView ingameTileView)
    {
        if (tile.OccupiedCharacter != null)
        {
            if (tile.OccupiedCharacter.AllianceType == AllianceType.None)
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
            if (_selectedCharacterController != null)
            {
                Vector3 targetPosition = ingameTileView.CachedTr.transform.position;
                AnimateCharacterMove(_selectedCharacterController, targetPosition, () =>
                {
                    InGameObjectManager.Instance.ChangeTile(_selectedCharacterController, tile);
                    CancelMoveCharacter();
                });
            }
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
        InGameMain.GetInGameMain().SetFocusSlot(character.GetCharacterStat().Spec.prefab_id);
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
        }
    }
}
