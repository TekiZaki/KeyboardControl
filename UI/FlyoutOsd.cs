// ---
// Summary:
// - Purpose: Floating on-screen display overlay.
// - Role: Visual notification UI.
// - Used by: MainForm.
// - Depends on: System.Windows.Forms, System.Drawing.
// - Key Responsibilities: Display level bars on screen top and auto-hide.
// - Notes: Operates as a topmost, non-activating tool window.
// ---

using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeyboardControl.UI
{
    public class FlyoutOsd : Form
    {
        private readonly Label _titleLabel;
        private readonly ProgressBar _progressBar;
        private readonly Timer _hideTimer;

        public FlyoutOsd()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(30, 30, 30);
            Size = new Size(240, 64);
            Padding = new Padding(1);

            var borderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(51, 51, 51),
                Padding = new Padding(1)
            };

            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(16, 10, 16, 10)
            };

            _titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 16,
                Minimum = 0,
                Maximum = 100
            };

            containerPanel.Controls.Add(_titleLabel);
            containerPanel.Controls.Add(_progressBar);
            borderPanel.Controls.Add(containerPanel);
            Controls.Add(borderPanel);

            _hideTimer = new Timer
            {
                Interval = 1200
            };
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                Hide();
            };
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        public void ShowOsd(string controlType, int value, bool isMuted = false)
        {
            var screen = Screen.PrimaryScreen.Bounds;
            Location = new Point((screen.Width - Width) / 2, 50);

            if (controlType == "volume")
            {
                if (isMuted)
                {
                    _titleLabel.Text = "Volume: Muted";
                    _progressBar.Value = 0;
                }
                else
                {
                    _titleLabel.Text = string.Format("Volume: {0}%", value);
                    _progressBar.Value = Math.Max(0, Math.Min(100, value));
                }
            }
            else if (controlType == "brightness")
            {
                _titleLabel.Text = string.Format("Kecerahan: {0}%", value);
                _progressBar.Value = Math.Max(0, Math.Min(100, value));
            }

            _hideTimer.Stop();
            Show();
            _hideTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hideTimer != null)
                {
                    _hideTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
