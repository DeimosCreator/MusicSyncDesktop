using MusicSyncDesktop.Services;
using MusicSyncDesktop;
using System.Diagnostics;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var settings = new AppSettings();

        while (true)
        {
            var localPath = settings.Get("Music:LocalPath");
            var token = settings.Get("Yandex:Token");
            var yandexPath = settings.Get("Yandex:DiskUploadPath");
            var clientId = settings.Get("Yandex:ClientId");
            Console.WriteLine(clientId);

            if (string.IsNullOrWhiteSpace(token))
            {
                token = Token.GetToken(settings);
            }

            // Если не получили — спросим в консоли
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Требуется авторизация. Открываем окно входа...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://oauth.yandex.ru/authorize?response_type=token&client_id={clientId}&redirect_uri=https://oauth.yandex.ru/verification_code",
                    UseShellExecute = true
                });
                Console.WriteLine("После входа вставьте сюда access_token:");
                token = Console.ReadLine().Trim();
                settings.Update("Yandex:Token", token);
            }

            Console.WriteLine("Введите команду ('upload' — загрузка, 'download' — скачивание, 'list' — список файлов, 'settings' — настройки, 'exit' — выход):");
            var action = Console.ReadLine()?.Trim().ToLower();

            var syncService = new MusicSyncService(localPath, token, yandexPath);

            // Выполняем синхронизацию в зависимости от команды
            if (action == "upload")
            {
                syncService.SyncAsync().Wait();
                Console.WriteLine("Загрузка завершена.");
            }
            else if (action == "download")
            {
                syncService.DownloadAllAsync().Wait();
                Console.WriteLine("Скачивание завершено.");
            }
            else if (action == "list")
            {
                syncService.ListFilesAsync().Wait();
            }
            else if (action == "settings")
            {
                var appsettings = new AppSettings();
                var editor = new SettingsEdit(appsettings);
                editor.StartSettingsEditor();
            }
            else if (action == "exit")
            {
                break;
            }
            else
            {
                Console.WriteLine("Неизвестная команда.");
            }

            Console.WriteLine("Завершено. Нажмите Enter для выхода.");

            Console.ReadLine();
        }
    }
}
