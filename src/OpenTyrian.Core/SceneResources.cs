namespace OpenTyrian.Core;

public sealed class SceneResources
{
    public required int PaletteCount { get; init; }

    public PicImage? TitleImage { get; init; }

    public PcxImage? TestPcxImage { get; init; }

    public Sprite2Sheet? TestSpriteSheet { get; init; }

    public MainShapeTables? MainShapeTables { get; init; }

    public TyrianFontRenderer? FontRenderer { get; init; }

    public required IReadOnlyList<EpisodeInfo> Episodes { get; init; }

    public GameplayTextInfo? GameplayText { get; init; }

    public ItemCatalog? ItemCatalog { get; init; }
}
