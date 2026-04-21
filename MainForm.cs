using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace SimpleYoutubeDownloader;

public class MainForm : Form
{
    public MainForm(bool isPrivate)
    {
        AppFileLogger.EnsureInitialized();
        InitializeComponents(isPrivate);
        try
        {
            _isPrivate = isPrivate;
            if (!Directory.Exists(DownloadFolder)) Directory.CreateDirectory(DownloadFolder);
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}", ex);
        }
    }


    private void InitializeComponents(bool isPrivate)
    {
        _ = isPrivate;

        UiTheme.StyleForm(this);
        Text = "Simple YouTube Downloader";
        MinimumSize = new Size(760, 560);
        Size = new Size(940, 720);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        // Não definir Panel1MinSize/Panel2MinSize aqui: com altura 0 o SplitContainer recalcula
        // SplitterDistance e lança InvalidOperationException (app fecha sem abrir janela).
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BackColor = UiTheme.Background,
            SplitterIncrement = 12
        };
        split.Panel1.BackColor = UiTheme.Background;
        split.Panel2.BackColor = UiTheme.Background;

        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(16, 14, 16, 12)
        };
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitle = new Label
        {
            Text = "Simple YouTube Downloader",
            Font = UiTheme.TitleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        UiTheme.StyleLabel(lblTitle);

        var lblSubtitle = new Label
        {
            Text = "Baixe áudio ou vídeo a partir de links ou playlists. Os arquivos são salvos na pasta \"downloaded\".",
            AutoSize = true,
            MaximumSize = new Size(880, 0),
            Margin = new Padding(0, 0, 0, 10)
        };
        UiTheme.StyleLabel(lblSubtitle, muted: true);

        var lblLinks = new Label
        {
            Text = "Links (um por linha)",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };
        UiTheme.StyleLabel(lblLinks);

        _txtInput = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            AcceptsTab = false,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
            PlaceholderText = "https://www.youtube.com/watch?v=…\nhttps://youtu.be/…\nhttps://www.youtube.com/playlist?list=…"
        };
        UiTheme.StyleTextBox(_txtInput);

        var optionsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12),
            Height = 44
        };
        optionsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        optionsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var flowFormats = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 0, 0)
        };

        var lblFormat = new Label { Text = "Formato:", AutoSize = true, Margin = new Padding(0, 8, 10, 0) };
        UiTheme.StyleLabel(lblFormat, muted: true);
        flowFormats.Controls.Add(lblFormat);

        _formatCombo = new ComboBox
        {
            Width = 160,
            Margin = new Padding(0, 2, 0, 0)
        };
        UiTheme.StyleComboBox(_formatCombo);
        _formatCombo.Items.AddRange(new[] { "MP3", "MP4", "WebM" });
        _formatCombo.SelectedItem = "MP4";
        flowFormats.Controls.Add(_formatCombo);

        _lblUrlKind = new Label { Text = "—" };
        UiTheme.StyleUrlKindTag(_lblUrlKind);
        flowFormats.Controls.Add(_lblUrlKind);

        _txtInput.TextChanged += (_, _) => UpdateUrlKindTag();

        var parallelPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };
        var lblParallel = new Label { Text = "Downloads paralelos:", AutoSize = true, Margin = new Padding(0, 6, 8, 0) };
        UiTheme.StyleLabel(lblParallel, muted: true);
        _downloadsNumber = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = 3,
            Width = 64,
            Margin = new Padding(0, 2, 0, 0)
        };
        UiTheme.StyleNumeric(_downloadsNumber);
        parallelPanel.Controls.Add(lblParallel);
        parallelPanel.Controls.Add(_downloadsNumber);

        optionsRow.Controls.Add(flowFormats, 0, 0);
        optionsRow.Controls.Add(parallelPanel, 1, 0);

        var toolbarBg = UiTheme.SurfaceElevated;
        var buttonRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            BackColor = toolbarBg,
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(0, 4, 0, 0)
        };

        _btnDownload = new Button { Text = "Baixar" };
        UiTheme.ApplyBootstrapPrimary(_btnDownload);
        _btnDownload.Click += BtnDownload_Click;

        _btnCancelar = new Button { Text = "Cancelar" };
        UiTheme.ApplyBootstrapOutlineDanger(_btnCancelar, toolbarBg);
        _btnCancelar.Click += BtnCancel_Click;

        var btnLogin = new Button { Text = "YouTube (login / verificação)" };
        UiTheme.ApplyBootstrapOutlineSecondary(btnLogin, toolbarBg);
        btnLogin.Click += BtnLogin_Click;

        var btnOpenOutput = new Button { Text = "Abrir pasta de saída" };
        UiTheme.ApplyBootstrapLink(btnOpenOutput, toolbarBg);
        btnOpenOutput.Click += BtnOpenOutputFolder_Click;

        buttonRow.Controls.Add(_btnDownload);
        buttonRow.Controls.Add(_btnCancelar);
        buttonRow.Controls.Add(btnLogin);
        buttonRow.Controls.Add(btnOpenOutput);

        topLayout.Controls.Add(lblTitle, 0, 0);
        topLayout.Controls.Add(lblSubtitle, 0, 1);
        topLayout.Controls.Add(lblLinks, 0, 2);
        topLayout.Controls.Add(_txtInput, 0, 3);
        topLayout.Controls.Add(optionsRow, 0, 4);
        topLayout.Controls.Add(buttonRow, 0, 5);

        split.Panel1.Controls.Add(topLayout);

        var logLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16, 8, 16, 14)
        };
        logLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        logLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblLog = new Label
        {
            Text = "Registro",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };
        UiTheme.StyleLabel(lblLog, muted: true);

        _txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            TabStop = false
        };
        UiTheme.StyleTextBox(_txtLog, readOnly: true, monospace: true);

        logLayout.Controls.Add(lblLog, 0, 0);
        logLayout.Controls.Add(_txtLog, 0, 1);
        split.Panel2.Controls.Add(logLayout);

        Controls.Add(split);

        UpdateUrlKindTag();

        Load += (_, _) =>
        {
            try
            {
                split.SuspendLayout();
                var h = split.ClientSize.Height;
                if (h <= split.SplitterWidth + 80) return;

                var panel2Min = Math.Min(140, Math.Max(72, h / 5));
                var panel1Min = Math.Min(220, Math.Max(96, h / 4));
                split.Panel2MinSize = panel2Min;
                split.Panel1MinSize = panel1Min;

                var maxDist = h - split.Panel2MinSize - split.SplitterWidth;
                var minDist = split.Panel1MinSize;
                if (maxDist >= minDist)
                    split.SplitterDistance = Math.Clamp((int)(h * 0.46), minDist, maxDist);
            }
            catch
            {
                // ignored
            }
            finally
            {
                split.ResumeLayout();
            }
        };
    }


    private static readonly string DownloadFolder = "downloaded";
    private static bool _isPrivate;

    private CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>Contagem de falhas <see cref="VideoUnavailableException"/> no último lote (para oferecer janela manual).</summary>
    private int _videoUnavailableHits;

    /// <summary>Contagem de falhas HTTP 400/401 do InnerTube no último lote (bloqueio/limitação da biblioteca).</summary>
    private int _httpBlockedHits;

    private int _processed;
    private int _skipped;
    private int _error;

    private TextBox? _txtInput;
    private NumericUpDown? _downloadsNumber;
    private ComboBox? _formatCombo;
    private Label? _lblUrlKind;
    private Button? _btnDownload;
    private Button? _btnCancelar;
    private TextBox? _txtLog;

  


    private void BtnOpenOutputFolder_Click(object? sender, EventArgs e)
    {
        try
        {
            var path = Path.GetFullPath(DownloadFolder);
            Directory.CreateDirectory(path);
            var args = "\"" + path.Replace("\"", "") + "\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = args,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log(ex.Message, ex);
        }
    }

    private void BtnLogin_Click(object? sender, EventArgs e) => OpenYouTubeManualSession();

    /// <summary>Abre WebView2: login, verificação manual, etc.; grava sessão encriptada em AppData e em memória.</summary>
    private void OpenYouTubeManualSession()
    {
        var startUrl = GetFirstYouTubeUrlFromInput() ?? "https://www.youtube.com/";
        var hint =
            "Use esta janela para o que o YouTube pedir: iniciar sessão, verificação (captcha), aceitar avisos ou abrir o vídeo. " +
            "Quando estiver pronto, use «Salvar cookies e fechar» e volte a premir «Baixar».";
        using var loginForm = new YouTubeLogin(
            _isPrivate,
            navigateOnLoad: startUrl,
            hintText: hint,
            titleText: "YouTube — resolução manual");
        if (loginForm.ShowDialog() == DialogResult.OK)
            Log("Sessão YouTube guardada (AppData encriptada + memória). Pode tentar «Baixar» de novo.");
    }

    private string? GetFirstYouTubeUrlFromInput()
    {
        if (_txtInput == null) return null;
        var line = _txtInput.Text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .FirstOrDefault(s => s.Length > 0);
        if (line == null) return null;
        line = line.Replace("https://youtu.be/", "https://www.youtube.com/watch?v=", StringComparison.Ordinal);
        if (line.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            return line;
        return null;
    }

    private void NoteVideoUnavailable(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            if (e is VideoUnavailableException)
            {
                Interlocked.Increment(ref _videoUnavailableHits);
                return;
            }
        }
    }

    private void NoteHttpBlocked(Exception ex)
    {
        if (MayRetryWithoutSessionCookies(ex))
            Interlocked.Increment(ref _httpBlockedHits);
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        //  cancellationTokenSource = new CancellationTokenSource();
        // var cancellationToken = cancellationTokenSource.Token;

        _cancellationTokenSource.Cancel();
        Log("Cancelled");
    }


    private async void BtnDownload_Click(object sender, EventArgs e)
    {
        try
        {
            var watch = Stopwatch.StartNew();
            watch.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;


            var ext = (_formatCombo?.SelectedItem as string)?.ToLowerInvariant();
            var fileType = ext is "mp3" or "mp4" or "webm" ? ext : "mp4";


            _btnDownload.Enabled = false;
            _processed = 0;
            _skipped = 0;
            _error = 0;

            _txtLog.Clear();
            Interlocked.Exchange(ref _videoUnavailableHits, 0);
            Interlocked.Exchange(ref _httpBlockedHits, 0);

            var semaphore = new SemaphoreSlim(decimal.ToInt32(_downloadsNumber.Value));

            var urls = _txtInput.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length == 0)
            {
                Log("Please insert one or more youtube links");
                _btnDownload.Enabled = true;
                return;
            }

            Log($"Início do lote: {urls.Length} URL(s), formato={fileType}, paralelo={_downloadsNumber!.Value}, pasta={Path.GetFullPath(DownloadFolder)}");

            var (youtube, usingCookies, loadedCookies) = await CreateYoutubeClientAsync(cancellationToken);
            Log(usingCookies
                ? "YouTube: sessão guardada (memória ou ficheiro encriptado em AppData; ver pasta em «Entrar no YouTube»)."
                : HasLegacyPlainCookiesHint()
                    ? "YouTube: cookies.json legado ao lado do .exe está vazio ou inválido; a pedir como anónimo."
                    : "YouTube: sem sessão guardada. Se o YouTube bloquear pedidos anónimos, use «Entrar no YouTube» e guarde os cookies.");
            if (usingCookies)
                Log(YouTubeSessionStore.Summarize(loadedCookies));

            var tasks = urls.Select(url =>
                    ProcessUrlAsync(youtube, usingCookies, url, fileType, cancellationToken, semaphore))
                .ToArray();
            await Task.WhenAll(tasks);
            _btnDownload.Enabled = true;

            var vuHits = Interlocked.Exchange(ref _videoUnavailableHits, 0);
            var blockedHits = Interlocked.Exchange(ref _httpBlockedHits, 0);

            if (blockedHits > 0)
            {
                Log(FormatYouTubeBlockedExplanation(blockedHits, usingCookies));
            }
            else if (vuHits > 0)
            {
                Log($"Aviso: {vuHits} falha(s) com «vídeo não disponível» (muitas vezes bloqueio/verificação do YouTube).");
                var open = MessageBox.Show(
                    this,
                    "O YouTube pode estar a pedir verificação ou sessão (sem sessão o pedido parece um bot).\r\n\r\n" +
                    "Quer abrir o browser integrado para resolver manualmente e gravar a sessão?\r\n\r\n" +
                    "Depois use outra vez «Baixar».",
                    "YouTube — verificação manual",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (open == DialogResult.Yes)
                    OpenYouTubeManualSession();
            }

            Log("Download(s) finalizado(s).");
            Log($"Completed: {_processed}");
            Log($"Skipped:  {_skipped}");
            Log($"Error:  {_error}");
            watch.Stop();
            var elapsed = (decimal)(1 + watch.ElapsedMilliseconds / 1000);
            Log($"Elapsed Time: {elapsed} seconds");
            Log($"Avg speed: {_processed * 60 / elapsed} vídeos per minute");
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}", ex);
        }
    }

    private static async Task<IReadOnlyList<PlaylistVideo>> MaterializePlaylistAsync(YoutubeClient client, string url,
        CancellationToken cancellationToken)
    {
        var list = new List<PlaylistVideo>();
        await foreach (var video in client.Playlists.GetVideosAsync(url, cancellationToken))
            list.Add(video);
        return list;
    }

    private async Task ProcessUrlAsync(YoutubeClient youtube, bool batchUsingCookies, string url, string fileType,
        CancellationToken cancellationToken, SemaphoreSlim semaphore)
    {
        url = url.Replace("https://youtu.be/", "https://www.youtube.com/watch?v=");

        //
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsPlaylistUrl(url))
            {
                IReadOnlyList<PlaylistVideo> videos;
                YoutubeClient listClient;
                bool allowDownload400Fallback;

                if (batchUsingCookies)
                {
                    try
                    {
                        listClient = youtube;
                        videos = await MaterializePlaylistAsync(listClient, url, cancellationToken);
                        allowDownload400Fallback = true;
                    }
                    catch (Exception ex) when (MayRetryWithoutSessionCookies(ex))
                    {
                        Log("HTTP 400 ou 401 ao ler playlist com cookies. A tentar lista sem sessão…");
                        listClient = new YoutubeClient();
                        videos = await MaterializePlaylistAsync(listClient, url, cancellationToken);
                        allowDownload400Fallback = false;
                    }
                }
                else
                {
                    listClient = youtube;
                    videos = await MaterializePlaylistAsync(listClient, url, cancellationToken);
                    allowDownload400Fallback = false;
                }

                Log($"Playlist: {url} ({videos.Count} vídeo(s))");

                var videoTasks = videos.Select(video =>
                    DownloadVideoOrAudioAsync(listClient, allowDownload400Fallback, video.Title, video.Url, fileType,
                        cancellationToken, semaphore));
                await Task.WhenAll(videoTasks);
            }
            else
            {
                Video video;
                YoutubeClient downloadClient = youtube;
                try
                {
                    video = await youtube.Videos.GetAsync(url, cancellationToken);
                }
                catch (Exception ex) when (batchUsingCookies && MayRetryWithoutSessionCookies(ex))
                {
                    Log("HTTP 400 ou 401 ao obter metadados com cookies. A tentar sem sessão…");
                    downloadClient = new YoutubeClient();
                    video = await downloadClient.Videos.GetAsync(url, cancellationToken);
                }

                await DownloadVideoOrAudioAsync(downloadClient, downloadClient == youtube && batchUsingCookies,
                    video.Title, url, fileType, cancellationToken, semaphore);
            }
        }
        catch (Exception ex)
        {
            NoteVideoUnavailable(ex);
            NoteHttpBlocked(ex);
            Log(FormatProcessUrlError(url, ex), ex);
            _error++;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string VideoUnavailableExtraHint(Exception ex) =>
        ex is VideoUnavailableException
            ? Environment.NewLine +
              "Nota (YoutubeExplode): «não disponível» costuma aparecer quando a página deixa de expor metadados (bloqueio a pedidos anónimos/bots, idade, região, membros, etc.). " +
              "Se o vídeo abre no browser, use «Entrar no YouTube» para gravar a sessão, ou outra rede/IP."
            : "";

    /// <summary>
    /// Com cookies, o YouTube / InnerTube pode responder 400 (pedido «player») ou 401 (sessão inválida ou recusada).
    /// Para vídeos públicos, repetir sem cookies costuma funcionar.
    /// </summary>
    private static bool MayRetryWithoutSessionCookies(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            if (e is HttpRequestException hre &&
                hre.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
                return true;
        }

        return false;
    }

    private static string FormatYouTubeBlockedExplanation(int failures, bool usingCookies)
    {
        var sb = new StringBuilder();
        sb.Append("Aviso: ").Append(failures).AppendLine(" falha(s) HTTP 400/401 no endpoint interno do YouTube (VideoController.GetPlayerResponseAsync).");
        sb.AppendLine("Causa provável: o YouTube mudou o protocolo InnerTube e a versão atual de YoutubeExplode (6.5.7) ainda não acompanha essa mudança.");
        sb.Append("Estado da sessão: ")
          .AppendLine(usingCookies
              ? "cookies completos — o problema NÃO é falta de login."
              : "sem cookies — mesmo assim, o YouTube costuma exigir sessão para estes pedidos.");
        sb.AppendLine("O que fazer:");
        sb.AppendLine("  • Tentar de novo daqui a alguns minutos/horas (bloqueios costumam ser temporários).");
        sb.AppendLine("  • Vigiar uma nova versão de YoutubeExplode (ver github.com/Tyrrrz/YoutubeExplode/issues/852 e 781).");
        sb.Append("  • Se for urgente, considerar usar yt-dlp externamente enquanto a biblioteca não é corrigida.");
        return sb.ToString();
    }

    private static string FormatProcessUrlError(string url, Exception ex) =>
        $"Erro ao processar URL: {url} — {ex.Message}{VideoUnavailableExtraHint(ex)}";

    private static bool HasLegacyPlainCookiesHint() =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, "cookies.json"));

    private static async Task<(YoutubeClient Client, bool UsingCookies, IReadOnlyList<Cookie> Cookies)>
        CreateYoutubeClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            var (cookies, ok) = await YouTubeSessionStore.TryLoadForYoutubeClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (ok && cookies.Count > 0)
                return (new YoutubeClient(cookies), true, cookies);
        }
        catch (Exception ex)
        {
            AppFileLogger.Write("Falha ao preparar cliente YouTube com sessão.", ex);
        }

        return (new YoutubeClient(), false, Array.Empty<Cookie>());
    }

    private async Task DownloadVideoOrAudioAsync(YoutubeClient youtube, bool batchUsingCookies, string videoTitle,
        string url, string fileExt, CancellationToken cancellationToken, SemaphoreSlim semaphore)
    {
        try
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await semaphore.WaitAsync();
            cancellationToken.ThrowIfCancellationRequested();

            var safeTitle = MakeSafeFileName(videoTitle);
            var outputPath = Path.Combine(DownloadFolder, $"{safeTitle}.{fileExt}");
            if (File.Exists(outputPath))
            {
                Log($"Ignorado (ficheiro já existe): {videoTitle} → {outputPath}");
                _skipped++;
                return;
            }

            Log($"Início: {videoTitle} | URL: {url} | Saída: {outputPath}");
            var beforeTimer = watch.ElapsedMilliseconds;
            await youtube.Videos.DownloadAsync(url, outputPath, o => o
                .SetPreset(ConversionPreset.UltraFast)
                .SetFFmpegPath("ffmpeg.exe"), null, cancellationToken);

            var afterTimer = watch.ElapsedMilliseconds;
            Log($"{(decimal)(afterTimer - beforeTimer) / 1000}s: {videoTitle} concluído");
            _processed++;
        }
        catch (Exception ex)
        {
            NoteVideoUnavailable(ex);
            NoteHttpBlocked(ex);
            Log(
                $"Erro ao baixar: {videoTitle} | URL: {url} — {ex.Message}{VideoUnavailableExtraHint(ex)}",
                ex);
            _error++;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private bool IsPlaylistUrl(string url)
    {
        return url.Contains("playlist") || url.Contains("list=");
    }

    private static string NormalizeUrlForKindCheck(string line) =>
        line.Trim().Replace("https://youtu.be/", "https://www.youtube.com/watch?v=", StringComparison.Ordinal);

    private void UpdateUrlKindTag()
    {
        if (_lblUrlKind is null || _txtInput is null) return;

        var lines = _txtInput.Text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeUrlForKindCheck)
            .Where(s => s.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            _lblUrlKind.Text = "—";
            _lblUrlKind.ForeColor = UiTheme.TextMuted;
            return;
        }

        var anyPlaylist = lines.Any(IsPlaylistUrl);
        _lblUrlKind.Text = anyPlaylist ? "Lista" : "Simples";
        _lblUrlKind.ForeColor = anyPlaylist ? UiTheme.BsLink : UiTheme.TextMuted;
    }

    private string MakeSafeFileName(string title)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) title = title.Replace(c, '_');
        return title;
    }

    private void Log(string message, Exception? exceptionForFileOnly = null)
    {
        AppFileLogger.Write(message, exceptionForFileOnly);

        if (_txtLog == null) return;

        var uiLine = exceptionForFileOnly == null
            ? message + Environment.NewLine
            : message + Environment.NewLine + FormatExceptionForUi(exceptionForFileOnly) + Environment.NewLine;

        if (_txtLog.InvokeRequired)
            _txtLog.BeginInvoke((MethodInvoker)delegate { _txtLog.AppendText(uiLine); });
        else
            _txtLog.AppendText(uiLine);
    }

    /// <summary>Chain completa (tipos + mensagens) + 1ª linha do stack de cada nível que ajuda a localizar o método interno (ex.: VideoController.GetPlayerResponseAsync).</summary>
    private static string FormatExceptionForUi(Exception ex)
    {
        var sb = new StringBuilder();
        var depth = 0;
        for (Exception? e = ex; e != null && depth < 6; e = e.InnerException, depth++)
        {
            sb.Append(new string(' ', depth * 2));
            sb.Append("↳ ").Append(e.GetType().Name).Append(": ").Append(e.Message);

            if (e is HttpRequestException hre && hre.StatusCode.HasValue)
                sb.Append(" [HTTP ").Append((int)hre.StatusCode.Value).Append(']');

            var frame = FirstRelevantStackFrame(e.StackTrace);
            if (frame is not null)
            {
                sb.AppendLine();
                sb.Append(new string(' ', depth * 2)).Append("   at ").Append(frame);
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string? FirstRelevantStackFrame(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return null;
        foreach (var raw in stackTrace.Split('\n'))
        {
            var line = raw.TrimStart().TrimStart("at ".ToCharArray()).TrimEnd('\r');
            if (line.Length == 0) continue;
            if (line.StartsWith("YoutubeExplode", StringComparison.Ordinal) ||
                line.StartsWith("SimpleYoutubeDownloader", StringComparison.Ordinal))
                return line;
        }

        var first = stackTrace.Split('\n').FirstOrDefault();
        return first?.TrimStart().TrimStart("at ".ToCharArray()).TrimEnd('\r');
    }
}