namespace OpenTyrian.Core;

public interface IScene
{
    IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds);

    void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds);
}
