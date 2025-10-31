using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.TextCore.Text;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/DialogueShowPopup.prefab")]
    public class DialogueShowPopup : UILayer
    {
        [SerializeField]
        private TMP_FontAsset temTMPFontAsset;

        [Header("Chracter Layer")]
        [SerializeField] private GameObject _characeterIllustParentObject;
        [SerializeField] private TextMeshProUGUI _characterNameText;

        [Header("Dialogue Layer")]
        [SerializeField] private CAButton _blockLayerButton;
        [SerializeField] private GameObject _extraBGObj;
        [SerializeField] private Image _extraBGImage;
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

        private SpecDialogue _currentSpecDialogueData;
        private List<SpecDialogue> _dialogueList = new List<SpecDialogue>();

        private int currentDialogueSeq = 0;
        private int _dialogueGroupID = 0;
        private Action _onComplete;
        private bool _isAnimating = false;

        protected override void Awake()
        {
            base.Awake();

            _blockLayerButton.onClick.AddListener(OnClickNextDialogue);
            _blockLayerButton.onClick.AddListener(OnClickRefreshTextTween);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _blockLayerButton.onClick.RemoveListener(OnClickNextDialogue);
            _blockLayerButton.onClick.RemoveListener(OnClickRefreshTextTween);
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

            if (isChangePrefab)
                BMUtil.RemoveChildObjects(_characeterIllustParentObject.transform);

            // 추가 배경 설정
            _extraBGObj.SetActive(false);
            if (_currentSpecDialogueData.bg_image != "none")
            {
                var targetSprite = ImageManager.Instance.GetCutSceneSprite(_currentSpecDialogueData.bg_image);
                if (targetSprite != null)
                {
                    _extraBGObj.SetActive(true);
                    _extraBGImage.sprite = targetSprite;
                }
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
                var gameObject = AddressablesUtil.Instantiate(characterPrefabName, _characeterIllustParentObject.transform);
            }

            _characterNameText.text = LanguageManager.Instance.GetLanguageText(_currentSpecDialogueData.character_name_token);
            _dialogueText.text = LanguageManager.Instance.GetLanguageText(_currentSpecDialogueData.text_desc_token);
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
                // 가이드 미션 완료 체크
                GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.END_DIALOGUE, 0, 1);

                // 보상 지급 여부 체크
                bool isGetReward = _currentSpecDialogueData.reward_id > 0;
                if (isGetReward)
                {
                    var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList(_currentSpecDialogueData.reward_id);
                    var rewardItemList = SpecDataManager.Instance.GetRewardItemListByRewadInfoList(rewardInfoList);

                    SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), callback =>
                    {
                        // 가이드 미션 가이드 효과 재생
                        GuideMissionManager.Instance.UpdateGuideMissionAlert();
                    }).Forget();

                    UserDataManager.Instance.IncreaseRewardItemList(rewardItemList, true);

                    InGameMain.GetInGameMain()?.SetInGameBottomUIInGuide();
                }

                // 다이얼로그 히스토리 데이터 추가 및 저장
                UserDataManager.Instance.AddDialogHistory(_dialogueGroupID);

                if (isGetReward == false)
                {
                    // 가이드 미션 가이드 효과 재생
                    GuideMissionManager.Instance.UpdateGuideMissionAlert();
                }

                if (_currentSpecDialogueData.image_info_id != 0)
                    SceneUILayerManager.Instance.PushUILayerAsync<ImageInfoPop>((int)_currentSpecDialogueData.image_info_id).Forget();

                _onComplete?.Invoke();

                SceneUILayerManager.Instance.PopUILayer(this);
                return;
            }

            SetDialogueData(currentDialogueSeq);
        }

        private void ClearPopup()
        {
            currentDialogueSeq = 0;

            BMUtil.RemoveChildObjects(_characeterIllustParentObject.transform);
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
    }
}
