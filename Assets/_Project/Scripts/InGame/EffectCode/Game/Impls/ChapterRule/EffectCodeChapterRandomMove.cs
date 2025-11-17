using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeChapterRandomMove : EffectCodeGameBase
    {
        private const int CodeId = (int) EffectCodeNameType.CHAPTER_RANDOM_MOVE;
        Dictionary<InGameTile, InGameVfx> _chapterRuleTiles = new Dictionary<InGameTile, InGameVfx>();
        private float _effectCodeStat;

        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            int tileID = codeInfo.GetCodeStatToInt(0);
            InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

            var vfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_01,
                inGameTile.View.CachedTr.position);
            _chapterRuleTiles.Add(inGameTile, vfx);
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _chapterRuleTiles.Clear();

            SetRuleTileByInfo(codeInfo);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            
            SetRuleTileByInfo(codeInfo);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.ContainsKey(tile))
            {
                SkillAction(tile).Forget();
            }
        }
    
        private async UniTaskVoid SkillAction(InGameTile tile)
        {
            var character = tile.OccupiedCharacter;

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_06_01,
                tile.View.CachedTr.position);

            await UniTask.Delay(TimeSpan.FromSeconds(0.3));

            if (character != null)
            {
                var randomTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile();
                character.ChangeOccupiedTile(randomTile);
                character.Position3D = randomTile.View.Position;
                character.GetCharacterView().CachedTr.localPosition = randomTile.View.Position;

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_06_02,
                    randomTile.View.CachedTr.position);
            }
        }
    }
}
