using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControlarTela;

sealed class ScreenRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    [JsonIgnore]
    public bool IsConfigured => Width > 1 && Height > 1;
}

sealed class ClickPointConfig
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Configured { get; set; }
}

sealed class SpotConfig
{
    public string Name { get; set; } = "Spot";
    public int X { get; set; }
    public int Y { get; set; }
    public bool Enabled { get; set; } = true;

    public override string ToString() => $"{Name}  ({X}, {Y})";
}

sealed class WindowProfile
{
    public const string RotateSpots = "RotateSpots";
    public const string StopAfterTeleport = "StopAfterTeleport";

    public string Name { get; set; } = "Janela";
    public string WindowTitle { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public bool ProtectionEnabled { get; set; } = true;
    public bool BackgroundMode { get; set; } = true;
    public ScreenRegion HealthBar { get; set; } = new();
    public int FullHealthRedWidth { get; set; }
    public decimal LifeThresholdPercent { get; set; } = 40;
    public ClickPointConfig TeleportPoint { get; set; } = new();
    public ClickPointConfig RandomTeleportPoint { get; set; } = new();
    public string TeleportMode { get; set; } = "Safe";
    public string ReactionMode { get; set; } = RotateSpots;
    public ScreenRegion SpotWindowRegion { get; set; } = new();
    public byte[] SpotWindowReferencePng { get; set; } = [];
    public ScreenRegion MinimapArrowRegion { get; set; } = new();
    public byte[] MinimapArrowReferencePng { get; set; } = [];
    public ClickPointConfig SpotOpenIconPoint { get; set; } = new();
    public ClickPointConfig NpcIconPoint { get; set; } = new();
    public ClickPointConfig ConfirmTeleportPoint { get; set; } = new();
    public ClickPointConfig AutoPoint { get; set; } = new();
    public BindingList<SpotConfig> Spots { get; set; } = [];
    public decimal SpotWindowMinimumSimilarity { get; set; } = 80;
    public int CycleCount { get; set; } = 1;
    public int SessionLimitMinutes { get; set; } = 60;
    public int TeleportToSpotDelayMs { get; set; } = 1000;
    public int TeleportRetryCount { get; set; } = 5;
    public int LoadingTimeoutMs { get; set; } = 30_000;
    public int NpcWaitMs { get; set; } = 3000;
    public int SpotMenuRetryWaitMs { get; set; } = 5000;
    public int PostSpotTeleportWaitMs { get; set; } = 30_000;
    public int RearmDelayMs { get; set; } = 5000;
    public int StableTimeMs { get; set; } = 2000;

    [JsonIgnore]
    public bool UsesSpotRotation => ReactionMode == RotateSpots;

    [JsonIgnore]
    public bool IsConfigured => HealthBar.IsConfigured
                                && FullHealthRedWidth > 0
                                && (string.Equals(TeleportMode, "Random", StringComparison.OrdinalIgnoreCase)
                                    ? RandomTeleportPoint.Configured
                                    : TeleportPoint.Configured)
                                && (!UsesSpotRotation
                                    || (TeleportPoint.Configured
                                        && SpotWindowRegion.IsConfigured
                                        && SpotWindowReferencePng.Length > 0
                                        && MinimapArrowRegion.IsConfigured
                                        && MinimapArrowReferencePng.Length > 0
                                        && SpotOpenIconPoint.Configured
                                        && NpcIconPoint.Configured
                                        && ConfirmTeleportPoint.Configured
                                        && AutoPoint.Configured
                                        && Spots.Any(spot => spot.Enabled)));
}

sealed class AppConfig
{
    public int SchemaVersion { get; set; } = 8;
    public int CaptureIntervalMs { get; set; } = 300;
    public bool InitialSetupCompleted { get; set; }
    public int SetupProfileIndex { get; set; }
    public string SetupStepId { get; set; } = "window";
    public List<WindowProfile> Windows { get; set; } = [];

    public void Normalize()
    {
        SchemaVersion = 8;
        CaptureIntervalMs = Math.Clamp(CaptureIntervalMs, 100, 2000);
        SetupProfileIndex = Math.Clamp(SetupProfileIndex, 0, 1);
        SetupStepId = string.IsNullOrWhiteSpace(SetupStepId) ? "window" : SetupStepId;
        while (Windows.Count < 2)
            Windows.Add(new WindowProfile { Name = $"Janela {Windows.Count + 1}" });
        if (Windows.Count > 2)
            Windows.RemoveRange(2, Windows.Count - 2);

        foreach (var profile in Windows)
        {
            profile.HealthBar ??= new ScreenRegion();
            profile.FullHealthRedWidth = Math.Clamp(profile.FullHealthRedWidth, 0, Math.Max(0, profile.HealthBar.Width));
            profile.LifeThresholdPercent = Math.Clamp(profile.LifeThresholdPercent, 1, 100);
            profile.TeleportPoint ??= new ClickPointConfig();
            profile.RandomTeleportPoint ??= new ClickPointConfig();
            profile.TeleportMode = string.Equals(profile.TeleportMode, "Random", StringComparison.OrdinalIgnoreCase)
                ? "Random"
                : "Safe";
            profile.ReactionMode = profile.ReactionMode == WindowProfile.StopAfterTeleport
                ? WindowProfile.StopAfterTeleport
                : WindowProfile.RotateSpots;
            profile.SpotWindowRegion ??= new ScreenRegion();
            profile.SpotWindowReferencePng ??= [];
            profile.MinimapArrowRegion ??= new ScreenRegion();
            profile.MinimapArrowReferencePng ??= [];
            profile.SpotOpenIconPoint ??= new ClickPointConfig();
            profile.NpcIconPoint ??= new ClickPointConfig();
            profile.ConfirmTeleportPoint ??= new ClickPointConfig();
            profile.AutoPoint ??= new ClickPointConfig();
            profile.Spots ??= [];
            profile.SpotWindowMinimumSimilarity = Math.Clamp(profile.SpotWindowMinimumSimilarity, 50, 100);
            profile.CycleCount = Math.Clamp(profile.CycleCount, 1, 999);
            profile.SessionLimitMinutes = Math.Clamp(profile.SessionLimitMinutes, 1, 10_080);
            profile.TeleportToSpotDelayMs = Math.Clamp(profile.TeleportToSpotDelayMs, 100, 10_000);
            profile.TeleportRetryCount = Math.Clamp(profile.TeleportRetryCount, 1, 20);
            profile.LoadingTimeoutMs = Math.Clamp(profile.LoadingTimeoutMs, 1000, 120_000);
            profile.NpcWaitMs = Math.Clamp(profile.NpcWaitMs, 100, 60_000);
            profile.SpotMenuRetryWaitMs = Math.Clamp(profile.SpotMenuRetryWaitMs, 100, 60_000);
            profile.PostSpotTeleportWaitMs = Math.Clamp(profile.PostSpotTeleportWaitMs, 1000, 120_000);
            profile.RearmDelayMs = Math.Clamp(profile.RearmDelayMs, 1000, 60_000);
            profile.StableTimeMs = Math.Clamp(profile.StableTimeMs, 500, 10_000);
        }
    }
}

static class ConfigStore
{
    static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ControlarTela");

