using System;
using Cysharp.Threading.Tasks;

namespace CookApps.SampleTeamBattle
{
    public delegate void ProgressCallback(int hashCode, float progress);

    public interface ITitleTask
    {
        ITitleTaskPriority Priority { get; }

        void Initialize(Title titleUI, ProgressCallback getProgress);
        UniTask RunTask();
        (bool, string) HasError();
        UniTask HandleError();
        T GetResult<T>();
    }

    public enum ITitleTaskPriority
    {
        Step_0,
        Step_1,
        Step_2,
        Step_3,
        Step_4,
        Step_5,
        MAX,
    }
}
