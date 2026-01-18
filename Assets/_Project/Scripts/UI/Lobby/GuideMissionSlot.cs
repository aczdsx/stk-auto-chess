using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GuideMissionSlot : CachedMonoBehaviour
    {
        [SerializeField] private GameObject _activateLayerObject;
        [SerializeField] private CAButton _guideMissionButton;

        [SerializeField] private TextMeshProUGUI _missionTitleText;
        [SerializeField] private TextMeshProUGUI _missionDescText;

        [SerializeField] private Image _missionRewardItemImage;
        [SerializeField] private SpriteLoader _missionRewardItemSpriteLoader;
        [SerializeField] private Image _rewardCharacterBGImage;
        [SerializeField] private Image _missionRewardCharacterImage;
        [SerializeField] private SpriteLoader _missionRewardCharacterSpriteLoader;
        [SerializeField] private TextMeshProUGUI _missionRewardAmountText;

        private GuideMissionModel GuideMissionModel => ServerDataManager.Instance.GuideMission;
        private GuideMissionInfo _specGuideMissionData;

        private void Awake()
        {
            _guideMissionButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickMissionSlotButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);

            // 가이드 미션 데이터 변경 시 UI 갱신
            GuideMissionModel.OnChanged
                .Subscribe(this, (_, self) => self.RefreshGuideMissionSlot())
                .AddTo(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public void InitGuideMissionSlot()
        {
            int guideMissionId = (int)GuideMissionModel.GuideMissionId;
            _specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(guideMissionId);

            SetGuideMissionSlot();

            // 다이얼로그 팝업 체크
            if (_specGuideMissionData != null)
            {
                DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.GUIDE_START, _specGuideMissionData.id.ToString());
            }
        }

        public void RefreshGuideMissionSlot()
        {
            // 모든 가이드 미션 완료 시 off 처리
            if (GuideMissionModel.IsAllCompleted)
            {
                gameObject.SetActive(false);
                return;
            }

            int currentOrder = (int)GuideMissionModel.Order;

            // 가이드 미션 최대 오더일 경우 off 처리
            if (currentOrder > SpecDataManager.Instance.GetGuideMissionMaxOrder())
            {
                gameObject.SetActive(false);
                return;
            }

            // 가이드 미션 슬롯 데이터 세팅
            int guideMissionId = (int)GuideMissionModel.GuideMissionId;
            _specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(guideMissionId);

            SetGuideMissionSlot();

            // 다이얼로그 팝업 체크
            if (_specGuideMissionData != null)
            {
                DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.GUIDE_START, _specGuideMissionData.id.ToString(),
                    () =>
                    {
                        if (_specGuideMissionData.id == 1)
                            SceneUILayerManager.Instance.PushUILayerAsync<NicknamePopup>(true).Forget();
                    });
            }
        }

        private void SetGuideMissionSlot()
        {
            if (_specGuideMissionData == null) return;

            _missionTitleText.text = LanguageManager.Instance.GetDefaultText(_specGuideMissionData.name_token);
            _missionDescText.text = LanguageManager.Instance.GetDefaultText(_specGuideMissionData.desc_token);

            SetGuideMissionRewardImage();
            _missionRewardAmountText.text = $"x{_specGuideMissionData.item_count}";

            // 보상 수령 가능 여부에 따라 활성화 레이어 표시
            _activateLayerObject.SetActive(GuideMissionModel.CanClaimReward);
        }

        private void SetGuideMissionRewardImage()
        {
            ItemId itemId = _specGuideMissionData.item_id;
            _missionRewardItemImage.gameObject.SetActive(false);
            _rewardCharacterBGImage.gameObject.SetActive(itemId.IsCharacter());
            _missionRewardCharacterImage.gameObject.SetActive(itemId.IsCharacterPiece());
            if (itemId.IsCharacter())
            {
                itemId.GetCharacterId(out var charIndex);
                var characterData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);
                _missionRewardCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(characterData.prefab_id)).Forget();
            }
            else if (itemId.IsCharacterPiece())
            {
                itemId.GetCharacterId(out var charIndex);
                var characterPieceData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);
                _missionRewardItemSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(characterPieceData.prefab_id)).Forget();
            }
            else
            {
                _missionRewardItemSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(itemId)).Forget();
                _missionRewardItemImage.gameObject.SetActive(true);
            }
        }

        private async UniTask OnClickMissionSlotButtonAsync()
        {
            if (_specGuideMissionData == null) return;

            // 보상을 받을 수 있는 경우
            if (GuideMissionModel.CanClaimReward)
            {
                await ClaimRewardAsync();
            }
            else
            {
                HandleGuideMissionNavigation();
            }
        }

        private async UniTask ClaimRewardAsync()
        {
            // 서버에 보상 수령 요청
            var response = await NetManager.Instance.GuideMission.ClaimRewardAsync(GuideMissionModel.GuideMissionId);
            if (response == null || !response.IsSuccess)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_ERROR_NETWORK");
                return;
            }

            // 보상 목록 생성 (서버 응답 기반)
            List<RewardItem> rewardItemList = new List<RewardItem>();
            if (response.Rewards != null)
            {
                for (int i = 0; i < response.Rewards.Count; i++)
                {
                    var reward = response.Rewards[i];
                    rewardItemList.Add(new RewardItem(reward));
                }
            }

            // 보상 팝업 표시
            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), callback =>
            {
                // 앱이벤트 처리
                AppEventManager.Instance.GuideMissionClear(_specGuideMissionData.order);
            }).Forget();
        }

        private void HandleGuideMissionNavigation()
        {
            if (_specGuideMissionData.guide_mission_type == GuideMissionType.CLEAR_STAGE)
            {
                GuideMissionInfo specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(_specGuideMissionData.id);

                StageInfo guideStageData = SpecDataManager.Instance.GetStageData(_specGuideMissionData.sub_key);
                StageInfo currentStageData = SpecDataManager.Instance.GetStageData((int)LocalDataManager.Instance.GetLastPlayStageId());

                bool isMatchChapter = guideStageData.chapter_id == currentStageData.chapter_id;

                if (specGuideMissionData.guide_mission_type == GuideMissionType.CLEAR_STAGE && !isMatchChapter)
                {
                    // 스테이지 데이터 세팅
                    var lastestStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
                    var lastestSpecStageData = SpecDataManager.Instance.GetStageData(lastestStageID);
                    var nextStageData = SpecDataManager.Instance.GetNextStageData(lastestStageID);

                    // 가장 최신 챕터를 확인하고 플레이 가능한 최대 스테이지 넘버로 이동
                    int targetStageNumber = 1;
                    if (lastestSpecStageData != null && lastestSpecStageData.chapter_id == guideStageData.chapter_id)
                    {
                        if (nextStageData != null)
                        {
                            targetStageNumber = nextStageData.stage_number;
                        }
                    }
                    // 스테이지 데이터 세팅
                    var targetSpecStage = SpecDataManager.Instance.GetStageData(nextStageData.chapter_id, targetStageNumber, nextStageData.difficulty_type);
                    LocalDataManager.Instance.SetLastPlayStageId((uint)targetSpecStage.stage_id);


                    // 로비 배경 전환
                    InGameManager.Instance.EndInGame();
                    // 로비 배경 전환 및 챕터 이동
                    SceneTransition.Create<SceneTransition_FadeInOut>();
                    SceneTransition.FadeInAsync().Forget();
                    SceneLoading.GoToNextScene("Lobby", guideStageData.chapter_id);

                    // 로비 메인 하단 스테이지 UI 갱신
                    var battleReadyMain = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
                    if (battleReadyMain != null)
                    {
                        battleReadyMain.RefreshUI(LobbyMainRefreshType.STAGE);
                    }
                    return;
                }
            }
            ObjectRegistry.GetObject<GuideAlert>(RegistryKey.GuideAlert)?.UpdateAlert();
        }
    }
}
