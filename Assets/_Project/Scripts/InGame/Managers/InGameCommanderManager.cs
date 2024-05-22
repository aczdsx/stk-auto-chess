using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InGameCommanderManager : SingletonMonoBehaviour<InGameCommanderManager>, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject switchObj;
    public float switchThreshold = 40f;
    public float maxFadeAlpha = 0.5f;

    private Camera _mainCamera;
    private bool _isDragging = false;
    private Vector2 _dragStartPosition;
    private Image _selectedImage;
    private InGameTileView _hitTileView;
    private List<InGameTile> _activeTiles = new List<InGameTile>();

    void Start()
    {
        _mainCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _dragStartPosition = eventData.position;

        // [TODO] CommanderSkill UI 제작 후 변경 필요.
        _selectedImage = eventData.pointerCurrentRaycast.gameObject?.GetComponent<Image>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            float distance = Vector2.Distance(eventData.position, _dragStartPosition);
            if (distance < switchThreshold)
            {
                // 거리에 따른 알파값 계산
                float normalizedDistance = Mathf.Clamp01(distance / switchThreshold);
                float fadeAlpha = Mathf.Lerp(0f, maxFadeAlpha, normalizedDistance);
                if (_selectedImage != null)
                {
                    Color color = _selectedImage.color;
                    color.a = fadeAlpha;
                    _selectedImage.color = color;
                }
            }
            else
            {
                Vector3 worldPos = HandleRuntimeDrag(eventData);
                switchObj.SetActive(true);
                switchObj.transform.position = worldPos;

                if (Physics.Raycast(_mainCamera.ScreenPointToRay(eventData.position), out RaycastHit hit))
                {
                    _hitTileView = hit.transform.GetComponent<InGameTileView>();
                    if (_hitTileView != null)
                    {
                        Debug.LogColor("충돌한 오브젝트: " + _hitTileView.ID);
                        InGameTile centerTile = InGameObjectManager.Instance.GetInGameTile(_hitTileView.ID);
                        var tiles = InGameObjectManager.Instance.InGameGrid.GetManhattanDistanceTiles(centerTile, 2);
                        ClearAndSetActive(tiles);
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        switchObj.SetActive(false);
        _isDragging = false;
        ClearAndSetActive(null);

        // [TODO] _hitTileView 해당 위치에 지휘자 스킬 액션
    }

    Vector3 HandleRuntimeDrag(PointerEventData eventData)
    {
        return _mainCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _mainCamera.nearClipPlane));
    }

    private void ClearAndSetActive(IEnumerable<InGameTile> newTiles)
    {
        foreach (var tile in _activeTiles)
        {
            tile.View.SetActiveObj(false);
        }
        _activeTiles.Clear();

        if (newTiles != null)
        {
            foreach (var tile in newTiles)
            {
                tile.View.SetActiveObj(true);
                _activeTiles.Add(tile);
            }
        }
    }
}
