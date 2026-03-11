using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 전투 VFX 생명주기 관리.
    /// OneShotVfx: 적용 순간 1회 재생 후 자동 제거.
    /// LoopVfx: 효과 지속 중 유닛에 부착, 해제 시 제거.
    /// 레퍼런스 카운팅으로 같은 타입 중복 적용 대응.
    /// </summary>
    public class CombatVfxManager
    {
        private readonly CombatVfxConfigSO _config;
        private readonly UnitViewManager _unitViewManager;

        // 활성 루프 VFX: (combatId, type) → 인스턴스 핸들
        private readonly Dictionary<(int, CombatVfxType), AsyncOperationHandle<GameObject>> _activeLoopVfx = new();

        // 레퍼런스 카운팅 (같은 타입 중복 적용 대응)
        private readonly Dictionary<(int, CombatVfxType), int> _refCounts = new();

        private const float OneShotLifetime = 3f;

        public CombatVfxManager(CombatVfxConfigSO config, UnitViewManager unitViewManager)
        {
            _config = config;
            _unitViewManager = unitViewManager;
        }

        /// <summary>효과 추가 — OneShot 1회 재생 + Loop 부착</summary>
        public void OnEffectAdded(int combatId, CombatVfxType type)
        {
            if (_config == null) return;
            if (!_config.TryGetEntry(type, out var entry)) return;

            var key = (combatId, type);

            // 레퍼런스 카운트 증가 — 이미 VFX가 붙어있으면 스킵
            if (_refCounts.TryGetValue(key, out int count))
            {
                _refCounts[key] = count + 1;
                return;
            }
            _refCounts[key] = 1;

            // 사운드 재생
            PlayEffectSound(type);

            // OneShot VFX (적용 순간 1회)
            if (entry.OneShotVfx != null && entry.OneShotVfx.RuntimeKeyIsValid())
                SpawnOneShotAsync(combatId, entry.OneShotVfx).Forget();

            // Loop VFX (지속 중 부착)
            if (entry.LoopVfx != null && entry.LoopVfx.RuntimeKeyIsValid())
                SpawnLoopAsync(combatId, type, entry.LoopVfx).Forget();
        }

        /// <summary>효과 제거 — Loop VFX 제거</summary>
        public void OnEffectRemoved(int combatId, CombatVfxType type)
        {
            var key = (combatId, type);

            if (_refCounts.TryGetValue(key, out int count))
            {
                count--;
                if (count > 0)
                {
                    _refCounts[key] = count;
                    return;
                }
                _refCounts.Remove(key);
            }

            ReleaseLoop(key);
        }

        /// <summary>전투 종료 — 모든 VFX 정리</summary>
        public void OnCombatEnd()
        {
            foreach (var kvp in _activeLoopVfx)
            {
                if (kvp.Value.IsValid())
                    Addressables.ReleaseInstance(kvp.Value);
            }
            _activeLoopVfx.Clear();
            _refCounts.Clear();
        }

        // ── OneShot: 1회 재생 후 자동 제거 ──

        private async UniTaskVoid SpawnOneShotAsync(int combatId, AssetReferenceGameObject assetRef)
        {
            var unitView = _unitViewManager?.FindCombatView(combatId);
            if (unitView == null) return;

            var handle = Addressables.InstantiateAsync(assetRef, unitView.transform.position, Quaternion.identity);
            var go = await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded || go == null)
            {
                if (handle.IsValid()) Addressables.ReleaseInstance(handle);
                return;
            }

            ReleaseAfterDelay(handle, OneShotLifetime).Forget();
        }

        // ── Loop: 유닛에 부착, 해제 시 제거 ──

        private async UniTaskVoid SpawnLoopAsync(int combatId, CombatVfxType type, AssetReferenceGameObject assetRef)
        {
            var unitView = _unitViewManager?.FindCombatView(combatId);
            if (unitView == null) return;

            var key = (combatId, type);
            var handle = Addressables.InstantiateAsync(assetRef, unitView.transform);
            _activeLoopVfx[key] = handle;

            var go = await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded || go == null)
            {
                _activeLoopVfx.Remove(key);
                if (handle.IsValid()) Addressables.ReleaseInstance(handle);
                return;
            }

            // 로드 완료 시점에 이미 제거된 경우 (유닛 사망 등)
            if (!_activeLoopVfx.ContainsKey(key))
            {
                Addressables.ReleaseInstance(handle);
                return;
            }

            go.transform.localPosition = Vector3.zero;
        }

        private void ReleaseLoop((int, CombatVfxType) key)
        {
            if (!_activeLoopVfx.TryGetValue(key, out var handle)) return;
            _activeLoopVfx.Remove(key);

            if (handle.IsValid())
                Addressables.ReleaseInstance(handle);
        }

        private static async UniTaskVoid ReleaseAfterDelay(AsyncOperationHandle<GameObject> handle, float delay)
        {
            await UniTask.Delay((int)(delay * 1000));
            if (handle.IsValid())
                Addressables.ReleaseInstance(handle);
        }

        // ── 사운드 ──

        private static void PlayEffectSound(CombatVfxType type)
        {
            var sfx = GetEffectSound(type);
            if (sfx != SoundFX.NONE)
                SoundManager.Instance.PlaySFX(sfx);
        }

        private static SoundFX GetEffectSound(CombatVfxType type)
        {
            switch (type)
            {
                // 버프
                case CombatVfxType.StatBuff_Attack:
                case CombatVfxType.StatBuff_Armor:
                case CombatVfxType.StatBuff_MagicResist:
                case CombatVfxType.StatBuff_AttackSpeed:
                case CombatVfxType.ContinuousHeal:
                case CombatVfxType.CCImmunity:
                case CombatVfxType.DOTImmunity:
                case CombatVfxType.DebuffImmunity:
                    return SoundFX.snd_sfx_ingame_buff;

                // 디버프
                case CombatVfxType.StatDebuff_Attack:
                case CombatVfxType.StatDebuff_Armor:
                case CombatVfxType.StatDebuff_MagicResist:
                case CombatVfxType.StatDebuff_AttackSpeed:
                case CombatVfxType.ContinuousDamage:
                    return SoundFX.snd_sfx_ingame_debuff;

                // CC (개별 사운드)
                case CombatVfxType.CC_Stun:
                    return SoundFX.snd_sfx_ingame_stun;
                case CombatVfxType.CC_Freeze:
                    return SoundFX.snd_sfx_ingame_freeze;

                // CC (공통 디버프 사운드)
                case CombatVfxType.CC_Silence:
                case CombatVfxType.CC_Slow:
                case CombatVfxType.CC_Taunt:
                case CombatVfxType.CC_Airborne:
                case CombatVfxType.CC_KnockBack:
                    return SoundFX.snd_sfx_ingame_debuff;

                default:
                    return SoundFX.NONE;
            }
        }
    }
}
