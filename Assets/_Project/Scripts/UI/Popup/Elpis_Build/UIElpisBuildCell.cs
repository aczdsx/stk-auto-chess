using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class UIElpisBuildCell : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private SpriteLoader _iconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _conditionText;
        [SerializeField] private CAButton _installButton;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject _installedOverlay;

        private ElpisBuildInfo _buildInfo;
        private Action<ElpisBuildInfo> _onInstallClicked;

        public void SetData(ElpisBuildInfo buildInfo, bool isLocked, bool isInstalled, bool canAfford, Action<ElpisBuildInfo> onInstallClicked)
        {
            _buildInfo = buildInfo;
            _onInstallClicked = onInstallClicked;

            // Basic Info
            if (!string.IsNullOrEmpty(buildInfo.build_prefab))
            {
                // TODO: 실제 아이콘 경로에 맞게 수정 필요. 현재는 prefab 이름이나 ID를 사용한다고 가정.
                 _iconSpriteLoader.SetSprite(buildInfo.build_prefab).Forget(); 
            }
            
            _nameText.text = LanguageManager.Instance.GetLanguageText(buildInfo.buld_name_token);
            _descText.text = LanguageManager.Instance.GetLanguageText(buildInfo.buld_desc_token);
            _costText.text = buildInfo.item_INT.ToString();

            // State Handling
            _lockedOverlay.SetActive(isLocked);
            _installedOverlay.SetActive(isInstalled);
            
            if (isInstalled)
            {
                _installButton.gameObject.SetActive(false);
                _conditionText.text = "설치 완료";
            }
            else if (isLocked)
            {
                _installButton.gameObject.SetActive(false);
                // Locked condition text handling could be more specific
                _conditionText.text = "사령부 레벨 부족"; 
            }
            else
            {
                _installButton.gameObject.SetActive(true);
                _installButton.SetClickableState(canAfford);
                _conditionText.text = "";
            }

            // Bind Button
            _installButton.onClick.RemoveAllListeners();
            _installButton.onClick.AddListener(OnInstallClick);
        }

        private void OnInstallClick()
        {
            _onInstallClicked?.Invoke(_buildInfo);
        }
    }
}
