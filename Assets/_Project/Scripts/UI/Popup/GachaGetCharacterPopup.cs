using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가챠 획득 캐릭터 단일 풀스크린 팝업 파라미터
    /// </summary>
    public class GachaGetCharacterPopupParam
    {
        /// <summary>획득한 캐릭터 ID</summary>
        public int CharacterId;
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

        [Header("Button")]
        [SerializeField] private CAButton _skipButton;

        #endregion

        #region Private Fields

        private AsyncOperationHandle<GameObject> _ldHandle;
        private AsyncOperationHandle<GameObject> _sdHandle;

        #endregion

        #region Grade SD Stand Sprite Mapping

        private static readonly Dictionary<GradeType, string> SDStandSpriteMap = new()
        {
            [GradeType.RARE]      = "UI_Gacha_Common_SD_Stand_R",
            [GradeType.EPIC]      = "UI_Gacha_Common_SD_Stand_SR",
            [GradeType.LEGENDARY] = "UI_Gacha_Common_SD_Stand_SSR",
        };

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            _skipButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => SceneUILayerManager.Instance.PopUILayer(self))
                .AddTo(this);
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
            ReleaseResources();
            base.OnPreExit();
        }

        #endregion

        #region Private Methods

        private void SetCharacterData(CharacterInfo charInfo)
        {
            var grade = charInfo.grade_type;

            // 1. 등급 아이콘
            _gradeSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(grade)).Forget();

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
            if (SDStandSpriteMap.TryGetValue(grade, out var sdStandSprite))
            {
                _sdStandSpriteLoader.SetSprite(sdStandSprite).Forget();
            }
            else
            {
                Debug.LogWarning($"[GachaGetCharacterPopup] SDStandSpriteMap에 {grade} 등록 없음");
            }

            // 9. 이펙트 그룹 — LEGENDARY만 활성화
            _effectGroup.SetActive(grade == GradeType.LEGENDARY);
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
            await _ldHandle;
        }

        private async UniTaskVoid LoadSDCharacter(int prefabId)
        {
            string prefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, prefabId);
            _sdHandle = Addressables.InstantiateAsync(prefabName, _sdPosTransform);
            await _sdHandle;
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

            // 자식 오브젝트 정리
            BMUtil.RemoveChildObjects(_ldPosTransform);
            BMUtil.RemoveChildObjects(_sdPosTransform);
        }

        #endregion
    }
}
