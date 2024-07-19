using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using UnityEngine.TextCore.Text;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleAD : EffectCodeGameBase
    {
        private float _atkUpRate;
        private const long BuffEffectCodeID = (long)EffectCodeNameType.BUFF_AD_PERCENT_UP;
        private const int CodeId = (int)EffectCodeNameType.RULE_AD;

        private List<InGameTile> _chapterRuleTiles = new();
        private List<CharacterController> _characterControllers  = new();

        private void SetRuleTileByTEST(List<int> testRuleTile, InGameVfxNameType vnt)
        {
            foreach (var tileID in testRuleTile)
            {
                var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
                _chapterRuleTiles.Add(inGameTile);

                InGameVfxManager.Instance.AddInGameVfx(vnt, inGameTile.View.CachedTr.position);
            }
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _atkUpRate = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _chapterRuleTiles.Clear();
            
            SetRuleTileByTEST(new List<int> { 0, 4 }, InGameVfxNameType.fx_common_bufftrap_ad);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _atkUpRate = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _chapterRuleTiles.Clear();
            SetRuleTileByTEST(new List<int> { 0, 4 }, InGameVfxNameType.fx_common_bufftrap_ad);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            Debug.LogWarning($"[Enter Tile] {character.CharacterId} : ({tile.X}, {tile.Y})");

            if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
            {
                if (character.AllianceType != AllianceType.Wall)
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = CodeId;
                    eccStats[1] = 99999f;
                    eccStats[2] = _atkUpRate;

                    Debug.LogWarning($"에펙트 배율 {_atkUpRate}");

                    var effectCodeID = BuffEffectCodeID;
                    var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats); 

                    character.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, source);

                    _characterControllers.Add(character);
                    Debug.LogWarning($"{character.CharacterId} + 버프 추가!");
                }
                else
                {
                    Debug.LogWarning("이미 캐릭터가 버프를 받고있음");
                }
            }
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            Debug.LogWarning($"[Enter Tile] {character.CharacterId} : ({tile.X}, {tile.Y})");

            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                if (character.AllianceType != AllianceType.Wall)
                {
                    if(_characterControllers.Exists(c => c.CharacterUId == character.CharacterUId))
                    {
                        character.GetEffectCodeContainer().RemoveEffectCode(BuffEffectCodeID);
                    }
                    _characterControllers.Remove(character);
                }
                else
                {
                    Debug.LogWarning("이미 캐릭터가 버프를 받지 않고있음");
                }
            }
        }
    }
}