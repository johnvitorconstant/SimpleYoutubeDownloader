using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace SimpleYoutubeDownloader;

/// <summary>Janela com WebView2 para login, verificação manual (captcha, etc.) e gravação da sessão YouTube.</summary>
public class YouTubeLogin : Form
{
    private WebView2 webView = null!;
    private TextBox txtUrl = null!;
    private Button btnGo = null!;
    private Button btnSalvar = null!;
    private readonly bool _sessionIsPrivate;
    private readonly string? _navigateOnLoad;
    private readonly string _hintText;
    private readonly string _titleText;

    /// <param name="navigateOnLoad">URL inicial (ex. página do vídeo). Null = página inicial do YouTube.</param>
    /// <param name="hintText">Texto de ajuda no topo.</param>
    /// <param name="titleText">Título da janela.</param>
    public YouTubeLogin(bool isPrivate, string? navigateOnLoad = null, string? hintText = null, string? titleText = null)
    {
        _sessionIsPrivate = isPrivate;
        _navigateOnLoad = string.IsNullOrWhiteSpace(navigateOnLoad) ? null : navigateOnLoad.Trim();
        _hintText = hintText ??
                    "Faça login no YouTube abaixo. Depois use «Salvar cookies e fechar» — a sessão fica encriptada na pasta do utilizador (AppData), não na pasta do projeto.";
        _titleText = titleText ?? "YouTube — sessão manual";
        FormClosing += YouTubeLogin_FormClosing;
        InitializeComponents();
    }

    private void YouTubeLogin_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (_sessionIsPrivate && webView.CoreWebView2 != null) webView.CoreWebView2.CookieManager.DeleteAllCookies();
        }
        catch
        {
            // ignored
        }
    }

    private async void InitializeComponents()
    {
        UiTheme.StyleForm(this);
        Text = _titleText;
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
            Text = _hintText,
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

        btnSalvar = new Button { Text = "Salvar cookies e fechar", Enabled = false };
        UiTheme.ApplyBootstrapPrimary(btnSalvar, fillWidth: true);
        btnSalvar.Click += BtnSalvar_Click;

        root.Controls.Add(hint, 0, 0);
        root.Controls.Add(topBar, 0, 1);
        root.Controls.Add(webView, 0, 2);
        root.Controls.Add(btnSalvar, 0, 3);

        Controls.Add(root);

        await webView.EnsureCoreWebView2Async(null);
        webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;

        var start = _navigateOnLoad ?? "https://www.youtube.com/";
        txtUrl.Text = start;
        webView.CoreWebView2.Navigate(start);

        btnSalvar.Enabled = true;
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        txtUrl.Text = webView.Source?.ToString() ?? "";
    }

    private void BtnGo_Click(object? sender, EventArgs e)
    {
        if (webView.CoreWebView2 != null && !string.IsNullOrWhiteSpace(txtUrl.Text))
            webView.CoreWebView2.Navigate(txtUrl.Text);
    }

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        if (webView.CoreWebView2 == null)
        {
            MessageBox.Show(this, "O browser ainda não está pronto. Aguarde um momento e tente de novo.", "Aguarde",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnSalvar.Enabled = false;
        try
        {
            var cookieManager = webView.CoreWebView2.CookieManager;
            var cookies = await CollectYoutubeCookiesAsync(cookieManager).ConfigureAwait(true);
            if (cookies.Count == 0)
            {
                MessageBox.Show(this,
                    "Não foram encontrados cookies do YouTube/Google. Abra youtube.com, inicie sessão se precisar, e volte a premir «Salvar».",
                    "Sem cookies",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            YouTubeSessionStore.SaveProtected(cookies);

            MessageBox.Show(this,
                "Sessão guardada de forma encriptada (só esta conta Windows) em:\r\n" + YouTubeSessionStore.EncryptedSessionDirectory,
                "Concluído",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erro: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSalvar.Enabled = true;
        }
    }

    /// <summary>O InnerTube precisa de cookies de vários domínios; um único GetCookiesAsync costuma ser incompleto.</summary>
    private static async Task<List<Cookie>> CollectYoutubeCookiesAsync(CoreWebView2CookieManager manager)
    {
        var uris = new[]
        {
            "https://www.youtube.com/",
            "https://youtube.com/",
            "https://www.google.com/",
            "https://accounts.google.com/"
        };

        var byKey = new Dictionary<string, CoreWebView2Cookie>(StringComparer.Ordinal);
        foreach (var uri in uris)
        {
            var batch = await manager.GetCookiesAsync(uri).ConfigureAwait(true);
            foreach (var c in batch)
            {
                var key = (c.Domain ?? "") + "\n" + (c.Path ?? "") + "\n" + (c.Name ?? "");
                byKey[key] = c;
            }
        }

        return byKey.Values.Select(ToNetCookie).ToList();
    }

    private static Cookie ToNetCookie(CoreWebView2Cookie c)
    {
        var path = string.IsNullOrEmpty(c.Path) ? "/" : c.Path;
        var domain = string.IsNullOrEmpty(c.Domain) ? ".youtube.com" : c.Domain;
        // Expires omisso: o tipo da propriedade varia entre versões do WebView2; YoutubeExplode usa sobretudo Name/Value/Domain.
        return new Cookie(c.Name, c.Value, path, domain) { Secure = c.IsSecure, HttpOnly = c.IsHttpOnly };
    }
}
