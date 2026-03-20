using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가챠 획득 캐릭터 단일 풀스크린 팝업 파라미터
    /// </summary>
    public class GachaGetCharacterPopupParam
    {
        /// <summary>획득한 캐릭터 ID</summary>
        public int CharacterId;

        /// <summary>
        /// true면 시퀀스 모드: Skip 버튼이 Pop 대신 IsSkipPressed 플래그만 세팅.
        /// 외부에서 WaitUntil(IsSkipPressed) 후 UpdateCharacter 호출.
        /// </summary>
        public bool IsSequenceMode;
    }

    /// <summary>
    /// 가챠 획득 캐릭터를 한 명씩 풀스크린으로 연출하는 팝업.
    /// LD 일러스트 + SD 캐릭터를 크게 보여주며 이름/등급/별/시너지 정보를 표시한다.
    /// </summary>
    public class GachaGetCharacterPopup : UILayerPopupBase
    {
        #region Serialized Fields

        [Header("Character Illust")]
        [SerializeField] private Transform _ldPosTransform;

        [Header("SD Character")]
        [SerializeField] private Transform _sdPosTransform;
        [SerializeField] private SpriteLoader _sdStandSpriteLoader;

        [Header("Grade")]
        [SerializeField] private SpriteLoader _gradeSpriteLoader;
        [SerializeField] private List<CharacterGradeStar> _starList;

        [Header("Name")]
        [SerializeField] private TextMeshProUGUI _characterNameText;

        [Header("Synergy")]
        [SerializeField] private SynergyUI _synergy1;
        [SerializeField] private TextMeshProUGUI _synergy1NameText;
        [SerializeField] private SynergyUI _synergy2;
        [SerializeField] private TextMeshProUGUI _synergy2NameText;

        [Header("Effect")]
        [SerializeField] private GameObject _effectGroup;

        [Header("Animator")]
        [SerializeField] private Animator _animator;

        [Header("Button")]
        [SerializeField] private CAButton _skipButton;

        [Header("Background Touch")]
        [SerializeField] private CAButton _bgTouchButton;

        [Header("RevealCharacterImages Duration")]
        [SerializeField] private float _revealCharacterImagesDuration = 0.3f;

        #endregion

        #region Private Fields

        private AsyncOperationHandle<GameObject> _ldHandle;
        private AsyncOperationHandle<GameObject> _sdHandle;
        private Graphic[] _ldGraphics;
        private Graphic[] _sdGraphics;
        private bool _isSequenceMode;
        private float _characterShowElapsed;

        #endregion

        #region Public Properties

        /// <summary>시퀀스 모드에서 유저가 배경 터치로 다음 캐릭터를 요청했는지 여부 (3초 후 활성화)</summary>
        public bool IsSkipPressed { get; private set; }

        /// <summary>시퀀스 모드에서 유저가 SKIP 버튼으로 전체 스킵을 요청했는지 여부</summary>
        public bool IsSkipAllPressed { get; private set; }

        #endregion

        #region Grade SD Stand Sprite Mapping

        private static readonly Dictionary<GradeType, string> SDStandSpriteMap = new()
        {
            [GradeType.RARE] = "UI_Gacha_Common_SD_Stand_R",
            [GradeType.EPIC] = "UI_Gacha_Common_SD_Stand_SR",
            [GradeType.LEGENDARY] = "UI_Gacha_Common_SD_Stand_SSR",
        };

        private static readonly Dictionary<GradeType, string> GradeAnimTriggerMap = new()
        {
            [GradeType.RARE] = "R",
            [GradeType.EPIC] = "SR",
            [GradeType.LEGENDARY] = "SSR",
        };

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // 배경 터치 버튼: Inspector 미할당 시 이름으로 탐색
            if (_bgTouchButton == null)
            {
                var bgTouch = transform.Find("BgTouchButton");
                if (bgTouch != null)
                    _bgTouchButton = bgTouch.GetComponent<CAButton>();
            }

            // 배경 터치 버튼을 Content 뒤(낮은 sibling)로 배치하여 다른 버튼 클릭을 방해하지 않도록 함
            if (_bgTouchButton != null)
            {
                _bgTouchButton.transform.SetAsFirstSibling();

                // alpha=0이면 CanvasRenderer가 메시를 컬링하여 depth=-1 → 레이캐스트 제외됨
                // 극소 alpha로 설정하여 레이캐스트 수신 보장
                var bgImg = _bgTouchButton.GetComponent<Image>();
                if (bgImg != null)
                    bgImg.color = new Color(0f, 0f, 0f, 0.004f);

                DisableContentRaycastTargets();
            }

            // SKIP 버튼: 시퀀스 모드에서 전체 스킵
            _skipButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) =>
                {
                    if (self._isSequenceMode)
                        self.IsSkipAllPressed = true;
                    else
                        SceneUILayerManager.Instance.PopUILayer(self);
                })
                .AddTo(this);

            // 배경 터치: 3초 경과 후 다음 캐릭터로 넘기기
            if (_bgTouchButton != null)
            {
                _bgTouchButton
                    .OnClickAsObservable()
                    .Subscribe(this, (_, self) =>
                    {
                        if (self._isSequenceMode && self._characterShowElapsed >= 2f)
                            self.IsSkipPressed = true;
                    })
                    .AddTo(this);
            }
        }

        private void Update()
        {
            if (_isSequenceMode)
                _characterShowElapsed += Time.deltaTime;
        }

        #endregion

        #region Lifecycle Overrides

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            var p = param as GachaGetCharacterPopupParam;
            if (p == null)
            {
                Debug.LogError("[GachaGetCharacterPopup] param이 GachaGetCharacterPopupParam 타입이 아닙니다.");
                return;
            }

            _isSequenceMode = p.IsSequenceMode;
            IsSkipPressed = false;
            IsSkipAllPressed = false;
            _characterShowElapsed = 0f;

            var charInfo = SpecDataManager.Instance.CharacterInfo.Get(p.CharacterId);
            if (charInfo == null)
            {
                Debug.LogError($"[GachaGetCharacterPopup] CharacterId={p.CharacterId} 에 해당하는 CharacterInfo를 찾을 수 없습니다.");
                return;
            }

            SetCharacterData(charInfo);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
        }

        protected override void OnPostExit()
        {
            ReleaseResources();
            base.OnPostExit();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 시퀀스 모드에서 팝업을 유지한 채 캐릭터 데이터만 교체한다.
        /// 기존 리소스 해제 후 새 캐릭터 세팅.
        /// </summary>
        public void UpdateCharacter(int charId)
        {
            IsSkipPressed = false;
            _characterShowElapsed = 0f;
            ReleaseResources();

            var charInfo = SpecDataManager.Instance.CharacterInfo.Get(charId);
            if (charInfo == null)
            {
                Debug.LogError($"[GachaGetCharacterPopup] UpdateCharacter: CharacterId={charId} 에 해당하는 CharacterInfo를 찾을 수 없습니다.");
                return;
            }

            SetCharacterData(charInfo);
        }

        #endregion

        #region Private Methods

        private void SetCharacterData(CharacterInfo charInfo)
        {
            var grade = charInfo.grade_type;

            // 1. 등급 아이콘
            _gradeSpriteLoader.SetSprite(SpriteNameParser.GetNewGradeSpriteName(grade)).Forget();
            _gradeSpriteLoader.SetNativeSize();

            // 2. 별 활성화
            for (int i = 0; i < _starList.Count; i++)
            {
                _starList[i].SetStar(i < charInfo.init_star);
            }

            // 3. 캐릭터 이름
            _characterNameText.text = LanguageManager.Instance.GetDefaultText(charInfo.name_token);

            // 4. 시너지1 (속성 element)
            _synergy1.SetSynergyUI(charInfo.character_element_type);
            _synergy1NameText.text = GetSynergyName(charInfo.character_element_type);

            // 5. 시너지2 (성군 stella)
            _synergy2.SetSynergyUI(charInfo.character_stella_type);
            _synergy2NameText.text = GetSynergyName(charInfo.character_stella_type);

            // 6. LD 일러스트 (Addressable 프리팹)
            LoadLDIllust(charInfo.prefab_id).Forget();

            // 7. SD 캐릭터 (Addressable 프리팹)
            LoadSDCharacter(charInfo.prefab_id).Forget();

            // 8. SD 스탠드 스프라이트 (등급별)
            // stand sprite 우선 주석처리
            // if (SDStandSpriteMap.TryGetValue(grade, out var sdStandSprite))
            // {
            //     _sdStandSpriteLoader.SetSprite(sdStandSprite).Forget();
            // }
            // else
            // {
            //     Debug.LogWarning($"[GachaGetCharacterPopup] SDStandSpriteMap에 {grade} 등록 없음");
            // }

            // 9. 이펙트 그룹 — LEGENDARY만 활성화
            _effectGroup.SetActive(grade == GradeType.LEGENDARY);

            // 10. 등급별 애니메이션 Trigger
            if (_animator != null && GradeAnimTriggerMap.TryGetValue(grade, out var trigger))
            {
                _animator.SetTrigger(trigger);
            }
        }

        private string GetSynergyName(SynergyType synergyType)
        {
            var synergyList = SpecDataManager.Instance.GetSpecSynergyList(synergyType);
            if (synergyList != null && synergyList.Count > 0)
            {
                return LanguageManager.Instance.GetDefaultText(synergyList[0].name_token);
            }
            return string.Empty;
        }

        private async UniTaskVoid LoadLDIllust(int prefabId)
        {
            string prefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, prefabId);
            _ldHandle = Addressables.InstantiateAsync(prefabName, _ldPosTransform);
            var go = await _ldHandle;
            if (go != null)
            {
                _ldGraphics = go.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < _ldGraphics.Length; i++)
                {
                    _ldGraphics[i].raycastTarget = false;
                    _ldGraphics[i].color = Color.black;
                }
            }
        }

        private async UniTaskVoid LoadSDCharacter(int prefabId)
        {
            string prefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, prefabId);
            _sdHandle = Addressables.InstantiateAsync(prefabName, _sdPosTransform);
            var go = await _sdHandle;
            if (go != null)
            {
                _sdGraphics = go.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < _sdGraphics.Length; i++)
                {
                    _sdGraphics[i].color = Color.black;
                }
            }
        }

        /// <summary>
        /// Black → White로 0.4초간 색상 전환. Animation Event에서 호출.
        /// </summary>
        public void RevealCharacterImages()
        {
            RevealCharacterImagesAsync().Forget();
        }

        private async UniTaskVoid RevealCharacterImagesAsync()
        {
            float duration = _revealCharacterImagesDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Color color = Color.Lerp(Color.black, Color.white, t);

                if (_ldGraphics != null)
                {
                    for (int i = 0; i < _ldGraphics.Length; i++)
                    {
                        if (_ldGraphics[i] != null)
                            _ldGraphics[i].color = color;
                    }
                }

                if (_sdGraphics != null)
                {
                    for (int i = 0; i < _sdGraphics.Length; i++)
                    {
                        if (_sdGraphics[i] != null)
                            _sdGraphics[i].color = color;
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // 최종 White 보장
            if (_ldGraphics != null)
                for (int i = 0; i < _ldGraphics.Length; i++)
                    if (_ldGraphics[i] != null)
                        _ldGraphics[i].color = Color.white;

            if (_sdGraphics != null)
                for (int i = 0; i < _sdGraphics.Length; i++)
                    if (_sdGraphics[i] != null)
                        _sdGraphics[i].color = Color.white;
        }

        /// <summary>
        /// 전체 하위의 비버튼 Graphic들의 raycastTarget을 비활성화하여
        /// 배경 터치 버튼이 클릭을 수신할 수 있도록 한다.
        /// ButtonSkip과 BgTouchButton의 Graphic만 유지.
        /// </summary>
        private void DisableContentRaycastTargets()
        {
            var skipButtonTransform = _skipButton != null ? _skipButton.transform : null;
            var bgTouchTransform = _bgTouchButton != null ? _bgTouchButton.transform : null;
            var graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                // ButtonSkip 하위 Graphic은 유지 (버튼 클릭 필요)
                if (skipButtonTransform != null && graphics[i].transform.IsChildOf(skipButtonTransform))
                    continue;
                // BgTouchButton 하위 Graphic은 유지 (배경 터치 클릭 필요)
                if (bgTouchTransform != null && graphics[i].transform.IsChildOf(bgTouchTransform))
                    continue;
                graphics[i].raycastTarget = false;
            }
        }

        private void ReleaseResources()
        {
            // SpriteLoader 언로드
            _sdStandSpriteLoader.UnloadSprite();
            _gradeSpriteLoader.UnloadSprite();

            // Addressable 핸들 해제
            if (_ldHandle.IsValid())
            {
                Addressables.ReleaseInstance(_ldHandle);
                _ldHandle = default;
            }

            if (_sdHandle.IsValid())
            {
                Addressables.ReleaseInstance(_sdHandle);
                _sdHandle = default;
            }

            // Graphic 참조 정리
            _ldGraphics = null;
            _sdGraphics = null;

            // 자식 오브젝트 정리
            BMUtil.RemoveChildObjects(_ldPosTransform);
            BMUtil.RemoveChildObjects(_sdPosTransform);
        }

        #endregion
    }
}
