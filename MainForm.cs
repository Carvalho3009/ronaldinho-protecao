namespace ControlarTela;

sealed class MainForm : Form
{
    const int BarSearchIntervalSeconds = 5;
    static readonly Color Ink = Color.FromArgb(7, 9, 9);
    static readonly Color InkSoft = Color.FromArgb(13, 17, 16);
    static readonly Color Bone = Color.FromArgb(244, 242, 235);
    static readonly Color Muted = Color.FromArgb(185, 181, 170);
    static readonly Color Gold = Color.FromArgb(212, 166, 77);
    static readonly Color Acid = Color.FromArgb(168, 255, 22);
    static readonly Color Coral = Color.FromArgb(255, 101, 71);
    static readonly Color Water = Color.FromArgb(99, 185, 243);
    readonly AppConfig _config;
    readonly List<ProfileUi> _profiles = [];
    readonly System.Windows.Forms.Timer _timer = new();
    readonly Button _startStop = new();
    readonly Button _updateStatus = new();
    readonly Label _status = new();
    bool _running;
    bool _busy;
    bool _loading;
    bool _checkingUpdate;

    public MainForm()
    {
        _config = ConfigStore.Load(out var warning);
        Text = "Ronaldinho • Proteção por Barra de Vida";
        Width = 1600;
        Height = 1000;
        MinimumSize = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ink;
        ForeColor = Bone;
        Font = new Font("Bahnschrift Condensed", 9.5F);

        BuildInterface();
        ApplyBrandTheme(this);
        RefreshWindows();
        Shown += async (_, _) => await CheckForUpdatesAsync();
        if (warning is not null)
            SetStatus(warning, true);

        _timer.Interval = _config.CaptureIntervalMs;
        _timer.Tick += MonitorTick;
        FormClosing += (_, _) =>
        {
            _timer.Stop();
            TrySave();
            foreach (var profile in _profiles)
                profile.Dispose();
        };
    }

    void BuildInterface()
    {
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 92,
            Padding = new Padding(18, 10, 18, 10),
            ColumnCount = 4,
            BackColor = Ink
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = LoadBrandLogo(),
            Margin = new Padding(0, 0, 18, 0)
        };
        top.Controls.Add(logo, 0, 0);

