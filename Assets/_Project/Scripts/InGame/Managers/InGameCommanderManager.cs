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
    public GameObject tempSwitchObj; // [TODO] 임시 Obj
    public float switchThreshold = 40f;
    public float maxFadeAlpha = 0.5f;

    private Camera _mainCamera;
    private bool _isDragging = false;
    private GameObject _instantiatedObj;
    private Vector2 _dragStartPosition;
    private Image _selectedImage;
    private InGameTileView _hitTileView;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _dragStartPosition = eventData.position;

        //[TODO] CommanderSkill UI 제작 후 변경 필요.
        GameObject hitGameObject = eventData.pointerCurrentRaycast.gameObject;
        _selectedImage = hitGameObject.GetComponent<Image>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            if (Vector2.Distance(eventData.position, _dragStartPosition) < switchThreshold)
            {
                // 드래그가 시작된 지점과 현재 위치 간의 거리 계산
                float distance = Vector2.Distance(eventData.position, _dragStartPosition);

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
                if (_instantiatedObj == null)
                {
                    _instantiatedObj = Instantiate(tempSwitchObj, Vector3.zero, Quaternion.identity);
                    _instantiatedObj.gameObject.SetActive(true);
                }

                _instantiatedObj.transform.position = worldPos;

                RaycastHit hit;
                if (Physics.Raycast(_mainCamera.ScreenPointToRay(eventData.position), out hit))
                {
                    if (hit.collider != null)
                    {
                        _hitTileView = hit.transform.GetComponent<InGameTileView>();
                        Debug.LogColor("충돌한 오브젝트: " + _hitTileView.ID);
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        // [TODO] _hitTileView 해당 위치에 액션
    }

    Vector3 HandleRuntimeDrag(PointerEventData eventData)
    {
        return _mainCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _mainCamera.nearClipPlane));
    }
}

