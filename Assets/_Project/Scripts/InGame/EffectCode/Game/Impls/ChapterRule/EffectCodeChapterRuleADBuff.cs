using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleAD : EffectCodeGameBase
    {
        public class BuffInfo
        {
            public EffectCodeInfo Info { get; set; }
            public InGameVfx Vfx { get; set; }

            public BuffInfo(EffectCodeInfo info, InGameVfx vfx)
            {
                Info = info;
                Vfx = vfx;
            }
        }

        private float _effectCodeStat;
        private const int CodeId = (int) EffectCodeNameType.RULE_AD;

        private List<InGameTile> _chapterRuleTiles = new();
        private Dictionary<CharacterController, BuffInfo> _characterByEffectCode = new();

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
            {``
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
                // Key는 유저 Value 이펙트 인포로 적용 해제를 레퍼런스.
                // 추가하고 실행

                if (!_characterByEffectCode.ContainsKey(character))
                {
                    Span<double> eccStats = stackalloc double[0];
                    eccStats.Clear();
                    eccStats[0] = _effectCodeStat;

                    var effectCodeID = (long) CodeId;
                    var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats); // 근데 이펙트 코드는 Struct이다.
                    var addBuffInfo = new BuffInfo(new EffectCodeInfo(effectCodeID, 0, eccStats),
                        InGameVfxManager.Instance.AddInGameVfx(
                            InGameEnumExtensions.GetLoopVfxName(BuffDebuffType.AttackUp),
                            character.SkillRootTransformFollowable));
                    _characterByEffectCode.Add(character, addBuffInfo);
                    character.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, null);
                }
                else
                {
                    Debug.LogWarning("이미 캐릭터가 버프를 받고있음");
                }
            }
        }

        public virtual void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                // 딕셔너리에서 캐릭터 검색
                // 이펙트 코드 리무브 하기
                if (!_characterByEffectCode.ContainsKey(character))
                {
                    BuffInfo removeBuffInfo = _characterByEffectCode[character];
                    character.GetEffectCodeContainer().RemoveEffectCode(removeBuffInfo.Info.CodeId);
                    _characterByEffectCode.Remove(character);
                    InGameVfxManager.Instance.RemoveInGameVfx(removeBuffInfo.Vfx);
                }
                else
                {
                    Debug.LogWarning("이미 캐릭터가 버프를 받고있음");
                }
            }
        }
    }
}
