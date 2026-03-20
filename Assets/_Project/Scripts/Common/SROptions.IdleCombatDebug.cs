using System.ComponentModel;

public partial class SROptions
{
    private bool _idleCombatDebugGUI;

    [Category("Idle 전투")]
    public bool Idle전투_디버그GUI
    {
        get => _idleCombatDebugGUI;
        set => _idleCombatDebugGUI = value;
    }
}
