using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 인게임 터치 입력 총괄 매니저 (partial class)
/// - Main        : 공유 필드, Update/HandleTouch 입력 감지, Raycast 유틸리티
/// - FieldControl: 보드 위 캐릭터 선택 · 드래그 이동 · 스왑 · 아이템 적용
/// - Camera      : 카메라 드래그 이동 · 핀치 줌
/// - Placement   : UI → 보드 캐릭터 배치 (고스트 프리뷰, 배치 확정/취소, 추천 행 VFX)
/// </summary>
public partial class InGameTouchManager : SingletonMonoBehaviour<InGameTouchManager>
{
    private bool _touchLocked = false;

    private CharacterController _selectedCharacterController = null;
    private InGameTileView _selectedTileView = null;
    private List<InGameTile> _attackRangeTileList = new List<InGameTile>();
    private InGameTileView _selectedFirstTileView = null;
    private Vector3 _offset;
    private bool _isMoveEndAnimation;

    private int _selectedFirstTileID = -1;
    public int SelectedFirstTileID { get => _selectedFirstTileID; set => _selectedFirstTileID = value; }

    private bool _isDragFromUI = false;  // UI에서 드래그해서 올린 캐릭터인지

    // 고스트 캐릭터 관련 (UI 드래그 프리뷰용)
    private CharacterController _ghostCharacterController = null;
    private CharacterStatData _ghostStatData = null;
    private Action<CharacterStatData> _onPlacementConfirmed = null;  // 배치 확정 시 콜백

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

                    // if (_selectedCharacterController == null)
                    //     CameraMove(isPointerOverUI);
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
        if (_isDragFromUI) return;  // UI 드래그 중일 때는 보드 터치 무시

        // 튜토리얼 중 3D 터치 차단 (Allow3DTouch가 false면 전체 차단)
        if (TutorialTouchBlocker.IsBlocking &&
            !TutorialTouchBlocker.Allow3DTouch)
            return;

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

    /// <summary>
    /// RaycastAll로 모든 hit를 받고 Slot 태그를 우선 반환, 없으면 Playground 반환
    /// </summary>
    private bool TryGetPrioritizedHit(Vector3 touchPosition, out RaycastHit resultHit, out string hitTag)
    {
        resultHit = default;
        hitTag = null;

        Ray ray = MainCameraHolder.MainCamera.ScreenPointToRay(touchPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        if (hits.Length == 0)
            return false;

        // Slot 태그 우선 검색
        foreach (var hit in hits)
        {
            if (hit.transform.CompareTag("Slot"))
            {
                resultHit = hit;
                hitTag = "Slot";
                return true;
            }
        }

        // Slot이 없으면 Playground 검색
        foreach (var hit in hits)
        {
            if (hit.transform.CompareTag("Playground"))
            {
                resultHit = hit;
                hitTag = "Playground";
                return true;
            }
        }

        // 그 외 첫 번째 hit 반환
        resultHit = hits[0];
        hitTag = hits[0].transform.tag;
        return true;
    }

    /// <summary>
    /// UI 드래그 취소 - 고스트 제거
    /// </summary>
    public void CancelDragFromUI()
    {
        CancelGhostDrag();
    }

    /// <summary>
    /// 스크린 좌표에서 타일뷰 가져오기 (Slot 우선)
    /// </summary>
    public InGameTileView GetTileViewFromScreenPosition(Vector3 screenPosition)
    {
        if (TryGetPrioritizedHit(screenPosition, out RaycastHit hit, out string hitTag))
        {
            if (hitTag == "Slot")
            {
                return hit.transform.GetComponent<InGameTileView>();
            }
        }
        return null;
    }

    /// <summary>
    /// 스크린 좌표가 보드 영역(Slot 또는 Playground) 위인지 확인
    /// </summary>
    /// <param name="screenPosition">스크린 좌표</param>
    /// <param name="tileView">Slot 위면 타일뷰 반환, Playground면 null</param>
    /// <returns>보드 영역 위면 true</returns>
    public bool IsPointOverBoard(Vector3 screenPosition, out InGameTileView tileView)
    {
        tileView = null;

        if (!TryGetPrioritizedHit(screenPosition, out RaycastHit hit, out string hitTag))
            return false;

        if (hitTag == "Slot")
        {
            tileView = hit.transform.GetComponent<InGameTileView>();
            return true;
        }
        else if (hitTag == "Playground")
        {
            // Playground 위 - tileView는 null이지만 보드 영역임
            return true;
        }

        return false;
    }
}
