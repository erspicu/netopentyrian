namespace OpenTyrian.Platform;

public readonly record struct InputSnapshot(
    bool Up,
    bool Down,
    bool Left,
    bool Right,
    bool Confirm,
    bool Cancel)
{
    public bool PointerPresent { get; init; }

    public int PointerX { get; init; }

    public int PointerY { get; init; }

    public bool PointerConfirm { get; init; }

    public bool PointerCancel { get; init; }

    public bool IsDown(InputButton button)
    {
        return button switch
        {
            InputButton.Up => Up,
            InputButton.Down => Down,
            InputButton.Left => Left,
            InputButton.Right => Right,
            InputButton.Confirm => Confirm,
            InputButton.Cancel => Cancel,
            _ => false,
        };
    }
}
