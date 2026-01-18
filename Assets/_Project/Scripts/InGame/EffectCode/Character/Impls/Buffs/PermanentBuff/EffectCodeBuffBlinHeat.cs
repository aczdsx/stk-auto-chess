using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine.Pool;

/// <summary>
/// 아이콘을 위해 buff로 처리
/// 무조건 한개의 버프 스택만 유지한다.
/// </summary>.
/// 블린 패시브 버프
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffBlinHeat : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_SPECIAL_BLIN_HEAT;

    private const BuffDebuffType buffDebuffType = BuffDebuffType.BlinHeat;
    public override bool IsNeedToShowIcon => true;


    private int _overheatMaxCount; // 최대 중첩 횟수
    private int _currentOverheatCount; // 현재 중첩 횟수


    private float _overheatDamageRatePercent; // 폭염 데미지 비율
    private float _overheatDuration; // 지속 시간
    private float _overheatDurationElapsedTime; // 지속 시간 경과 시간
    private float _overheatDamageRate; // 지속 데미지 비율
    private float _overheatDamageElapsedTime; // 지속 데미지 경과 시간

    private List<InGameTile> _overHeatTiles = new List<InGameTile>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);



        _overheatMaxCount = codeInfo.GetCodeStatToInt(1);
        _overheatDamageRatePercent = codeInfo.GetCodeStatToFloat(2);
        _overheatDuration = codeInfo.GetCodeStatToFloat(3);
        _overheatDamageRate = codeInfo.GetCodeStatToFloat(4);


        _currentOverheatCount = 0;
        var jobPassiveEsper = owner.GetEffectCodeContainer().GetEffectCode((int)EffectCodeNameType.JOBS_ESPER) as EffectCodeJobPassiveEsper;
        jobPassiveEsper.OnExplosionDamage += OnExplosionDamage;

        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();

        buffStackData.SetData(
        sourceCodeId: codeInfo.GetCodeStatToInt(0),
        duration: 999f,
        value: 0,
        source: source,
        isShowValue: true,
        showPosition: BuffStackData.BuffShowPosition.SIDE
        );

        _stackDatas.Add(buffStackData);

        owner.AddBuffDebuffType(buffDebuffType);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _overheatMaxCount = codeInfo.GetCodeStatToInt(1);
        _overheatDamageRatePercent = codeInfo.GetCodeStatToFloat(2);
        _overheatDuration = codeInfo.GetCodeStatToFloat(3);
        _overheatDamageRate = codeInfo.GetCodeStatToFloat(4);

        int newSourceCodeId = codeInfo.GetCodeStatToInt(0);

        // 같은 source가 있는지 확인
        for (int i = 0; i < _stackDatas.Count; i++)
        {
            if (_stackDatas[i].sourceCodeId == newSourceCodeId)
            {
                // 같은 source가 있으면 덮어쓰기
                var stackData = _stackDatas[i];
                stackData.duration = 999f;
                stackData.value = 0;
                stackData.elapsedTime = 0f;
                stackData.isShowValue = true;
                owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
                return;
            }
        }

        // 같은 source가 없으면 기존 것을 모두 제거하고 새로 하나만 추가
        // 항상 한 개만 유지하기 위해
        for (int i = _stackDatas.Count - 1; i >= 0; i--)
        {
            owner.RemoveBuffStackData(_stackDatas[i]);
            GenericPool<BuffStackData>.Release(_stackDatas[i]);
            _stackDatas.RemoveAt(i);
        }

        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            newSourceCodeId,
            999f,
            0,
            source,
            isShowValue: true
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override void OnUpdate(float dt)
    {
        if (_overHeatTiles.Count == 0)
            return;

        // 지속 시간 경과 시간 증가
        _overheatDurationElapsedTime += dt;

        // 지속 시간이 지나면 불지대 제거
        if (_overheatDurationElapsedTime >= _overheatDuration)
        {
            _overHeatTiles.Clear();
            _overheatDurationElapsedTime = 0f;
            _overheatDamageElapsedTime = 0f;
            return;
        }

        // 지속 데미지 경과 시간 증가
        _overheatDamageElapsedTime += dt;

        // 1초마다 지속 데미지 적용
        if (_overheatDamageElapsedTime >= 0.5f)
        {
            _overheatDamageElapsedTime -= 0.5f;

            foreach (var overHeatTile in _overHeatTiles)
            {
                // 타일 VFX 표시
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, overHeatTile);

                // 타일이 유효하고 적군이 있으면 데미지 적용
                if (overHeatTile.CheckValidTile(owner.AllianceType, false))
                {
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                        overHeatTile.OccupiedCharacter.SkillRootTransformFollowable);

                    // 지속 데미지 계산 (공격력의 _overheatDamageRate%만큼)
                    var defaultDamage = owner.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AP;
                    var damage = owner.CalculateDamageAmount(defaultDamage * _overheatDamageRate, 0,
                        overHeatTile.OccupiedCharacter, codeId, true);

                    overHeatTile.OccupiedCharacter.GetDamaged(damage, owner);
                }
            }
        }
    }

    private void OnExplosionDamage(int damagedTargetCount, InGameTile explosionStartTile)
    {
        _currentOverheatCount += damagedTargetCount;
        _currentOverheatCount = Math.Min(_currentOverheatCount, _overheatMaxCount);
        _stackDatas[0].value = _currentOverheatCount;

        if (_currentOverheatCount < _overheatMaxCount)
        {
            owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
            return;
        }
        owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
        // 열기 중첩이 최대치에 도달하면 폭염 발동
        _overHeatTiles.Clear();
        _currentOverheatCount = 0;
        _stackDatas[0].value = _currentOverheatCount;
        _overHeatTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(explosionStartTile, 1);
    }

    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


}
