using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;
using System;
using Unity.VisualScripting;

/// <summary>
/// 루키다
// 대상: 자기 자신
// 재사용 시간: {0}초
// 효과: 즉시 여우불을 {1}개 만큼 획득합니다. {2}초간 보유한 여우불의 갯수당 공격속도가 {3}% 만큼 증가합니다.
/// </summary>
[UseEffectCodeIds(217263103)]
public partial class EffectCodeSkill217263103 : EffectCodeCharacterBase
{
    private int _ownedFoxFireCount;

    private int _maxFoxFireCount = 9;

    private int _increaseFoxFireCount;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _attackSpeedIncreaseRate;

    private InGameVfx _tailVfx;

    private List<GameObject> _tailGameObjects = new();


    private bool _isReadyToActivate;
    private SkillActive _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _increaseFoxFireCount = codeInfo.GetCodeStatToInt(1);
        _buffTime = codeInfo.GetCodeStatToFloat(2);
        _attackSpeedIncreaseRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();

        _tailVfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillMiddleFXTransformFollowable);
        for (int i = 0; i < _tailVfx.CachedTr.childCount; i++)
        {
            _tailGameObjects.Add(_tailVfx.CachedTr.GetChild(i).gameObject);
            _tailGameObjects[i].GetComponent<ParticleSystem>().Play();
            _tailGameObjects[i].SetActive(false);
        }
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _increaseFoxFireCount = codeInfo.GetCodeStatToInt(1);
        _attackSpeedIncreaseRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }

        // target check
        if (false)
        {
            owner.AddNextState<CharacterStateIdle>();
            CoolTimeElapsedTime = CoolTimeDurationTime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || IsSkillActivated)
            return;
        CoolTimeElapsedTime += dt;
        if (CoolTimeElapsedTime >= CoolTimeDurationTime)
        {
            _isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return _isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();

        var isInRange = InGameObjectManager.Instance.IsInRange(owner, owner.Target);
        if (!isInRange)
        {
            if (owner.Target != null)
            {
                InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(owner.CurrentTile,
                    owner.Target.CurrentTile);
                owner.MoveTile(bestTile);
            }
            return;
        }

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        // InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
        //     owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        AddFoxFire(_increaseFoxFireCount);
        IncreaseAttackSpeed();

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }



    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }

    public override void OnPreRemoved()
    {
        _tailVfx.Remove();
        _tailVfx = null;
        _tailGameObjects.Clear();

        base.OnPreRemoved();
    }
    private void AddFoxFire(int increaseCnt)
    {
        _ownedFoxFireCount += increaseCnt;
        Math.Min(_ownedFoxFireCount, _maxFoxFireCount);

        for (int i = 0; i < _tailGameObjects.Count; i++)
        {
            if (i < _ownedFoxFireCount)
            {
                _tailGameObjects[i].SetActive(true);
            }
            else
            {
                _tailGameObjects[i].SetActive(false);
            }
        }
    }

    private void IncreaseAttackSpeed()
    {

        double[] eccStats = new double[3];
        eccStats[0] = codeId;
        eccStats[1] = _buffTime;
        eccStats[2] = _attackSpeedIncreaseRate.Value * _ownedFoxFireCount;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_ATK_SPEED_UP, owner, eccStats, source);
    }
    


}
