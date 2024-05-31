using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/InGameResultPopup.prefab")]
    public class InGameResultPopup : UILayer
    {
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _nextStageButton;

        [SerializeField] private GameObject _failObj;
        [SerializeField] private GameObject _victoryObj;

        private bool _isVictory = false;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            _isVictory = (bool) param;
            _failObj.SetActive(!_isVictory);
            _victoryObj.SetActive(_isVictory);

            _exitButton?.onClick.AddListener(OnExitButtonClicked);
            _nextStageButton?.onClick.AddListener(OnNextStageButtonClicked);
        }

        private void OnExitButtonClicked()
        {
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby", null, transition).Forget();
        }

        private void OnNextStageButtonClicked()
        {
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby", null, transition).Forget();
        }
    }
}
