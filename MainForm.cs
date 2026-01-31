using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MultiAudioOutput;

public partial class MainForm : Form
{
    // Premium Design Tokens
    private static readonly Color Bg = Color.FromArgb(11, 15, 20);               // --bg: window background
    private static readonly Color Surface1 = Color.FromArgb(17, 24, 35);         // --surface-1: cards/rows
    private static readonly Color Surface2 = Color.FromArgb(14, 20, 29);         // --surface-2: inputs/dropdowns
    private static readonly Color Text1 = Color.FromArgb(234, 234, 234);         // --text-1: rgba(255,255,255,0.92)
    private static readonly Color Text2 = Color.FromArgb(179, 179, 179);         // --text-2: rgba(255,255,255,0.70)
    private static readonly Color Text3 = Color.FromArgb(140, 140, 140);         // --text-3: rgba(255,255,255,0.55)
    private static readonly Color BorderSoft = Color.FromArgb(15, 255, 255, 255);// --border-soft: rgba(255,255,255,0.06)
    private static readonly Color Accent = Color.FromArgb(43, 217, 127);         // --accent: #2BD97F
    private static readonly Color AccentWeak = Color.FromArgb(46, 43, 217, 127); // --accent-weak: rgba(43,217,127,0.18)
    private static readonly Color AccentHover = Color.FromArgb(35, 200, 110);    // Slightly darker
    private static readonly Color Danger = Color.FromArgb(255, 77, 77);          // --danger: #FF4D4D
    private static readonly Color DangerWeak = Color.FromArgb(46, 255, 77, 77);  // --danger-weak: rgba(255,77,77,0.18)
    private static readonly Color DangerHover = Color.FromArgb(230, 60, 60);     // Slightly darker

    private const int RadiusMd = 12;  // --radius-md
    private const int RadiusSm = 10;  // --radius-sm
    private const int PadRowY = 12;   // --pad-row-y
    private const int PadRowX = 14;   // --pad-row-x
    private const int GapRow = 10;    // --gap-row

    private NotifyIcon trayIcon = null!;
    private ContextMenuStrip trayMenu = null!;
    private FlowLayoutPanel deviceCardsPanel = null!;
    private Button startButton = null!;
    private Button stopButton = null!;
    private Button refreshButton = null!;
    private Button testButton = null!;
    private Label statusLabel = null!;
    private CustomDropdown sourceCombo = null!;
    private Panel titleBar = null!;
    private ToolTip mainTooltip = null!;

    private readonly List<DeviceCard> deviceCards = new();
    private WasapiLoopbackCapture? loopbackCapture;
    private readonly List<WasapiOut> outputDevices = new();
    private readonly List<ChannelMixingProvider> mixers = new();
    private bool isRunning = false;

    private AppSettings settings = null!;
    private bool startMinimized;

    private System.Windows.Forms.Timer? pulseTimer;
    private float pulseValue = 0f;