        var heading = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 5, 0, 0)
        };
        heading.Controls.Add(new Label
        {
            Text = "PROTEÇÃO POR BARRA DE VIDA",
            AutoSize = true,
            ForeColor = Bone,
            Font = new Font("Arial Narrow", 19F, FontStyle.Bold)
        });
        var versionLine = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        versionLine.Controls.Add(new Label
        {
            Text = $"v{typeof(MainForm).Assembly.GetName().Version?.ToString(3)}",
            AutoSize = true,
            ForeColor = Muted,
            Margin = new Padding(0, 5, 10, 0)
        });
        _updateStatus.Text = "ATUALIZAÇÕES";
        _updateStatus.AutoSize = true;
        _updateStatus.MinimumSize = new Size(92, 23);
        _updateStatus.Font = new Font("Arial Narrow", 8F, FontStyle.Bold);
        _updateStatus.Tag = "badge";
        _updateStatus.Click += async (_, _) => await CheckForUpdatesAsync(true);
        versionLine.Controls.Add(_updateStatus);
        heading.Controls.Add(versionLine);
        top.Controls.Add(heading, 1, 0);

        var refresh = new Button { Text = "↻  ATUALIZAR JANELAS", AutoSize = true };
        refresh.Tag = "secondary";
        refresh.Click += (_, _) => RefreshWindows();
        top.Controls.Add(refresh, 2, 0);

        _startStop.Text = "⛨  INICIAR PROTEÇÃO";
        _startStop.AutoSize = true;
        _startStop.Tag = "primary";
        _startStop.BackColor = Acid;
        _startStop.Click += async (_, _) =>
        {
            if (_running)
                StopProtection();
            else
                await StartProtectionAsync();
        };
        top.Controls.Add(_startStop, 3, 0);

        var tabHost = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        var tabBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            WrapContents = false,
            Padding = new Padding(18, 4, 0, 0),
            BackColor = Ink
        };
        var pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        var pages = new List<Panel>();
        var tabButtons = new List<Button>();
        foreach (var profile in _config.Windows)
        {
            var page = BuildProfilePage(profile);
            page.Visible = false;
            pages.Add(page);
            pageHost.Controls.Add(page);

            var index = pages.Count - 1;
            var tab = new Button
            {
                Text = profile.Name.ToUpperInvariant(),
                Width = 165,
                Height = 40,
                Margin = new Padding(0, 0, 6, 0),
                Tag = "tab"
            };
            tab.Click += (_, _) => SelectPage(index);
            tabButtons.Add(tab);
            tabBar.Controls.Add(tab);
        }
        SelectPage(0);
        tabHost.Controls.Add(pageHost);
        tabHost.Controls.Add(tabBar);

        _status.Dock = DockStyle.Bottom;
        _status.Height = 36;
        _status.Padding = new Padding(18, 8, 18, 0);
        _status.BackColor = InkSoft;
        _status.ForeColor = Muted;
        _status.Text = "Configure ao menos uma janela.";

        Controls.Add(tabHost);
        Controls.Add(top);
        Controls.Add(_status);

        void SelectPage(int selected)
        {
            for (var index = 0; index < pages.Count; index++)
            {
                pages[index].Visible = index == selected;
                tabButtons[index].Tag = index == selected ? "tabSelected" : "tab";
                StyleButton(tabButtons[index]);
            }
            pages[selected].BringToFront();
        }
    }

    Panel BuildProfilePage(WindowProfile profile)
    {
        var page = new Panel { Name = "WindowPage", Dock = DockStyle.Fill, BackColor = Ink, ForeColor = Bone };
        var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Ink };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 788,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(18, 8, 18, 8),
            BackColor = Ink
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 430));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 302));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 1 };

        var windowGroup = Group("JANELA CONFIGURADA", 82);
        var windowCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Bottom,
            Margin = new Padding(0),
            Height = 30
        };
        var windowLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(12, 13, 12, 5)
        };
        windowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        windowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        windowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        var windowSelector = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 20, 0) };
        windowSelector.Controls.Add(windowCombo);
        windowSelector.Controls.Add(new Label
        {
            Text = "JANELA DO JOGO SELECIONADA",
            Dock = DockStyle.Top,
            Height = 20,
            ForeColor = Muted,
            Font = new Font("Arial Narrow", 8.5F, FontStyle.Bold)
        });
        var backgroundMode = new BrandToggle
        {
            Text = "SEGUNDO PLANO",
            Checked = profile.BackgroundMode,
            Dock = DockStyle.Fill
        };
        var protectionEnabled = new BrandToggle
        {
            Name = "ProtectionEnabled",
            Text = "PROTEÇÃO ATIVA",
            Checked = profile.ProtectionEnabled,
            Dock = DockStyle.Fill
        };
        windowLayout.Controls.Add(windowSelector, 0, 0);
        windowLayout.Controls.Add(protectionEnabled, 1, 0);
        windowLayout.Controls.Add(backgroundMode, 2, 0);
        windowGroup.Controls.Add(windowLayout);
        root.Controls.Add(windowGroup, 0, 0);
        root.SetColumnSpan(windowGroup, 2);

        var barGroup = Group("1  BARRA DE VIDA", 294);
        var barLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10, 14, 10, 7)
        };
        barLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        barLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        barLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        barLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        barLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var barLine = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var selectBar = new Button { Text = "⌖  MARCAR BARRA", AutoSize = true };
        var barStatus = StepStatus();
        barLine.Controls.Add(selectBar);
        barLine.Controls.Add(barStatus);
        barLayout.Controls.Add(barLine, 0, 0);
        barLayout.SetColumnSpan(barLine, 2);

        var preview = new PictureBox
        {
            Dock = DockStyle.Fill,
            Name = "HealthPreview",
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(18, 22, 21),
            Margin = new Padding(4, 3, 20, 5)
        };
        barLayout.Controls.Add(preview, 0, 1);

        var lifePanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        lifePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        lifePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        lifePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        lifePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lifePanel.Controls.Add(new Label
        {
            Text = "VIDA",
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Arial Narrow", 9F, FontStyle.Bold)
        }, 0, 0);
        var lifePercent = new Label
        {
            Name = "LifePercent",
            Text = "--%",
            Dock = DockStyle.Fill,
            ForeColor = Coral,
            Font = new Font("Arial Narrow", 40F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var lifeLoss = new Label { Text = "Queda --", Dock = DockStyle.Fill, ForeColor = Muted };
        var lifeMeter = new BrandProgressBar { Dock = DockStyle.Top, Height = 14, Value = 0 };
        lifePanel.Controls.Add(lifePercent, 0, 1);
        lifePanel.Controls.Add(lifeLoss, 0, 2);
        lifePanel.Controls.Add(lifeMeter, 0, 3);
        barLayout.Controls.Add(lifePanel, 1, 1);

        var thresholdLine = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(2, 6, 0, 0) };
        thresholdLine.Controls.Add(new Label
        {
            Text = "REAGIR AO CAIR",
            AutoSize = true,
            ForeColor = Muted,
            Font = new Font("Arial Narrow", 9F, FontStyle.Bold),
            Margin = new Padding(3, 7, 12, 0)
        });
        var threshold = Number(profile.DropLimitPercent, 1, 90, 1, 70);
        thresholdLine.Controls.Add(threshold);
        thresholdLine.Controls.Add(new Label { Text = "%", AutoSize = true, Margin = new Padding(0, 7, 3, 0) });
        barLayout.Controls.Add(thresholdLine, 0, 2);

        var readNow = new Button { Text = "↻  ATUALIZAR LEITURA", AutoSize = true, Anchor = AnchorStyles.Right };
        readNow.Tag = "water";
        barLayout.Controls.Add(readNow, 1, 2);
        barGroup.Controls.Add(barLayout);
        left.Controls.Add(barGroup, 0, 0);

        var teleportGroup = Group("2  TELEPORTE", 118);
        var teleportLine = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10, 24, 8, 4) };
        var selectTeleport = new Button { Text = "⌖  MARCAR ITEM", AutoSize = true };
        var teleportStatus = StepStatus();
        teleportLine.Controls.Add(selectTeleport);
        teleportLine.Controls.Add(teleportStatus);
        teleportGroup.Controls.Add(teleportLine);
        left.Controls.Add(teleportGroup, 0, 1);

        var advancedToggle = new CheckBox
        {
            Text = "⚙  OPÇÕES AVANÇADAS",
            AutoSize = true,
            Margin = new Padding(8, 12, 3, 3)
        };
        var advancedGroup = Group("Opções avançadas", 235);
        advancedGroup.Visible = false;
        var advanced = VerticalPanel(false);
        advanced.Padding = new Padding(8, 14, 8, 4);
        var delay = Number(profile.TeleportToSpotDelayMs, 100, 10_000, 100, 90);
        var teleportRetries = Number(profile.TeleportRetryCount, 1, 20, 1, 90);
        var rearmDelay = Number(profile.RearmDelayMs, 1000, 60_000, 500, 90);
        var stableTime = Number(profile.StableTimeMs, 500, 10_000, 500, 90);
        var spotSimilarity = Number(profile.SpotWindowMinimumSimilarity, 50, 100, 1, 90);
        advanced.Controls.Add(OptionLine("Intervalo entre cliques (ms):", delay));
        advanced.Controls.Add(OptionLine("Tentativas no botão Teleportar:", teleportRetries));
        advanced.Controls.Add(OptionLine("Semelhança mínima da janela de spots (%):", spotSimilarity));
        advanced.Controls.Add(OptionLine("Espera mínima após reação (ms):", rearmDelay));
        advanced.Controls.Add(OptionLine("Barra estável por (ms):", stableTime));
        advancedGroup.Controls.Add(advanced);
        var advancedArea = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4, 0, 4, 0)
        };
        advancedArea.Controls.Add(advancedToggle);
        advancedArea.Controls.Add(advancedGroup);

        var spotsGroup = Group("3  ROTA DE SPOTS", 420);
        var spotsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(10, 13, 10, 6)
        };
        spotsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        spotsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        spotsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        spotsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        spotsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        spotsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        spotsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        var useSpots = new BrandToggle
        {
            Text = "USAR SPOTS",
            Checked = profile.UseSpots,
            Dock = DockStyle.Right,
            Width = 190
        };
        spotsLayout.Controls.Add(useSpots, 0, 0);

        var markers = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        markers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        markers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        markers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        var spotWindowLine = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var selectSpotWindow = MarkerButton("JANELA DE SPOTS");
        var spotWindowStatus = StepStatus();
        spotWindowLine.Controls.Add(selectSpotWindow);
        spotWindowLine.Controls.Add(spotWindowStatus);

        var spotMenuLine = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var selectSpotMenu = MarkerButton("ABRIR MENU");
        var spotMenuStatus = StepStatus();
        spotMenuLine.Controls.Add(selectSpotMenu);
        spotMenuLine.Controls.Add(spotMenuStatus);

        var confirmTeleportLine = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var selectConfirmTeleport = MarkerButton("BOTÃO TELEPORTAR");
        var confirmTeleportStatus = StepStatus();
        confirmTeleportLine.Controls.Add(selectConfirmTeleport);
        confirmTeleportLine.Controls.Add(confirmTeleportStatus);
        markers.Controls.Add(spotWindowLine, 0, 0);
        markers.Controls.Add(spotMenuLine, 1, 0);
        markers.Controls.Add(confirmTeleportLine, 2, 0);
        spotsLayout.Controls.Add(markers, 0, 1);

        var markerTools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var spotMatch = new Label { Text = "Semelhança atual: --", AutoSize = true, Margin = new Padding(3, 8, 14, 0) };
        var showMarks = new Button { Text = "MOSTRAR MARCAÇÕES", AutoSize = true };
        markerTools.Controls.Add(spotMatch);
        markerTools.Controls.Add(showMarks);
        spotsLayout.Controls.Add(markerTools, 0, 2);

        spotsLayout.Controls.Add(new Label
        {
            Text = "ORDEM     NOME DO SPOT                                            ATIVO",
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Arial Narrow", 8.5F, FontStyle.Bold)
        }, 0, 3);
        var spots = new CheckedListBox { Name = "SpotsList", Dock = DockStyle.Fill, CheckOnClick = true };
        spotsLayout.Controls.Add(spots, 0, 4);

        var spotButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var addSpot = SmallButton("＋ ADICIONAR");
        var updateSpot = SmallButton("↕ POSIÇÃO");
        var renameSpot = SmallButton("✎ RENOMEAR");
        var moveUp = SmallButton("▲");
        var moveDown = SmallButton("▼");
        var removeSpot = SmallButton("REMOVER");
        removeSpot.Tag = "danger";
        spotButtons.Controls.AddRange([addSpot, updateSpot, renameSpot, moveUp, moveDown, removeSpot]);
        spotsLayout.Controls.Add(spotButtons, 0, 5);

        var cyclesLine = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 5, 0, 0) };
        cyclesLine.Controls.Add(new Label
        {
            Text = "REPETIR ROTA",
            AutoSize = true,
            ForeColor = Muted,
            Font = new Font("Arial Narrow", 9F, FontStyle.Bold),
            Margin = new Padding(3, 7, 12, 0)
        });
        var cycles = Number(profile.CycleCount, 1, 999, 1, 70);
        cyclesLine.Controls.Add(cycles);
        cyclesLine.Controls.Add(new Label { Text = "vezes", AutoSize = true, Margin = new Padding(0, 7, 3, 0) });
        spotsLayout.Controls.Add(cyclesLine, 0, 6);
        spotsGroup.Controls.Add(spotsLayout);
        right.Controls.Add(spotsGroup, 0, 0);

        var stateGroup = Group("ESTADO DA SESSÃO", 142);
        stateGroup.Name = "SessionGroup";
        var stateFlow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            Padding = new Padding(10, 14, 10, 5)
        };
        stateFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        stateFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        stateFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
        stateFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        stateFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        stateFlow.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        stateFlow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var state = new Label { Text = "PARADA", AutoSize = true, ForeColor = Acid, Font = new Font("Arial Narrow", 13F, FontStyle.Bold) };
        var progress = new Label { AutoSize = true, ForeColor = Water, MaximumSize = new Size(360, 0) };
        var detected = new Label { Text = "Barra ainda não calibrada.", AutoSize = true, ForeColor = Acid, MaximumSize = new Size(360, 0) };
        var captureStatus = new Label { Text = "Captura: parada", AutoSize = true };
        var stateCell = MetricCell("STATUS", state, captureStatus);
        var lifeCell = MetricCell("LEITURA", detected);
        var progressCell = MetricCell("PRÓXIMA REAÇÃO", progress);

        var sessionLine = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        sessionLine.Controls.Add(new Label
        {
            Text = "Limite da sessão:",
            AutoSize = true,
            Margin = new Padding(3, 7, 3, 0)
        });
        var sessionHours = Number(profile.SessionLimitMinutes / 60, 0, 168, 1, 60);
        var sessionMinutes = Number(profile.SessionLimitMinutes % 60, 0, 59, 1, 60);
        sessionLine.Controls.Add(sessionHours);
        sessionLine.Controls.Add(new Label { Text = "h", AutoSize = true, Margin = new Padding(0, 7, 3, 0) });
        sessionLine.Controls.Add(sessionMinutes);
        sessionLine.Controls.Add(new Label { Text = "min", AutoSize = true, Margin = new Padding(0, 7, 3, 0) });
        var sessionTime = new Label
        {
            Text = "00:00:00",
            AutoSize = true,
            ForeColor = Water,
            Font = new Font("Arial Narrow", 11F, FontStyle.Bold)
        };
        var remainingTime = new Label
        {
            Text = "01:00:00",
            AutoSize = true,
            ForeColor = Water,
            Font = new Font("Arial Narrow", 11F, FontStyle.Bold)
        };
        var timeCell = MetricCell("TEMPO ATIVO", sessionTime);
        var remainingCell = MetricCell("RESTANTE", remainingTime);
        stateFlow.Controls.Add(stateCell, 0, 0);
        stateFlow.Controls.Add(lifeCell, 1, 0);
        stateFlow.Controls.Add(progressCell, 2, 0);
        stateFlow.Controls.Add(timeCell, 3, 0);
        stateFlow.Controls.Add(remainingCell, 4, 0);

        var stateButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var rearm = SmallButton("⌖ RECALIBRAR");
        var reset = SmallButton("↻ REINICIAR SESSÃO");
        var test = SmallButton("▷ TESTAR REAÇÃO");
        var backgroundTest = SmallButton("Testar clique em 2º plano");
        var backgroundCaptureTest = SmallButton("Testar captura em 2º plano");
        test.Tag = "water";
        stateButtons.Controls.AddRange([rearm, reset, test]);
        advanced.Controls.Add(new Label { Text = "TESTES DE COMPATIBILIDADE", AutoSize = true, ForeColor = Gold, Margin = new Padding(3, 8, 3, 2) });
        var compatibilityTests = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        compatibilityTests.Controls.AddRange([backgroundTest, backgroundCaptureTest]);
        advanced.Controls.Add(compatibilityTests);
        advanced.Controls.Add(new Label { Text = "LIMITE DA SESSÃO", AutoSize = true, ForeColor = Gold, Margin = new Padding(3, 8, 3, 2) });
        advanced.Controls.Add(sessionLine);
        stateFlow.Controls.Add(stateButtons, 0, 1);
        stateFlow.SetColumnSpan(stateButtons, 5);
        stateGroup.Controls.Add(stateFlow);
        root.Controls.Add(stateGroup, 0, 2);
        root.SetColumnSpan(stateGroup, 2);

        root.Controls.Add(left, 0, 1);
        root.Controls.Add(right, 1, 1);
        root.Controls.Add(advancedArea, 0, 3);
        root.SetColumnSpan(advancedArea, 2);
        root.SizeChanged += (_, _) => advancedGroup.Width = Math.Max(500, root.ClientSize.Width - 42);
        viewport.Controls.Add(root);
        page.Controls.Add(viewport);

        var ui = new ProfileUi(
            profile,
            windowCombo,
            barStatus,
            teleportStatus,
            spotWindowStatus,
            spotMenuStatus,
            confirmTeleportStatus,
            spotMatch,
            preview,
            spots,
            state,
            progress,
            detected,
            captureStatus,
            sessionTime,
            lifeMeter,
            lifePercent,
            lifeLoss,
            remainingTime);
        _profiles.Add(ui);
        SetSpotControlsEnabled();
        RefreshProfileUi(ui);
        if (!profile.ProtectionEnabled)
            SetProfileStatus(ui, "Desativada", "Proteção desligada para esta janela.");

        windowCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_loading || windowCombo.SelectedItem is not WindowChoice choice)
                return;
            profile.WindowTitle = choice.Title;
            profile.ProcessName = choice.ProcessName;
            ui.DisposeCapture();
            if (_running)
                PauseProfile(ui, "Janela alterada. Inicie novamente.");
            TrySave();
        };
        backgroundMode.CheckedChanged += (_, _) =>
        {
            if (_running)
            {
                if (backgroundMode.Checked != profile.BackgroundMode)
                {
                    backgroundMode.Checked = profile.BackgroundMode;
                    MessageBox.Show(this, "Pare a proteção antes de alterar o modo de captura.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            profile.BackgroundMode = backgroundMode.Checked;
            ui.CaptureStatus.Text = backgroundMode.Checked
                ? "Captura: segundo plano será iniciado com a proteção"
                : "Captura: tela visível (modo compatibilidade)";
            TrySave();
        };
        protectionEnabled.CheckedChanged += async (_, _) =>
        {
            protectionEnabled.Enabled = false;
            try
            {
                await SetProfileProtectionAsync(ui, protectionEnabled.Checked);
            }
            finally
            {
                protectionEnabled.Enabled = true;
            }
        };
        selectBar.Click += (_, _) => SelectHealthBar(ui);
        readNow.Click += async (_, _) => await ReadHealthNowAsync(ui);
        selectTeleport.Click += (_, _) => SelectTeleport(ui);
        selectSpotWindow.Click += (_, _) => SelectSpotWindow(ui);
        selectSpotMenu.Click += (_, _) => SelectSpotMenu(ui);
        selectConfirmTeleport.Click += (_, _) => SelectConfirmTeleport(ui);
        showMarks.Click += (_, _) => ShowMarks(ui);
        threshold.ValueChanged += (_, _) => { profile.DropLimitPercent = threshold.Value; TrySave(); };
        cycles.ValueChanged += (_, _) => { profile.CycleCount = (int)cycles.Value; UpdateProgress(ui); TrySave(); };
        sessionHours.ValueChanged += (_, _) => SaveSessionLimit();
        sessionMinutes.ValueChanged += (_, _) => SaveSessionLimit();
        useSpots.CheckedChanged += (_, _) =>
        {
            if (_running)
            {
                useSpots.Checked = profile.UseSpots;
                MessageBox.Show(this, "Pare a proteção antes de alterar o uso de spots.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            profile.UseSpots = useSpots.Checked;
            ui.NextSpotIndex = Math.Max(0, FindEnabledSpot(profile.Spots));
            ui.CompletedCycles = 0;
            if (ui.State == ProfileState.Completed)
                ui.State = ProfileState.Stopped;
            SetSpotControlsEnabled();
            UpdateProgress(ui);
            TrySave();
        };
        spots.ItemCheck += (_, eventArgs) =>
        {
            if (ui.UpdatingSpotChecks || eventArgs.Index < 0 || eventArgs.Index >= profile.Spots.Count)
                return;

            var enabled = eventArgs.NewValue == CheckState.Checked;
            var current = profile.Spots[eventArgs.Index];
            var remaining = profile.Spots.Count(item => item.Enabled)
                            - (current.Enabled ? 1 : 0)
                            + (enabled ? 1 : 0);
            if (remaining == 0)
            {
                eventArgs.NewValue = eventArgs.CurrentValue;
                MessageBox.Show(this,
                    "Mantenha ao menos um spot ativo. Para usar somente o teleporte, desative o modo de spots.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            current.Enabled = enabled;
            if (ui.NextSpotIndex < 0
                || ui.NextSpotIndex >= profile.Spots.Count
                || !profile.Spots[ui.NextSpotIndex].Enabled)
                ui.NextSpotIndex = FindEnabledSpot(profile.Spots, ui.NextSpotIndex + 1);
            UpdateProgress(ui);
            TrySave();
            SetStatus($"{current.Name} {(enabled ? "ativado" : "desativado")}; alteração válida para a próxima reação.");
        };
        delay.ValueChanged += (_, _) => { profile.TeleportToSpotDelayMs = (int)delay.Value; TrySave(); };
        teleportRetries.ValueChanged += (_, _) => { profile.TeleportRetryCount = (int)teleportRetries.Value; TrySave(); };
        spotSimilarity.ValueChanged += (_, _) => { profile.SpotWindowMinimumSimilarity = spotSimilarity.Value; TrySave(); };
        rearmDelay.ValueChanged += (_, _) => { profile.RearmDelayMs = (int)rearmDelay.Value; TrySave(); };
        stableTime.ValueChanged += (_, _) => { profile.StableTimeMs = (int)stableTime.Value; TrySave(); };
        advancedToggle.CheckedChanged += (_, _) =>
        {
            advancedGroup.Visible = advancedToggle.Checked;
            root.RowStyles[3].Height = advancedToggle.Checked ? 250 : 66;
            root.Height = advancedToggle.Checked ? 972 : 788;
        };

        addSpot.Click += (_, _) => AddSpot(ui);
        updateSpot.Click += (_, _) => UpdateSpot(ui);
        renameSpot.Click += (_, _) => RenameSpot(ui);
        moveUp.Click += (_, _) => MoveSpot(ui, -1);
        moveDown.Click += (_, _) => MoveSpot(ui, 1);
        removeSpot.Click += (_, _) => RemoveSpot(ui);
        rearm.Click += async (_, _) => await RearmProfileAsync(ui);
        reset.Click += async (_, _) => await ResetSequenceAsync(ui);
        test.Click += async (_, _) => await TestReactionAsync(ui);
        backgroundTest.Click += async (_, _) => await TestBackgroundClickAsync(ui);
        backgroundCaptureTest.Click += async (_, _) => await TestBackgroundCaptureAsync(ui);
        return page;

        void SaveSessionLimit()
        {
            if (sessionHours.Value == 168 && sessionMinutes.Value > 0)
            {
                sessionMinutes.Value = 0;
                return;
            }
            var totalMinutes = (int)sessionHours.Value * 60 + (int)sessionMinutes.Value;
            if (totalMinutes < 1)
            {
                sessionMinutes.Value = 1;
                return;
            }
            profile.SessionLimitMinutes = totalMinutes;
            UpdateTime(ui);
            TrySave();
        }

        void SetSpotControlsEnabled()
        {
            spots.Enabled = useSpots.Checked;
            spotWindowLine.Enabled = useSpots.Checked;
            spotMatch.Enabled = useSpots.Checked;
            spotMenuLine.Enabled = useSpots.Checked;
            confirmTeleportLine.Enabled = useSpots.Checked;
            spotButtons.Enabled = useSpots.Checked;
            cyclesLine.Enabled = useSpots.Checked;
        }
    }

    static FlowLayoutPanel VerticalPanel(bool scroll = true) => new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = scroll
    };

    static GroupBox Group(string text, int height) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        Height = height,
        Margin = new Padding(6),
        Padding = new Padding(9),
        BackColor = InkSoft,
        ForeColor = Gold,
        Font = new Font("Arial Narrow", 10.5F, FontStyle.Bold)
    };

    static Label StepStatus() => new()
    {
        Text = "Não configurado",
        AutoSize = true,
        ForeColor = Coral,
        Font = new Font("Bahnschrift Condensed", 8.5F),
        Margin = new Padding(10, 7, 3, 0)
    };

    static Button SmallButton(string text) => new() { Text = text, AutoSize = true, MinimumSize = new Size(0, 34) };

    static Button MarkerButton(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 32,
        Font = new Font("Arial Narrow", 8F, FontStyle.Bold)
    };

    static NumericUpDown Number(decimal value, decimal min, decimal max, decimal increment, int width) => new()
    {
        Minimum = min,
        Maximum = max,
        Increment = increment,
        Value = Math.Clamp(value, min, max),
        Width = width
    };

    static FlowLayoutPanel OptionLine(string label, Control control)
    {
        var line = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        line.Controls.Add(new Label { Text = label, AutoSize = true, Width = 280, Margin = new Padding(3, 7, 3, 0) });
        line.Controls.Add(control);
        return line;
    }

    static FlowLayoutPanel MetricCell(string caption, params Control[] values)
    {
        var cell = VerticalPanel(false);
        cell.Padding = new Padding(8, 2, 8, 2);
        cell.Controls.Add(new Label
        {
            Text = caption,
            AutoSize = true,
            ForeColor = Gold,
            Font = new Font("Arial Narrow", 9F, FontStyle.Bold)
        });
        foreach (var value in values)
        {
            value.Margin = new Padding(3, 4, 3, 0);
            cell.Controls.Add(value);
        }
        return cell;
    }

    static Image? LoadBrandLogo()
    {
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream(
            "ControlarTela.Assets.ronaldinho-wordmark-gold.png");
        return stream is null ? null : new Bitmap(stream);
    }

    static void ApplyBrandTheme(Control root)
    {
        foreach (Control control in root.Controls)
        {
            switch (control)
            {
                case GroupBox group:
                    group.BackColor = InkSoft;
                    group.ForeColor = Gold;
                    break;
                case TabPage page:
                    page.BackColor = Ink;
                    page.ForeColor = Bone;
                    break;
                case Button button:
                    StyleButton(button);
                    break;
                case BrandToggle toggle:
                    toggle.BackColor = toggle.Parent?.BackColor ?? InkSoft;
                    toggle.ForeColor = Bone;
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = checkBox.Parent?.BackColor ?? Ink;
                    checkBox.ForeColor = Bone;
                    checkBox.FlatStyle = FlatStyle.Flat;
                    break;
                case ComboBox or NumericUpDown or TextBox or CheckedListBox:
                    control.BackColor = Color.FromArgb(18, 22, 21);
                    control.ForeColor = Bone;
                    break;
                case Label label when label.ForeColor == Color.DarkRed:
                    label.ForeColor = Coral;
                    break;
                case Label label when label.ForeColor == Color.DarkGreen:
                    label.ForeColor = Acid;
                    break;
                case Label label when label.ForeColor == SystemColors.ControlText:
                    label.ForeColor = Bone;
                    break;
                case FlowLayoutPanel or TableLayoutPanel or Panel:
                    control.BackColor = control.Parent?.BackColor ?? Ink;
                    control.ForeColor = Bone;
                    break;
            }
            ApplyBrandTheme(control);
        }
    }

    static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(8, 3, 8, 3);
        var role = button.Tag as string;
        button.BackColor = role switch
        {
            "primary" => Acid,
            "badge" => Color.FromArgb(31, 45, 10),
            "tabSelected" => InkSoft,
            "tab" => Ink,
            "danger" => InkSoft,
            _ => InkSoft
        };
        button.ForeColor = role switch
        {
            "primary" => Ink,
            "danger" => Coral,
            "water" => Water,
            "badge" => Acid,
            "tabSelected" => Gold,
            "tab" => Muted,
            _ => Gold
        };
        button.FlatAppearance.BorderColor = button.ForeColor;
        if (role is "badge")
            button.Padding = new Padding(6, 0, 6, 0);
        if (role is "tab" or "tabSelected")
            button.FlatAppearance.BorderSize = role == "tabSelected" ? 1 : 0;
    }

    sealed class BrandToggle : CheckBox
    {
        public BrandToggle()
        {
            AutoSize = false;
            Height = 42;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnCheckedChanged(EventArgs eventArgs)
        {
            base.OnCheckedChanged(eventArgs);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent?.BackColor ?? InkSoft);
            var switchWidth = 48;
            var switchHeight = 24;
            var switchLeft = Math.Max(4, Width - switchWidth - 12);
            var switchTop = (Height - switchHeight) / 2;
            var track = new Rectangle(switchLeft, switchTop, switchWidth, switchHeight);
            using var trackBrush = new SolidBrush(Checked && Enabled ? Acid : Color.FromArgb(42, 48, 46));
            eventArgs.Graphics.FillEllipse(trackBrush, track.Left, track.Top, track.Height, track.Height);
            eventArgs.Graphics.FillEllipse(trackBrush, track.Right - track.Height, track.Top, track.Height, track.Height);
            eventArgs.Graphics.FillRectangle(trackBrush,
                track.Left + track.Height / 2, track.Top, track.Width - track.Height, track.Height);
            var knobSize = 18;
            var knobLeft = Checked ? track.Right - knobSize - 3 : track.Left + 3;
            using var knob = new SolidBrush(Enabled ? Bone : Muted);
            eventArgs.Graphics.FillEllipse(knob, knobLeft, track.Top + 3, knobSize, knobSize);
            TextRenderer.DrawText(eventArgs.Graphics, Text, Font,
                new Rectangle(8, 0, Math.Max(0, switchLeft - 14), Height),
                Enabled ? ForeColor : Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    sealed class BrandProgressBar : Control
    {
        int _value;

        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, 0, 100);
                Invalidate();
            }
        }

        public BrandProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            var bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            using var background = new SolidBrush(Color.FromArgb(52, 57, 55));
            using var fill = new SolidBrush(Coral);
            using var border = new Pen(Color.FromArgb(82, 87, 84));
            eventArgs.Graphics.FillRectangle(background, bounds);
            if (_value > 0)
                eventArgs.Graphics.FillRectangle(fill, new Rectangle(0, 0, bounds.Width * _value / 100, bounds.Height));
            eventArgs.Graphics.DrawRectangle(border, bounds);
        }
    }

    void RefreshWindows()
    {
        var choices = NativeMethods.ListWindows();
        _loading = true;
        try
        {
            foreach (var ui in _profiles)
            {
                ui.Window.DataSource = choices.ToList();
                ui.Window.DisplayMember = nameof(WindowChoice.Display);
                var matches = choices.Where(choice =>
                    choice.ProcessName.Equals(ui.Profile.ProcessName, StringComparison.OrdinalIgnoreCase)
                    && choice.Title.Equals(ui.Profile.WindowTitle, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
                ui.Window.SelectedItem = matches.Count == 1 ? matches[0] : null;
                if (ui.Window.SelectedItem is null)
                    ui.Window.SelectedIndex = -1;
            }
        }
        finally
        {
            _loading = false;
        }
        SetStatus($"{choices.Count} janelas encontradas.");
    }

    void SelectHealthBar(ProfileUi ui)
    {
        if (!CanEdit() || !TryGetTarget(ui, out var choice, out var bounds))
            return;

        Rectangle selected = Rectangle.Empty;
        Bitmap? captured = null;
        Hide();
        try
        {
            if (!NativeMethods.TryActivate(choice.Handle))
                return;
            using var overlay = new RegionOverlay(bounds);
            if (overlay.ShowDialog() != DialogResult.OK)
                return;
            selected = overlay.SelectedRegion;
            captured = Recognition.Capture(bounds, new ScreenRegion
            {
                X = selected.X,
                Y = selected.Y,
                Width = selected.Width,
                Height = selected.Height
            });
        }
        finally
        {
            Show();
            Activate();
        }

        if (captured is null)
            return;
        using (captured)
        {
            if (!Recognition.LooksLikeBar(captured))
            {
                MessageBox.Show(this,
                    "Não foi possível localizar a parte vermelha. Marque novamente com a barra cheia.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            ui.Profile.HealthBar = new ScreenRegion
            {
                X = selected.X,
                Y = selected.Y,
                Width = selected.Width,
                Height = selected.Height
            };
            ui.Profile.FullHealthRedWidth = Recognition.RedWidth(captured);
            ui.ClearRuntime();
            SetPreview(ui, captured);
            SetLifeReading(ui, 100, 0);
        }
        RefreshProfileUi(ui);
        TrySave();
    }

    async Task ReadHealthNowAsync(ProfileUi ui)
    {
        if (_running)
        {
            MessageBox.Show(this, "A leitura já é atualizada durante o monitoramento.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ui.Profile.HealthBar.IsConfigured || ui.Window.SelectedItem is not WindowChoice)
        {
            MessageBox.Show(this, "Marque a barra de vida primeiro.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var useVisibleCapture = !ui.Profile.BackgroundMode;
        if (useVisibleCapture)
        {
            WindowState = FormWindowState.Minimized;
            await Task.Delay(350);
        }
        var temporaryCapture = false;
        try
        {
            if (ui.Profile.BackgroundMode && ui.Capture is null)
            {
                var capture = await StartBackgroundCaptureAsync(ui);
                if (!capture.Started)
                {
                    MessageBox.Show(this, capture.Error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                temporaryCapture = true;
            }
            if (!TryCaptureBar(ui, out var current, out var error))
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (current)
            {
                if (Recognition.RedWidth(current) == 0)
                {
                    MessageBox.Show(this, "A parte vermelha da barra não foi localizada.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var life = Recognition.LifePercent(current, ui.Profile.FullHealthRedWidth);
                SetLifeReading(ui, life, 100 - life);
                ui.Detected.Text = $"Leitura manual: vida em {life:F1}%.";
                SetPreview(ui, current);
            }
        }
        finally
        {
            if (temporaryCapture)
                ui.DisposeCapture();
            if (useVisibleCapture)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }
    }

    void SelectTeleport(ProfileUi ui)
    {
        if (!CanEdit() || !TrySelectPoint(ui, out var point))
            return;
        ui.Profile.TeleportPoint = new ClickPointConfig { X = point.X, Y = point.Y, Configured = true };
        RefreshProfileUi(ui);
        TrySave();
    }

    void SelectSpotWindow(ProfileUi ui)
    {
        if (!CanEdit() || !TryGetTarget(ui, out var choice, out var bounds))
            return;

        Rectangle selected = Rectangle.Empty;
        Bitmap? captured = null;
        Hide();
        try
        {
            if (!NativeMethods.TryActivate(choice.Handle))
                return;
            using var overlay = new RegionOverlay(
                bounds,
                instruction: "Arraste sobre uma parte fixa da janela de spots — Esc cancela");
            if (overlay.ShowDialog() != DialogResult.OK)
                return;
            selected = overlay.SelectedRegion;
            captured = Recognition.Capture(bounds, new ScreenRegion
            {
                X = selected.X,
                Y = selected.Y,
                Width = selected.Width,
                Height = selected.Height
            });
        }
        finally
        {
            Show();
            Activate();
        }

        if (captured is null)
            return;
        using (captured)
        using (var stream = new MemoryStream())
        {
            captured.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            ui.Profile.SpotWindowRegion = new ScreenRegion
            {
                X = selected.X,
                Y = selected.Y,
                Width = selected.Width,
                Height = selected.Height
            };
            ui.Profile.SpotWindowReferencePng = stream.ToArray();
            ui.SetSpotWindowReference(captured);
            ui.SpotMatch.Text = "Semelhança atual: 100% (referência)";
        }
        RefreshProfileUi(ui);
        TrySave();
    }

    void SelectSpotMenu(ProfileUi ui)
    {
        if (!CanEdit() || !TrySelectPoint(ui, out var point))
            return;
        ui.Profile.SpotMenuPoint = new ClickPointConfig { X = point.X, Y = point.Y, Configured = true };
        RefreshProfileUi(ui);
        TrySave();
    }

    void SelectConfirmTeleport(ProfileUi ui)
    {
        if (!CanEdit() || !TrySelectPoint(ui, out var point))
            return;
        ui.Profile.ConfirmTeleportPoint = new ClickPointConfig { X = point.X, Y = point.Y, Configured = true };
        RefreshProfileUi(ui);
        TrySave();
    }

    void ShowMarks(ProfileUi ui)
    {
        if (!CanEdit() || !TryGetTarget(ui, out var choice, out var bounds))
            return;

        var regions = new List<(Rectangle Region, string Label, Color Color)>();
        if (ui.Profile.HealthBar.IsConfigured)
            regions.Add((ToRectangle(ui.Profile.HealthBar), "Barra de vida", Color.Yellow));
        if (ui.Profile.SpotWindowRegion.IsConfigured)
            regions.Add((ToRectangle(ui.Profile.SpotWindowRegion), "Janela de spots", Color.DeepSkyBlue));

        var points = new List<(Point Point, string Label, Color Color)>();
        if (ui.Profile.TeleportPoint.Configured)
            points.Add((ToPoint(ui.Profile.TeleportPoint), "1 Item de teleporte", Color.Red));
        if (ui.Profile.SpotMenuPoint.Configured)
            points.Add((ToPoint(ui.Profile.SpotMenuPoint), "2 Abrir spots", Color.Orange));
        for (var index = 0; index < ui.Profile.Spots.Count; index++)
            points.Add((new Point(ui.Profile.Spots[index].X, ui.Profile.Spots[index].Y),
                $"3.{index + 1} {ui.Profile.Spots[index].Name}{(ui.Profile.Spots[index].Enabled ? "" : " (desativado)")}",
                ui.Profile.Spots[index].Enabled ? Color.DodgerBlue : Color.Gray));
        if (ui.Profile.ConfirmTeleportPoint.Configured)
            points.Add((ToPoint(ui.Profile.ConfirmTeleportPoint), "4 Teleportar", Color.LimeGreen));

        if (regions.Count == 0 && points.Count == 0)
        {
            MessageBox.Show(this, "Ainda não há marcações nesta janela.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Hide();
        try
        {
            if (!NativeMethods.TryActivate(choice.Handle))
                return;
            using var overlay = new RegionOverlay(
                bounds,
                instruction: "Marcações configuradas — clique ou aguarde 8 segundos para fechar",
                regions: regions,
                points: points);
            overlay.ShowDialog();
        }
        finally
        {
            Show();
            Activate();
        }
    }

    static Rectangle ToRectangle(ScreenRegion region) =>
        new(region.X, region.Y, region.Width, region.Height);

    static Point ToPoint(ClickPointConfig point) => new(point.X, point.Y);

    void AddSpot(ProfileUi ui)
    {
        if (!CanEdit() || !TrySelectPoint(ui, out var point))
            return;
        ui.Profile.Spots.Add(new SpotConfig
        {
            Name = $"Spot {ui.Profile.Spots.Count + 1}",
            X = point.X,
            Y = point.Y
        });
        RefreshSpotList(ui, ui.Profile.Spots.Count - 1);
        TrySave();
    }

    void UpdateSpot(ProfileUi ui)
    {
        var index = ui.Spots.SelectedIndex;
        if (!CanEdit() || index < 0 || !TrySelectPoint(ui, out var point))
            return;
        ui.Profile.Spots[index].X = point.X;
        ui.Profile.Spots[index].Y = point.Y;
        RefreshSpotList(ui, index);
        TrySave();
    }

    void RenameSpot(ProfileUi ui)
    {
        var index = ui.Spots.SelectedIndex;
        if (!CanEdit() || index < 0)
            return;
        var spot = ui.Profile.Spots[index];
        var name = AskName(spot.Name);
        if (string.IsNullOrWhiteSpace(name))
            return;
        spot.Name = name.Trim();
        RefreshSpotList(ui, index);
        UpdateProgress(ui);
        TrySave();
    }

    void MoveSpot(ProfileUi ui, int direction)
    {
        var from = ui.Spots.SelectedIndex;
        var to = from + direction;
        if (!CanEdit() || from < 0 || to < 0 || to >= ui.Profile.Spots.Count)
            return;
        var item = ui.Profile.Spots[from];
        ui.Profile.Spots.RemoveAt(from);
        ui.Profile.Spots.Insert(to, item);
        RefreshSpotList(ui, to);
        UpdateProgress(ui);
        TrySave();
    }

    void RemoveSpot(ProfileUi ui)
    {
        var index = ui.Spots.SelectedIndex;
        if (!CanEdit() || index < 0)
            return;
        ui.Profile.Spots.RemoveAt(index);
        ui.NextSpotIndex = Math.Max(0, FindEnabledSpot(ui.Profile.Spots, Math.Min(index, ui.Profile.Spots.Count - 1)));
        RefreshSpotList(ui, Math.Min(index, ui.Profile.Spots.Count - 1));
        UpdateProgress(ui);
        TrySave();
    }

    bool TrySelectPoint(ProfileUi ui, out Point point)
    {
        point = Point.Empty;
        if (!TryGetTarget(ui, out var choice, out var bounds))
            return false;

        var accepted = false;
        Hide();
        try
        {
            if (!NativeMethods.TryActivate(choice.Handle))
                return false;
            using var overlay = new RegionOverlay(bounds, true);
            if (overlay.ShowDialog() != DialogResult.OK)
                return false;
            point = overlay.SelectedPoint;
            accepted = true;
        }
        finally
        {
            Show();
            Activate();
        }
        return accepted;
    }

    bool TryGetTarget(ProfileUi ui, out WindowChoice choice, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        if (ui.Window.SelectedItem is not WindowChoice selected)
        {
            choice = null!;
            MessageBox.Show(this, "Escolha a janela do jogo primeiro.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        choice = selected;
        if (NativeMethods.IsIconic(choice.Handle)
            || !NativeMethods.TryGetClientScreenBounds(choice.Handle, out bounds))
        {
            MessageBox.Show(this, "Restaure a janela do jogo antes de continuar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    bool CanEdit()
    {
        if (!_running)
            return true;
        MessageBox.Show(this, "Pare a proteção antes de alterar o roteiro.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    async Task StartProtectionAsync()
    {
        var candidates = _profiles.Where(ui => ui.Profile.ProtectionEnabled
                                               && ui.Profile.IsConfigured
                                               && (!ui.Profile.UseSpots || ui.SpotWindowReference is not null)
                                               && ui.Window.SelectedItem is WindowChoice).ToList();
        if (candidates.Count == 0)
        {
            MessageBox.Show(this,
                "Conclua as marcações obrigatórias. Após esta atualização, remarque a barra uma vez com a vida cheia.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (candidates.Any(ui => !ui.Profile.BackgroundMode))
        {
            WindowState = FormWindowState.Minimized;
            await Task.Delay(500);
        }
        var started = 0;
        foreach (var ui in candidates)
            if (await StartProfileAsync(ui))
                started++;

        if (started == 0)
        {
            WindowState = FormWindowState.Normal;
            Activate();
            MessageBox.Show(this,
                "Nenhuma janela pôde ser iniciada. Verifique se está visível e não minimizada.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _running = true;
        _startStop.Text = "■  PARAR PROTEÇÃO";
        _startStop.BackColor = Coral;
        _timer.Start();
        SetStatus($"Proteção ativa em {started} janela(s).");
    }

    async Task<bool> StartProfileAsync(ProfileUi ui)
    {
        if (!ui.Profile.ProtectionEnabled
            || ui.State is ProfileState.Completed or ProfileState.SessionExpired)
            return false;
        if (HasSessionExpired(ui))
        {
            ExpireProfile(ui);
            return false;
        }
        if (ui.Profile.BackgroundMode)
        {
            var capture = await StartBackgroundCaptureAsync(ui);
            if (!capture.Started)
            {
                BeginBarSearch(ui, capture.Error);
                return true;
            }
        }
        if (!ui.Profile.BackgroundMode)
        {
            ui.DisposeCapture();
            ui.CaptureStatus.Text = "Captura: tela visível (modo compatibilidade)";
        }
        if (!TryCaptureBar(ui, out var current, out var error))
        {
            BeginBarSearch(ui, error);
            return true;
        }
        using (current)
        {
            if (Recognition.RedWidth(current) == 0)
            {
                BeginBarSearch(ui, "A parte vermelha da barra não foi localizada.");
                return true;
            }
            SetPreview(ui, current);
            var life = Recognition.LifePercent(current, ui.Profile.FullHealthRedWidth);
            SetLifeReading(ui, life, 100 - life);
        }
        ui.State = ProfileState.Monitoring;
        ui.SessionWatch.Start();
        ui.LossConfirmations = 0;
        SetProfileStatus(ui, "Monitorando", "Barra localizada; monitoramento iniciado.");
        return true;
    }

    async Task SetProfileProtectionAsync(ProfileUi ui, bool enabled)
    {
        ui.Profile.ProtectionEnabled = enabled;
        TrySave();
        if (!enabled)
        {
            ui.SessionWatch.Stop();
            ui.DisposeCapture();
            ui.ResetStableCandidate();
            ui.LossConfirmations = 0;
            if (ui.State is not (ProfileState.Completed or ProfileState.SessionExpired))
            {
                ui.State = ProfileState.Stopped;
                SetProfileStatus(ui, "Desativada", "Proteção desligada para esta janela.");
            }
            SetStatus($"Proteção desativada em {ui.Profile.Name}.");
            return;
        }

        if (!_running)
        {
            if (ui.State is not (ProfileState.Completed or ProfileState.SessionExpired))
                SetProfileStatus(ui, "Pronta", "Será iniciada pelo botão Iniciar proteção.");
            return;
        }
        if (ui.State is ProfileState.Completed or ProfileState.SessionExpired)
        {
            SetStatus($"{ui.Profile.Name} permanece encerrada; reinicie sua sequência para ativá-la.", true);
            return;
        }

        var useVisibleCapture = !ui.Profile.BackgroundMode;
        if (useVisibleCapture)
        {
            WindowState = FormWindowState.Minimized;
            await Task.Delay(500);
        }
        try
        {
            if (await StartProfileAsync(ui))
                SetStatus($"Proteção ativada em {ui.Profile.Name}.");
        }
        finally
        {
            if (useVisibleCapture)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }
    }

    async Task<(bool Started, string Error)> StartBackgroundCaptureAsync(ProfileUi ui)
    {
        ui.DisposeCapture();
        if (ui.Window.SelectedItem is not WindowChoice choice)
            return (false, "Janela não selecionada.");
        ui.CaptureStatus.Text = "Captura: iniciando segundo plano...";
        var result = await WindowCapture.StartAsync(choice.Handle);
        if (result.Capture is null)
        {
            ui.CaptureStatus.Text = "Captura: incompatível ou sem imagem";
            return (false, result.Error);
        }
        ui.Capture = result.Capture;
        ui.CaptureStatus.Text = "Captura: segundo plano ativo";
        return (true, "");
    }

    void BeginBarSearch(ProfileUi ui, string reason)
    {
        if (ui.State != ProfileState.Searching)
            ui.ResumeAfterSearch = ui.State == ProfileState.Stabilizing
                ? ProfileState.Stabilizing
                : ProfileState.Monitoring;
        ui.SessionWatch.Stop();
        ui.DisposeCapture();
        ui.State = ProfileState.Searching;
        ui.NextBarSearchAt = DateTimeOffset.Now.AddSeconds(BarSearchIntervalSeconds);
        ui.LossConfirmations = 0;
        ui.ResetStableCandidate();
        ui.CaptureStatus.Text = "Captura: procurando barra";
        SetProfileStatus(ui, "Procurando barra", $"{reason} Nova tentativa em {BarSearchIntervalSeconds} segundos.");
    }

    void StopProtection()
    {
        _running = false;
        _timer.Stop();
        foreach (var ui in _profiles)
            ui.DisposeCapture();
        foreach (var ui in _profiles.Where(item => IsActiveState(item.State)))
        {
            ui.SessionWatch.Stop();
            ui.State = ProfileState.Stopped;
            SetProfileStatus(ui, "Parada", "Progresso preservado.");
        }
        _startStop.Text = "⛨  INICIAR PROTEÇÃO";
        _startStop.BackColor = Acid;
        SetStatus("Proteção parada.");
    }

    async void MonitorTick(object? sender, EventArgs eventArgs)
    {
        if (!_running || _busy)
            return;
        _busy = true;
        _timer.Stop();
        try
        {
            foreach (var ui in _profiles)
                await ProcessProfileAsync(ui);
        }
        finally
        {
            _busy = false;
            if (_running && _profiles.Any(item => IsActiveState(item.State)))
                _timer.Start();
            else if (_running)
            {
                _running = false;
                _startStop.Text = "⛨  INICIAR PROTEÇÃO";
                _startStop.BackColor = Acid;
                SetStatus("Todas as sequências ativas foram concluídas ou pausadas.");
            }
        }
    }

    async Task ProcessProfileAsync(ProfileUi ui)
    {
        if (!ui.Profile.ProtectionEnabled)
            return;
        if (ui.State == ProfileState.Searching)
        {
            await SearchForBarAsync(ui);
            return;
        }
        if (ui.State is not (ProfileState.Monitoring or ProfileState.Stabilizing))
            return;
        UpdateTime(ui);
        if (HasSessionExpired(ui))
        {
            ExpireProfile(ui);
            return;
        }
        if (!TryCaptureBar(ui, out var current, out var error))
        {
            BeginBarSearch(ui, error);
            return;
        }

        using (current)
        {
            SetPreview(ui, current);
            if (Recognition.RedWidth(current) == 0)
            {
                BeginBarSearch(ui, "A parte vermelha da barra não foi localizada.");
                return;
            }
            if (ui.State == ProfileState.Stabilizing)
            {
                ProcessStabilizing(ui, current);
                return;
            }

            var life = Recognition.LifePercent(current, ui.Profile.FullHealthRedWidth);
            var loss = 100 - life;
            SetLifeReading(ui, life, loss);
            ui.Detected.Text = $"Vida estimada: {life:F0}% — queda: {loss:F1}%";

            if (loss >= (double)ui.Profile.DropLimitPercent)
                ui.LossConfirmations++;
            else
                ui.LossConfirmations = 0;

            if (ui.LossConfirmations >= 3)
                await ReactAsync(ui);
        }
    }

    async Task SearchForBarAsync(ProfileUi ui)
    {
        UpdateTime(ui);
        if (!IsBarSearchDue(DateTimeOffset.Now, ui.NextBarSearchAt))
            return;

        if (ui.Profile.BackgroundMode)
        {
            var capture = await StartBackgroundCaptureAsync(ui);
            if (!capture.Started)
            {
                BeginBarSearch(ui, capture.Error);
                return;
            }
        }
        if (!TryCaptureBar(ui, out var current, out var error))
        {
            BeginBarSearch(ui, error);
            return;
        }
        using (current)
        {
            SetPreview(ui, current);
            if (Recognition.RedWidth(current) == 0)
            {
                BeginBarSearch(ui, "A parte vermelha da barra ainda não foi localizada.");
                return;
            }

            ui.State = ui.ResumeAfterSearch;
            ui.SessionWatch.Start();
            ui.LossConfirmations = 0;
            if (ui.State == ProfileState.Stabilizing)
            {
                ui.RearmNotBefore = DateTimeOffset.Now;
                ui.ResetStableCandidate();
                SetProfileStatus(ui, "Aguardando", "Barra reencontrada; confirmando estabilidade.");
                return;
            }

            var life = Recognition.LifePercent(current, ui.Profile.FullHealthRedWidth);
            SetLifeReading(ui, life, 100 - life);
            SetProfileStatus(ui, "Monitorando", $"Barra reencontrada; vida em {life:F0}%.");
        }
    }

    static bool IsActiveState(ProfileState state) =>
        state is ProfileState.Monitoring or ProfileState.Reacting
            or ProfileState.Stabilizing or ProfileState.Searching;

    static bool IsBarSearchDue(DateTimeOffset now, DateTimeOffset nextSearch) => now >= nextSearch;

    async Task ReactAsync(ProfileUi ui)
    {
        if (!ui.Profile.ProtectionEnabled)
            return;
        if (ui.Profile.UseSpots && !ui.Profile.Spots.Any(item => item.Enabled))
        {
            PauseProfile(ui, "Nenhum spot ativo.");
            return;
        }

        ui.State = ProfileState.Reacting;
        ui.LossConfirmations = 0;
        SpotConfig? spot = null;
        var executedSpotIndex = -1;
        if (ui.Profile.UseSpots)
        {
            ui.NextSpotIndex = FindEnabledSpot(ui.Profile.Spots, ui.NextSpotIndex);
            executedSpotIndex = ui.NextSpotIndex;
            spot = ui.Profile.Spots[executedSpotIndex];
        }
        SetProfileStatus(ui, "Reagindo", spot is null
            ? "Usando somente o teleporte..."
            : $"Usando teleporte e depois {spot.Name}...");

        if (!await ExecuteClicksAsync(ui, spot, true))
            return;

        if (ui.Profile.UseSpots)
        {
            var advanced = AdvanceSequence(
                executedSpotIndex,
                ui.CompletedCycles,
                ui.Profile.Spots,
                ui.Profile.CycleCount);
            ui.NextSpotIndex = advanced.NextSpot;
            ui.CompletedCycles = advanced.CompletedCycles;

            if (advanced.Finished)
            {
                ui.State = ProfileState.Completed;
                ui.SessionWatch.Stop();
                ui.DisposeCapture();
                ui.DisposeImages();
                SetProfileStatus(ui, "Concluída", "Todos os ciclos foram executados.");
                UpdateProgress(ui);
                return;
            }
        }

        ui.State = ProfileState.Stabilizing;
        ui.RearmNotBefore = DateTimeOffset.Now.AddMilliseconds(ui.Profile.RearmDelayMs);
        ui.ResetStableCandidate();
        SetProfileStatus(ui, "Aguardando", "Esperando o jogo e a barra estabilizarem.");
        UpdateProgress(ui);
    }

    async Task<bool> ExecuteClicksAsync(ProfileUi ui, SpotConfig? spot, bool pauseOnError)
    {
        if (!ui.Profile.ProtectionEnabled || ui.Window.SelectedItem is not WindowChoice choice)
            return false;

        if (!Click(ui.Profile.TeleportPoint.X, ui.Profile.TeleportPoint.Y, "Item de teleporte"))
            return false;

        if (spot is null)
            return true;

        await Task.Delay(ui.Profile.TeleportToSpotDelayMs);
        if (!ui.Profile.ProtectionEnabled)
            return false;
        if (!TryMeasureSpotWindow(ui, out var similarity, out var error))
            return Fail(error);

        if (similarity < (double)ui.Profile.SpotWindowMinimumSimilarity)
        {
            if (!Click(ui.Profile.SpotMenuPoint.X, ui.Profile.SpotMenuPoint.Y, "Abrir spots"))
                return false;
            await Task.Delay(ui.Profile.TeleportToSpotDelayMs);
            if (!ui.Profile.ProtectionEnabled)
                return false;
            if (!TryMeasureSpotWindow(ui, out similarity, out error))
                return Fail(error);
            if (similarity < (double)ui.Profile.SpotWindowMinimumSimilarity)
                return Fail($"Janela de spots não reconhecida ({similarity:F0}% de semelhança). Refaça a marcação ou ajuste o limite.");
        }

        if (!Click(spot.X, spot.Y, spot.Name))
            return false;
        await Task.Delay(ui.Profile.TeleportToSpotDelayMs);
        if (!ui.Profile.ProtectionEnabled)
            return false;
        for (var attempt = 1; attempt <= ui.Profile.TeleportRetryCount; attempt++)
        {
            SetProfileStatus(ui, "Reagindo", $"Confirmando teleporte ({attempt}/{ui.Profile.TeleportRetryCount})...");
            if (!Click(ui.Profile.ConfirmTeleportPoint.X, ui.Profile.ConfirmTeleportPoint.Y, "Botão Teleportar"))
                return false;
            await Task.Delay(ui.Profile.TeleportToSpotDelayMs);
            if (!ui.Profile.ProtectionEnabled)
                return false;
            if (!TryMeasureSpotWindow(ui, out similarity, out error))
                return Fail(error);
            if (similarity < (double)ui.Profile.SpotWindowMinimumSimilarity)
                return true;
        }
        return Fail($"Teleporte não confirmado após {ui.Profile.TeleportRetryCount} tentativa(s). A janela de spots continuou aberta.");

        bool Click(int x, int y, string name)
        {
            if (!ui.Profile.ProtectionEnabled)
                return false;
            var clicked = ui.Profile.BackgroundMode
                ? NativeMethods.TryBackgroundClick(choice.Handle, x, y, out var clickError)
                : NativeMethods.TryClick(choice.Handle, x, y, out clickError);
            if (clicked)
                return true;
            return Fail($"{name} não clicado: {clickError}");
        }

        bool Fail(string message)
        {
            if (pauseOnError)
                PauseProfile(ui, message);
            else
                MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    void ProcessStabilizing(ProfileUi ui, Bitmap current)
    {
        var remaining = ui.RearmNotBefore - DateTimeOffset.Now;
        if (remaining > TimeSpan.Zero)
        {
            ui.Detected.Text = $"Aguardando {Math.Ceiling(remaining.TotalSeconds)} s antes de procurar a barra.";
            return;
        }
        if (ui.StableCandidate is null)
        {
            ui.SetStableCandidate(current);
            ui.StableSince = DateTimeOffset.Now;
            return;
        }

        if (Recognition.DifferencePercent(ui.StableCandidate, current) > 2)
        {
            ui.SetStableCandidate(current);
            ui.StableSince = DateTimeOffset.Now;
            ui.Detected.Text = "Barra encontrada; aguardando ficar estável.";
            return;
        }

        var stableFor = DateTimeOffset.Now - ui.StableSince;
        ui.Detected.Text = $"Barra estável por {stableFor.TotalSeconds:F1} s.";
        if (stableFor.TotalMilliseconds < ui.Profile.StableTimeMs)
            return;

        ui.ResetStableCandidate();
        ui.State = ProfileState.Monitoring;
        var life = Recognition.LifePercent(current, ui.Profile.FullHealthRedWidth);
        SetLifeReading(ui, life, 100 - life);
        ui.LossConfirmations = 0;
        SetProfileStatus(ui, "Monitorando", $"Barra estabilizada; vida em {life:F0}%.");
    }

    async Task RearmProfileAsync(ProfileUi ui)
    {
        if (ui.State == ProfileState.SessionExpired)
        {
            MessageBox.Show(this,
                "O limite desta sessão terminou. Use 'Recomeçar do Spot 1' para zerar o tempo.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }
        var wasRunning = _running;
        var useVisibleCapture = !ui.Profile.BackgroundMode;
        if (useVisibleCapture)
        {
            WindowState = FormWindowState.Minimized;
            await Task.Delay(350);
        }
        var temporaryCapture = false;
        try
        {
        if (ui.Profile.BackgroundMode && ui.Capture is null)
        {
            var capture = await StartBackgroundCaptureAsync(ui);
            if (!capture.Started)
            {
                if (wasRunning)
                    BeginBarSearch(ui, capture.Error);
                else
                    MessageBox.Show(this, capture.Error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            temporaryCapture = !wasRunning;
        }
        if (TryCaptureBar(ui, out var current, out var error))
        {
            using (current)
            {
                if (Recognition.RedWidth(current) == 0)
                {
                    if (wasRunning)
                        BeginBarSearch(ui, "A parte vermelha da barra não foi localizada.");
                    else
                        MessageBox.Show(this, "A parte vermelha da barra não foi localizada.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SetPreview(ui, current);
                var life = Recognition.LifePercent(current, ui.Profile.FullHealthRedWidth);
                SetLifeReading(ui, life, 100 - life);
            }
            ui.State = wasRunning ? ProfileState.Monitoring : ProfileState.Stopped;
            if (wasRunning)
            ui.SessionWatch.Start();
            ui.LossConfirmations = 0;
            SetProfileStatus(ui, wasRunning ? "Monitorando" : "Parada", "Leitura da barra atualizada.");
        }
        else
        {
            if (wasRunning)
                BeginBarSearch(ui, error);
            else
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        }
        finally
        {
            if (!wasRunning && temporaryCapture)
                ui.DisposeCapture();
            if (useVisibleCapture)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }
    }

    async Task ResetSequenceAsync(ProfileUi ui)
    {
        ui.NextSpotIndex = Math.Max(0, FindEnabledSpot(ui.Profile.Spots));
        ui.CompletedCycles = 0;
        ui.SessionWatch.Reset();
        UpdateTime(ui);
        UpdateProgress(ui);
        if (_running)
            await RearmProfileAsync(ui);
        else
        {
            ui.State = ProfileState.Stopped;
            ui.DisposeImages();
            SetProfileStatus(ui, "Parada", "Sequência reiniciada no primeiro spot ativo.");
        }
    }

    async Task TestReactionAsync(ProfileUi ui)
    {
        if (_running)
        {
            MessageBox.Show(this, "Pare a proteção antes de testar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ui.Profile.IsConfigured
            || (ui.Profile.UseSpots && ui.SpotWindowReference is null)
            || ui.Window.SelectedItem is not WindowChoice)
        {
            MessageBox.Show(this, "Conclua os quatro passos antes de testar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SpotConfig? spot = null;
        if (ui.Profile.UseSpots)
        {
            ui.NextSpotIndex = FindEnabledSpot(ui.Profile.Spots, ui.NextSpotIndex);
            spot = ui.Profile.Spots[ui.NextSpotIndex];
        }
        if (MessageBox.Show(this,
                spot is null
                    ? "O teste clicará somente no item de teleporte. Continuar?"
                    : $"O teste abrirá o item de teleporte, localizará a janela de spots, escolherá {spot.Name} e confirmará. Continuar?",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        var temporaryCapture = false;
        if (ui.Profile.BackgroundMode && spot is not null)
        {
            var capture = await StartBackgroundCaptureAsync(ui);
            if (!capture.Started)
            {
                MessageBox.Show(this, capture.Error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            temporaryCapture = true;
        }
        await ExecuteClicksAsync(ui, spot, false);
        if (temporaryCapture)
            ui.DisposeCapture();
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    async Task TestBackgroundClickAsync(ProfileUi ui)
    {
        if (_running)
        {
            MessageBox.Show(this, "Pare a proteção antes de testar a compatibilidade.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (ui.Window.SelectedItem is not WindowChoice choice || !ui.Profile.TeleportPoint.Configured)
        {
            MessageBox.Show(this, "Escolha a janela e marque o item de teleporte primeiro.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this,
                "Após confirmar, aguarde 5 segundos. Mantenha o jogo aberto; ele pode ficar coberto, mas não minimizado. O clique será enviado sem mover o cursor nem ativar a janela.",
                Text,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information) != DialogResult.OK)
            return;

        await Task.Delay(5000);
        if (NativeMethods.TryBackgroundClick(
                choice.Handle,
                ui.Profile.TeleportPoint.X,
                ui.Profile.TeleportPoint.Y,
                out var error))
        {
            SetStatus("Clique em segundo plano enviado. Confira se o item de teleporte abriu.");
            return;
        }

        MessageBox.Show(this, $"Teste em segundo plano não enviado: {error}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    async Task TestBackgroundCaptureAsync(ProfileUi ui)
    {
        if (_running)
        {
            MessageBox.Show(this, "Pare a proteção antes de testar a captura.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (ui.Window.SelectedItem is not WindowChoice choice || !ui.Profile.HealthBar.IsConfigured)
        {
            MessageBox.Show(this, "Escolha a janela e marque a barra de vida primeiro.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this,
                "Após confirmar, aguarde 5 segundos. Mantenha o jogo aberto; ele pode ficar coberto, mas não minimizado. A barra será capturada sem ativar a janela.",
                Text,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information) != DialogResult.OK)
            return;

        await Task.Delay(5000);
        var result = await WindowCapture.StartAsync(choice.Handle);

        if (result.Capture is null)
        {
            MessageBox.Show(this, result.Error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (result.Capture)
        {
            if (!result.Capture.TryGetRegion(ui.Profile.HealthBar, out var bar, out var error))
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (bar)
            {
            var content = Recognition.ContentPercent(bar);
            var looksLikeBar = Recognition.LooksLikeBar(bar);
            SetPreview(ui, bar);
            ui.Detected.Text = $"Captura em segundo plano: {content:F0}% de conteúdo visível.";
            MessageBox.Show(this,
                $"Captura recebida: {content:F0}% de conteúdo visível. A imagem {(looksLikeBar ? "parece" : "não parece")} conter a barra. Confira o visualizador.",
                Text,
                MessageBoxButtons.OK,
                looksLikeBar ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }
    }

    bool TryMeasureSpotWindow(ProfileUi ui, out double similarity, out string error)
    {
        similarity = 0;
        if (ui.SpotWindowReference is null)
        {
            error = "A referência da janela de spots está ausente. Marque-a novamente.";
            return false;
        }
        if (!TryCaptureRegion(ui, ui.Profile.SpotWindowRegion, "A janela de spots", out var current, out error))
            return false;

        using (current)
            similarity = 100 - Recognition.DifferencePercent(ui.SpotWindowReference, current);
        similarity = Math.Clamp(similarity, 0, 100);
        ui.SpotMatch.Text = $"Semelhança atual: {similarity:F0}%";
        ui.SpotMatch.ForeColor = similarity >= (double)ui.Profile.SpotWindowMinimumSimilarity
            ? Acid
            : Coral;
        return true;
    }

    bool TryCaptureBar(ProfileUi ui, out Bitmap bitmap, out string error) =>
        TryCaptureRegion(ui, ui.Profile.HealthBar, "A barra", out bitmap, out error);

    bool TryCaptureRegion(ProfileUi ui, ScreenRegion region, string name, out Bitmap bitmap, out string error)
    {
        bitmap = null!;
        error = "";
        if (ui.Window.SelectedItem is not WindowChoice choice || !NativeMethods.IsWindow(choice.Handle))
        {
            error = "Janela não encontrada.";
            return false;
        }
        if (ui.Profile.BackgroundMode)
        {
            if (ui.Capture is null)
            {
                error = "A captura em segundo plano não foi iniciada.";
                ui.CaptureStatus.Text = "Captura: parada";
                return false;
            }
            if (ui.Capture.TryGetRegion(region, out bitmap, out error))
            {
                ui.CaptureStatus.Text = "Captura: segundo plano ativo";
                return true;
            }
            ui.CaptureStatus.Text = error.Contains("preta", StringComparison.OrdinalIgnoreCase)
                ? "Captura: quadro preto"
                : "Captura: sem quadros recentes";
            return false;
        }
        if (NativeMethods.IsIconic(choice.Handle)
            || !NativeMethods.TryGetClientScreenBounds(choice.Handle, out var client))
        {
            error = "Janela minimizada ou indisponível.";
            return false;
        }
        if (!region.IsConfigured
            || region.X < 0 || region.Y < 0
            || region.X + region.Width > client.Width
            || region.Y + region.Height > client.Height)
        {
            error = $"{name} ficou fora da janela. Marque-a novamente.";
            return false;
        }

        try
        {
            bitmap = Recognition.Capture(client, region);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    void PauseProfile(ProfileUi ui, string reason)
    {
        ui.SessionWatch.Stop();
        ui.DisposeCapture();
        if (ui.Profile.BackgroundMode)
            ui.CaptureStatus.Text = "Captura: pausada por falha";
        ui.State = ProfileState.Error;
        ui.ResetStableCandidate();
        SetProfileStatus(ui, "Pausada", reason);
    }

    void SetProfileStatus(ProfileUi ui, string state, string detail)
    {
        ui.StateLabel.Text = state;
        ui.StateLabel.ForeColor = state switch
        {
            "Monitorando" or "Pronta" or "Concluída" => Acid,
            "Pausada" or "Tempo encerrado" or "Desativada" => Coral,
            "Reagindo" or "Aguardando" or "Procurando barra" => Gold,
            _ => Bone
        };
        ui.Detected.Text = detail;
        UpdateProgress(ui);
        UpdateTime(ui);
    }

    void UpdateProgress(ProfileUi ui)
    {
        if (ui.State == ProfileState.SessionExpired)
        {
            ui.Progress.Text = "Sessão encerrada pelo limite de tempo.";
            return;
        }
        if (ui.State == ProfileState.Completed)
        {
            ui.Progress.Text = $"Concluído: {ui.CompletedCycles} de {ui.Profile.CycleCount} ciclo(s).";
            return;
        }
        if (!ui.Profile.UseSpots)
        {
            ui.Progress.Text = "Próxima reação: somente teleporte — repete até o fim da sessão.";
            return;
        }
        var activeCount = ui.Profile.Spots.Count(item => item.Enabled);
        if (activeCount == 0)
        {
            ui.Progress.Text = "Ative ao menos um spot.";
            return;
        }
        ui.NextSpotIndex = FindEnabledSpot(ui.Profile.Spots, ui.NextSpotIndex);
        var cycle = Math.Min(ui.CompletedCycles + 1, ui.Profile.CycleCount);
        ui.Progress.Text = $"Próxima reação: {ui.Profile.Spots[ui.NextSpotIndex].Name} — ciclo {cycle} de {ui.Profile.CycleCount} — {activeCount}/{ui.Profile.Spots.Count} ativo(s).";
    }

    void RefreshProfileUi(ProfileUi ui)
    {
        var barConfigured = ui.Profile.HealthBar.IsConfigured && ui.Profile.FullHealthRedWidth > 0;
        ui.BarStatus.Text = barConfigured
            ? $"✓ {ui.Profile.HealthBar.Width} × {ui.Profile.HealthBar.Height}"
            : ui.Profile.HealthBar.IsConfigured ? "Remarque com a vida cheia" : "Não configurado";
        ui.BarStatus.ForeColor = barConfigured ? Acid : Coral;
        ui.TeleportStatus.Text = ui.Profile.TeleportPoint.Configured ? "✓ Ponto selecionado" : "Não configurado";
        ui.TeleportStatus.ForeColor = ui.Profile.TeleportPoint.Configured ? Acid : Coral;
        var spotWindowConfigured = ui.Profile.SpotWindowRegion.IsConfigured && ui.SpotWindowReference is not null;
        ui.SpotWindowStatus.Text = spotWindowConfigured
            ? $"✓ {ui.Profile.SpotWindowRegion.Width} × {ui.Profile.SpotWindowRegion.Height}"
            : "Não configurado";
        ui.SpotWindowStatus.ForeColor = spotWindowConfigured ? Acid : Coral;
        ui.SpotMenuStatus.Text = ui.Profile.SpotMenuPoint.Configured ? "✓ Ponto selecionado" : "Não configurado";
        ui.SpotMenuStatus.ForeColor = ui.Profile.SpotMenuPoint.Configured ? Acid : Coral;
        ui.ConfirmTeleportStatus.Text = ui.Profile.ConfirmTeleportPoint.Configured ? "✓ Ponto selecionado" : "Não configurado";
        ui.ConfirmTeleportStatus.ForeColor = ui.Profile.ConfirmTeleportPoint.Configured ? Acid : Coral;
        if (!spotWindowConfigured)
        {
            ui.SpotMatch.Text = "Semelhança atual: --";
            ui.SpotMatch.ForeColor = Bone;
        }
        RefreshSpotList(ui, ui.Spots.SelectedIndex);
        if (ui.Capture is null)
            ui.CaptureStatus.Text = ui.Profile.BackgroundMode
                ? "Captura: segundo plano será iniciado com a proteção"
                : "Captura: tela visível (modo compatibilidade)";
        UpdateProgress(ui);
        UpdateTime(ui);
    }

    static TimeSpan SessionLimit(ProfileUi ui) => TimeSpan.FromMinutes(ui.Profile.SessionLimitMinutes);

    static bool HasSessionExpired(ProfileUi ui) =>
        HasSessionExpired(ui.SessionWatch.Elapsed, ui.Profile.SessionLimitMinutes);

    static bool HasSessionExpired(TimeSpan elapsed, int limitMinutes) =>
        elapsed >= TimeSpan.FromMinutes(limitMinutes);

    static void UpdateTime(ProfileUi ui)
    {
        var active = ui.SessionWatch.Elapsed;
        var remaining = SessionLimit(ui) - active;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;
        ui.TimeLabel.Text = FormatTime(active);
        ui.RemainingTimeLabel.Text = FormatTime(remaining);
    }

    void ExpireProfile(ProfileUi ui)
    {
        ui.SessionWatch.Stop();
        ui.State = ProfileState.SessionExpired;
        ui.DisposeCapture();
        ui.DisposeImages();
        UpdateTime(ui);
        SetProfileStatus(ui, "Tempo encerrado", "Esta janela foi desativada pelo limite da sessão.");
    }

    static string FormatTime(TimeSpan time) =>
        $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";

    static void RefreshSpotList(ProfileUi ui, int selected = -1)
    {
        ui.UpdatingSpotChecks = true;
        ui.Spots.BeginUpdate();
        try
        {
            ui.Spots.Items.Clear();
            for (var index = 0; index < ui.Profile.Spots.Count; index++)
                ui.Spots.Items.Add($"{index + 1}. {ui.Profile.Spots[index]}", ui.Profile.Spots[index].Enabled);
        }
        finally
        {
            ui.Spots.EndUpdate();
            ui.UpdatingSpotChecks = false;
        }
        if (selected >= 0 && selected < ui.Spots.Items.Count)
            ui.Spots.SelectedIndex = selected;
    }

    static void SetPreview(ProfileUi ui, Bitmap bitmap)
    {
        var previous = ui.Preview.Image;
        ui.Preview.Image = new Bitmap(bitmap);
        previous?.Dispose();
    }

    static void SetLifeReading(ProfileUi ui, double life, double loss)
    {
        var safeLife = Math.Clamp(life, 0, 100);
        ui.LifeMeter.Value = (int)Math.Round(safeLife);
        ui.LifePercent.Text = $"{safeLife:F0}%";
        ui.LifeLoss.Text = $"Queda {Math.Clamp(loss, 0, 100):F1}%";
    }

    static string? AskName(string current)
    {
        using var dialog = new Form
        {
            Text = "Nome do spot",
            Width = 360,
            Height = 145,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var text = new TextBox { Text = current, Left = 15, Top = 18, Width = 310 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 165, Top = 55, Width = 75 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 250, Top = 55, Width = 75 };
        dialog.Controls.AddRange([text, ok, cancel]);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog() == DialogResult.OK ? text.Text : null;
    }

    void TrySave()
    {
        try
        {
            ConfigStore.Save(_config);
        }
        catch (Exception error)
        {
            SetStatus($"Não foi possível salvar: {error.Message}", true);
        }
    }

    void SetStatus(string message, bool error = false)
    {
        _status.Text = message;
        _status.ForeColor = error ? Coral : Muted;
    }

    async Task CheckForUpdatesAsync(bool reportLatest = false)
    {
        if (_checkingUpdate)
            return;
        _checkingUpdate = true;
        try
        {
            _updateStatus.Text = "VERIFICANDO";
            SetStatus("Verificando atualizações...");
            var update = await Updater.CheckAsync();
            if (update is null)
            {
                _updateStatus.Text = "ATUALIZADO";
                SetStatus($"Ronaldinho v{Updater.CurrentVersion.ToString(3)} está atualizado.");
                if (reportLatest)
                    MessageBox.Show(this, "Você já está usando a versão mais recente.", "Atualização",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var answer = MessageBox.Show(this,
                $"A versão {update.Tag} está disponível. Deseja baixar, instalar e reiniciar agora?",
                "Atualização disponível", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            _updateStatus.Text = $"{update.Tag} DISPONÍVEL";
            if (answer != DialogResult.Yes)
            {
                SetStatus($"Atualização {update.Tag} disponível.");
                return;
            }

            SetStatus($"Baixando atualização {update.Tag}...");
            await Updater.InstallAndRestartAsync(update);
            Application.Exit();
        }
        catch (Exception error)
        {
            _updateStatus.Text = "OFFLINE";
            SetStatus($"Não foi possível verificar atualizações: {error.Message}", true);
            if (reportLatest)
                MessageBox.Show(this, error.Message, "Atualização", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _checkingUpdate = false;
        }
    }

    static (int NextSpot, int CompletedCycles, bool Finished) AdvanceSequence(
        int currentSpot,
        int completedCycles,
        IList<SpotConfig> spots,
        int cycleCount)
    {
        if (currentSpot < 0 || currentSpot >= spots.Count || cycleCount < 1)
            throw new ArgumentOutOfRangeException(nameof(currentSpot));

        var nextSpot = -1;
        for (var index = currentSpot + 1; index < spots.Count; index++)
        {
            if (!spots[index].Enabled)
                continue;
            nextSpot = index;
            break;
        }
        if (nextSpot < 0)
        {
            nextSpot = FindEnabledSpot(spots);
            if (nextSpot < 0)
                throw new InvalidOperationException("A sequência não possui spots ativos.");
            completedCycles++;
        }
        return (nextSpot, completedCycles, completedCycles >= cycleCount);
    }

    static int FindEnabledSpot(IList<SpotConfig> spots, int start = 0)
    {
        if (spots.Count == 0)
            return -1;
        start %= spots.Count;
        if (start < 0)
            start += spots.Count;
        for (var offset = 0; offset < spots.Count; offset++)
        {
            var index = (start + offset) % spots.Count;
            if (spots[index].Enabled)
                return index;
        }
        return -1;
    }

    public static void RunSelfTest()
    {
        var route = new List<SpotConfig>
        {
            new(),
            new() { Enabled = false },
            new()
        };
        var spot = 0;
        var cycles = 0;
        for (var reaction = 1; reaction <= 4; reaction++)
        {
            var result = AdvanceSequence(spot, cycles, route, 2);
            spot = result.NextSpot;
            cycles = result.CompletedCycles;
            if (result.Finished != (reaction == 4))
                throw new InvalidOperationException("Falha no autoteste do ciclo de spots.");
        }
        if (spot != 0 || cycles != 2)
            throw new InvalidOperationException("Falha no estado final do ciclo de spots.");
        if (FormatTime(TimeSpan.FromSeconds(3661)) != "01:01:01")
            throw new InvalidOperationException("Falha no formato do contador de sessão.");
        if (HasSessionExpired(TimeSpan.FromSeconds(3599), 60)
            || !HasSessionExpired(TimeSpan.FromHours(1), 60))
            throw new InvalidOperationException("Falha no limite do contador de sessão.");
        if (!IsActiveState(ProfileState.Searching) || IsActiveState(ProfileState.Error))
            throw new InvalidOperationException("Falha no estado de procura automática da barra.");
        var nextSearch = DateTimeOffset.UnixEpoch.AddSeconds(BarSearchIntervalSeconds);
        if (IsBarSearchDue(nextSearch.AddMilliseconds(-1), nextSearch)
            || !IsBarSearchDue(nextSearch, nextSearch))
            throw new InvalidOperationException("Falha no intervalo de procura automática da barra.");

        var profile = new WindowProfile
        {
            HealthBar = new ScreenRegion { Width = 100, Height = 10 },
            FullHealthRedWidth = 100,
            TeleportPoint = new ClickPointConfig { Configured = true },
            UseSpots = false
        };
        if (!profile.IsConfigured)
            throw new InvalidOperationException("O modo sem spots deveria estar configurado.");
        profile.UseSpots = true;
        if (profile.IsConfigured)
            throw new InvalidOperationException("O modo com spots deveria exigir a rota completa.");
        profile.SpotWindowRegion = new ScreenRegion { Width = 20, Height = 20 };
        profile.SpotWindowReferencePng = [1];
        profile.SpotMenuPoint = new ClickPointConfig { Configured = true };
        profile.ConfirmTeleportPoint = new ClickPointConfig { Configured = true };
        profile.Spots.Add(new SpotConfig());
        if (!profile.IsConfigured)
            throw new InvalidOperationException("A rota completa de spots deveria estar configurada.");
        profile.TeleportRetryCount = 99;
        var config = new AppConfig { Windows = [profile, new()] };
        config.Normalize();
        if (profile.TeleportRetryCount != 20)
            throw new InvalidOperationException("Falha no limite de tentativas do teleporte.");
    }

    enum ProfileState
    {
        Stopped,
        Monitoring,
        Reacting,
        Stabilizing,
        Searching,
        Completed,
        SessionExpired,
        Error
    }

    sealed class ProfileUi : IDisposable
    {
        public WindowProfile Profile { get; }
        public ComboBox Window { get; }
        public Label BarStatus { get; }
        public Label TeleportStatus { get; }
        public Label SpotWindowStatus { get; }
        public Label SpotMenuStatus { get; }
        public Label ConfirmTeleportStatus { get; }
        public Label SpotMatch { get; }
        public PictureBox Preview { get; }
        public CheckedListBox Spots { get; }
        public Label StateLabel { get; }
        public Label Progress { get; }
        public Label Detected { get; }
        public Label CaptureStatus { get; }
        public Label TimeLabel { get; }
        public Label RemainingTimeLabel { get; }
        public BrandProgressBar LifeMeter { get; }
        public Label LifePercent { get; }
        public Label LifeLoss { get; }
        public System.Diagnostics.Stopwatch SessionWatch { get; } = new();
        public ProfileState State { get; set; } = ProfileState.Stopped;
        public int NextSpotIndex { get; set; }
        public int CompletedCycles { get; set; }
        public int LossConfirmations { get; set; }
        public bool UpdatingSpotChecks { get; set; }
        public Bitmap? StableCandidate { get; private set; }
        public Bitmap? SpotWindowReference { get; private set; }
        public WindowCapture? Capture { get; set; }
        public DateTimeOffset StableSince { get; set; }
        public DateTimeOffset RearmNotBefore { get; set; }
        public DateTimeOffset NextBarSearchAt { get; set; }
        public ProfileState ResumeAfterSearch { get; set; } = ProfileState.Monitoring;

        public ProfileUi(
            WindowProfile profile,
            ComboBox window,
            Label barStatus,
            Label teleportStatus,
            Label spotWindowStatus,
            Label spotMenuStatus,
            Label confirmTeleportStatus,
            Label spotMatch,
            PictureBox preview,
            CheckedListBox spots,
            Label stateLabel,
            Label progress,
            Label detected,
            Label captureStatus,
            Label timeLabel,
            BrandProgressBar lifeMeter,
            Label lifePercent,
            Label lifeLoss,
            Label remainingTimeLabel)
        {
            Profile = profile;
            Window = window;
            BarStatus = barStatus;
            TeleportStatus = teleportStatus;
            SpotWindowStatus = spotWindowStatus;
            SpotMenuStatus = spotMenuStatus;
            ConfirmTeleportStatus = confirmTeleportStatus;
            SpotMatch = spotMatch;
            Preview = preview;
            Spots = spots;
            StateLabel = stateLabel;
            Progress = progress;
            Detected = detected;
            CaptureStatus = captureStatus;
            TimeLabel = timeLabel;
            LifeMeter = lifeMeter;
            LifePercent = lifePercent;
            LifeLoss = lifeLoss;
            RemainingTimeLabel = remainingTimeLabel;

            if (profile.SpotWindowReferencePng.Length > 0)
            {
                try
                {
                    using var stream = new MemoryStream(profile.SpotWindowReferencePng);
                    using var bitmap = new Bitmap(stream);
                    SpotWindowReference = new Bitmap(bitmap);
                }
                catch
                {
                    SpotWindowReference = null;
                }
            }
        }

        public void SetStableCandidate(Bitmap bitmap)
        {
            StableCandidate?.Dispose();
            StableCandidate = new Bitmap(bitmap);
        }

        public void SetSpotWindowReference(Bitmap bitmap)
        {
            SpotWindowReference?.Dispose();
            SpotWindowReference = new Bitmap(bitmap);
        }

        public void ResetStableCandidate()
        {
            StableCandidate?.Dispose();
            StableCandidate = null;
        }

        public void ClearRuntime()
        {
            State = ProfileState.Stopped;
            ResumeAfterSearch = ProfileState.Monitoring;
            SessionWatch.Reset();
            LossConfirmations = 0;
            DisposeImages();
        }

        public void DisposeCapture()
        {
            Capture?.Dispose();
            Capture = null;
            CaptureStatus.Text = "Captura: parada";
        }

        public void DisposeImages()
        {
            ResetStableCandidate();
        }

        public void Dispose()
        {
            SessionWatch.Stop();
            DisposeCapture();
            DisposeImages();
            SpotWindowReference?.Dispose();
            SpotWindowReference = null;
            Preview.Image?.Dispose();
            Preview.Image = null;
        }
    }
}
