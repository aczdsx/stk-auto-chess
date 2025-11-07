using Naninovel;
using Naninovel.Commands;

[InitializeAtRuntime]
public class ChangeNextRevealSpeedService : IStatefulService<GameStateMap>
{
    [System.Serializable]
    class GameState { public float Modifier; }

    public float NextRevealSpeedModifier { get; set; }

    public UniTask InitializeService() => UniTask.CompletedTask;

    public void ResetService() => NextRevealSpeedModifier = 1;

    public void DestroyService() { }

    public void SaveServiceState(GameStateMap stateMap)
    {
        var state = new GameState() { Modifier = NextRevealSpeedModifier };
        stateMap.SetState(state);
    }

    public UniTask LoadServiceState(GameStateMap stateMap)
    {
        NextRevealSpeedModifier = stateMap.GetState<GameState>()?.Modifier ?? 1;
        return UniTask.CompletedTask;
    }
}

[CommandAlias("s")]
public class ChangeNextRevealSpeedCommand : Command
{
    [ParameterAlias(NamelessParameterAlias), RequiredParameter]
    public DecimalParameter SpeedModifier;

    public override UniTask Execute(AsyncToken token = default)
    {
        Engine.GetService<ChangeNextRevealSpeedService>().NextRevealSpeedModifier = SpeedModifier;
        return UniTask.CompletedTask;
    }
}

[CommandAlias("print")]
public class MyCustomPrintCommand : PrintText
{
    protected override float AssignedRevealSpeed => base.AssignedRevealSpeed *
                                                    Engine.GetService<ChangeNextRevealSpeedService>().NextRevealSpeedModifier;

    public override async UniTask Execute(AsyncToken token = default)
    {
        await base.Execute(token);
        Engine.GetService<ChangeNextRevealSpeedService>().NextRevealSpeedModifier = 1f;
    }
}