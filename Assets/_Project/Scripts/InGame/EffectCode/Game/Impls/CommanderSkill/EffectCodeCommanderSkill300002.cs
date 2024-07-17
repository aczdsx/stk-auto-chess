using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

//십자범위 내 적을 {0}초 동안 빙결 시킨다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(300002)]
    public class EffectCodeCommanderSkill300002 : EffectCodeGameBase
    {
        private ObfuscatorInt _tileID;
        private ObfuscatorFloat _time;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _time = codeInfo.GetCodeStatToFloat(1);

            SkillAction();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _time = codeInfo.GetCodeStatToFloat(1);

            SkillAction();
        }

        private void SkillAction()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(inGameTile, 1);
            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_04,
                    tile.View.CachedTr.position);

                if (tile.OccupiedCharacter != null)
                {
                    if (tile.OccupiedCharacter.AllianceType != AllianceType.Player &&
                        tile.OccupiedCharacter.AllianceType != AllianceType.None)
                    {
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_02,
                            tile.View.CachedTr.position);

                        Span<double> eccStats = stackalloc double[1];
                        eccStats.Clear();
                        eccStats[0] = _time;

                        long effectCodeID = (long) EffectCodeNameType.STUN;
                        var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
                        tile.OccupiedCharacter.GetEffectCodeContainer()
                            .AddOrMergeEffectCode(effectCodeInfo, null);
                        tile.OccupiedCharacter.Position3D =
                            tile.OccupiedCharacter.CurrentTile.View.CachedTr.position;

                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_ice);
                    }
                }
            }
        }
    }
}
