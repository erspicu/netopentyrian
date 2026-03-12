namespace OpenTyrian.Core;

public sealed class PaletteBank
{
    public const int ColorsPerPalette = 256;
    public const int BytesPerColor = 3;
    public const int BytesPerPalette = ColorsPerPalette * BytesPerColor;

    public PaletteBank(IReadOnlyList<PaletteColor[]> palettes)
    {
        Palettes = palettes;
    }

    public IReadOnlyList<PaletteColor[]> Palettes { get; }

    public int Count => Palettes.Count;
}
