using System;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]

    public class EffectCodeBattleItemDynamite : EffectCodeGameBase
    {
        //위 클래스의 codeinfo는 0 데미지 1~n은 생성될 타일인덱스로 관리.
        private const int CodeId = (int)EffectCodeNameType.BATTLE_ITEM_DYNAMITE;
        private CharacterController _bombController = null;

        private const InGameVfxNameType ExplosionVfxEnum = InGameVfxNameType.fx_common_asterism_ts_bomb_01;

        //폭발 범위는 해당 타일 기준 얼마나 떨어져있는지 변수. 현재 3x3을 터트리고자 1을 사용.
        private int _explosionRange = 1;

        //트랩 데미지
        private float _effectCodeStat;
        private int _killLogSynergyID;


        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            var InGameObejctManagerInstance = InGameObjectManager.Instance;
            InGameTile inGameTile = InGameObejctManagerInstance.GetInGameTile(codeInfo.GetCodeStatToInt(0));
            
            _bombController = inGameTile.OccupiedCharacter;

            inGameTile.SetUnoccupied();
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            SetRuleTileByInfo(codeInfo);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _killLogSynergyID = codeInfo.GetCodeStatToInt(2);
            
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            SetRuleTileByInfo(codeInfo);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _killLogSynergyID = codeInfo.GetCodeStatToInt(2);
        }

        public override void OnTileMoveEnd(InGameTile tile, CharacterController character)
        {
            if (character.AllianceType == AllianceType.Player || character.AllianceType == AllianceType.Neutral)
                return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_shooter_mine);
            //펑 터져서 지워야할 타일들 목록.
            var InGameObjectManagerInstance = InGameObjectManager.Instance;
            var InGameVfxManagerInstance = InGameVfxManager.Instance;
            var floorDamage = Math.Floor(_effectCodeStat);

            var damage = CharacterController.DamageInfo.Create(floorDamage, _killLogSynergyID, AttackerType.SYNERGY_STAR_ASTERISM);
            var explosionTiles = InGameObjectManagerInstance.InGameGrid.GetTileListByShapeSquare(tile, _explosionRange);

            foreach (var explosionTile in explosionTiles)
            {
                if(explosionTile.OccupiedCharacter is null ||
                explosionTile.OccupiedCharacter.AllianceType == AllianceType.Player||
                explosionTile.OccupiedCharacter.AllianceType == AllianceType.BattleItem||
                explosionTile.OccupiedCharacter.AllianceType == AllianceType.Neutral)
                    continue;
                
                explosionTile.OccupiedCharacter.GetDamaged(damage, _bombController);
            }
            InGameVfxManagerInstance.AddInGameVfx(ExplosionVfxEnum, tile.View.CachedTr.position);

            // _bombController가 null이 아니고 아직 필드에 있는 경우에만 제거
            if (_bombController != null)
            {
                // GetCharacterInField로 캐릭터가 아직 필드에 있는지 확인
                var characterInField = InGameObjectManagerInstance.GetCharacterInField(_bombController.CharacterUId);
                if (characterInField == _bombController)
                {
                    InGameObjectManagerInstance.RemoveCharacterFromField(_bombController);
                }
            }
            
            tile.EffectCodeContainer.RemoveEffectCode(codeId);
        }
    }
}
