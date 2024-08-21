using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommanderSkillData
{
    public SpecCommanderSkill Spec => _spec;

    public float ElapsedTime
    {
        get => _elapsedTime;
        set => _elapsedTime = value;
    }

    public float DurationTime => _durationTime;
    public float StatValue => _statValue;

    private SpecCommanderSkill _spec;
    private ObfuscatorFloat _elapsedTime;
    private ObfuscatorFloat _durationTime;
    private ObfuscatorFloat _statValue;

    public CommanderSkillData(SpecCommanderSkill spec, float durationTime, float statValue)
    {
        _spec = spec;
        _elapsedTime = 0f;
        _durationTime = durationTime;
        _statValue = statValue;
    }
}

public class InGameCommanderManager : GameObjectSingleton<InGameCommanderManager>, IBeginDragHandler, IDragHandler,
    IEndDragHandler
{
    public InGameCamera InGameCamera => _inGameCamera;

    [SerializeField]
    private InGameCamera _inGameCamera;

    //[TODO] switchObj 필요
    public GameObject switchObj;
    public float switchThreshold = 50f;
    public float maxFadeAlpha = 0.9f;

    private InGameCamera _ingameCamera;
    private Camera _mainCamera;
    private bool _isDragging = false;
    private Vector2 _dragStartPosition;
    private InGameTileView _hitTileView;
    private List<InGameTile> _activeTiles = new List<InGameTile>();
    private List<CommanderSkillData> _commandSkillDataList = new List<CommanderSkillData>();
    private CommanderSkillData _selectedCommanderSkillData;
    private bool isCanUseCommanderSkill = false;
    private Vector2 _offset = new Vector2(-10, 10);

    public void Initialize()
    {
        _mainCamera = Camera.main;

        InGameMainFlowManager.Instance.AddUpdateListener(InGameMainFlowManager.UpdatePriority_Objects,
            ManagedUpdate);
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

        _selectedCommanderSkillData = _commandSkillDataList.Find(l => l.Spec.id == commanderSkillUI.Data.Spec.id);

        if (_selectedCommanderSkillData == null)
            return;

        if (_selectedCommanderSkillData.DurationTime > _selectedCommanderSkillData.ElapsedTime)
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

        if (_selectedCommanderSkillData == null)
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
            double[] eccStat = new double[2];
            eccStat[0] = _hitTileView.ID;
            eccStat[1] = (double) _selectedCommanderSkillData.StatValue;
            var effectCodeInfo = new EffectCodeInfo(_selectedCommanderSkillData.Spec.commander_skill_id, 0, eccStat);

            InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeInfo, null);
            _selectedCommanderSkillData.ElapsedTime = 0;
            isCanUseCommanderSkill = false;
            _selectedCommanderSkillData = null;
        }
    }

    public void ManagedUpdate(float dt)
    {
        if (isCanUseCommanderSkill)
            return;

        foreach (var commanderSkillData in _commandSkillDataList)
        {
            commanderSkillData.ElapsedTime += dt;
        }
    }

    Vector3 HandleRuntimeDrag(Vector2 adjustedPosition)
    {
        return _mainCamera.ScreenToWorldPoint(new Vector3(adjustedPosition.x, adjustedPosition.y,
            _mainCamera.nearClipPlane));
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

    public CommanderSkillData InitCommanderSkillData(SpecCommanderSkill data)
    {
        var coolTimeData = SpecDataManager.Instance.GetCommanderSkillData(data.commander_skill_id, SkillValueType.COOL);
        var statValueData =
            SpecDataManager.Instance.GetCommanderSkillData(data.commander_skill_id, SkillValueType.PERCENT);
        if (statValueData == null)
            statValueData =
                SpecDataManager.Instance.GetCommanderSkillData(data.commander_skill_id, SkillValueType.TIME);

        float stat = statValueData?.base_rate ?? 0;

        var commanderSkillData = new CommanderSkillData(data, coolTimeData.base_rate,stat);
        //[TODO] 나중에는 성장 스텟으로 빼기
        commanderSkillData.ElapsedTime = commanderSkillData.DurationTime * 0.5f;
        _commandSkillDataList.Add(commanderSkillData);

        return commanderSkillData;
    }

    public void Clear()
    {
        InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        _selectedCommanderSkillData = null;
        _commandSkillDataList.Clear();
    }

    private bool CheckSkillTile(Vector2 adjustedPosition, bool isNavigate)
    {
        RaycastHit[] hits = Physics.RaycastAll(_mainCamera.ScreenPointToRay(adjustedPosition));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject.CompareTag("Slot"))
            {
                _hitTileView = hit.transform.GetComponent<InGameTileView>();
                if (_hitTileView != null)
                {
                    InGameTile centerTile = InGameObjectManager.Instance.GetInGameTile(_hitTileView.ID);
                    var tiles = new List<InGameTile>();

                    // [TODO] 나중에는 데이터에서 처리 필요
                    if (_selectedCommanderSkillData.Spec.commander_skill_id == 300001)
                        tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByShapeXInRange(centerTile, 2));
                    else if (_selectedCommanderSkillData.Spec.commander_skill_id == 300002)
                        tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(centerTile, 1));
                    else if (_selectedCommanderSkillData.Spec.commander_skill_id == 300004)
                        tiles.AddRange(
                            InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(centerTile, 1));
                    else if (_selectedCommanderSkillData.Spec.commander_skill_id == 300006)
                        tiles.AddRange(
                            InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(centerTile, 1));
                    else
                        tiles.Add(centerTile);

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
        }

        return _hitTileView != null;
    }
}
