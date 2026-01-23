using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class InGameTestMain : UILayer
    {
        public float InGameTime => _inGameTime;

        [SerializeField] private Transform _canvasTransform;
        [SerializeField] private RawImage _vignetteImage;

        [SerializeField] private SkillTooltipPopup _skillTooltipPopup;

        [SerializeField] private UIObjectMover _uiObjectMover;
        [SerializeField] private AssetReferenceGameObject _commanderManagerPrefab;

        private float _inGameTime = 0f;
        private IGameStateUICore _currentGameStateUI;
        private InGameType _inGameType;

        public static InGameTestMain GetInGameTestMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameTestMain>();
        }

        public void SetObjectMover(InGameTile startTile, InGameTile endTile)
        {
            if (_uiObjectMover)
                _uiObjectMover.SetMover(startTile, endTile);
        }

        public void SetActiveObjectMover(bool isActive)
        {
            if (_uiObjectMover)
                _uiObjectMover.gameObject.SetActive(isActive);
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            EnterAsync(param).Forget();
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        }
        
        private async UniTask EnterAsync(object param)
        {
            await UniTask.Yield();

            var handle = _commanderManagerPrefab.InstantiateAsync();
            await handle.WaitUntilDone();
            
            switch (param)
            {
                case InGameMainParams inGameParams:
                    _inGameType = inGameParams.InGameType;
                    _currentGameStateUI = inGameParams.GameStateUI;
                    _currentGameStateUI.Initialize(_canvasTransform, inGameParams.StageId).Forget();
                    InGameManager.Instance.SetSessionIdAndRandomSeed(inGameParams.SessionId, inGameParams.RandomSeed);
                    break;
                default:
                    throw new ArgumentException("Invalid parameter type");
            }

            InGameMainFlowManager.Instance.AddUpdateListener(0, ManagedUpdate);
            await SceneTransition.FadeOutAsync();
        }

        public void ShowSKillTooltip(CharacterStatData getCharacterStat)
        {
            if (getCharacterStat == null) return;
            if (_skillTooltipPopup == null) return;

            var specSkillList = SpecDataManager.Instance.GetSkillDataListByPrefabID(getCharacterStat.Spec.prefab_id);
            if (specSkillList != null && specSkillList.Count > 0)
            {
                var skillData = specSkillList[0];

                _skillTooltipPopup.gameObject.SetActive(true);

                _skillTooltipPopup.SetSkillToolTipPopup(specSkillList);
            }
        }

        public void CloseSkillTooltip()
        {
            _skillTooltipPopup?.gameObject.SetActive(false);
        }

        public void InitCombatStateUI()
        {
            _currentGameStateUI.InitCombatStateUI();
        }

        public void SetInGameTime(float time)
        {
            _inGameTime = time;
        }

        public void SetVignette(int id)
        {
            var data = SoDataProvider.Instance.Get<VignetteSO>();
            var vignette = data.stageColors.FirstOrDefault(x => x.InGameType == _inGameType && x.ID == id);
            _vignetteImage.material = vignette.Material.Asset as Material;
            _vignetteImage.material.SetColor("_DotColor", vignette.Color);
        }

        public void ReturnCharacterUI(CharacterController characterController)
        {
            if (_currentGameStateUI is IReturnCharacterUI returner)
                returner.ReturnCharacterUI(characterController);
        }

        public void SetInGameBottomUIInGuide()
        {
            if (_currentGameStateUI is IGuideBottomUI guide)
                guide.SetInGameBottomUIInGuideUI();
        }

        public void InitReadyStateUI(List<DeckCharacterPlacement> battleDeckList)
        {
            _currentGameStateUI.InitReadyStateUI(battleDeckList);
        }

        public void RefreshInGameTopUI(bool isCombat)
        {
            _currentGameStateUI.RefreshInGameTopUI(isCombat);
        }

        private void ManagedUpdate(float dt)
        {
            _currentGameStateUI.ManagedUpdate(dt);
        }

        public void SetCommanderSkillUI(int index, int equippedCommanderSkillId)
        {
            if (_currentGameStateUI is ICommanderSkillUI cmd)
                cmd.SetCommanderSkillUI(index, equippedCommanderSkillId);
        }

        public void SetFocusSlotUI(CharacterInfo spec)
        {
            if (_currentGameStateUI is IFocusSlotUI focus)
                focus.SetFocusSlotUI(spec);
        }

        public void UnSetFocusSlotUI(bool isDropFx)
        {
            if (_currentGameStateUI is IFocusSlotUI focus)
                focus.UnSetFocusSlotUI(isDropFx);
        }

        public bool IsCheckTouchTile(InGameTile tile)
        {
            return _currentGameStateUI.IsCheckTouchTile(tile);
        }

        public void AddKillLog(object source, CharacterController death, bool isPlayerKill)
        {
            var ks = CookApps.AutoBattler.KillSource.From(source, isPlayerKill);
            if (_currentGameStateUI is IKillLogUI killLog)
                killLog.AddKillLog(ks, death, isPlayerKill);
        }

        public void SetAlertBottomCharacter(int characterID)
        {
            if (_currentGameStateUI is IAlertBottomCharacterUI alert)
                alert.SetAlertBottomCharacter(characterID);
        }

        public void Skip()
        {
            if (_currentGameStateUI is ISkipUI skip)
                skip.OnSkipRequested();
        }
    }
}
