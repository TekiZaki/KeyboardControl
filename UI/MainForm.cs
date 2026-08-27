// ---
// Summary:
// - Purpose: Main form UI for brightness and master volume adjustments.
// - Role: Main user interface and coordinator.
// - Used by: Program.cs.
// - Depends on: VolumeControl, BrightnessControl, HotkeyManager, FlyoutOsd.
// - Key Responsibilities: Provide slider controls, step buttons, and background polling synchronization.
// - Notes: Modern, cleanly spaced UI with no overlapping elements and smooth async syncing.
// ---

using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardControl.Controls;

namespace KeyboardControl.UI
{
    public class MainForm : Form
    {
        private readonly VolumeControl _volumeControl;
        private readonly BrightnessControl _brightnessControl;
        private readonly HotkeyManager _hotkeyManager;
        private readonly FlyoutOsd _osd;

        private Label _brightnessValueLabel;
        private TrackBar _brightnessSlider;
        private Label _volumeValueLabel;
        private TrackBar _volumeSlider;
        private Button _muteButton;
        private Timer _syncTimer;
        private int _syncTickCount = 0;

        private int _currentBrightness;
        private int _currentVolume;

        public MainForm()
        {
            _volumeControl = new VolumeControl();
            _brightnessControl = new BrightnessControl();
            _hotkeyManager = new HotkeyManager(_volumeControl, _brightnessControl);
            _osd = new FlyoutOsd();

            InitializeComponent();
            SetupHotkeys();
            StartSyncTimer();
        }

        private void InitializeComponent()
        {
            Text = "Pengatur Kecerahan & Volume";
            ClientSize = new Size(430, 410);
            MinimumSize = new Size(430, 410);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));

