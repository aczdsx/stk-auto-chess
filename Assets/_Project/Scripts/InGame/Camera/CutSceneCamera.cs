using UnityEngine;
using System.Threading;
using System.Linq;
using System.Collections;
using System.Reflection;
using PrimeTween;
using Naninovel;

public class CutSceneCamera : MonoBehaviour
{
    [SerializeField] private Camera _cutSceneCamera;
    private Camera _mainCamera;

    protected virtual void Awake()
    {
        if (_cutSceneCamera == null)
        {
            Debug.LogError($"[CutSceneCamera] _cutSceneCamera is not assigned!");
            return;
        }
    }

    protected virtual void Start()
    {
        // UICamera가 먼저 스택에 추가된 후에 실행되도록 Start에서 처리
        StartCoroutine(ConfigureCameraStackDelayed());
    }

    private System.Collections.IEnumerator ConfigureCameraStackDelayed()
    {
        // UICamera가 스택에 추가될 때까지 대기
        yield return null;
        yield return null; // 한 프레임 더 대기
        
        // NaninovelMainCamera 태그를 가진 카메라 찾기
        var naninovelCamera = GameObject.FindGameObjectWithTag("NaninovelMainCamera")?.GetComponent<Camera>();
        if (naninovelCamera != null)
        {
            _mainCamera = naninovelCamera;
            
            // 메인 카메라와 동일한 설정 적용
            MatchCameraProperties(naninovelCamera, _cutSceneCamera);
            
            ConfigureCutSceneCameraForURP(naninovelCamera, _cutSceneCamera);
        }
        else
        {
            Debug.LogWarning($"[CutSceneCamera] NaninovelMainCamera not found!");
        }
    }

    private void MatchCameraProperties(Camera mainCamera, Camera cutSceneCamera)
    {
        // 메인 카메라와 동일한 출력 속성 설정
        cutSceneCamera.targetDisplay = mainCamera.targetDisplay;
        cutSceneCamera.targetTexture = mainCamera.targetTexture;
        cutSceneCamera.allowHDR = mainCamera.allowHDR;
        cutSceneCamera.allowMSAA = mainCamera.allowMSAA;
        
        // 프로젝션 설정도 동일하게
        cutSceneCamera.orthographic = mainCamera.orthographic;
        if (mainCamera.orthographic)
        {
            cutSceneCamera.orthographicSize = mainCamera.orthographicSize;
        }
        else
        {
            cutSceneCamera.fieldOfView = mainCamera.fieldOfView;
        }
        
        // Clipping Planes
        // cutSceneCamera.nearClipPlane = mainCamera.nearClipPlane;
        // cutSceneCamera.farClipPlane = mainCamera.farClipPlane;
        
        // Culling Mask - 메인 카메라와 동일하게 설정 (컷씬 오브젝트들이 보이도록)
        cutSceneCamera.cullingMask = mainCamera.cullingMask;
        
        // 카메라 활성화 확인
        if (!cutSceneCamera.enabled)
        {
            cutSceneCamera.enabled = true;
            Debug.Log($"[CutSceneCamera] Enabled CutSceneCamera");
        }
        
        Debug.Log($"[CutSceneCamera] Matched camera properties - targetDisplay: {cutSceneCamera.targetDisplay}, orthographic: {cutSceneCamera.orthographic}, cullingMask: {cutSceneCamera.cullingMask}, enabled: {cutSceneCamera.enabled}");
    }

    private void ConfigureCutSceneCameraForURP(Camera mainCamera, Camera cutSceneCamera)
    {
        // URP 런타임 체크
        if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null) return;
        
        // URP 타입인지 확인
        var pipelineType = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.GetType();
        if (!pipelineType.Name.Contains("UniversalRenderPipeline"))
        {
            Debug.LogWarning($"[CutSceneCamera] Not using URP. Current pipeline: {pipelineType.Name}");
            return;
        }
        
        Debug.Log($"[CutSceneCamera] ConfigureCutSceneCameraForURP");
        
