using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 에이프릴
// 범위 : 전방 1칸, 3칸, 5칸, 7칸 
// 효과 : 넓은 범위에 다수의 총기를 꺼내 3초 동안 {0}회 난사하며, 범위별로 다른 대미지를 부여한다. 
//     대미지 : 
// -전방 1칸, 3칸은 회당 공격력 {1}%의 대미지
//     -전방 5칸은 회당 공격력 {2}%의 대미지 
//     -전방 7칸은 회당 공겨력 {3}%의 대미지 
/// </summary>
/// 
[UseEffectCodeIds(217333202)]
public partial class EffectCodeSkill217333202 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _perCount;
    private ObfuscatorFloat _powerRate1;
    private ObfuscatorFloat _powerRate2;
    private ObfuscatorFloat _powerRate3;

    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    
    private float _elapsedTime;
    private float _totalElapsedTime;
    private InGameVfx _vfx;
    private int _count;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _perCount = codeInfo.GetCodeStatToFloat(1);
        _powerRate1 = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _powerRate2 = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _powerRate3 = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _perCount = codeInfo.GetCodeStatToFloat(1);
        _powerRate1 = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _powerRate2 = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _powerRate3 = codeInfo.GetCodeStatToFloat(4) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }
        
        _elapsedTime += dt;

        if (_elapsedTime >= 1f)
        {
            _elapsedTime -= 1f;
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

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        float duration = 3.0f;
        _count = 0;
        
        var inGameTileList = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        if (inGameTileList.Count > 0)
        {
            ProcessTarget(duration, inGameTileList[0]).Forget();
            PlayEffect(duration, inGameTileList[0]).Forget();
        }
    }
    
    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private async UniTask ProcessTarget(float duration, InGameTile tile)
    {
        for (int i = 0; i < _perCount; i++)
        {
            if (owner != null)
            {
                var inGameTiles1 = new List<InGameTile>();
                inGameTiles1.Add(tile);
                var inGameTiles2 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 2, 1);
                var inGameTiles3 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 3, 2);
                var inGameTiles4 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 4, 3);

                // bool isFx = i % (int) duration == 0;
                bool isFx = true;
                
                
                List<int> targetCharacterList = new();
                ProcessTiles(inGameTiles1, owner, _powerRate1 / _perCount, isFx, targetCharacterList);
                ProcessTiles(inGameTiles2, owner, _powerRate1 / _perCount, isFx, targetCharacterList);
                ProcessTiles(inGameTiles3, owner, _powerRate2 / _perCount, isFx, targetCharacterList);
                ProcessTiles(inGameTiles4, owner, _powerRate3 / _perCount, isFx, targetCharacterList);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(duration / _perCount));
        }
    }

    private async UniTask PlayEffect(float duration, InGameTile tile)
    {
        for (int i = 0; i < duration; i++)
        {
            if (owner != null)
            {
                _vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);
                Vector3 direction = (tile.View.CachedTr.position - _vfx.CachedTr.position).normalized;
                _vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
            
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    private void ProcessTiles(List<InGameTile> tiles, CharacterController owner, float powerRate, bool isTileFx, List<int> targetCharacterList)
    {
        foreach (var tile in tiles)
        {
            if (isTileFx)
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);

            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                if (!targetCharacterList.Contains(tile.OccupiedCharacter.CharacterUId))
                {
                    targetCharacterList.Add(tile.OccupiedCharacter.CharacterUId);
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                        tile.OccupiedCharacter.SkillRootTransformFollowable);

                    var damage = owner.CalculateDamageAmount(owner.AD * powerRate, 0, tile.OccupiedCharacter, codeId, true);
                    tile.OccupiedCharacter.GetDamaged(damage, owner);
                }
            }
        }
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}
