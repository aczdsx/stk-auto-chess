using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public enum LobbyMainRefreshType
    {
        ALL,
        STAGE,
        GUIDE_MISSION,
        CHARACTER_LAYER,
    }

    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Lobby/LobbyMain.prefab")]
    public class LobbyMain : UILayer
    {
        [SerializeField] private CAButton _playButton;
        [SerializeField] private CAButton _stageSelectButton;
        [SerializeField] private CAButton _shopButton;
        [SerializeField] private CAButton _gachaButton;

        [Header("User Info Layer")]
        [SerializeField] private Image _userIconImage;
        [SerializeField] private TextMeshProUGUI _userNameText;
        [SerializeField] private TextMeshProUGUI _userLevelText;
        [SerializeField] private TextMeshProUGUI _userExpText;
        [SerializeField] private Slider _userExpSlider;

        [Header("Bottom Stage Select Layer")]
        [SerializeField] private ScrollRect _stageSelectScrollRect;
        [SerializeField] private GameObject _stageSelectSlotObject;
        [SerializeField] private Image _chapterImage;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _stageProgressText;

        [Header("Guide Mission")]
        [SerializeField] private GuideMissionSlot _guideMissionSlot;

        private List<LobbyBottomStageSlot> _stageSlotList = new();

        protected override void Awake()
        {
            base.Awake();
            _playButton.onClick.AddListener(OnClickStartButton);
            _stageSelectButton.onClick.AddListener(OnClickChapterStageButton);
            _shopButton.onClick.AddListener(OnClickCharacterCollectionButton);
            _gachaButton.onClick.AddListener(OnClickGachaButton);

            //SceneLoading.GoToNextScene("InGame", (1, 1, DifficultyType.NORMAL)).Forget();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _playButton.onClick.RemoveListener(OnClickStartButton);
            _stageSelectButton.onClick.RemoveListener(OnClickChapterStageButton);
            _shopButton.onClick.RemoveListener(OnClickCharacterCollectionButton);
            _gachaButton.onClick.RemoveListener(OnClickGachaButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.Jewel, TopPanelType.Menu);

            SetLobbyMainUI();
            _guideMissionSlot?.InitGuideMissionSlot();

            // test
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.FIRST_IN, "0");
        }

        public void RefreshUI(LobbyMainRefreshType refreshType)
        {
            switch (refreshType)
            {
                case LobbyMainRefreshType.ALL:
                    SetBottomStageUI();     // 하단 스테이지 UI 갱신
                    _guideMissionSlot?.RefreshGuideMissionSlot();   // 가이드 미션 갱신
                    SetUserInfoLayer();     // 유저 정보 갱신
                    break;
                case LobbyMainRefreshType.STAGE:
                    SetBottomStageUI();
                    break;
                case LobbyMainRefreshType.GUIDE_MISSION:
                    _guideMissionSlot?.RefreshGuideMissionSlot();
                    break;
                case LobbyMainRefreshType.CHARACTER_LAYER:
                    SetUserInfoLayer();
                    break;
            }
        }

        public void RefreshBottomStageUI()
        {
            if (_stageSlotList == null || _stageSlotList.Count <= 0) return;

            // 기본 데이터 갱신
            int currentStageId = UserDataManager.Instance.GetCurrentStageId();

            var stageSpecData = SpecDataManager.Instance.SpecStage.Get(currentStageId);
            var chapterSpecData = SpecDataManager.Instance.SpecChapter.Get(stageSpecData.chapter_id);

            //_chapterImage.sprite = specStage.chapter_image;
            _chapterNameText.SetText(chapterSpecData.name_token);

            int totalStageCount = SpecDataManager.Instance.GetStageCount(stageSpecData.chapter_id, DifficultyType.NORMAL);
            _stageProgressText.SetText("{0}/{1}", stageSpecData.stage_number, totalStageCount);

            // 슬롯 데이터 갱신
            _stageSlotList.ForEach(slot => slot.RefershSlot());
        }

        private void SetLobbyMainUI()
        {
            SetUserInfoLayer();
            SetBottomStageUI();

            //TEST
            TestAddCharacter();
            TestAddStage();
        }

        private void SetUserInfoLayer()
        {
            var userBasicData = UserDataManager.Instance.UserBasicData;

            _userIconImage.sprite = ImageManager.Instance.GetCharacterSubIllustSprite(userBasicData.UserIconId);
            _userNameText.text = userBasicData.Nickname;

            int userLevel = SpecDataManager.Instance.GetAccountLevelByExp(userBasicData.Exp);
            _userLevelText.text = $"Lv.{userLevel}";

            var specLevelData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel(userLevel);
            if (specLevelData != null)
            {
                long leftExp = userBasicData.Exp - specLevelData.exp_start;
                float resultValue = leftExp / (float) specLevelData.exp_need;

                _userExpSlider.value = resultValue;
                _userExpText.text = string.Format("{0:N2}%", resultValue * 100);
            }
        }

        private void SetBottomStageUI()
        {
            ClearBottomSlotLayer();

            int currentStagdId = UserDataManager.Instance.GetCurrentStageId();
            int currentChapterId = UserDataManager.Instance.UserStageGroup.CurrentSelectedChapterId;

            var stageSpecData = SpecDataManager.Instance.SpecStage.Get(currentStagdId);
            var chapterSpecData = SpecDataManager.Instance.SpecChapter.Get(currentChapterId);

            var stageList = SpecDataManager.Instance.GetStageList(chapterSpecData.chapter_id, chapterSpecData.difficulty_type);

            //_chapterImage.sprite = specStage.chapter_image;
            _chapterNameText.SetText(chapterSpecData.name_token);

            int totalStageCount = stageList.Count;
            _stageProgressText.SetText("{0}/{1}", stageSpecData.stage_number, totalStageCount);

            for (int i = 0; i < stageList.Count; i++)
            {
                GameObject newSlotObject = Instantiate(_stageSelectSlotObject, _stageSelectScrollRect.content);
                LobbyBottomStageSlot slot = newSlotObject.GetComponent<LobbyBottomStageSlot>();
                slot.SetStageItemSlot(stageList[i]);

                _stageSlotList.Add(slot);
            }
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

            var stageList3 = SpecDataManager.Instance.GetStageList(1, DifficultyType.HARD);
            foreach (var stageData in stageList3)
            {
                int random = Random.Range(1, 4);

                UserDataManager.Instance.SetUserStage(stageData.id, random);
            }

            var stageList2 = SpecDataManager.Instance.GetStageList(2, DifficultyType.NORMAL);
            foreach (var stageData in stageList2)
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
            SceneLoading.GoToNextScene("InGame", (1, 1, DifficultyType.NORMAL)).Forget();
        }

        private void OnClickChapterStageButton()
        {
            int currentStageId = UserDataManager.Instance.GetCurrentStageId();
            SceneUILayerManager.Instance.PushUILayerAsync<ChapterListPopup>(currentStageId).Forget();
        }

        private void OnClickGachaButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<GachaPopup>().Forget();
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
