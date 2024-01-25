using CookApps.SampleTeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;

public class TitleTask_LoadManagers : ITitleTask
{
    private bool isComplete;

    public ITitleTaskPriority Priority => ITitleTaskPriority.Step_0;

    private ProgressCallback progressCallback;

    public void Initialize(TitleMain titleMainUI, ProgressCallback progressCallback)
    {
        this.progressCallback = progressCallback;
        progressCallback.Invoke(GetHashCode(), 0f);
    }

    public async UniTask RunTask()
    {
        SceneUIManager.OnStartChangeScene += SceneLoadingTask.HandleLoading;
        await AtlasManager.Instance.Initialize("Data/AtlasManager.asset");
        isComplete = true;
        progressCallback.Invoke(GetHashCode(), 1f);
    }

    public (bool, string) HasError()
    {
        if (!isComplete)
        {
            return (true, "아직 처리중");
        }

        return (false, null);
    }

    public async UniTask HandleError()
    {
    }

    public T GetResult<T>()
    {
        return default;
    }
}
