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
        // await RuntimeInitializer.InitializeAsync();

        // var localizationManager = Engine.GetService<ILocalizationManager>();

        // var lanCode = Language.TC.ToString();
        
        // if(DataManager.Instance.localSaveData != null)
        //     lanCode = DataManager.Instance.UserData.LanguageCodeValue;
        
        // var locale = "en"; 
        // if(lanCode == Language.TC.ToString())
        //     locale = "zh-TW";
        // else if (lanCode == Language.SC.ToString())
        //     locale = "zh-CN";
        // else if(lanCode == Language.KR.ToString())
        //     locale = "ko";
        // else if(lanCode == Language.JP.ToString())
        //     locale = "ja";

        // Debug.Log($"locale {locale}");
        // await localizationManager.SelectLocaleAsync(locale);
        // //await localizationManager.SelectLocaleAsync(localizationManager.Configuration.DefaultLocale);
        
        // var scriptPlayer = Engine.GetService<IScriptPlayer>();
        // // choiJE.230522 일단은 다이얼로그 테스트를 위하여 임시 처리 해둔다.
        // await scriptPlayer.PreloadAndPlayAsync(string.IsNullOrEmpty(DataManager.TestDialogueScriptName) ? testScriptName : DataManager.TestDialogueScriptName);

        // eventStartTime = Time.realtimeSinceStartup;
    }
}
