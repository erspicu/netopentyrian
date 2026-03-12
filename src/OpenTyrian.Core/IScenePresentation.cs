namespace OpenTyrian.Core;

public interface IScenePresentation
{
    int? BackgroundPictureNumber { get; }

    SceneMusicKind? MusicOverride { get; }
}
