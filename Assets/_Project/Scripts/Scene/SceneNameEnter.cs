using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class SceneNameEnter : MonoBehaviour
{
    [Header("Step1")] [SerializeField] private TMP_InputField inputFieldInputUserName;
    
    private bool isFlag = false;
    public string[] lines;
    string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    private void Start()
    {
        // AppEventManager.Instance.SendInitial_Launch_Funnel(50100);
        
        if (File.Exists(GetPath() + "BadWord.txt"))
        {
            StreamReader word = new StreamReader(GetPath() + "BadWord.txt");
            string source = word.ReadToEnd();
            word.Close();

            lines = Regex.Split(source, LINE_SPLIT_RE);
        }
    }

    public void OnPressInputUserNameOK()
    {
        if(isFlag)
            return;
        
        if (!string.IsNullOrEmpty(inputFieldInputUserName.text))
        {

            if (inputFieldInputUserName.text.Length > 8)
            {
                // ToastManager.Open("MSG_NAME_ERROR", negativeMessage:true);
                return;
            }
            
            
            // string Check = Regex.Replace(inputFieldInputUserName.text, @"[^a-zA-Z0-9]", "", RegexOptions.Singleline);
            // Check = Regex.Replace(inputFieldInputUserName.text, @"[^\w\.@-]", "", RegexOptions.Singleline);
            Regex rx = new Regex(@"[^a-zA-Z0-9一-龥]");

            if (rx.IsMatch(inputFieldInputUserName.text))
            {
                // ToastManager.Open("MSG_NAME_ERROR", negativeMessage:true);
                return;
            }

            if (lines != null)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (inputFieldInputUserName.text.Contains(lines[i]))
                    {
                        // ToastManager.Open("MSG_NAME_ERROR", negativeMessage:true);
                        return;
                    }
                }    
            }
            
            
            SoundManager.Instance.PlayButtonClick();
            // ToDo:이름 저장 필요 서버 통신
            isFlag = true;

            // NetworkManager.Instance.ChangeNickName(inputFieldInputUserName.text, result =>
            // {
            //     isFlag = false;
            //     if (result == 200)
            //     {
            //         dataManager.UserData.NickName = inputFieldInputUserName.text;
            //         DataManager.Instance.SaveData();
            //         Observable.Timer(System.TimeSpan.FromSeconds(0.1f))
            //             .Subscribe(_ =>
            //             {
            //                 DataManager.TestDialogueScriptName = "0-2-2";
            //                 GameSceneManager.MoveScene(Scene.Dialogue);
            //             });

            //     }
            //     else
            //     {
            //         ToastManager.Open("MSG_NICKNAME_ALREADY_EXIST", negativeMessage:true);
            //     }
            // }, () =>
            // {
            //     isFlag = false;
            // });




        }
    }
    
    public string GetPath()
    {
        string path = null;
        
#if UNITY_EDITOR
        path = Application.dataPath;
        path = path.Substring(0, path.LastIndexOf('/'));
        return Path.Combine(path, "Assets", "Resources/");
#elif UNITY_ANDROID
        path = Application.persistentDataPath;
        path = path.Substring(0, path.LastIndexOf('/'));
        return Path.Combine(Application.persistentDataPath, "Resources/");
#elif UNITY_IPHONE
        path = Application.persistentDataPath;
        path = path.Substring(0, path.LastIndexOf('/'));
        return Path.Combine(path, "Assets", "Resources/");
#else
        path = Application.dataPath;
        path = path.Substring(0, path.LastIndexOf('/'));
        return Path.Combine(path, "Assets", "Resources/");
#endif
        
    }
}
