using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MusicSyncDesktop
{
    public class MusicSyncService
    {
        private readonly string _localPath;
        private readonly string _token;
        private readonly string _yandexPath;
        private readonly HttpClient _client;

        // Добавляем конструктор для инициализации клиента с таймаутом
        public MusicSyncService(string localPath, string token, string yandexPath)
        {
            _localPath = localPath;
            _token = token;
            _yandexPath = yandexPath;

            // Инициализация HttpClient с таймаутом
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        public async Task SyncAsync()
        {
            // Проверяем и создаём папку, если она не существует
            await CreateDirectoryIfNotExistsAsync(_yandexPath);

            // Загрузка файлов на Яндекс.Диск
            var filesToUpload = Directory.GetFiles(_localPath);
            foreach (var file in filesToUpload)
            {
                await UploadFileAsync(file);
            }
        }

        public async Task DownloadAllAsync()
        {
            List<string> fileUrls;
            try
            {
                fileUrls = await GetFileUrlsFromDiskAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Network error fetching file list: " + ex.Message);
                return;
            }

            foreach (var fileUrl in fileUrls)
            {
                try
                {
                    await DownloadFileAsync(fileUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading '{fileUrl}': {ex.Message}");
                }
            }
        }

        private async Task<List<string>> GetFileUrlsFromDiskAsync()
        {
            var fileUrls = new List<string>();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", _token);

            string encodedPath = Uri.EscapeDataString(_yandexPath);
            var requestUrl = $"https://cloud-api.yandex.net/v1/disk/resources?path={encodedPath}&limit=1000";

            try
            {
                var response = await _client.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка при получении списка файлов. Статус: {response.StatusCode}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<JObject>(responseContent);

                var items = json["_embedded"]?["items"] as JArray;
                if (items == null || items.Count == 0)
                {
                    Console.WriteLine("Полученные данные не содержат файлов или они имеют неверный формат.");
                    return fileUrls;
                }

                foreach (var item in items)
                {
                    if (item["type"]?.ToString() == "file")
                    {
                        var path = item["path"]?.ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            fileUrls.Add(path);
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Ошибка при выполнении HTTP-запроса: " + ex.Message);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка парсинга JSON: {ex.Message}");
            }

            return fileUrls;
        }


        private async Task DownloadFileAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var downloadPath = Path.Combine(_localPath, fileName);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", _token);

            // Получение ссылки на скачивание
            var encodedPath = Uri.EscapeDataString(filePath);
            var requestUrl = $"https://cloud-api.yandex.net/v1/disk/resources/download?path={encodedPath}";

            var response = await _client.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка при получении ссылки на скачивание для {fileName}. Код ошибки: {response.StatusCode}");
                return;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var downloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(responseContent);
            if (downloadInfo?.Href == null)
            {
                Console.WriteLine($"Не удалось получить ссылку на скачивание для {fileName}.");
                return;
            }

            // Скачивание файла
            var fileBytes = await _client.GetByteArrayAsync(downloadInfo.Href);
            await File.WriteAllBytesAsync(downloadPath, fileBytes);
            Console.WriteLine($"Файл {fileName} успешно скачан.");
        }

        private class DownloadInfo
        {
            [JsonProperty("href")]
            public string Href { get; set; }
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
            // Используем _client для запроса URL загрузки
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", _token);

            var response = await _client.GetStringAsync($"https://cloud-api.yandex.net/v1/disk/resources/upload?path={yandexFilePath}&overwrite=true");

            // Преобразуем ответ в объект
            var uploadInfo = JsonConvert.DeserializeObject<UploadInfo>(response);
            return uploadInfo?.Href; // Возвращаем ссылку для загрузки
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

        public async Task ListFilesAsync()
        {
            Console.WriteLine($"Listing files in '{_yandexPath}':");
            try
            {
                // Обновляем путь: убираем завершающий '/'
                var path = _yandexPath.TrimEnd('/');

                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("OAuth", _token);

                var limit = 1000;
                var encodedPath = Uri.EscapeUriString(path);
                var requestUrl =
                    $"https://cloud-api.yandex.net/v1/disk/resources?path={encodedPath}&limit={limit}&fields=items.name,items.path,items.type";

                var response = await _client.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to list files: {response.StatusCode}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var diskResponse = JsonConvert.DeserializeObject<YandexDiskResponse>(json);
                if (diskResponse?.Items == null || diskResponse.Items.Count == 0)
                {
                    Console.WriteLine("No items found.");
                    return;
                }

                foreach (var item in diskResponse.Items)
                {
                    Console.WriteLine(item.Type == "file"
                        ? $"[FILE] {item.Path}"
                        : $"[DIR ] {item.Path}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Network error listing files: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error listing files: " + ex.Message);
            }
        }


        private async Task CreateDirectoryIfNotExistsAsync(string yandexDirectoryPath)
        {
            // Используем _client для проверки и создания папки
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", _token);

            var response = await _client.GetAsync($"https://cloud-api.yandex.net/v1/disk/resources?path={Uri.EscapeDataString(yandexDirectoryPath)}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var createResponse = await _client.PutAsync($"https://cloud-api.yandex.net/v1/disk/resources?path={Uri.EscapeDataString(yandexDirectoryPath)}", null);
                if (createResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Папка '{yandexDirectoryPath}' была успешно создана.");
                }
                else
                {
                    Console.WriteLine($"Ошибка при создании папки '{yandexDirectoryPath}'. Код ошибки: {createResponse.StatusCode}");
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
        public string Path { get; set; }
    }
}
