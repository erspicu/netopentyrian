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
        var gameHost = new GameHost(assetLocator);

        Application.Run(new MainForm(gameHost));
    }
}