    private Point lastMousePosition;
    private bool isDragging = false;

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, int attrSize);

    public MainForm(bool startMinimized = false)
    {
        this.startMinimized = startMinimized;
        settings = AppSettings.Load();
        Localization.SetLanguage(settings.Language);

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
        InitializeComponent();
        LoadDevices();
        ApplySettings();

        if (settings.AutoStart && startMinimized)
        {
            Load += (s, e) => StartAudio();
        }

        // Pulse animation timer
        pulseTimer = new System.Windows.Forms.Timer { Interval = 50 };
        pulseTimer.Tick += (s, e) =>
        {
            pulseValue = (pulseValue + 0.1f) % (float)(Math.PI * 2);
            if (isRunning) Invalidate(true);
        };
        pulseTimer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int value = 2;
            DwmSetWindowAttribute(Handle, 33, ref value, sizeof(int));
        }
        catch { }
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "Multi Audio Output";
        Size = new Size(900, 680);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Bg;
        ForeColor = Text1;
        Font = new Font("Segoe UI", 9f);
        Padding = new Padding(0);

        // Initialize premium tooltip (matches app theme)
        mainTooltip = new ToolTip
        {
            BackColor = Surface2,
            ForeColor = Text1,
            OwnerDraw = true,
            IsBalloon = false
        };
        mainTooltip.Draw += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw rounded background
            using (var bgBrush = new SolidBrush(Surface2))
            {
                var path = GetRoundedRect(e.Bounds, 8);
                e.Graphics.FillPath(bgBrush, path);
            }

            // Draw subtle border
            using (var borderPen = new Pen(BorderSoft, 1))
            {
                var path = GetRoundedRect(e.Bounds, 8);
                e.Graphics.DrawPath(borderPen, path);
            }

            // Draw text with proper padding
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, new Font("Segoe UI", 9),
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 6, e.Bounds.Width - 16, e.Bounds.Height - 12),
                Text1, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        };

        // Custom Title Bar
        titleBar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(900, 40),
            BackColor = Bg,
            Cursor = Cursors.Hand
        };

        // Drag functionality
        titleBar.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePosition = e.Location;
            }
        };
        titleBar.MouseMove += (s, e) =>
        {
            if (isDragging)
            {
                Location = new Point(Location.X + e.X - lastMousePosition.X, Location.Y + e.Y - lastMousePosition.Y);
            }
        };
        titleBar.MouseUp += (s, e) => isDragging = false;

        var titleBarTitle = new Label
        {
            Text = "Multi Audio Output",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Text1,
            Location = new Point(15, 10),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        titleBar.Controls.Add(titleBarTitle);

        // Close button
        var closeBtn = new Button
        {
            Text = "✕",
            Location = new Point(860, 0),
            Size = new Size(40, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Text2,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.FlatAppearance.MouseOverBackColor = Danger;
        closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = Color.White;
        closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = Text2;
        closeBtn.Click += (s, e) =>
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        };
        titleBar.Controls.Add(closeBtn);

        // Minimize button
        var minBtn = new Button
        {
            Text = "−",
            Location = new Point(820, 0),
            Size = new Size(40, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Text2,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        minBtn.FlatAppearance.BorderSize = 0;
        minBtn.FlatAppearance.MouseOverBackColor = Surface1;
        minBtn.Click += (s, e) =>
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        };
        titleBar.Controls.Add(minBtn);

        Controls.Add(titleBar);

        // Main container
        var container = new Panel
        {
            Location = new Point(30, 60),
            Size = new Size(840, 530),
            BackColor = Color.Transparent
        };

        // Source Section (micro-label)
        var sourceLabel = new Label
        {
            Text = "SOURCE DEVICE",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Text3,
            Location = new Point(0, 0),
            Size = new Size(400, 20),
            BackColor = Color.Transparent
        };
        container.Controls.Add(sourceLabel);

        sourceCombo = new CustomDropdown
        {
            Location = new Point(0, 30),
            Size = new Size(840, 40),
            BackColor = Surface2,
            ForeColor = Text1,
            Font = new Font("Segoe UI", 10f)
        };

        sourceCombo.SelectedIndexChanged += (s, e) =>
        {
            SourceCombo_SelectedIndexChanged(s, e);
        };

        container.Controls.Add(sourceCombo);

        // Outputs Section (micro-label)
        var outputsLabel = new Label
        {
            Text = "OUTPUT DEVICES",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Text3,
            Location = new Point(0, 90),
            Size = new Size(400, 20),
            BackColor = Color.Transparent
        };
        container.Controls.Add(outputsLabel);

        deviceCardsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 120),
            Size = new Size(840, 400),
            BackColor = Color.Transparent,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 0, 10, 0)
        };
        container.Controls.Add(deviceCardsPanel);

        Controls.Add(container);

        // Bottom Bar
        var bottomBar = new Panel
        {
            Location = new Point(0, 600),
            Size = new Size(900, 80),
            BackColor = Bg
        };

        // Separator line (subtle)
        bottomBar.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderSoft, 1);
            e.Graphics.DrawLine(pen, 0, 0, 900, 0);
        };

        statusLabel = new Label
        {
            Text = "Ready",
            Font = new Font("Segoe UI", 9),
            ForeColor = Text2,
            Location = new Point(30, 30),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        bottomBar.Controls.Add(statusLabel);

        // Buttons container (right aligned)
        var btnContainer = new Panel
        {
            Location = new Point(550, 20),
            Size = new Size(320, 45),
            BackColor = Color.Transparent
        };

        refreshButton = CreateIconButton("⟳", 45, 45);
        refreshButton.Location = new Point(0, 0);
        refreshButton.Click += (s, e) => { LoadDevices(); };
        btnContainer.Controls.Add(refreshButton);

        var settingsBtn = CreateIconButton("⋮", 45, 45);
        settingsBtn.Location = new Point(55, 0);
        settingsBtn.Click += (s, e) => ShowSettingsDialog();
        settingsBtn.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        btnContainer.Controls.Add(settingsBtn);

        testButton = CreateIconButton("🔊", 45, 45);
        testButton.Location = new Point(110, 0);
        testButton.Click += (s, e) => PlayTestSound();
        mainTooltip.SetToolTip(testButton, "Test Sound");
        btnContainer.Controls.Add(testButton);

        startButton = CreatePrimaryButton("▶ START", 200, 45);
        startButton.Location = new Point(165, 0);
        startButton.Click += StartButton_Click;
        btnContainer.Controls.Add(startButton);

        stopButton = CreateDangerButton("Stop", 200, 45);
        stopButton.Location = new Point(165, 0);
        stopButton.Visible = false;
        stopButton.Click += StopButton_Click;
        btnContainer.Controls.Add(stopButton);

        bottomBar.Controls.Add(btnContainer);

        Controls.Add(bottomBar);

        // System Tray
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add(Localization.Get("ShowWindow"), null, (s, e) => { Show(); WindowState = FormWindowState.Normal; });
        trayMenu.Items.Add(Localization.Get("StartAudio"), null, (s, e) => StartAudio());
        trayMenu.Items.Add(Localization.Get("StopAudio"), null, (s, e) => StopAudio());
        trayMenu.Items.Add("-");
        trayMenu.Items.Add(Localization.Get("Settings"), null, (s, e) => ShowSettingsDialog());
        trayMenu.Items.Add(Localization.Get("Exit"), null, (s, e) => Application.Exit());

        trayIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon(),
            ContextMenuStrip = trayMenu,
            Visible = true,
            Text = "Multi Audio Output"
        };
        trayIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };

        // Minimize to tray instead of closing when X is clicked
        FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                Hide();
            }
        };

        ResumeLayout(false);

        if (startMinimized)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Subtle border
        using var pen = new Pen(BorderSoft, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    private Button CreateIconButton(string icon, int width, int height)
    {
        var btn = new Button
        {
            Text = icon,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Text2,
            Font = new Font("Segoe UI", 14),
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Surface1;
        btn.MouseEnter += (s, e) => btn.ForeColor = Text1;
        btn.MouseLeave += (s, e) => btn.ForeColor = Text2;
        btn.MouseDown += (s, e) => btn.ForeColor = Accent;
        btn.MouseUp += (s, e) => btn.ForeColor = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)) ? Text1 : Text2;

        return btn;
    }

    private Button CreatePrimaryButton(string text, int width, int height)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Bg,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = AccentHover;
        btn.MouseDown += (s, e) => btn.BackColor = Color.FromArgb(30, 180, 100);
        btn.MouseUp += (s, e) => btn.BackColor = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)) ? AccentHover : Accent;

        // Add rounded corners via region
        btn.Paint += (s, e) =>
        {
            var path = GetRoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), RadiusMd);
            btn.Region = new Region(path);
        };

        return btn;
    }

    private Button CreateDangerButton(string text, int width, int height)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Danger,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = DangerHover;
        btn.MouseDown += (s, e) => btn.BackColor = Color.FromArgb(200, 50, 50);
        btn.MouseUp += (s, e) => btn.BackColor = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)) ? DangerHover : Danger;

        // Add rounded corners via region
        btn.Paint += (s, e) =>
        {
            var path = GetRoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), RadiusMd);
            btn.Region = new Region(path);
        };

        return btn;
    }

    private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, radius, radius, 180, 90);
        path.AddArc(bounds.Right - radius, bounds.Top, radius, radius, 270, 90);
        path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void LoadDevices()
    {
        deviceCardsPanel.Controls.Clear();
        deviceCards.Clear();
        sourceCombo.Items.Clear();

        using var enumerator = new MMDeviceEnumerator();

        // Add only render (output) devices to source dropdown - loopback capture only works with output devices
        var sourceDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
        foreach (var device in sourceDevices)
        {
            sourceCombo.Items.Add(new DeviceItem(device));
        }

        // Only render (output) devices for output cards
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

        foreach (var device in devices)
        {

            var card = new DeviceCard(device);

            // Load saved settings
            var savedDevice = settings.Devices.FirstOrDefault(d => d.DeviceId == device.ID);
            if (savedDevice != null)
            {
                if (!string.IsNullOrEmpty(savedDevice.CustomName))
                    card.CustomName = savedDevice.CustomName;
                card.ChannelMode = savedDevice.ChannelMode;
                card.IsEnabled = savedDevice.IsSelected;
            }

            card.EnabledChanged += (s, e) => SaveDeviceSettings();
            card.ChannelChanged += (s, e) => SaveDeviceSettings();
            card.RenameRequested += (s, e) => RenameDevice(card);

            deviceCards.Add(card);
            deviceCardsPanel.Controls.Add(card);
        }

        // Select saved source
        if (!string.IsNullOrEmpty(settings.SourceDeviceId))
        {
            for (int i = 0; i < sourceCombo.Items.Count; i++)
            {
                if (sourceCombo.Items[i] is DeviceItem item && item.Device.ID == settings.SourceDeviceId)
                {
                    sourceCombo.SelectedIndex = i;
                    break;
                }
            }
        }

        if (sourceCombo.SelectedIndex == -1 && sourceCombo.Items.Count > 0)
            sourceCombo.SelectedIndex = 0;

        statusLabel.Text = $"{devices.Count} devices found";
    }

    private void SaveDeviceSettings()
    {
        settings.Devices.Clear();
        foreach (var card in deviceCards)
        {
            settings.Devices.Add(new DeviceSettings
            {
                DeviceId = card.DeviceId,
                CustomName = card.CustomName,
                ChannelMode = card.ChannelMode,
                IsSelected = card.IsEnabled
            });
        }
        settings.Save();
    }

    private void ApplySettings()
    {
        // Applied when loading devices
    }

    private void RenameDevice(DeviceCard card)
    {
        var dialog = new InputDialog("Rename Device", "Enter new name:", card.CustomName);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            card.CustomName = dialog.InputText;
            SaveDeviceSettings();
        }
    }

    private void SourceCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (sourceCombo.SelectedItem is DeviceItem item)
        {
            settings.SourceDeviceId = item.Device.ID;
            settings.Save();
        }
    }

    private void StartButton_Click(object? sender, EventArgs e)
    {
        StartAudio();
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        StopAudio();
    }

    private void StartAudio()
    {
        if (isRunning) return;

        var selectedCards = deviceCards.Where(c => c.IsEnabled).ToList();
        if (selectedCards.Count == 0)
        {
            MessageBox.Show(Localization.Get("NoDevicesSelected"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (sourceCombo.SelectedItem is not DeviceItem sourceItem)
        {
            MessageBox.Show(Localization.Get("NoSourceSelected"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var sourceDevice = sourceItem.Device;
            loopbackCapture = new WasapiLoopbackCapture(sourceDevice);

            using var enumerator = new MMDeviceEnumerator();
            var outputDevicesById = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .ToDictionary(device => device.ID, device => device);

            foreach (var card in selectedCards)
            {
                // Skip if output is same as source
                if (card.DeviceId == sourceDevice.ID)
                    continue;

                outputDevicesById.TryGetValue(card.DeviceId, out var outputDevice);

                if (outputDevice == null) continue;

                var output = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 15);
                var mixer = new ChannelMixingProvider(loopbackCapture.WaveFormat, card.ChannelMode);

                outputDevices.Add(output);
                mixers.Add(mixer);
            }

            if (outputDevices.Count == 0)
            {
                loopbackCapture?.Dispose();
                loopbackCapture = null;
                MessageBox.Show(Localization.Get("NoValidOutputs"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            loopbackCapture.DataAvailable += (s, e) =>
            {
                foreach (var mixer in mixers)
                {
                    mixer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                }
            };

            for (int i = 0; i < outputDevices.Count; i++)
            {
                outputDevices[i].Init(mixers[i]);
                outputDevices[i].Play();
            }

            loopbackCapture.StartRecording();

            isRunning = true;
            startButton.Visible = false;
            stopButton.Visible = true;
            statusLabel.Text = $"Playing on {outputDevices.Count} device(s)";
            statusLabel.ForeColor = Accent;

            trayIcon.Text = "Multi Audio Output - Running";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting audio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            StopAudio();
        }
    }

    private void StopAudio()
    {
        try
        {
            loopbackCapture?.StopRecording();
            loopbackCapture?.Dispose();
            loopbackCapture = null;

            foreach (var output in outputDevices)
            {
                output.Stop();
                output.Dispose();
            }
            outputDevices.Clear();
            mixers.Clear();

            isRunning = false;
            stopButton.Visible = false;
            startButton.Visible = true;
            statusLabel.Text = "Ready";
            statusLabel.ForeColor = Text2;

            trayIcon.Text = "Multi Audio Output";
        }
        catch { }
    }

    private void PlayTestSound()
    {
        var selectedCards = deviceCards.Where(c => c.IsEnabled).ToList();
        if (selectedCards.Count == 0)
        {
            MessageBox.Show("Select at least one output device to test.", "Test Sound", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        statusLabel.Text = "Playing test sound...";
        statusLabel.ForeColor = Accent;

        Task.Run(() =>
        {
            var testOutputs = new List<WasapiOut>();
            var testStreams = new List<Stream>();
            try
            {
                using var enumerator = new MMDeviceEnumerator();

                // Generate a short test tone (440Hz sine wave for 500ms)
                var sampleRate = 44100;
                var frequency = 440.0;
                var duration = 0.5;
                var samples = (int)(sampleRate * duration);
                var buffer = new byte[samples * 4]; // 16-bit stereo

                for (int i = 0; i < samples; i++)
                {
                    var sample = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * 16000);
                    // Fade in/out to avoid clicks
                    var envelope = 1.0;
                    if (i < sampleRate * 0.02) envelope = i / (sampleRate * 0.02);
                    if (i > samples - sampleRate * 0.02) envelope = (samples - i) / (sampleRate * 0.02);
                    sample = (short)(sample * envelope);

                    var bytes = BitConverter.GetBytes(sample);
                    buffer[i * 4] = bytes[0];     // Left
                    buffer[i * 4 + 1] = bytes[1];
                    buffer[i * 4 + 2] = bytes[0]; // Right
                    buffer[i * 4 + 3] = bytes[1];
                }

                var waveFormat = new WaveFormat(sampleRate, 16, 2);

                foreach (var card in selectedCards)
                {
                    var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                        .FirstOrDefault(d => d.ID == card.DeviceId);

                    if (device == null) continue;

                    var output = new WasapiOut(device, AudioClientShareMode.Shared, true, 50);
                    var stream = new MemoryStream(buffer, writable: false);
                    var provider = new RawSourceWaveStream(stream, waveFormat);
                    output.Init(provider);
                    testStreams.Add(provider);
                    testStreams.Add(stream);
                    testOutputs.Add(output);
                }

                // Play on all devices simultaneously
                foreach (var output in testOutputs)
                    output.Play();

                // Wait for playback to finish
                Thread.Sleep(600);

                foreach (var output in testOutputs)
                {
                    output.Stop();
                    output.Dispose();
                }

                foreach (var stream in testStreams)
                    stream.Dispose();
            }
            catch (Exception ex)
            {
                this.Invoke(() => MessageBox.Show($"Test failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                this.Invoke(() =>
                {
                    statusLabel.Text = isRunning ? $"Playing on {outputDevices.Count} device(s)" : "Ready";
                    statusLabel.ForeColor = isRunning ? Accent : Text2;
                });
            }
        });
    }

    private void ShowSettingsDialog()
    {
        var dialog = new SettingsDialog(settings);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            settings.Language = dialog.Settings.Language;
            settings.StartWithWindows = dialog.Settings.StartWithWindows;
            settings.StartMinimized = dialog.Settings.StartMinimized;
            settings.AutoStart = dialog.Settings.AutoStart;

            settings.SetStartWithWindows(settings.StartWithWindows);
            settings.Save();

            Localization.SetLanguage(settings.Language);
            MessageBox.Show(Localization.Get("RestartRequired"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private Icon LoadTrayIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch { }

        // Fallback to embedded resource or default
        return SystemIcons.Application;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pulseTimer?.Stop();
            pulseTimer?.Dispose();
            StopAudio();
            trayIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Device Card - Premium Row Style
class DeviceCard : Panel
{
    // Premium Design Tokens (match MainForm)
    private static readonly Color Bg = Color.FromArgb(11, 15, 20);
    private static readonly Color Surface1 = Color.FromArgb(17, 24, 35);
    private static readonly Color Surface2 = Color.FromArgb(14, 20, 29);
    private static readonly Color Text1 = Color.FromArgb(234, 234, 234);
    private static readonly Color Text2 = Color.FromArgb(179, 179, 179);
    private static readonly Color Text3 = Color.FromArgb(140, 140, 140);
    private static readonly Color BorderSoft = Color.FromArgb(15, 255, 255, 255);
    private static readonly Color Accent = Color.FromArgb(43, 217, 127);
    private static readonly Color AccentWeak = Color.FromArgb(46, 43, 217, 127);
    private const int RadiusMd = 12;
    private const int GapRow = 10;

    private readonly CheckBox enableCheckbox;
    private readonly Label nameLabel;
    private readonly CustomDropdown channelCombo;
    private readonly Button renameButton;
    private readonly ToolTip deviceTooltip;
    private int channelMode;
    private string customName;
    private bool isHovering = false;

    public string DeviceId { get; }
    public string CustomName
    {
        get => customName;
        set
        {
            customName = value;
            if (nameLabel != null)
                nameLabel.Text = value;
        }
    }
    public int ChannelMode
    {
        get => channelMode;
        set
        {
            channelMode = value;
            if (channelCombo != null && channelCombo.SelectedIndex != value)
                channelCombo.SelectedIndex = value;
        }
    }
    public bool IsEnabled
    {
        get => enableCheckbox.Checked;
        set => enableCheckbox.Checked = value;
    }

    public new event EventHandler? EnabledChanged;
    public event EventHandler? ChannelChanged;
    public event EventHandler? RenameRequested;

    public DeviceCard(MMDevice device)
    {
        DeviceId = device.ID;
        customName = device.FriendlyName;

        // Enable double buffering for smooth hover effects
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        Size = new Size(820, 70);
        BackColor = Surface1;
        Margin = new Padding(0, 0, 0, GapRow);
        Padding = new Padding(0);

        // Hover state tracking (on panel AND all child controls)
        MouseEnter += (s, e) =>
        {
            if (!isHovering)
            {
                isHovering = true;
                Invalidate();
            }
        };
        MouseLeave += (s, e) =>
        {
            // Only leave if mouse is truly outside the panel bounds
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                if (isHovering)
                {
                    isHovering = false;
                    Invalidate();
                }
            }
        };

        // Helper to propagate hover from child controls
        void PropagateHover(Control control)
        {
            control.MouseEnter += (s, e) =>
            {
                if (!isHovering)
                {
                    isHovering = true;
                    Invalidate();
                }
            };
            control.MouseLeave += (s, e) =>
            {
                // Only clear hover if mouse left the entire panel
                if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
                {
                    if (isHovering)
                    {
                        isHovering = false;
                        Invalidate();
                    }
                }
            };
        }

        // Initialize premium ToolTip for full device names
        deviceTooltip = new ToolTip
        {
            BackColor = Surface2,
            ForeColor = Text1,
            OwnerDraw = true,
            IsBalloon = false
        };
        deviceTooltip.Draw += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Rounded background
            using (var bgBrush = new SolidBrush(Surface2))
            {
                var path = GetRoundedRect(e.Bounds, 8);
                e.Graphics.FillPath(bgBrush, path);
            }

            // Subtle border
            using (var borderPen = new Pen(BorderSoft, 1))
            {
                var path = GetRoundedRect(e.Bounds, 8);
                e.Graphics.DrawPath(borderPen, path);
            }

            // Text with padding
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, new Font("Segoe UI", 9),
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 6, e.Bounds.Width - 16, e.Bounds.Height - 12),
                Text1, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        };

        // Enable checkbox (FIXED: proper contrast)
        enableCheckbox = new CheckBox
        {
            Location = new Point(20, 25),
            Size = new Size(22, 22),
            BackColor = Color.Transparent,
            ForeColor = Bg,  // DARK checkmark on green background
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        enableCheckbox.FlatAppearance.BorderColor = BorderSoft;
        enableCheckbox.FlatAppearance.BorderSize = 1;
        enableCheckbox.FlatAppearance.CheckedBackColor = Accent;  // Green when checked
        enableCheckbox.FlatAppearance.MouseOverBackColor = AccentWeak;
        enableCheckbox.CheckedChanged += (s, e) =>
        {
            // Dark checkmark when checked, invisible when unchecked
            enableCheckbox.ForeColor = enableCheckbox.Checked ? Bg : BorderSoft;
            EnabledChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        };
        Controls.Add(enableCheckbox);
        PropagateHover(enableCheckbox);

        // Device icon
        var icon = new Label
        {
            Text = GetDeviceIcon(device),
            Font = new Font("Segoe UI Emoji", 16),
            ForeColor = Text2,
            Location = new Point(55, 20),
            Size = new Size(30, 30),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(icon);
        PropagateHover(icon);

        // Device name (STRETCH to fill available space)
        nameLabel = new Label
        {
            Text = customName,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Text1,
            Location = new Point(95, 10),
            Size = new Size(540, 48),  // 648 - 95 - 13 spacing
            BackColor = Color.Transparent,
            AutoSize = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,  // Let it use available space
            UseMnemonic = false
        };

        // No tooltip on device name (removed for cleaner UX)
        Controls.Add(nameLabel);
        PropagateHover(nameLabel);

        // Channel dropdown (anchored to right, premium styling)
        channelCombo = new CustomDropdown
        {
            Location = new Point(648, 20),
            Size = new Size(120, 30),
            BackColor = Surface2,
            ForeColor = Text2,
            Font = new Font("Segoe UI", 9),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        channelCombo.Items.AddRange(new object[]
        {
            "Stereo",
            "Left",
            "Right",
            "Center",
            "Front Left",
            "Front Right",
            "Back Left",
            "Back Right",
            "Back/Surround",
            "Subwoofer (LFE)"
        });

        channelCombo.SelectedIndex = 0;
        channelCombo.SelectedIndexChanged += (s, e) =>
        {
            ChannelMode = channelCombo.SelectedIndex;
            ChannelChanged?.Invoke(this, EventArgs.Empty);
        };
        Controls.Add(channelCombo);
        PropagateHover(channelCombo);

        // Rename button (subtle secondary action)
        renameButton = new Button
        {
            Text = "✏",
            Location = new Point(778, 20),  // 820 - 32 - 10
            Size = new Size(32, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Text2,  // Default: text2 (70% opacity)
            Font = new Font("Segoe UI", 11),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        renameButton.FlatAppearance.BorderSize = 0;
        renameButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
        renameButton.Click += (s, e) => RenameRequested?.Invoke(this, EventArgs.Empty);

        // Subtle interaction states
        renameButton.MouseEnter += (s, e) => { renameButton.ForeColor = Text1; isHovering = true; Invalidate(); };
        renameButton.MouseLeave += (s, e) => { renameButton.ForeColor = Text2; isHovering = false; Invalidate(); };
        renameButton.MouseDown += (s, e) => renameButton.ForeColor = Accent;
        renameButton.MouseUp += (s, e) => renameButton.ForeColor = renameButton.ClientRectangle.Contains(renameButton.PointToClient(Cursor.Position)) ? Text1 : Text2;

        Controls.Add(renameButton);
        PropagateHover(renameButton);

        channelCombo.SelectedIndex = ChannelMode;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(0, 0, Width, Height);
        var path = GetRoundedRect(bounds, RadiusMd);

        // Draw background with rounded corners
        using (var bgBrush = new SolidBrush(BackColor))
        {
            e.Graphics.FillPath(bgBrush, path);
        }

        // Hover state - very subtle overlay (3% lighter)
        if (isHovering)
        {
            // Subtle white overlay at 3% opacity
            using var overlay = new SolidBrush(Color.FromArgb(8, 255, 255, 255));
            e.Graphics.FillPath(overlay, path);
        }

        // Left accent bar (2px, 60% opacity) when enabled
        if (IsEnabled)
        {
            // 60% opacity accent color
            var accentBar = Color.FromArgb(153, Accent.R, Accent.G, Accent.B);
            using var brush = new SolidBrush(accentBar);
            var barPath = new GraphicsPath();
            barPath.AddArc(0, 0, RadiusMd, RadiusMd, 180, 90);
            barPath.AddLine(0, RadiusMd / 2, 0, Height - RadiusMd / 2);
            barPath.AddArc(0, Height - RadiusMd, RadiusMd, RadiusMd, 90, 90);
            barPath.AddLine(2, Height, 2, 0);  // 2px instead of 3px
            barPath.CloseFigure();
            e.Graphics.FillPath(brush, barPath);
        }

        // Rounded corners
        Region = new Region(path);
    }

    private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, radius, radius, 180, 90);
        path.AddArc(bounds.Right - radius, bounds.Top, radius, radius, 270, 90);
        path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        return path;
    }

    private string GetDeviceIcon(MMDevice device)
    {
        var name = device.FriendlyName.ToLower();
        if (name.Contains("headphone")) return "🎧";
        if (name.Contains("speaker")) return "🔊";
        if (name.Contains("monitor") || name.Contains("display")) return "🖥️";
        return "🔈";
    }

    private string GetDeviceType(MMDevice device)
    {
        var name = device.FriendlyName.ToLower();
        if (name.Contains("headphone")) return "Headphones";
        if (name.Contains("speaker")) return "Speakers";
        if (name.Contains("monitor") || name.Contains("display")) return "Monitor Audio";
        if (name.Contains("hdmi") || name.Contains("displayport")) return "Display Audio";
        return "Audio Device";
    }
}

// Settings Dialog
class SettingsDialog : Form
{
    private static readonly Color BgPrimary = Color.FromArgb(15, 16, 32);
    private static readonly Color BgSecondary = Color.FromArgb(24, 26, 42);
    private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
    private static readonly Color TextSecondary = Color.FromArgb(160, 164, 192);

    public AppSettings Settings { get; }

    public SettingsDialog(AppSettings settings)
    {
        Settings = new AppSettings
        {
            Language = settings.Language,
            StartWithWindows = settings.StartWithWindows,
            StartMinimized = settings.StartMinimized,
            AutoStart = settings.AutoStart
        };

        Text = Localization.Get("SettingsTitle");
        Size = new Size(450, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = BgPrimary;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10);

        var langLabel = new Label { Text = Localization.Get("Language"), Location = new Point(24, 24), AutoSize = true };
        Controls.Add(langLabel);

        var langCombo = new ComboBox
        {
            Location = new Point(24, 48),
            Size = new Size(390, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = BgSecondary,
            ForeColor = TextPrimary
        };
        foreach (var lang in Localization.AvailableLanguages)
        {
            langCombo.Items.Add(new LanguageItem(lang.Key, lang.Value));
            if (lang.Key == Settings.Language) langCombo.SelectedIndex = langCombo.Items.Count - 1;
        }
        langCombo.SelectedIndexChanged += (s, e) =>
        {
            if (langCombo.SelectedItem is LanguageItem item) Settings.Language = item.Code;
        };
        Controls.Add(langCombo);

        var startWinCheck = new CheckBox
        {
            Text = Localization.Get("StartWithWindows"),
            Location = new Point(24, 100),
            AutoSize = true,
            Checked = Settings.StartWithWindows,
            ForeColor = TextPrimary
        };
        startWinCheck.CheckedChanged += (s, e) => Settings.StartWithWindows = startWinCheck.Checked;
        Controls.Add(startWinCheck);

        var startMinCheck = new CheckBox
        {
            Text = Localization.Get("StartMinimized"),
            Location = new Point(24, 140),
            AutoSize = true,
            Checked = Settings.StartMinimized,
            ForeColor = TextPrimary
        };
        startMinCheck.CheckedChanged += (s, e) => Settings.StartMinimized = startMinCheck.Checked;
        Controls.Add(startMinCheck);

        var autoStartCheck = new CheckBox
        {
            Text = Localization.Get("AutoStartAudio"),
            Location = new Point(24, 180),
            AutoSize = true,
            Checked = Settings.AutoStart,
            ForeColor = TextPrimary
        };
        autoStartCheck.CheckedChanged += (s, e) => Settings.AutoStart = autoStartCheck.Checked;
        Controls.Add(autoStartCheck);

        var okBtn = new Button
        {
            Text = "OK",
            Location = new Point(240, 310),
            Size = new Size(80, 35),
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(124, 108, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okBtn.FlatAppearance.BorderSize = 0;
        Controls.Add(okBtn);

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(330, 310),
            Size = new Size(80, 35),
            DialogResult = DialogResult.Cancel,
            BackColor = BgSecondary,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat
        };
        cancelBtn.FlatAppearance.BorderSize = 0;
        Controls.Add(cancelBtn);
    }
}

class InputDialog : Form
{
    private static readonly Color BgPrimary = Color.FromArgb(15, 16, 32);
    private static readonly Color BgSecondary = Color.FromArgb(24, 26, 42);
    private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);

    private readonly TextBox inputBox;
    public string InputText => inputBox.Text;

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Text = title;
        Size = new Size(400, 200);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = BgPrimary;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10);

        var label = new Label
        {
            Text = prompt,
            Location = new Point(20, 20),
            AutoSize = true
        };
        Controls.Add(label);

        inputBox = new TextBox
        {
            Location = new Point(20, 50),
            Size = new Size(340, 25),
            Text = defaultValue,
            BackColor = BgSecondary,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(inputBox);

        var okBtn = new Button
        {
            Text = "OK",
            Location = new Point(200, 110),
            Size = new Size(75, 30),
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(124, 108, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okBtn.FlatAppearance.BorderSize = 0;
        Controls.Add(okBtn);

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(285, 110),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel,
            BackColor = BgSecondary,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat
        };
        cancelBtn.FlatAppearance.BorderSize = 0;
        Controls.Add(cancelBtn);

        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }
}

// Custom high-performance dropdown with themed colors
class CustomDropdown : Panel
{
    private static readonly Color Surface2 = Color.FromArgb(14, 20, 29);
    private static readonly Color Text1 = Color.FromArgb(234, 234, 234);
    private static readonly Color Text2 = Color.FromArgb(179, 179, 179);
    private static readonly Color AccentWeak = Color.FromArgb(46, 43, 217, 127);
    private static readonly Color BorderSoft = Color.FromArgb(15, 255, 255, 255);

    private readonly Label displayLabel;
    private readonly Panel dropdownButton;
    private readonly ToolStripDropDown popup;
    private int selectedIndex = -1;
    private bool isDropdownOpen = false;
    private DateTime lastCloseTime = DateTime.MinValue;

    public object? SelectedItem => selectedIndex >= 0 && selectedIndex < Items.Count ? Items[selectedIndex] : null;
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (selectedIndex != value && value >= -1 && value < Items.Count)
            {
                selectedIndex = value;
                UpdateDisplay();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ComboBox.ObjectCollection Items { get; }
    public event EventHandler? SelectedIndexChanged;

    public CustomDropdown()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

        BackColor = Surface2;
        Height = 40;
        Cursor = Cursors.Hand;

        // Display label
        displayLabel = new Label
        {
            Location = new Point(12, 0),
            AutoSize = false,
            Size = new Size(Width - 50, Height),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Text1,
            BackColor = Color.Transparent,
            Font = Font,
            Cursor = Cursors.Hand
        };
        displayLabel.Click += (s, e) => ToggleDropdown();
        Controls.Add(displayLabel);

        // Dropdown arrow button
        dropdownButton = new Panel
        {
            Location = new Point(Width - 40, 0),
            Size = new Size(40, Height),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        // Enable double buffering for smooth arrow rendering
        typeof(Panel).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, dropdownButton, new object[] { true });

        dropdownButton.Paint += (s, e) =>
        {
            // Draw arrow
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var points = new Point[]
            {
                new Point(15, 14),
                new Point(25, 14),
                new Point(20, 20)
            };
            using (var brush = new SolidBrush(Text2))
                e.Graphics.FillPolygon(brush, points);
        };
        dropdownButton.Click += (s, e) => ToggleDropdown();
        Controls.Add(dropdownButton);

        // Create popup with custom renderer
        popup = new ToolStripDropDown
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Surface2,
            Renderer = new ToolStripProfessionalRenderer(new CustomColorTable())
        };

        // Track when popup closes
        popup.Closed += (s, e) =>
        {
            isDropdownOpen = false;
            lastCloseTime = DateTime.Now;
        };

        // Initialize items collection
        var tempCombo = new ComboBox();
        Items = tempCombo.Items;

        SizeChanged += (s, e) =>
        {
            displayLabel.Width = Width - 50;
            dropdownButton.Location = new Point(Width - 40, 0);
        };
    }

    private void UpdateDisplay()
    {
        if (SelectedItem is DeviceItem item)
            displayLabel.Text = item.Device.FriendlyName;
        else if (SelectedItem != null)
            displayLabel.Text = SelectedItem.ToString();
        else
            displayLabel.Text = "";
    }

    private void ToggleDropdown()
    {
        // Prevent reopening immediately after closing (debounce)
        if ((DateTime.Now - lastCloseTime).TotalMilliseconds < 200)
            return;

        // Toggle behavior: if already open, close it
        if (isDropdownOpen)
        {
            popup.Close();
            isDropdownOpen = false;
            return;
        }

        popup.Items.Clear();

        foreach (var item in Items)
        {
            var menuItem = new ToolStripMenuItem
            {
                Text = item is DeviceItem deviceItem ? deviceItem.Device.FriendlyName : item.ToString(),
                BackColor = Surface2,
                ForeColor = Text1,
                AutoSize = false,
                Size = new Size(Width - 4, 30),
                Padding = new Padding(12, 5, 12, 5),
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var itemIndex = Items.IndexOf(item);
            menuItem.Click += (s, e) =>
            {
                SelectedIndex = itemIndex;
                popup.Close();
            };

            popup.Items.Add(menuItem);
        }

        // Calculate height based on number of items
        popup.Width = Width;
        popup.Height = popup.Items.Count * 30 + 4;
        popup.Show(this, new Point(0, Height));
        isDropdownOpen = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Draw border
        using (var pen = new Pen(BorderSoft, 1))
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    // Custom color table to prevent default cyan selection
    private class CustomColorTable : ProfessionalColorTable
    {
        private static readonly Color Surface2 = Color.FromArgb(14, 20, 29);
        private static readonly Color AccentWeak = Color.FromArgb(46, 43, 217, 127);

        public override Color MenuItemSelected => AccentWeak;
        public override Color MenuItemSelectedGradientBegin => AccentWeak;
        public override Color MenuItemSelectedGradientEnd => AccentWeak;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuBorder => Surface2;
        public override Color ImageMarginGradientBegin => Surface2;
        public override Color ImageMarginGradientMiddle => Surface2;
        public override Color ImageMarginGradientEnd => Surface2;
        public override Color MenuItemPressedGradientBegin => Surface2;
        public override Color MenuItemPressedGradientEnd => Surface2;
    }
}

class DeviceItem
{
    public MMDevice Device { get; }
    public DeviceItem(MMDevice device) => Device = device;
    public override string ToString() => Device.FriendlyName;
}

class LanguageItem
{
    public string Code { get; }
    public string Name { get; }
    public LanguageItem(string code, string name) { Code = code; Name = name; }
    public override string ToString() => Name;
}

// Channel mixing provider using NAudio's optimized BufferedWaveProvider
class ChannelMixingProvider : IWaveProvider
{
    private readonly BufferedWaveProvider bufferedProvider;
    private readonly int channelMode;

    public WaveFormat WaveFormat => bufferedProvider.WaveFormat;

    public ChannelMixingProvider(WaveFormat sourceFormat, int mode)
    {
        channelMode = mode;
        bufferedProvider = new BufferedWaveProvider(sourceFormat)
        {
            // Discard old audio when buffer is full - prevents latency buildup
            DiscardOnBufferOverflow = true,
            // Small buffer = low latency
            BufferLength = sourceFormat.AverageBytesPerSecond / 14  // ~70ms max
        };
    }

    public void AddSamples(byte[] samples, int offset, int count)
    {
        bufferedProvider.AddSamples(samples, offset, count);
    }

    public int Read(byte[] outBuffer, int offset, int count)
    {
        int read = bufferedProvider.Read(outBuffer, offset, count);

        // Fill any remaining with silence
        if (read < count)
        {
            Array.Clear(outBuffer, offset + read, count - read);
        }

        if (channelMode > 0 && bufferedProvider.WaveFormat.Channels == 2 && read > 0)
        {
            ApplyChannelMixing(outBuffer, offset, read);
        }

        return count;
    }

    private void ApplyChannelMixing(byte[] buffer, int offset, int count)
    {
        int bytesPerSample = bufferedProvider.WaveFormat.BitsPerSample / 8;
        int samplePairs = count / (bytesPerSample * 2);

        for (int i = 0; i < samplePairs; i++)
        {
            int leftIndex = offset + (i * bytesPerSample * 2);
            int rightIndex = leftIndex + bytesPerSample;

            switch (channelMode)
            {
                case 1: // Left
                case 4: // Front Left
                    Array.Copy(buffer, leftIndex, buffer, rightIndex, bytesPerSample);
                    break;

                case 2: // Right
                case 5: // Front Right
                    Array.Copy(buffer, rightIndex, buffer, leftIndex, bytesPerSample);
                    break;

                case 3: // Center
                case 8: // Back/Surround (mono mix)
                    if (bytesPerSample == 2)
                    {
                        short left = BitConverter.ToInt16(buffer, leftIndex);
                        short right = BitConverter.ToInt16(buffer, rightIndex);
                        short mono = (short)((left + right) / 2);
                        Array.Copy(BitConverter.GetBytes(mono), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(mono), 0, buffer, rightIndex, 2);
                    }
                    break;

                case 6: // Back Left (left channel, slightly reduced volume)
                    if (bytesPerSample == 2)
                    {
                        short left = BitConverter.ToInt16(buffer, leftIndex);
                        short reduced = (short)(left * 0.85); // 85% volume
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, rightIndex, 2);
                    }
                    break;

                case 7: // Back Right (right channel, slightly reduced volume)
                    if (bytesPerSample == 2)
                    {
                        short right = BitConverter.ToInt16(buffer, rightIndex);
                        short reduced = (short)(right * 0.85); // 85% volume
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, rightIndex, 2);
                    }
                    break;

                case 9: // Subwoofer (LFE - bass boost)
                    if (bytesPerSample == 2)
                    {
                        short left = BitConverter.ToInt16(buffer, leftIndex);
                        short right = BitConverter.ToInt16(buffer, rightIndex);
                        // Simple bass boost: mix to mono and amplify
                        short bass = (short)(((left + right) / 2) * 1.3);
                        Array.Copy(BitConverter.GetBytes(bass), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(bass), 0, buffer, rightIndex, 2);
                    }
                    break;
            }
        }
    }
}
