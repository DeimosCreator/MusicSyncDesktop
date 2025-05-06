using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicSyncDesktop
{
    public partial class AuthForm : Form
    {
        private readonly string authUrl;
        public string AccessToken { get; private set; }

        private WebBrowser web;
        private Label loadingLabel;

        public AuthForm(string authUrl)
        {
            InitializeComponent();
            this.authUrl = authUrl;
            this.Load += AuthForm_Load;
        }

        private void AuthForm_Load(object sender, EventArgs e)
        {
            // Лейбл загрузки
            loadingLabel = new Label
            {
                Text = "Загрузка...",
                Font = new Font("Arial", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point((ClientSize.Width - 100) / 2, 20),
                Anchor = AnchorStyles.Top
            };
            Controls.Add(loadingLabel);

            // WebBrowser
            web = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true
            };
            web.Navigating += (s, ev) => loadingLabel.Visible = true;
            web.DocumentCompleted += (s, ev) => loadingLabel.Visible = false;
            web.Navigated += Web_Navigated;

            Controls.Add(web);
            web.BringToFront();

            // Запускаем навигацию один раз
            web.Navigate(authUrl);
        }

        private void Web_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            var frag = e.Url.Fragment;
            if (!string.IsNullOrEmpty(frag) && frag.StartsWith("#access_token="))
            {
                AccessToken = frag.Substring("#access_token=".Length);
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
