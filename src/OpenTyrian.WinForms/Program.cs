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
        var userFileStore = new FileSystemUserFileStore(AppDomain.CurrentDomain.BaseDirectory);
        var inputSource = new WinFormsInputSource();
        var audioDevice = new WaveOutAudioDevice();
        var gameHost = new GameHost(assetLocator, inputSource, audioDevice, userFileStore);

        Application.Run(new MainForm(gameHost, inputSource));
    }
}
