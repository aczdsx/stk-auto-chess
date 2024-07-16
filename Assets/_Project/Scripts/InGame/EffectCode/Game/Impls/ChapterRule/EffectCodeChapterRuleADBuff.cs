using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleAD : EffectCodeGameBase
    {
        private List<InGameTile> _chapterRuleTiles = new();
        private float _effectCodeStat;
        private const int CodeId = (int) EffectCodeNameType.RULE_AD;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(0);
            _chapterRuleTiles.Clear();
            for (var i = 1; i < codeInfo.StatsLength; i++)
            {
                int tileID = codeInfo.GetCodeStatToInt(i);
                InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
                _chapterRuleTiles.Add(inGameTile);

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_bufftrap_ad,
                    inGameTile.View.CachedTr.position);
            }
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(0);
            _chapterRuleTiles.Clear();
            for (var i = 1; i < codeInfo.StatsLength; i++)
            {
                int tileID = codeInfo.GetCodeStatToInt(i);
                InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
                _chapterRuleTiles.Add(inGameTile);

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_bufftrap_ad,
                    inGameTile.View.CachedTr.position);
            }
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            // 현재 챕터 스테이트가 Combat이 아니라.
            // FlowStateStageReady, FlowStateStageStart등등.. 일때 작동하게 하지 않기.
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat))
            {
                return;
            }

            // InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID)...
            // _chapterRuleTiles.Add(inGameTile)...
            // 위의 챕터 생성된 타일과, OnTileCharacterEnter 된 타일과 일치 하는지 안하는지 에러 체크

            if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
            {
            }
        }

        public override void OnUpdate(float dt)
        {
            return;
        }

        public virtual void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            return;
        }
    }
}
