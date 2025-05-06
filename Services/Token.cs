using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicSyncDesktop.Services
{
    public class Token
    {
        AppSettings _appSettings;
        public Token(AppSettings appSettings) 
        { 
            _appSettings = appSettings;
        }

        public static string GetToken(AppSettings settings)
        {
            string token = string.Empty;
            var clientId = settings.Get("Yandex:ClientId");
            string authUrl = $"https://oauth.yandex.ru/authorize?response_type=token&client_id={clientId}&redirect_uri=https://oauth.yandex.ru/verification_code";

            using (var authForm = new AuthForm(authUrl))
            {
                var result = authForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string raw = authForm.AccessToken;
                    string tokenOnly = raw.Split('&')[0];
                    token = tokenOnly;
                    Console.WriteLine("Успешная авторизация.");
                    settings.Update("Yandex:Token", token);
                    return token;
                }
                else
                {
                    MessageBox.Show("Авторизация не выполнена. Токен не получен.");
                    return string.Empty;
                }
            }
        }
    }
}
