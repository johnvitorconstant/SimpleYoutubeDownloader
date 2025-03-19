using System.Diagnostics;
using System.Net;
using System.Text.Json;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;

namespace SimpleYoutubeDownloader;

public class MainForm : Form
{
    public MainForm(bool isPrivate)
    {

        // isPrivate = isPrivate;
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
        Text = @"Simple Youtube Downloader";
        Width = 800;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        var lblInstructions = new Label();
        lblInstructions.Text = @"Paste links here:";
        lblInstructions.AutoSize = true;
        lblInstructions.Top = 10;
        lblInstructions.Left = 10;
        Controls.Add(lblInstructions);

        _txtInput = new TextBox();
        _txtInput.Multiline = true;
        _txtInput.ScrollBars = ScrollBars.Vertical;
        _txtInput.Width = 760;
        _txtInput.Height = 200;
        _txtInput.Top = lblInstructions.Bottom + 5;
        _txtInput.Left = 10;
        Controls.Add(_txtInput);

        var spacing = 1;
        _rbMp3 = new RadioButton();
        _rbMp3.Text = @"mp3";
        _rbMp3.Top = _txtInput.Bottom + 10;
        _rbMp3.Left = 10;
        Controls.Add(_rbMp3);

        _rbMp4 = new RadioButton();
        _rbMp4.Text = @"mp4";
        _rbMp4.Top = _txtInput.Bottom + 10;
        _rbMp4.Left = _rbMp3.Left + _rbMp3.Width + spacing;
        _rbMp4.Checked = true;
        Controls.Add(_rbMp4);

        _rbWebm = new RadioButton();
        _rbWebm.Text = @"webm";
        _rbWebm.Top = _txtInput.Bottom + 10;
        _rbWebm.Left = _rbMp4.Left + _rbMp4.Width + spacing;
        Controls.Add(_rbWebm);

        _downloadsNumber = new NumericUpDown();
        _downloadsNumber.Minimum = 1;
        _downloadsNumber.Maximum = 100;
        _downloadsNumber.Value = 3;
        _downloadsNumber.Top = _txtInput.Bottom + 10;
        _downloadsNumber.Left = _rbWebm.Right;
        Controls.Add(_downloadsNumber);

        _btnDownload = new Button();
        _btnDownload.Text = @"Download";
        _btnDownload.Top = _rbMp3.Bottom + 10;
        _btnDownload.Left = 10;
        _btnDownload.Click += BtnDownload_Click;
        Controls.Add(_btnDownload);

        _btnCancelar = new Button();
        _btnCancelar.Text = @"Cancel";
        _btnCancelar.Top = _rbMp3.Bottom + 10;
        _btnCancelar.Left = _btnDownload.Right;
        _btnCancelar.Click += BtnCancel_Click;
        Controls.Add(_btnCancelar);

        var btnLogin = new Button();
        btnLogin.Text = @"Login";
        btnLogin.Top = _rbMp3.Bottom + 10;
        btnLogin.Left = _btnCancelar.Right;
        btnLogin.Click += BtnLogin_Click;
        Controls.Add(btnLogin);


        _txtLog = new TextBox();
        _txtLog.Multiline = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.Width = 760;
        _txtLog.Height = 250;
        _txtLog.Top = _btnDownload.Bottom + 10;
        _txtLog.Left = 10;
        _txtLog.ReadOnly = true;
        _txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.Add(_txtLog);
    }


    private static readonly string DownloadFolder = "downloaded";
    private static bool _isPrivate;

    private CancellationTokenSource _cancellationTokenSource = new();

    private int _processed;
    private int _skipped;
    private int _error;

    private TextBox? _txtInput;
    private NumericUpDown? _downloadsNumber;
    private RadioButton? _rbMp3;
    private RadioButton? _rbMp4;
    private RadioButton? _rbWebm;
    private Button? _btnDownload;
    private Button? _btnCancelar;
    private TextBox? _txtLog;

  


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


            var fileType =
                _rbMp3.Checked ? "mp3" :
                _rbMp4.Checked ? "mp4" :
                _rbWebm.Checked ? "webm" : "mp3";


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