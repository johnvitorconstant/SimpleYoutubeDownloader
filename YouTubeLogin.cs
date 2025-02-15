using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleYoutubeDownloader
{
    public class YouTubeLogin : Form
    {
        private WebView2 webView;
        private Button btnSalvar;

        public YouTubeLogin()
        {
            InitializeComponents();
        }

        private async void InitializeComponents()
        {
            this.Width = 800;
            this.Height = 600;
            this.Text = "Login no YouTube";

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

            // Inicializa o WebView2 e navega para a página de login do Google/YouTube
          
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.CookieManager.DeleteAllCookies();
            //webView.CoreWebView2.Reload();
            webView.CoreWebView2.Navigate("https://accounts.google.com/ServiceLogin?service=youtube");
        }

        private async void BtnSalvar_Click(object sender, EventArgs e)
        {
            try
            {
                // Obtém os cookies do domínio youtube.com
                var cookieManager = webView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.youtube.com");

                // Converte os cookies para uma lista de System.Net.Cookie para facilitar o uso
                var cookieList = cookies.Select(c => new Cookie(c.Name, c.Value, c.Path, c.Domain)).ToList();

                // Serializa os cookies para JSON (você pode escolher outro formato, se preferir)
                string json = JsonSerializer.Serialize(cookieList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("cookies.json", json);

                MessageBox.Show("Cookies salvos com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar os cookies: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
