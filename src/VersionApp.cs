using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NestsOS
{
    internal class VersionApp : UserControl
    {
        public VersionApp()
        {
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            var title = new Label();
            title.Text = "NestOS Build 112026330350";
            title.Font = RetroFont.UiBold();
            title.AutoSize = false;
            title.SetBounds(12, 12, 300, 20);

            var info = new TextBox();
            info.Multiline = true;
            info.ReadOnly = true;
            info.ScrollBars = ScrollBars.Vertical;
            info.Font = RetroFont.Ui();
            info.SetBounds(12, 40, 488, 268);
            info.Text = BuildInfo();

            Controls.Add(title);
            Controls.Add(info);
        }

        private string BuildInfo()
        {
            var lines = new StringBuilder();
            lines.AppendLine("NestOS Build 112026330350");
            lines.AppendLine();
            lines.AppendLine("Machine: " + Environment.MachineName);
            lines.AppendLine("User: " + Environment.UserName);
            lines.AppendLine("OS Version: " + Environment.OSVersion.VersionString);
            lines.AppendLine("64-bit OS: " + (Environment.Is64BitOperatingSystem ? "Yes" : "No"));
            lines.AppendLine("64-bit Process: " + (Environment.Is64BitProcess ? "Yes" : "No"));
            lines.AppendLine("Processors: " + Environment.ProcessorCount);
            lines.AppendLine(".NET Runtime: " + Environment.Version);
            lines.AppendLine("System Directory: " + Environment.SystemDirectory);
            lines.AppendLine("Current Directory: " + Environment.CurrentDirectory);
            lines.AppendLine("SAVE Root: " + SaveSystem.EnsureRoot());
            lines.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return lines.ToString();
        }
    }
}
