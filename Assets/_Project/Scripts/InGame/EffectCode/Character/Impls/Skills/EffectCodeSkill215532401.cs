using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 필리아
// 대상 : 가장 가까운 적
// 대미지 : 샷건을 발사해 필리아 공격력 {0}%의 대미지를 가한다.
//     특수 효과 : 스킬로 적을 사망 시켰을 시, 스킬 쿨타임이 즉시 초기화된다.
/// </summary>
[UseEffectCodeIds(215532401)]
public partial class EffectCodeSkill215532401 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

    private bool _isReadyToActivate;

    private SkillActive _specSkill;

    private bool isKilled;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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

        owner.Target = InGameObjectManager.Instance.GetOptimalAttackTarget(owner);

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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (owner.Target == null)
            return;

        InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, owner.Target.CurrentTile);
        var directionTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        if (directionTile.Count > 0)
        {
            var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);
            Vector3 direction = (directionTile[0].View.CachedTr.position - vfx.CachedTr.position).normalized;
            vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                owner.Target.SkillRootTransformFollowable);

            var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, owner.Target, codeId, true);
            // var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, owner.Target, codeId, true);
            // owner.PostCalculateDamageAmount(ref damage, owner.Target);
            var type = owner.Target.GetDamaged(damage, owner);

            if (type == DamageReturnType.Killed)
            {
                owner.ShowNormalText("INGAME_UI_BUFF_COOLDOWN_RESET").Forget();
                isKilled = true;
            }
        }

        IsSkillActivated = false;


    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }



    public override void OnSkillAnimationEnd()
    {
        if (isKilled)
        {
            CoolTimeElapsedTime = CoolTimeDurationTime;
            _isReadyToActivate = true;
            isKilled = false;
        }
        else
        {
            CoolTimeElapsedTime = 0.0f;
        }
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
    }
}
