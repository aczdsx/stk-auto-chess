using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

public class CommanderSkillInGameData
{
    public SkillCommander Spec => _spec;

    public float ElapsedTime
    {
        get => _elapsedTime;
        set => _elapsedTime = value;
    }

    public float DurationTime => _spec.cool_time;
    private SkillCommander _spec;
    private ObfuscatorFloat _elapsedTime;

    public CommanderSkillInGameData(SkillCommander spec)
    {
        _spec = spec;
        _elapsedTime = 0f;
    }
}

public class InGameCommanderManager : SingletonMonoBehaviour<InGameCommanderManager>, IBeginDragHandler, IDragHandler,
    IEndDragHandler
{
    //[TODO] switchObj 필요
    public GameObject switchObj;
    public float switchThreshold = 50f;
    public float maxFadeAlpha = 0.9f;
    private bool _isDragging = false;
    private Vector2 _dragStartPosition;
    private InGameTileView _hitTileView;
    private List<InGameTile> _activeTiles = new List<InGameTile>();
    private List<CommanderSkillInGameData> _commandSkillDataList = new List<CommanderSkillInGameData>();
    private CommanderSkillInGameData selectedCommanderSkillData;
    private bool isCanUseCommanderSkill = false;
    private Vector2 _offset = new Vector2(-10, 10);

    // private bool _isCommanderGuideStage;

    private Dictionary<int, EffectCodeCommanderSkillBase> _effectCodeDictForAutoSkill = null;

    protected override void Awake()
    {
        base.Awake();
        ObjectRegistry.Registered += RegisterCommanderSkillTrail;
        ObjectRegistry.Unregistered += UnregisterCommanderSkillTrail;
    }
    public void Initialize()
    {
        InGameMainFlowManager.Instance.AddUpdateListener(InGameMainFlowManager.UpdatePriority_Objects,
            ManagedUpdate);

        var guideMission = ServerDataManager.Instance.GuideMission;
        // _isCommanderGuideStage = guideMission.GuideMissionId == 18;
        if (_effectCodeDictForAutoSkill == null)
            _effectCodeDictForAutoSkill = new Dictionary<int, EffectCodeCommanderSkillBase>();
        else
            _effectCodeDictForAutoSkill.Clear();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ObjectRegistry.Registered -= RegisterCommanderSkillTrail;
        ObjectRegistry.Unregistered -= UnregisterCommanderSkillTrail;
        _effectCodeDictForAutoSkill.Clear();
        _effectCodeDictForAutoSkill = null;
    }
    private void RegisterCommanderSkillTrail(RegistryKey key, IRegistrable obj)
    {
        if (key != RegistryKey.CommanderSkillTrail)
            return;

        switchObj = (obj as RegisteredObject)?.gameObject;
    }

    private void UnregisterCommanderSkillTrail(RegistryKey key, IRegistrable obj)
    {
        if (key != RegistryKey.CommanderSkillTrail)
            return;

        switchObj = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
            return;

        if (eventData.pointerCurrentRaycast.gameObject == null)
            return;

        Vector2 adjustedPosition = eventData.pointerCurrentRaycast.screenPosition + _offset;

        RaycastResult newRaycastResult = eventData.pointerCurrentRaycast;
        newRaycastResult.screenPosition = adjustedPosition;

        if (!newRaycastResult.gameObject.TryGetComponent<CommanderSkillUI>(out var commanderSkillUI))
            return;

        selectedCommanderSkillData = _commandSkillDataList.Find(l => l.Spec.id == commanderSkillUI.Data.Spec.id);

        if (selectedCommanderSkillData == null)
            return;

        if (selectedCommanderSkillData.DurationTime > selectedCommanderSkillData.ElapsedTime)
            return;

        _isDragging = true;
        _dragStartPosition = eventData.position;

        InGameMainFlowManager.Instance.SetPlaySpeed(0.1f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            Vector2 adjustedPosition = eventData.position + _offset;

            float distance = Vector2.Distance(adjustedPosition, _dragStartPosition);

            float normalizedDistance = Mathf.Clamp01(distance / switchThreshold);
            float fadeAlpha = Mathf.Lerp(0f, maxFadeAlpha, normalizedDistance);

            if (distance >= switchThreshold)
            {
                Vector3 worldPos = HandleRuntimeDrag(adjustedPosition);
                if (switchObj != null)
                {
                    switchObj.SetActive(true);
                    switchObj.transform.position = worldPos;
                }

                CheckSkillTile(adjustedPosition, true);
            }
            else
            {
                ClearAndSetActive(null);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        if (selectedCommanderSkillData == null)
            return;

        Vector2 adjustedPosition = eventData.position + _offset;

        bool isSpeedUp = Preference.LoadPreference(Pref.IS_SPEED_UP, false);
        InGameMainFlowManager.Instance.SetInGameSpeed(isSpeedUp);

        if (switchObj)
            switchObj.SetActive(false);
        _isDragging = false;

        _hitTileView = null;
        ClearAndSetActive(null);

        if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
            return;

        if (_hitTileView == null)
        {
            bool isHitTileView = CheckSkillTile(adjustedPosition, false);
            if (!isHitTileView)
            {
                // 타일을 다시 설정해주세요.
                ToastManager.Instance.ShowToastByTokenKey("MSG_ALERT_COMMAND_SKILL_COORDINATE");
                return;
            }
        }

        float distance = Vector2.Distance(eventData.position, _dragStartPosition);
        if (distance >= switchThreshold)
        {
            var effectCodeInfo = EffectCodeCommanderSkillBase.GenerateEffectCodeInfo(selectedCommanderSkillData.Spec, _hitTileView);

            InGameManager.Instance.TeamEcc.AddOrMergeEffectCode(effectCodeInfo, null);

            selectedCommanderSkillData.ElapsedTime = 0;
            isCanUseCommanderSkill = false;
            selectedCommanderSkillData = null;
        }
    }

    public void ManagedUpdate(float dt)
    {
        if (isCanUseCommanderSkill)
            return;

        for (int i = 0; i < _commandSkillDataList.Count; i++)
        {
            _commandSkillDataList[i].ElapsedTime += dt;

            if (_commandSkillDataList[i].ElapsedTime >= _commandSkillDataList[i].DurationTime)
            {
                string preferenceKey = $"COMMANDER_AUTO_{(int)(i + 1)}";

                bool isActiveAuto = false;
                if (Enum.TryParse(preferenceKey, out Pref prefEnum))
                    isActiveAuto = Preference.LoadPreference(prefEnum, false);

                if (isActiveAuto)
                {
                    AutoSkill(_commandSkillDataList[i]);
                    return;
                }

                // if (_isCommanderGuideStage)
                // {
                //     InGameMainFlowManager.Instance.SetPlaySpeed(0.1f);
                //     ToastManager.Instance.ShowToastByTokenKey("MSG_FIRST_COMMANDER_SKILL");
                // }
            }
        }
    }

    Vector3 HandleRuntimeDrag(Vector2 adjustedPosition)
    {
        return MainCameraHolder.MainCamera.ScreenToWorldPoint(new Vector3(adjustedPosition.x, adjustedPosition.y,
            MainCameraHolder.MainCamera.nearClipPlane));
    }

    private void ClearAndSetActive(IEnumerable<InGameTile> newTiles)
    {
        foreach (var tile in _activeTiles)
        {
            tile.View.SetNavigateObj(false);
        }

        _activeTiles.Clear();

        if (newTiles != null)
        {
            foreach (var tile in newTiles)
            {
                tile.View.SetNavigateObj(true);
                _activeTiles.Add(tile);
            }
        }
    }

    public CommanderSkillInGameData InitCommanderSkillData(SkillCommander data)
    {
        var commandSkillData = new CommanderSkillInGameData(data);
        commandSkillData.ElapsedTime = commandSkillData.DurationTime * 0.5f;
        _commandSkillDataList.Add(commandSkillData);
        return commandSkillData;
    }

    public void Clear()
    {
        InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        selectedCommanderSkillData = null;
        _commandSkillDataList.Clear();
    }

    private bool CheckSkillTile(Vector2 adjustedPosition, bool isNavigate)
    {
        RaycastHit[] hits = Physics.RaycastAll(MainCameraHolder.MainCamera.ScreenPointToRay(adjustedPosition));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject.CompareTag("Slot"))
            {
                _hitTileView = hit.transform.GetComponent<InGameTileView>();
                if (_hitTileView == null)
                {
                    continue;
                }

                InGameTile centerTile = InGameObjectManager.Instance.GetInGameTile(_hitTileView.ID);
                var tiles = new List<InGameTile>();
                var targetCommanderSkillData = selectedCommanderSkillData.Spec;
                switch (targetCommanderSkillData.commander_range_shape_type)
                {
                    // case CommanderRangeShapeType.SHAPE_X:
                    //     tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(centerTile, targetCommanderSkillData.commander_range_size / 2));
                    //     break;
                    case CommanderRangeShapeType.SHAPE_SQUARE:
                        tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(centerTile, targetCommanderSkillData.commander_range_size - 2));
                        break;
                    case CommanderRangeShapeType.SHAPE_GARO:
                        tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByColumn(centerTile, targetCommanderSkillData.commander_range_size / 2));
                        break;
                    case CommanderRangeShapeType.SHAPE_SERO:
                        tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByRow(centerTile, targetCommanderSkillData.commander_range_size / 2));
                        break;
                    case CommanderRangeShapeType.SHAPE_PLUS:
                        tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByShapePlusInRange(centerTile, targetCommanderSkillData.commander_range_size / 2));
                        break;
                    case CommanderRangeShapeType.SHAPE_SINGLE:
                        tiles.Add(centerTile);
                        break;
                }

                if (isNavigate)
                    ClearAndSetActive(tiles);
                if (centerTile.OccupiedCharacter != null &&
                    centerTile.OccupiedCharacter.AllianceType != AllianceType.Wall)
                {
                    Debug.LogColor(
                        $"충돌한 오브젝트 : {centerTile.View.ID} ({centerTile.X}, {centerTile.Y}) Occupied :({centerTile.OccupiedCharacter.CharacterId})");
                }
                else
                {
                    Debug.LogColor($"충돌한 오브젝트 : {centerTile.View.ID} ({centerTile.X}, {centerTile.Y})");
                }

            }
        }

        return _hitTileView != null;
    }

    private void AutoSkill(CommanderSkillInGameData commanderSkillData)
    {
        if (_effectCodeDictForAutoSkill == null)
        {
            _effectCodeDictForAutoSkill = new Dictionary<int, EffectCodeCommanderSkillBase>();
        }

        //해당 커맨드스킬이 없다면 생성한다.
        if (!_effectCodeDictForAutoSkill.ContainsKey(commanderSkillData.Spec.commander_skill_id))
        {
            var effectCode = EffectCodePoolManager.Instance.Get(commanderSkillData.Spec.commander_skill_id);
            if (effectCode == null)
            {
                Debug.LogError($"EffectCodeCommanderSkillBase is not found : {commanderSkillData.Spec.commander_skill_id}");
                return;
            }
            _effectCodeDictForAutoSkill.Add(commanderSkillData.Spec.commander_skill_id, effectCode as EffectCodeCommanderSkillBase);
        }

        var effectCodeCommanderSkill = _effectCodeDictForAutoSkill[commanderSkillData.Spec.commander_skill_id];
        var recommendedTile = effectCodeCommanderSkill.GetRecommendedTile(commanderSkillData.Spec);
        if (recommendedTile == null)
        {
            Debug.LogError($"Recommended tiles are not found : {commanderSkillData.Spec.commander_skill_id}");
            return;
        }

        var hitTileView = recommendedTile.View;
        if (hitTileView)
        {
            isCanUseCommanderSkill = true;
            var effectCodeInfo = EffectCodeCommanderSkillBase.GenerateEffectCodeInfo(commanderSkillData.Spec, hitTileView);
            InGameManager.Instance.TeamEcc.AddOrMergeEffectCode(effectCodeInfo, null);
            commanderSkillData.ElapsedTime = 0;
        }

        isCanUseCommanderSkill = false;
    }
}
