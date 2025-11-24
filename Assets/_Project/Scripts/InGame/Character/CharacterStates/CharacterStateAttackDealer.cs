using CookApps.AutoBattler;
using CookApps.BattleSystem;

public class CharacterStateAttackDealer : CharacterStateAttack
{
    protected override void RunAttackAnimation()
    {
        Debug.Log("CharacterStateAttackDealer RunAttackAnimation");
        base.RunAttackAnimation();
    }

    public override void StateEnd(bool isForced)
    {
        Debug.Log("CharacterStateAttackDealer RunAttackAnimation");
        base.StateEnd(isForced);
    }
}

