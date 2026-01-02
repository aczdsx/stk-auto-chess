using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class UIElpisBuildSlot : CachedMonoBehaviour
    {
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
            
            if (!CachedGo.activeSelf)
            {
                CachedGo.SetActive(true);
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            var mainCamera = MainCameraHolder.MainCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, target.CachedTr.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, mainCamera, out Vector2 localPoint);
            CachedRectTr.anchoredPosition = localPoint;
        }

        private void OnClick()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<UIElpisBuildPopup>(facilityData).Forget();
        }
    }
}
