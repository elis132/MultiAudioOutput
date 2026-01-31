using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MultiAudioOutput;

/// <summary>
/// Main application window for Multi Audio Output.
/// Provides a modern dark-themed UI for routing system audio to multiple output devices simultaneously.
/// </summary>
public partial class MainForm : Form
{
    #region Design Tokens - Premium Dark Theme Color Palette
    // These colors define the visual appearance of the application
    // Following a consistent design system for a professional look

    private static readonly Color Bg = Color.FromArgb(11, 15, 20);               // Main window background
    private static readonly Color Surface1 = Color.FromArgb(17, 24, 35);         // Card/row backgrounds
    private static readonly Color Surface2 = Color.FromArgb(14, 20, 29);         // Input fields, dropdowns
    private static readonly Color Text1 = Color.FromArgb(234, 234, 234);         // Primary text (92% white)
    private static readonly Color Text2 = Color.FromArgb(179, 179, 179);         // Secondary text (70% white)
    private static readonly Color Text3 = Color.FromArgb(140, 140, 140);         // Tertiary text (55% white)
    private static readonly Color BorderSoft = Color.FromArgb(15, 255, 255, 255);// Subtle borders (6% white)
    private static readonly Color Accent = Color.FromArgb(43, 217, 127);         // Primary accent (green)
    private static readonly Color AccentWeak = Color.FromArgb(46, 43, 217, 127); // Accent with 18% opacity
    private static readonly Color AccentHover = Color.FromArgb(35, 200, 110);    // Accent hover state
    private static readonly Color Danger = Color.FromArgb(255, 77, 77);          // Danger/stop color (red)
    private static readonly Color DangerWeak = Color.FromArgb(46, 255, 77, 77);  // Danger with 18% opacity
    private static readonly Color DangerHover = Color.FromArgb(230, 60, 60);     // Danger hover state
    #endregion

    #region Design Tokens - Spacing and Sizing
    private const int RadiusMd = 12;  // Medium border radius for cards
    private const int RadiusSm = 10;  // Small border radius for buttons
    private const int PadRowY = 12;   // Vertical padding inside rows
    private const int PadRowX = 14;   // Horizontal padding inside rows
    private const int GapRow = 10;    // Gap between device cards
    #endregion

    #region UI Controls
    private NotifyIcon trayIcon = null!;           // System tray icon for background operation
    private ContextMenuStrip trayMenu = null!;     // Right-click menu for tray icon
    private FlowLayoutPanel deviceCardsPanel = null!; // Container for output device cards
    private Button startButton = null!;            // Start audio routing button
    private Button stopButton = null!;             // Stop audio routing button
    private Button refreshButton = null!;          // Refresh device list button
    private Button testButton = null!;             // Test sound playback button
    private Label statusLabel = null!;             // Status display at bottom of window
    private CustomDropdown sourceCombo = null!;    // Source device selection dropdown
    private Panel titleBar = null!;                // Custom title bar for borderless window
    private ToolTip mainTooltip = null!;           // Themed tooltip for UI elements
    #endregion

    #region Audio State
    private readonly List<DeviceCard> deviceCards = new();           // UI cards for each output device
    private WasapiLoopbackCapture? loopbackCapture;                  // Captures audio from source device
    private readonly List<WasapiOut> outputDevices = new();          // Active output device instances
    private readonly List<ChannelMixingProvider> mixers = new();     // Audio processors for each output
    private bool isRunning = false;                                   // Whether audio routing is active
    #endregion

    #region Settings and State
    private AppSettings settings = null!;  // Persisted application settings
    private bool startMinimized;           // Whether app started minimized (from command line)

    // Pulse animation for visual feedback when running
    private System.Windows.Forms.Timer? pulseTimer;
    private float pulseValue = 0f;

    // Window dragging state (for borderless window)
    private Point lastMousePosition;
    private bool isDragging = false;
    #endregion

