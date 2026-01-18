using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneIntro : MonoBehaviour
{
    enum IntroStep
    {
        None = -1,
        Step1_1 = 0,
        Step1_2,
        Step1_3,
        Step1_4,
        Step2_1
    }

    [SerializeField] private CanvasGroup FirstTextGroup;// 초반 딜레이를 줘서 폰트교체눈치 못채게..
    [SerializeField] private GameObject[] ObjSteps;
    [Header("Step1")] [SerializeField] private TMP_InputField inputFieldInputUserName;
    
    private IntroStep step = IntroStep.None;
    private bool isFlag = false;// 버튼 이중 처리 막기 위한 것
    private int step2_1 = 0;
    [SerializeField] private TextMeshProUGUI[] ObjStep1_3Text;
    
    [SerializeField] private TextMeshProUGUI[] ObjStep2_1Text;

    [SerializeField] private LineRenderer[] LineRendererStars;
    [SerializeField] private CanvasGroup[] ObjStars;
    [SerializeField] private float lineAnimationDuration = 3f;

    [SerializeField] private RectTransform StarGroupRect;
    [SerializeField] private GameObject EndObject;

    [SerializeField] private CanvasGroup GroupStep1_2Button;

    [SerializeField] private TextMeshProUGUI LastText;
    [SerializeField] private RectTransform ArrowImage;

    [SerializeField] private List<Vector2> StartVector2;
    [SerializeField] private List<Vector2> EndVector2;
    // Start is called before the first frame update
    void Start()
    {
        FirstTextGroup.alpha = 0;
        foreach(var obj in ObjSteps) // 모든 스텝 초기화
            obj.SetActive(false);

        foreach (var obj in ObjStep1_3Text)
            obj.alpha = 0;
        foreach (var obj in ObjStep2_1Text)
            obj.alpha = 0;
        
        foreach (var obj in LineRendererStars)
            obj.gameObject.SetActive(false);
        foreach (var obj in ObjStars)
            obj.alpha = 0;
        EndObject.SetActive(false);

        GroupStep1_2Button.alpha = 0;
        
        isFlag = false;
        SetStep(IntroStep.Step1_1);
        // AppEventManager.Instance.SendInitial_Launch_Funnel(10210);
        // SoundManager.Instance.PlayBGM(BGMIndex.dialog_sad_001);
        ArrowImage.gameObject.SetActive(false);
    }

    void SetStep(IntroStep newStep, float fadeSpeed = 1.5f)
    {
        if (ObjSteps == null)
            return;
            
        Sequence tweenSequence = DOTween.Sequence();
        
        if(step != IntroStep.None)
        {
            var oldStepCanvasGroup = ObjSteps[(int)step].GetComponent<CanvasGroup>();
            if (oldStepCanvasGroup)
            {
                tweenSequence.Append(oldStepCanvasGroup.DOFade(0, fadeSpeed));
            }
        }

        if (step == IntroStep.Step1_4)
        {
            StopCoroutine(CouroutineStep1_3());
            isFlag = true;
        }
        else if (step == IntroStep.Step2_1)
        {
            // SoundManager.Instance.PlayBGM(BGMIndex.dialog_mad_001);
            isFlag = true;
        }

        tweenSequence.AppendCallback(() => ObjSteps[(int)step].SetActive(false));
        
        // step = newStep;
        
        tweenSequence.AppendCallback(() => ObjSteps[(int)newStep].SetActive(true));
        
        var cg =ObjSteps[(int)newStep].GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 0;
            tweenSequence.Append(cg.DOFade(2, fadeSpeed));
        }

        tweenSequence.Play().OnComplete(() =>
        {
            step = newStep;
            PlayDetail(newStep);
        });
    }
    

    private void PlayDetail(IntroStep _step)
    {
        if (_step == IntroStep.Step1_1)
        {
            // Observable.Timer(System.TimeSpan.FromSeconds(1))
            //     .Subscribe(_ =>
            //     {
            //         FirstTextGroup.DOFade(1, 0.5f);
            //     });
            // Observable.Timer(System.TimeSpan.FromSeconds(3.5f))
            //     .Subscribe(_ =>
            //     {
            //         // SetStep(IntroStep.Step1_2);
            //         //todo pre test 용
            //         FirstTextGroup.DOFade(0, 2f);
            //         SoundManager.Instance.StopBGM(2f);
            //     });
            // Observable.Timer(System.TimeSpan.FromSeconds(6.5f))
            //     .Subscribe(_ =>
            //     {
            //         // AppEventManager.Instance.SendInitial_Launch_Funnel(20000);
            //         // DataManager.TestDialogueScriptName = "0-0(Opening)";
            //         SoundManager.Instance.PlayBGM(BGMIndex.ingame_battle_003, true);
            //         popupManager.Create<PopupSkipMessage>().Open();
            //         // GameSceneManager.MoveScene(Scene.Dialogue);
            //     });
        }
        else if (_step == IntroStep.Step1_2)
        {
            //뱔 깜빡임 코드에서는 안건들여도 될듯
            GroupStep1_2Button.DOFade(1, 1.5f);

        }
        else if (_step == IntroStep.Step1_3)
        {
            isFlag = false;// 스텝 1_2 에서 버튼 막아준것 다시 풀어줌
            //대사 한줄한줄 연출로 나오게 
            StartCoroutine(CouroutineStep1_3());

        }
        else if (_step == IntroStep.Step1_4)
        {
            isFlag = false;
        }
        else if (_step == IntroStep.Step2_1)
        {
            
            foreach (var obj in ObjStep2_1Text)
            {
                obj.gameObject.SetActive(false);
                obj.alpha = 0;
            }
            StartCoroutine(CoroutineLineRenderer(0));
        }
    }

    private IEnumerator CouroutineStep1_3()
    {
        for (int i = 0; i < ObjStep1_3Text.Length; i++)
        {
            ObjStep1_3Text[i].DOFade(1, 0.75f);
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(0.75f);
        if(isFlag == false)
            SetStep(IntroStep.Step1_4);
    }

    public void OnClickStep1_3()
    {
        if(isFlag == true)
            return;
        isFlag = false;
        for (int i = 0; i < ObjStep1_3Text.Length; i++)
        {
            ObjStep1_3Text[i].DOFade(1, 0.2f);
        }
        // Observable.Timer(System.TimeSpan.FromSeconds(0.7f))
        //     .Subscribe(_ =>
        //     {
        //         StopAllCoroutines();
        //         isFlag = true;
        //         SetStep(IntroStep.Step1_4);
        //     });
    }
    
    

    public void OnPressInputUserNameOK()
    {
        if(isFlag)
            return;
        
        if (!string.IsNullOrEmpty(inputFieldInputUserName.text))
        {
            // ToDo:이름 저장 필요 서버 통신
            isFlag = true;
            // SingletonManager.dataManager.UserData.NickName = inputFieldInputUserName.text;
            
            // Observable.Timer(System.TimeSpan.FromSeconds(0.1f))
            //     .Subscribe(_ =>
            //     {
            //         SetStep(IntroStep.Step2_1, 1f);
            //     });
        }
    }

    public void OnPressStep1_2WaitButton()
    {
        if(isFlag)
            return;
        isFlag = true;
        
        GroupStep1_2Button.DOFade(0, 1.5f);
        // Observable.Timer(System.TimeSpan.FromSeconds(2))
        //     .Subscribe(_ =>
        //     {
        //         SetStep(IntroStep.Step1_3);
        //     });
        
        // Run.After(3f, () => GameSceneManager.MoveScene(Scene.LobbyScene));
    }

    public void OnPressStep2_1()
    {
        if(isFlag || step2_1 >=6)
            return;
        ArrowImage.gameObject.SetActive(false);
        foreach (var obj in ObjStep2_1Text)
        {
            obj.DOFade(0, 0.3f).SetEase(Ease.Linear).OnComplete(() =>
            {
                obj.gameObject.SetActive(false);
            });
        }
            
        
        StopAllCoroutines();
        isFlag = true;
        if (step2_1 == 1) // 페이즈마다 텍스트 설정
        {
            
            StartCoroutine(CoroutineLineRenderer(1));

        }
        else if (step2_1 == 2)
        {
            
            StartCoroutine(CoroutineLineRenderer(2));
        }
        else if (step2_1 == 3)
        {
            StartCoroutine(CoroutineLineRenderer(3));
            
        }
        else if (step2_1 == 4)
        {
            StartCoroutine(CoroutineLineRenderer(4));
            
        }
        else if (step2_1 == 5)
        {
            StartCoroutine(CoroutineLineRenderer(5));
            
        }
        else if (step2_1 == 6)
        {
            // ObjStep2_1Text[0].text = Localization.GetLocalizedString("Intro_Step_2_5");//"...구해주세요.";
            // StartCoroutine(CouroutineStep2_1(1));
            
        }
        // else if (step2_1 == 5)
        // {
        //    
        //     StartCoroutine(CoroutineLineRenderer(5));
        //     Observable.Timer(System.TimeSpan.FromSeconds(2))
        //         .Subscribe(_ =>
        //         {
        //             step2_1++;
        //             isFlag = false;
        //         });
        // }
        // else
        // {
        //     
        //     StartCoroutine(CoroutineLineRenderer(6));
        //     
        // }
    }

    private Tweener arrowTween;
    private Tweener arrowFadeTween;
    private IEnumerator CouroutineStep2_1(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ObjStep2_1Text[i].DOFade(1, 1);
            yield return new WaitForSeconds(1.5f);
        }
        yield return new WaitForSeconds(0.75f);
        step2_1++;
        isFlag = false;

        if (step2_1 < 6)
        {
            ArrowImage.gameObject.SetActive(true);
            if(arrowTween != null)
                arrowTween.Kill();
            if(arrowFadeTween != null)
                arrowFadeTween.Kill();
            ArrowImage.anchoredPosition = StartVector2[step2_1-1];
            ArrowImage.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            arrowFadeTween = ArrowImage.GetComponent<Image>().DOFade(0.5f, 1.2f).SetLoops(-1).SetEase(Ease.Linear);
            arrowTween = ArrowImage.DOLocalMove(EndVector2[step2_1-1], 1.2f).SetLoops(-1).SetEase(Ease.Linear);
        }
    }
    private IEnumerator CoroutineLineRenderer(int idx)
    {
        float startTime = Time.time;
        LineRendererStars[idx].gameObject.SetActive(true);
        Vector3 startPosition = LineRendererStars[idx].GetPosition(0);
        Vector3 endPosition = LineRendererStars[idx].GetPosition(1);
        Vector3 pos = startPosition;
        ObjStars[idx].DOFade(1, lineAnimationDuration).SetEase(Ease.Linear);
        while (pos != endPosition)
        {
            float t = (Time.time - startTime) / lineAnimationDuration;
            pos = Vector3.Lerp(startPosition, endPosition, t);
            LineRendererStars[idx].SetPosition(1, pos);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
        
        if (idx >= 5 && idx <= 7)
        {
            if (idx == 5)
            {
                // ObjStep2_1Text[0].gameObject.SetActive(true);
                // ObjStep2_1Text[0].text = Localization.GetLocalizedString("Intro_Step_2_4_5");//"그리고...";
                // StartCoroutine(CouroutineStep2_1(1));
                // StarGroupRect.DOScale(new Vector3(0.5f, 0.5f, 0.5f), 8f);
                // Observable.Timer(System.TimeSpan.FromSeconds(2.35f))
                //     .Subscribe(_ =>
                //     {
                //         foreach (var obj in ObjStep2_1Text)
                //         {
                //             obj.DOFade(0, 0.3f).SetEase(Ease.Linear).OnComplete(() =>
                //             {
                //                 obj.gameObject.SetActive(false);
                //             });
                //         }
                //         StartCoroutine(CoroutineLineRenderer(idx+1));
                //     });
                
            }
            else if (idx == 6)
            {
                // Observable.Timer(System.TimeSpan.FromSeconds(3.5f))
                //     .Subscribe(_ =>
                //     {
                //         LastText.alpha = 0f;
                //         EndObject.SetActive(true);
                //     });
                // Observable.Timer(System.TimeSpan.FromSeconds(4.5f))
                //     .Subscribe(_ =>
                //     {
                //         LastText.DOFade(1, 1f).SetEase(Ease.Linear);
                        
                //     });
                // Observable.Timer(System.TimeSpan.FromSeconds(7f))
                //     .Subscribe(_ =>
                //     {
                //         GameSceneManager.MoveScene(Scene.Dialogue);
                //     });
                StartCoroutine(CoroutineLineRenderer(idx+1));
            }
            else
            {
                StartCoroutine(CoroutineLineRenderer(idx+1));    
            }
            
            
        }
        else
        {
            // if (idx == 0)
            // {
            //     ObjStep2_1Text[0].gameObject.SetActive(true);
            //     ObjStep2_1Text[1].gameObject.SetActive(true);
            //     ObjStep2_1Text[0].text = Localization.GetLocalizedString("Intro_Step_2_1_1");//"...다행이에요.";
            //     ObjStep2_1Text[1].text = Localization.GetLocalizedString("Intro_Step_2_1_2");//"가장 중요한건 당신을 잃지 않는 것이죠.";
            //     StartCoroutine(CouroutineStep2_1(2));    
            // }
            // else if (idx == 1)
            // {
            //     ObjStep2_1Text[0].gameObject.SetActive(true);
            //     ObjStep2_1Text[1].gameObject.SetActive(true);
            //     ObjStep2_1Text[0].text = Localization.GetLocalizedString("Intro_Step_2_2_1");// "어쩌면 당신은 이곳에 이미 와보셨을지도 몰라요.";
            //     ObjStep2_1Text[1].text = Localization.GetLocalizedString("Intro_Step_2_2_2");//"다만 모두 잊혀진 것일 뿐.";
            //     StartCoroutine(CouroutineStep2_1(2));
            // }
            // else if (idx == 2)
            // {
            //     ObjStep2_1Text[0].gameObject.SetActive(true);
            //     ObjStep2_1Text[0].text = Localization.GetLocalizedString("Intro_Step_2_3_1");//"너무 불안해 하지 마세요. 답은 별들이 알려줄테니까요.";
            //     StartCoroutine(CouroutineStep2_1(1));
            // }
            // else if (idx == 3)
            // {
            //     ObjStep2_1Text[0].gameObject.SetActive(true);
            //     ObjStep2_1Text[1].gameObject.SetActive(true);
            //     ObjStep2_1Text[0].text = Localization.GetLocalizedString("Intro_Step_2_4_1");//"그리고...";
            //     ObjStep2_1Text[1].text = Localization.GetLocalizedString("Intro_Step_2_4_2");//"다시 한번 부탁 드릴께요.";
                
            //     StartCoroutine(CouroutineStep2_1(2));
            // }
            // else if (idx == 4)
            // {
            //     ObjStep2_1Text[0].gameObject.SetActive(true);
            //     ObjStep2_1Text[1].gameObject.SetActive(true);
                
            //     ObjStep2_1Text[0].text = Localization.GetLocalizedString("Intro_Step_2_4_3");//"그리고...";
            //     ObjStep2_1Text[1].text = Localization.GetLocalizedString("Intro_Step_2_4_4");//"다시 한번 부탁 드릴께요.";
                
            //     StartCoroutine(CouroutineStep2_1(2));
            // }
           
            
        }
        
    }
    
    // test
    public void OnPressSkip()
    {
        StopAllCoroutines();
        // DataManager.TestDialogueScriptName = "0-0(Opening)";
        // GameSceneManager.MoveScene(Scene.Dialogue);
    }
}
