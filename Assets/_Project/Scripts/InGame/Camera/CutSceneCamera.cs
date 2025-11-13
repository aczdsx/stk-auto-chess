using UnityEngine;
using System.Threading;
using System.Linq;
using PrimeTween;
using Naninovel;

public class CutSceneCamera : MonoBehaviour
{
    [SerializeField] private Camera _cutSceneCamera;
    private Camera _mainCamera;

    protected virtual void Awake()
    {
        _cutSceneCamera.targetDisplay = 1;

        // NaninovelMainCamera 태그를 가진 카메라 찾기
        var naninovelCamera = GameObject.FindGameObjectWithTag("NaninovelMainCamera")?.GetComponent<Camera>();
        if (naninovelCamera != null)
        {
            ConfigureCutSceneCameraForURP(naninovelCamera, _cutSceneCamera);
        }
    }

    private void ConfigureCutSceneCameraForURP(Camera mainCamera, Camera cutSceneCamera)
    {
        #if URP_AVAILABLE
        if (!UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline) return;
        
        // CutScene 카메라를 Overlay로 설정
        var cutSceneData = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cutSceneCamera);
        cutSceneData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
        
        // 메인 카메라의 스택에 추가
        var mainData = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(mainCamera);
        mainData.cameraStack.Add(cutSceneCamera);
        #endif
    }
}   
