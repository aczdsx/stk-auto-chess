using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가챠 결과 팝업의 개별 결과 카드 슬롯.
    /// 등급별 색상/스프라이트, LD 일러스트, SD 캐릭터, 별, 시너지, 조각 표시를 담당한다.
    /// </summary>
    public class GachaResultItemSlot : CachedMonoBehaviour
    {
        #region Serialized Fields

        [Header("Background")]
        [SerializeField] private Image _bgGroupImage;
        [SerializeField] private Image _toneAdjustImage;
        [SerializeField] private Image _gradientTopImage;
        [SerializeField] private Image _gradientBotImage;
        [SerializeField] private SpriteLoader _sdStandSpriteLoader;
        [SerializeField] private Transform _sdPosTransform;

        [Header("Character")]
        [SerializeField] private Transform _ldPosTransform;

        [Header("Frame")]
        [SerializeField] private SpriteLoader _frameSpriteLoader;

        [Header("Info")]
        [SerializeField] private List<GameObject> _starObjects;
        [SerializeField] private SynergyUI _synergy1;
        [SerializeField] private SynergyUI _synergy2;

        [Header("Effect")]
        [SerializeField] private GameObject _effectGroup;

        [Header("Animation")]
        [SerializeField] private Animator _animator;

        [Header("Piece")]
        [SerializeField] private GameObject _pieceGroup;
        [SerializeField] private Image _pieceGradientImage;
        [SerializeField] private SpriteLoader _pieceIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _pieceCountText;

        #endregion

        #region Private Fields

        private AsyncOperationHandle<GameObject> _ldHandle;
        private AsyncOperationHandle<GameObject> _sdHandle;

        #endregion

        #region Grade Visual Data

        private struct GradeVisualData
        {
            public Color BgGroupColor;
            public Color ToneAdjustColor;
            public Color GradientTopColor;
            public Color GradientBotColor;
            public Color PieceGradientColor;
            public string SDStandSprite;
            public string FrameSprite;
        }

        private static Color ParseHexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        private static readonly Dictionary<GradeType, GradeVisualData> GradeVisuals = new()
        {
            [GradeType.RARE] = new GradeVisualData
            {
                BgGroupColor = ParseHexColor("#7287C2"),
                ToneAdjustColor = ParseHexColor("#69B4FF40"),
                GradientTopColor = ParseHexColor("#B3D9FFBF"),
                GradientBotColor = ParseHexColor("#6A97F2"),
                PieceGradientColor = ParseHexColor("#0095FF"),
                SDStandSprite = "UI_Gacha_Common_SD_Stand_R",
                FrameSprite = "UI_Gacha_Result_List_Frame_R",
            },
            [GradeType.EPIC] = new GradeVisualData
            {
                BgGroupColor = ParseHexColor("#916CA9"),
                ToneAdjustColor = ParseHexColor("#CD69FF40"),
                GradientTopColor = ParseHexColor("#E6B3FFBF"),
                GradientBotColor = ParseHexColor("#B84FEC"),
                PieceGradientColor = ParseHexColor("#AA00FF"),
                SDStandSprite = "UI_Gacha_Common_SD_Stand_SR",
                FrameSprite = "UI_Gacha_Result_List_Frame_SR",
            },
            [GradeType.LEGENDARY] = new GradeVisualData
            {
                BgGroupColor = ParseHexColor("#DDA84B"),
                ToneAdjustColor = ParseHexColor("#FFC56940"),
                GradientTopColor = ParseHexColor("#FFE5B3BF"),
                GradientBotColor = ParseHexColor("#F29B5C"),
                PieceGradientColor = ParseHexColor("#FFBA00"),
                SDStandSprite = "UI_Gacha_Common_SD_Stand_SSR",
                FrameSprite = "UI_Gacha_Result_List_Frame_SSR",
            },
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// 가챠 결과 아이템과 캐릭터 정보를 받아 슬롯 전체를 세팅한다.
        /// </summary>
        public void SetData(RewardItem rewardItem, CharacterInfo charInfo)
        {
            var grade = charInfo.grade_type;
            if (!GradeVisuals.TryGetValue(grade, out var visual))
            {
                Debug.LogWarning($"[GachaResultItemSlot] GradeVisuals에 {grade} 등록 없음");
                return;
            }

            // 1. 등급별 배경 색상
            _bgGroupImage.color = visual.BgGroupColor;
            _toneAdjustImage.color = visual.ToneAdjustColor;
            _gradientTopImage.color = visual.GradientTopColor;
            _gradientBotImage.color = visual.GradientBotColor;

            // 2. 등급별 스프라이트
            // stand sprite 우선 주석처리
            // _sdStandSpriteLoader.SetSprite(visual.SDStandSprite).Forget();
            _frameSpriteLoader.SetSprite(visual.FrameSprite).Forget();

            // 3. LD 일러스트 (Addressable 프리팹)
            LoadLDIllust(charInfo.prefab_id).Forget();

            // 4. SD 캐릭터 (Addressable 프리팹)
            LoadSDCharacter(charInfo.prefab_id).Forget();

            // 5. 별 활성화
            SetStars(charInfo.init_star);

            // 6. 시너지 (element / stella)
            _synergy1.SetSynergyUI(charInfo.character_element_type);
            _synergy2.SetSynergyUI(charInfo.character_stella_type);

            // 7. 이펙트 그룹 — LEGENDARY만 활성화
            _effectGroup.SetActive(grade == GradeType.LEGENDARY);

            // 8. 조각 그룹
            bool isPiece = rewardItem.Id.IsCharacterPiece();
            _pieceGroup.SetActive(isPiece);
            if (isPiece)
            {
                Debug.LogColor($"charInfo.id : {charInfo.id}");
                Debug.LogColor($"SpriteNameParser.GetCharacterPieceSprite(charInfo.id) : {SpriteNameParser.GetCharacterPieceSprite(charInfo.id)}");
                _pieceGradientImage.color = visual.PieceGradientColor;
                _pieceIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(charInfo.id)).Forget();
                _pieceCountText.text = $"x{rewardItem.Count}";
            }
        }

        /// <summary>
        /// 등급에 맞는 Animator Trigger를 설정하여 등장 애니메이션을 재생한다.
        /// </summary>
        public void PlayGradeAnimation(GradeType grade)
        {
            if (_animator == null) return;

            switch (grade)
            {
                case GradeType.RARE:
                    _animator.SetTrigger("R");
                    break;
                case GradeType.EPIC:
                    _animator.SetTrigger("SR");
                    break;
                case GradeType.LEGENDARY:
                    _animator.SetTrigger("SSR");
                    break;
            }
        }

        /// <summary>
        /// Addressable 핸들 해제 및 자식 오브젝트 정리.
        /// ClearSlots에서 Destroy 직전에 반드시 호출해야 한다.
        /// </summary>
        public void Release()
        {
            _sdStandSpriteLoader.UnloadSprite();
            _frameSpriteLoader.UnloadSprite();
            _pieceIconSpriteLoader?.UnloadSprite();

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

            BMUtil.RemoveChildObjects(_ldPosTransform);
            BMUtil.RemoveChildObjects(_sdPosTransform);
        }

        #endregion

        #region Private Methods

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

        private void SetStars(int starCount)
        {
            for (int i = 0; i < _starObjects.Count; i++)
            {
                _starObjects[i].SetActive(i < starCount);
            }
        }

        #endregion
    }
}
