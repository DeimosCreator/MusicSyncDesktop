using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

namespace MusicSynsDesktop.Yandex
{
    public class YandexDiskClient
    {
        private readonly HttpClient _client;

        public YandexDiskClient(string token)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", token);
        }

        public async Task<bool> UploadFileAsync(string localFilePath, string remotePath)
        {
            // Правильное экранирование пути
            string encodedPath = EncodeRemotePath(remotePath);

            Console.WriteLine("Uploading to: " + encodedPath);

            // Получаем ссылку на загрузку
            var uploadUrl = $"https://cloud-api.yandex.net/v1/disk/resources/upload?path={encodedPath}&overwrite=true";
            Console.WriteLine("Requesting upload URL: " + uploadUrl);

            var uploadUrlResponse = await _client.GetAsync(uploadUrl);
            if (!uploadUrlResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Upload URL request failed: " + uploadUrlResponse.StatusCode);
                return false;
            }

            var json = await uploadUrlResponse.Content.ReadAsStringAsync();
            Console.WriteLine("Upload URL JSON: " + json);

            var href = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("href").GetString();

            // Загружаем файл
            using var fileStream = File.OpenRead(localFilePath);
            var putResult = await _client.PutAsync(href, new StreamContent(fileStream));

            Console.WriteLine("Upload result: " + putResult.StatusCode);
            return putResult.IsSuccessStatusCode;
        }

        private string EncodeRemotePath(string path)
        {
            var segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Uri.EscapeDataString(segments[i]);
            }
            return string.Join("/", segments);
        }



    }
}
