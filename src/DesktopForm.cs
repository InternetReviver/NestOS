using System;
using System.Collections.Generic;
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
        private Control lastDesktopIcon;
        private int lastDesktopIconTick;
        private int windowOffset;

        public DesktopForm()
        {
            windows = new List<RetroWindow>();

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
            desktopArea.MouseDown += delegate { ActivateWindow(null); };

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
            footerLabel.Text = "NestsOS 3.1   SAVE: " + SaveSystem.EnsureRoot();
            footerLabel.TextAlign = ContentAlignment.MiddleRight;
            footerLabel.Font = RetroFont.Ui();
            footerLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            footerLabel.SetBounds(890, 10, 420, 20);

            taskbar.Controls.Add(nestButton);
            taskbar.Controls.Add(taskButtons);
            taskbar.Controls.Add(footerLabel);

            Controls.Add(desktopArea);
            Controls.Add(taskbar);

            AddDesktopIcon("Paint", new Point(24, 24), delegate { OpenPaint(null); });
            AddDesktopIcon("TextPad", new Point(24, 110), delegate { OpenTextEditor(null); });
            AddDesktopIcon("File Cabinet", new Point(24, 196), delegate { OpenFileCabinet(); });
            AddDesktopIcon("NestDOS", new Point(24, 282), delegate { OpenNestDos(); });
            AddDesktopIcon("Solitaire", new Point(24, 368), delegate { OpenSolitaire(); });
            AddDesktopIcon("Mahjong", new Point(24, 454), delegate { OpenMahjong(); });
            AddDesktopIcon("Chess", new Point(24, 540), delegate { OpenChess(); });
            AddDesktopIcon("Analog Clock", new Point(136, 24), delegate { OpenAnalogClock(); });
            AddDesktopIcon("Digital Clock", new Point(136, 110), delegate { OpenDigitalClock(); });

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
        }

        public void OpenTextEditor(string path)
        {
            OpenWindow("TextPad", new Size(640, 420), new TextEditorApp(this, path));
        }

        public void OpenPaint(string path)
        {
            OpenWindow("Paint", new Size(760, 560), new PaintApp(this, path));
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

        public void OpenVersionInfo()
        {
            OpenWindow("Version", new Size(520, 360), new VersionApp());
        }

        public void OpenNostRun(string initialCommand)
        {
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
                case "textpad":
                case "edit":
                case "notepad":
                case "write":
                    OpenTextEditor(null);
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
            button.Location = location;
            button.Image = LegacyIconProvider.TryLoadDesktopIcon(label) ?? BuildDesktopGlyph(label);
            button.Click += delegate(object sender, EventArgs e)
            {
                var now = Environment.TickCount;
                var sameButton = lastDesktopIcon == button;
                var withinWindow = sameButton && unchecked(now - lastDesktopIconTick) <= SystemInformation.DoubleClickTime;

                lastDesktopIcon = button;
                lastDesktopIconTick = now;

                if (withinWindow)
                {
                    lastDesktopIcon = null;
                    lastDesktopIconTick = 0;
                    clickHandler(sender, e);
                }
            };

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
            menu.Items.Add("Paint", null, delegate { OpenPaint(null); });
            menu.Items.Add("TextPad", null, delegate { OpenTextEditor(null); });
            menu.Items.Add("File Cabinet", null, delegate { OpenFileCabinet(); });
            menu.Items.Add("NestDOS", null, delegate { OpenNestDos(); });
            menu.Items.Add("Nost64/32", null, delegate { OpenNostRun(string.Empty); });
            menu.Items.Add("Analog Clock", null, delegate { OpenAnalogClock(); });
            menu.Items.Add("Digital Clock", null, delegate { OpenDigitalClock(); });
            menu.Items.Add("-");
            menu.Items.Add("Solitaire", null, delegate { OpenSolitaire(); });
            menu.Items.Add("Mahjong", null, delegate { OpenMahjong(); });
            menu.Items.Add("Chess", null, delegate { OpenChess(); });
            menu.Items.Add("Version", null, delegate { OpenVersionInfo(); });
            menu.Items.Add("-");
            menu.Items.Add("Open SAVE Folder", null, delegate
            {
                var root = SaveSystem.EnsureRoot();
                if (Directory.Exists(root))
                {
                    System.Diagnostics.Process.Start("explorer.exe", root);
                }
            });
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
    }
}
