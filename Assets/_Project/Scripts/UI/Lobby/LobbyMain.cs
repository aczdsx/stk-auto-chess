using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Lobby/LobbyMain.prefab")]
    public class LobbyMain : UILayer
    {
        [SerializeField] private CAButton _playButton;
        [SerializeField] private CAButton _stageSelectButton;
        [SerializeField] private CAButton _shopButton;

        [Header("Bottom Stage Select Layer")]
        [SerializeField] private ScrollRect _stageSelectScrollRect;
        [SerializeField] private GameObject _stageSelectSlotObject;
        [SerializeField] private Image _chapterImage;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _stageProgressText;


        private List<LobbyBottomStageItemSlot> _stageSlotList = new();

        protected override void Awake()
        {
            base.Awake();
            _playButton.onClick.AddListener(OnClickStartButton);
            _stageSelectButton.onClick.AddListener(OnClickChapterStageButton);
            _shopButton.onClick.AddListener(OnClickCharacterCollectionButton);

            //SceneLoading.GoToNextScene("InGame", (1, 1)).Forget();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _playButton.onClick.RemoveListener(OnClickStartButton);
            _stageSelectButton.onClick.RemoveListener(OnClickChapterStageButton);
            _shopButton.onClick.RemoveListener(OnClickCharacterCollectionButton);
        }

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.Jewel, TopPanelType.Menu);

            SetLobbyMainUI();
        }

        public void RefreshUI()
        {
            // 하단 스테이지 UI 갱신
            RefreshBottomStageUI();
        }

        private void SetLobbyMainUI()
        {
            SetBottomStageUI();

            //TEST
            TestAddCharacter();
            TestAddStage();
        }

        private void SetBottomStageUI()
        {
            ClearBottomSlotLayer();

            int currentStageId = UserDataManager.Instance.GetCurrentStageId();

            var stageSpecData = SpecDataManager.Instance.Stage.Get(currentStageId);
            var chapterSpecData = SpecDataManager.Instance.Chapter.Get(stageSpecData.chapter_id);

            var stageList = SpecDataManager.Instance.GetStageList(stageSpecData.chapter_id, DifficultyType.NORMAL);

            //_chapterImage.sprite = specStage.chapter_image;
            _chapterNameText.SetText(chapterSpecData.name_token);

            int totalStageCount = SpecDataManager.Instance.GetStageCount(stageSpecData.chapter_id, DifficultyType.NORMAL);
            _stageProgressText.SetText("{0}/{1}", stageSpecData.stage_number, totalStageCount);

            for (int i = 0; i < stageList.Count; i++)
            {
                GameObject newSlotObject = Instantiate(_stageSelectSlotObject, _stageSelectScrollRect.content);
                LobbyBottomStageItemSlot slot = newSlotObject.GetComponent<LobbyBottomStageItemSlot>();
                slot.SetStageItemSlot(stageList[i]);

                _stageSlotList.Add(slot);
            }
        }

        private void RefreshBottomStageUI()
        {
            if (_stageSlotList == null || _stageSlotList.Count <= 0) return;

            // 기본 데이터 갱신
            int currentStageId = UserDataManager.Instance.GetCurrentStageId();

            var stageSpecData = SpecDataManager.Instance.Stage.Get(currentStageId);
            var chapterSpecData = SpecDataManager.Instance.Chapter.Get(stageSpecData.chapter_id);

            //_chapterImage.sprite = specStage.chapter_image;
            _chapterNameText.SetText(chapterSpecData.name_token);

            int totalStageCount = SpecDataManager.Instance.GetStageCount(stageSpecData.chapter_id, DifficultyType.NORMAL);
            _stageProgressText.SetText("{0}/{1}", stageSpecData.stage_number, totalStageCount);

            // 슬롯 데이터 갱신
            _stageSlotList.ForEach(slot => slot.RefershSlot());
        }

        private void TestAddCharacter()
        {
            UserDataManager.Instance.AddCharacter(40101);
            UserDataManager.Instance.AddCharacter(30201);
            UserDataManager.Instance.AddCharacter(40201);
            UserDataManager.Instance.AddCharacter(40301);
            UserDataManager.Instance.AddCharacter(30401);
        }

        private void TestAddStage()
        {
            var stageList1 = SpecDataManager.Instance.GetStageList(1, DifficultyType.NORMAL);
            foreach (var stageData in stageList1)
            {
                int random = Random.Range(1, 4);

                UserDataManager.Instance.SetUserStage(stageData.id, random);
            }
        }

        private void OnClickCommanderSkillButton()
        {
            SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(false);
            SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>(null, callbackObject =>
            {
                SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(true);
            }).Forget();
        }
        private void OnClickStartButton()
        {
            SceneLoading.GoToNextScene("InGame", (1, 1)).Forget();
        }

        private void OnClickChapterStageButton()
        {
            int currentStageId = UserDataManager.Instance.GetCurrentStageId();
            SceneUILayerManager.Instance.PushUILayerAsync<ChapterListPopup>(currentStageId).Forget();
        }

        private void OnClickCharacterCollectionButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget();
        }

        private void ClearBottomSlotLayer()
        {
            _stageSlotList.Clear();

            BMUtil.RemoveChildObjects(_stageSelectScrollRect.content);
        }
    }
}
