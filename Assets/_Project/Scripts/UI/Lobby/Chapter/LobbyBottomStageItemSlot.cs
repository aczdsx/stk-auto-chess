using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class LobbyBottomStageItemSlot : CachedMonoBehaviour
    {
        [Header("State - Normal")]
        [SerializeField] private GameObject _normalLayerObject;
        [SerializeField] private TextMeshProUGUI _normalStageNumberText;

        [Header("State - Boss")]
        [SerializeField] private GameObject _bossLayerObject;
        [SerializeField] private TextMeshProUGUI _bossStageNumberText;

        [Header("State - Done")]
        [SerializeField] private GameObject _doneLayerObject;
        [SerializeField] private TextMeshProUGUI _doneStageNumberText;

        [Header("State - Current")]
        [SerializeField] private GameObject _currentLayerObject;
        [SerializeField] private TextMeshProUGUI _currentStageNumberText;

        private Stage _currentStageData;

        public void SetStageItemSlot(Stage data)
        {
            if (data == null) return;

            _currentStageData = data;
        }

        private void SetStageState()
        {
            switch (_currentStageData.stage_type)
            {

            }
        }
    }
}
