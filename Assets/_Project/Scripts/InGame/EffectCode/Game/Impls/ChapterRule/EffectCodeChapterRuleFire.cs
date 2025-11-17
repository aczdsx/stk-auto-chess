using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeChapterRuleFire : EffectCodeGameBase
    {
        private const int CodeId = (int)EffectCodeNameType.CHAPTER_FIRE;
        List<InGameTile> _chapterRuleTiles = new List<InGameTile>();
        List<CharacterController> _characterList = new List<CharacterController>();
        private float _effectCodeStat;
        private float elapsedTime = 0f;

        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            int tileID = codeInfo.GetCodeStatToInt(0);
            InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
            _chapterRuleTiles.Add(inGameTile);

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_fire,
                inGameTile.View.CachedTr.position);
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
            _chapterRuleTiles.Clear();
            
            SetRuleTileByInfo(codeInfo);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
                return;

            if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
            {
                CharacterController.DamageInfo damage = CharacterController.DamageInfo.Create(_effectCodeStat, codeId, AttackerType.CHAPTER_RULE);

                character.GetDamaged(damage, null, hexColor: "#FF470000");
                InGameVfxManager.Instance.AddInGameTileFx(ElementType.FIRE, tile);
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_hit_03,
                    tile.View.CachedTr.position);

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_fire);

            }
        }

        public override void OnUpdate(float dt)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
                return;

            elapsedTime += dt;
            if (elapsedTime >= 1f)
            {
                foreach (var ruleTile in _chapterRuleTiles)
                {
                    if (ruleTile.OccupiedCharacter != null)
                    {
                        CharacterController.DamageInfo damage = CharacterController.DamageInfo.Create(_effectCodeStat, codeId, AttackerType.CHAPTER_RULE);

                        ruleTile.OccupiedCharacter.GetDamaged(damage, null, hexColor: "#FF470000");
                        InGameVfxManager.Instance.AddInGameTileFx(ElementType.FIRE, ruleTile);
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_hit_03,
                            ruleTile.View.CachedTr.position);
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_fire);
                    }
                }
                elapsedTime -= 1f;
            }
        }
    }
}