    #region Windows API Imports
    /// <summary>
    /// Sets window attributes via Desktop Window Manager.
    /// Used here to enable dark mode title bar on Windows 10/11.
    /// </summary>
    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, int attrSize);
    #endregion

    #region Constructor and Initialization
    /// <summary>
    /// Initializes the main form with optional minimized start.
    /// </summary>
    /// <param name="startMinimized">If true, starts minimized to system tray</param>
    public MainForm(bool startMinimized = false)
    {
        this.startMinimized = startMinimized;

        // Load persisted settings (device selections, language, etc.)
        settings = AppSettings.Load();
        Localization.SetLanguage(settings.Language);

        // Enable double buffering for smooth rendering
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);

        InitializeComponent();
        LoadDevices();
        ApplySettings();

        // Auto-start audio if enabled and starting minimized
        if (settings.AutoStart && startMinimized)
        {
            Load += (s, e) => StartAudio();
        }

        // Setup pulse animation timer for visual feedback
        pulseTimer = new System.Windows.Forms.Timer { Interval = 50 };
        pulseTimer.Tick += (s, e) =>
        {
            pulseValue = (pulseValue + 0.1f) % (float)(Math.PI * 2);
            if (isRunning) Invalidate(true);
        };
        pulseTimer.Start();
    }

    /// <summary>
    /// Enables Windows dark mode for the title bar when the window handle is created.
    /// </summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 10) or 33 (Windows 11)
            int value = 2;
            DwmSetWindowAttribute(Handle, 33, ref value, sizeof(int));
        }
        catch { /* Ignore if not supported */ }
    }

    /// <summary>
    /// Creates and configures all UI components.
    /// This method builds the entire UI programmatically for full control over styling.
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        // === Window Configuration ===
        Text = "Multi Audio Output";
        Size = new Size(900, 680);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;  // Borderless for custom title bar
        BackColor = Bg;
        ForeColor = Text1;
        Font = new Font("Segoe UI", 9f);
        Padding = new Padding(0);

        // === Premium Themed Tooltip ===
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

        // === Custom Title Bar (for borderless window) ===
        titleBar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(900, 40),
            BackColor = Bg,
            Cursor = Cursors.Hand
        };

        // Enable window dragging via title bar
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

        // Title text
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

        // Close button (minimizes to tray)
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

        // === Main Content Container ===
        var container = new Panel
        {
            Location = new Point(30, 60),
            Size = new Size(840, 530),
            BackColor = Color.Transparent
        };

        // Source device section label
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

        // Source device dropdown
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

        // Output devices section label
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

        // Scrollable panel for device cards
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

        // === Bottom Bar (status and buttons) ===
        var bottomBar = new Panel
        {
            Location = new Point(0, 600),
            Size = new Size(900, 80),
            BackColor = Bg
        };

        // Separator line
        bottomBar.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderSoft, 1);
            e.Graphics.DrawLine(pen, 0, 0, 900, 0);
        };

        // Status label
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

        // Button container (right-aligned)
        var btnContainer = new Panel
        {
            Location = new Point(550, 20),
            Size = new Size(320, 45),
            BackColor = Color.Transparent
        };

        // Refresh devices button
        refreshButton = CreateIconButton("⟳", 45, 45);
        refreshButton.Location = new Point(0, 0);
        refreshButton.Click += (s, e) => { LoadDevices(); };
        btnContainer.Controls.Add(refreshButton);

        // Settings button
        var settingsBtn = CreateIconButton("⋮", 45, 45);
        settingsBtn.Location = new Point(55, 0);
        settingsBtn.Click += (s, e) => ShowSettingsDialog();
        settingsBtn.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        btnContainer.Controls.Add(settingsBtn);

        // Test sound button
        testButton = CreateIconButton("🔊", 45, 45);
        testButton.Location = new Point(110, 0);
        testButton.Click += (s, e) => PlayTestSound();
        mainTooltip.SetToolTip(testButton, "Test Sound");
        btnContainer.Controls.Add(testButton);

        // Start button (primary action)
        startButton = CreatePrimaryButton("▶ START", 200, 45);
        startButton.Location = new Point(165, 0);
        startButton.Click += StartButton_Click;
        btnContainer.Controls.Add(startButton);

        // Stop button (danger action, initially hidden)
        stopButton = CreateDangerButton("Stop", 200, 45);
        stopButton.Location = new Point(165, 0);
        stopButton.Visible = false;
        stopButton.Click += StopButton_Click;
        btnContainer.Controls.Add(stopButton);

        bottomBar.Controls.Add(btnContainer);
        Controls.Add(bottomBar);

        // === System Tray Configuration ===
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

        // Minimize to tray instead of closing
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

        // Handle minimized start
        if (startMinimized)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }
    }
    #endregion

    #region Custom Painting
    /// <summary>
    /// Draws the window border.
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Draw subtle border around borderless window
        using var pen = new Pen(BorderSoft, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
    #endregion

    #region Button Factory Methods
    /// <summary>
    /// Creates a minimal icon button (transparent background, icon only).
    /// </summary>
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

    /// <summary>
    /// Creates a primary action button (green background).
    /// </summary>
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

        // Apply rounded corners
        btn.Paint += (s, e) =>
        {
            var path = GetRoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), RadiusMd);
            btn.Region = new Region(path);
        };

        return btn;
    }

    /// <summary>
    /// Creates a danger action button (red background).
    /// </summary>
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

        // Apply rounded corners
        btn.Paint += (s, e) =>
        {
            var path = GetRoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), RadiusMd);
            btn.Region = new Region(path);
        };

        return btn;
    }

    /// <summary>
    /// Creates a GraphicsPath for a rounded rectangle.
    /// </summary>
    /// <param name="bounds">Rectangle bounds</param>
    /// <param name="radius">Corner radius</param>
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
    #endregion

    #region Device Management
    /// <summary>
    /// Enumerates all audio devices and populates the UI.
    /// Creates device cards for each output device and populates the source dropdown.
    /// </summary>
    private void LoadDevices()
    {
        deviceCardsPanel.Controls.Clear();
        deviceCards.Clear();
        sourceCombo.Items.Clear();

        using var enumerator = new MMDeviceEnumerator();

        // Source dropdown: Only render (output) devices work with loopback capture
        // Loopback capture records what's being played to an output device
        var sourceDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
        foreach (var device in sourceDevices)
        {
            sourceCombo.Items.Add(new DeviceItem(device));
        }

        // Output cards: All render devices can be targets
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

        foreach (var device in devices)
        {

            var card = new DeviceCard(device);

            // Restore saved settings for this device
            var savedDevice = settings.Devices.FirstOrDefault(d => d.DeviceId == device.ID);
            if (savedDevice != null)
            {
                if (!string.IsNullOrEmpty(savedDevice.CustomName))
                    card.CustomName = savedDevice.CustomName;
                card.ChannelMode = savedDevice.ChannelMode;
                card.IsEnabled = savedDevice.IsSelected;
            }

            // Wire up event handlers
            card.EnabledChanged += (s, e) => SaveDeviceSettings();
            card.ChannelChanged += (s, e) => SaveDeviceSettings();
            card.RenameRequested += (s, e) => RenameDevice(card);

            deviceCards.Add(card);
            deviceCardsPanel.Controls.Add(card);
        }

        // Restore selected source device
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

        // Default to first device if none selected
        if (sourceCombo.SelectedIndex == -1 && sourceCombo.Items.Count > 0)
            sourceCombo.SelectedIndex = 0;

        statusLabel.Text = $"{devices.Count} devices found";
    }

    /// <summary>
    /// Persists current device settings to disk.
    /// Called whenever device selection, channel mode, or name changes.
    /// </summary>
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

    /// <summary>
    /// Applies loaded settings to UI. Currently a no-op as settings are applied in LoadDevices.
    /// </summary>
    private void ApplySettings()
    {
        // Settings are applied when loading devices
    }

    /// <summary>
    /// Shows a dialog to rename a device with a custom friendly name.
    /// </summary>
    private void RenameDevice(DeviceCard card)
    {
        var dialog = new InputDialog("Rename Device", "Enter new name:", card.CustomName);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            card.CustomName = dialog.InputText;
            SaveDeviceSettings();
        }
    }

    /// <summary>
    /// Handles source device selection changes.
    /// </summary>
    private void SourceCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (sourceCombo.SelectedItem is DeviceItem item)
        {
            settings.SourceDeviceId = item.Device.ID;
            settings.Save();
        }
    }
    #endregion

    #region Audio Control
    /// <summary>
    /// Handles Start button click.
    /// </summary>
    private void StartButton_Click(object? sender, EventArgs e)
    {
        StartAudio();
    }

    /// <summary>
    /// Handles Stop button click.
    /// </summary>
    private void StopButton_Click(object? sender, EventArgs e)
    {
        StopAudio();
    }

    /// <summary>
    /// Starts audio routing from source device to all enabled output devices.
    ///
    /// Audio Pipeline:
    /// 1. WasapiLoopbackCapture captures audio from the source device (what's being played)
    /// 2. DataAvailable event fires with audio data chunks
    /// 3. Each chunk is fed to all ChannelMixingProviders (one per output)
    /// 4. Each WasapiOut reads from its ChannelMixingProvider and plays to its device
    /// </summary>
    private void StartAudio()
    {
        if (isRunning) return;

        // Validate device selection
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

            // Create loopback capture - captures what's being played to the source device
            loopbackCapture = new WasapiLoopbackCapture(sourceDevice);

            // Build dictionary of output devices for O(1) lookup
            using var enumerator = new MMDeviceEnumerator();
            var outputDevicesById = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .ToDictionary(device => device.ID, device => device);

            // Create output pipeline for each enabled device
            foreach (var card in selectedCards)
            {
                // Skip if output is same as source (would cause feedback)
                if (card.DeviceId == sourceDevice.ID)
                    continue;

                outputDevicesById.TryGetValue(card.DeviceId, out var outputDevice);

                if (outputDevice == null) continue;

                // WasapiOut: Plays audio to the device
                // Parameters: device, share mode, event callback mode, latency in ms
                var output = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 15);

                // ChannelMixingProvider: Buffers audio and applies channel mixing
                var mixer = new ChannelMixingProvider(loopbackCapture.WaveFormat, card.ChannelMode);

                outputDevices.Add(output);
                mixers.Add(mixer);
            }

            // Handle case where no valid outputs after filtering
            if (outputDevices.Count == 0)
            {
                loopbackCapture?.Dispose();
                loopbackCapture = null;
                MessageBox.Show(Localization.Get("NoValidOutputs"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Wire up audio data flow: source -> all mixers
            loopbackCapture.DataAvailable += (s, e) =>
            {
                foreach (var mixer in mixers)
                {
                    mixer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                }
            };

            // Initialize and start playback on all outputs
            for (int i = 0; i < outputDevices.Count; i++)
            {
                outputDevices[i].Init(mixers[i]);
                outputDevices[i].Play();
            }

            // Start capturing from source
            loopbackCapture.StartRecording();

            // Update UI state
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

    /// <summary>
    /// Stops all audio routing and releases resources.
    /// </summary>
    private void StopAudio()
    {
        try
        {
            // Stop and dispose loopback capture
            loopbackCapture?.StopRecording();
            loopbackCapture?.Dispose();
            loopbackCapture = null;

            // Stop and dispose all output devices
            foreach (var output in outputDevices)
            {
                output.Stop();
                output.Dispose();
            }
            outputDevices.Clear();
            mixers.Clear();

            // Update UI state
            isRunning = false;
            stopButton.Visible = false;
            startButton.Visible = true;
            statusLabel.Text = "Ready";
            statusLabel.ForeColor = Text2;

            trayIcon.Text = "Multi Audio Output";
        }
        catch { /* Ignore cleanup errors */ }
    }

    /// <summary>
    /// Plays a test tone on all enabled output devices simultaneously.
    /// Useful for verifying device configuration and checking latency.
    /// </summary>
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

        // Run on background thread to avoid UI freeze
        Task.Run(() =>
        {
            var testOutputs = new List<WasapiOut>();
            var testStreams = new List<Stream>();
            try
            {
                using var enumerator = new MMDeviceEnumerator();

                // Generate a 440Hz sine wave (A4 note) for 500ms
                var sampleRate = 44100;
                var frequency = 440.0;
                var duration = 0.5;
                var samples = (int)(sampleRate * duration);
                var buffer = new byte[samples * 4]; // 16-bit stereo = 4 bytes per sample

                for (int i = 0; i < samples; i++)
                {
                    // Generate sine wave sample
                    var sample = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * 16000);

                    // Apply fade in/out envelope to avoid clicks
                    var envelope = 1.0;
                    if (i < sampleRate * 0.02) envelope = i / (sampleRate * 0.02);  // 20ms fade in
                    if (i > samples - sampleRate * 0.02) envelope = (samples - i) / (sampleRate * 0.02);  // 20ms fade out
                    sample = (short)(sample * envelope);

                    // Write sample to buffer (16-bit stereo: L L R R)
                    var bytes = BitConverter.GetBytes(sample);
                    buffer[i * 4] = bytes[0];     // Left low byte
                    buffer[i * 4 + 1] = bytes[1]; // Left high byte
                    buffer[i * 4 + 2] = bytes[0]; // Right low byte
                    buffer[i * 4 + 3] = bytes[1]; // Right high byte
                }

                var waveFormat = new WaveFormat(sampleRate, 16, 2);

                // Create output for each selected device
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

                // Start playback on all devices simultaneously
                foreach (var output in testOutputs)
                    output.Play();

                // Wait for playback to complete
                Thread.Sleep(600);

                // Cleanup
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
                // Restore status label on UI thread
                this.Invoke(() =>
                {
                    statusLabel.Text = isRunning ? $"Playing on {outputDevices.Count} device(s)" : "Ready";
                    statusLabel.ForeColor = isRunning ? Accent : Text2;
                });
            }
        });
    }
    #endregion

    #region Settings Dialog
    /// <summary>
    /// Shows the settings dialog for configuring application preferences.
    /// </summary>
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
    #endregion

    #region Resource Loading
    /// <summary>
    /// Loads the tray icon from resources or falls back to system default.
    /// </summary>
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

        // Fallback to default system icon
        return SystemIcons.Application;
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Cleans up resources when the form is disposed.
    /// </summary>
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
    #endregion
}

