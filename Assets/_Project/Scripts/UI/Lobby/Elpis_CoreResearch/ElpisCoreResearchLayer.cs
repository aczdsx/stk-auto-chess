using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using R3;
using R3.Triggers;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public struct CoreResearchCacheData
    {
        public ElpisDimensionLab Data;
        public bool IsMax;
    }

    public class ElpisCoreResearchLayer : UILayer
    {
        public SerializableDictionary<CoreResearchType, Color> iconColors;
        public SerializableDictionary<CoreResearchType, Gradient> iconGradients;

        [SerializeField] private SerializableDictionary<DimensionType, ElpisCoreCategory> dimensionToggles;
        [SerializeField] private SerializableDictionary<int, GameObject> coreItemParents;

        [SerializeField] private TMP_Text decoText;
        [SerializeField] private SimpleImageSwapper dimensionSwapper;
        [SerializeField] private CAButton closeButton;
        
        [Header("강화 관련")]
        [SerializeField] private TMP_Text requiredCoreText;
        [SerializeField] private TMP_Text currentCoreText;
        [SerializeField] private CAButton upgradeButton;
        [SerializeField] private TMP_Text[] upgradeButtonTexts;
        [SerializeField] private TMP_Text coreTitleText;
        [SerializeField] private TMP_Text requiredCoreTitleText;
        [SerializeField] private SpriteLoader requireItemImage;
        
        [Header("스탯 관련")]
        [SerializeField] private ElpisCoreStatItem[] coreStats;

        private List<ElpisCoreItem> currentCoreItems;
        private readonly Dictionary<int, List<ElpisDimensionLab>> cachedCoreDatasByUpgradeGroupId = new();
        private readonly Dictionary<DimensionType, List<CoreResearchCacheData>> userCachedCoreDatas = new();

        private readonly List<CoreResearch> tempUserDataList = new();
        private readonly Dictionary<int, int> tempUserLevelMap = new();

        private const DimensionType defaultType = DimensionType.KNIGHT;

        private GuideMissionDataBridge guideMissionDataBridge;

        private ElpisDataBridge elpisDataBridge;
        
        private Dictionary<int, List<ElpisCoreItem>> coreItems = new();
        private ElpisDimensionLab selectedCoreData;
        private ElpisCoreItem selectedCoreItem;
        private InventoryDataBridge inventoryDataBridge;

        private int currentItemCount;
        private SimpleTextColorSwapper _cacheRequireCoreTextColorSwapper;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            
            inventoryDataBridge = new InventoryDataBridge();
            
            // 버튼 클릭 이벤트 구독 (AwaitOperation.Drop: 비동기 작업 중일 때 새 클릭 무시)
            upgradeButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.Upgrade(), AwaitOperation.Drop).AddTo(this);
            closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.CloseThisUILayer()).AddTo(this);

            elpisDataBridge ??= new ElpisDataBridge();
            guideMissionDataBridge ??= new GuideMissionDataBridge();

            CacheCoreDatasByUpgradeGroupId();
            CacheUserCoreDatas();

            InitializeToggles();
            InitializeCoreItems();

            if (_cacheRequireCoreTextColorSwapper is null)
            {
                _cacheRequireCoreTextColorSwapper = requiredCoreText.gameObject.GetComponent<SimpleTextColorSwapper>();
            }

            SetToggle(defaultType);
        }

        private void InitializeToggles()
        {
            foreach (var toggle in dimensionToggles)
            {
                userCachedCoreDatas.TryGetValue(toggle.Key, out var coreDataList);
                toggle.Value.Initialize(toggle.Key, coreDataList, SetToggle);
            }
        }

        private void InitializeCoreItems()
        {
            coreItems.Clear();

            foreach (var coreItemParent in coreItemParents)
            {
                var items = coreItemParent.Value.GetComponentsInChildren<ElpisCoreItem>(true);
                coreItems[coreItemParent.Key] = new List<ElpisCoreItem>(items);
            }
        }

        #region Cache Data

        private void CacheCoreDatasByUpgradeGroupId()
        {
            cachedCoreDatasByUpgradeGroupId.Clear();

            var allDatas = SpecDataManager.Instance.GetAllElpisDimensionLab();

            foreach (var data in allDatas)
            {
                if (!cachedCoreDatasByUpgradeGroupId.TryGetValue(data.upgrade_group_id, out var list))
                {
                    list = new List<ElpisDimensionLab>();
                    cachedCoreDatasByUpgradeGroupId[data.upgrade_group_id] = list;
                }
                list.Add(data);
            }
        }

        private void CacheUserCoreDatas()
        {
            userCachedCoreDatas.Clear();
            tempUserDataList.Clear();
            tempUserLevelMap.Clear();

            elpisDataBridge.GetAllCoreResearches(tempUserDataList);

            foreach (var userData in tempUserDataList)
                tempUserLevelMap[(int)userData.UpgradeGroupId] = (int)userData.Level;

            foreach (var coreDatas in cachedCoreDatasByUpgradeGroupId)
            {
                var upgradeGroupId = coreDatas.Key;

                tempUserLevelMap.TryGetValue(upgradeGroupId, out int currentLevel);
                var nextLevelData = GetCoreDataByUpgradeGroupId(upgradeGroupId, currentLevel + 1);

                CoreResearchCacheData cacheData;
                if (nextLevelData != null)
                {
                    cacheData = new CoreResearchCacheData { Data = nextLevelData, IsMax = false };
                }
                else
                {
                    var currentLevelData = GetCoreDataByUpgradeGroupId(upgradeGroupId, currentLevel) ?? coreDatas.Value[0];
                    cacheData = new CoreResearchCacheData { Data = currentLevelData, IsMax = true };
                }

                if (!userCachedCoreDatas.TryGetValue(cacheData.Data.dimension_type, out var list))
                {
                    list = new List<CoreResearchCacheData>();
                    userCachedCoreDatas[cacheData.Data.dimension_type] = list;
                }
                list.Add(cacheData);
            }
        }

        private ElpisDimensionLab GetCoreDataByUpgradeGroupId(int upgradeGroupId, int level)
        {
            if(!cachedCoreDatasByUpgradeGroupId.TryGetValue(upgradeGroupId, out var list)) return null;
            foreach (var data in list)
            {
                if(data.lv == level) return data;
            }

            return null;
        }

        private void UpdateUserCachedCoreData(DimensionType dimensionType, CoreResearchCacheData newCacheData)
        {
            if (!userCachedCoreDatas.TryGetValue(dimensionType, out var list))
                return;

            var upgradeGroupId = newCacheData.Data.upgrade_group_id;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Data.upgrade_group_id == upgradeGroupId)
                {
                    list[i] = newCacheData;
                    return;
                }
            }
        }

        #endregion

        #region Get

        public CoreResearchCacheData GetNextLevelCoreResearch(ElpisDimensionLab coreData)
        {
            var targetLevel = coreData.lv + 1;
            var nextLevelData = GetCoreDataByUpgradeGroupId(coreData.upgrade_group_id, targetLevel);

            if (nextLevelData != null)
                return new CoreResearchCacheData { Data = nextLevelData, IsMax = false };

            return new CoreResearchCacheData { Data = coreData, IsMax = true };
        }

        private (int, int) GetCumulatedValueByLevel(int groupId, int level)
        {
            int value1 = 0, value2 = 0;
            var targetList = cachedCoreDatasByUpgradeGroupId[groupId];

            foreach (var data in targetList)
            {
                if(data.lv >= level) //UI에 표시는 -1 레벨이라, 레벨이 같아도 넘겨야 됨
                    continue;

                value1 += data.effect_stat_value01;
                value2 += data.effect_stat_value02;
            }

            return (value1, value2);
        }

        #endregion

        #region Set

        private void SetCoreStatItems()
        {
            var (cumulatedValue1, cumulatedValue2) = GetCumulatedValueByLevel(selectedCoreData.upgrade_group_id, selectedCoreData.lv);

            var baseText1 = LanguageManager.Instance.GetDefaultText(selectedCoreData.upgrade_desc_token01);
            var baseText2 = LanguageManager.Instance.GetDefaultText(selectedCoreData.upgrade_desc_token02);

            var titleString1 = cumulatedValue1 > 0 ? baseText1 + " " + ZString.Concat(cumulatedValue1) : baseText1;
            var titleString2 = cumulatedValue2 > 0 ? baseText2 + " " + ZString.Concat(cumulatedValue2) : baseText2;

            coreStats[0].Set(titleString1, ZString.Concat(selectedCoreData.effect_stat_value01));
            coreStats[1].Set(titleString2, ZString.Concat(selectedCoreData.effect_stat_value02));
            
            coreStats[0].SetSprite(selectedCoreData.upgrade_desc_token01);
            coreStats[1].SetSprite(selectedCoreData.upgrade_desc_token02);
        }

        private void SetToggle(DimensionType dimensionType)
        {
            decoText.text = ZString.Concat(dimensionType);
            SetDecoImage(dimensionType);
            SetCoreItems(dimensionType);
        }

        private void SetDecoImage(DimensionType dimensionType)
        {
            var targetType = dimensionType == DimensionType.KNIGHT ? SimpleSwapType.Custom_0 : 
                dimensionType == DimensionType.ELEMENTAL ? SimpleSwapType.Custom_1 : SimpleSwapType.Custom_2;
            dimensionSwapper.Swap(targetType);
        }

        private void SetCoreItems(DimensionType dimensionType)
        {
            SetCoreItemsObjectActive(userCachedCoreDatas[dimensionType].Count);
            SetCoreItemsData(dimensionType);
        }

        private void SetCoreItemsData(DimensionType dimensionType)
        {
            var targetDataList = userCachedCoreDatas[dimensionType];
            for (var i = 0; i < currentCoreItems.Count; i++)
            {
                var targetItem = currentCoreItems[i];
                var targetData = targetDataList[i];
                
                targetItem.SetUp(targetData, this);
            }
            
            CoreSelected(currentCoreItems[0]);
        }

        private void SetCoreItemsObjectActive(int count)
        {
            foreach (var parent in coreItemParents)
                parent.Value.SetActive(parent.Key == count);
            
            currentCoreItems = coreItems[count];
        }

        #endregion

        private void ShowCoreDetail(CoreResearchCacheData coreData)
        {
            SetCoreStatItems();

            requireItemImage.SetSprite(ZString.Format("Icon_Cunsumable{0}", coreData.Data.item_id)).Forget();
            
            requiredCoreTitleText.text = LanguageManager.Instance.GetDefaultText(ZString.Format("ITEM_INFO_NAME_{0}", coreData.Data.item_id));
            coreTitleText.text = ZString.Format(LanguageManager.Instance.GetDefaultText(coreData.Data.upgrade_name_token), coreData.Data.lv - 1);

            currentItemCount = (int)inventoryDataBridge.GetCurrency(coreData.Data.item_id);
            currentCoreText.text = ZString.Concat(currentItemCount, " /");
            requiredCoreText.text = ZString.Concat(coreData.Data.item_INT);
            
            var swapType = currentItemCount >= coreData.Data.item_INT ? SimpleSwapType.Possible 
                : SimpleSwapType.Impossible;
            _cacheRequireCoreTextColorSwapper.Swap(swapType);
            
            var isOverNeedLevel = IsOverNeedLevel();
            foreach (var upgradeButtonText in upgradeButtonTexts)
            {
                var upgradeLocalized = LanguageManager.Instance.GetDefaultText(isOverNeedLevel ? "DIMENSION_UPGRADE" : "DIMENSION_NOT_REACHED_LEVEL");
                upgradeButtonText.text = upgradeLocalized;
            }

            var canUpgrade = !coreData.IsMax && (currentItemCount >= coreData.Data.item_INT) && isOverNeedLevel;
            upgradeButton.SetClickableState( canUpgrade);
        }

        public void CoreSelected(ElpisCoreItem elpisCoreItem)
        {
            foreach (var coreItem in currentCoreItems)
            {
                coreItem.SetHighlight(elpisCoreItem && elpisCoreItem == coreItem);
                
                if (elpisCoreItem != coreItem)
                    continue;
                
                selectedCoreItem = coreItem;
                selectedCoreData = coreItem.Data;
                ShowCoreDetail(selectedCoreItem.CachedData);
            }
        }

        public async UniTask Upgrade()
        {
            // 디버깅: 함수 호출 확인
            Debug.Log($"[ElpisCoreResearch] Upgrade() 호출됨 - selectedCoreItem: {selectedCoreItem != null}, IsOverNeedLevel: {IsOverNeedLevel()}, currentItemCount: {currentItemCount}, required: {selectedCoreData?.item_INT}");
            
            if(selectedCoreItem == null)
            {
                Debug.LogWarning("[ElpisCoreResearch] Upgrade() - selectedCoreItem이 null입니다.");
                return;
            }

            if(!IsOverNeedLevel())
            {
                Debug.LogWarning("[ElpisCoreResearch] Upgrade() - 레벨 조건을 만족하지 않습니다.");
                return;
            }

            // if(currentItemCount < selectedCoreData.item_INT)
            // {
            //     Debug.LogWarning($"[ElpisCoreResearch] Upgrade() - 코어 개수 부족 (현재: {currentItemCount}, 필요: {selectedCoreData.item_INT})");
            //     return;
            // }

            var response = await NetManager.Instance.Elpis.ResearchCoreAsync((uint)selectedCoreData.upgrade_group_id, 1);
            if(!response.IsSuccess)
                return;

            var targetData = GetNextLevelCoreResearch(selectedCoreData);

            // 캐시 및 아이템 데이터 업데이트
            UpdateUserCachedCoreData(targetData.Data.dimension_type, targetData);
            selectedCoreItem.UpdateData(targetData);

            // selectedCoreData는 selectedCoreItem.Data로 동기화
            selectedCoreData = selectedCoreItem.Data;

            ShowCoreDetail(selectedCoreItem.CachedData);

            // await guideMissionDataBridge.AddActionAsync(GuideMissionType.UPGRADE_DIMENSION_CUBE_CORE_RESEARCH, 1);
        }

        private bool IsOverNeedLevel()
        {
            var currentDataNeedLevel = selectedCoreData.need_condition;
            var currentFacilityLevel = elpisDataBridge.GetFacilityLevel(ElpisFacilityType.FacilityTypeDimensionLab);

            return currentFacilityLevel >= currentDataNeedLevel;
        }
    }
}