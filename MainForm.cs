﻿using SimpleYoutubeDownloader;
using System.Net;
using System.Text.Json;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;

public class MainForm : Form
{
   
    public MainForm(bool isPrivate)
    {
        // isPrivate = isPrivate;
        InitializeComponents(isPrivate);
        try
        {
           IsPrivate = isPrivate;
            if (!Directory.Exists(DownloadFolder))
            {
                Directory.CreateDirectory(DownloadFolder);
            }
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}");
        }

    }


    private void InitializeComponents(bool isPrivate)
    {

        this.Text = "Simple Youtube Downloader";
        this.Width = 800;
        this.Height = 600;
        this.StartPosition = FormStartPosition.CenterScreen;

        Label lblInstructions = new Label();
        lblInstructions.Text = "Paste links here:";
        lblInstructions.AutoSize = true;
        lblInstructions.Top = 10;
        lblInstructions.Left = 10;
        this.Controls.Add(lblInstructions);

        txtInput = new TextBox();
        txtInput.Multiline = true;
        txtInput.ScrollBars = ScrollBars.Vertical;
        txtInput.Width = 760;
        txtInput.Height = 200;
        txtInput.Top = lblInstructions.Bottom + 5;
        txtInput.Left = 10;
        this.Controls.Add(txtInput);

        int spacing = 0;
        rbMP3 = new RadioButton();
        rbMP3.Text = "mp3";
        rbMP3.Top = txtInput.Bottom + 10;
        rbMP3.Left = 10;
        this.Controls.Add(rbMP3);

        rbMP4 = new RadioButton();
        rbMP4.Text = "mp4";
        rbMP4.Top = txtInput.Bottom + 10;
        rbMP4.Left = rbMP3.Left + rbMP3.Width + spacing;
        rbMP4.Checked = true;
        this.Controls.Add(rbMP4);

        rbWEBM = new RadioButton();
        rbWEBM.Text = "webm";
        rbWEBM.Top = txtInput.Bottom + 10;
        rbWEBM.Left = rbMP4.Left + rbMP4.Width + spacing;
        this.Controls.Add(rbWEBM);

        downloadsNumber = new NumericUpDown();
        downloadsNumber.Minimum = 1;
        downloadsNumber.Maximum = 100;
        downloadsNumber.Value = 3;
        downloadsNumber.Top = txtInput.Bottom + 10;
        downloadsNumber.Left = rbWEBM.Right;
        this.Controls.Add(downloadsNumber);

        btnDownload = new Button();
        btnDownload.Text = "Download";
        btnDownload.Top = rbMP3.Bottom + 10;
        btnDownload.Left = 10;
        btnDownload.Click += BtnDownload_Click;
        this.Controls.Add(btnDownload);

        btnCancelar = new Button();
        btnCancelar.Text = "Cancelar";
        btnCancelar.Top = rbMP3.Bottom + 10;
        btnCancelar.Left = btnDownload.Right;
        btnCancelar.Click += BtnCancel_Click;
        this.Controls.Add(btnCancelar);

        Button btnLogin = new Button();
        btnLogin.Text = "Fazer Login";
        btnLogin.Top = rbMP3.Bottom + 10;
        btnLogin.Left = btnCancelar.Right;
        btnLogin.Click += BtnLogin_Click;
        this.Controls.Add(btnLogin);

        txtLog = new TextBox();
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Width = 760;
        txtLog.Height = 250;
        txtLog.Top = btnDownload.Bottom + 10;
        txtLog.Left = 10;
        txtLog.ReadOnly = true;
        txtLog.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        this.Controls.Add(txtLog);
    }


    private static readonly string DownloadFolder = "downloaded";
    public static bool IsPrivate;

    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    private int Processed;
    private int Skipped;
    private int Error;

    private TextBox txtInput;
    private NumericUpDown downloadsNumber;
    private RadioButton rbMP3;
    private RadioButton rbMP4;
    private RadioButton rbWEBM;
    private Button btnDownload;
    private Button btnCancelar;
    private TextBox txtLog;




    private void BtnLogin_Click(object sender, EventArgs e)
    {
        Console.WriteLine(IsPrivate);
        using (var loginForm = new YouTubeLogin(IsPrivate))
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

        cancellationTokenSource.Cancel();
        Log("Cancelled");
    }



    private async void BtnDownload_Click(object sender, EventArgs e)
    {
        try
        {

      
            var watch = System.Diagnostics.Stopwatch.StartNew();
            watch.Start();

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;


            string fileType =
                      rbMP3.Checked ? "mp3" :
                      rbMP4.Checked ? "mp4" :
                      rbWEBM.Checked ? "webm" : "mp3";


            btnDownload.Enabled = false;
            Processed = 0;
            Skipped = 0;
            Error = 0;




            txtLog.Clear();


            SemaphoreSlim semaphore = new SemaphoreSlim(Decimal.ToInt32(downloadsNumber.Value));

            string[] urls = txtInput.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length == 0)
            {
                Log("Please insert one or more youtube links");
                btnDownload.Enabled = true;
                return;
            }

            YoutubeClient youtube;
            {
                youtube = new YoutubeClient();
            }

            var tasks = urls.Select(url => ProcessUrlAsync(youtube, url, fileType, cancellationToken, semaphore)).ToArray();
            await Task.WhenAll(tasks);
            btnDownload.Enabled = true;

            Log("Download(s) finalizado(s).");
            Log($"Completed: {Processed}");
            Log($"Skipped:  {Skipped}");
            Log($"Error:  {Error}");
            watch.Stop();
            var elapsed = (decimal)(1 + (watch.ElapsedMilliseconds) / 1000);
            Log($"Elapsed Time: {(elapsed)} seconds");
            Log($"Avg speed: {(decimal)((Processed * 60) / (elapsed))} vídeos per minute");
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}");

        }



    }

    private async Task ProcessUrlAsync(YoutubeClient youtube, string url, string fileType, CancellationToken cancellationToken, SemaphoreSlim semaphore)
    {

        url = url.Replace("https://youtu.be/", "https://www.youtube.com/watch?v=");


        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsPlaylistUrl(url))
            {


                List<Cookie>? cookies = new List<Cookie>();
                IReadOnlyList<YoutubeExplode.Playlists.PlaylistVideo> videos;
                if (File.Exists("cookies.json"))
                {
                    string json = File.ReadAllText("cookies.json");
                    cookies = JsonSerializer.Deserialize<List<Cookie>>(json);
                    var youtubeWCookies = new YoutubeClient(cookies);
                    videos = await youtubeWCookies.Playlists.GetVideosAsync(url);
                }
                else
                {
                    videos = await youtube.Playlists.GetVideosAsync(url);
                }


                Log($"Processing playlist: {url}");



                // Processa cada vídeo da playlist (em paralelo)
                var videoTasks = videos.Select(video => DownloadVideoOrAudioAsync(youtube, video.Title, video.Url, fileType, cancellationToken, semaphore));
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
            Error++;

        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task DownloadVideoOrAudioAsync(YoutubeClient youtube, string videoTitle, string url, string fileExt, CancellationToken cancellationToken, SemaphoreSlim semaphore)
    {

        try
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await semaphore.WaitAsync();
            cancellationToken.ThrowIfCancellationRequested();

            string safeTitle = MakeSafeFileName(videoTitle);
            string outputPath = Path.Combine(DownloadFolder, $"{safeTitle}.{fileExt}");
            if (File.Exists(outputPath))
            {
                Log("Skipped: " + videoTitle + ", file already exists");
                Skipped++;
                return;
            }

            Log("Started: " + videoTitle);
            var beforeTimer = watch.ElapsedMilliseconds;
            await youtube.Videos.DownloadAsync(url, outputPath, o => o
            .SetPreset(ConversionPreset.UltraFast), null, cancellationToken);
            var afterTimer = watch.ElapsedMilliseconds;
            Log($"{(decimal)(afterTimer - beforeTimer) / 1000}s: {videoTitle} completed");
            Processed++;

        }
        catch (Exception ex)
        {
            Log("Error: " + videoTitle);

            Error++;
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
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            title = title.Replace(c, '_');
        }
        return title;
    }

    private void Log(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.BeginInvoke((MethodInvoker)delegate
            {
                txtLog.AppendText(message + Environment.NewLine);
            });
        }
        else
        {
            txtLog.AppendText(message + Environment.NewLine);
        }
    }


}

