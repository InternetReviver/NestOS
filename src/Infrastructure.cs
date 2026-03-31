using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal static class SaveSystem
    {
        private static string rootPath;

        public static string RootPath
        {
            get
            {
                if (string.IsNullOrEmpty(rootPath))
                {
                    rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SAVE");
                }

                return rootPath;
            }
        }

        public static string EnsureRoot()
        {
            if (!Directory.Exists(RootPath))
            {
                Directory.CreateDirectory(RootPath);
            }

            return RootPath;
        }

        public static string BuildPath(string fileName, string extension)
        {
            EnsureRoot();

            var trimmed = (fileName ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                trimmed = "untitled";
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                trimmed = trimmed.Replace(invalid, '_');
            }

            if (!trimmed.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                trimmed += extension;
            }

            return Path.Combine(RootPath, trimmed);
        }
    }

    internal static class RetroPalette
    {
        public static readonly Color DesktopTeal = Color.FromArgb(0, 128, 128);
        public static readonly Color SetupBlueTop = Color.FromArgb(4, 31, 109);
        public static readonly Color SetupBlueMid = Color.FromArgb(6, 64, 168);
        public static readonly Color SetupBlueBottom = Color.FromArgb(35, 126, 214);
        public static readonly Color WindowBackground = Color.FromArgb(192, 192, 192);
        public static readonly Color ButtonFace = Color.FromArgb(192, 192, 192);
        public static readonly Color Shadow = Color.FromArgb(128, 128, 128);
        public static readonly Color DarkShadow = Color.Black;
        public static readonly Color Highlight = Color.White;
        public static readonly Color TitleBlue = Color.FromArgb(0, 0, 128);
        public static readonly Color TitleInactive = Color.FromArgb(96, 96, 96);
        public static readonly Color Accent = Color.FromArgb(0, 0, 168);
        public static readonly Color FeltGreen = Color.FromArgb(0, 102, 0);
    }

    internal static class RetroFont
    {
        private static readonly string UiFamilyName = ResolveUiFamily();

        public static Font Ui()
        {
            return Create(8.0f, FontStyle.Regular);
        }

        public static Font UiBold()
        {
            return Create(8.0f, FontStyle.Bold);
        }

        public static Font Title()
        {
            return Create(8.0f, FontStyle.Bold);
        }

        public static Font Banner()
        {
            return Create(24.0f, FontStyle.Bold);
        }

        public static Font Dos()
        {
            return new Font("Consolas", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font Create(float size, FontStyle style)
        {
            return new Font(UiFamilyName, size, style, GraphicsUnit.Point);
        }

        private static string ResolveUiFamily()
        {
            var preferred = new[] { "MS Sans Serif", "Microsoft Sans Serif", "Small Fonts" };
            using (var fonts = new System.Drawing.Text.InstalledFontCollection())
            {
                for (var i = 0; i < preferred.Length; i++)
                {
                    for (var j = 0; j < fonts.Families.Length; j++)
                    {
                        if (string.Equals(fonts.Families[j].Name, preferred[i], StringComparison.OrdinalIgnoreCase))
                        {
                            return fonts.Families[j].Name;
                        }
                    }
                }
            }

            return "Microsoft Sans Serif";
        }
    }

    internal interface IDesktopHost
    {
        void OpenTextEditor(string path);
        void OpenPaint(string path);
        void OpenFileCabinet();
        void OpenNestDos();
        void OpenSolitaire();
        void OpenMahjong();
        void OpenChess();
        void OpenAnalogClock();
        void OpenDigitalClock();
        void OpenVersionInfo();
        void OpenNostRun(string initialCommand);
        bool RunShellCommand(string command);
        void ShowMessage(string title, string message);
        bool Confirm(string title, string message);
    }

    internal sealed class DesktopBackdropPanel : Panel
    {
        public DesktopBackdropPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                base.OnPaintBackground(e);
                return;
            }

            using (var brush = new LinearGradientBrush(bounds, RetroPalette.SetupBlueTop, RetroPalette.SetupBlueBottom, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend();
                blend.Positions = new float[] { 0.0f, 0.42f, 1.0f };
                blend.Colors = new Color[] { RetroPalette.SetupBlueTop, RetroPalette.SetupBlueMid, RetroPalette.SetupBlueBottom };
                brush.InterpolationColors = blend;
                e.Graphics.FillRectangle(brush, bounds);
            }

            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(-bounds.Width / 8, -bounds.Height / 3, bounds.Width, bounds.Height);
                using (var pathBrush = new PathGradientBrush(glow))
                {
                    pathBrush.CenterColor = Color.FromArgb(78, 122, 188, 255);
                    pathBrush.SurroundColors = new Color[] { Color.FromArgb(0, 122, 188, 255) };
                    e.Graphics.FillRectangle(pathBrush, bounds);
                }
            }

            var bandHeight = Math.Max(170, bounds.Height / 3);
            var bandTop = Math.Max(bounds.Height / 2, bounds.Height - bandHeight);
            var bandRect = new Rectangle(0, bandTop, bounds.Width, bounds.Height - bandTop);
            using (var bandBrush = new LinearGradientBrush(bandRect, Color.FromArgb(16, 255, 255, 255), Color.FromArgb(108, 196, 228, 255), LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(bandBrush, bandRect);
            }

            using (var pen = new Pen(Color.FromArgb(48, 255, 255, 255)))
            {
                for (var y = bandRect.Top + 2; y < bandRect.Bottom; y += 7)
                {
                    e.Graphics.DrawLine(pen, 0, y, bounds.Width, y);
                }
            }
        }
    }

    internal static class LegacyIconProvider
    {
        private static string iconsRoot;

        private static readonly Dictionary<string, string> LocalIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Paint", "PBRUS001.PNG" },
            { "TextPad", "WRITE001.PNG" },
            { "File Cabinet", "WINFI001.PNG" },
            { "NestDOS", "TERMI001.PNG" },
            { "Solitaire", "SOL001.PNG" },
            { "Mahjong", "CARDF001.PNG" },
            { "Chess", "PROGM015.PNG" },
            { "Analog Clock", "CLOCK001.PNG" },
            { "Digital Clock", "CLOCK001.PNG" },
            { "Version", "WINVE001.PNG" }
        };

        public static Image TryLoadDesktopIcon(string label)
        {
            string fileName;
            if (!LocalIconMap.TryGetValue(label, out fileName))
            {
                return null;
            }

            var iconsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ICONS");
            var path = Path.Combine(ResolveIconsRoot(), fileName);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using (var source = Image.FromFile(path))
                using (var bitmap = new Bitmap(source))
                {
                    return new Bitmap(bitmap, new Size(32, 32));
                }
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveIconsRoot()
        {
            if (!string.IsNullOrEmpty(iconsRoot))
            {
                return iconsRoot;
            }

            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ICONS"),
                Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "ICONS"),
                Path.Combine(Environment.CurrentDirectory, "ICONS"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "NestsOS", "ICONS")
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                try
                {
                    if (Directory.Exists(candidates[i]))
                    {
                        iconsRoot = candidates[i];
                        return iconsRoot;
                    }
                }
                catch
                {
                }
            }

            iconsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ICONS");
            return iconsRoot;
        }
    }

    internal static class RetroStyle
    {
        public static void ApplyInsetBorder(PaintEventArgs e, Rectangle bounds)
        {
            ControlPaint.DrawBorder(
                e.Graphics,
                bounds,
                RetroPalette.DarkShadow,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.Highlight,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.Highlight,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                1,
                ButtonBorderStyle.Solid);
        }

        public static void ApplyRaisedBorder(PaintEventArgs e, Rectangle bounds)
        {
            ControlPaint.DrawBorder(
                e.Graphics,
                bounds,
                RetroPalette.Highlight,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.Highlight,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                1,
                ButtonBorderStyle.Solid);
        }

        public static void ApplySunkenBorder(Graphics graphics, Rectangle bounds)
        {
            ControlPaint.DrawBorder(
                graphics,
                bounds,
                RetroPalette.DarkShadow,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.Highlight,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.Highlight,
                1,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                1,
                ButtonBorderStyle.Solid);
        }
    }

    internal class RetroDialogForm : Form
    {
        private readonly Panel titleBar;
        private readonly Label titleLabel;
        private readonly RetroWindowButton closeButton;
        private readonly Panel bodyPanel;
        private bool dragging;
        private Point dragOffset;

        public RetroDialogForm(string title, Size clientSize)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = RetroPalette.WindowBackground;
            Font = RetroFont.Ui();
            ClientSize = clientSize;
            Padding = new Padding(3, 23, 3, 3);

            titleBar = new Panel();
            titleBar.BackColor = RetroPalette.TitleBlue;
            titleBar.SetBounds(3, 3, clientSize.Width - 28, 18);
            titleBar.MouseDown += BeginDrag;
            titleBar.MouseMove += ContinueDrag;
            titleBar.MouseUp += EndDrag;

            titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.ForeColor = Color.White;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.Font = RetroFont.Title();
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.SetBounds(4, 1, clientSize.Width - 40, 16);
            titleLabel.MouseDown += BeginDrag;
            titleLabel.MouseMove += ContinueDrag;
            titleLabel.MouseUp += EndDrag;

            closeButton = new RetroWindowButton();
            closeButton.Text = "X";
            closeButton.Location = new Point(clientSize.Width - 21, 3);
            closeButton.ButtonPressed += delegate { DialogResult = DialogResult.Cancel; Close(); };

            bodyPanel = new Panel();
            bodyPanel.SetBounds(6, 28, clientSize.Width - 12, clientSize.Height - 34);
            bodyPanel.BackColor = RetroPalette.WindowBackground;
            bodyPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            titleBar.Controls.Add(titleLabel);
            Controls.Add(titleBar);
            Controls.Add(closeButton);
            Controls.Add(bodyPanel);
        }

        public Panel BodyPanel
        {
            get { return bodyPanel; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            ControlPaint.DrawBorder(
                e.Graphics,
                new Rectangle(0, 0, Width - 1, Height - 1),
                RetroPalette.Highlight,
                2,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                2,
                ButtonBorderStyle.Solid,
                RetroPalette.Highlight,
                2,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                2,
                ButtonBorderStyle.Solid);

            base.OnPaint(e);
        }

        private void BeginDrag(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragOffset = e.Location;
        }

        private void ContinueDrag(object sender, MouseEventArgs e)
        {
            if (!dragging)
            {
                return;
            }

            var screen = PointToScreen(e.Location);
            Location = new Point(screen.X - dragOffset.X - 3, screen.Y - dragOffset.Y - 3);
        }

        private void EndDrag(object sender, MouseEventArgs e)
        {
            dragging = false;
        }
    }

    internal static class PromptDialog
    {
        public static string ShowDialog(IWin32Window owner, string title, string message, string value)
        {
            using (var dialog = new RetroDialogForm(title, new Size(332, 132)))
            using (var label = new Label())
            using (var input = new TextBox())
            using (var ok = new RetroButton())
            using (var cancel = new RetroButton())
            {
                label.Text = message;
                label.AutoSize = false;
                label.SetBounds(8, 8, 304, 18);

                input.Text = value ?? string.Empty;
                input.Font = RetroFont.Ui();
                input.SetBounds(8, 34, 304, 22);

                ok.Text = "OK";
                ok.SetBounds(160, 72, 72, 24);
                ok.Click += delegate { dialog.DialogResult = DialogResult.OK; dialog.Close(); };

                cancel.Text = "Cancel";
                cancel.SetBounds(240, 72, 72, 24);
                cancel.Click += delegate { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); };

                dialog.BodyPanel.Controls.Add(label);
                dialog.BodyPanel.Controls.Add(input);
                dialog.BodyPanel.Controls.Add(ok);
                dialog.BodyPanel.Controls.Add(cancel);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;

                return dialog.ShowDialog(owner) == DialogResult.OK ? input.Text : null;
            }
        }
    }

    internal static class RetroDialogs
    {
        public static void ShowMessage(IWin32Window owner, string title, string message)
        {
            using (var dialog = new RetroDialogForm(title, new Size(360, 158)))
            using (var iconPanel = new Panel())
            using (var label = new Label())
            using (var ok = new RetroButton())
            {
                iconPanel.SetBounds(12, 18, 34, 34);
                iconPanel.Paint += delegate(object sender, PaintEventArgs e)
                {
                    e.Graphics.FillEllipse(Brushes.RoyalBlue, 1, 1, 30, 30);
                    TextRenderer.DrawText(e.Graphics, "i", RetroFont.Create(16.0f, FontStyle.Bold), new Rectangle(0, 0, 32, 32), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };

                label.Text = message;
                label.AutoSize = false;
                label.SetBounds(58, 14, 282, 60);

                ok.Text = "OK";
                ok.SetBounds(268, 88, 72, 24);
                ok.Click += delegate { dialog.DialogResult = DialogResult.OK; dialog.Close(); };

                dialog.BodyPanel.Controls.Add(iconPanel);
                dialog.BodyPanel.Controls.Add(label);
                dialog.BodyPanel.Controls.Add(ok);
                dialog.AcceptButton = ok;
                dialog.ShowDialog(owner);
            }
        }

        public static bool Confirm(IWin32Window owner, string title, string message)
        {
            using (var dialog = new RetroDialogForm(title, new Size(356, 144)))
            using (var label = new Label())
            using (var yes = new RetroButton())
            using (var no = new RetroButton())
            {
                label.Text = message;
                label.AutoSize = false;
                label.SetBounds(12, 18, 320, 42);

                yes.Text = "Yes";
                yes.SetBounds(184, 74, 72, 24);
                yes.Click += delegate { dialog.DialogResult = DialogResult.Yes; dialog.Close(); };

                no.Text = "No";
                no.SetBounds(264, 74, 72, 24);
                no.Click += delegate { dialog.DialogResult = DialogResult.No; dialog.Close(); };

                dialog.BodyPanel.Controls.Add(label);
                dialog.BodyPanel.Controls.Add(yes);
                dialog.BodyPanel.Controls.Add(no);
                dialog.AcceptButton = yes;
                dialog.CancelButton = no;
                return dialog.ShowDialog(owner) == DialogResult.Yes;
            }
        }
    }
}
