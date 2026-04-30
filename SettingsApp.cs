using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal class SettingsApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly Label backgroundLabel;
        private readonly ComboBox graphicsModeBox;
        private readonly TextBox displayNameBox;
        private readonly CheckBox startupSoundBox;
        private readonly CheckBox shellSoundBox;

        public SettingsApp(IDesktopHost host)
        {
            this.host = host;
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            var title = new Label();
            title.Text = "Desktop Settings";
            title.Font = RetroFont.UiBold();
            title.AutoSize = false;
            title.SetBounds(12, 12, 200, 18);

            var backgroundTitle = new Label();
            backgroundTitle.Text = "Background:";
            backgroundTitle.SetBounds(12, 48, 90, 18);

            backgroundLabel = new Label();
            backgroundLabel.AutoSize = false;
            backgroundLabel.BorderStyle = BorderStyle.FixedSingle;
            backgroundLabel.BackColor = Color.White;
            backgroundLabel.SetBounds(12, 70, 420, 42);

            var importButton = new RetroButton();
            importButton.Text = "Import...";
            importButton.SetBounds(446, 70, 110, 24);
            importButton.Click += delegate { ImportBackground(); };

            var clearButton = new RetroButton();
            clearButton.Text = "Clear";
            clearButton.SetBounds(446, 98, 110, 24);
            clearButton.Click += delegate { ClearBackground(); };

            var graphicsLabel = new Label();
            graphicsLabel.Text = "Graphics Driver:";
            graphicsLabel.SetBounds(12, 136, 96, 18);

            graphicsModeBox = new ComboBox();
            graphicsModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            graphicsModeBox.SetBounds(12, 158, 180, 22);
            graphicsModeBox.Items.AddRange(new object[] { "Classic", "Soft", "Vivid" });

            var displayNameLabel = new Label();
            displayNameLabel.Text = "Display Name:";
            displayNameLabel.SetBounds(220, 136, 82, 18);

            displayNameBox = new TextBox();
            displayNameBox.SetBounds(220, 158, 212, 22);
            displayNameBox.Font = RetroFont.Ui();

            var qolTitle = new Label();
            qolTitle.Text = "Quality of Life:";
            qolTitle.Font = RetroFont.UiBold();
            qolTitle.SetBounds(12, 206, 100, 18);

            startupSoundBox = new CheckBox();
            startupSoundBox.Text = "Play startup sound";
            startupSoundBox.SetBounds(12, 230, 140, 18);
            startupSoundBox.BackColor = Color.Transparent;

            shellSoundBox = new CheckBox();
            shellSoundBox.Text = "Play shell sound effects";
            shellSoundBox.SetBounds(12, 254, 170, 18);
            shellSoundBox.BackColor = Color.Transparent;

            var homePanelButton = new RetroButton();
            homePanelButton.Text = "Open Home Panel";
            homePanelButton.SetBounds(12, 292, 126, 24);
            homePanelButton.Click += delegate { host.OpenHomePanel(); };

            var applyButton = new RetroButton();
            applyButton.Text = "Apply";
            applyButton.SetBounds(446, 156, 110, 24);
            applyButton.Click += delegate { ApplySettings(); };

            var factoryResetTitle = new Label();
            factoryResetTitle.Text = "Factory Reset:";
            factoryResetTitle.Font = RetroFont.UiBold();
            factoryResetTitle.SetBounds(12, 344, 100, 18);

            var factoryResetInfo = new Label();
            factoryResetInfo.Text = "Erase everything in the SAVE folder and restart NestOS.";
            factoryResetInfo.AutoSize = false;
            factoryResetInfo.SetBounds(12, 366, 360, 32);

            var factoryResetButton = new RetroButton();
            factoryResetButton.Text = "Factory Reset";
            factoryResetButton.SetBounds(446, 364, 110, 24);
            factoryResetButton.Click += delegate { host.FactoryResetNestOs(); };

            Controls.Add(title);
            Controls.Add(backgroundTitle);
            Controls.Add(backgroundLabel);
            Controls.Add(importButton);
            Controls.Add(clearButton);
            Controls.Add(graphicsLabel);
            Controls.Add(graphicsModeBox);
            Controls.Add(displayNameLabel);
            Controls.Add(displayNameBox);
            Controls.Add(qolTitle);
            Controls.Add(startupSoundBox);
            Controls.Add(shellSoundBox);
            Controls.Add(homePanelButton);
            Controls.Add(applyButton);
            Controls.Add(factoryResetTitle);
            Controls.Add(factoryResetInfo);
            Controls.Add(factoryResetButton);

            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var settings = host.GetDesktopSettings();
            backgroundLabel.Text = string.IsNullOrEmpty(settings.BackgroundPath) ? "(Default background)" : settings.BackgroundPath;
            graphicsModeBox.SelectedItem = settings.GraphicsMode.ToString();
            if (graphicsModeBox.SelectedIndex < 0)
            {
                graphicsModeBox.SelectedIndex = 0;
            }

            displayNameBox.Text = settings.DisplayName;
            startupSoundBox.Checked = settings.StartupSoundEnabled;
            shellSoundBox.Checked = settings.ShellSoundEffectsEnabled;
        }

        private void ImportBackground()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image Files|*.bmp;*.png;*.jpg;*.jpeg;*.gif";
                dialog.Title = "Import Desktop Background";
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    try
                    {
                        var settings = host.GetDesktopSettings();
                        settings.BackgroundPath = DesktopSettingsStore.ImportBackground(dialog.FileName);
                        host.ApplyDesktopSettings(settings);
                        LoadCurrentSettings();
                    }
                    catch (Exception ex)
                    {
                        host.ShowMessage("Settings", "Could not import background.\r\n\r\n" + ex.Message);
                    }
                }
            }
        }

        private void ClearBackground()
        {
            var settings = host.GetDesktopSettings();
            settings.BackgroundPath = string.Empty;
            host.ApplyDesktopSettings(settings);
            LoadCurrentSettings();
        }

        private void ApplySettings()
        {
            var settings = host.GetDesktopSettings();
            settings.GraphicsMode = (DesktopGraphicsMode)Enum.Parse(typeof(DesktopGraphicsMode), graphicsModeBox.SelectedItem.ToString(), true);
            settings.DisplayName = displayNameBox.Text.Trim().Length == 0 ? Environment.UserName : displayNameBox.Text.Trim();
            settings.StartupSoundEnabled = startupSoundBox.Checked;
            settings.ShellSoundEffectsEnabled = shellSoundBox.Checked;
            host.ApplyDesktopSettings(settings);
            LoadCurrentSettings();
        }
    }
}
