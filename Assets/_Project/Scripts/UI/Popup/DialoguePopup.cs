using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Coffee.UIEffects;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class DialoguePopup : UILayerPopupBase
    {
        [SerializeField]
        private TMP_FontAsset temTMPFontAsset;

        [Header("Chracter Layer")]
        [SerializeField] private GameObject _characeterIllustParentLeftObject;
        [SerializeField] private GameObject _characeterIllustParentRightObject;
        [SerializeField] private TextMeshProUGUI _characterNameText;

        [Header("Dialogue Layer")]
        [SerializeField] private CAButton _blockLayerButton;
        [SerializeField] private GameObject _extraBGObj;
        [SerializeField] private Image _extraBGImage;
        [SerializeField] private SpriteLoader _extraBGSpriteLoader;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private RectTransform _dialogueTextRect;

        [SerializeField] private GameObject _maskObj;
        [SerializeField] private RectTransform _unmaskTransform;
        [SerializeField] private AnimationCurve holeRadiusCurve;
        [SerializeField] private AnimationCurve aspectRatioCurve;
        [SerializeField] float animationDuration = 2.0f;  // 애니메이션 시간

        [SerializeField] private UIEffect _uiEffect;
        [SerializeField] private Image _bgImage;

        private Vector2 _tweenVector = new Vector2(1550f, 192f);

        private DialogueLanguage _currentSpecDialogueData;
        private List<DialogueLanguage> _dialogueList = new List<DialogueLanguage>();

        private int currentDialogueSeq = 0;
        private int _dialogueGroupID = 0;
        private Action _onComplete;
        private bool _isAnimating = false;

        protected override void Awake()
        {
            base.Awake();

            _blockLayerButton.OnClickAsObservable().Subscribe(this, (_, self) =>
            {
                self.OnClickNextDialogue();
                self.OnClickRefreshTextTween();
            }).AddTo(this);
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
            OnClickNextDialogue();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            (_dialogueGroupID, _onComplete) = ((int, Action))param;
            _dialogueList = SpecDataManager.Instance.GetDialogueListByGroupID(_dialogueGroupID);

            _characterNameText.font = temTMPFontAsset;
            _dialogueText.font = temTMPFontAsset;

            ClearPopup();

            SetDialogueData(currentDialogueSeq);
        }

        private void SetDialogueData(int seq)
        {
            if (_dialogueList == null || _dialogueList.Count == 0) return;

            bool isChangePrefab = (seq == 0) || (_currentSpecDialogueData == null ||
                                              _currentSpecDialogueData.prefab_id != _dialogueList[seq].prefab_id);

            _currentSpecDialogueData = _dialogueList[seq];
            // SetCharacters(_currentSpecDialogueData);

            if (isChangePrefab)
            {
                BMUtil.RemoveChildObjects(_characeterIllustParentLeftObject.transform);
                BMUtil.RemoveChildObjects(_characeterIllustParentRightObject.transform);
            }

            // 추가 배경 설정
            _extraBGObj.SetActive(false);
            if (_currentSpecDialogueData.bg_image != "none")
            {
                _extraBGObj.SetActive(true);
                _extraBGSpriteLoader.SetSprite(_currentSpecDialogueData.bg_image).Forget();
            }

            if (_currentSpecDialogueData.dialogue_animation_type != DialogueAnimationType.NONE)
            {
                switch (_currentSpecDialogueData.dialogue_animation_type)
                {
                    case DialogueAnimationType.CURVE_ANIMATION:
                        _isAnimating = true;
                        StartCoroutine(ScaleWithAnimationCurve(animationDuration));
                        break;
                    case DialogueAnimationType.SCALE_DOWN:
                        _isAnimating = true;
                        StartCoroutine(ScaleDownHoleRadius(animationDuration));
                        break;
                    case DialogueAnimationType.SCALE_UP:
                        _isAnimating = true;
                        StartCoroutine(ScaleUpHoleRadius(animationDuration));
                        break;
                    default:
                        break;
                }
            }
            else
            {
                _maskObj.SetActive(false);
            }

            if (_currentSpecDialogueData.prefab_id > 0 && isChangePrefab)
            {
                string characterPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _currentSpecDialogueData.prefab_id);

                string[] chPosStrings = _currentSpecDialogueData.character_pos.Split(',');
                List<int> charPosX = new List<int>();
                List<int> charPosY = new List<int>();
                List<int> charPosDir = new List<int>();
                for (int i = 0; i < chPosStrings.Length; i++)
                {
                    string[] chfStrings = chPosStrings[i].Split('_');
                    charPosX.Add(int.Parse(chfStrings[0], CultureInfo.InvariantCulture));
                    charPosY.Add(int.Parse(chfStrings[1], CultureInfo.InvariantCulture));
                    charPosDir.Add(int.Parse(chfStrings[2], CultureInfo.InvariantCulture));
                }
                if (charPosDir[0] == 0)
                {
                    var obj = AddressablesUtil.Instantiate(characterPrefabName, _characeterIllustParentLeftObject.transform);
                    obj.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                    if (obj != null)
                    {
                        var characterIllust = obj.GetComponent<CharacterIllust>();
                        characterIllust.SetCharacterAnimation("idle");
                    }
                }
                else
                {
                    var obj = AddressablesUtil.Instantiate(characterPrefabName, _characeterIllustParentRightObject.transform);
                    obj.GetComponent<RectTransform>().localScale = new Vector3(-1, 1, 1);
                    if (obj != null)
                    {
                        var characterIllust = obj.GetComponent<CharacterIllust>();
                        characterIllust.SetCharacterAnimation("idle");
                    }
                }
            }

            _characterNameText.text = LanguageManager.Instance.GetDefaultText(_currentSpecDialogueData.character_name_token);
            _dialogueText.text = LanguageManager.Instance.GetDialogueText(_currentSpecDialogueData.text_desc_token);
        }

        // 다음 대화로 넘어가기
        private void OnClickNextDialogue()
        {
            // if (_isAnimating) return;
            currentDialogueSeq++;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_dialogue);

            // 다이얼로그 종료 처리
            if (currentDialogueSeq >= _dialogueList.Count)
            {
                OnDialogueCompleteAsync().Forget();
                return;
            }

            SetDialogueData(currentDialogueSeq);
        }

        private async UniTaskVoid OnDialogueCompleteAsync()
        {
            // TODO: 가이드 미션 완료 체크
            // ServerDataManager.Instance.GuideMission.AddActionValue(GuideMissionType.END_DIALOGUE);
            if (_currentSpecDialogueData == null) return;

            // 보상 지급 여부 체크
            bool isGetReward = _currentSpecDialogueData.reward_id > 0;
            if (isGetReward && !ClientProgressData.Get().IsRewardReceived(_currentSpecDialogueData.reward_id))
            {
                // 서버에 보상 수령 요청
                var resp = await NetManager.Instance.CustomLobby.ClaimOtherRewardAsync((uint)_currentSpecDialogueData.reward_id);
                if (resp != null && resp.IsSuccess && resp.Rewards != null && resp.Rewards.Count > 0)
                {
                    // 보상 수령 처리
                    ClientProgressData.Get().AddReceivedRewardId(_currentSpecDialogueData.reward_id);
                    
                    // 서버 응답의 Reward를 RewardItem으로 변환
                    List<RewardItem> rewardItemList = new List<RewardItem>();
                    for (int i = 0; i < resp.Rewards.Count; i++)
                    {
                        var reward = resp.Rewards[i];
                        rewardItemList.Add(new RewardItem(reward));
                    }

                    SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), callback =>
                    {
                        // 가이드 미션 가이드 효과 재생
                        ObjectRegistry.GetObject<GuideAlert>(RegistryKey.GuideAlert)?.UpdateAlert();
                        InGameMain.GetInGameMain()?.SetInGameBottomUIInGuide();

                        TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.DIALOGUE_POP_END, _dialogueGroupID.ToString());
                    }).Forget();
                }
            }
            else
            {
                TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.DIALOGUE_POP_END, _dialogueGroupID.ToString());
            }

            // 다이얼로그 히스토리 데이터 추가 및 저장
            ClientProgressData.Get().AddCompleteDialogueId(_dialogueGroupID);

            if (isGetReward == false)
            {
                // 가이드 미션 가이드 효과 재생
                ObjectRegistry.GetObject<GuideAlert>(RegistryKey.GuideAlert)?.UpdateAlert();
            }

            if (_currentSpecDialogueData.image_info_id != 0)
                SceneUILayerManager.Instance.PushUILayerAsync<ImageInfoPop>((int)_currentSpecDialogueData.image_info_id).Forget();

            _onComplete?.Invoke();

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            currentDialogueSeq = 0;

            BMUtil.RemoveChildObjects(_characeterIllustParentLeftObject.transform);
            BMUtil.RemoveChildObjects(_characeterIllustParentRightObject.transform);
        }

        public void OnClickRefreshTextTween()
        {
            // if (_isAnimating) return;
            _dialogueText.DOFade(0, 0f);
            _dialogueText.DOFade(0, 0.3f).SetEase(Ease.OutQuad).From();
            _dialogueTextRect.DOSizeDelta(_tweenVector, 0.3f).SetEase(Ease.OutQuad).From();
        }

        private IEnumerator ScaleWithAnimationCurve(float duration)
        {
            _maskObj.SetActive(true);
            Vector3 initialScale = new Vector3(0, 0, 0);
            Vector3 finalScale = new Vector3(3, 2.5f, 3);

            _unmaskTransform.localScale = initialScale;

            // 배경 이미지의 종횡비 계산
            var bgRt = _bgImage.GetComponent<RectTransform>();
            float aspectRatio = Mathf.Max(0.0001f, bgRt.rect.width / Mathf.Max(0.0001f, bgRt.rect.height));

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;

                // 각각의 커브로 개별 제어
                float holeRadiusValue = holeRadiusCurve.Evaluate(normalizedTime);
                float aspectRatioValue = aspectRatioCurve.Evaluate(normalizedTime);

                // 블러 효과 (기존 로직 유지)
                _uiEffect.blurFactor = 1 - holeRadiusValue;

                // 셰이더 파라미터 설정
                _bgImage.material.SetFloat("_HoleRadius", holeRadiusValue);
                _bgImage.material.SetFloat("_AspectRatio", aspectRatio * aspectRatioValue);

                yield return null;
            }

            // 최종 값 설정
            _unmaskTransform.localScale = finalScale;
            _bgImage.material.SetFloat("_HoleRadius", holeRadiusCurve.Evaluate(1f));
            _bgImage.material.SetFloat("_AspectRatio", aspectRatio * aspectRatioCurve.Evaluate(1f));
            // _maskObj.SetActive(false);
            _isAnimating = false;
        }

        private IEnumerator ScaleUpHoleRadius(float duration)
        {
            _maskObj.SetActive(true);
            if (duration <= 0f)
            {
                _bgImage.material.SetFloat("_HoleRadius", 0f);
                _isAnimating = false;
                yield break;
            }

            float start = 0;
            float end = 1f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float v = Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                _bgImage.material.SetFloat("_HoleRadius", v);
                yield return null;
            }
            _bgImage.material.SetFloat("_HoleRadius", end);
            _isAnimating = false;
        }

        private IEnumerator ScaleDownHoleRadius(float duration)
        {
            _maskObj.SetActive(true);
            if (duration <= 0f)
            {
                _bgImage.material.SetFloat("_HoleRadius", 1f);
                _isAnimating = false;
                yield break;
            }

            float start = 1;
            float end = 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float v = Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                _bgImage.material.SetFloat("_HoleRadius", v);
                yield return null;
            }
            _bgImage.material.SetFloat("_HoleRadius", end);
            _isAnimating = false;
        }

        private string GetAnimationType(string type)
        {
            string result = "normal";
            if (type == "1")
            {
                result = "happy";
            }
            else if (type == "2")
            {
                result = "angry";
            }
            else if (type == "3")
            {
                result = "panic";
            }
            else if (type == "4")
            {
                result = "thinking";
            }
            else if (type == "5")
            {
                result = "worry";
            }
            else if (type == "6")
            {
                result = "sad";
            }


            return result;
        }

        private void SetCharacters(DialogueLanguage dialogueData)
        {
            List<string> CharacterIds = new List<string>();
            List<string> CharacterFace = new List<string>();
            List<int> charPosX = new List<int>();
            List<int> charPosY = new List<int>();
            List<int> charPosDir = new List<int>();

            // for (int i = CharacterTransform.transform.childCount - 1; i > 0; i--)
            // {
            //     Destroy(CharacterTransform.transform.GetChild(i).gameObject);
            // }
            for (int i = _characeterIllustParentLeftObject.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(_characeterIllustParentLeftObject.transform.GetChild(i).gameObject);
            }

            // string[] chStrings = dialogueData.character_img_id_n_emotion.Split(',');
            // for (int i = 0; i < chStrings.Length; i++)
            // {
            //     string[] chfStrings = chStrings[i].Split('_');
            //     CharacterIds.Add(chfStrings[0]);
            //     CharacterFace.Add(chfStrings[1]);
            // }

            string[] chPosStrings = dialogueData.character_pos.Split(',');
            for (int i = 0; i < chPosStrings.Length; i++)
            {
                string[] chfStrings = chPosStrings[i].Split('_');
                charPosX.Add(int.Parse(chfStrings[0], CultureInfo.InvariantCulture));
                charPosY.Add(int.Parse(chfStrings[1], CultureInfo.InvariantCulture));
                charPosDir.Add(int.Parse(chfStrings[2], CultureInfo.InvariantCulture));
            }

            List<GameObject> chaObjs = new List<GameObject>();
            for (int i = 0; i < chPosStrings.Length; i++)
            {
                GameObject obj = AddressablesUtil.Instantiate($"{CharacterIds[i]}_Static", _characeterIllustParentLeftObject.transform);
                if (obj != null)
                {
                    obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(charPosX[i], charPosY[i]);
                    if (charPosDir[i] == 0)
                    {

                        obj.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                    }
                    else
                    {
                        obj.GetComponent<RectTransform>().localScale = new Vector3(-1, 1, 1);
                    }

                    Color color = Color.white;
                    obj.GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, GetAnimationType(CharacterFace[i]), true);
                    if (CharacterIds[i] != dialogueData.prefab_id.ToString())
                    {
                        ColorUtility.TryParseHtmlString("#A1A1A1", out color);
                    }

                    obj.GetComponent<SkeletonGraphic>().color = color;
                }
                chaObjs.Add(obj);
            }

            for (int i = 0; i < chaObjs.Count; i++)
            {
                if (CharacterIds[i] == dialogueData.prefab_id.ToString())
                {
                    chaObjs[i].transform.SetAsLastSibling();
                }
            }
        }
    }
}
