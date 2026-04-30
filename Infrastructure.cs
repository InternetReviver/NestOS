using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NestsOS
{
    internal static class AppInfo
    {
        public const string BuildLabel = "NestOS build 4.0.2026.4.29";
    }

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

    internal static class NestExportCodec
    {
        private static readonly byte[] TextMagic = Encoding.ASCII.GetBytes("NESTCOMW1");
        private static readonly byte[] BitmapMagic = Encoding.ASCII.GetBytes("NESTCOMU1");
        private static readonly byte[] TransformKey = Encoding.ASCII.GetBytes("NestOS");

        public static bool IsNestTextPath(string path)
        {
            return string.Equals(Path.GetExtension(path), ".comw", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsNestBitmapPath(string path)
        {
            return string.Equals(Path.GetExtension(path), ".comu", StringComparison.OrdinalIgnoreCase);
        }

        public static string ReadTextFile(string path)
        {
            if (!IsNestTextPath(path))
            {
                return File.ReadAllText(path);
            }

            return Encoding.UTF8.GetString(ReadEncodedFile(path, TextMagic));
        }

        public static void WriteTextFile(string path, string text)
        {
            if (!IsNestTextPath(path))
            {
                File.WriteAllText(path, text ?? string.Empty);
                return;
            }

            WriteEncodedFile(path, TextMagic, Encoding.UTF8.GetBytes(text ?? string.Empty));
        }

        public static void ExportTextFile(string sourcePath, string destinationPath)
        {
            WriteTextFile(destinationPath, File.ReadAllText(sourcePath));
        }

        public static Bitmap ReadBitmapFile(string path)
        {
            if (!IsNestBitmapPath(path))
            {
                return new Bitmap(path);
            }

            var bytes = ReadEncodedFile(path, BitmapMagic);
            using (var stream = new MemoryStream(bytes))
            using (var image = Image.FromStream(stream))
            {
                return new Bitmap(image);
            }
        }

        public static void WriteBitmapFile(string path, Bitmap bitmap)
        {
            if (!IsNestBitmapPath(path))
            {
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
                return;
            }

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                WriteEncodedFile(path, BitmapMagic, stream.ToArray());
            }
        }

        public static void ExportBitmapFile(string sourcePath, string destinationPath)
        {
            WriteEncodedFile(destinationPath, BitmapMagic, File.ReadAllBytes(sourcePath));
        }

        private static void WriteEncodedFile(string path, byte[] magic, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(magic);
                writer.Write(bytes.Length);
                writer.Write(Transform(bytes));
            }
        }

        private static byte[] ReadEncodedFile(string path, byte[] expectedMagic)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                var actualMagic = reader.ReadBytes(expectedMagic.Length);
                if (actualMagic.Length != expectedMagic.Length || !AreEqual(actualMagic, expectedMagic))
                {
                    throw new InvalidDataException("This file is not a valid NestOS export.");
                }

                var length = reader.ReadInt32();
                if (length < 0)
                {
                    throw new InvalidDataException("This file has an invalid NestOS payload.");
                }

                var encoded = reader.ReadBytes(length);
                if (encoded.Length != length)
                {
                    throw new InvalidDataException("This file is incomplete.");
                }

                return Transform(encoded);
            }
        }

        private static byte[] Transform(byte[] input)
        {
            var output = new byte[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                output[i] = (byte)(input[i] ^ TransformKey[i % TransformKey.Length] ^ (i * 17));
            }

            return output;
        }

        private static bool AreEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
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

    internal enum DesktopGraphicsMode
    {
        Classic,
        Soft,
        Vivid
    }

    internal sealed class DesktopSettingsData
    {
        public DesktopGraphicsMode GraphicsMode;
        public string BackgroundPath;
        public Dictionary<string, Point> IconPositions;
        public List<string> PinnedApps;
        public string DisplayName;
        public string ComputerAlias;
        public Color ActiveTitleColor;
        public Color InactiveTitleColor;
        public bool StartupSoundEnabled;
        public bool ShellSoundEffectsEnabled;

        public DesktopSettingsData()
        {
            IconPositions = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
            PinnedApps = new List<string>();
            DisplayName = Environment.UserName;
            ComputerAlias = Environment.MachineName;
            ActiveTitleColor = RetroPalette.TitleBlue;
            InactiveTitleColor = RetroPalette.TitleInactive;
            StartupSoundEnabled = true;
            ShellSoundEffectsEnabled = true;
        }

        public DesktopSettingsData Clone()
        {
            var copy = new DesktopSettingsData();
            copy.GraphicsMode = GraphicsMode;
            copy.BackgroundPath = BackgroundPath;
            copy.DisplayName = DisplayName;
            copy.ComputerAlias = ComputerAlias;
            copy.ActiveTitleColor = ActiveTitleColor;
            copy.InactiveTitleColor = InactiveTitleColor;
            copy.StartupSoundEnabled = StartupSoundEnabled;
            copy.ShellSoundEffectsEnabled = ShellSoundEffectsEnabled;

            foreach (var pair in IconPositions)
            {
                copy.IconPositions[pair.Key] = pair.Value;
            }

            for (var i = 0; i < PinnedApps.Count; i++)
            {
                copy.PinnedApps.Add(PinnedApps[i]);
            }

            return copy;
        }
    }

    internal static class DesktopSettingsStore
    {
        private static readonly string SettingsPath = Path.Combine(SaveSystem.RootPath, "desktop.settings");

        public static DesktopSettingsData Load()
        {
            var data = new DesktopSettingsData();
            data.GraphicsMode = DesktopGraphicsMode.Classic;
            data.BackgroundPath = string.Empty;

            if (!File.Exists(SettingsPath))
            {
                return data;
            }

            var lines = File.ReadAllLines(SettingsPath);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                if (key.Equals("GraphicsMode", StringComparison.OrdinalIgnoreCase))
                {
                    DesktopGraphicsMode mode;
                    if (Enum.TryParse(value, true, out mode))
                    {
                        data.GraphicsMode = mode;
                    }
                }
                else if (key.Equals("BackgroundPath", StringComparison.OrdinalIgnoreCase))
                {
                    data.BackgroundPath = value;
                }
                else if (key.Equals("DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    data.DisplayName = value.Length == 0 ? Environment.UserName : value;
                }
                else if (key.Equals("ComputerAlias", StringComparison.OrdinalIgnoreCase))
                {
                    data.ComputerAlias = value.Length == 0 ? Environment.MachineName : value;
                }
                else if (key.Equals("ActiveTitleColor", StringComparison.OrdinalIgnoreCase))
                {
                    data.ActiveTitleColor = ParseColor(value, RetroPalette.TitleBlue);
                }
                else if (key.Equals("InactiveTitleColor", StringComparison.OrdinalIgnoreCase))
                {
                    data.InactiveTitleColor = ParseColor(value, RetroPalette.TitleInactive);
                }
                else if (key.Equals("StartupSoundEnabled", StringComparison.OrdinalIgnoreCase))
                {
                    bool enabled;
                    if (bool.TryParse(value, out enabled))
                    {
                        data.StartupSoundEnabled = enabled;
                    }
                }
                else if (key.Equals("ShellSoundEffectsEnabled", StringComparison.OrdinalIgnoreCase))
                {
                    bool enabled;
                    if (bool.TryParse(value, out enabled))
                    {
                        data.ShellSoundEffectsEnabled = enabled;
                    }
                }
                else if (key.StartsWith("Icon.", StringComparison.OrdinalIgnoreCase))
                {
                    var iconName = key.Substring(5).Trim();
                    Point location;
                    if (TryParsePoint(value, out location) && iconName.Length > 0)
                    {
                        data.IconPositions[iconName] = location;
                    }
                }
                else if (key.Equals("PinnedApp", StringComparison.OrdinalIgnoreCase))
                {
                    if (value.Length > 0 && data.PinnedApps.IndexOf(value) < 0)
                    {
                        data.PinnedApps.Add(value);
                    }
                }
            }

            return data;
        }

        public static void Save(DesktopSettingsData data)
        {
            SaveSystem.EnsureRoot();
            var lines = new List<string>();
            lines.Add("GraphicsMode=" + data.GraphicsMode);
            lines.Add("BackgroundPath=" + (data.BackgroundPath ?? string.Empty));
            lines.Add("DisplayName=" + (data.DisplayName ?? string.Empty));
            lines.Add("ComputerAlias=" + (data.ComputerAlias ?? string.Empty));
            lines.Add("ActiveTitleColor=" + FormatColor(data.ActiveTitleColor));
            lines.Add("InactiveTitleColor=" + FormatColor(data.InactiveTitleColor));
            lines.Add("StartupSoundEnabled=" + data.StartupSoundEnabled);
            lines.Add("ShellSoundEffectsEnabled=" + data.ShellSoundEffectsEnabled);

            foreach (var pair in data.IconPositions)
            {
                lines.Add("Icon." + pair.Key + "=" + pair.Value.X + "," + pair.Value.Y);
            }

            for (var i = 0; i < data.PinnedApps.Count; i++)
            {
                lines.Add("PinnedApp=" + data.PinnedApps[i]);
            }

            File.WriteAllLines(SettingsPath, lines);
        }

        public static string ImportBackground(string sourcePath)
        {
            SaveSystem.EnsureRoot();
            var extension = Path.GetExtension(sourcePath);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".bmp";
            }

            var destination = Path.Combine(SaveSystem.RootPath, "background" + extension.ToLowerInvariant());
            File.Copy(sourcePath, destination, true);
            return destination;
        }

        public static void FactoryReset()
        {
            var root = SaveSystem.EnsureRoot();
            var directory = new DirectoryInfo(root);

            var files = directory.GetFiles();
            for (var i = 0; i < files.Length; i++)
            {
                files[i].IsReadOnly = false;
                files[i].Delete();
            }

            var directories = directory.GetDirectories();
            for (var i = 0; i < directories.Length; i++)
            {
                directories[i].Delete(true);
            }
        }

        private static bool TryParsePoint(string value, out Point point)
        {
            point = Point.Empty;
            var parts = (value ?? string.Empty).Split(',');
            if (parts.Length != 2)
            {
                return false;
            }

            int x;
            int y;
            if (!int.TryParse(parts[0].Trim(), out x) || !int.TryParse(parts[1].Trim(), out y))
            {
                return false;
            }

            point = new Point(x, y);
            return true;
        }

        private static string FormatColor(Color color)
        {
            return color.R + "," + color.G + "," + color.B;
        }

        private static Color ParseColor(string value, Color fallback)
        {
            var parts = (value ?? string.Empty).Split(',');
            if (parts.Length != 3)
            {
                return fallback;
            }

            int red;
            int green;
            int blue;
            if (!int.TryParse(parts[0].Trim(), out red) ||
                !int.TryParse(parts[1].Trim(), out green) ||
                !int.TryParse(parts[2].Trim(), out blue))
            {
                return fallback;
            }

            red = Math.Max(0, Math.Min(255, red));
            green = Math.Max(0, Math.Min(255, green));
            blue = Math.Max(0, Math.Min(255, blue));
            return Color.FromArgb(red, green, blue);
        }
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
        void OpenCalculator();
        void OpenExport();
        void OpenHomePanel();
        void OpenMediaCenter();
        void OpenSettings();
        void OpenFileCabinet();
        void OpenNestDos();
        void OpenSolitaire();
        void OpenMinesweeper();
        void OpenMahjong();
        void OpenChess();
        void OpenAnalogClock();
        void OpenDigitalClock();
        void OpenSystemUsage();
        void OpenVersionInfo();
        void OpenNostRun(string initialCommand);
        bool RunShellCommand(string command);
        void ShowMessage(string title, string message);
        bool Confirm(string title, string message);
        DesktopSettingsData GetDesktopSettings();
        void ApplyDesktopSettings(DesktopSettingsData settings);
        void FactoryResetNestOs();
    }

    internal sealed class DesktopBackdropPanel : Panel
    {
        private Image customBackground;

        public DesktopBackdropPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            GraphicsMode = DesktopGraphicsMode.Classic;
        }

        public DesktopGraphicsMode GraphicsMode { get; set; }

        public void ApplySettings(DesktopSettingsData settings)
        {
            GraphicsMode = settings.GraphicsMode;

            if (customBackground != null)
            {
                customBackground.Dispose();
                customBackground = null;
            }

            if (!string.IsNullOrEmpty(settings.BackgroundPath) && File.Exists(settings.BackgroundPath))
            {
                using (var source = Image.FromFile(settings.BackgroundPath))
                {
                    customBackground = new Bitmap(source);
                }
            }

            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                base.OnPaintBackground(e);
                return;
            }

            if (customBackground != null)
            {
                if (GraphicsMode == DesktopGraphicsMode.Classic)
                {
                    e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                }
                else
                {
                    e.Graphics.InterpolationMode = GraphicsMode == DesktopGraphicsMode.Vivid ? InterpolationMode.HighQualityBicubic : InterpolationMode.Bilinear;
                }

                e.Graphics.DrawImage(customBackground, bounds);
                return;
            }

            var top = RetroPalette.SetupBlueTop;
            var mid = RetroPalette.SetupBlueMid;
            var bottom = RetroPalette.SetupBlueBottom;
            if (GraphicsMode == DesktopGraphicsMode.Soft)
            {
                top = Color.FromArgb(18, 46, 124);
                mid = Color.FromArgb(34, 82, 175);
                bottom = Color.FromArgb(74, 145, 219);
            }
            else if (GraphicsMode == DesktopGraphicsMode.Vivid)
            {
                top = Color.FromArgb(0, 25, 120);
                mid = Color.FromArgb(0, 77, 200);
                bottom = Color.FromArgb(46, 150, 255);
            }

            using (var brush = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend();
                blend.Positions = new float[] { 0.0f, 0.42f, 1.0f };
                blend.Colors = new Color[] { top, mid, bottom };
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
            { "Calculator", "CALC001.PNG" },
            { "Export", "PACKA001.PNG" },
            { "Home Panel", "WINHE001.PNG" },
            { "Media Center", "MPLAY001.PNG" },
            { "TextPad", "WRITE001.PNG" },
            { "File Cabinet", "WINFI001.PNG" },
            { "NestDOS", "TERMI001.PNG" },
            { "Solitaire", "SOL001.PNG" },
            { "Minesweeper", "SCHDP004.PNG" },
            { "Mahjong", "SOL001.PNG" },
            { "Chess", "SOL001.PNG" },
            { "Analog Clock", "CLOCK001.PNG" },
            { "Digital Clock", "CLOCK001.PNG" },
            { "Settings", "CONTR001.PNG" },
            { "Version", "WINVE001.PNG" }
        };

        public static Image TryLoadDesktopIcon(string label)
        {
            string fileName;
            if (!LocalIconMap.TryGetValue(label, out fileName))
            {
                return null;
            }

            return TryLoadIconByFileName(fileName, new Size(32, 32));
        }

        public static Image TryLoadIconByFileName(string fileName, Size size)
        {
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
                    var scaled = new Bitmap(size.Width, size.Height);
                    using (var graphics = Graphics.FromImage(scaled))
                    {
                        graphics.Clear(Color.Transparent);
                        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                        graphics.PixelOffsetMode = PixelOffsetMode.Half;

                        var scale = Math.Min((float)size.Width / bitmap.Width, (float)size.Height / bitmap.Height);
                        var targetWidth = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
                        var targetHeight = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
                        var targetX = (size.Width - targetWidth) / 2;
                        var targetY = (size.Height - targetHeight) / 2;
                        graphics.DrawImage(bitmap, new Rectangle(targetX, targetY, targetWidth, targetHeight));
                    }

                    return scaled;
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

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parentDirectory = Directory.GetParent(baseDirectory);
            var candidates = new[]
            {
                @"D:\NestsOS\ICONS",
                Path.Combine(baseDirectory, "ICONS"),
                parentDirectory == null ? string.Empty : Path.Combine(parentDirectory.FullName, "ICONS"),
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

    internal static class WindowTheme
    {
        public static Color ActiveTitleColor = RetroPalette.TitleBlue;
        public static Color InactiveTitleColor = RetroPalette.TitleInactive;

        public static void Apply(DesktopSettingsData settings)
        {
            ActiveTitleColor = settings.ActiveTitleColor;
            InactiveTitleColor = settings.InactiveTitleColor;
        }
    }

    internal static class SoundPreferences
    {
        public static bool StartupSoundEnabled = true;
        public static bool ShellSoundEffectsEnabled = true;

        public static void Apply(DesktopSettingsData settings)
        {
            StartupSoundEnabled = settings.StartupSoundEnabled;
            ShellSoundEffectsEnabled = settings.ShellSoundEffectsEnabled;
        }
    }

    internal static class StartupAudio
    {
        private static string activeAlias;

        public static void PlayWin31(bool force)
        {
            if (!force && !SoundPreferences.StartupSoundEnabled)
            {
                return;
            }

            var path = ResolveSoundPath("win31.mp3");
            if (path == null)
            {
                return;
            }

            Play(path, "nests_startup");
        }

        public static void PlayChimes(bool force)
        {
            if (!force && !SoundPreferences.ShellSoundEffectsEnabled)
            {
                return;
            }

            var path = ResolveSoundPath("chimes.mp3");
            if (path == null)
            {
                return;
            }

            Play(path, "nests_chimes");
        }

        private static void Play(string path, string alias)
        {
            try
            {
                Stop();
                activeAlias = alias;
                mciSendString("open \"" + path + "\" type mpegvideo alias " + activeAlias, null, 0, IntPtr.Zero);
                mciSendString("play " + activeAlias + " from 0", null, 0, IntPtr.Zero);
            }
            catch
            {
                activeAlias = null;
            }
        }

        public static void Stop()
        {
            if (string.IsNullOrEmpty(activeAlias))
            {
                return;
            }

            try
            {
                mciSendString("stop " + activeAlias, null, 0, IntPtr.Zero);
                mciSendString("close " + activeAlias, null, 0, IntPtr.Zero);
            }
            catch
            {
            }
            finally
            {
                activeAlias = null;
            }
        }

        private static string ResolveSoundPath(string fileName)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parentDirectory = Directory.GetParent(baseDirectory);
            var candidates = new[]
            {
                Path.Combine(baseDirectory, fileName),
                parentDirectory == null ? string.Empty : Path.Combine(parentDirectory.FullName, "BUILD", fileName),
                parentDirectory == null ? string.Empty : Path.Combine(parentDirectory.FullName, "build", fileName)
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                if (!string.IsNullOrEmpty(candidates[i]) && File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return null;
        }

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string command, System.Text.StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
    }

    internal static class MediaPlayback
    {
        private const string Alias = "nests_media";
        private static bool open;

        public static void Play(string path)
        {
            Stop();
            mciSendString("open \"" + path + "\" alias " + Alias, null, 0, IntPtr.Zero);
            mciSendString("play " + Alias + " from 0", null, 0, IntPtr.Zero);
            open = true;
        }

        public static void Stop()
        {
            if (!open)
            {
                return;
            }

            try
            {
                mciSendString("stop " + Alias, null, 0, IntPtr.Zero);
                mciSendString("close " + Alias, null, 0, IntPtr.Zero);
            }
            catch
            {
            }
            finally
            {
                open = false;
            }
        }

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
    }

    internal static class SystemUsageSnapshot
    {
        public static string GetPrimaryIpv4Address()
        {
            try
            {
                var host = Dns.GetHostAddresses(Dns.GetHostName());
                for (var i = 0; i < host.Length; i++)
                {
                    if (host[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(host[i]))
                    {
                        return host[i].ToString();
                    }
                }
            }
            catch
            {
            }

            return "Unavailable";
        }

        public static string GetInternetStatus()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    var current = interfaces[i];
                    if (current.OperationalStatus == OperationalStatus.Up &&
                        current.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        current.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        return "Connected via " + current.Name;
                    }
                }
            }
            catch
            {
            }

            return NetworkInterface.GetIsNetworkAvailable() ? "Connected" : "Offline";
        }

        public static long GetSaveFolderSize()
        {
            return GetDirectorySize(new DirectoryInfo(SaveSystem.EnsureRoot()));
        }

        private static long GetDirectorySize(DirectoryInfo directory)
        {
            long total = 0;
            FileInfo[] files;
            DirectoryInfo[] directories;

            try
            {
                files = directory.GetFiles();
            }
            catch
            {
                return total;
            }

            for (var i = 0; i < files.Length; i++)
            {
                total += files[i].Length;
            }

            try
            {
                directories = directory.GetDirectories();
            }
            catch
            {
                return total;
            }

            for (var i = 0; i < directories.Length; i++)
            {
                total += GetDirectorySize(directories[i]);
            }

            return total;
        }

        public static string FormatBytes(long bytes)
        {
            var size = (double)bytes;
            var suffixes = new[] { "B", "KB", "MB", "GB" };
            var suffixIndex = 0;
            while (size >= 1024.0 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024.0;
                suffixIndex++;
            }

            return size.ToString(size >= 10 ? "0.0" : "0.00") + " " + suffixes[suffixIndex];
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
            titleBar.BackColor = WindowTheme.ActiveTitleColor;
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
