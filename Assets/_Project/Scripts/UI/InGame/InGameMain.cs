using System;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public interface IGameStateUI
    {
        void RefreshInGameTopUI(bool isCombat);
        UniTask Initialize(Transform canvasTransform, int id);
        void ReturnCharacterUI(CharacterController characterController);
        void AddCharacterUI(List<UserCharacterBattleDeck> battleDeckList);
        void SetInGameBottomUIInGuideUI();
        void ManagedUpdate(float dt);
        void SetReadyStateUI();
        void SetFocusSlotUI(SpecCharacter spec);
        void UnSetFocusSlotUI(bool isDropFx);
        void SetCommanderSkillUI(int index, int equippedCommanderSkillId);
    }

    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/InGame/InGameMain.prefab")]
    public class InGameMain : UILayer
    {
        public float InGameTime => _inGameTime;

        [SerializeField] private Transform _canvasTransform;
        [SerializeField] private RawImage _vignetteImage;
        [SerializeField] private Animator _sceneAnimator;

        [SerializeField] private SkillTooltipPopup _skillTooltipPopup;
        [SerializeField] private VignetteSO _vignetteData;

        private float _inGameTime = 0f;
        private IGameStateUI _currentGameStateUI;
        private InGameType _inGameType;

        public static InGameMain GetInGameMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameMain>();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            (InGameType inGameType, IGameStateUI gameState, int id) = ((InGameType, IGameStateUI, int)) param;
            _inGameType = inGameType;
            _currentGameStateUI = gameState;
            _currentGameStateUI.Initialize(_canvasTransform, id).Forget();
            InGameMainFlowManager.Instance.AddUpdateListener(0, ManagedUpdate);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        }

        public void OpenStatisticPop()
        {
            bool isOpenStatisticPop = Preference.LoadPreference(Pref.STATISTIC, false);
            if (isOpenStatisticPop)
                SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(this).Forget();
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

                _skillTooltipPopup.SetSkillToolTipPopup(skillData);
            }
        }

        public void PlaySceneAnimation(string name)
        {
            _sceneAnimator.SetTrigger(name);
        }

        public void CloseSkillTooltip()
        {
            _skillTooltipPopup?.gameObject.SetActive(false);
        }

        public void SetInGameTime(float time)
        {
            _inGameTime = time;
        }

        public void SetVignette(int id)
        {
            var vignette = _vignetteData.stageColors.FirstOrDefault(x => x.InGameType == _inGameType && x.ID == id);
            _vignetteImage.material = vignette.Material;
            _vignetteImage.material.SetColor("_DotColor", vignette.Color);
        }

        public void ReturnCharacter(CharacterController characterController)
        {
            _currentGameStateUI.ReturnCharacterUI(characterController);
        }

        public void SetInGameBottomUIInGuide()
        {
            _currentGameStateUI.SetInGameBottomUIInGuideUI();
        }

        public void SetReadyStateUI()
        {
            _currentGameStateUI.SetReadyStateUI();
        }

        public void AddCharacter(List<UserCharacterBattleDeck> battleDeckList)
        {
            _currentGameStateUI.AddCharacterUI(battleDeckList);
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
            _currentGameStateUI.SetCommanderSkillUI(index, equippedCommanderSkillId);
        }

        public void SetFocusSlotUI(SpecCharacter spec)
        {
            _currentGameStateUI.SetFocusSlotUI(spec);
        }

        public void UnSetFocusSlotUI(bool isDropFx)
        {
            _currentGameStateUI.UnSetFocusSlotUI(isDropFx);
        }
    }
}
