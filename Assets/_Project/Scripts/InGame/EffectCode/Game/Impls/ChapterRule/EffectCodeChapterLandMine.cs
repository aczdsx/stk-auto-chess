using System;
using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeChapterLandMine : EffectCodeGameBase
    {
        private const int CodeId = (int)EffectCodeNameType.CHAPTER_LANDMINE;
        private float _effectCodeStat;
        private float _damage;

        private InGameTile _targetTile;
        private InGameVfx _vfx;


        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            int tileID = codeInfo.GetCodeStatToInt(0);
            InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

            _vfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_explosion,
                inGameTile.View.CachedTr.position);
            _targetTile = inGameTile;
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _damage = codeInfo.GetCodeStatToFloat(2);

            SetRuleTileByInfo(codeInfo);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _damage = codeInfo.GetCodeStatToFloat(2);
            SetRuleTileByInfo(codeInfo);
        }

        public override void OnTileMoveEnd(InGameTile tile, CharacterController character)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
                return;
            if (_targetTile != tile)
                return;

            _targetTile.EffectCodeContainer.RemoveEffectCode(CodeId);
            _vfx.Remove();
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_shooter_mine);

            var explosionTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(tile, 1);
            var damage = CharacterController.DamageInfo.Create(_damage, codeId, AttackerType.CHAPTER_RULE);
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_bomb_01, _targetTile.View.CachedTr.position);

            Span<double> eccStats = stackalloc double[1];
            eccStats.Clear();
            eccStats[0] = _effectCodeStat;

            foreach (var explosionTile in explosionTiles)
            {
                var occupiedCharacter = explosionTile.OccupiedCharacter;
                if (occupiedCharacter == null || !occupiedCharacter.IsAlive)
                    continue;
                occupiedCharacter.GetDamaged(damage, null);
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, occupiedCharacter, eccStats, source);
            }
        }
    }
}
