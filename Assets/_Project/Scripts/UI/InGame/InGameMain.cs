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
using Random = UnityEngine.Random;

namespace CookApps.AutoBattler
{
    public readonly struct KillSource
    {
        public readonly AttackerType Type;
        public readonly long Id;
        public readonly bool IsPlayerOwned;
        public readonly CharacterController Character; // 선택적 참조(캐릭터만)

        public KillSource(AttackerType type, long id, bool isPlayerOwned, CharacterController character = null)
        {
            Type = type;
            Id = id;
            IsPlayerOwned = isPlayerOwned;
            Character = character;
        }

        public static KillSource From(object source, bool isPlayerOwned)
        {
            switch (source)
            {
                case CharacterController c:
                    return new KillSource(AttackerType.CHARCTER, c.CharacterId, isPlayerOwned, c);
                case SkillCommander s:
                    return new KillSource(AttackerType.COMMANDER_SKILL, s.commander_skill_id, isPlayerOwned);
                case ChapterRule r:
                    return new KillSource(AttackerType.CHAPTER_RULE, r.effect_code_id, isPlayerOwned);
                case SynergyStarAsterism st:
                    return new KillSource(AttackerType.SYNERGY_STAR_ASTERISM, st.synergy_group_id, isPlayerOwned);
                case long id:
                    return new KillSource(AttackerType.CHARCTER, id, isPlayerOwned);
                default:
                    return new KillSource(AttackerType.CHARCTER, 0, isPlayerOwned);
            }
        }
    }

    public interface IGameStateUICore
    {
        UniTask Initialize(Transform canvasTransform, int id);
        void InitReadyStateUI(List<DeckCharacterPlacement> battleDeckList);
        void InitCombatStateUI();
        void RefreshInGameTopUI(bool isCombat);
        void ManagedUpdate(float dt);
        bool IsCheckTouchTile(InGameTile tile);
    }

    // 선택 능력 인터페이스들
    public interface IReturnCharacterUI
    {
        void ReturnCharacterUI(CharacterController characterController);
    }

    public interface ICommanderSkillUI
    {
        void SetCommanderSkillUI(int index, int equippedCommanderSkillId);
    }

    public interface IGuideBottomUI
    {
        void SetInGameBottomUIInGuideUI();
    }

    public interface IFocusSlotUI
    {
        void SetFocusSlotUI(CharacterInfo spec);
        void UnSetFocusSlotUI(bool isDropFx);
    }

    public interface IKillLogUI
    {
        void AddKillLog(in CookApps.AutoBattler.KillSource source, CharacterController death, bool isPlayerKill);
    }

    public interface IAlertBottomCharacterUI
    {
        void SetAlertBottomCharacter(int characterID);
    }

    public interface ISkipUI
    {
        void OnSkipRequested();
    }

    public interface IBottomScrollRectCheck
    {
        bool IsPointInBottomScrollRect(Vector2 screenPosition);
        void SetDropHighlight(bool active);
    }

    public class InGameMainParams
    {
        public InGameType InGameType;
        public IGameStateUICore GameStateUI;
        public int StageId;
        public string SessionId;
        public int RandomSeed;
        
        public InGameMainParams(InGameType inGameType, IGameStateUICore gameStateUI, int stageId)
        {
            InGameType = inGameType;
            GameStateUI = gameStateUI;
            StageId = stageId;
            SessionId = Guid.NewGuid().ToString();
            RandomSeed = Random.Range(int.MinValue, int.MaxValue);
        }
        
        public InGameMainParams(InGameType inGameType, IGameStateUICore gameStateUI, int stageId, string sessionId, ulong randomSeed)
        {
            InGameType = inGameType;
            GameStateUI = gameStateUI;
            StageId = stageId;
            SessionId = sessionId;
            RandomSeed = (int)randomSeed;
        }
    }
    
    public class InGameMain : UILayer
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

        public static InGameMain GetInGameMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameMain>();
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
                case null:
                    throw new ArgumentException("Invalid parameter type: param is null");
                default:
                    throw new ArgumentException($"Invalid parameter type: expected InGameMainParams, got {param.GetType().Name} (value: {param})");
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

        public void ShowEnemySkillTooltip(MonsterInfo monsterInfo)
        {
            if (monsterInfo == null) return;
            if (monsterInfo.skill_ids == null || monsterInfo.skill_ids.Length == 0) return;

            SceneUILayerManager.Instance.PushUILayerAsync<EnemySkillTooltipPopup>(monsterInfo).Forget();
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

        public bool IsPointInBottomScrollRect(Vector2 screenPosition)
        {
            if (_currentGameStateUI is IBottomScrollRectCheck check)
                return check.IsPointInBottomScrollRect(screenPosition);
            return false;
        }

        public void SetDropHighlight(bool active)
        {
            if (_currentGameStateUI is IBottomScrollRectCheck check)
                check.SetDropHighlight(active);
        }
    }
}
