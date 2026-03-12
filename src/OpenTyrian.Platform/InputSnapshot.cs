namespace OpenTyrian.Platform;

public readonly record struct InputSnapshot(
    bool Up,
    bool Down,
    bool Left,
    bool Right,
    bool Confirm,
    bool Cancel)
{
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
