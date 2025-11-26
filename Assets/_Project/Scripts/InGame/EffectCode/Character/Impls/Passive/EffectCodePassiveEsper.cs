using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;


namespace CookApps.BattleSystem
{
    /// <summary>
    /// 평타 공격 시 {0}% 확률로 폭발 3x3
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodePassiveEsper : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.PASSIVE_ESPER;
        private const InGameVfxNameType _explosionVfxEnum = InGameVfxNameType.fx_common_hit_02;

        private float _passivePercentage = 0f;
        private float _explosionDamate = 0f;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _passivePercentage = codeInfo.GetCodeStatToFloat(1);
            _explosionDamate = codeInfo.GetCodeStatToFloat(3);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _passivePercentage = codeInfo.GetCodeStatToFloat(1);
            _explosionDamate = codeInfo.GetCodeStatToFloat(3);
        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            if (InGameRandomManager.GetUniversalRandomValue(0f, 100f) < _passivePercentage * 100)
            {
                return;
            }
            //펑 터져서 지워야할 타일들 목록.
            var InGameObjectManagerInstance = InGameObjectManager.Instance;
            var InGameVfxManagerInstance = InGameVfxManager.Instance;

            //target에게 폭발 3x3 적용
            var damage = CharacterController.DamageInfo.Create(_explosionDamate, codeId, AttackerType.CHARCTER);
            var explosionTiles = InGameObjectManagerInstance.InGameGrid.GetTileListByShapeSquare(target.CurrentTile, 1);

            foreach (var explosionTile in explosionTiles)
            {
                explosionTile.OccupiedCharacter?.GetDamaged(damage, null);
                InGameVfxManagerInstance.AddInGameVfx(_explosionVfxEnum, explosionTile.View.CachedTr.position);
            }
        }


    }

}