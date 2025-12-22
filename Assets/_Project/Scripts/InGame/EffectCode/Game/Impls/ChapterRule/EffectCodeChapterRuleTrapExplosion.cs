using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Google.Protobuf.WellKnownTypes;
using Unity.Mathematics;
using UnityEditor.Localization.Plugins.XLIFF.V20;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]

    public class EffectCodeChapterRuleTrapExplosion : EffectCodeGameBase
    {
        //위 클래스의 codeinfo는 0 데미지 1~n은 생성될 타일인덱스로 관리.
        private const int CodeId = (int)EffectCodeNameType.CHAPTER_TRAP;
        private const string DamageColor = "#FF550000";
        private const InGameVfxNameType TrapBodyVfxEnum = InGameVfxNameType.fx_common_trap_explosion;

        private const InGameVfxNameType ExplosionVfxEnum = InGameVfxNameType.fx_common_hit_02;

        //폭발 범위는 해당 타일 기준 얼마나 떨어져있는지 변수. 현재 3x3을 터트리고자 1을 사용.
        private int _explosionRange = 1;

        //트랩 데미지
        private float _effectCodeStat;

        //Dictionary Key: 트랩타일, Value: 밟으면 사라져야할 Vfx
        private InGameVfx _targetTileVfx;

        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            var InGameObejctManagerInstance = InGameObjectManager.Instance;
            var InGameVfxManagerInstance = InGameVfxManager.Instance;

            InGameTile inGameTile = InGameObejctManagerInstance.GetInGameTile(codeInfo.GetCodeStatToInt(0));
            var TrapBodyVfx = InGameVfxManagerInstance.AddInGameVfx(TrapBodyVfxEnum, inGameTile.View.CachedTr.position);

            _targetTileVfx = TrapBodyVfx;
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);

            SetRuleTileByInfo(codeInfo);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            SetRuleTileByInfo(codeInfo);
        }

        public override void OnTileMoveEnd(InGameTile tile, CharacterController character)
        {
            //펑 터져서 지워야할 타일들 목록.
            var InGameObjectManagerInstance = InGameObjectManager.Instance;
            var InGameVfxManagerInstance = InGameVfxManager.Instance;


            //여기까지오면 펑 터지기 수행. 타일의 Vfx를 우선 제거한다.
            _targetTileVfx.Remove();

            var damage = CharacterController.DamageInfo.Create(_effectCodeStat, codeId, AttackerType.CHAPTER_RULE);
            var explosionTiles = InGameObjectManagerInstance.InGameGrid.GetTileListByShapeSquare(tile, _explosionRange);

            foreach (var explosionTile in explosionTiles)
            {
                explosionTile.OccupiedCharacter?.GetDamaged(damage, null, hexColor: DamageColor);
                InGameVfxManagerInstance.AddInGameVfx(ExplosionVfxEnum, explosionTile.View.CachedTr.position);
            }
        }
    }
}
