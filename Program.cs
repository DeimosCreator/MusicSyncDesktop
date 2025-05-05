using Microsoft.Extensions.Configuration;
using MusicSyncDesktop;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var localPath = config["Music:LocalPath"];
        var token = config["Yandex:Token"];
        var yandexPath = config["Yandex:DiskUploadPath"];
        var clientId = config["Yandex:ClientId"];

        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Требуется авторизация. Открываем окно входа...");
            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://oauth.yandex.ru/authorize?response_type=token&client_id={clientId}&redirect_uri=https://oauth.yandex.ru/verification_code",
                UseShellExecute = true
            });
            Console.WriteLine("После входа вставьте сюда access_token:");
            token = Console.ReadLine();
            UpdateAppSettings("Yandex:Token", token);
        }

        Console.WriteLine("Type 'upload' to upload music or 'download' to download:");
        var action = Console.ReadLine()?.Trim().ToLower();

        var syncService = new MusicSyncService(localPath, token, yandexPath);

        if (action == "upload")
        {
            await syncService.SyncAsync();
            Console.WriteLine("Upload completed.");
        }
        else if (action == "download")
        {
            await syncService.DownloadAllAsync();
            Console.WriteLine("Download completed.");
        }
        else
        {
            Console.WriteLine("Unknown command.");
        }

        Console.WriteLine("Done. Press Enter to exit.");
        Console.ReadLine();
    }

    static void UpdateAppSettings(string key, string value)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var json = File.ReadAllText(path);
        var jsonObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
        var section = key.Split(':')[0];
        var subkey = key.Split(':')[1];
        jsonObj[section][subkey] = value;
        var newJson = System.Text.Json.JsonSerializer.Serialize(jsonObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, newJson);
    }
}
