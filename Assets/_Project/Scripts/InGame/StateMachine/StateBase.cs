using System;
using Cysharp.Threading.Tasks;

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
        protected async UniTaskVoid StartDrawingLinesAsync(float intervalITime)
        {
            while (InGameMainFlowManager.Instance.CurrentFlowState is StateReadyBase)
            {
                InGameObjectManager.Instance.DrawPlayerLine(true);
            
                await UniTask.Delay(TimeSpan.FromSeconds(intervalITime));
            
                if (InGameMainFlowManager.Instance.CurrentFlowState is not StateReadyBase)
                    break;
            
                InGameObjectManager.Instance.DrawPlayerLine(false);

                await UniTask.Delay(TimeSpan.FromSeconds(intervalITime));
            }
        }
    }
}
