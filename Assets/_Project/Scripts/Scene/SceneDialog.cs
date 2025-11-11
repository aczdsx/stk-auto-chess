using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Naninovel;
using Unity.VisualScripting;

public class SceneDialog : MonoBehaviour
{
    [SerializeField] private string testScriptName;
    // Start is called before the first frame update

    public static float eventStartTime = 0f;
    public static bool isSkip = false;
    public static bool isAuto = false;
    
    private async void Start()
    {
        try
        {
            await RuntimeInitializer.Initialize();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Naninovel 초기화 실패: {ex.Message}\n{ex.StackTrace}");
            // Camera 설정 문제일 수 있음 - CameraConfiguration 확인 필요
            Debug.LogError("CameraConfiguration의 CustomCameraPrefab과 CustomUICameraPrefab이 올바르게 설정되어 있는지 확인하세요.");
            return;
        }

        if (!Engine.Initialized)
        {
            Debug.LogError("Naninovel 엔진이 초기화되지 않았습니다!");
            return;
        }

        var localizationManager = Engine.GetService<ILocalizationManager>();

        // var lanCode = Language.TC.ToString();
        
        // if(DataManager.Instance.localSaveData != null)
        //     lanCode = DataManager.Instance.UserData.LanguageCodeValue;
        
        var locale = "en"; 
        // if(lanCode == Language.TC.ToString())
        //     locale = "zh-TW";
        // else if (lanCode == Language.SC.ToString())
        //     locale = "zh-CN";
        // else if(lanCode == Language.KR.ToString())
        //     locale = "ko";
        // else if(lanCode == Language.JP.ToString())
        //     locale = "ja";

        Debug.Log($"locale {locale}");
        await localizationManager.SelectLocale(locale);
        
        // Naninovel 1.21: MainTrack.LoadAndPlay 사용
        // 1.18: await scriptPlayer.PreloadAndPlayAsync(testScriptName);
        // 1.21: scriptPlayer.MainTrack.LoadAndPlay(scriptPath) 사용
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        
        if (string.IsNullOrEmpty(testScriptName))
        {
            Debug.LogError("testScriptName이 비어있습니다!");
            return;
        }

        // 스크립트 경로 형식 확인 (Naninovel은 "Scripts/" 경로 사용, 확장자 제거)
        var scriptPath = testScriptName;
        if (!scriptPath.StartsWith("Scripts/"))
        {
            scriptPath = $"Scripts/{scriptPath}";
        }
        if (scriptPath.EndsWith(".nani"))
        {
            scriptPath = scriptPath.Substring(0, scriptPath.Length - 5);
        }
        
        Debug.Log($"Playing script: {scriptPath}");

        try
        {
            // Naninovel 1.20: MainTrack.LoadAndPlay 사용 (스크립트 로드 + 재생)
            // await scriptPlayer.MainTrack.LoadAndPlay(scriptPath);
            Debug.Log($"Script playing: {scriptPlayer.Playing}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"스크립트 재생 실패: {ex.Message}\n스크립트 경로: {scriptPath}\n원본 이름: {testScriptName}");
        }

        eventStartTime = Time.realtimeSinceStartup;
    }
}