    public static string FilePath => Path.Combine(Folder, "config.json");

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppConfig Load(out string? warning)
    {
        warning = null;
        if (!File.Exists(FilePath))
        {
            var fresh = new AppConfig();
            fresh.Normalize();
            return fresh;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            using var document = JsonDocument.Parse(json);
            var schemaVersion = document.RootElement.TryGetProperty(nameof(AppConfig.SchemaVersion), out var version)
                ? version.GetInt32()
                : 0;
            var isOld = schemaVersion < 3;
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
                         ?? new AppConfig();
            if (isOld)
                warning = "A rota de spots foi atualizada. Configure a janela, o menu e o botão Teleportar.";
            if (schemaVersion < 6)
            {
                config.InitialSetupCompleted = true;
                if (document.RootElement.TryGetProperty(nameof(AppConfig.Windows), out var windows))
                {
                    for (var index = 0; index < Math.Min(config.Windows.Count, windows.GetArrayLength()); index++)
                    {
                        var oldProfile = windows[index];
                        if (oldProfile.TryGetProperty("DropLimitPercent", out var oldDrop))
                            config.Windows[index].LifeThresholdPercent = 100 - oldDrop.GetDecimal();
                        if (oldProfile.TryGetProperty("UseSpots", out var oldUseSpots))
                            config.Windows[index].ReactionMode = oldUseSpots.GetBoolean()
                                ? WindowProfile.RotateSpots
                                : WindowProfile.StopAfterTeleport;
                    }
                }
                warning = "O fluxo de proteção foi atualizado. Revise as novas marcações em Configuração guiada.";
            }
            if (schemaVersion < 7)
            {
                var windows = document.RootElement.TryGetProperty(nameof(AppConfig.Windows), out var savedWindows)
                    ? savedWindows
                    : default;
                for (var index = 0; index < config.Windows.Count; index++)
                {
                    var profile = config.Windows[index];
                    profile.SpotOpenIconPoint ??= new ClickPointConfig();
                    profile.NpcIconPoint ??= new ClickPointConfig();
                    if (windows.ValueKind != JsonValueKind.Array || index >= windows.GetArrayLength())
                        continue;
                    if (!profile.SpotOpenIconPoint.Configured
                        && TryCenter(windows[index], "SpotOpenIconRegion", out var openPoint))
                        profile.SpotOpenIconPoint = openPoint;
                    if (!profile.NpcIconPoint.Configured
                        && TryCenter(windows[index], "NpcIconRegion", out var npcPoint))
                        profile.NpcIconPoint = npcPoint;
                }
                warning = "Os ícones Abrir Spots e NPC agora usam pontos de clique. Confira as marcações na Configuração guiada.";
            }
            if (schemaVersion < 8)
                warning = "O fluxo agora reconhece a seta do minimapa. Adicione essa referência visual na Configuração guiada.";
            config.Normalize();
            return config;
        }
        catch (Exception error)
        {
            warning = $"Configuração inválida preservada em {FilePath}: {error.Message}";
            var fresh = new AppConfig();
            fresh.Normalize();
            return fresh;
        }
    }

    public static void Save(AppConfig config)
    {
        config.Normalize();
        Directory.CreateDirectory(Folder);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(config, JsonOptions));
        if (File.Exists(FilePath))
            File.Copy(FilePath, FilePath + ".bak", true);
        File.Move(temporary, FilePath, true);
    }

    static bool TryCenter(JsonElement profile, string propertyName, out ClickPointConfig point)
    {
        point = new ClickPointConfig();
        if (!profile.TryGetProperty(propertyName, out var region)
            || !region.TryGetProperty(nameof(ScreenRegion.X), out var x)
            || !region.TryGetProperty(nameof(ScreenRegion.Y), out var y)
            || !region.TryGetProperty(nameof(ScreenRegion.Width), out var width)
            || !region.TryGetProperty(nameof(ScreenRegion.Height), out var height)
            || width.GetInt32() < 2 || height.GetInt32() < 2)
            return false;
        point = new ClickPointConfig
        {
            X = x.GetInt32() + width.GetInt32() / 2,
            Y = y.GetInt32() + height.GetInt32() / 2,
            Configured = true
        };
        return true;
    }
}
