using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;


namespace CookApps.BattleSystem
{
    /// <summary>
    /// 평타 공격 시 {0}% 확률로 폭발 3x3
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeJobPassiveEsper : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_ESPER;
        private const InGameVfxNameType _explosionVfxEnum = InGameVfxNameType.fx_common_job_espar_01;
        private float _passivePercentage = 0f;
        private float _explosionDamage = 0f;

        private Action<int, InGameTile> _onExplosionDamage;

        public event Action<int, InGameTile> OnExplosionDamage;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            owner.SetStateType(typeof(CharacterStateAttack), typeof(CharacterStateAttackEsper));
            _passivePercentage = codeInfo.GetCodeStatToFloat(1);
            _explosionDamage = codeInfo.GetCodeStatToFloat(4);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _passivePercentage = codeInfo.GetCodeStatToFloat(1);
            _explosionDamage = codeInfo.GetCodeStatToFloat(4);
        }

        public bool IsExplosionDamage()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < _passivePercentage * 100;
        }
        public float GetPassivePercentage()
        {
            return _passivePercentage;
        }
        public void ProgressExplosionDamage(CharacterController target)
        {
            //펑 터져서 지워야할 타일들 목록.
            var InGameObjectManagerInstance = InGameObjectManager.Instance;
            var InGameVfxManagerInstance = InGameVfxManager.Instance;
            var explosionStartTile = target.CurrentTile;

            //target에게 폭발 3x3 적용
            var damage = CharacterController.DamageInfo.Create(_explosionDamage, codeId, AttackerType.CHARCTER);
            var explosionTiles = InGameObjectManagerInstance.InGameGrid.GetTileListByShapeSquare(explosionStartTile, 1);

            InGameVfxManagerInstance.AddInGameVfx(_explosionVfxEnum, target.CurrentTile.View.CachedTr.position);

            int damagedTargetCount = 0;
            foreach (var explosionTile in explosionTiles)
            {
                if (explosionTile.OccupiedCharacter == null
                || explosionTile.OccupiedCharacter.AllianceType == owner.AllianceType)
                    continue;
                explosionTile.OccupiedCharacter.GetDamaged(damage, owner);
                damagedTargetCount++;
            }

            _onExplosionDamage?.Invoke(damagedTargetCount, explosionStartTile);
        }


        public override void OnPreRemoved()
        {
            owner.RemoveStateType(typeof(CharacterStateAttack));
            base.OnPreRemoved();
        }



    }

}