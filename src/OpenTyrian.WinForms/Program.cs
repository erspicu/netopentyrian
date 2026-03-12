using OpenTyrian.Core;
using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string dataDirectory = TyrianDataDirectoryResolver.Resolve();
        var assetLocator = new FileSystemAssetLocator(dataDirectory);
        var inputSource = new WinFormsInputSource();
        var audioDevice = new SilentAudioDevice();
        var gameHost = new GameHost(assetLocator, inputSource, audioDevice);

        Application.Run(new MainForm(gameHost, inputSource));
    }
}