        try
        {
            // 리플렉션으로 URP 타입 동적 로드
            var universalNamespace = "UnityEngine.Rendering.Universal";
            var cameraExtensionsType = System.Type.GetType($"{universalNamespace}.CameraExtensions, Unity.RenderPipelines.Universal.Runtime");
            
            if (cameraExtensionsType == null)
            {
                Debug.LogError($"[CutSceneCamera] URP CameraExtensions type not found!");
                return;
            }
            
            // GetUniversalAdditionalCameraData 메서드
            var getCameraDataMethod = cameraExtensionsType.GetMethod("GetUniversalAdditionalCameraData", 
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Camera) }, null);
            
            if (getCameraDataMethod == null)
            {
                Debug.LogError($"[CutSceneCamera] GetUniversalAdditionalCameraData method not found!");
                return;
            }
            
            // CutScene 카메라 데이터
            var cutSceneData = getCameraDataMethod.Invoke(null, new object[] { cutSceneCamera });
            var cutSceneDataType = cutSceneData.GetType();
            
            // RenderType enum
            var renderTypeEnumType = System.Type.GetType($"{universalNamespace}.CameraRenderType, Unity.RenderPipelines.Universal.Runtime");
            if (renderTypeEnumType == null)
            {
                Debug.LogError($"[CutSceneCamera] CameraRenderType enum not found!");
                return;
            }
            
            var overlayValue = System.Enum.Parse(renderTypeEnumType, "Overlay");
            var renderTypeProperty = cutSceneDataType.GetProperty("renderType");
            
            if (renderTypeProperty != null)
            {
                var currentRenderType = renderTypeProperty.GetValue(cutSceneData);
                Debug.Log($"[CutSceneCamera] cutSceneData renderType: {currentRenderType}");
                
                if (!currentRenderType.Equals(overlayValue))
                {
                    renderTypeProperty.SetValue(cutSceneData, overlayValue);
                }
                Debug.Log($"[CutSceneCamera] cutSceneData renderType after: {renderTypeProperty.GetValue(cutSceneData)}");
            }
            
            // 메인 카메라 데이터
            var mainData = getCameraDataMethod.Invoke(null, new object[] { mainCamera });
            var mainDataType = mainData.GetType();
            var cameraStackProperty = mainDataType.GetProperty("cameraStack");
            
            if (cameraStackProperty != null)
            {
                var cameraStack = cameraStackProperty.GetValue(mainData);
                var cameraStackType = cameraStack.GetType();
                var containsMethod = cameraStackType.GetMethod("Contains", new[] { typeof(Camera) });
                var contains = (bool)containsMethod.Invoke(cameraStack, new object[] { cutSceneCamera });
                
                // UICamera를 찾아서 그 앞에 CutSceneCamera 삽입
                var indexOfMethod = cameraStackType.GetMethod("IndexOf", new[] { typeof(Camera) });
                var removeMethod = cameraStackType.GetMethod("Remove", new[] { typeof(Camera) });
                var insertMethod = cameraStackType.GetMethod("Insert", new[] { typeof(int), typeof(Camera) });
                var addMethod = cameraStackType.GetMethod("Add", new[] { typeof(Camera) });
                var countProperty = cameraStackType.GetProperty("Count");
                var count = (int)countProperty.GetValue(cameraStack);
                
                Debug.Log($"[CutSceneCamera] Current stack count: {count}");
                
                // 이미 스택에 있으면 제거
                if (contains)
                {
                    Debug.Log($"[CutSceneCamera] CutSceneCamera already in stack, removing first");
                    removeMethod.Invoke(cameraStack, new object[] { cutSceneCamera });
                    count = (int)countProperty.GetValue(cameraStack);
                }
                
                // UICamera 찾기
                int targetIndex = count; // 기본값: 맨 뒤
                var uiCamera = Engine.GetService<ICameraManager>()?.UICamera;
                
                if (uiCamera != null)
                {
                    var uiCameraIndex = (int)indexOfMethod.Invoke(cameraStack, new object[] { uiCamera });
                    Debug.Log($"[CutSceneCamera] UICamera found at index: {uiCameraIndex}");
                    
                    if (uiCameraIndex >= 0)
                    {
                        // UICamera 앞에 삽입 (UI가 가장 위에 렌더링되도록)
                        targetIndex = uiCameraIndex;
                        Debug.Log($"[CutSceneCamera] Will insert CutSceneCamera at index {targetIndex} (before UICamera)");
                    }
                    else
                    {
                        Debug.Log($"[CutSceneCamera] UICamera not in stack yet, will add at end");
                    }
                }
                else
                {
                    Debug.Log($"[CutSceneCamera] UICamera service not available, will add at end");
                }
                
                // 삽입 또는 추가
                try
                {
                    if (targetIndex < count)
                    {
                        Debug.Log($"[CutSceneCamera] Inserting at index {targetIndex}");
                        insertMethod.Invoke(cameraStack, new object[] { targetIndex, cutSceneCamera });
                    }
                    else
                    {
                        Debug.Log($"[CutSceneCamera] Adding at end (index {count})");
                        addMethod.Invoke(cameraStack, new object[] { cutSceneCamera });
                    }
                    
                    // 최종 확인
                    var finalCount = (int)countProperty.GetValue(cameraStack);
                    var finalContains = (bool)containsMethod.Invoke(cameraStack, new object[] { cutSceneCamera });
                    var finalCutSceneIndex = (int)indexOfMethod.Invoke(cameraStack, new object[] { cutSceneCamera });
                    
                    Debug.Log($"[CutSceneCamera] Final - Stack count: {finalCount}, Contains: {finalContains}, CutSceneCamera index: {finalCutSceneIndex}");
                    
                    if (uiCamera != null)
                    {
                        var finalUiIndex = (int)indexOfMethod.Invoke(cameraStack, new object[] { uiCamera });
                        Debug.Log($"[CutSceneCamera] Final UICamera index: {finalUiIndex}");
                        Debug.Log($"[CutSceneCamera] Render order: Base -> CutScene({finalCutSceneIndex}) -> UI({finalUiIndex})");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CutSceneCamera] Exception while adding to stack: {e.Message}\n{e.StackTrace}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CutSceneCamera] Failed to configure URP camera stack: {e.Message}\n{e.StackTrace}");
        }
    }
}   
