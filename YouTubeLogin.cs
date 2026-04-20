using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;

namespace SimpleYoutubeDownloader;

public class YouTubeLogin : Form
{
    private WebView2 webView = null!;
    private TextBox txtUrl = null!;
    private Button btnGo = null!;
    private Button btnSalvar = null!;
    private static bool IsPrivate;

    public YouTubeLogin(bool isPrivate)
    {
        IsPrivate = isPrivate;
        FormClosing += YouTubeLogin_FormClosing;
        InitializeComponents();
    }

    private void YouTubeLogin_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (IsPrivate && webView.CoreWebView2 != null) webView.CoreWebView2.CookieManager.DeleteAllCookies();
        }
        catch
        {
            // ignored
        }
    }

    private async void InitializeComponents()
    {
        UiTheme.StyleForm(this);
        Text = "Entrar no YouTube";
        MinimumSize = new Size(720, 520);
        Size = new Size(880, 640);
        StartPosition = FormStartPosition.CenterParent;
        DoubleBuffered = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12, 12, 12, 12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var hint = new Label
        {
            Text = "Faça login no YouTube abaixo. Em seguida use \"Salvar cookies\" para gravar em cookies.json.",
            AutoSize = true,
            MaximumSize = new Size(820, 0),
            Margin = new Padding(0, 0, 0, 10)
        };
        UiTheme.StyleLabel(hint, muted: true);

        var topBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Height = 36,
            Margin = new Padding(0, 0, 0, 10)
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        txtUrl = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            PlaceholderText = "https://www.youtube.com/…"
        };
        UiTheme.StyleTextBox(txtUrl);

        btnGo = new Button { Text = "Ir" };
        UiTheme.ApplyBootstrapOutlineSecondary(btnGo, UiTheme.Background);
        btnGo.Click += BtnGo_Click;

        topBar.Controls.Add(txtUrl, 0, 0);
        topBar.Controls.Add(btnGo, 1, 0);

        webView = new WebView2 { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        webView.BackColor = UiTheme.LogBackground;

        btnSalvar = new Button { Text = "Salvar cookies e fechar" };
        UiTheme.ApplyBootstrapPrimary(btnSalvar, fillWidth: true);
        btnSalvar.Click += BtnSalvar_Click;

        root.Controls.Add(hint, 0, 0);
        root.Controls.Add(topBar, 0, 1);
        root.Controls.Add(webView, 0, 2);
        root.Controls.Add(btnSalvar, 0, 3);

        Controls.Add(root);

        await webView.EnsureCoreWebView2Async(null);
        webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
        webView.CoreWebView2.Navigate("https://www.youtube.com/");
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        txtUrl.Text = webView.Source?.ToString() ?? "";
    }

    private void BtnGo_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(txtUrl.Text)) webView.CoreWebView2.Navigate(txtUrl.Text);
    }

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        try
        {
            var cookieManager = webView.CoreWebView2.CookieManager;
            var cookies = await cookieManager.GetCookiesAsync("https://www.youtube.com");
            var cookieList = cookies.Select(c => new Cookie(c.Name, c.Value, c.Path, c.Domain)).ToList();

            var json = JsonSerializer.Serialize(cookieList, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync("cookies.json", json);

            MessageBox.Show(this, "Cookies salvos em cookies.json.", "Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erro: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
