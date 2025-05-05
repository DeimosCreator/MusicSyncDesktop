using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MusicSyncDesktop
{
    public class MusicSyncService
    {
        private readonly string _localPath;
        private readonly string _token;
        private readonly string _yandexPath;

        public MusicSyncService(string localPath, string token, string yandexPath)
        {
            _localPath = localPath;
            _token = token;
            _yandexPath = yandexPath;
        }

        public async Task SyncAsync()
        {
            // Загрузка файлов на Яндекс.Диск
            var filesToUpload = Directory.GetFiles(_localPath);
            foreach (var file in filesToUpload)
            {
                await UploadFileAsync(file);
            }
        }

        public async Task DownloadAllAsync()
        {
            // 1. Получаем список файлов с Яндекс.Диска
            var fileUrls = await GetFileUrlsFromDiskAsync();

            // 2. Скачиваем файлы
            foreach (var fileUrl in fileUrls)
            {
                // 3. Скачиваем каждый файл в локальную папку
                await DownloadFileAsync(fileUrl);
            }
        }

        private async Task<List<string>> GetFileUrlsFromDiskAsync()
        {
            var fileUrls = new List<string>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"OAuth {_token}");

                // Кодируем путь правильно
                string encodedPath = Uri.EscapeUriString(_yandexPath);
                var requestUrl = $"https://cloud-api.yandex.net/v1/disk/resources?path={encodedPath}";

                // Получаем список ресурсов
                var response = await client.GetStringAsync(requestUrl);

                if (response != null)
                {
                    try
                    {
                        // Разбираем JSON-ответ
                        var jsonResponse = JsonConvert.DeserializeObject<YandexDiskResponse>(response);
                        if (jsonResponse != null)
                        {
                            foreach (var resource in jsonResponse.Items)
                            {
                                if (resource.Type == "file")
                                {
                                    fileUrls.Add(resource.PublicUrl); // Можно также использовать ресурс для получения прямого URL
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Ошибка парсинга JSON: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Не удалось получить ответ от Яндекс.Диска.");
                }
            }

            return fileUrls;
        }


        private async Task DownloadFileAsync(string fileUrl)
        {
            var fileName = Path.GetFileName(fileUrl);
            var downloadPath = Path.Combine(_localPath, fileName);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"OAuth {_token}");
                var response = await client.GetAsync(fileUrl);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(downloadPath, fileBytes);
                    Console.WriteLine($"Файл {fileName} успешно скачан.");
                }
                else
                {
                    Console.WriteLine($"Ошибка при скачивании {fileName}. Код ошибки: {response.StatusCode}");
                }
            }
        }

        private async Task UploadFileAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var yandexFilePath = Uri.EscapeDataString(Path.Combine(_yandexPath, fileName)); // Путь на Яндекс.Диске

            // Получаем URL для загрузки файла
            var uploadUrl = await GetUploadUrlAsync(yandexFilePath);

            // Загружаем файл
            if (uploadUrl != null)
            {
                await UploadFileToYandexAsync(uploadUrl, filePath);
            }
        }

        private async Task<string> GetUploadUrlAsync(string yandexFilePath)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"OAuth {_token}");

                // Запрашиваем URL для загрузки файла на Яндекс.Диск
                var response = await client.GetStringAsync($"https://cloud-api.yandex.net/v1/disk/resources/upload?path={yandexFilePath}&overwrite=true");

                // Преобразуем ответ в объект
                var uploadInfo = JsonConvert.DeserializeObject<UploadInfo>(response);
                return uploadInfo?.Href; // Возвращаем ссылку для загрузки
            }
        }

        private async Task UploadFileToYandexAsync(string uploadUrl, string localFilePath)
        {
            using (var client = new HttpClient())
            {
                // Читаем файл для отправки
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(localFilePath));
                fileContent.Headers.Add("Content-Type", "application/octet-stream");

                // Отправляем файл на Яндекс.Диск
                var response = await client.PutAsync(uploadUrl, fileContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Файл {localFilePath} успешно загружен на Яндекс.Диск.");
                }
                else
                {
                    Console.WriteLine($"Ошибка при загрузке файла {localFilePath}. Код ошибки: {response.StatusCode}");
                }
            }
        }

        // Класс для парсинга ответа от Яндекс.Диска
        public class UploadInfo
        {
            public string Href { get; set; }
        }

    }

    public class YandexDiskResponse
    {
        public List<YandexDiskItem> Items { get; set; }
    }

    public class YandexDiskItem
    {
        public string Type { get; set; }
        public string PublicUrl { get; set; }
    }

}