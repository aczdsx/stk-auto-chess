using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class RequirementCurrency : CachedMonoBehaviour
    {
        [SerializeField] private TMP_Text userHavingCountText;
        [SerializeField] private TMP_Text requirementCountText;
        [SerializeField] private TMP_Text currencyTitleText;
        [SerializeField] private SpriteLoader currencyIconLoader;

        private SimpleTextColorSwapper _colorSwapper;

        private void Awake()
        {
            if (userHavingCountText != null)
                _colorSwapper = userHavingCountText.GetComponent<SimpleTextColorSwapper>();
        }

        public void SetAmount(int having, int required)
        {
            if (userHavingCountText != null)
                userHavingCountText.text = ZString.Concat(having);

            if (requirementCountText != null)
                requirementCountText.text = ZString.Concat("/ ", required);

            if (_colorSwapper != null)
            {
                var swapType = having >= required ? SimpleSwapType.Possible : SimpleSwapType.Impossible;
                _colorSwapper.Swap(swapType);
            }
        }

        public void SetTitle(string title)
        {
            if (currencyTitleText != null)
                currencyTitleText.text = title;
        }

        public void SetIcon(string spriteName)
        {
            if (currencyIconLoader != null)
                currencyIconLoader.SetSprite(spriteName).Forget();
        }
    }
}
