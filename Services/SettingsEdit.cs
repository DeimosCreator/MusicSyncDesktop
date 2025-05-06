using MusicSyncDesktop.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace MusicSyncDesktop.Services
{
    public class SettingsEdit
    {
        private readonly AppSettings _appSettings;

        public SettingsEdit(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public void StartSettingsEditor()
        {
            Console.WriteLine("Редактор настроек. Выберите, что хотите изменить:");
            Console.WriteLine("  login  - Переавторизоваться и получить новый токен");
            Console.WriteLine("  local  - Изменить путь к локальной папке");
            Console.WriteLine("  remote - Изменить путь на Яндекс.Диск");
            Console.Write("Ваш выбор: ");

            string input = Console.ReadLine()?.Trim().ToLower();

            switch (input)
            {
                case "login":
                    ReAuthenticate();
                    break;
                case "local":
                    ChangeLocalPath();
                    break;
                case "remote":
                    ChangeRemotePath();
                    break;
                default:
                    Console.WriteLine("Неизвестная опция.");
                    break;
            }
        }

        private void ReAuthenticate()
        {
            BrowserCacheCleaner.FullClearInternetExplorerData();
            Thread.Sleep(1000);

            var clientId = _appSettings.Get("Yandex:ClientId");
            string authUrl = $"https://oauth.yandex.ru/authorize?response_type=token&client_id={clientId}&redirect_uri=https://oauth.yandex.ru/verification_code&prompt=login";

            using (var authForm = new AuthForm(authUrl))
            {
                var result = authForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string token = authForm.AccessToken;
                    _appSettings.Update("Yandex:Token", token);
                    Console.WriteLine("Токен успешно обновлён.");
                }
                else
                {
                    Console.WriteLine("Авторизация отменена.");
                }
            }
        }

        private void ChangeLocalPath()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите локальную папку для синхронизации музыки";
                dialog.ShowNewFolderButton = true;

                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    _appSettings.Update("Settings:LocalPath", dialog.SelectedPath);
                    Console.WriteLine($"Локальный путь обновлён: {dialog.SelectedPath}");
                }
                else
                {
                    Console.WriteLine("Изменение локального пути отменено.");
                }
            }
        }


        private void ChangeRemotePath()
        {
            Console.Write("Введите новый путь на Яндекс.Диск (например, /MusicSync): ");
            string remotePath = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(remotePath))
            {
                _appSettings.Update("Yandex:Path", remotePath);
                Console.WriteLine($"Путь на Яндекс.Диск обновлён: {remotePath}");
            }
            else
            {
                Console.WriteLine("Изменение пути на Яндекс.Диск отменено.");
            }
        }
    }
}
