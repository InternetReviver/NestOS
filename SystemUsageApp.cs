using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class SystemUsageApp : UserControl
    {
        private readonly Label cpuLabel;
        private readonly Label gpuLabel;
        private readonly Label saveLabel;
        private readonly Label netLabel;
        private readonly Label ipLabel;
        private readonly Label updateLabel;
        private readonly ProgressBar cpuBar;
        private readonly ProgressBar gpuBar;
        private readonly Timer refreshTimer;
        private PerformanceCounter cpuCounter;
        private List<PerformanceCounter> gpuCounters;

        public SystemUsageApp()
        {
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            var title = new Label();
            title.Text = "System Usage";
            title.Font = RetroFont.UiBold();
            title.AutoSize = false;
            title.SetBounds(12, 12, 160, 18);

            var info = new Label();
            info.Text = "Live system monitor for NestOS.";
            info.AutoSize = false;
            info.SetBounds(12, 34, 220, 18);

            cpuBar = BuildBar(12, 72);
            cpuLabel = BuildValueLabel(12, 96);
            gpuBar = BuildBar(12, 134);
            gpuLabel = BuildValueLabel(12, 158);
            saveLabel = BuildValueLabel(12, 210);
            netLabel = BuildValueLabel(12, 236);
            ipLabel = BuildValueLabel(12, 262);
            updateLabel = BuildValueLabel(12, 294);

            Controls.Add(title);
            Controls.Add(info);
            Controls.Add(BuildCaption("CPU Usage", 12, 56));
            Controls.Add(cpuBar);
            Controls.Add(cpuLabel);
            Controls.Add(BuildCaption("GPU Usage", 12, 118));
            Controls.Add(gpuBar);
            Controls.Add(gpuLabel);
            Controls.Add(BuildCaption("SAVE Folder Size", 12, 192));
            Controls.Add(saveLabel);
            Controls.Add(BuildCaption("Internet", 12, 218));
            Controls.Add(netLabel);
            Controls.Add(BuildCaption("IP Address", 12, 244));
            Controls.Add(ipLabel);
            Controls.Add(updateLabel);

            TryInitializeCounters();

            refreshTimer = new Timer();
            refreshTimer.Interval = 1200;
            refreshTimer.Tick += delegate { RefreshUsage(); };
            refreshTimer.Start();
            RefreshUsage();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (refreshTimer != null)
                {
                    refreshTimer.Stop();
                    refreshTimer.Dispose();
                }

                if (cpuCounter != null)
                {
                    cpuCounter.Dispose();
                }

                if (gpuCounters != null)
                {
                    for (var i = 0; i < gpuCounters.Count; i++)
                    {
                        gpuCounters[i].Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        private Label BuildCaption(string text, int left, int top)
        {
            var label = new Label();
            label.Text = text;
            label.Font = RetroFont.UiBold();
            label.AutoSize = false;
            label.SetBounds(left, top, 160, 18);
            return label;
        }

        private ProgressBar BuildBar(int left, int top)
        {
            var bar = new ProgressBar();
            bar.SetBounds(left, top, 404, 18);
            bar.Minimum = 0;
            bar.Maximum = 100;
            bar.Style = ProgressBarStyle.Continuous;
            return bar;
        }

        private Label BuildValueLabel(int left, int top)
        {
            var label = new Label();
            label.AutoSize = false;
            label.SetBounds(left, top, 404, 18);
            return label;
        }

        private void TryInitializeCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                cpuCounter.NextValue();
            }
            catch
            {
                cpuCounter = null;
            }

            gpuCounters = new List<PerformanceCounter>();
            try
            {
                if (PerformanceCounterCategory.Exists("GPU Engine"))
                {
                    var category = new PerformanceCounterCategory("GPU Engine");
                    var instances = category.GetInstanceNames();
                    for (var i = 0; i < instances.Length; i++)
                    {
                        if (instances[i].IndexOf("engtype_", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instances[i], true);
                        counter.NextValue();
                        gpuCounters.Add(counter);
                    }
                }
            }
            catch
            {
                gpuCounters.Clear();
            }
        }

        private void RefreshUsage()
        {
            var cpuValue = ReadCpuUsage();
            cpuBar.Value = cpuValue;
            cpuLabel.Text = cpuCounter == null ? "CPU usage is unavailable on this PC." : cpuValue + "% total processor load";

            var gpuValue = ReadGpuUsage();
            gpuBar.Value = Math.Max(0, Math.Min(gpuValue, 100));
            gpuLabel.Text = gpuValue < 0 ? "GPU usage is unavailable on this PC." : gpuValue + "% active GPU load";

            saveLabel.Text = SystemUsageSnapshot.FormatBytes(SystemUsageSnapshot.GetSaveFolderSize()) + " in " + SaveSystem.EnsureRoot();
            netLabel.Text = SystemUsageSnapshot.GetInternetStatus();
            ipLabel.Text = SystemUsageSnapshot.GetPrimaryIpv4Address();
            updateLabel.Text = "Updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private int ReadCpuUsage()
        {
            if (cpuCounter == null)
            {
                return 0;
            }

            try
            {
                return ClampPercent(cpuCounter.NextValue());
            }
            catch
            {
                return 0;
            }
        }

        private int ReadGpuUsage()
        {
            if (gpuCounters == null || gpuCounters.Count == 0)
            {
                return -1;
            }

            float total = 0;
            try
            {
                for (var i = 0; i < gpuCounters.Count; i++)
                {
                    total += gpuCounters[i].NextValue();
                }
            }
            catch
            {
                return -1;
            }

            return ClampPercent(total);
        }

        private int ClampPercent(float value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                value = 100;
            }

            return (int)Math.Round(value);
        }
    }
}
