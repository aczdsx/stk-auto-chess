using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Google.Protobuf.WellKnownTypes;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Unity.Mathematics;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]

    /// <summary>
    /// 일정 시간마다 적군의 위치에 폭탄을 떨어뜨립니다.
    /// </summary>
    public partial class EffectCodeBattleItemDroppingBombs : EffectCodeGameBase
    {
        private const int CodeId = (int)EffectCodeNameType.DROPPING_BOMBS;
        private static readonly InGameVfxNameType ExplosionVfxEnum = InGameVfxNameType.fx_common_asterism_ts_bomb_02;
        private float _duration;
        private float _elapsedTime;
        private float _explosionDamage;
        private int _explosionOneTimeCount;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _duration = codeInfo.GetCodeStatToFloat(0);
            _explosionDamage = codeInfo.GetCodeStatToFloat(1);
            _explosionOneTimeCount = codeInfo.GetCodeStatToInt(2);

            _elapsedTime = 0f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _explosionDamage = codeInfo.GetCodeStatToFloat(1);
            _explosionOneTimeCount = codeInfo.GetCodeStatToInt(2);

            _elapsedTime = 0f;
        }

        public override void OnUpdate(float dt)
        {
            _elapsedTime += dt;
            if (_elapsedTime >= _duration)
            {
                _elapsedTime = 0f;
                DroppingBombs();
            }
        }

        private void DroppingBombs()
        {
            var enemyCharacterList = InGameObjectManager.Instance.GetCharacterList(allianceType: AllianceType.Enemy);
            var targetList = new List<CharacterController>();

            foreach (var enemy in enemyCharacterList)
            {
                if (enemy == null || enemy.IsAlive is false || enemy.CurrentTile == null)
                    continue;
                targetList.Add(enemy);
            }

            if (targetList.Count == 0)
                return;

            for (int i = 0; i < _explosionOneTimeCount; i++)
            {
                var randomTarget = targetList[InGameRandomManager.GetUniversalRandomValue(targetList.Count - 1)];
                var explosionTile = randomTarget.CurrentTile;
                InGameVfxManager.Instance.AddInGameVfx(ExplosionVfxEnum, explosionTile.View.CachedTr.position);

                CharacterController.DamageInfo damageInfo = new CharacterController.DamageInfo();
                damageInfo.damageAmount = Math.Floor(_explosionDamage);
                randomTarget. GetDamaged(damageInfo, null);
            }


        }
    }
}
