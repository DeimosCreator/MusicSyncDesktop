using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MusicSyncDesktop.Models
{
    public static class BrowserCacheCleaner
    {
        public static void FullClearInternetExplorerData()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = "InetCpl.cpl,ClearMyTracksByProcess 4351",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi);
        }

    }
}
