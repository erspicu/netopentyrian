namespace OpenTyrian.Core;

public sealed class MenuState
{
    private readonly MenuDefinition _definition;

    public MenuState(MenuDefinition definition, int selectedIndex = 0)
    {
        _definition = definition;
        SelectedIndex = Clamp(selectedIndex, 0, Math.Max(0, definition.Items.Count - 1));
    }

    public int SelectedIndex { get; private set; }

    public MenuItemDefinition SelectedItem => _definition.Items[SelectedIndex];

    public void SetSelectedIndex(int selectedIndex)
    {
        if (_definition.Items.Count == 0)
        {
            SelectedIndex = 0;
            return;
        }

        SelectedIndex = Clamp(selectedIndex, 0, _definition.Items.Count - 1);
    }

    public void MovePrevious()
    {
        if (_definition.Items.Count == 0)
        {
            return;
        }

        int start = SelectedIndex;
        do
        {
            SelectedIndex = SelectedIndex == 0 ? _definition.Items.Count - 1 : SelectedIndex - 1;
        }
        while (!_definition.Items[SelectedIndex].IsEnabled && SelectedIndex != start);
    }

    public void MoveNext()
    {
        if (_definition.Items.Count == 0)
        {
            return;
        }

        int start = SelectedIndex;
        do
        {
            SelectedIndex = SelectedIndex == _definition.Items.Count - 1 ? 0 : SelectedIndex + 1;
        }
        while (!_definition.Items[SelectedIndex].IsEnabled && SelectedIndex != start);
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
