using Naninovel;
using UnityEngine;

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
            return;
        }

        if (!Engine.Initialized)
        {
            Debug.LogError("Naninovel 엔진이 초기화되지 않았습니다!");
            return;
        }

        // Title UI 숨기기
        var uiManager = Engine.GetService<IUIManager>();
        var titleUI = uiManager?.GetUI<Naninovel.UI.ITitleUI>();
        if (titleUI != null)
        {
            titleUI.Hide();
            Debug.Log("SceneDialog: TitleMenu UI 숨김");
        }

        var localizationManager = Engine.GetService<ILocalizationManager>();

        // var lanCode = Language.TC.ToString();
        
        // if(DataManager.Instance.localSaveData != null)
        //     lanCode = DataManager.Instance.UserData.LanguageCodeValue;
        
        var locale = "ko"; 
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

        // 스크립트 경로 형식: PathPrefix("Scripts") 제외, 확장자 제거
        // 예: "0-1" 또는 "Scripts/0-1" 또는 "0-1.nani" → "0-1"
        var scriptPath = testScriptName;
        
        // "Scripts/" 접두사 제거
        // if (scriptPath.StartsWith("Scripts/"))
        // {
        //     scriptPath = scriptPath.Substring(7); // "Scripts/".Length = 7
        // }
        
        // // 확장자 제거
        // if (scriptPath.EndsWith(".nani"))
        // {
        //     scriptPath = scriptPath.Substring(0, scriptPath.Length - 5);
        // }
        
        Debug.Log($"Playing script: {scriptPath}");

        try
        {
            await scriptPlayer.LoadAndPlay(scriptPath);
            Debug.Log($"Script playing: {scriptPlayer.Playing}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"스크립트 재생 실패: {ex.Message}\n스크립트 경로: {scriptPath}\n원본 이름: {testScriptName}");
            Debug.LogError(ex.ToString());
        }

        eventStartTime = Time.realtimeSinceStartup;
    }
}
