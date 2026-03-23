using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 개별 유닛 비주얼. 보드/벤치/전투 유닛의 시각적 표현.
    /// 위치 보간, HP/마나 바, 상태 애니메이션 관리.
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private SpriteRenderer _selectionIndicator;

        // ── 상태 ──
        public int EntityId { get; private set; }
        public int CombatId { get; private set; } = CombatUnit.InvalidId;
        public bool IsCombatUnit { get; private set; }
        public bool IsReady => _characterView != null;
        public int ChampSpecId => _champSpecId;

        private Vector3 _targetPosition;
        private float _interpolationSpeed = 15f;
        private bool _isActive = true;

        // HP/Mana (0-1 정규화)
        public float HPRatio { get; private set; } = 1f;
        public float ManaRatio { get; private set; }
        public byte StarLevel { get; private set; } = 1;

        // ── 캐릭터 비주얼 ──
        private SpriteCharacterView _characterView;
        private AsyncOperationHandle<GameObject> _loadHandle;

        // ── HP 바 ──
        private HpBarView _hpBarView;
        private int _champSpecId;
        private bool _isPlayer;
        private int _lastMaxHP;      // 배치 프리뷰용 최근 maxHP 캐시

        // ── Desired State (로딩 전 호출된 상태를 추적, 로딩 후 ApplyDeferredState로 일괄 적용) ──
        private CombatState _lastState = CombatState.Idle;
        private float _attackAnimEndTime;
        private bool _isHologram;
        private Vector3? _facingTarget;

        // ── ATK/ATK2/CRIT 애니메이션 선택 ──
        private bool _atkToggle;                                     // false=ATK, true=ATK2 (순차 토글)
        private AnimationKey _pendingAttackAnimKey = AnimationKey.ATK; // 다음 공격 애니메이션
        private bool _pendingAttackPrepared;                          // PrepareAttackAnimation 호출 여부

        // ── 초기화 ──

        public void Initialize(int entityId, byte starLevel, string prefabPath, int champSpecId = 0)
        {
            EntityId = entityId;
            CombatId = CombatUnit.InvalidId;
            IsCombatUnit = false;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _champSpecId = champSpecId;
            _isPlayer = true;
            _lastState = CombatState.Idle;
            _attackAnimEndTime = 0f;
            _isHologram = false;
            _facingTarget = null;
            _atkToggle = false;
            _pendingAttackAnimKey = AnimationKey.ATK;
            _pendingAttackPrepared = false;
            _isActive = true;
            ReleaseHpBar();
            gameObject.SetActive(true);
            LoadCharacterVisual(prefabPath).Forget();
        }

        public void InitializeAsCombat(int combatId, int sourceEntityId, byte starLevel, string prefabPath,
            int champSpecId = 0, bool isPlayer = true)
        {
            EntityId = sourceEntityId;
            CombatId = combatId;
            IsCombatUnit = true;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _champSpecId = champSpecId;
            _isPlayer = isPlayer;
            _lastState = CombatState.Idle;
            _attackAnimEndTime = 0f;
            _isHologram = false;
            _facingTarget = null;
            _atkToggle = false;
            _pendingAttackAnimKey = AnimationKey.ATK;
            _pendingAttackPrepared = false;
            _targetPosition = transform.position;
            _isActive = true;
            _lastMaxHP = 0;
            ReleaseHpBar();
            gameObject.SetActive(true);
            LoadCharacterVisual(prefabPath).Forget();
        }

        // ── 캐릭터 프리팹 로딩 ──

        private async UniTaskVoid LoadCharacterVisual(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)) return;
            ReleaseCharacterVisual();

            var handle = Addressables.InstantiateAsync(prefabPath, _modelRoot);
            _loadHandle = handle;
            await handle.WaitUntilDone();

            // await 완료 후: _loadHandle이 교체되었으면 이 결과는 버림
            if (!_loadHandle.Equals(handle))
            {
                if (handle.IsValid())
                    Addressables.ReleaseInstance(handle);
                return;
            }

            var go = handle.Result;

            if (!_isActive || go == null)
            {
                ReleaseCharacterVisual();
                return;
            }

            _characterView = go.GetComponent<SpriteCharacterView>();
            _characterView?.PlayAnimation(AnimationKey.IDLE);
            ApplyDeferredState();
        }

        /// <summary>로딩 완료 후 지연된 상태를 일괄 적용</summary>
        private void ApplyDeferredState()
        {
            if (_characterView == null) return;

            if (_isHologram)
                _characterView.SetHologramShader();

            if (_facingTarget.HasValue)
                ApplyFacing();

            // HP 바 부착 (전투 유닛 또는 보드 유닛)
            if (_champSpecId > 0 && (IsCombatUnit || EntityId > 0))
                AttachHpBar();
        }

        private void AttachHpBar()
        {
            if (_hpBarView != null || _characterView == null) return;

            _hpBarView = InGameHpBarViewPool.Instance.Get();
            if (_hpBarView == null) return;

            _hpBarView.Initialize(_champSpecId, _isPlayer, _lastMaxHP);
            _hpBarView.SetHpBarType(IsCombatUnit
                ? HpBarType.HpBar | HpBarType.Buff
                : HpBarType.HpBar | HpBarType.Synergy);

            var spec = SpecDataManager.Instance.GetSpecCharacter(_champSpecId);
            float height = spec?.height ?? 1.5f;
            _characterView.SetHpBarView(_hpBarView, height);

            // 캐시된 HP가 있으면 즉시 반영 (비동기 로딩으로 인해 UpdateHP보다 늦게 부착될 수 있음)
            // _lastMaxHP가 0이어도 SetValue를 호출하여 CachedGo를 활성화 (ShowHpBar)
            _hpBarView.SetValue(_lastMaxHP > 0 ? _lastMaxHP : 1, _lastMaxHP > 0 ? _lastMaxHP : 1, 0);
        }

        private void ReleaseHpBar()
        {
            if (_hpBarView == null) return;
            _hpBarView.OnPreReturn();
            InGameHpBarViewPool.Instance.Return(_hpBarView);
            _hpBarView = null;
        }

        private void ReleaseCharacterVisual()
        {
            _characterView = null;
            if (_loadHandle.IsValid())
            {
                Addressables.ReleaseInstance(_loadHandle);
                _loadHandle = default;
            }
        }

        // ── 위치 업데이트 ──

        /// <summary>부드러운 보간 이동 (활성 보드)</summary>
        public void SetTargetPosition(Vector3 pos)
        {
            _targetPosition = pos;
        }

        /// <summary>즉시 이동 (비활성 보드 / 초기 배치)</summary>
        public void SetPositionImmediate(Vector3 pos)
        {
            _targetPosition = pos;
            transform.position = pos;
        }

        // ── 스탯 업데이트 ──

        public void UpdateHP(int current, int max, int shield = 0)
        {
            HPRatio = max > 0 ? (float)current / max : 0f;
            _lastMaxHP = max;
            // HP바가 해제된 상태(사망 후 부활 등)에서 캐릭터 뷰가 있으면 재부착
            if (_hpBarView == null && _characterView != null && _champSpecId > 0)
                AttachHpBar();
            _hpBarView?.SetValue(current, max, shield);
        }

        public void UpdateMana(int current, int max)
        {
            ManaRatio = max > 0 ? (float)current / max : 0f;
            _hpBarView?.OnCoolTimeUpdated(0, current, max);
        }

        public void UpdateBuffIcons(IReadOnlyList<HpBarView.NewBuffIconData> buffIcons)
        {
            _hpBarView?.RestructBuffIcon(buffIcons);
        }

        public void UpdateStarLevel(byte level)
        {
            StarLevel = level;
            // TODO: 별 레벨 비주얼 업데이트 (파티클, 이펙트)
        }

        // ── 상태 + 애니메이션 ──

        public void SetCombatState(CombatState state, int attackSpeed = 100)
        {
            // 공격/스킬 애니메이션 재생 중이면 다른 상태 전환 차단 (공격 모션 끊김 + 방향 전환 방지)
            // 단, 사망/CC/새 공격/스킬은 허용
            if (IsPlayingAttackAnim
                && state != CombatState.Dead
                && state != CombatState.Attacking
                && state != CombatState.CastingSkill
                && state != CombatState.CrowdControlled)
                return;

            if (state == _lastState && !(state == CombatState.Attacking && _pendingAttackPrepared)) return;
            if (_characterView == null) return;
            _lastState = state;

            AnimationKey animKey;
            if (state == CombatState.Attacking && _pendingAttackPrepared)
            {
                animKey = _pendingAttackAnimKey;
                _pendingAttackPrepared = false;
            }
            else
            {
                animKey = StateToAnimKey(state);
            }

            var clip = _characterView.PlayAnimation(animKey);

            if ((state == CombatState.Attacking || state == CombatState.CastingSkill) && clip != null)
            {
                // 공격속도에 따른 애니메이션 속도 조절 (구 CharacterStateAttack 로직)
                float atkSpeedF = attackSpeed / 100f;
                if (state == CombatState.Attacking && atkSpeedF > 0f)
                {
                    float atkTime = 1f / atkSpeedF;
                    float animTime = clip.length * 1.5f;
                    float animSpeed = animTime > atkTime ? animTime * atkSpeedF : 1f;
                    _characterView.SetAnimationSpeed(animSpeed);
                    _attackAnimEndTime = Time.time + clip.length / animSpeed;
                }
                else
                {
                    _characterView.SetAnimationSpeed(1f);
                    _attackAnimEndTime = Time.time + clip.length;
                }
            }
            else if (state == CombatState.Idle || state == CombatState.Moving)
            {
                _characterView.SetAnimationSpeed(1f);
            }
        }

        /// <summary>전투 종료 시 강제 Idle 전환 (_attackAnimEndTime 보호 무시)</summary>
        public void ForceIdle()
        {
            if (_lastState == CombatState.Dead) return;
            if (_characterView == null) return;
            _attackAnimEndTime = 0f;
            _lastState = CombatState.Idle;
            _characterView.PlayAnimation(AnimationKey.IDLE);
        }

        private static AnimationKey StateToAnimKey(CombatState state)
        {
            return state switch
            {
                CombatState.Idle => AnimationKey.IDLE,
                CombatState.Moving => AnimationKey.MOVE,
                CombatState.Attacking => AnimationKey.ATK,
                CombatState.CastingSkill => AnimationKey.SKL,
                CombatState.Dead => AnimationKey.DEAD,
                CombatState.CrowdControlled => AnimationKey.DEAD,
                _ => AnimationKey.IDLE,
            };
        }

        /// <summary>ATK 키프레임 정보 반환 (기본 ATK)</summary>
        public AnimKeyframeInfo GetAtkInfo()
        {
            if (_characterView == null) return default;
            return _characterView.GetAtkInfo();
        }

        /// <summary>지정된 클립 타입의 키프레임 정보 반환. 데이터 없으면 ATK로 폴백.</summary>
        public AnimKeyframeInfo GetAtkInfo(AnimClipType clipType)
        {
            if (_characterView == null) return default;
            if (clipType == AnimClipType.ATK) return _characterView.GetAtkInfo();
            return _characterView.GetAtkInfoForClip(clipType);
        }

        /// <summary>현재 방향(front/back) 반환</summary>
        public bool IsFacingFront()
        {
            return _characterView != null && _characterView.CachedFront;
        }

        /// <summary>현재 Animator 재생 속도 (슬로우 디버프 등 반영)</summary>
        public float AnimatorSpeed => _characterView != null ? _characterView.AnimatorSpeed : 1f;

        /// <summary>공격/스킬 애니메이션 재생 중 여부</summary>
        public bool IsPlayingAttackAnim => Time.time < _attackAnimEndTime;


        public Vector3 GetProjectileSpawnPosition()
        {
            if (_characterView == null) return transform.position;
            return _characterView.GetProjectileSpawnPosition();
        }

        public GameObject GetProjectilePrefab()
        {
            return _characterView != null ? _characterView.ProjectilePrefab : null;
        }

        public SkillViewData[] GetSkillEffectPrefabs()
        {
            return _characterView != null ? _characterView.SkillEffectPrefabs : null;
        }

        public Transform GetSkillPositionTransform(SkillPosition pos)
        {
            if (_characterView == null) return transform;
            switch (pos)
            {
                case SkillPosition.SKILL_ROOT:       return _characterView.SkillRootTransform;
                case SkillPosition.SKILL_TOP:        return _characterView.SkillTopFXTransform;
                case SkillPosition.SKILL_MIDDLE:     return _characterView.SkillMiddleFXTransform;
                case SkillPosition.SKILL_BOTTOM:     return _characterView.SkillBottomFXTransform;
                default:                             return transform;
            }
        }

        public bool HasCastingVfx => _characterView != null
            && _characterView.VfxConfig != null
            && _characterView.VfxConfig.HasCastingVfx;

        public float GetCharacterHeight()
        {
            if (_champSpecId <= 0) return 1.5f;
            var spec = SpecDataManager.Instance.GetSpecCharacter(_champSpecId);
            return spec?.height ?? 1.5f;
        }

        /// <summary>
        /// 다음 공격 애니메이션 타입을 결정 (이벤트 수신 시 호출).
        /// CRIT이면 CRIT 재생, 아니면 ATK↔ATK2 토글 순차 실행.
        /// CRIT은 토글 상태를 변경하지 않음.
        /// </summary>
        public void PrepareAttackAnimation(bool isCrit)
        {
            if (isCrit && _characterView != null && _characterView.HasCritClip)
            {
                _pendingAttackAnimKey = AnimationKey.CRIT;
            }
            else
            {
                bool useAtk2 = _atkToggle && _characterView != null && _characterView.HasAtk2Clip;
                _pendingAttackAnimKey = useAtk2 ? AnimationKey.ATK2 : AnimationKey.ATK;
                _atkToggle = !_atkToggle;
            }
            _pendingAttackPrepared = true;
        }

        /// <summary>현재 재생 중인 공격 애니메이션의 AnimClipType 반환</summary>
        public AnimClipType GetCurrentAttackClipType()
        {
            return _pendingAttackAnimKey switch
            {
                AnimationKey.ATK2 => AnimClipType.ATK2,
                AnimationKey.CRIT => AnimClipType.CRIT,
                _ => AnimClipType.ATK,
            };
        }

        public void PlayAttackAnimation()
        {
            var animKey = _pendingAttackPrepared ? _pendingAttackAnimKey : AnimationKey.ATK;
            _pendingAttackPrepared = false;
            _characterView?.PlayAnimation(animKey);
        }

        public void PlayHitEffect()
        {
            _characterView?.OnHit();
        }

        public void PlayDeathAnimation()
        {
            if (_lastState == CombatState.Dead) return;
            if (_characterView == null) return;
            _lastState = CombatState.Dead;
            _attackAnimEndTime = 0f;
            ReleaseHpBar();
            _characterView.SetShadowActive(false);
            var clip = _characterView.PlayAnimation(AnimationKey.DEAD);
            if (clip != null)
                _characterView.SetDeadSprite(clip);
        }

        // ── 방향 전환 ──

        public void UpdateFacing(Vector3 targetWorldPos)
        {
            _facingTarget = targetWorldPos;
            if (_characterView == null) return;
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (_characterView == null || !_facingTarget.HasValue) return;
            var myPos = transform.position;
            _characterView.LookAt(
                new Vector2(myPos.z, myPos.x),
                new Vector2(_facingTarget.Value.z, _facingTarget.Value.x));
        }

        // ── 모델 가시성 (미사 봉인 등) ──

        private bool _modelHidden;
        public bool IsModelHidden => _modelHidden;

        /// <summary>캐릭터 모델 숨김/표시 (관에 가둠 등 전용 연출).
        /// 스프라이트 렌더러만 끄므로 VFX Transform 위치에 영향 없음.</summary>
        public void SetModelVisible(bool visible)
        {
            _modelHidden = !visible;
            if (_characterView != null)
                _characterView.SpriteRendererSetActive(visible);
        }

        // ── 홀로그램 ──

        public void SetHologram(bool isHologram)
        {
            _isHologram = isHologram;
            if (_characterView == null) return;
            if (isHologram)
                _characterView.SetHologramShader();
            else
                _characterView.SetDisolveShader();
        }

        // ── Persistent VFX (루키다 도깨비불 등 유닛에 부착되는 지속형 VFX) ──

        private readonly System.Collections.Generic.Dictionary<int, GameObject> _persistentVfx
            = new System.Collections.Generic.Dictionary<int, GameObject>();
        private readonly System.Collections.Generic.Dictionary<int, SkillPosition> _persistentVfxPositions
            = new System.Collections.Generic.Dictionary<int, SkillPosition>();

        /// <summary>지속형 VFX의 자식 GO를 count만큼 활성화.
        /// 최초 호출 시 프리팹을 인스턴스화하여 유닛에 부착, 이후엔 자식 on/off만.</summary>
        public void UpdatePersistentVfx(int skillSpecId, GameObject prefab, SkillPosition position, int count)
        {
            if (!_persistentVfx.TryGetValue(skillSpecId, out var vfxGo) || vfxGo == null)
            {
                if (prefab == null) return;
                var posTransform = GetSkillPositionTransform(position);
                vfxGo = Instantiate(prefab, posTransform.position, posTransform.rotation, posTransform);
                _persistentVfx[skillSpecId] = vfxGo;
                _persistentVfxPositions[skillSpecId] = position;
            }

            // 자식 GO를 count만큼 활성화 (Fire_01, Fire_02, ...)
            for (int i = 0; i < vfxGo.transform.childCount; i++)
            {
                vfxGo.transform.GetChild(i).gameObject.SetActive(i < count);
            }
        }

        /// <summary>해당 skillSpecId의 persistent VFX가 이미 존재하는지</summary>
        public bool HasPersistentVfx(int skillSpecId)
        {
            return _persistentVfx.TryGetValue(skillSpecId, out var go) && go != null;
        }

        /// <summary>Persistent VFX를 파괴하지 않고 분리하여 반환 (parking용).</summary>
        public List<(int skillSpecId, GameObject go, SkillPosition position)> DetachPersistentVfx()
        {
            if (_persistentVfx.Count == 0) return null;
            var result = new List<(int, GameObject, SkillPosition)>();
            foreach (var kvp in _persistentVfx)
            {
                if (kvp.Value == null) continue;
                _persistentVfxPositions.TryGetValue(kvp.Key, out var pos);
                result.Add((kvp.Key, kvp.Value, pos));
            }
            _persistentVfx.Clear();
            _persistentVfxPositions.Clear();
            return result;
        }

        /// <summary>외부에서 전달받은 VFX GO를 이 UnitView의 적절한 Transform에 reparent.
        /// worldPositionStays:false로 localTransform 보존.</summary>
        public void AdoptPersistentVfx(int skillSpecId, GameObject vfxGo, SkillPosition position)
        {
            if (vfxGo == null) return;
            var targetTransform = GetSkillPositionTransform(position);
            vfxGo.transform.SetParent(targetTransform, worldPositionStays: false);
            _persistentVfx[skillSpecId] = vfxGo;
            _persistentVfxPositions[skillSpecId] = position;
        }

        private void ClearPersistentVfx()
        {
            foreach (var kvp in _persistentVfx)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _persistentVfx.Clear();
            _persistentVfxPositions.Clear();
        }

        // ── 비활성화 ──

        public void AddViewScale(float scale, bool forceSet = false)
        {
            if (_characterView != null)
                _characterView.AddViewScale(scale, forceSet);
        }

        public void RemoveViewScale(float scale)
        {
            if (_characterView != null)
                _characterView.RemoveViewScale(scale);
        }

        public void Deactivate()
        {
            _isActive = false;
            ReleaseHpBar();
            ClearPersistentVfx();
            ReleaseCharacterVisual();
            gameObject.SetActive(false);
        }

        /// <summary>Persistent VFX를 파괴하지 않고 분리한 뒤 비활성화 (parking용)</summary>
        public List<(int skillSpecId, GameObject go, SkillPosition position)> DeactivateWithParking()
        {
            _isActive = false;
            ReleaseHpBar();
            var detached = DetachPersistentVfx();
            ReleaseCharacterVisual();
            gameObject.SetActive(false);
            return detached;
        }

        // ── Unity Lifecycle ──

        private void Update()
        {
            if (!_isActive) return;

            // Preparation 페이즈 전용 보간 (전투 중은 SyncCombatUnits에서 SetPositionImmediate 사용)
            if (!IsCombatUnit && Vector3.SqrMagnitude(transform.position - _targetPosition) > 0.001f)
            {
                transform.position = Vector3.Lerp(
                    transform.position, _targetPosition,
                    Time.deltaTime * _interpolationSpeed);
            }
        }
    }
}
