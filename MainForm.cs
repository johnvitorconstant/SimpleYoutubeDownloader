using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;

namespace SimpleYoutubeDownloader;

public class MainForm : Form
{
    public MainForm(bool isPrivate)
    {
        InitializeComponents(isPrivate);
        try
        {
            _isPrivate = isPrivate;
            if (!Directory.Exists(DownloadFolder)) Directory.CreateDirectory(DownloadFolder);
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}");
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

        var btnLogin = new Button { Text = "Entrar no YouTube" };
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
            Log(ex.Message);
        }
    }

    private void BtnLogin_Click(object sender, EventArgs e)
    {
        Console.WriteLine(_isPrivate);
        using (var loginForm = new YouTubeLogin(_isPrivate))
        {
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Log("Cookies saved on 'cookies.json'.");
            }
            else
            {
                //Log("Login cancelado ou não realizado.");
            }
        }
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


            var semaphore = new SemaphoreSlim(decimal.ToInt32(_downloadsNumber.Value));

            var urls = _txtInput.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length == 0)
            {
                Log("Please insert one or more youtube links");
                _btnDownload.Enabled = true;
                return;
            }

            YoutubeClient youtube;
            {
                youtube = new YoutubeClient();
            }

            var tasks = urls.Select(url => ProcessUrlAsync(youtube, url, fileType, cancellationToken, semaphore))
                .ToArray();
            await Task.WhenAll(tasks);
            _btnDownload.Enabled = true;

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
            Log($"{ex.Message}");
        }
    }

    private async Task ProcessUrlAsync(YoutubeClient youtube, string url, string fileType,
        CancellationToken cancellationToken, SemaphoreSlim semaphore)
    {
        url = url.Replace("https://youtu.be/", "https://www.youtube.com/watch?v=");

        //
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsPlaylistUrl(url))
            {
                IReadOnlyList<YoutubeExplode.Playlists.PlaylistVideo> videos;
                if (File.Exists("cookies.json"))
                {
                    var json = await File.ReadAllTextAsync("cookies.json", cancellationToken);
                    List<Cookie>? cookies = JsonSerializer.Deserialize<List<Cookie>>(json);
                    Debug.Assert(cookies != null, nameof(cookies) + " != null");
                    var youtubeWCookies = new YoutubeClient(cookies);
                    videos = await youtubeWCookies.Playlists.GetVideosAsync(url, cancellationToken);
                }
                else
                {
                    videos = await youtube.Playlists.GetVideosAsync(url, cancellationToken);
                }


                Log($"Processing playlist: {url}");


                // Processa cada vídeo da playlist (em paralelo)
                var videoTasks = videos.Select(video =>
                    DownloadVideoOrAudioAsync(youtube, video.Title, video.Url, fileType, cancellationToken, semaphore));
                await Task.WhenAll(videoTasks);
            }
            else
            {
                var video = await youtube.Videos.GetAsync(url);
                await DownloadVideoOrAudioAsync(youtube, video.Title, url, fileType, cancellationToken, semaphore);
            }
        }
        catch (Exception ex)
        {
            Log($"Processing error {url}: {ex.Message}");
            _error++;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task DownloadVideoOrAudioAsync(YoutubeClient youtube, string videoTitle, string url, string fileExt,
        CancellationToken cancellationToken, SemaphoreSlim semaphore)
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
                Log("Skipped: " + videoTitle + ", file already exists");
                _skipped++;
                return;
            }

            Log("Started: " + videoTitle);
            var beforeTimer = watch.ElapsedMilliseconds;
            await youtube.Videos.DownloadAsync(url, outputPath, o => o
                .SetPreset(ConversionPreset.UltraFast)
                .SetFFmpegPath("ffmpeg.exe"), null, cancellationToken);
            var afterTimer = watch.ElapsedMilliseconds;
            Log($"{(decimal)(afterTimer - beforeTimer) / 1000}s: {videoTitle} completed");
            _processed++;
        }
        catch (Exception ex)
        {
            Log("Error: " + videoTitle);

            _error++;
            Log(ex.Message);
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

    private void Log(string message)
    {
        if (_txtLog.InvokeRequired)
            _txtLog.BeginInvoke((MethodInvoker)delegate { _txtLog.AppendText(message + Environment.NewLine); });
        else
            _txtLog.AppendText(message + Environment.NewLine);
    }
}