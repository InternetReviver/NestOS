using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal class DesktopForm : Form, IDesktopHost
    {
        private readonly DesktopBackdropPanel desktopArea;
        private readonly Panel taskbar;
        private readonly FlowLayoutPanel taskButtons;
        private readonly Label footerLabel;
        private readonly List<RetroWindow> windows;
        private readonly Dictionary<string, Control> desktopIcons;
        private readonly Timer desktopIconMenuTimer;
        private DesktopSettingsData desktopSettings;
        private Control lastDesktopIcon;
        private int lastDesktopIconTick;
        private int windowOffset;
        private Control pressedDesktopIcon;
        private Point pressedDesktopIconMouse;
        private bool desktopIconDragging;
        private Control suppressedDesktopIconClickTarget;
        private Control pendingDesktopIcon;
        private string pendingDesktopIconLabel;

        public DesktopForm()
        {
            windows = new List<RetroWindow>();
            desktopIcons = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
            desktopIconMenuTimer = new Timer();
            desktopIconMenuTimer.Interval = Math.Max(300, SystemInformation.DoubleClickTime);
            desktopIconMenuTimer.Tick += HandleDesktopIconMenuTimerTick;

            Text = "NestsOS";
            BackColor = RetroPalette.SetupBlueMid;
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1024, 720);
            Font = RetroFont.Ui();
            KeyPreview = true;
            KeyDown += HandleDesktopKeyDown;

            desktopArea = new DesktopBackdropPanel();
            desktopArea.Dock = DockStyle.Fill;
            desktopArea.MouseDown += delegate
            {
                CancelPendingDesktopIconMenu();
                ActivateWindow(null);
            };

            taskbar = new Panel();
            taskbar.Dock = DockStyle.Bottom;
            taskbar.Height = 40;
            taskbar.BackColor = RetroPalette.WindowBackground;
            taskbar.Paint += DrawTaskbarBorder;

            var nestButton = new RetroButton();
            nestButton.Text = "Nest";
            nestButton.SetBounds(6, 8, 72, 24);
            nestButton.Click += delegate { ShowLauncherMenu(nestButton); };

            taskButtons = new FlowLayoutPanel();
            taskButtons.SetBounds(84, 6, 800, 28);
            taskButtons.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            taskButtons.WrapContents = false;
            taskButtons.AutoScroll = true;
            taskButtons.BackColor = Color.Transparent;

            footerLabel = new Label();
            footerLabel.AutoSize = false;
            footerLabel.Text = AppInfo.BuildLabel + "   SAVE: " + SaveSystem.EnsureRoot();
            footerLabel.TextAlign = ContentAlignment.MiddleRight;
            footerLabel.Font = RetroFont.Ui();
            footerLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            footerLabel.SetBounds(890, 10, 420, 20);

            taskbar.Controls.Add(nestButton);
            taskbar.Controls.Add(taskButtons);
            taskbar.Controls.Add(footerLabel);

            Controls.Add(desktopArea);
            Controls.Add(taskbar);

            desktopSettings = DesktopSettingsStore.Load();
            WindowTheme.Apply(desktopSettings);
            SoundPreferences.Apply(desktopSettings);
            desktopArea.ApplySettings(desktopSettings);

            AddDesktopIcon("Paint", new Point(24, 24), delegate { OpenPaint(null); });
            AddDesktopIcon("Calculator", new Point(24, 110), delegate { OpenCalculator(); });
            AddDesktopIcon("TextPad", new Point(24, 196), delegate { OpenTextEditor(null); });
            AddDesktopIcon("File Cabinet", new Point(24, 282), delegate { OpenFileCabinet(); });
            AddDesktopIcon("NestDOS", new Point(24, 368), delegate { OpenNestDos(); });
            AddDesktopIcon("Solitaire", new Point(24, 454), delegate { OpenSolitaire(); });
            AddDesktopIcon("Minesweeper", new Point(24, 540), delegate { OpenMinesweeper(); });
            AddDesktopIcon("Mahjong", new Point(24, 626), delegate { OpenMahjong(); });
            AddDesktopIcon("Chess", new Point(136, 454), delegate { OpenChess(); });
            AddDesktopIcon("Analog Clock", new Point(136, 24), delegate { OpenAnalogClock(); });
            AddDesktopIcon("Digital Clock", new Point(136, 110), delegate { OpenDigitalClock(); });
            AddDesktopIcon("Settings", new Point(136, 196), delegate { OpenSettings(); });
            AddDesktopIcon("Export", new Point(136, 282), delegate { OpenExport(); });
            AddDesktopIcon("Media Center", new Point(136, 540), delegate { OpenMediaCenter(); });
            AddDesktopIcon("Home Panel", new Point(248, 24), delegate { OpenHomePanel(); });

            var splash = new Label();
            splash.Text = "NestsOS";
            splash.Font = RetroFont.Banner();
            splash.ForeColor = Color.White;
            splash.BackColor = Color.Transparent;
            splash.AutoSize = true;
            splash.Location = new Point(160, 70);

            var sub = new Label();
            sub.Text = "Windows 3.1 inspired desktop in one executable";
            sub.Font = RetroFont.UiBold();
            sub.ForeColor = Color.White;
            sub.BackColor = Color.Transparent;
            sub.AutoSize = true;
            sub.Location = new Point(166, 118);

            desktopArea.Controls.Add(splash);
            desktopArea.Controls.Add(sub);

            Resize += delegate
            {
                footerLabel.SetBounds(ClientSize.Width - 430, 10, 420, 20);
                taskButtons.Width = ClientSize.Width - 530;
            };

            Load += delegate
            {
                footerLabel.SetBounds(ClientSize.Width - 430, 10, 420, 20);
                taskButtons.Width = ClientSize.Width - 530;
            };

            Shown += delegate { StartupAudio.PlayWin31(false); };
            FormClosed += delegate { StartupAudio.Stop(); };
        }

        public void OpenTextEditor(string path)
        {
            OpenWindow("TextPad", new Size(640, 420), new TextEditorApp(this, path));
        }

        public void OpenPaint(string path)
        {
            OpenWindow("Paint", new Size(760, 560), new PaintApp(this, path));
        }

        public void OpenCalculator()
        {
            OpenWindow("Calculator", new Size(270, 320), new CalculatorApp());
        }

        public void OpenExport()
        {
            OpenWindow("Export", new Size(540, 380), new ExportApp(this));
        }

        public void OpenHomePanel()
        {
            OpenWindow("Home Panel", new Size(760, 520), new HomePanelApp(this));
        }

        public void OpenMediaCenter()
        {
            OpenWindow("Media Center", new Size(1080, 650), new MediaCenterApp(this));
        }

        public void OpenSettings()
        {
            OpenWindow("Settings", new Size(620, 460), new SettingsApp(this));
        }

        public void OpenFileCabinet()
        {
            OpenWindow("File Cabinet", new Size(620, 420), new FileCabinetApp(this));
        }

        public void OpenNestDos()
        {
            OpenWindow("NestDOS", new Size(720, 430), new NestDosApp(this));
        }

        public void OpenSolitaire()
        {
            OpenWindow("Solitaire", new Size(860, 620), new SolitaireApp(this));
        }

        public void OpenMinesweeper()
        {
            OpenWindow("Minesweeper", new Size(320, 340), new MinesweeperApp(this));
        }

        public void OpenMahjong()
        {
            OpenWindow("Mahjong", new Size(820, 560), new MahjongApp(this));
        }

        public void OpenChess()
        {
            OpenWindow("Chess", new Size(660, 620), new ChessApp());
        }

        public void OpenAnalogClock()
        {
            OpenWindow("Analog Clock", new Size(340, 390), new AnalogClockApp());
        }

        public void OpenDigitalClock()
        {
            OpenWindow("Digital Clock", new Size(420, 220), new DigitalClockApp());
        }

        public void OpenSystemUsage()
        {
            OpenWindow("System Usage", new Size(440, 340), new SystemUsageApp());
        }

        public void OpenVersionInfo()
        {
            OpenWindow("Version", new Size(520, 360), new VersionApp());
        }

        public void OpenNostRun(string initialCommand)
        {
            StartupAudio.PlayChimes(false);
            OpenWindow("Nost64/32", new Size(380, 260), new NostRunApp(this, initialCommand));
        }

        public bool RunShellCommand(string command)
        {
            var normalized = (command ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized.Length == 0)
            {
                return false;
            }

            switch (normalized)
            {
                case "paint":
                case "pbrush":
                case "mspaint":
                    OpenPaint(null);
                    return true;
                case "calc":
                case "calculator":
                    OpenCalculator();
                    return true;
                case "media":
                case "media center":
                case "mediacenter":
                    OpenMediaCenter();
                    return true;
                case "home":
                case "home panel":
                case "homepanel":
                    OpenHomePanel();
                    return true;
                case "export":
                case "exporter":
                case "convert":
                    OpenExport();
                    return true;
                case "textpad":
                case "edit":
                case "notepad":
                case "write":
                    OpenTextEditor(null);
                    return true;
                case "settings":
                case "control":
                case "control panel":
                    OpenSettings();
                    return true;
                case "files":
                case "filecabinet":
                case "file cabinet":
                case "explorer":
                    OpenFileCabinet();
                    return true;
                case "nestdos":
                case "dos":
                case "cmd":
                    OpenNestDos();
                    return true;
                case "sol":
                case "solitaire":
                    OpenSolitaire();
                    return true;
                case "mines":
                case "minesweeper":
                case "winmine":
                    OpenMinesweeper();
                    return true;
                case "mahjong":
                case "mahjongg":
                    OpenMahjong();
                    return true;
                case "chess":
                    OpenChess();
                    return true;
                case "clock":
                case "analog":
                case "analog clock":
                    OpenAnalogClock();
                    return true;
                case "digital":
                case "digital clock":
                case "dclock":
                    OpenDigitalClock();
                    return true;
                case "system usage":
                case "sysusage":
                case "usage":
                case "sysinfo":
                    OpenSystemUsage();
                    return true;
                case "version":
                case "ver":
                case "about":
                    OpenVersionInfo();
                    return true;
                case "nost64/32":
                case "run":
                    OpenNostRun(string.Empty);
                    return true;
                default:
                    return false;
            }
        }

        public void ShowMessage(string title, string message)
        {
            RetroDialogs.ShowMessage(this, title, message);
        }

        public bool Confirm(string title, string message)
        {
            return RetroDialogs.Confirm(this, title, message);
        }

        public DesktopSettingsData GetDesktopSettings()
        {
            return desktopSettings.Clone();
        }

        public void ApplyDesktopSettings(DesktopSettingsData settings)
        {
            desktopSettings = settings;
            DesktopSettingsStore.Save(desktopSettings);
            WindowTheme.Apply(desktopSettings);
            SoundPreferences.Apply(desktopSettings);
            desktopArea.ApplySettings(desktopSettings);
            RefreshWindowThemes();
        }

        public void FactoryResetNestOs()
        {
            if (!Confirm("Factory Reset", "This will erase everything in the SAVE folder and restart NestOS. Continue?"))
            {
                return;
            }

            try
            {
                desktopArea.ApplySettings(new DesktopSettingsData());
                DesktopSettingsStore.FactoryReset();
                RestartNestOs();
            }
            catch (Exception ex)
            {
                ShowMessage("Factory Reset", "Factory reset failed.\r\n\r\n" + ex.Message);
            }
        }

        private void AddDesktopIcon(string label, Point location, EventHandler clickHandler)
        {
            var button = new Button();
            button.Text = label;
            button.Font = RetroFont.Ui();
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.ForeColor = Color.White;
            button.BackColor = Color.Transparent;
            button.TextAlign = ContentAlignment.BottomCenter;
            button.ImageAlign = ContentAlignment.TopCenter;
            button.Size = new Size(104, 72);
            button.Location = GetSavedDesktopIconLocation(label, location);
            button.Image = LegacyIconProvider.TryLoadDesktopIcon(label) ?? BuildDesktopGlyph(label);
            button.MouseDown += BeginDesktopIconDrag;
            button.MouseMove += ContinueDesktopIconDrag;
            button.MouseUp += EndDesktopIconDrag;
            button.Click += delegate(object sender, EventArgs e)
            {
                if (suppressedDesktopIconClickTarget == button)
                {
                    suppressedDesktopIconClickTarget = null;
                    return;
                }

                if (pendingDesktopIcon == button && desktopIconMenuTimer.Enabled)
                {
                    desktopIconMenuTimer.Stop();
                    pendingDesktopIcon = null;
                    pendingDesktopIconLabel = null;
                    clickHandler(sender, e);
                    return;
                }

                var now = Environment.TickCount;
                var sameButton = lastDesktopIcon == button;
                var withinWindow = sameButton && unchecked(now - lastDesktopIconTick) <= SystemInformation.DoubleClickTime;

                lastDesktopIcon = button;
                lastDesktopIconTick = now;

                if (withinWindow)
                {
                    lastDesktopIcon = null;
                    lastDesktopIconTick = 0;
                    CancelPendingDesktopIconMenu();
                    clickHandler(sender, e);
                    return;
                }

                pendingDesktopIcon = button;
                pendingDesktopIconLabel = label;
                desktopIconMenuTimer.Stop();
                desktopIconMenuTimer.Start();
            };

            desktopIcons[label] = button;
            desktopArea.Controls.Add(button);
        }

        private Image BuildDesktopGlyph(string label)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                if (label == "Paint")
                {
                    g.FillRectangle(Brushes.White, 4, 6, 24, 18);
                    g.DrawRectangle(Pens.Black, 4, 6, 24, 18);
                    g.FillRectangle(Brushes.Red, 6, 8, 6, 6);
                    g.FillRectangle(Brushes.Blue, 13, 8, 6, 6);
                    g.FillRectangle(Brushes.Green, 20, 8, 6, 6);
                }
                else if (label == "Calculator")
                {
                    g.FillRectangle(Brushes.Bisque, 7, 4, 18, 24);
                    g.DrawRectangle(Pens.Black, 7, 4, 18, 24);
                    g.FillRectangle(Brushes.DarkSlateGray, 10, 7, 12, 5);
                    g.FillRectangle(Brushes.LightGray, 10, 15, 4, 4);
                    g.FillRectangle(Brushes.LightGray, 15, 15, 4, 4);
                    g.FillRectangle(Brushes.LightGray, 20, 15, 4, 4);
                    g.FillRectangle(Brushes.LightGray, 10, 20, 4, 4);
                    g.FillRectangle(Brushes.LightGray, 15, 20, 4, 4);
                    g.FillRectangle(Brushes.LightGray, 20, 20, 4, 4);
                }
                else if (label == "Export")
                {
                    g.FillRectangle(Brushes.Wheat, 6, 8, 20, 14);
                    g.DrawRectangle(Pens.Black, 6, 8, 20, 14);
                    g.FillRectangle(Brushes.Khaki, 10, 5, 10, 6);
                    g.DrawRectangle(Pens.Black, 10, 5, 10, 6);
                    g.DrawLine(Pens.Blue, 11, 15, 21, 15);
                    g.DrawLine(Pens.Blue, 18, 12, 21, 15);
                    g.DrawLine(Pens.Blue, 18, 18, 21, 15);
                }
                else if (label == "TextPad")
                {
                    g.FillRectangle(Brushes.White, 7, 4, 18, 24);
                    g.DrawRectangle(Pens.Black, 7, 4, 18, 24);
                    g.DrawLine(Pens.Blue, 10, 11, 22, 11);
                    g.DrawLine(Pens.Blue, 10, 15, 22, 15);
                    g.DrawLine(Pens.Blue, 10, 19, 22, 19);
                }
                else if (label == "File Cabinet")
                {
                    g.FillRectangle(Brushes.Goldenrod, 4, 10, 24, 16);
                    g.DrawRectangle(Pens.Black, 4, 10, 24, 16);
                    g.FillRectangle(Brushes.Khaki, 8, 6, 10, 8);
                    g.DrawRectangle(Pens.Black, 8, 6, 10, 8);
                }
                else if (label == "NestDOS")
                {
                    g.FillRectangle(Brushes.Black, 3, 6, 26, 20);
                    g.DrawRectangle(Pens.White, 3, 6, 26, 20);
                    g.DrawString(">", RetroFont.UiBold(), Brushes.Lime, 9, 10);
                }
                else if (label == "Solitaire")
                {
                    g.FillRectangle(Brushes.DarkGreen, 4, 4, 24, 24);
                    g.DrawRectangle(Pens.Black, 4, 4, 24, 24);
                    g.FillRectangle(Brushes.White, 8, 8, 10, 14);
                    g.DrawRectangle(Pens.Black, 8, 8, 10, 14);
                    g.DrawString("A", RetroFont.Ui(), Brushes.Red, 9, 10);
                }
                else if (label == "Minesweeper")
                {
                    g.FillRectangle(Brushes.Green, 4, 4, 24, 24);
                    g.DrawRectangle(Pens.Black, 4, 4, 24, 24);
                    g.FillRectangle(Brushes.White, 8, 8, 14, 14);
                    g.DrawRectangle(Pens.DarkGreen, 8, 8, 14, 14);
                    g.DrawString("*", RetroFont.UiBold(), Brushes.Red, 12, 10);
                }
                else if (label == "Mahjong")
                {
                    g.FillRectangle(Brushes.Bisque, 5, 6, 22, 18);
                    g.DrawRectangle(Pens.Black, 5, 6, 22, 18);
                    g.DrawString("M", RetroFont.UiBold(), Brushes.Maroon, 11, 10);
                }
                else if (label == "Analog Clock")
                {
                    g.FillEllipse(Brushes.White, 4, 4, 24, 24);
                    g.DrawEllipse(Pens.Black, 4, 4, 24, 24);
                    g.DrawLine(Pens.Black, 16, 16, 16, 9);
                    g.DrawLine(Pens.Black, 16, 16, 22, 19);
                }
                else if (label == "Digital Clock")
                {
                    g.FillRectangle(Brushes.Black, 3, 7, 26, 18);
                    g.DrawRectangle(Pens.White, 3, 7, 26, 18);
                    g.DrawString("12", RetroFont.UiBold(), Brushes.Lime, 8, 10);
                }
                else if (label == "Settings")
                {
                    g.FillRectangle(Brushes.LightGray, 6, 6, 20, 20);
                    g.DrawRectangle(Pens.Black, 6, 6, 20, 20);
                    g.DrawEllipse(Pens.Black, 10, 10, 12, 12);
                    g.DrawLine(Pens.Black, 16, 10, 16, 22);
                    g.DrawLine(Pens.Black, 10, 16, 22, 16);
                }
                else if (label == "Media Center")
                {
                    g.FillRectangle(Brushes.LightSkyBlue, 4, 5, 24, 20);
                    g.DrawRectangle(Pens.Navy, 4, 5, 24, 20);
                    g.FillRectangle(Brushes.MidnightBlue, 7, 8, 18, 8);
                    g.FillRectangle(Brushes.PaleGreen, 7, 18, 18, 4);
                    g.DrawLine(Pens.White, 11, 11, 21, 11);
                }
                else if (label == "Home Panel")
                {
                    g.FillRectangle(Brushes.LightGray, 4, 4, 24, 24);
                    g.DrawRectangle(Pens.Black, 4, 4, 24, 24);
                    g.FillRectangle(Brushes.DeepSkyBlue, 7, 7, 18, 6);
                    g.FillRectangle(Brushes.White, 7, 15, 7, 9);
                    g.FillRectangle(Brushes.White, 18, 15, 7, 9);
                    g.DrawRectangle(Pens.Navy, 7, 15, 7, 9);
                    g.DrawRectangle(Pens.ForestGreen, 18, 15, 7, 9);
                }
                else
                {
                    g.FillRectangle(Brushes.Wheat, 4, 4, 24, 24);
                    g.DrawRectangle(Pens.Black, 4, 4, 24, 24);
                    g.DrawEllipse(Pens.Black, 8, 8, 16, 16);
                    g.DrawLine(Pens.Black, 16, 8, 16, 24);
                    g.DrawLine(Pens.Black, 8, 16, 24, 16);
                }
            }

            return bmp;
        }

        private void OpenWindow(string title, Size size, Control content)
        {
            var window = new RetroWindow();
            window.Title = title;
            window.Size = size;
            window.Content = content;
            window.Location = new Point(160 + (windowOffset * 24), 140 + (windowOffset * 20));
            window.Anchor = AnchorStyles.None;
            window.SetActive(true);

            var taskButton = new RetroButton();
            taskButton.Text = title;
            taskButton.Size = new Size(140, 24);
            taskButton.Margin = new Padding(2);
            taskButton.Click += delegate
            {
                if (!window.Visible)
                {
                    window.RestoreFromMinimize();
                }
                else
                {
                    ActivateWindow(window);
                }
            };

            window.WindowClosed += delegate
            {
                windows.Remove(window);
                desktopArea.Controls.Remove(window);
                taskButtons.Controls.Remove(taskButton);
                taskButton.Dispose();
                window.Dispose();
                ActivateWindow(windows.Count == 0 ? null : windows[windows.Count - 1]);
            };

            window.WindowMinimized += delegate
            {
                window.Visible = false;
            };

            window.Activated += delegate { ActivateWindow(window); };

            desktopArea.Controls.Add(window);
            taskButtons.Controls.Add(taskButton);
            windows.Add(window);
            ActivateWindow(window);

            windowOffset++;
            if (windowOffset > 8)
            {
                windowOffset = 0;
            }
        }

        private void ActivateWindow(RetroWindow window)
        {
            for (var i = 0; i < windows.Count; i++)
            {
                windows[i].SetActive(windows[i] == window);
            }

            if (window != null)
            {
                window.Visible = true;
                window.BringToFront();
            }
        }

        private void DrawTaskbarBorder(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder3D(e.Graphics, taskbar.ClientRectangle, Border3DStyle.Raised);
        }

        private void ShowLauncherMenu(Control source)
        {
            var menu = new ContextMenuStrip();
            menu.Font = RetroFont.Ui();

            var startApps = GetStartMenuAppLabels();
            var pinnedApps = new List<string>();
            for (var i = 0; i < desktopSettings.PinnedApps.Count; i++)
            {
                var app = desktopSettings.PinnedApps[i];
                if (Array.IndexOf(startApps, app) >= 0)
                {
                    pinnedApps.Add(app);
                }
            }

            if (pinnedApps.Count > 0)
            {
                var pinnedHeader = new ToolStripMenuItem("Pinned");
                pinnedHeader.Enabled = false;
                menu.Items.Add(pinnedHeader);
                for (var i = 0; i < pinnedApps.Count; i++)
                {
                    menu.Items.Add(BuildStartMenuAppItem(menu, pinnedApps[i]));
                }

                menu.Items.Add("-");
            }

            var appsHeader = new ToolStripMenuItem("Apps");
            appsHeader.Enabled = false;
            menu.Items.Add(appsHeader);

            for (var i = 0; i < startApps.Length; i++)
            {
                if (pinnedApps.IndexOf(startApps[i]) >= 0)
                {
                    continue;
                }

                menu.Items.Add(BuildStartMenuAppItem(menu, startApps[i]));
            }

            menu.Items.Add("-");
            menu.Items.Add("Open SAVE Folder", null, delegate
            {
                var root = SaveSystem.EnsureRoot();
                if (Directory.Exists(root))
                {
                    Process.Start("explorer.exe", root);
                }
            });
            menu.Items.Add("-");
            menu.Items.Add("Restart NestOS", null, delegate { RestartNestOs(); });
            menu.Items.Add("Shut Down PC", null, delegate { ShutdownPc(); });
            menu.Items.Add("-");
            menu.Items.Add("Exit NestsOS", null, delegate { Close(); });
            menu.Show(source, new Point(0, -menu.Height));
        }

        private void HandleDesktopKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.R)
            {
                OpenNostRun(string.Empty);
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private void RestartNestOs()
        {
            if (!Confirm("Restart NestOS", "Restart the NestOS shell now?"))
            {
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo(Application.ExecutablePath);
                startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(startInfo);
                Close();
            }
            catch (Exception ex)
            {
                ShowMessage("Restart NestOS", "Could not restart NestOS.\r\n\r\n" + ex.Message);
            }
        }

        private void ShutdownPc()
        {
            if (!Confirm("Shut Down PC", "This will shut down the entire PC immediately. Continue?"))
            {
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo("shutdown.exe", "/s /t 0");
                startInfo.UseShellExecute = false;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ShowMessage("Shut Down PC", "Could not start Windows shutdown.\r\n\r\n" + ex.Message);
            }
        }

        private Point GetSavedDesktopIconLocation(string label, Point fallback)
        {
            Point location;
            if (desktopSettings.IconPositions.TryGetValue(label, out location))
            {
                return location;
            }

            return fallback;
        }

        private void BeginDesktopIconDrag(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            CancelPendingDesktopIconMenu();
            pressedDesktopIcon = sender as Control;
            if (pressedDesktopIcon == null)
            {
                return;
            }

            pressedDesktopIconMouse = e.Location;
            desktopIconDragging = false;
        }

        private void ContinueDesktopIconDrag(object sender, MouseEventArgs e)
        {
            if (pressedDesktopIcon == null || e.Button != MouseButtons.Left)
            {
                return;
            }

            var offsetX = e.X - pressedDesktopIconMouse.X;
            var offsetY = e.Y - pressedDesktopIconMouse.Y;
            if (!desktopIconDragging)
            {
                if (Math.Abs(offsetX) < SystemInformation.DragSize.Width / 2 &&
                    Math.Abs(offsetY) < SystemInformation.DragSize.Height / 2)
                {
                    return;
                }

                desktopIconDragging = true;
            }

            var nextLocation = new Point(pressedDesktopIcon.Left + offsetX, pressedDesktopIcon.Top + offsetY);
            nextLocation = ClampDesktopIconLocation(nextLocation, pressedDesktopIcon.Size);
            pressedDesktopIcon.Location = nextLocation;
            pressedDesktopIcon.BringToFront();
        }

        private void EndDesktopIconDrag(object sender, MouseEventArgs e)
        {
            if (pressedDesktopIcon == null)
            {
                return;
            }

            if (desktopIconDragging)
            {
                suppressedDesktopIconClickTarget = pressedDesktopIcon;
                SaveDesktopIconLocation(pressedDesktopIcon.Text, pressedDesktopIcon.Location);
            }

            pressedDesktopIcon = null;
            desktopIconDragging = false;
        }

        private Point ClampDesktopIconLocation(Point location, Size size)
        {
            var maxX = Math.Max(0, desktopArea.ClientSize.Width - size.Width);
            var maxY = Math.Max(0, desktopArea.ClientSize.Height - size.Height);
            return new Point(Math.Max(0, Math.Min(location.X, maxX)), Math.Max(0, Math.Min(location.Y, maxY)));
        }

        private void SaveDesktopIconLocation(string label, Point location)
        {
            desktopSettings.IconPositions[label] = location;
            DesktopSettingsStore.Save(desktopSettings);
        }

        private void HandleDesktopIconMenuTimerTick(object sender, EventArgs e)
        {
            desktopIconMenuTimer.Stop();
            if (pendingDesktopIcon == null || pendingDesktopIcon.IsDisposed || pendingDesktopIconLabel == null)
            {
                pendingDesktopIcon = null;
                pendingDesktopIconLabel = null;
                return;
            }

            ShowAppOptionsMenu(pendingDesktopIconLabel, pendingDesktopIcon, new Point(pendingDesktopIcon.Width / 2, pendingDesktopIcon.Height));
            pendingDesktopIcon = null;
            pendingDesktopIconLabel = null;
        }

        private void CancelPendingDesktopIconMenu()
        {
            desktopIconMenuTimer.Stop();
            pendingDesktopIcon = null;
            pendingDesktopIconLabel = null;
        }

        private ToolStripMenuItem BuildStartMenuAppItem(ContextMenuStrip ownerMenu, string label)
        {
            var item = new ToolStripMenuItem(label);
            var image = LegacyIconProvider.TryLoadDesktopIcon(label);
            if (image != null)
            {
                item.Image = image;
            }

            item.DropDownItems.Add("Open", null, delegate
            {
                ownerMenu.Close();
                ExecuteAppLaunch(label);
            });

            item.DropDownItems.Add(IsAppPinned(label) ? "Unpin from Start" : "Pin to Start", null, delegate
            {
                ownerMenu.Close();
                SetAppPinned(label, !IsAppPinned(label));
            });

            return item;
        }

        private void ShowAppOptionsMenu(string label, Control source, Point location)
        {
            var menu = new ContextMenuStrip();
            menu.Font = RetroFont.Ui();
            var header = new ToolStripMenuItem(label);
            header.Enabled = false;
            var image = LegacyIconProvider.TryLoadDesktopIcon(label);
            if (image != null)
            {
                header.Image = image;
            }

            menu.Items.Add(header);
            menu.Items.Add("Open", null, delegate { ExecuteAppLaunch(label); });
            menu.Items.Add(IsAppPinned(label) ? "Unpin from Start" : "Pin to Start", null, delegate { SetAppPinned(label, !IsAppPinned(label)); });
            menu.Show(source, location);
        }

        private bool IsAppPinned(string label)
        {
            return desktopSettings.PinnedApps.IndexOf(label) >= 0;
        }

        private void SetAppPinned(string label, bool pinned)
        {
            if (pinned)
            {
                if (!IsAppPinned(label))
                {
                    desktopSettings.PinnedApps.Add(label);
                }
            }
            else
            {
                desktopSettings.PinnedApps.RemoveAll(delegate(string current) { return string.Equals(current, label, StringComparison.OrdinalIgnoreCase); });
            }

            DesktopSettingsStore.Save(desktopSettings);
        }

        private string[] GetStartMenuAppLabels()
        {
            return new[]
            {
                "Paint",
                "Calculator",
                "Home Panel",
                "Media Center",
                "Export",
                "TextPad",
                "File Cabinet",
                "Settings",
                "NestDOS",
                "Nost64/32",
                "Analog Clock",
                "Digital Clock",
                "Solitaire",
                "Minesweeper",
                "Mahjong",
                "Chess",
                "Version"
            };
        }

        private void ExecuteAppLaunch(string label)
        {
            switch (label)
            {
                case "Paint":
                    OpenPaint(null);
                    break;
                case "Calculator":
                    OpenCalculator();
                    break;
                case "Home Panel":
                    OpenHomePanel();
                    break;
                case "Media Center":
                    OpenMediaCenter();
                    break;
                case "Export":
                    OpenExport();
                    break;
                case "TextPad":
                    OpenTextEditor(null);
                    break;
                case "File Cabinet":
                    OpenFileCabinet();
                    break;
                case "Settings":
                    OpenSettings();
                    break;
                case "NestDOS":
                    OpenNestDos();
                    break;
                case "Nost64/32":
                    OpenNostRun(string.Empty);
                    break;
                case "Analog Clock":
                    OpenAnalogClock();
                    break;
                case "Digital Clock":
                    OpenDigitalClock();
                    break;
                case "Solitaire":
                    OpenSolitaire();
                    break;
                case "Minesweeper":
                    OpenMinesweeper();
                    break;
                case "Mahjong":
                    OpenMahjong();
                    break;
                case "Chess":
                    OpenChess();
                    break;
                case "Version":
                    OpenVersionInfo();
                    break;
            }
        }

        private void RefreshWindowThemes()
        {
            RetroWindow activeWindow = null;
            var bestZOrder = int.MaxValue;
            for (var i = 0; i < windows.Count; i++)
            {
                if (!windows[i].Visible)
                {
                    continue;
                }

                var zOrder = desktopArea.Controls.GetChildIndex(windows[i]);
                if (zOrder < bestZOrder)
                {
                    bestZOrder = zOrder;
                    activeWindow = windows[i];
                }
            }

            for (var i = 0; i < windows.Count; i++)
            {
                windows[i].SetActive(windows[i] == activeWindow);
                windows[i].Invalidate(true);
            }
        }
    }
}
