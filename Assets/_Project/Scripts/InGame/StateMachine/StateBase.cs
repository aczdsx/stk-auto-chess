namespace CookApps.BattleSystem
{
    public abstract class StateBase
    {
        public virtual void SetStateData(object data) {}
        public abstract void StateInit(object owner);
        public abstract void StateStart();
        public abstract void StateRunning(float dt);
        public abstract void StateEnd(bool isForced);
    }
    
    public abstract class StateCombatBase : StateBase
    {
    }
    
    public abstract class StateReadyBase : StateBase
    {
    }
}