            // ==========================================
            // 1. BRIGHTNESS GROUP
            // ==========================================
            var brightnessGroup = new GroupBox
            {
                Text = " Kontrol Kecerahan Layar (Alt+← / Alt+→ / Ctrl+[ / Ctrl+]) ",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8)
            };

            var brightnessInnerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            brightnessInnerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            brightnessInnerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            brightnessInnerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            _currentBrightness = _brightnessControl.GetCurrent();

            var brightnessHeaderPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var briTitle = new Label
            {
                Text = "Kecerahan saat ini:",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _brightnessValueLabel = new Label
            {
                Text = string.Format("{0}%", _currentBrightness),
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                TextAlign = ContentAlignment.MiddleRight
            };
            brightnessHeaderPanel.Controls.Add(briTitle);
            brightnessHeaderPanel.Controls.Add(_brightnessValueLabel);

            _brightnessSlider = new TrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Value = _currentBrightness,
                Margin = new Padding(2, 2, 2, 2)
            };
            _brightnessSlider.ValueChanged += (s, e) => UpdateBrightnessFromSlider(_brightnessSlider.Value);

            var brightnessBtnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            var dimBtn = new Button
            {
                Text = "◀ Redupkan (-2%)",
                Width = 140,
                Height = 30,
                FlatStyle = FlatStyle.System
            };
            dimBtn.Click += (s, e) => AdjustBrightness(-2);

            var brightBtn = new Button
            {
                Text = "Cerahkan (+2%) ▶",
                Width = 140,
                Height = 30,
                FlatStyle = FlatStyle.System
            };
            brightBtn.Click += (s, e) => AdjustBrightness(2);

            brightnessBtnFlow.Controls.Add(dimBtn);
            brightnessBtnFlow.Controls.Add(brightBtn);

            brightnessInnerLayout.Controls.Add(brightnessHeaderPanel, 0, 0);
            brightnessInnerLayout.Controls.Add(_brightnessSlider, 0, 1);
            brightnessInnerLayout.Controls.Add(brightnessBtnFlow, 0, 2);
            brightnessGroup.Controls.Add(brightnessInnerLayout);

            // ==========================================
            // 2. VOLUME GROUP
            // ==========================================
            var volumeGroup = new GroupBox
            {
                Text = " Kontrol Volume Suara (Alt+- / Alt+= / Alt+↑ / Alt+↓) ",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8)
            };

            var volumeInnerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            volumeInnerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            volumeInnerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            volumeInnerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            _currentVolume = _volumeControl.GetCurrentPercent();

            var volumeHeaderPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var volTitle = new Label
            {
                Text = "Volume saat ini:",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _volumeValueLabel = new Label
            {
                Text = string.Format("{0}%", _currentVolume),
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 130, 60),
                TextAlign = ContentAlignment.MiddleRight
            };
            volumeHeaderPanel.Controls.Add(volTitle);
            volumeHeaderPanel.Controls.Add(_volumeValueLabel);

            _volumeSlider = new TrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Value = _currentVolume,
                Margin = new Padding(2, 2, 2, 2)
            };
            _volumeSlider.ValueChanged += (s, e) => UpdateVolumeFromSlider(_volumeSlider.Value);

            var volumeBtnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            var volDownBtn = new Button
            {
                Text = "◀ Kecilkan (-2%)",
                Width = 140,
                Height = 30,
                FlatStyle = FlatStyle.System
            };
            volDownBtn.Click += (s, e) => AdjustVolume(-2);

            var volUpBtn = new Button
            {
                Text = "Besarkan (+2%) ▶",
                Width = 140,
                Height = 30,
                FlatStyle = FlatStyle.System
            };
            volUpBtn.Click += (s, e) => AdjustVolume(2);

            volumeBtnFlow.Controls.Add(volDownBtn);
            volumeBtnFlow.Controls.Add(volUpBtn);

            volumeInnerLayout.Controls.Add(volumeHeaderPanel, 0, 0);
            volumeInnerLayout.Controls.Add(_volumeSlider, 0, 1);
            volumeInnerLayout.Controls.Add(volumeBtnFlow, 0, 2);
            volumeGroup.Controls.Add(volumeInnerLayout);

            // ==========================================
            // 3. MUTE BUTTON
            // ==========================================
            _muteButton = new Button
            {
                Text = _volumeControl.IsMuted() ? "Unmute Suara (Muted)" : "Mute Suara",
                Dock = DockStyle.Fill,
                Height = 34,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.System
            };
            _muteButton.Click += (s, e) => ToggleMute();

            rootLayout.Controls.Add(brightnessGroup, 0, 0);
            rootLayout.Controls.Add(volumeGroup, 0, 1);
            rootLayout.Controls.Add(_muteButton, 0, 2);

            Controls.Add(rootLayout);
            UpdateVolumeLabel();
        }

        private void SetupHotkeys()
        {
            _hotkeyManager.OnChange += (controlType, value) =>
            {
                BeginInvoke(new Action(() =>
                {
                    if (controlType == "volume")
                    {
                        var isMuted = _volumeControl.IsMuted();
                        _currentVolume = value;
                        if (_volumeSlider.Value != value)
                        {
                            _volumeSlider.Value = value;
                        }
                        UpdateVolumeLabel();
                        _osd.ShowOsd("volume", value, isMuted);
                    }
                    else if (controlType == "brightness")
                    {
                        _currentBrightness = value;
                        if (_brightnessSlider.Value != value)
                        {
                            _brightnessSlider.Value = value;
                        }
                        _brightnessValueLabel.Text = string.Format("{0}%", value);
                        _osd.ShowOsd("brightness", value);
                    }
                }));
            };
            _hotkeyManager.Setup();
        }

        private void StartSyncTimer()
        {
            _syncTimer = new Timer { Interval = 250 };
            _syncTimer.Tick += (s, e) => SyncSystemLevels();
            _syncTimer.Start();
        }

        private void SyncSystemLevels()
        {
            try
            {
                // 1. Sync Volume (instant)
                var realVol = _volumeControl.GetCurrentPercent();
                var isMuted = _volumeControl.IsMuted();

                if (realVol != _currentVolume)
                {
                    _currentVolume = realVol;
                    if (Math.Abs(_volumeSlider.Value - realVol) >= 1)
                    {
                        _volumeSlider.Value = realVol;
                    }
                    UpdateVolumeLabel();
                }
                else if (isMuted != _volumeValueLabel.Text.Contains("Muted"))
                {
                    UpdateVolumeLabel();
                }

                // 2. Sync Brightness periodically (every 2 seconds)
                _syncTickCount++;
                if (_syncTickCount >= 8)
                {
                    _syncTickCount = 0;
                    var realBri = _brightnessControl.QueryHardwareBrightness();
                    if (realBri.HasValue && realBri.Value != _currentBrightness)
                    {
                        _currentBrightness = realBri.Value;
                        if (Math.Abs(_brightnessSlider.Value - realBri.Value) >= 1)
                        {
                            _brightnessSlider.Value = realBri.Value;
                        }
                        _brightnessValueLabel.Text = string.Format("{0}%", realBri.Value);
                    }
                }
            }
            catch
            {
            }
        }

        private void AdjustBrightness(int delta)
        {
            var target = Math.Max(0, Math.Min(100, _currentBrightness + delta));
            UpdateBrightness(target);
        }

        private void UpdateBrightnessFromSlider(int value)
        {
            if (value != _currentBrightness)
            {
                UpdateBrightness(value);
            }
        }

        private void UpdateBrightness(int value)
        {
            _currentBrightness = value;
            _brightnessValueLabel.Text = string.Format("{0}%", value);
            if (_brightnessSlider.Value != value)
            {
                _brightnessSlider.Value = value;
            }
            _brightnessControl.Set(value);
            _osd.ShowOsd("brightness", value);
        }

        private void AdjustVolume(int delta)
        {
            var target = Math.Max(0, Math.Min(100, _currentVolume + delta));
            UpdateVolume(target);
        }

        private void UpdateVolumeFromSlider(int value)
        {
            if (value != _currentVolume)
            {
                UpdateVolume(value);
            }
        }

        private void UpdateVolume(int value)
        {
            var res = _volumeControl.SetVolume(value);
            _currentVolume = res;
            if (_volumeSlider.Value != res)
            {
                _volumeSlider.Value = res;
            }
            UpdateVolumeLabel();
        }

        private void ToggleMute()
        {
            _volumeControl.ToggleMute();
            UpdateVolumeLabel();
        }

        private void UpdateVolumeLabel()
        {
            var isMuted = _volumeControl.IsMuted();
            _muteButton.Text = isMuted ? "Unmute Suara (Muted)" : "Mute Suara";
            _volumeValueLabel.Text = isMuted ? "Muted" : string.Format("{0}%", _currentVolume);
            _volumeValueLabel.ForeColor = isMuted ? Color.Red : Color.FromArgb(0, 130, 60);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_syncTimer != null) _syncTimer.Stop();
            if (_hotkeyManager != null) _hotkeyManager.Dispose();
            if (_volumeControl != null) _volumeControl.Dispose();
            if (_osd != null) _osd.Dispose();
            base.OnFormClosing(e);
        }
    }
}
