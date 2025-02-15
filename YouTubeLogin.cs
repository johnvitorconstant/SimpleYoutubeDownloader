using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;



namespace SimpleYoutubeDownloader
{
    public class YouTubeLogin : Form
    {
        private WebView2 webView;
        private TextBox txtUrl;
        private Button btnGo;
        private Button btnSalvar;
        private static bool IsPrivate;
        public YouTubeLogin(bool isPrivate)
        {
            IsPrivate = isPrivate;
            this.FormClosing += YouTubeLogin_FormClosing;
            InitializeComponents();
        }

        private async void YouTubeLogin_FormClosing(object sender, FormClosingEventArgs e)
        {

            try
            {
                // Apagar todos os cookies do WebView2
                if (IsPrivate && webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.CookieManager.DeleteAllCookies();
                }

            }
            catch (Exception ex)
            {
            }
        }


        private async void InitializeComponents()
        {
            this.Width = 800;
            this.Height = 600;
            this.Text = "Login no YouTube";

            // Criar barra de endereços
            txtUrl = new TextBox()
            {
                Dock = DockStyle.Top,
                Height = 30
            };

            btnGo = new Button()
            {
                Text = "Go",
                Dock = DockStyle.Top,
                Height = 30
            };
            btnGo.Click += BtnGo_Click;

            webView = new WebView2()
            {
                Dock = DockStyle.Fill
            };

            btnSalvar = new Button()
            {
                Text = "Save Cookies",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            btnSalvar.Click += BtnSalvar_Click;

            this.Controls.Add(webView);
            this.Controls.Add(btnSalvar);
            this.Controls.Add(btnGo);
            this.Controls.Add(txtUrl);

            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2.Navigate("https://www.youtube.com/");
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            txtUrl.Text = webView.Source.ToString();
        }

        private void BtnGo_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                webView.CoreWebView2.Navigate(txtUrl.Text);
            }
        }

        private async void BtnSalvar_Click(object sender, EventArgs e)
        {
            try
            {
                var cookieManager = webView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.youtube.com");
                var cookieList = cookies.Select(c => new Cookie(c.Name, c.Value, c.Path, c.Domain)).ToList();

                string json = JsonSerializer.Serialize(cookieList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("cookies.json", json);

                MessageBox.Show("Cookies saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);


                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
