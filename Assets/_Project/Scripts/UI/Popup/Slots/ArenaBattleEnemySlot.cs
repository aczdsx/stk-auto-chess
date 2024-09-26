using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace CookApps.AutoBattler
{
    public class ArenaBattleEnemySlot : MonoBehaviour
    {
        [Header("Common")] 
        [SerializeField] private GameObject _tierLayer;
        [SerializeField] private GameObject _battleLayer;
        [SerializeField] private CAButton _battleButton;
        [SerializeField] private GameObject _enableBattleButtonObject;
        [SerializeField] private GameObject _disableBattleButtonObject;
        [SerializeField] private Gradient2 _winGradient2;
        [SerializeField] private Gradient2 _loseGradient2;
        
        [Header("Enemy Info")]
        [SerializeField] private TextMeshProUGUI _enemyLevelText;
        [SerializeField] private TextMeshProUGUI _enemyNicknameText;
        [SerializeField] private TextMeshProUGUI _enemyBattlePowerText;
        [SerializeField] private Image _enemyRankTierImage;
        [SerializeField] private TextMeshProUGUI _enemyRankPointText;

        [Header("Character Layer")] 
        [SerializeField] private ScrollRect _characterDeckScrollRect;
        [SerializeField] private GameObject _characterDeckObject;

        [Header("Battle Result Layer")]
        [SerializeField] private GameObject _battleResultLayer;
        [SerializeField] private GameObject _battleResultWinObject;
        [SerializeField] private GameObject _battleResultLoseObject;
        [SerializeField] private TextMeshProUGUI _battleResultText;
        [SerializeField] private TextMeshProUGUI _battleResultTimeText;
        [SerializeField] private Gradient2 _battleResultImageGradient2;
        
        [Header("Revenge Layer")]
        [SerializeField] private GameObject _revengeLayer;
        [SerializeField] private GameObject _revengeTierLayer;
        [SerializeField] private CAButton _revengeButton;
        [SerializeField] private Image _revengeRankTierImage;
        [SerializeField] private TextMeshProUGUI _revengeRankPointText;
        
        private UserPVPBattleSimpleData _userPVPBattleSimpleData;
        private PvpMatchHistoryData _pvpMatchHistoryData;

        private SpecPVPTier _specTierData;
        private ArenaMainPopup _parentPopup;
        
        private bool _isBattleLogSlot = false;
        private bool _isDummyUser = false;
        
        private void Awake()
        {
            _battleButton.onClick.AddListener(OnClickBattleButton);
            _revengeButton.onClick.AddListener(OnClickRevengeButton);
        }

        private void OnDestroy()
        {
            _battleButton.onClick.RemoveListener(OnClickBattleButton);
            _revengeButton.onClick.RemoveListener(OnClickRevengeButton);
        }

        public void InitMatchSlot(UserPVPBattleSimpleData data, ArenaMainPopup parentPopup)
        {
            if (data == null) return;

            _isBattleLogSlot = false;
            _parentPopup = parentPopup;
            
            _userPVPBattleSimpleData = data;       
            _pvpMatchHistoryData = null;
            
            _isDummyUser = SpecDataManager.Instance.IsPVPDummyUser(_userPVPBattleSimpleData.PlayerId);
            
            _battleLayer.SetActive(true);
            
            _enemyLevelText.text = $"Lv.{_userPVPBattleSimpleData.PlayerLv}";
            _enemyNicknameText.text = _userPVPBattleSimpleData.Nickname;
            _enemyBattlePowerText.text = _userPVPBattleSimpleData.BattlePoint.ToString();

            _specTierData = SpecDataManager.Instance.GetPVPTierData(_userPVPBattleSimpleData.RankId);
            _enemyRankTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(_specTierData.pvp_tier_type);
            _enemyRankPointText.text = _userPVPBattleSimpleData.RankPoint.ToString();
            
            CreateCharacterDeckList();

            SetBattleResultLayer();
            SetRevengeLayer();
        }

        public void InitBattleLogSlot(PvpMatchHistoryData data)
        {
            if (data == null) return;

            _isBattleLogSlot = true;
            
            _userPVPBattleSimpleData = BMUtil.DecompressGzipToDataClass<UserPVPBattleSimpleData>(data.OpponentSimpleInfo);
            _userPVPBattleSimpleData.MatchResult = (int)data.Result;    // 전투 결과 데이터 추가 설정
            
            _isDummyUser = SpecDataManager.Instance.IsPVPDummyUser(_userPVPBattleSimpleData.PlayerId);
            
            _pvpMatchHistoryData = data;
            
            _battleLayer.SetActive(false);
            
            _enemyLevelText.text = $"Lv.{_userPVPBattleSimpleData.PlayerLv}";
            _enemyNicknameText.text = _userPVPBattleSimpleData.Nickname;
            _enemyBattlePowerText.text = _userPVPBattleSimpleData.BattlePoint.ToString();

            _specTierData = SpecDataManager.Instance.GetPVPTierData(_userPVPBattleSimpleData.RankId);
            _enemyRankTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(_specTierData.pvp_tier_type);
            _enemyRankPointText.text = _userPVPBattleSimpleData.RankPoint.ToString();
            
            CreateCharacterDeckList();

            SetBattleResultLayer();
            SetRevengeLayer();
        }

        public void RefreshSlot()
        {
            
        }
        
        private void CreateCharacterDeckList()
        {
            ClearSlot();

            foreach (var simpleDeckData in _userPVPBattleSimpleData.SimpleDeckList)
            {
                GameObject newSlotObject = Instantiate(_characterDeckObject, _characterDeckScrollRect.content);
                var characterDeckSlot = newSlotObject.GetComponent<CharacterItemSlot>();
                
                characterDeckSlot.SetSlot(simpleDeckData.Id, simpleDeckData.Lv);
            }
        }

        private void SetBattleResultLayer()
        {
            bool haveResult = _userPVPBattleSimpleData.MatchResult != (int)PvpMatchResult.Unspecified;

            _tierLayer.SetActive(!haveResult);
            _battleResultLayer.SetActive(haveResult);
            
            _enableBattleButtonObject.SetActive(!haveResult);
            _disableBattleButtonObject.SetActive(haveResult);
            
            if (haveResult)
            {
                bool isWin = _userPVPBattleSimpleData.MatchResult == (int)PvpMatchResult.Win || _userPVPBattleSimpleData.MatchResult == (int)PvpMatchResult.RevengeWin;
                bool isLose = _userPVPBattleSimpleData.MatchResult == (int)PvpMatchResult.Lose || _userPVPBattleSimpleData.MatchResult == (int)PvpMatchResult.RevengeLose;
                
                _battleResultWinObject.SetActive(isWin);
                _battleResultLoseObject.SetActive(isLose);
                
                _battleResultImageGradient2.EffectGradient = isWin ? _winGradient2.EffectGradient : _loseGradient2.EffectGradient;
                
                string resultText = isWin  ? LanguageManager.Instance.GetLanguageText("UI_WIN") : LanguageManager.Instance.GetLanguageText("UI_LOSE");
                _battleResultText.text = resultText;

                _battleResultText.color = isWin ? _winGradient2.EffectGradient.colorKeys[0].color : _loseGradient2.EffectGradient.colorKeys[0].color;

                string resultString = "";
                if (_userPVPBattleSimpleData.RefreshTimestamp > 0)
                {
                    resultString = LanguageManager.Instance.GetTimeSpanFromNowText(_userPVPBattleSimpleData.RefreshTimestamp);
                }
                _battleResultTimeText.text = resultString;
                
                _battleResultTimeText.color = isWin ? _winGradient2.EffectGradient.colorKeys[0].color : _loseGradient2.EffectGradient.colorKeys[0].color;
            }
        }

        private void SetRevengeLayer()
        {
            bool isLoseBattle = _userPVPBattleSimpleData.MatchResult == (int)PvpMatchResult.Lose;
            
            _revengeLayer.SetActive(_isBattleLogSlot);
            _revengeButton.gameObject.SetActive(isLoseBattle);
            
            float tierLayerScale = isLoseBattle ? 1.0f : 1.5f;
            _revengeTierLayer.GetComponent<RectTransform>().localScale = new Vector3(tierLayerScale, tierLayerScale, tierLayerScale);

            if (_isBattleLogSlot)
            {
                _revengeRankTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(_specTierData.pvp_tier_type);

                int rankPointDiff = _pvpMatchHistoryData.MyAfterScore - _pvpMatchHistoryData.MyBeforeScore;
                string markText = rankPointDiff > 0 ? "+" : "";
                _revengeRankPointText.text = $"{_pvpMatchHistoryData.MyBeforeScore}<color=#BFFF39>({markText}{rankPointDiff})</color>";
            }
        }
        
        private async void OnClickBattleButton()
        {
            // 아레나 티켓 체크
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.PVP_TICKET, 0, 1, true))
            {
                return;
            }
            
            // 방어덱 설정 여부 체크
            var defenseDeckList = UserDataManager.Instance.GetPVPDefenseCharacterDeckDataList();
            if (defenseDeckList == null || defenseDeckList.Count <= 0)
            {
                _parentPopup.PlayGuide();
                ToastManager.Instance.ShowToastByTokenKey("MSG_PVP_SET_DEF_DECK");
                return;
            }
            
            // todo.. 상대방 덱 업데이트 서버 체크

            // 서버로 부터 상대방 덱 정보 로드 및 체크
            PVPProfileData pvpProfileData = new PVPProfileData();
            if (_isDummyUser)
            {
                var specDummyData = SpecDataManager.Instance.GetPVPDummyData(_userPVPBattleSimpleData.PlayerId);
                if (specDummyData == null)
                {
                    ToastManager.Instance.ShowToastByTokenKey("PVP_OPPONENT_DATA_ERROR");
                    return;
                }
                
                pvpProfileData.SimpleData = BMUtil.ConvertFromJsonDeserialize<UserPVPBattleSimpleData>(specDummyData.dummy_simple_info);
                pvpProfileData.DetailData = BMUtil.ConvertFromJsonDeserialize<UserPVPBattleDetailData>(specDummyData.dummy_heavy_info);
                
                // 데이터 유효성 검증
                if (pvpProfileData.SimpleData == null || pvpProfileData.DetailData == null)
                {
                    ToastManager.Instance.ShowToastByTokenKey("PVP_OPPONENT_DATA_ERROR");
                    return;
                }
            }
            else
            {
                pvpProfileData = await PVPManager.Instance.GetPVPProfileData(_userPVPBattleSimpleData.PlayerId, 2);
                if (pvpProfileData == null || pvpProfileData.DetailData == null)
                {
                    ToastManager.Instance.ShowToastByTokenKey("PVP_OPPONENT_DATA_ERROR");
                    return;
                }
            }
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            // 인게임 씬 집입
            InGameManager.Instance.EndInGame();
            SceneTransition_Animator transition = SceneTransition_Animator.Create();
            UserPVPBattleDetailData data = pvpProfileData.DetailData;   // 상대방 디테일 덱
            data.MatchId = "";
            SceneLoading.GoToNextScene("InGame",
                (InGameType.PVP, (IGameStateUI) new InGameMainStatePvpUI(), data),
                transition).Forget();
        }

        private async void OnClickRevengeButton()
        {
            // 아레나 티켓 체크
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.PVP_TICKET, 0, 1, true))
            {
                return;
            }
            
            // 방어덱 설정 여부 체크
            var defenseDeckList = UserDataManager.Instance.GetPVPDefenseCharacterDeckDataList();
            if (defenseDeckList == null || defenseDeckList.Count <= 0)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_PVP_SET_DEF_DECK");
                return;
            }
            
            // 서버로 부터 상대방 덱 정보 로드 및 체크
            var pvpProfileData = await PVPManager.Instance.GetPVPProfileData(_userPVPBattleSimpleData.PlayerId, 2);
            if (pvpProfileData == null || pvpProfileData.DetailData == null)
            {
                ToastManager.Instance.ShowToastByTokenKey("PVP_OPPONENT_DATA_ERROR");
                return;
            }
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            // 인게임 씬 집입
            InGameManager.Instance.EndInGame();
            SceneTransition_Animator transition = SceneTransition_Animator.Create();
            UserPVPBattleDetailData data = pvpProfileData.DetailData;   // 상대방 디테일 덱
            data.MatchId = _pvpMatchHistoryData.MatchId;
            SceneLoading.GoToNextScene("InGame",
                (InGameType.PVP, (IGameStateUI) new InGameMainStatePvpUI(), data),
                transition).Forget();
        }
        
        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_characterDeckScrollRect.content);
        }
        
        //////// TEST ///////
        [ContextMenu("Play Test Battle - WIN")]
        public async void TestBattle_Win()
        {
            var simpleData = BMUtil.ConvertToJsonSerialize(_userPVPBattleSimpleData);
            await PVPManager.Instance.SendMatchPVPBattleResult(PvpMatchResult.Win, _userPVPBattleSimpleData.PlayerId, simpleData);
        }
        
        [ContextMenu("Play Test Battle - LOSE")]
        public async void TestBattle_Lose()
        {
            var simpleData = BMUtil.ConvertToJsonSerialize(_userPVPBattleSimpleData);
            await PVPManager.Instance.SendMatchPVPBattleResult(PvpMatchResult.Lose, _userPVPBattleSimpleData.PlayerId, simpleData);
        }
        
        [ContextMenu("Play Test Revenge - WIN")]
        public async void TestRevenge_Win()
        {
            var simpleData = BMUtil.ConvertToJsonSerialize(_userPVPBattleSimpleData);
            await PVPManager.Instance.SendMatchPVPRevengeResult(PvpMatchResult.RevengeWin, _userPVPBattleSimpleData.PlayerId, simpleData, _pvpMatchHistoryData.MatchId);
        }
        
        [ContextMenu("Play Test Revenge - LOSE")]
        public async void TestRevenge_Lose()
        {
            var simpleData = BMUtil.ConvertToJsonSerialize(_userPVPBattleSimpleData);
            await PVPManager.Instance.SendMatchPVPRevengeResult(PvpMatchResult.RevengeLose, _userPVPBattleSimpleData.PlayerId, simpleData, _pvpMatchHistoryData.MatchId);
        }
        
    }
}