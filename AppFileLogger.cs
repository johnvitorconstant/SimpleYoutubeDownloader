using System.Text;

namespace SimpleYoutubeDownloader;

/// <summary>Registo persistente em texto (thread-safe) ao lado do executável, pasta <c>logs/</c>.</summary>
internal static class AppFileLogger
{
    private static readonly object Gate = new();
    private static string? _logFilePath;
    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    public static string LogsDirectory => Path.Combine(AppContext.BaseDirectory, "logs");

    /// <summary>Cria a pasta e o ficheiro do dia (um ficheiro por data, várias sessões em append).</summary>
    public static void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_logFilePath != null) return;

            Directory.CreateDirectory(LogsDirectory);
            var name = $"SimpleYoutubeDownloader-{DateTime.Now:yyyy-MM-dd}.txt";
            _logFilePath = Path.Combine(LogsDirectory, name);
            var header =
                $"========== sessão iniciada {DateTime.Now:yyyy-MM-dd HH:mm:ss} (PID {Environment.ProcessId}) =========={Environment.NewLine}";
            File.AppendAllText(_logFilePath, header, Utf8Bom);
        }
    }

    /// <param name="message">Linha principal (também vai para a UI).</param>
    /// <param name="exception">Se não for null, o stack completo é escrito só no ficheiro.</param>
    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            EnsureInitialized();
            var sb = new StringBuilder(256);
            sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");
            sb.AppendLine(message);
            if (exception != null)
            {
                sb.AppendLine(exception.ToString());
                sb.AppendLine();
            }

            lock (Gate)
            {
                if (_logFilePath != null)
                    File.AppendAllText(_logFilePath, sb.ToString(), Utf8Bom);
            }
        }
        catch
        {
            // Nunca falhar a app por causa do log em disco
        }
    }
}
