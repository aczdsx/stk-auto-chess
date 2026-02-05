using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GachaCommonCharacterLayer : GachaBaseLayer
    {
        [Header("Button")]
        [SerializeField] private CAButton _gacha1Button;
        [SerializeField] private Image _gacha1ButtonCostImage;
        [SerializeField] private SpriteLoader _gacha1ButtonCostSpriteLoader;
        [SerializeField] private TextMeshProUGUI _gacha1ButtonCostText;

        [Space(10)]
        [SerializeField] private CAButton _gacha10Button;
        [SerializeField] private Image _gacha10ButtonCostImage;
        [SerializeField] private SpriteLoader _gacha10ButtonCostSpriteLoader;
        [SerializeField] private TextMeshProUGUI _gacha10ButtonCostText;

        private void Awake()
        {
            _gacha1Button.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickGacha1Button(), AwaitOperation.Drop).AddTo(this);
            _gacha10Button.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickGacha10Button(), AwaitOperation.Drop).AddTo(this);
        }

        public void SetGachaLayer(GachaPopup parentPopup)
        {
            _parentGachaPopup = parentPopup;

            _specGachaDataOneTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_1_TIME_COUNT);
            _specGachaDataTenTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_10_TIME_COUNT);

            _gacha1ButtonCostSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_specGachaDataOneTime.gacha_cost_item_id)).Forget();
            _gacha1ButtonCostText.text = $"x{_specGachaDataOneTime.gacha_cost}";

            _gacha10ButtonCostSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_specGachaDataTenTime.gacha_cost_item_id)).Forget();
            _gacha10ButtonCostText.text = $"x{_specGachaDataTenTime.gacha_cost}";
        }

        private async UniTask OnClickGacha1Button()
        {
            if (_specGachaDataOneTime == null) return;

            //[TODO] 임시 단챠 early return
            if (ServerDataManager.Instance.GuideMission.Data.GuideMissionId == 101)
            {
                ToastManager.Instance.ShowToastByTokenKey("10회 모집을 진행해주세요.");
                return;
            }

            await ProcessCharacterGacha(GachaCountType.ONE);
        }

        private async UniTask OnClickGacha10Button()
        {
            if (_specGachaDataTenTime == null) return;

            await ProcessCharacterGacha(GachaCountType.TEN);
        }
    }
}