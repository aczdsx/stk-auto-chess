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
    // [TODO] switchObj 추가 필요
    public GameObject switchObj;
    public float switchThreshold = 40f;
    public float maxFadeAlpha = 0.5f;

    private InGameCamera _ingameCamera;
    private Camera _mainCamera;
    private bool _isDragging = false;
    private Vector2 _dragStartPosition;
    private InGameTileView _hitTileView;
    private List<InGameTile> _activeTiles = new List<InGameTile>();
    private CommanderSkillData _commanderSkillData;

    public void Initialize()
    {
        _mainCamera = Camera.main;

        InGameMainFlowManager.Instance.AddUpdateListener(InGameMainFlowManager.UpdatePriority_Objects,
            ManagedUpdate);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat))
            return;

        var isCommanderSkillTouch = eventData.pointerCurrentRaycast.gameObject.CompareTag("CommanderSkill");
        if (!isCommanderSkillTouch)
            return;

        if (_commanderSkillData == null)
            return;

        if (_commanderSkillData.DurationTime > _commanderSkillData.ElapsedTime)
            return;

        _isDragging = true;
        _dragStartPosition = eventData.position;

        InGameMainFlowManager.Instance.SetPlaySpeed(0.1f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            float distance = Vector2.Distance(eventData.position, _dragStartPosition);
            if (distance < switchThreshold)
            {
                // 거리에 따른 알파값 계산
                float normalizedDistance = Mathf.Clamp01(distance / switchThreshold);
                float fadeAlpha = Mathf.Lerp(0f, maxFadeAlpha, normalizedDistance);
                InGameMain.GetInGameMain().SetIconColor(fadeAlpha);
            }
            else
            {
                Vector3 worldPos = HandleRuntimeDrag(eventData);
                if (switchObj != null)
                {
                    switchObj.SetActive(true);
                    switchObj.transform.position = worldPos;
                }

                if (Physics.Raycast(_mainCamera.ScreenPointToRay(eventData.position), out RaycastHit hit))
                {
                    _hitTileView = hit.transform.GetComponent<InGameTileView>();
                    if (_hitTileView != null)
                    {
                        InGameTile centerTile = InGameObjectManager.Instance.GetInGameTile(_hitTileView.ID);
                        var tiles = new List<InGameTile>();

                        // [TODO] 나중에는 데이터에서 처리 필요
                        if (_commanderSkillData.Spec.commander_skill_id == 300001)
                            tiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(centerTile));
                        else
                            tiles.AddRange(
                                InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(centerTile, 1));

                        ClearAndSetActive(tiles);
                        if (centerTile.OccupiedCharacter != null &&
                            centerTile.OccupiedCharacter.AllianceType != AllianceType.None)
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
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InGameMainFlowManager.Instance.SetPlaySpeed(1.0f);
        if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat))
            return;

        if (!_isDragging)
            return;

        if (_hitTileView == null)
        {
            // 타일을 다시 설정해주세요.
            ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_CHAR_EXP");
            return;
        }

        if (switchObj)
            switchObj.SetActive(false);
        _isDragging = false;
        ClearAndSetActive(null);

        double[] eccStat = new double[2];
        eccStat[0] = _hitTileView.ID;
        eccStat[1] = (double) _commanderSkillData.StatValue;
        var effectCodeInfo = new EffectCodeInfo(_commanderSkillData.Spec.commander_skill_id, 0, eccStat);

        InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeInfo, null);
        _commanderSkillData.ElapsedTime = 0;
    }

    public void ManagedUpdate(float dt)
    {
        if (_commanderSkillData == null)
            return;

        if (_commanderSkillData.ElapsedTime < _commanderSkillData.DurationTime)
        {
            _commanderSkillData.ElapsedTime += dt;
            InGameMain.GetInGameMain()
                .SetCommanderSkillCoolTime(_commanderSkillData.ElapsedTime, _commanderSkillData.DurationTime);
        }
    }

    Vector3 HandleRuntimeDrag(PointerEventData eventData)
    {
        return _mainCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y,
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

    public void SetCommanderSkillData(SpecCommanderSkill data)
    {
        var coolTimeData = SpecDataManager.Instance.GetCommanderSkillData(data.commander_skill_id, SkillValueType.COOL);
        var statValueData =
            SpecDataManager.Instance.GetCommanderSkillData(data.commander_skill_id, SkillValueType.PERCENT);

        _commanderSkillData = new CommanderSkillData(data, coolTimeData.base_rate, statValueData.base_rate);
    }

    public void Clear()
    {
        InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        _commanderSkillData = null;
    }
}
