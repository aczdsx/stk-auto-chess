using System.Collections.Generic;
using Naninovel;
using Naninovel.Commands;

[CommandAlias("forceHighlight")]
public class NaninovelForceHighlightCommand : Command
{
    [RequiredParameter, ParameterAlias(NamelessParameterAlias)]
    public StringListParameter CharacterIds;

    [RequiredParameter] public BooleanParameter Enable;
    public StringParameter Tint;
    public DecimalParameter Time = .35f;

    public override UniTask Execute(AsyncToken token = default)
    {
        var charManager = Engine.GetService<ICharacterManager>();
        var tasks = new List<UniTask>();
        foreach (var id in CharacterIds)
        {
            var meta = charManager.Configuration.GetMetadataOrDefault(id);
            meta.HighlightWhenSpeaking = Enable;

            if (Assigned(Tint))
            {
                var command = new ModifyCharacter
                {
                    IdAndAppearance = new NamedString(id, null), TintColor = Tint, Duration = Time
                };
                tasks.Add(command.Execute(token));
            }
        }

        return UniTask.WhenAll(tasks);
    }
}