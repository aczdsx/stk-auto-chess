using System.Collections;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.EventSystems;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameTouchManager : SingletonMonoBehaviour<InGameTouchManager>
{
    private Camera _mainCamera = null;
    private bool _touchLocked = false;

    private CharacterController _selectedCharacterController = null;
    private InGameTileView _selectedTileView = null;

    private Vector3 _offset;

    /////////////////////////////////////////////////////////////
    // protected

    protected void Start()
    {
        _mainCamera = Camera.main;
    }

    protected void Update()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

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
                CancelMoveCharacter();
                break;
            case TouchPhase.Ended:
                CheckChangeCharacter(touchPosition);
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
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

private void CheckSelectedCharacter(Vector3 touchPosition)
{
    Ray ray = _mainCamera.ScreenPointToRay(touchPosition);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit))
    {
        if (hit.transform.CompareTag("Slot"))
        {
            InGameTileView inGameTileView = hit.transform.GetComponent<InGameTileView>();
            InGameTile tile =
                InGameObjectManager.Instance.GetInGameTile(inGameTileView.ID);
            if (tile.IsOccupied() && tile.OccupiedCharacter.AllianceType == AllianceType.Player)
            {
                _selectedTileView = inGameTileView;
                SetSelectedCharacter(tile.OccupiedCharacter);

                _offset = hit.transform.position -
                          _mainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y,
                              _mainCamera.nearClipPlane));
            }
        }
    }
}

private void MoveCharacter(Vector3 touchPosition)
{
    if (_selectedCharacterController != null)
    {
        Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, _mainCamera.nearClipPlane));
        Vector3 newCharacterPosition = worldPoint + _offset;
        newCharacterPosition.y = _selectedCharacterController.GetCharacterView().CachedTr.transform.position.y;

        Ray ray = _mainCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Slot"))
            {
                InGameTileView ingameTileView = hit.transform.GetComponent<InGameTileView>();
                if (ingameTileView.ID != _selectedTileView.ID)
                {
                    if (ingameTileView.AllianceAllianceType == AllianceType.Player)
                    {
                        _selectedTileView.SetActiveObj(false);
                        _selectedTileView = ingameTileView;
                        _selectedTileView.SetActiveObj(true);
                        _selectedCharacterController.Position3D =
                            ingameTileView.CachedTr.transform.position;
                    }
                }
                else
                {

                }
            }
        }
    }
}




    private void CancelMoveCharacter(bool isSameHero = false)
    {
        if (_selectedCharacterController != null)
        {
            _selectedCharacterController.ChangeTile(_selectedCharacterController.CurrentTile);
            ReleaseSelectedHero();
        }
    }

    private void CheckChangeCharacter(Vector3 touchPosition)
    {
        if (_selectedCharacterController != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(touchPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.tag.Equals("Slot"))
                {
                    InGameTileView ingameTileView = hit.transform.GetComponent<InGameTileView>();

                    if (ingameTileView.AllianceAllianceType == AllianceType.Player)
                    {
                        InGameTile tile =
                            InGameObjectManager.Instance.GetInGameTile(ingameTileView.ID);
                        if (tile.OccupiedCharacter != null)
                        {
                            InGameObjectManager.Instance.ChangeTileCharacterToCharacter(_selectedCharacterController, tile.OccupiedCharacter);
                        }
                        else
                        {
                            InGameObjectManager.Instance.ChangeTile(_selectedCharacterController, tile);
                        }
                    }
                }
            }
            CancelMoveCharacter();
        }
        else
        {
            CancelMoveCharacter();
        }
    }

    private void SetSelectedCharacter(CharacterController character)
    {
        _selectedCharacterController = character;
        _selectedTileView.SetActiveObj(true);
        _selectedCharacterController.SetSelectedCharacter(true);
    }

    private void ReleaseSelectedHero()
    {
        _selectedCharacterController.SetSelectedCharacter(false);
        _selectedTileView.SetActiveObj(false);
        _selectedCharacterController = null;
    }
}
