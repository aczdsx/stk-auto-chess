using CookApps.AutoBattler;
using CookApps.BattleSystem;
using LitMotion;
using UnityEngine;

public class CharacterStateAssassinFirstMove : CharacterStateBase
{
    private const float ScanTargetInterval = 0.1f;
    private float scanTargetTime = 0f;
    public override StatePriority StatePriority => StatePriority.Move;

    public override void StateStart()
    {
        base.StateStart();
        isBlockingChangeState = true;
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.IDLE);
        scanTargetTime = ScanTargetInterval;

        var moveDuration = SpecOptionCache.DefaultAssassinFirstMoveSpeed;

        characCtrl.Target = InGameObjectManager.Instance.GetFarthestTargetByOnce(characCtrl);

        InGameTile tile = InGameObjectManager.Instance.InGameGrid.GetTileForAssassin(characCtrl);

        InGameVfxNameType assassinFxType = (characCtrl.AllianceType == AllianceType.Player)
            ? InGameVfxNameType.fx_common_assassin_awful
            : InGameVfxNameType.fx_common_assassin_enemy;

        InGameVfxManager.Instance.AddInGameVfx(assassinFxType, characCtrl.GetCharacterView().CachedTr.position);


        if (tile != null)
        {
            characCtrl.ChangeOccupiedTile(tile);

            LMotion.Create(
                characCtrl.Position3D,
                characCtrl.CurrentTile.View.Position,
                moveDuration)
                .WithEase(Ease.Linear)
                .WithOnComplete(() =>
                {
                    if (characCtrl == null)
                        return;

                    characCtrl.GetCharacterView().LookAt(tile, characCtrl.Target.CurrentTile);
                    characCtrl.Position3D = tile.View.Position;
                    characCtrl.GetCharacterView().CachedTr.localPosition = tile.View.Position;

                    if (characCtrl.AllianceType == AllianceType.Player)
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_summon_awful,
                            tile.View.CachedTr.position);
                    else if (characCtrl.AllianceType == AllianceType.Enemy)
                    {
                        InGameVfxManager.Instance.AddInGameVfx(assassinFxType, tile.View.CachedTr.position);
                    }

                    characCtrl.AddNextState<CharacterStateIdle>();
                    isBlockingChangeState = false;
                })
                .Bind(value =>
                {
                    if (characCtrl != null)
                        characCtrl.Position3D = value;
                });
        }
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }
}