#region DeviceCard - Output Device UI Component
/// <summary>
/// A premium-styled card control representing an audio output device.
/// Features enable checkbox, device icon, name, channel mode selector, and rename button.
/// </summary>
class DeviceCard : Panel
{
    // Design tokens (matching MainForm theme)
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

    // Controls
    private readonly CheckBox enableCheckbox;
    private readonly Label nameLabel;
    private readonly CustomDropdown channelCombo;
    private readonly Button renameButton;
    private readonly ToolTip deviceTooltip;

    // State
    private int channelMode;
    private string customName;
    private bool isHovering = false;

    /// <summary>Windows audio device ID</summary>
    public string DeviceId { get; }

    /// <summary>User-customizable display name</summary>
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

    /// <summary>Selected channel mixing mode (0-9)</summary>
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

    /// <summary>Whether this device is enabled for output</summary>
    public bool IsEnabled
    {
        get => enableCheckbox.Checked;
        set => enableCheckbox.Checked = value;
    }

    // Events
    public new event EventHandler? EnabledChanged;
    public event EventHandler? ChannelChanged;
    public event EventHandler? RenameRequested;

    /// <summary>
    /// Creates a new device card for the specified audio device.
    /// </summary>
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

        // Hover state tracking
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
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                if (isHovering)
                {
                    isHovering = false;
                    Invalidate();
                }
            }
        };

        // Helper to propagate hover state from child controls
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

        // Premium tooltip for showing full device names
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

            using (var bgBrush = new SolidBrush(Surface2))
            {
                var path = GetRoundedRect(e.Bounds, 8);
                e.Graphics.FillPath(bgBrush, path);
            }

            using (var borderPen = new Pen(BorderSoft, 1))
            {
                var path = GetRoundedRect(e.Bounds, 8);
                e.Graphics.DrawPath(borderPen, path);
            }

            TextRenderer.DrawText(e.Graphics, e.ToolTipText, new Font("Segoe UI", 9),
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 6, e.Bounds.Width - 16, e.Bounds.Height - 12),
                Text1, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        };

        // Enable/disable checkbox
        enableCheckbox = new CheckBox
        {
            Location = new Point(20, 25),
            Size = new Size(22, 22),
            BackColor = Color.Transparent,
            ForeColor = Bg,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        enableCheckbox.FlatAppearance.BorderColor = BorderSoft;
        enableCheckbox.FlatAppearance.BorderSize = 1;
        enableCheckbox.FlatAppearance.CheckedBackColor = Accent;
        enableCheckbox.FlatAppearance.MouseOverBackColor = AccentWeak;
        enableCheckbox.CheckedChanged += (s, e) =>
        {
            enableCheckbox.ForeColor = enableCheckbox.Checked ? Bg : BorderSoft;
            EnabledChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        };
        Controls.Add(enableCheckbox);
        PropagateHover(enableCheckbox);

        // Device type icon (emoji-based)
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

        // Device name label
        nameLabel = new Label
        {
            Text = customName,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Text1,
            Location = new Point(95, 10),
            Size = new Size(540, 48),
            BackColor = Color.Transparent,
            AutoSize = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            UseMnemonic = false
        };
        Controls.Add(nameLabel);
        PropagateHover(nameLabel);

        // Channel mode dropdown
        channelCombo = new CustomDropdown
        {
            Location = new Point(648, 20),
            Size = new Size(120, 30),
            BackColor = Surface2,
            ForeColor = Text2,
            Font = new Font("Segoe UI", 9),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        // Available channel modes
        channelCombo.Items.AddRange(new object[]
        {
            "Stereo",        // 0: Pass through both channels
            "Left",          // 1: Left channel to both outputs
            "Right",         // 2: Right channel to both outputs
            "Center",        // 3: Mono mix (L+R)/2
            "Front Left",    // 4: Same as Left
            "Front Right",   // 5: Same as Right
            "Back Left",     // 6: Left at 85% volume
            "Back Right",    // 7: Right at 85% volume
            "Back/Surround", // 8: Mono mix
            "Subwoofer (LFE)" // 9: Mono mix with 1.3x boost
        });

        channelCombo.SelectedIndex = 0;
        channelCombo.SelectedIndexChanged += (s, e) =>
        {
            ChannelMode = channelCombo.SelectedIndex;
            ChannelChanged?.Invoke(this, EventArgs.Empty);
        };
        Controls.Add(channelCombo);
        PropagateHover(channelCombo);

        // Rename button
        renameButton = new Button
        {
            Text = "✏",
            Location = new Point(778, 20),
            Size = new Size(32, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Text2,
            Font = new Font("Segoe UI", 11),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        renameButton.FlatAppearance.BorderSize = 0;
        renameButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
        renameButton.Click += (s, e) => RenameRequested?.Invoke(this, EventArgs.Empty);

        renameButton.MouseEnter += (s, e) => { renameButton.ForeColor = Text1; isHovering = true; Invalidate(); };
        renameButton.MouseLeave += (s, e) => { renameButton.ForeColor = Text2; isHovering = false; Invalidate(); };
        renameButton.MouseDown += (s, e) => renameButton.ForeColor = Accent;
        renameButton.MouseUp += (s, e) => renameButton.ForeColor = renameButton.ClientRectangle.Contains(renameButton.PointToClient(Cursor.Position)) ? Text1 : Text2;

        Controls.Add(renameButton);
        PropagateHover(renameButton);

        channelCombo.SelectedIndex = ChannelMode;
    }

    /// <summary>
    /// Custom painting for rounded corners, hover state, and enabled accent bar.
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(0, 0, Width, Height);
        var path = GetRoundedRect(bounds, RadiusMd);

        // Background
        using (var bgBrush = new SolidBrush(BackColor))
        {
            e.Graphics.FillPath(bgBrush, path);
        }

        // Hover overlay (subtle lightening)
        if (isHovering)
        {
            using var overlay = new SolidBrush(Color.FromArgb(8, 255, 255, 255));
            e.Graphics.FillPath(overlay, path);
        }

        // Left accent bar when enabled
        if (IsEnabled)
        {
            var accentBar = Color.FromArgb(153, Accent.R, Accent.G, Accent.B);
            using var brush = new SolidBrush(accentBar);
            var barPath = new GraphicsPath();
            barPath.AddArc(0, 0, RadiusMd, RadiusMd, 180, 90);
            barPath.AddLine(0, RadiusMd / 2, 0, Height - RadiusMd / 2);
            barPath.AddArc(0, Height - RadiusMd, RadiusMd, RadiusMd, 90, 90);
            barPath.AddLine(2, Height, 2, 0);
            barPath.CloseFigure();
            e.Graphics.FillPath(brush, barPath);
        }

        // Apply rounded corner clipping
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

    /// <summary>
    /// Returns an appropriate emoji icon based on the device name.
    /// </summary>
    private string GetDeviceIcon(MMDevice device)
    {
        var name = device.FriendlyName.ToLower();
        if (name.Contains("headphone")) return "🎧";
        if (name.Contains("speaker")) return "🔊";
        if (name.Contains("monitor") || name.Contains("display")) return "🖥️";
        return "🔈";
    }

    /// <summary>
    /// Returns a human-readable device type description.
    /// </summary>
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
#endregion

#region Settings Dialog
/// <summary>
/// Dialog for configuring application settings.
/// </summary>
class SettingsDialog : Form
{
    private static readonly Color BgPrimary = Color.FromArgb(15, 16, 32);
    private static readonly Color BgSecondary = Color.FromArgb(24, 26, 42);
    private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
    private static readonly Color TextSecondary = Color.FromArgb(160, 164, 192);

    public AppSettings Settings { get; }

    public SettingsDialog(AppSettings settings)
    {
        // Create a copy of settings to allow cancel
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

        // Language selection
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

        // Start with Windows checkbox
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

        // Start minimized checkbox
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

        // Auto-start audio checkbox
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

        // OK button
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

        // Cancel button
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
#endregion

#region Input Dialog
/// <summary>
/// Simple dialog for text input (e.g., renaming devices).
/// </summary>
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
#endregion

#region Custom Dropdown
/// <summary>
/// A custom-styled dropdown control that matches the application's dark theme.
/// Replaces the standard ComboBox for consistent appearance.
/// </summary>
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

        // Display label shows selected item text
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

        // Popup menu for dropdown items
        popup = new ToolStripDropDown
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Surface2,
            Renderer = new ToolStripProfessionalRenderer(new CustomColorTable())
        };

        popup.Closed += (s, e) =>
        {
            isDropdownOpen = false;
            lastCloseTime = DateTime.Now;
        };

        // Borrow ObjectCollection from a temporary ComboBox
        var tempCombo = new ComboBox();
        Items = tempCombo.Items;

        SizeChanged += (s, e) =>
        {
            displayLabel.Width = Width - 50;
            dropdownButton.Location = new Point(Width - 40, 0);
        };
    }

    /// <summary>
    /// Updates the display label to show the currently selected item.
    /// </summary>
    private void UpdateDisplay()
    {
        if (SelectedItem is DeviceItem item)
            displayLabel.Text = item.Device.FriendlyName;
        else if (SelectedItem != null)
            displayLabel.Text = SelectedItem.ToString();
        else
            displayLabel.Text = "";
    }

    /// <summary>
    /// Opens or closes the dropdown popup.
    /// </summary>
    private void ToggleDropdown()
    {
        // Debounce rapid clicks
        if ((DateTime.Now - lastCloseTime).TotalMilliseconds < 200)
            return;

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

        popup.Width = Width;
        popup.Height = popup.Items.Count * 30 + 4;
        popup.Show(this, new Point(0, Height));
        isDropdownOpen = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using (var pen = new Pen(BorderSoft, 1))
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    /// <summary>
    /// Custom color table for themed dropdown menu appearance.
    /// </summary>
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
#endregion

#region Helper Classes
/// <summary>
/// Wrapper for MMDevice to display friendly name in dropdowns.
/// </summary>
class DeviceItem
{
    public MMDevice Device { get; }
    public DeviceItem(MMDevice device) => Device = device;
    public override string ToString() => Device.FriendlyName;
}

/// <summary>
/// Wrapper for language selection in settings.
/// </summary>
class LanguageItem
{
    public string Code { get; }
    public string Name { get; }
    public LanguageItem(string code, string name) { Code = code; Name = name; }
    public override string ToString() => Name;
}
#endregion

#region Audio Processing
/// <summary>
/// Audio provider that buffers incoming samples and applies channel mixing.
/// Uses NAudio's BufferedWaveProvider for efficient, low-latency buffering.
///
/// Channel Modes:
/// 0 = Stereo (pass-through)
/// 1 = Left channel only
/// 2 = Right channel only
/// 3 = Center/Mono (L+R mix)
/// 4 = Front Left (same as 1)
/// 5 = Front Right (same as 2)
/// 6 = Back Left (Left at 85%)
/// 7 = Back Right (Right at 85%)
/// 8 = Back/Surround (mono)
/// 9 = Subwoofer/LFE (mono with bass boost)
/// </summary>
class ChannelMixingProvider : IWaveProvider
{
    private readonly BufferedWaveProvider bufferedProvider;
    private readonly int channelMode;

    public WaveFormat WaveFormat => bufferedProvider.WaveFormat;

    /// <summary>
    /// Creates a new channel mixing provider.
    /// </summary>
    /// <param name="sourceFormat">Audio format from the source</param>
    /// <param name="mode">Channel mixing mode (0-9)</param>
    public ChannelMixingProvider(WaveFormat sourceFormat, int mode)
    {
        channelMode = mode;
        bufferedProvider = new BufferedWaveProvider(sourceFormat)
        {
            // Discard old audio when buffer is full - prevents latency buildup
            DiscardOnBufferOverflow = true,
            // ~70ms buffer - balance between latency and stability
            BufferLength = sourceFormat.AverageBytesPerSecond / 14
        };
    }

    /// <summary>
    /// Adds audio samples to the buffer. Called by the loopback capture's DataAvailable event.
    /// </summary>
    public void AddSamples(byte[] samples, int offset, int count)
    {
        bufferedProvider.AddSamples(samples, offset, count);
    }

    /// <summary>
    /// Reads audio samples from the buffer. Called by WasapiOut when it needs more data.
    /// Always returns the requested count, filling with silence if necessary.
    /// </summary>
    public int Read(byte[] outBuffer, int offset, int count)
    {
        int read = bufferedProvider.Read(outBuffer, offset, count);

        // Fill any remaining with silence to prevent WasapiOut from stopping
        if (read < count)
        {
            Array.Clear(outBuffer, offset + read, count - read);
        }

        // Apply channel mixing if not in stereo pass-through mode
        if (channelMode > 0 && bufferedProvider.WaveFormat.Channels == 2 && read > 0)
        {
            ApplyChannelMixing(outBuffer, offset, read);
        }

        return count;
    }

    /// <summary>
    /// Applies the selected channel mixing mode to the audio buffer.
    /// Operates on 16-bit stereo samples (4 bytes per sample pair).
    /// </summary>
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
                case 1: // Left - copy left channel to right
                case 4: // Front Left
                    Array.Copy(buffer, leftIndex, buffer, rightIndex, bytesPerSample);
                    break;

                case 2: // Right - copy right channel to left
                case 5: // Front Right
                    Array.Copy(buffer, rightIndex, buffer, leftIndex, bytesPerSample);
                    break;

                case 3: // Center/Mono - mix both channels
                case 8: // Back/Surround
                    if (bytesPerSample == 2)
                    {
                        short left = BitConverter.ToInt16(buffer, leftIndex);
                        short right = BitConverter.ToInt16(buffer, rightIndex);
                        short mono = (short)((left + right) / 2);
                        Array.Copy(BitConverter.GetBytes(mono), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(mono), 0, buffer, rightIndex, 2);
                    }
                    break;

                case 6: // Back Left - left channel at reduced volume
                    if (bytesPerSample == 2)
                    {
                        short left = BitConverter.ToInt16(buffer, leftIndex);
                        short reduced = (short)(left * 0.85);
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, rightIndex, 2);
                    }
                    break;

                case 7: // Back Right - right channel at reduced volume
                    if (bytesPerSample == 2)
                    {
                        short right = BitConverter.ToInt16(buffer, rightIndex);
                        short reduced = (short)(right * 0.85);
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(reduced), 0, buffer, rightIndex, 2);
                    }
                    break;

                case 9: // Subwoofer/LFE - mono with bass boost
                    if (bytesPerSample == 2)
                    {
                        short left = BitConverter.ToInt16(buffer, leftIndex);
                        short right = BitConverter.ToInt16(buffer, rightIndex);
                        // Mix to mono and apply 1.3x gain for bass emphasis
                        short bass = (short)(((left + right) / 2) * 1.3);
                        Array.Copy(BitConverter.GetBytes(bass), 0, buffer, leftIndex, 2);
                        Array.Copy(BitConverter.GetBytes(bass), 0, buffer, rightIndex, 2);
                    }
                    break;
            }
        }
    }
}
#endregion
