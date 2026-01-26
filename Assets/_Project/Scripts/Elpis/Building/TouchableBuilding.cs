using System;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchableBuilding : MonoBehaviour
{
    [SerializeField] private ElpisFacilityType facilityType;

    private ElpisDataBridge _elpisDataBridge;
    private Vector3 _mouseDownPosition;
    private bool _isDragging;
    private const float DragThreshold = 10f;

    // 튜토리얼용 TutorialTarget
    private TutorialTarget _tutorialTarget;

    /// <summary>
    /// 건물 FacilityType 반환
    /// </summary>
    public ElpisFacilityType FacilityType => facilityType;

    /// <summary>
    /// 튜토리얼에서 건설 완료 터치 시 호출될 콜백
    /// </summary>
    public static Action<ElpisFacilityType> OnBuildCompleteClicked;

    private void Awake()
    {
        _elpisDataBridge = new ElpisDataBridge();

        // TutorialTarget 동적 생성 및 등록
        _tutorialTarget = gameObject.AddComponent<TutorialTarget>();
        _tutorialTarget.SetTargetId($"Building_{facilityType}");
    }

    private void OnMouseDown()
    {
        if (IsPointerOverUI())
            return;

        // 튜토리얼 터치 차단 체크
        if (TutorialTouchBlocker.ShouldBlockTouch(Input.mousePosition))
            return;

        _mouseDownPosition = Input.mousePosition;
        _isDragging = false;
    }

    private void OnMouseDrag()
    {
        if (_isDragging)
            return;

        var dragDistance = Vector3.Distance(_mouseDownPosition, Input.mousePosition);
        if (dragDistance > DragThreshold)
        {
            _isDragging = true;
        }
    }

    private void OnMouseUp()
    {
        if (IsPointerOverUI())
            return;

        // 튜토리얼 터치 차단 체크
        if (TutorialTouchBlocker.ShouldBlockTouch(Input.mousePosition))
            return;

        if (_isDragging)
            return;

        OnTouchBuilding().Forget();
    }

    private bool IsPointerOverUI()
    {
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return EventSystem.current.IsPointerOverGameObject();
    }

    private async UniTaskVoid OnTouchBuilding()
    {
        CameraFocus();

        var facilityData = _elpisDataBridge.GetFacilityByType(facilityType);

        if (facilityData == null)
            return;

        // 건설 완료 대기 상태 (IsJustCompleted)
        if (facilityData.IsJustCompleted)
        {
            await HandleBuildComplete(facilityData);
            return;
        }

        // 건설 중이거나 설치 가능한 상태
        if (facilityData.IsBuilding || facilityData.Level <= 0)
        {
            OpenBuildLayer(facilityData);
            return;
        }

        // 이미 설치 완료된 상태 - 팝업 오픈
        await ElpisBuildingPopup.OpenPopup(facilityData);
    }

    private async UniTask HandleBuildComplete(ElpisFacility facilityData)
    {
        // 튜토리얼 콜백 호출 (등록되어 있다면)
        OnBuildCompleteClicked?.Invoke(facilityType);

        // 업그레이드인지 신규 건설인지 확인
        if (facilityData.Level >= 1 && facilityData.IsUpgrading)
        {
            await NetManager.Instance.Elpis.FinishUpgradingFacilityAsync((int)facilityData.BuildId);
        }
        else
        {
            await NetManager.Instance.Elpis.FinishBuildingFacilityAsync((int)facilityData.BuildId);
        }
    }

    private void OpenBuildLayer(ElpisFacility facilityData)
    {
        var buildInfo = SpecDataManager.Instance.GetBuildInfo((int)facilityData.BuildId);
        if (buildInfo == null)
            return;

        var newParam = new ElpisBuildLayer.ElpisBuildCacheData
        {
            slotIndex = buildInfo.slot_index
        };

        SceneUILayerManager.Instance.PushUILayerAsync<ElpisBuildLayer>(newParam).Forget();
    }

    private void CameraFocus()
    {
        var cameraController = MainCameraHolder.CameraGestureController;
        var targetZoom = 10.0f;

        cameraController.ZoomAndMoveAsync(transform.position, targetZoom, 0.3f).Forget();
    }
}