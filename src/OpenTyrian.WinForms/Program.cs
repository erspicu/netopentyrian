using OpenTyrian.Core;
using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        string dataDirectory = TyrianDataDirectoryResolver.Resolve();
        var assetLocator = new FileSystemAssetLocator(dataDirectory);
        var inputSource = new WinFormsKeyboardInputSource();
        var gameHost = new GameHost(assetLocator, inputSource);

        Application.Run(new MainForm(gameHost, inputSource));
    }
}
