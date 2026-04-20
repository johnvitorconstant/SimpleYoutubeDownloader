# Simple Youtube Downloader

Aplicativo **Windows Forms** (.NET 8) para baixar áudio ou vídeo do YouTube a partir de URLs coladas na interface. Suporta filas com paralelismo configurável, playlists e login opcional via navegador embutido (WebView2).

## Requisitos

- **Windows 10/11** (64 bits)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (para compilar)
- **ffmpeg.exe** na pasta do executável ou na raiz do projeto em desenvolvimento — o [YoutubeExplode.Converter](https://github.com/Tyrrrz/YoutubeExplode) usa o FFmpeg para mux/encode (por exemplo MP3). O binário **não** está versionado no Git; em desenvolvimento ou build local, baixe uma build oficial em [ffmpeg.org](https://ffmpeg.org/download.html) e coloque o `ffmpeg.exe` ao lado do `.exe`. O **zip gerado pelo GitHub Actions** na branch `develop` já inclui o FFmpeg (baixado na CI a partir do [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds), pacote win64-lgpl).

## Como usar

1. Execute o aplicativo.
2. Cole uma ou mais URLs do YouTube (uma por linha). URLs curtas `youtu.be/...` são normalizadas automaticamente.
3. Escolha o formato: **mp3**, **mp4** ou **webm**.
4. Ajuste o número de downloads simultâneos (padrão: 3).
5. Clique em **Download**. Os arquivos vão para a pasta **`downloaded`** (criada ao lado do executável).
6. **Login** (opcional): abre uma janela com WebView2 para autenticação quando necessário; cookies podem ser gravados em `cookies.json` conforme o fluxo da aplicação.

## Configuração (`appsettings.json`)

| Chave        | Descrição |
|-------------|-----------|
| `isPrivate` | Quando `true`, ao fechar o app o `cookies.json` é removido (modo mais “privado” para os cookies locais). |

O arquivo é copiado para a saída do build; edite na raiz do projeto ou na pasta de publicação.

## Compilar e publicar

Na raiz do repositório:

```powershell
dotnet restore
dotnet build -c Release
```

Publicar um único executável (conforme o `.csproj`, inclui `win-x64` e single file):

```powershell
dotnet publish -c Release
```

O resultado fica em `bin\Release\net8.0-windows\win-x64\publish\` (caminhos podem variar conforme configuração). Em publish **local**, copie **`ffmpeg.exe`** para a mesma pasta do `.exe` antes de distribuir (o zip da CI em `develop` já inclui o FFmpeg).

## Tecnologias principais

- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) / YoutubeExplode.Converter  
- Microsoft WebView2  
- Microsoft.Extensions.Configuration (JSON)

## Aviso legal

Baixar conteúdo do YouTube pode violar os [Termos de Serviço](https://www.youtube.com/t/terms) do site e direitos autorais do material. Use apenas conteúdo que você tenha permissão para baixar e armazenar; este projeto é fornecido como está, sem garantias.
