namespace OpenTyrian.Core;

public sealed class SceneResources
{
    public required int PaletteCount { get; init; }

    public IAudioCueSink? AudioCueSink { get; init; }

    public SaveSlotCatalog? SaveSlots { get; init; }

    public OpenTyrian.Platform.IInputConfigurator? InputConfigurator { get; init; }

    public OpenTyrian.Platform.IJoystickConfigurator? JoystickConfigurator { get; init; }

    public PicImage? TitleImage { get; init; }

    public PcxImage? TestPcxImage { get; init; }

    public Sprite2Sheet? TestSpriteSheet { get; init; }

    public MainShapeTables? MainShapeTables { get; init; }

    public TyrianFontRenderer? FontRenderer { get; init; }

    public required IList<EpisodeInfo> Episodes { get; init; }

    public GameplayTextInfo? GameplayText { get; init; }

    public ItemCatalog? ItemCatalog { get; init; }
}
