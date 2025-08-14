using Robust.Client.UserInterface;

namespace Content.Goobstation.Client.Fax.UI;

public sealed class FillableFormWindowBoundUi : BoundUserInterface
{
    [ViewVariables]
    private FillableFormWindow? _window;

    private bool _dialogIsOpen = false;

    public FillableFormWindowBoundUi(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<FillableFormWindow>();
    }
}
