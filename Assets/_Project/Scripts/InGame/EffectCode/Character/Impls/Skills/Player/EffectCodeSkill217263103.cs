using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using UnityEngine;

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

    private const int _maxFoxFireCount = 9;
    private float _foxFireDuration; // 각 foxfire의 지속 시간 (이건 패시브에서 부여한다.)
    public void SetFoxFireDuration(float duration)
    {
        _foxFireDuration = duration;
    }

    private int _increaseFoxFireCount;//증분량
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _attackSpeedIncreaseRate;

    private InGameVfx _tailVfx;

    private List<GameObject> _tailGameObjects = new();
    private List<ParticleSystem> _tailParticleSystems = new(); // ParticleSystem 컴포넌트 캐싱
    private List<float> _foxFireTimers = new List<float>(); // 각 foxfire의 개별 타이머
    private int _previousActiveCount = -1; // 이전 활성화된 개수 추적

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
            var childObj = _tailVfx.CachedTr.GetChild(i).gameObject;
            _tailGameObjects.Add(childObj);
            
            // ParticleSystem 컴포넌트 캐싱
            var particleSystem = childObj.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                _tailParticleSystems.Add(particleSystem);
                // 초기에는 모두 비활성화 상태
                childObj.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"ParticleSystem not found on {childObj.name}");
                _tailParticleSystems.Add(null);
            }
        }
        
        _ownedFoxFireCount = 0;
        _previousActiveCount = 0;
        _foxFireTimers.Clear();
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
        if (_foxFireTimers.Count == 0)
        {
            return;
        }

        // 각 foxfire의 타이머 업데이트 및 만료된 fire 제거
        for (int i = _foxFireTimers.Count - 1; i >= 0; i--)
        {
            _foxFireTimers[i] += dt;
            
            // 5초가 지난 fire 제거
            if (_foxFireTimers[i] >= _foxFireDuration)
            {
                _foxFireTimers.RemoveAt(i);
            }
        }

        // 활성화된 fire 개수 업데이트
        _ownedFoxFireCount = _foxFireTimers.Count;
        
        // VFX 업데이트
        UpdateFoxFireVisuals();
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
        // 모든 파티클 정지
        for (int i = 0; i < _tailParticleSystems.Count; i++)
        {
            if (_tailParticleSystems[i] != null)
            {
                _tailParticleSystems[i].Stop();
            }
        }
        
        _tailVfx?.Remove();
        _tailVfx = null;
        _tailGameObjects.Clear();
        _tailParticleSystems.Clear();

        base.OnPreRemoved();
    }
    public void AddFoxFire(int increaseCnt)
    {
        int currentCount = _foxFireTimers.Count;
        int totalAfterAdd = currentCount + increaseCnt;
        
        // 추가 후 개수가 최대값을 초과하면 초과된 개수만큼 가장 오래된 fire 제거
        if (totalAfterAdd > _maxFoxFireCount)
        {
            int excessCount = totalAfterAdd - _maxFoxFireCount;
            
            // 타이머가 가장 큰(가장 오래된) fire부터 제거
            // 리스트를 정렬하지 않고 직접 찾아서 제거
            for (int removeCount = 0; removeCount < excessCount && _foxFireTimers.Count > 0; removeCount++)
            {
                // 가장 오래된 fire 찾기 (타이머 값이 가장 큰 것)
                int oldestIndex = 0;
                float oldestTime = _foxFireTimers[0];
                for (int i = 1; i < _foxFireTimers.Count; i++)
                {
                    if (_foxFireTimers[i] > oldestTime)
                    {
                        oldestTime = _foxFireTimers[i];
                        oldestIndex = i;
                    }
                }
                
                // 가장 오래된 fire 제거
                _foxFireTimers.RemoveAt(oldestIndex);
            }
        }
        
        // 새로운 fire 추가 (각 fire마다 개별 타이머 시작)
        for (int i = 0; i < increaseCnt; i++)
        {
            if (_foxFireTimers.Count < _maxFoxFireCount)
            {
                _foxFireTimers.Add(0f); // 새 fire의 타이머를 0으로 시작
            }
        }

        _ownedFoxFireCount = _foxFireTimers.Count;
        
        // VFX 업데이트
        UpdateFoxFireVisuals();
    }
    
    private void UpdateFoxFireVisuals()
    {
        // 활성화 개수가 변경되지 않았으면 업데이트 불필요
        if (_previousActiveCount == _ownedFoxFireCount)
            return;

        // 이전에 활성화되었던 것들 중 비활성화해야 할 것들 처리
        for (int i = _ownedFoxFireCount; i < _previousActiveCount; i++)
        {
            if (i < _tailGameObjects.Count && _tailParticleSystems[i] != null)
            {
                _tailParticleSystems[i].Stop();
                _tailGameObjects[i].SetActive(false);
            }
        }

        // 새로 활성화해야 할 것들 처리
        for (int i = _previousActiveCount; i < _ownedFoxFireCount; i++)
        {
            if (i < _tailGameObjects.Count && _tailParticleSystems[i] != null)
            {
                _tailGameObjects[i].SetActive(true);
                _tailParticleSystems[i].Play();
            }
        }

        _previousActiveCount = _ownedFoxFireCount;
    }
    public int GetCurrentFoxFireCount()
    {
        return _ownedFoxFireCount;
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
