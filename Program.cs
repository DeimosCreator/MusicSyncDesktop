using Microsoft.Extensions.Configuration;
using MusicSyncDesktop;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Collections.Generic;

class Program
{
    [STAThread] // Обязательно для запуска UI в STA контексте
    static void Main()
    {
        // Запуск приложения в STA потоке
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Загружаем конфигурацию
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var localPath = config["Music:LocalPath"];
        var token = config["Yandex:Token"];
        var yandexPath = config["Yandex:DiskUploadPath"];
        var clientId = config["Yandex:ClientId"];

        // Запуск окна авторизации
        string authUrl = $"https://oauth.yandex.ru/authorize?response_type=token&client_id={clientId}&redirect_uri=https://oauth.yandex.ru/verification_code";
        using (var authForm = new AuthForm(authUrl))
        {
            var result = authForm.ShowDialog();  // Эта операция будет выполнена в STA потоке

            if (result == DialogResult.OK)
            {
                // Успех – читаем токен
                token = authForm.AccessToken;
                UpdateAppSettings("Yandex:Token", token);
            }
            else
            {
                MessageBox.Show("Авторизация отменена.");
            }
        }

        // Если токен пустой, просим пользователя вручную ввести его
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

        // Запрашиваем действие пользователя
        Console.WriteLine("Type 'upload' to upload music or 'download' to download:");
        var action = Console.ReadLine()?.Trim().ToLower();

        var syncService = new MusicSyncService(localPath, token, yandexPath);

        // Выполняем синхронизацию в зависимости от команды
        if (action == "upload")
        {
            syncService.SyncAsync().Wait();
            Console.WriteLine("Upload completed.");
        }
        else if (action == "download")
        {
            syncService.DownloadAllAsync().Wait();
            Console.WriteLine("Download completed.");
        }
        else if (action == "list")
        {
            syncService.ListFilesAsync().Wait();
        }
        else
        {
            Console.WriteLine("Unknown command.");
        }

        Console.WriteLine("Done. Press Enter to exit.");
        Console.ReadLine();
    }

    // Обновление настроек в appsettings.json
    static void UpdateAppSettings(string key, string value)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        // Чтение JSON
        var json = File.ReadAllText(path);
        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

        // Проверяем, что есть нужный раздел и ключ
        var sections = key.Split(':');
        var section = sections[0];
        var subkey = sections[1];

        if (!jsonObj.ContainsKey(section))
        {
            jsonObj[section] = new Dictionary<string, string>();
        }

        jsonObj[section][subkey] = value;

        // Перезаписываем JSON в файл
        var newJson = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, newJson);
    }
}
