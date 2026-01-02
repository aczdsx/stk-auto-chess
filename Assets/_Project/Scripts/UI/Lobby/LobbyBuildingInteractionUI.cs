using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class LobbyBuildingInteractionUI : CachedMonoBehaviour
    {
        [SerializeField] private TMP_Text buildingName;
        [SerializeField] private CAButton button;
        
        private ElpisBuildingBase target;
        private ElpisFacility facilityData;
        private RectTransform parentRect;
        private bool isInitialize;

        private void Awake()
        {
            button
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClick())
                .AddTo(this);
        }

        public void Initialize(ElpisBuildingBase target, ElpisFacility facilityData)
        {
            this.target = target;
            this.facilityData = facilityData;
            parentRect = CachedTr.parent.GetComponent<RectTransform>();
            isInitialize = true;
            
            UpdatePosition();
        }

        private void LateUpdate()
        {
            if (!isInitialize)
            {
                CachedGo.SetActive(false);
                return;
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            CachedRectTr.anchoredPosition = MainCameraHolder.WorldPointToLocalPointInRectangle(target.CachedTr.position, parentRect);
        }

        private void OnClick()
        {
            var buildingLayer = SceneUILayerManager.Instance.GetUILayer<ElpisBuildLayer>();
            if (buildingLayer == null)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<ElpisBuildLayer>((facilityData, target.SlotIndex)).Forget();
                return;
            }
            buildingLayer.SetTargetFacilityData(facilityData, target.SlotIndex);
        }
    }
}
