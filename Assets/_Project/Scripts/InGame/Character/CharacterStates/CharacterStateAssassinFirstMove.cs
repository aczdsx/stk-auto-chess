using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;

public class CharacterStateAssassinFirstMove : CharacterStateBase
{
    private const float ScanTargetInterval = 0.1f;
    private float scanTargetTime = 0f;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.IDLE);
        scanTargetTime = ScanTargetInterval;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        characCtrl.Target = InGameObjectManager.Instance.GetFarthestEnemy(characCtrl);

        InGameTile tile = InGameObjectManager.Instance.InGameGrid.GetTileForAssassin(characCtrl.Target.CurrentTile);
        if (tile != null)
        {
            characCtrl.GetCharacterView().LookAt(tile, characCtrl.Target.CurrentTile);
            characCtrl.ChangeOccupiedTile(tile);
            characCtrl.Position3D = tile.View.Position;
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }
}
