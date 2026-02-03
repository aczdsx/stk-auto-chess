using System;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine.Pool;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;


/// <summary>
/// 아이콘을 위해 buff로 처리
/// 무조건 한개의 버프 스택만 유지한다.
/// </summary>.
/// 시라유키 회피율 및 반격 
/// 반격 이펙트: 0번 = 상하(up/down), 1번 = 좌우(left/right)
/// up   → 0번, 기본(scale/rotation 그대로)
/// down → 0번, rotation·scale 변경
/// left → 1번, rotation·scale 변경
/// right→ 1번, 기본

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffShirayukiAvoidAndAttack : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_SPECIAL_SHIRAYUKI_AVOID_AND_ATTACK;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.ShirayukiAvoidAndAttack;
    public override bool IsNeedToShowIcon => true;


    private int _avoidSuccessMaxCount; // 최대 증가 횟수
    private float _avoidSuccessRatePercent; // 회피율 증가 비율
    private int _currentAvoidSuccessCount; // 현재 증가 횟수
    private float _damageRatePercent; // 반격 데미지 비율
    private InGameVfx _attackVfx; // 슬래시 VFX 인스턴스
    private SkillPassive _specSkillPassive;

    private static readonly Vector3 VfxScaleFlipped = new Vector3(-1f, 1f, 1f);
    private static readonly Vector3 VfxEulerY90 = new Vector3(0f, 90f, 0f);


    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _avoidSuccessRatePercent = codeInfo.GetCodeStatToFloat(1);
        _avoidSuccessMaxCount = codeInfo.GetCodeStatToInt(2);
        _damageRatePercent = codeInfo.GetCodeStatToFloat(3);
        _currentAvoidSuccessCount = 0;

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
        _specSkillPassive = SpecDataManager.Instance.GetSkillPassiveDataList(codeInfo.GetCodeStatToInt(0)).FirstOrDefault();


        _stackDatas.Add(buffStackData);

        owner.AddBuffDebuffType(buffDebuffType);
        owner.AddBuffStackData(CodeId, buffStackData);

    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        int newSourceCodeId = codeInfo.GetCodeStatToInt(0);

        _avoidSuccessRatePercent = codeInfo.GetCodeStatToFloat(1);
        _avoidSuccessMaxCount = codeInfo.GetCodeStatToInt(2);
        _damageRatePercent = codeInfo.GetCodeStatToFloat(3);

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
    public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageInfo, CharacterController attacker, bool isPure)
    {
        // if (damageInfo.isMissed)
        // {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_job_striker_brave);
            var prevAvoidSuccessCount = _currentAvoidSuccessCount;
            ++_currentAvoidSuccessCount;
            _currentAvoidSuccessCount = Math.Min(_currentAvoidSuccessCount, _avoidSuccessMaxCount);


            if (owner.SpecCharacter.atk_type == AtkType.AD)
            {
                var damage = owner.CalculateDamageAmount(attacker.AD * _damageRatePercent, 0, attacker, owner.CharacterId, isSkill: true,
                CharacterController.DamageTestFlags.None);
                attacker.GetDamaged(damage, owner);
            }
            else
            {
                var damage = owner.CalculateDamageAmount(0, attacker.AP * _damageRatePercent, attacker, owner.CharacterId, isSkill: true,
                CharacterController.DamageTestFlags.None);
                attacker.GetDamaged(damage, owner);
            }

            InGameVfx vfx;
            if (attacker == null || attacker.CurrentTile == null)
            {
                if (owner.GetCharacterView().CachedFront)
                    vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkillPassive.passive_skill_vfxs[0], owner.SkillRootTransformFollowable);
                else
                    vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkillPassive.passive_skill_vfxs[1], owner.SkillRootTransformFollowable);
                vfx.CachedTr.localScale = Vector3.one;
                vfx.CachedTr.localEulerAngles = Vector3.zero;
            }
            else
            {
                var ownerIdx = owner.CurrentTile.Int2Index;
                var attackerIdx = attacker.CurrentTile.Int2Index;
                var delta = attackerIdx - ownerIdx;
                bool isUp = delta.y > 0;
                bool isDown = delta.y < 0;
                bool isLeft = delta.x < 0;
                bool isRight = delta.x > 0;

                bool needToFlipAndRotation;
                if (isUp)
                {
                    vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkillPassive.passive_skill_vfxs[1], owner.SkillRootTransformFollowable);
                    needToFlipAndRotation = false;
                }
                else if (isDown)
                {
                    vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkillPassive.passive_skill_vfxs[0], owner.SkillRootTransformFollowable);
                    needToFlipAndRotation = true;
                }
                else if (isLeft)
                {
                    vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkillPassive.passive_skill_vfxs[0], owner.SkillRootTransformFollowable);
                    needToFlipAndRotation = false; 
                }
                else // isRight (또는 delta == 0)
                {
                    vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkillPassive.passive_skill_vfxs[1], owner.SkillRootTransformFollowable);
                    needToFlipAndRotation = true;
                }

                if (needToFlipAndRotation)
                {
                    vfx.CachedTr.localScale = VfxScaleFlipped;
                    vfx.CachedTr.localEulerAngles = VfxEulerY90;
                }
                else
                {
                    vfx.CachedTr.localScale = Vector3.one;
                    vfx.CachedTr.localEulerAngles = Vector3.zero;
                }
            }


            if (prevAvoidSuccessCount != _currentAvoidSuccessCount)
            {

                owner.GetEffectCodeContainer().SetDirtyFlag(this);
                _stackDatas[0].value = _currentAvoidSuccessCount;

                owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
            }
        // }
        return damageInfo;
    }

    public override float GetIncrementPercentAvoidProb()
    {
        return _avoidSuccessRatePercent * _currentAvoidSuccessCount;
    }


    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


}
