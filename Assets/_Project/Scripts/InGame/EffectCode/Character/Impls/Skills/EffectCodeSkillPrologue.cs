using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using System;
using UnityEngine;
using CookApps.Obfuscator;
using Unity.Mathematics;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler.Prologue
{
    /// <summary>
    /// 프롤로그_유니
    /// </summary>
    [UseEffectCodeIds(295250010)]
    public partial class EffectCodeSkill295250010 : EffectCodeCharacterBase
    {

        private float _healRate;
        private int _ccRigidCount;

        private bool _isReadyToActivate;
        private SkillActive _specSkill;

        private int _targetCount = 99;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            SkillIndex = 1;
            CoolTimeElapsedTime = 0f;
            CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
            _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _ccRigidCount = codeInfo.GetCodeStatToInt(2);

            _isReadyToActivate = false;
            IsSkillActivated = false;

            _specSkill = SpecDataManager.Instance.GetSkillDataList((int)codeId).First();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
            _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _ccRigidCount = codeInfo.GetCodeStatToInt(2);
        }

        public override void OnUpdate(float dt)
        {
            if (!IsSkillActivated)
            {
                return;
            }

            // target check
            {
                if (false)
                    owner.AddNextState<CharacterStateIdle>();
                CoolTimeElapsedTime = CoolTimeDurationTime;
            }
        }

        public override void OnCooltime(float dt)
        {
            if (_isReadyToActivate || IsSkillActivated)
            {
                return;
            }

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
            // TODO: Target Check
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
            {
                return;
            }
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_306011);
            // 나한테 붙은 vfx
            // InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], owner.SkillRootTransformFollowable);

            var targetCharacters = InGameObjectManager.Instance.GetCharacterList(owner.AllianceType);
            if (targetCharacters.Count > 0)
            {
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillMiddleFXTransformFollowable);

                for (int i = 0; i < _targetCount; i++)
                {
                    if (i >= targetCharacters.Count)
                        break;

                    InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type,
                        targetCharacters[i].CurrentTile);

                    InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], targetCharacters[i].SkillMiddleFXTransformFollowable);

                    double healAmount = owner.PostCalculateHealAmount(owner.AP * _healRate, targetCharacters[i], isSkill: true);
                    targetCharacters[i].GetHealed(healAmount, owner, codeId, true);


                    RemoveDebuffs(targetCharacters[i]);
                }
            }

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

        private void RemoveDebuffs(CharacterController targetCharacter)
        {
            var debuffs = targetCharacter.GetEffectCodeContainer().GetEffectCodesByType(EffectCodeType.Debuff);
            if (debuffs.Count > 0)
            {
                for (int i = 0; i < _ccRigidCount; i++)
                {
                    targetCharacter.GetEffectCodeContainer().RemoveEffectCode(debuffs[i].CodeId);
                }
            }
        }



    }




    /// <summary>
    /// 프롤로그_필리아
    /// </summary>
    [UseEffectCodeIds(295530011)]
    public partial class EffectCodeSkill295530011 : EffectCodeCharacterBase
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

            owner.Target = InGameObjectManager.Instance.GetNearestTargetOnce(owner);

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
            owner.GetCharacterView().LookAt(owner.CurrentTile, owner.Target.CurrentTile);

            owner.AddNextState<CharacterStateSkill>(this);

            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.Target.SkillRootTransformFollowable);
            InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
                owner.GetCharacterView().CachedTr.position);
        }

        public override void OnSkillExecute(int executeIndex, int totalLength)
        {
            base.OnSkillExecute(executeIndex, totalLength);

            if (owner.Target == null)
                return;
            //shooot effect
            var shootEffect = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], owner.SkillMiddleFXTransformFollowable);

            var direction = (owner.Target.CurrentTile.View.CachedTr.position - owner.CurrentTile.View.CachedTr.position).normalized;
            shootEffect.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            //hit effect
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[2], owner.Target.SkillMiddleFXTransformFollowable);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_304021);

            var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, owner.Target, codeId, true);
            owner.Target.GetDamaged(damage, owner);

            IsSkillActivated = false;

        }


        virtual protected void OnReachedTargetProcess()
        {
            if (owner == null || owner.Target == null || !owner.Target.IsAlive)
                return;
            var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, owner.Target, codeId, true);
            var type = owner.Target.GetDamaged(damage, owner);

            if (type == DamageReturnType.Killed)
            {
                isKilled = true;
            }
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



    /// <summary>
    /// 프롤로그_아트레시아
    /// </summary>
    [UseEffectCodeIds(297510012)]
    public partial class EffectCodeSkill297510012 : EffectCodeCharacterBase
    {
        private ObfuscatorFloat _powerRate;

        private bool _isReadyToActivate;

        private List<CharacterController> _hitCharacters = new List<CharacterController>();

        private WeakReference<InGameVfx> _vfx;

        private SkillActive _specSkill;

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
            // TODO: Target Check
            _hitCharacters.Clear();
            _isReadyToActivate = false;
            IsSkillActivated = true;
            owner.AddNextState<CharacterStateSkill>(this);
            InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
                owner.GetCharacterView().CachedTr.position);
        }

        public override void OnSkillExecute(int executeIndex, int totalLength)
        {
            base.OnSkillExecute(executeIndex, totalLength);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_401011);
            var inGameTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);


            if (inGameTile.Count > 0)
            {
                // InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);

                var vfxProjectile =
                    InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1],
                        owner.CurrentTile.View.CachedTr.position);

                var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
                Vector3 direction = (inGameTile[0].View.CachedTr.position - vfxProjectile.CachedTr.position).normalized;
                vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

                movement.SetData(vfxProjectile.CachedTr.position, inGameTile[0].View.CachedTr.position, 15);
                vfxProjectile.Initialize(false, movement);
                vfxProjectile.OnCollisionWithTile += OnCollision2DEnter;
                // movement.OnReachedTarget +=
            }
        }

        private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
        {
            // 타일 FX는 있으면 표시
            var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            if (tileFx != null)
            {
                tileFx.CachedTr.position = tile.View.CachedTr.position;
            }

            // 피격/데미지 로직은 tileFx와 무관하게 실행
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                if (!_hitCharacters.Exists(l => l == tile.OccupiedCharacter))
                {
                    InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                        tile.OccupiedCharacter.SkillRootTransformFollowable);

                    var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter, codeId, true);

                    tile.OccupiedCharacter.GetDamaged(damage, owner);
                    

                    _hitCharacters.Add(tile.OccupiedCharacter);
                }
            }
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

    }



    [UseEffectCodeIds(297510112)]
    public partial class EffectCodeSkill297510112 : EffectCodeCharacterBase
    {
        private ObfuscatorFloat _powerRate;

        private bool _isReadyToActivate;

        private List<CharacterController> _hitCharacters = new List<CharacterController>();

        private WeakReference<InGameVfx> _vfx;

        private SkillActive _specSkill;

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
            // TODO: Target Check
            _hitCharacters.Clear();
            _isReadyToActivate = false;
            IsSkillActivated = true;
            owner.AddNextState<CharacterStateSkill>(this);
            InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
                owner.GetCharacterView().CachedTr.position);
        }

        public override void OnSkillExecute(int executeIndex, int totalLength)
        {
            base.OnSkillExecute(executeIndex, totalLength);

            var inGameTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_401011);

            if (inGameTile.Count > 0)
            {
                // InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);

                var vfxProjectile =
                    InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1],
                        owner.CurrentTile.View.CachedTr.position);

                var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
                Vector3 direction = (inGameTile[0].View.CachedTr.position - vfxProjectile.CachedTr.position).normalized;
                vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

                movement.SetData(vfxProjectile.CachedTr.position, inGameTile[0].View.CachedTr.position, 15);
                vfxProjectile.Initialize(false, movement);
                vfxProjectile.OnCollisionWithTile += OnCollision2DEnter;
                // movement.OnReachedTarget +=
            }
        }

        private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
        {
            // 타일 FX는 있으면 표시
            var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            if (tileFx != null)
            {
                tileFx.CachedTr.position = tile.View.CachedTr.position;
            }

            // 피격/데미지 로직은 tileFx와 무관하게 실행
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                if (!_hitCharacters.Exists(l => l == tile.OccupiedCharacter))
                {
                    InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                        tile.OccupiedCharacter.SkillRootTransformFollowable);

                    var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter, codeId, true);

                    tile.OccupiedCharacter.GetDamaged(damage, owner);

                    _hitCharacters.Add(tile.OccupiedCharacter);
                }
            }
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

    }


    /// <summary>
    /// 프롤로그_클레이
    /// </summary>
    [UseEffectCodeIds(297550013)]
    public partial class EffectCodeSkill297550013 : EffectCodeCharacterBase
    {
    }



    /// <summary>
    /// 프롤로그_오데트
    /// </summary>
    [UseEffectCodeIds(297610014)]
    public partial class EffectCodeSkill297610014 : EffectCodeSkill217613501
    {

    }


    /// <summary>
    /// 프롤로그_마리에
    /// </summary>
    [UseEffectCodeIds(297560015)]
    public partial class EffectCodeSkill297560015 : EffectCodeCharacterBase
    {
        // 스킬 상태
        private bool _isReadyToActivate;
        private SkillActive _specSkill;
        private int _attackCount;
        private ObfuscatorFloat _damageRate;
        private ObfuscatorFloat _debuffDuration;
        private ObfuscatorFloat _debuffRate;
        private CharacterController _targetCharacter;
        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            SkillIndex = 1;
            CoolTimeElapsedTime = 0f;

            CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
            _attackCount = codeInfo.GetCodeStatToInt(1);
            _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
            _debuffDuration = codeInfo.GetCodeStatToFloat(3);
            _debuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;

            _isReadyToActivate = false;
            IsSkillActivated = false;

            _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            CoolTimeElapsedTime = 0f;

            CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
            _attackCount = codeInfo.GetCodeStatToInt(1);
            _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
            _debuffDuration = codeInfo.GetCodeStatToFloat(3);
            _debuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
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
        }

        public override void OnSkillExecute(int executeIndex, int totalLength)
        {
            base.OnSkillExecute(executeIndex, totalLength);
            if (owner == null)
                return;

            if (executeIndex == 0)
            {
                InGameTile targetBackTile = _targetCharacter.CurrentTile;
                owner.GetCharacterView().LookAt(targetBackTile, _targetCharacter.CurrentTile);
                owner.Target = _targetCharacter;
            }


            // 이동 완료 후 공격 시작
            StartAttackSequence(executeIndex, totalLength);
        }

        private List<InGameTile> GetPathBetweenTiles(InGameGrid grid, InGameTile startTile, InGameTile endTile)
        {
            var pathTiles = new List<InGameTile>();
            var startPos = startTile.Int2Index;
            var endPos = endTile.Int2Index;

            // Bresenham 알고리즘으로 직선 경로 계산
            int deltaX = Math.Abs(endPos.x - startPos.x);
            int deltaY = Math.Abs(endPos.y - startPos.y);
            int stepX = startPos.x < endPos.x ? 1 : -1;
            int stepY = startPos.y < endPos.y ? 1 : -1;
            int error = deltaX - deltaY;

            int currentX = startPos.x;
            int currentY = startPos.y;

            // 시작 타일부터 목표 타일까지 경로 추가
            while (true)
            {
                var pathTile = grid.GetTile(new int2(currentX, currentY));
                pathTiles.Add(pathTile);

                // 목표 타일에 도달하면 종료
                if (currentX == endPos.x && currentY == endPos.y)
                    break;

                // Bresenham 알고리즘: 다음 타일 결정
                int error2 = 2 * error;
                if (error2 > -deltaY)
                {
                    error -= deltaY;
                    currentX += stepX;
                }
                if (error2 < deltaX)
                {
                    error += deltaX;
                    currentY += stepY;
                }
            }

            return pathTiles;
        }

        private CharacterController FindValidTarget()
        {
            if (owner == null || owner.CurrentTile == null)
                return null;

            var allTargets = InGameObjectManager.Instance?.GetCharacterListSortedByADDescending(owner.AllianceType, false);
            if (allTargets == null || allTargets.Count == 0)
                return null;

            foreach (var target in allTargets)
            {
                if (target == null || !target.IsAlive || target.CurrentTile == null)
                    continue;

                // 타겟의 뒤로 이동할 타일 찾기
                InGameTile targetBackTile = GetTileBehindTarget(owner.CurrentTile, target.CurrentTile);

                // 이동 가능한 타일이 있고 타겟 타일이 아니면 유효한 타겟
                if (targetBackTile != null && targetBackTile != target.CurrentTile && targetBackTile.OccupiedCharacter == null)
                {
                    return target;
                }
            }

            return null; // 이동 가능한 타겟이 없음
        }

        private InGameTile GetTileBehindTarget(InGameTile attackerTile, InGameTile targetTile)
        {
            if (attackerTile == null || targetTile == null)
                return null;

            var grid = InGameObjectManager.Instance.InGameGrid;
            if (grid == null)
                return null;

            // 바로 뒤 타일(1칸) 찾기 - 파라미터 순서: (attackerTile, targetTile, count)
            InGameTile behindTile = grid.GetTileForKnockBack(attackerTile, targetTile, 1);

            // GetTileForKnockBack은 차단되면 targetTile을 반환할 수 있으므로 명시적으로 체크
            // 바로 뒤 타일이 유효하고 타겟 타일이 아니고 비어있으면 반환
            if (behindTile != null && behindTile != targetTile && behindTile.OccupiedCharacter == null)
            {
                return behindTile;
            }

            // 바로 뒤 타일이 없거나 이동 불가능하면 근처 빈 타일 찾기
            for (int distance = 1; distance <= 3; distance++)
            {
                var nearbyTiles = grid.GetTileListByManhattanDistance(targetTile, distance);
                if (nearbyTiles == null || nearbyTiles.Count == 0)
                    continue;

                var emptyTile = nearbyTiles.FirstOrDefault(t => t != null && t != targetTile && t.OccupiedCharacter == null);

                if (emptyTile != null)
                {
                    return emptyTile;
                }
            }

            return null;
        }

        private void StartAttackSequence(int executeIndex, int totalLength)
        {
            if (owner == null || _targetCharacter == null || !_targetCharacter.IsAlive)
                return;

            // executeIndex에 따라 공격 횟수 계산
            int baseAttackCount = _attackCount / totalLength;
            int remainder = _attackCount % totalLength;
            int attackCountForThisIndex = baseAttackCount + (executeIndex == 0 ? remainder : 0);
            if (executeIndex == 0)
            {
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], _targetCharacter.SkillTopFXTransformFollowable.GetPosition());
            }

            // 공격 횟수만큼 공격
            for (int i = 0; i < attackCountForThisIndex; i++)
            {
                if (owner == null || _targetCharacter == null || !_targetCharacter.IsAlive)
                    break;

                // 피해 계산 및 적용
                var damage = owner.CalculateDamageAmount(owner.AD * _damageRate, 0, _targetCharacter, codeId, true);
                damage.damageAmount = Math.Floor(damage.damageAmount.Value);
                _targetCharacter.GetDamaged(damage, owner);

                // TODO 표식-아라크네 체크 및 디버프 적용
                //CheckAndApplyDebuff();
            }
        }

        private void CheckAndApplyDebuff()
        {
            if (_targetCharacter == null || owner == null)
                return;

            // 표식-아라크네 체크 (EffectCodeNameType을 확인해야 함)
            var ecc = _targetCharacter.GetEffectCodeContainer();
            // TODO: 표식-아라크네 이펙트 코드 타입 확인 필요
            // if (ecc.GetEffectCode((int)EffectCodeNameType.MARK_ARACHNE) != null)
            {
                // 디버프 적용
                Span<double> debuffStats = stackalloc double[3];
                debuffStats[0] = codeId;
                debuffStats[1] = _debuffDuration;
                debuffStats[2] = _debuffRate;

                // 공격력 감소 디버프
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AD_PERCENT_DOWN, _targetCharacter, debuffStats, source);

                debuffStats[2] = _debuffRate * 100f;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_DEF_PERCENT_DOWN, _targetCharacter, debuffStats, source);
            }
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
    }
}