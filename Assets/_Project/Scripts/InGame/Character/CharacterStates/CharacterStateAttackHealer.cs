using CookApps.AutoBattler;
using CookApps.BattleSystem;

public class CharacterStateAttackHealer : CharacterStateAttack
{
    protected override void RunAttackAnimation()
    {
        Debug.Log("CharacterStateAttackHealer RunAttackAnimation");
        base.RunAttackAnimation();
    }

    public override void StateEnd(bool isForced)
    {
        Debug.Log("CharacterStateAttackHealer RunAttackAnimation");
        base.StateEnd(isForced);
    }
}

