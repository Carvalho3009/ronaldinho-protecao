using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace ControlarTela;

static class Updater
{
    const string LatestReleaseUrl = "https://api.github.com/repos/Carvalho3009/ronaldinho-protecao/releases/latest";
    const string LegacyAssetName = "ControlarTela.exe";
    const string PortableName = "Ronaldinho.exe";
    const string ApplyUpdateArgument = "--apply-update";
    const string CleanupUpdateArgument = "--cleanup-update";

    public sealed record UpdateInfo(Version Version, string Tag, string DownloadUrl);

    public static Version CurrentVersion => Normalize(
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0));

    public static async Task<UpdateInfo?> CheckAsync()
    {
        using var client = CreateClient();
        return ParseRelease(await client.GetStringAsync(LatestReleaseUrl), CurrentVersion);
    }

    public static async Task InstallAndRestartAsync(UpdateInfo update)
    {
        var currentExe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Não foi possível localizar o executável atual.");
        if (!IsPortableExecutable(Path.GetFileName(currentExe)))
            throw new InvalidOperationException("A atualização automática funciona somente no executável portátil.");

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"Ronaldinho-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var downloadedExe = Path.Combine(tempDirectory, PortableName);
        try
        {
            using (var client = CreateClient())
            using (var response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync();
                await using var destination = File.Create(downloadedExe);
                await source.CopyToAsync(destination);
            }

            ValidateDownloadedExecutable(downloadedExe, update.Version);
            _ = Process.Start(CreateApplyUpdateStartInfo(
                    downloadedExe,
                    Environment.ProcessId,
                    currentExe,
                    tempDirectory))
                ?? throw new InvalidOperationException("Não foi possível iniciar a instalação da atualização.");
        }
        catch
        {
            TryDeleteDirectory(tempDirectory);
            throw;
        }
    }

    public static bool TryHandleHelperMode(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length == 0 || !args[0].Equals(ApplyUpdateArgument, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            ApplyUpdate(args);
        }
        catch (Exception error)
        {
            exitCode = 1;
            var log = Path.Combine(Path.GetTempPath(), "Ronaldinho-update-error.txt");
            try
            {
                File.WriteAllText(log, $"{DateTimeOffset.Now:O}{Environment.NewLine}{error}");
            }
            catch
            {
                // O erro original ainda será exibido ao usuário.
            }
            MessageBox.Show(
                $"Não foi possível concluir a atualização.{Environment.NewLine}{error.Message}{Environment.NewLine}{Environment.NewLine}Detalhes: {log}",
                "Atualização do Ronaldinho",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        return true;
    }

    public static void ScheduleCleanup(string[] args)
    {
        if (args.Length < 3
            || !args[0].Equals(CleanupUpdateArgument, StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(args[2], out var helperProcessId)
            || !IsUpdateTempDirectory(args[1]))
            return;

        var tempDirectory = Path.GetFullPath(args[1]);
        _ = Task.Run(async () =>
        {
            await WaitForProcessExitAsync(helperProcessId, TimeSpan.FromSeconds(30));
            for (var attempt = 0; attempt < 20; attempt++)
            {
                if (TryDeleteDirectory(tempDirectory))
                    return;
                await Task.Delay(500);
            }
        });
    }

    static void ApplyUpdate(string[] args)
    {
        if (args.Length != 4
            || !int.TryParse(args[1], out var previousProcessId)
            || !IsPortableExecutable(Path.GetFileName(args[2]))
            || !IsUpdateTempDirectory(args[3]))
            throw new InvalidDataException("Os parâmetros da atualização são inválidos.");

        var targetExe = Path.GetFullPath(args[2]);
        var tempDirectory = Path.GetFullPath(args[3]);
        var helperExe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Não foi possível localizar o instalador da atualização.");
        if (!Path.GetDirectoryName(helperExe)!.Equals(tempDirectory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("O instalador não está na pasta temporária esperada.");
        var helperVersion = ReadExecutableVersion(helperExe);
        if (helperVersion <= ReadExecutableVersion(targetExe))
            throw new InvalidDataException("O instalador não é mais novo que a versão atual.");

        WaitForProcessExit(previousProcessId, TimeSpan.FromMinutes(1));
        CopyWithRetry(helperExe, targetExe);
        if (ReadExecutableVersion(targetExe) != helperVersion)
            throw new InvalidDataException("A versão copiada não corresponde ao instalador validado.");

        var restart = new ProcessStartInfo
        {
            FileName = targetExe,
            WorkingDirectory = Path.GetDirectoryName(targetExe)!,
            UseShellExecute = true
        };
        restart.ArgumentList.Add(CleanupUpdateArgument);
        restart.ArgumentList.Add(tempDirectory);
        restart.ArgumentList.Add(Environment.ProcessId.ToString());
        _ = Process.Start(restart)
            ?? throw new InvalidOperationException("A atualização foi instalada, mas o Ronaldinho não reiniciou.");
    }

    static ProcessStartInfo CreateApplyUpdateStartInfo(
        string downloadedExe,
        int currentProcessId,
        string currentExe,
        string tempDirectory)
    {
        var start = new ProcessStartInfo
        {
            FileName = downloadedExe,
            WorkingDirectory = tempDirectory,
            UseShellExecute = true
        };
        start.ArgumentList.Add(ApplyUpdateArgument);
        start.ArgumentList.Add(currentProcessId.ToString());
        start.ArgumentList.Add(currentExe);
        start.ArgumentList.Add(tempDirectory);
        return start;
    }

    static void CopyWithRetry(string source, string destination)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                File.Copy(source, destination, true);
                return;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                lastError = error;
                Thread.Sleep(500);
            }
        }
        throw new IOException("O arquivo antigo permaneceu bloqueado após várias tentativas.", lastError);
    }

    static void ValidateDownloadedExecutable(string path, Version expectedVersion)
    {
        using (var file = File.OpenRead(path))
        {
            if (file.Length < 1_000_000 || file.ReadByte() != 'M' || file.ReadByte() != 'Z')
                throw new InvalidDataException("O arquivo de atualização recebido não é um executável válido.");
        }

        var downloadedVersion = ReadExecutableVersion(path);
        if (downloadedVersion != Normalize(expectedVersion))
            throw new InvalidDataException(
                $"A versão do arquivo baixado ({downloadedVersion}) não corresponde à versão esperada ({expectedVersion}).");
    }

    static Version ReadExecutableVersion(string path)
    {
        var versionText = FileVersionInfo.GetVersionInfo(path).FileVersion;
        if (!Version.TryParse(versionText, out var version))
            throw new InvalidDataException($"O executável {Path.GetFileName(path)} não possui uma versão válida.");
        return Normalize(version);
    }

    static void WaitForProcessExit(int processId, TimeSpan timeout)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                throw new TimeoutException("O Ronaldinho anterior não encerrou dentro do tempo esperado.");
        }
        catch (ArgumentException)
        {
            // O processo já encerrou.
        }
    }

    static async Task WaitForProcessExitAsync(int processId, TimeSpan timeout)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            using var cancellation = new CancellationTokenSource(timeout);
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (Exception error) when (error is ArgumentException or OperationCanceledException)
        {
            // O processo encerrou ou a limpeza será tentada mesmo após o limite.
        }
    }

    static bool IsUpdateTempDirectory(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            return Path.GetDirectoryName(fullPath)!.Equals(tempRoot, StringComparison.OrdinalIgnoreCase)
                   && Path.GetFileName(fullPath).StartsWith("Ronaldinho-", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    static bool TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Ronaldinho", CurrentVersion.ToString(3)));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    static UpdateInfo? ParseRelease(string json, Version currentVersion)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var tag = root.GetProperty("tag_name").GetString()
            ?? throw new InvalidDataException("A versão publicada não possui identificação.");
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var parsedVersion))
            throw new InvalidDataException($"A versão publicada '{tag}' é inválida.");

        var version = Normalize(parsedVersion);
        if (version <= Normalize(currentVersion))
            return null;

        foreach (var assetName in new[] { PortableName, LegacyAssetName })
        {
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (!string.Equals(asset.GetProperty("name").GetString(), assetName, StringComparison.OrdinalIgnoreCase))
                    continue;
                var url = asset.GetProperty("browser_download_url").GetString();
                if (!string.IsNullOrWhiteSpace(url) && IsTrustedDownloadUrl(url, tag, assetName))
                    return new UpdateInfo(version, tag, url);
            }
        }
        throw new InvalidDataException($"A versão {tag} não contém um executável oficial do Ronaldinho.");
    }

    static bool IsTrustedDownloadUrl(string url, string tag, string assetName) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.Equals(
            $"/Carvalho3009/ronaldinho-protecao/releases/download/{tag}/{assetName}",
            StringComparison.Ordinal);

    static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(0, version.Build));

    static bool IsPortableExecutable(string fileName) =>
        fileName.Equals(LegacyAssetName, StringComparison.OrdinalIgnoreCase)
        || fileName.Equals(PortableName, StringComparison.OrdinalIgnoreCase);

    public static void RunSelfTest()
    {
        const string release = """
            {"tag_name":"v2.2.1","assets":[
              {"name":"ControlarTela.exe","browser_download_url":"https://github.com/Carvalho3009/ronaldinho-protecao/releases/download/v2.2.1/ControlarTela.exe"},
              {"name":"Ronaldinho.exe","browser_download_url":"https://github.com/Carvalho3009/ronaldinho-protecao/releases/download/v2.2.1/Ronaldinho.exe"}
            ]}
            """;
        var update = ParseRelease(release, new Version(2, 2, 0));
        if (update?.Version != new Version(2, 2, 1)
            || !update.DownloadUrl.EndsWith("/Ronaldinho.exe", StringComparison.Ordinal))
            throw new InvalidOperationException("Falha no autoteste do atualizador.");
        if (ParseRelease(release, new Version(2, 2, 1)) is not null)
            throw new InvalidOperationException("O atualizador ofereceu a versão já instalada.");
        if (!IsPortableExecutable("ControlarTela.exe") || !IsPortableExecutable("Ronaldinho.exe")
            || IsPortableExecutable("Outro.exe"))
            throw new InvalidOperationException("Falha na compatibilidade dos nomes do executável portátil.");
        if (!IsTrustedDownloadUrl(
                "https://github.com/Carvalho3009/ronaldinho-protecao/releases/download/v2.2.1/Ronaldinho.exe",
                "v2.2.1",
                "Ronaldinho.exe")
            || IsTrustedDownloadUrl(
                "https://example.test/Ronaldinho.exe",
                "v2.2.1",
                "Ronaldinho.exe"))
            throw new InvalidOperationException("Falha na validação da origem da atualização.");

        var tempDirectory = Path.Combine(Path.GetTempPath(), "Ronaldinho-self-test");
        if (!IsUpdateTempDirectory(tempDirectory)
            || IsUpdateTempDirectory(Path.Combine(tempDirectory, "filha"))
            || IsUpdateTempDirectory(Path.GetTempPath()))
            throw new InvalidOperationException("Falha no limite de segurança da pasta temporária.");
        var start = CreateApplyUpdateStartInfo(
            Path.Combine(tempDirectory, PortableName),
            123,
            Path.Combine(Path.GetTempPath(), PortableName),
            tempDirectory);
        if (!start.FileName.EndsWith(PortableName, StringComparison.Ordinal)
            || start.ArgumentList.Count != 4
            || start.ArgumentList[0] != ApplyUpdateArgument)
            throw new InvalidOperationException("Falha no helper interno da atualização.");

        var copyTest = Path.Combine(Path.GetTempPath(), $"Ronaldinho-copy-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(copyTest);
        try
        {
            var source = Path.Combine(copyTest, "source");
            var destination = Path.Combine(copyTest, "destination");
            File.WriteAllText(source, "atualização");
            CopyWithRetry(source, destination);
            if (File.ReadAllText(destination) != "atualização")
                throw new InvalidOperationException("Falha na cópia do helper da atualização.");
        }
        finally
        {
            Directory.Delete(copyTest, true);
        }
    }
}
