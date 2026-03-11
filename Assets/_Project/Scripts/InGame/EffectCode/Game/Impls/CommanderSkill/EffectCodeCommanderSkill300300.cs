using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

//3*3범위 내의 적의 공격력을 {0}초 동안 {1}% 감소 시킨다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeCommanderSkill300300 : EffectCodeCommanderSkillBase
    {
        private const int CodeId = 300300; // (int)EffectCodeNameType.COMMANDER_SKILL_EARTHEN_SOLDIERS;
        private const InGameVfxNameType _tileVfxName = InGameVfxNameType.fx_common_commander_skill_300;
        private const InGameVfxNameType _collisionVfxName = InGameVfxNameType.Skill_401021_1;

        private int _pushCount;
        private float _damageIfCollision;
        private float _stunDuration;
        private const float KNOCKBACK_TIME = 0.3f;

        private bool _colliderEnable = false;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            int userCommanderSkillLevel = codeInfo.GetCodeStatToInt(1);
            SpecDataManager specDataManager = SpecDataManager.Instance;
            _specTargetCommanderSkill = specDataManager.GetCommanderSkillListByUserSkillLevel(CodeId, userCommanderSkillLevel);

            var commanderSkillList = specDataManager.GetCommanderSkillDataList(CodeId);
            if (commanderSkillList == null || commanderSkillList.Count <= 0)
            {
                Debug.LogError($"CommanderSkillDataList is null or empty for CodeId: {CodeId}");
                return;
            }

            _pushCount = (int)_specTargetCommanderSkill.base_rate;
            _damageIfCollision = _specTargetCommanderSkill.base_rate_2;
            _stunDuration = _specTargetCommanderSkill.base_rate_3;

            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillAction();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            int userCommanderSkillLevel = codeInfo.GetCodeStatToInt(1);
            SpecDataManager specDataManager = SpecDataManager.Instance;
            _specTargetCommanderSkill = specDataManager.GetCommanderSkillListByUserSkillLevel(CodeId, userCommanderSkillLevel);

            var commanderSkillList = specDataManager.GetCommanderSkillDataList(CodeId);
            if (commanderSkillList == null || commanderSkillList.Count <= 0)
            {
                Debug.LogError($"CommanderSkillDataList is null or empty for CodeId: {CodeId}");
                return;
            }
            _pushCount = 2; //= (int)_specTargetCommanderSkill.base_rate;
            _damageIfCollision = _specTargetCommanderSkill.base_rate_2;
            _stunDuration = _specTargetCommanderSkill.base_rate_3;

            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));

            SkillAction();
        }

        protected override void SkillAction()
        {
            //흙의 병사 이펙트
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).ShakeCamera(0.4f, 0.15f);
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByRow(inGameTile,
             _specTargetCommanderSkill.commander_range_size / 2);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_commander_aegis);



            foreach (var tile in tileList)
            {
                var vfx = InGameVfxManager.Instance.AddInGameVfx(_tileVfxName, tile.View.CachedTr.position + Vector3.up * 2.4f);
                var animatorvfx = vfx as InGameVfxWithAnimation;

                // [InGame_New: removed] animatorvfx.OnCollisionWithTile += OnCollision2DEnter;

                animatorvfx.SetOnVfxStartCallback(OnVfxStart);
                animatorvfx.SetOnVfxEndCallback(OnVfxEnd);
            }
        }

        // [InGame_New: removed] private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
        // [InGame_New: removed] {
            // [InGame_New: removed] if(!_colliderEnable)
                // [InGame_New: removed] return;

            // [InGame_New: removed] if (tile.OccupiedCharacter == null || tile.CheckValidTile(AllianceType.Player, isCheckSameAllianceType: true)
            // [InGame_New: removed] || tile.OccupiedCharacter.IsAlive == false)
                // [InGame_New: removed] return;

            // [InGame_New: removed] Span<double> eccStats = stackalloc double[1];
            // [InGame_New: removed] eccStats.Clear();
            // [InGame_New: removed] eccStats[0] = _stunDuration;

            // [InGame_New: removed] EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, tile.OccupiedCharacter, eccStats, source);
            // [InGame_New: removed] ProcessKnockBack(tile);
        // [InGame_New: removed] }

        private void ProcessKnockBack(InGameTile TargetTile)
        {
            InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;
            Span<double> eccStats = stackalloc double[4];
            //해당 방향의 prev타일이 필요. 
            var directionTiles = inGameObjectManagerInstance.InGameGrid.GetTileListByDirectionInRange(
                TargetTile, dX: 0, dY: -1, count: 1);
            if (directionTiles.Count <= 0)
            {//데미지 줘야함.
                return;
            }

            var knockBackFinalTile = inGameObjectManagerInstance.InGameGrid.GetTileForKnockBack(directionTiles[0], TargetTile, _pushCount);

            int distance = inGameObjectManagerInstance.InGameGrid.GetManhattanDistance(knockBackFinalTile, TargetTile);
            if (distance < _pushCount)
            {
                ApplyCollisionVfxAsync(knockBackFinalTile, KNOCKBACK_TIME).Forget();
            }

            eccStats.Clear();
            eccStats[0] = KNOCKBACK_TIME;//knockBackTime
            eccStats[1] = 2.5f;//height
            eccStats[2] = knockBackFinalTile.View.ID;//tileID
            eccStats[3] = (int)Ease.OutExpo;//ease

            var knockbackEffectCodeBase = EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_KNOCKBACK, TargetTile.OccupiedCharacter, eccStats, source);

        }

        private void ApplyDamage(InGameTile TargetTile)
        {
            if (TargetTile.OccupiedCharacter == null) return;
            var damage = CharacterController.DamageInfo.Create(_damageIfCollision, codeId, AttackerType.COMMANDER_SKILL);
            TargetTile.OccupiedCharacter.GetDamaged(damage, null);
        }

        private async UniTaskVoid ApplyCollisionVfxAsync(InGameTile TargetTile, float second)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(second));

            InGameVfxManager.Instance.AddInGameVfx(_collisionVfxName, TargetTile.View.CachedTr.position);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(TargetTile, 1);
            foreach (var tile in tileList)
            {
                ApplyDamage(tile);
            }
        }

        public override InGameTile GetRecommendedTile(SkillCommander specCommanderSkillData)
        {
            InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;
            var enemyList = inGameObjectManagerInstance.GetCharacterListSortedByADDescending(AllianceType.Enemy, true);
            if (enemyList == null || enemyList.Count <= 0)
                return null;

            return enemyList[0].CurrentTile;
        }
        protected override void PromotionCommanderSkillCheck(PromotionLevelType firstPromotionLevel, PromotionLevelType secondPromotionLevel)
        {
        }

        private void OnVfxStart(IReadOnlyList<Transform> positions)
        {//콜라이더 껏다키기
            _colliderEnable = true;

        }
        private void OnVfxEnd(IReadOnlyList<Transform> positions)
        {//콜라이더 껏다키기
            _colliderEnable = false;
        }
    }
}
