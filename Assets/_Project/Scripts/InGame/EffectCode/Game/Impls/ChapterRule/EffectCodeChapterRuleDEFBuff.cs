using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleDEFBuff : EffectCodeGameBase
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

        private float _defUpRate;
        private const int CodeId = (int)EffectCodeNameType.RULE_DEF;

        private List<InGameTile> _chapterRuleTiles = new();
        private Dictionary<int, BuffInfo> _characterByEffectCode = new();

        private void SetRuleTileByInfo(EffectCodeInfo codeInfo, InGameVfxNameType vnt)
        {
            for (var i = 1; i < codeInfo.StatsLength; i++)
            {
                var tileID = codeInfo.GetCodeStatToInt(i);
                var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
                _chapterRuleTiles.Add(inGameTile);

                InGameVfxManager.Instance.AddInGameVfx(vnt, inGameTile.View.CachedTr.position);
            }
        }

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
            _defUpRate = codeInfo.GetCodeStatToInt(0) * 0.01f;
            _chapterRuleTiles.Clear();
            SetRuleTileByTEST(new List<int> { 6, 8 }, InGameVfxNameType.fx_common_bufftrap_defense);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _defUpRate = codeInfo.GetCodeStatToInt(0) * 0.01f;
            _chapterRuleTiles.Clear();
            SetRuleTileByTEST(new List<int> { 6, 8 }, InGameVfxNameType.fx_common_bufftrap_defense);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            Debug.LogWarning($"[Enter Tile] {character.CharacterId} : ({tile.X}, {tile.Y})");

            // 현재 챕터 스테이트가 Combat이 아니라.
            // FlowStateStageReady, FlowStateStageStart등등.. 일때 작동하게 하지 않기.
            // 하지만, Combat이전에 버프를 받던지 아니던지 해야한다.
            // if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)) return;

            // InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID)...
            // _chapterRuleTiles.Add(inGameTile)...
            // 위의 챕터 생성된 타일과, OnTileCharacterEnter 된 타일과 일치 하는지 안하는지 에러 체크
            if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
            {
                // Key는 유저 Value 이펙트 인포로 적용 해제를 레퍼런스.
                // 추가하고 실행

                if (character.AllianceType == AllianceType.Player &&
                    !_characterByEffectCode.ContainsKey(character.CharacterId))
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = CodeId;
                    eccStats[1] = 99999f;
                    eccStats[2] = _defUpRate;

                    Debug.LogWarning($"에펙트 배율 {_defUpRate}");

                    var effectCodeID = (long)EffectCodeNameType.BUFF_DEF_PERCENT_UP;
                    var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats); // 근데 이펙트 코드는 Struct이다.

                    // TODO!! 임시 VFX 확인용!!!
                    var ingameBuffVFX = InGameVfxManager.Instance.AddInGameVfx(
                        InGameEnumExtensions.GetLoopVfxName(BuffDebuffType.Shield),
                        character.SkillRootTransformFollowable);

                    var addBuffInfo = new BuffInfo(effectCodeInfo, ingameBuffVFX);

                    _characterByEffectCode.Add(character.CharacterId, addBuffInfo);
                    character.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, null); // Null 에러 발생

                    // InGameEnumExtensions.GetSoundFx(BuffDebuffType.DefenceUp);
                    Debug.LogWarning(
                        $"{character.CharacterId} + 버프 추가! | {addBuffInfo.Vfx.name} : 이펙트! | 방어력 : {character.DEF}");
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

            // 하지만, Combat이전에 버프를 받던지 아니던지 해야한다.
            // if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)) return;

            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                // 딕셔너리에서 캐릭터 검색
                // 이펙트 코드 리무브 하기
                if (character.AllianceType == AllianceType.Player &&
                    _characterByEffectCode.ContainsKey(character.CharacterId))
                {
                    var removeBuffInfo = _characterByEffectCode[character.CharacterId];
                    character.GetEffectCodeContainer().RemoveEffectCode(removeBuffInfo.Info.CodeId);
                    _characterByEffectCode.Remove(character.CharacterId);
                    InGameVfxManager.Instance.RemoveInGameVfx(removeBuffInfo.Vfx);
                    Debug.LogWarning(
                        $"{character.CharacterId}  버프 제거!! | {removeBuffInfo.Vfx.name} : 이펙트 제거!! | 방어력 : {character.DEF}");
                    removeBuffInfo = null;
                }
                else
                {
                    Debug.LogWarning("이미 캐릭터가 버프를 받지 않고있음");
                }
            }
        }
    }
}