using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicSynsDesktop.Utils
{
    public static class FileScanner
    {
        public static List<string> GetMusicFiles(string path)
        {
            if (!Directory.Exists(path)) return new List<string>();
            return Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories).ToList();
        }
    }
}
