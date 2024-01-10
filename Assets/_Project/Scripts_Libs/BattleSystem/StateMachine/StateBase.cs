namespace CookApps.TeamBattle.BattleSystem
{
    public abstract class StateBase
    {
        public abstract void StateInit(object owner);
        public abstract void StateStart();
        public abstract void StateRunning(float dt);
        public abstract void StateEnd(bool isForced);
    }
}
