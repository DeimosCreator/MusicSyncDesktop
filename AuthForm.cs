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
            // Создаём и добавляем лейбл загрузки
            loadingLabel = new Label
            {
                Text = "Загрузка...",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point((ClientSize.Width / 2) - 50, 20),
                Anchor = AnchorStyles.Top
            };
            Controls.Add(loadingLabel);

            // Создаём и добавляем WebBrowser
            web = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true
            };
            web.Navigating += Web_Navigating;
            web.DocumentCompleted += Web_DocumentCompleted;
            web.Navigated += Web_Navigated;

            Controls.Add(web);
            web.BringToFront(); // Чтобы браузер не перекрыл лейбл

            web.Navigate(authUrl);
        }

        private void Web_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            loadingLabel.Visible = true;
        }

        private void Web_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            loadingLabel.Visible = false;
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
