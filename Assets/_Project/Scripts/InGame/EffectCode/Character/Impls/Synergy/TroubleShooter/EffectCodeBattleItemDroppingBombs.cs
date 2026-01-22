using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using Unity.Mathematics;
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
        private List<CharacterController> _reuseableBombTargetCharacters = new();
        private Vector3 JetPlanePosition = new Vector3(-1.19f, 1.0f, 5.97f);
        private float JetPlaneHeight = 0.9f;
        private int _killLogSynergyID;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            var x = InGameObjectManager.Instance.InGameGrid.Width - 1;
            var y = InGameObjectManager.Instance.InGameGrid.Height / 2;

            var tile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(x, y));
            JetPlanePosition = tile.View.CachedTr.position + Vector3.up * JetPlaneHeight;

            _duration = codeInfo.GetCodeStatToFloat(0);
            _explosionDamage = codeInfo.GetCodeStatToFloat(1);
            _explosionOneTimeCount = codeInfo.GetCodeStatToInt(2);
            _killLogSynergyID = codeInfo.GetCodeStatToInt(3);
            _elapsedTime = 0f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _explosionDamage = codeInfo.GetCodeStatToFloat(1);
            _explosionOneTimeCount = codeInfo.GetCodeStatToInt(2);
            _killLogSynergyID = codeInfo.GetCodeStatToInt(3);
            _elapsedTime = 0f;
        }

        public override void OnUpdate(float dt)
        {
            _reuseableBombTargetCharacters.Clear();
            _elapsedTime += dt;
            if (_elapsedTime >= _duration)
            {
                _elapsedTime = 0f;
                ShootJetPlane();
            }
        }


        private void ShootJetPlane()
        {
            //비행기 출발
            var jetPlaneVfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_jet_01, JetPlanePosition);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_shooter_ship);
            jetPlaneVfx.Initialize(false);
            if (jetPlaneVfx is InGameVfxWithAnimation)
            {
                var animatorVfx = jetPlaneVfx as InGameVfxWithAnimation;
                animatorVfx.SetOnVfxStartCallback(OnStartBomb);
                animatorVfx.SetOnVfxEndCallback(OnEndBomb);
                animatorVfx.SetOnCustomAnimationEventCallback((eventKey, positions) => OnMiddleBomb(eventKey, positions));
            }

        }

        private void ShootMissiles(IReadOnlyList<Transform> positions, int count)
        {
            if (positions == null || positions.Count == 0 || count <= 0)
                return;

            // 타겟 적군 리스트 갱신
            _reuseableBombTargetCharacters.Clear();
            var enemyCharacterList = InGameObjectManager.Instance.GetCharacterList(allianceType: AllianceType.Enemy);
            foreach (var enemy in enemyCharacterList)
            {
                if (enemy == null || enemy.IsAlive is false || enemy.CurrentTile == null)
                    continue;
                _reuseableBombTargetCharacters.Add(enemy);
            }

            //타겟없음.
            if (_reuseableBombTargetCharacters.Count == 0)
                return;

            // 미사일 발사
            for (int i = 0; i < count; i++)
            {
                // 발사 위치 선택 (positions가 여러 개면 순환 사용)
                int positionIndex = i % positions.Count;
                Transform launchPosition = positions[positionIndex];
                if (launchPosition == null)
                    continue;

                // 타겟 선택 (랜덤)
                int targetIndex = InGameRandomManager.GetUniversalRandomValue(_reuseableBombTargetCharacters.Count - 1);
                var target = _reuseableBombTargetCharacters[targetIndex];
                if (target == null || !target.IsAlive || target.CurrentTile == null)
                    continue;

                // 미사일 VFX 생성
                var missileVfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_missile_01, launchPosition.position);
                missileVfx.CachedTr.rotation = Quaternion.LookRotation(target.CurrentTile.View.CachedTr.position - launchPosition.position)
                * Quaternion.Euler(-90, 0, -90f);


                // 미사일 이동 설정 (무한대 모양 움직임)
                var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
                Vector3 targetPosition = target.CurrentTile.View.CachedTr.position;

                // 무한대 모양을 그리면서 이동하다가 목표 지점에 떨어지는 움직임
                movement.SetData(launchPosition.position, targetPosition, 10f);
                missileVfx.Initialize(false, movement);

                // 목표 도착 시 폭발 처리
                void OnMissileReachedTarget()
                {
                    missileVfx.Remove();

                    if (target != null && target.IsAlive && target.CurrentTile != null)
                    {
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_shooter_mine);
                        // 폭발 VFX
                        InGameVfxManager.Instance.AddInGameVfx(ExplosionVfxEnum,
                            target.CurrentTile.View.CachedTr.position);

                        // 데미지 처리
                        CharacterController.DamageInfo damageInfo = new CharacterController.DamageInfo();
                        damageInfo.attackerType = AttackerType.SYNERGY_STAR_ASTERISM;
                        damageInfo.source = _killLogSynergyID;
                        damageInfo.damageAmount = Math.Floor(_explosionDamage);
                        target.GetDamaged(damageInfo, null);
                    }
                }

                movement.OnReachedTarget += OnMissileReachedTarget;
            }
        }


        private void OnStartBomb(IReadOnlyList<Transform> positions)
        {
            Debug.Log("TS!! OnStartBomb");
            int missileCount = _explosionOneTimeCount / 3;
            ShootMissiles(positions, missileCount);
        }

        private void OnMiddleBomb(AnimationEventKey eventKey, IReadOnlyList<Transform> positions)
        {
            Debug.Log("TS!! OnMiddleBomb");
            int missileCount = _explosionOneTimeCount / 3 + _explosionOneTimeCount % 3;
            ShootMissiles(positions, missileCount);
        }
        
        private void OnEndBomb(IReadOnlyList<Transform> positions)
        {
            Debug.Log("TS!! OnEndBomb");
            int missileCount = _explosionOneTimeCount / 3;
            ShootMissiles(positions, missileCount);
        }
    }
}
