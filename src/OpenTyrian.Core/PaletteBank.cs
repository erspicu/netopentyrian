namespace OpenTyrian.Core;

public sealed class PaletteBank
{
    public const int ColorsPerPalette = 256;
    public const int BytesPerColor = 3;
    public const int BytesPerPalette = ColorsPerPalette * BytesPerColor;

    public PaletteBank(IList<PaletteColor[]> palettes)
    {
        Palettes = palettes;
    }

    public IList<PaletteColor[]> Palettes { get; }

    public int Count => Palettes.Count;
}
