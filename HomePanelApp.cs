using System;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class HomePanelApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly Label welcomeLabel;
        private readonly TextBox displayNameBox;
        private readonly TextBox computerBox;
        private readonly ComboBox graphicsModeBox;
        private readonly Panel activeTitleSwatch;
        private readonly Panel inactiveTitleSwatch;
        private readonly CheckBox startupSoundBox;
        private readonly CheckBox shellSoundBox;

        public HomePanelApp(IDesktopHost host)
        {
            this.host = host;
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            welcomeLabel = new Label();
            welcomeLabel.Font = RetroFont.UiBold();
            welcomeLabel.AutoSize = false;
            welcomeLabel.SetBounds(16, 14, 560, 18);

            var subtitle = new Label();
            subtitle.Text = "Quick access for profile, display, audio, and shell tools.";
            subtitle.AutoSize = false;
            subtitle.SetBounds(16, 36, 420, 18);

            var profileBox = BuildGroup("Profile", 16, 64, 300, 112);
            var displayNameLabel = new Label();
            displayNameLabel.Text = "Display Name:";
            displayNameLabel.SetBounds(14, 20, 74, 18);
            displayNameBox = new TextBox();
            displayNameBox.SetBounds(104, 18, 180, 22);
            displayNameBox.Font = RetroFont.Ui();

            var computerLabel = new Label();
            computerLabel.Text = "Computer:";
            computerLabel.SetBounds(14, 52, 74, 18);
            computerBox = new TextBox();
            computerBox.SetBounds(104, 50, 180, 22);
            computerBox.Font = RetroFont.Ui();

            var saveProfileButton = new RetroButton();
            saveProfileButton.Text = "Save Profile";
            saveProfileButton.SetBounds(184, 80, 100, 24);
            saveProfileButton.Click += delegate { SaveProfile(); };

            profileBox.Controls.Add(displayNameLabel);
            profileBox.Controls.Add(displayNameBox);
            profileBox.Controls.Add(computerLabel);
            profileBox.Controls.Add(computerBox);
            profileBox.Controls.Add(saveProfileButton);

            var quickToolsBox = BuildGroup("Quick Tools", 332, 64, 286, 112);
            quickToolsBox.Controls.Add(BuildQuickButton("Open Settings", 14, 18, delegate { host.OpenSettings(); }));
            quickToolsBox.Controls.Add(BuildQuickButton("System Usage", 152, 18, delegate { host.OpenSystemUsage(); }));
            quickToolsBox.Controls.Add(BuildQuickButton("Version Info", 14, 52, delegate { host.OpenVersionInfo(); }));
            quickToolsBox.Controls.Add(BuildQuickButton("Open Nost64/32", 152, 52, delegate { host.OpenNostRun(string.Empty); }));

            var displayBox = BuildGroup("Graphics Driver & Theme", 16, 196, 300, 164);
            var graphicsLabel = new Label();
            graphicsLabel.Text = "Graphics Driver:";
            graphicsLabel.SetBounds(14, 22, 88, 18);
            graphicsModeBox = new ComboBox();
            graphicsModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            graphicsModeBox.Items.AddRange(new object[] { "Classic", "Soft", "Vivid" });
            graphicsModeBox.SetBounds(104, 20, 180, 22);

            var activeLabel = new Label();
            activeLabel.Text = "Active Title:";
            activeLabel.SetBounds(14, 56, 88, 18);
            activeTitleSwatch = BuildColorSwatch(104, 54);
            var activePickButton = new RetroButton();
            activePickButton.Text = "Pick...";
            activePickButton.SetBounds(164, 52, 68, 24);
            activePickButton.Click += delegate { PickColor(activeTitleSwatch); };

            var inactiveLabel = new Label();
            inactiveLabel.Text = "Inactive Title:";
            inactiveLabel.SetBounds(14, 88, 88, 18);
            inactiveTitleSwatch = BuildColorSwatch(104, 86);
            var inactivePickButton = new RetroButton();
            inactivePickButton.Text = "Pick...";
            inactivePickButton.SetBounds(164, 84, 68, 24);
            inactivePickButton.Click += delegate { PickColor(inactiveTitleSwatch); };

            var applyDisplayButton = new RetroButton();
            applyDisplayButton.Text = "Apply Display";
            applyDisplayButton.SetBounds(176, 124, 108, 24);
            applyDisplayButton.Click += delegate { ApplyDisplay(); };

            displayBox.Controls.Add(graphicsLabel);
            displayBox.Controls.Add(graphicsModeBox);
            displayBox.Controls.Add(activeLabel);
            displayBox.Controls.Add(activeTitleSwatch);
            displayBox.Controls.Add(activePickButton);
            displayBox.Controls.Add(inactiveLabel);
            displayBox.Controls.Add(inactiveTitleSwatch);
            displayBox.Controls.Add(inactivePickButton);
            displayBox.Controls.Add(applyDisplayButton);

            var audioBox = BuildGroup("Audio", 332, 196, 286, 164);
            startupSoundBox = new CheckBox();
            startupSoundBox.Text = "Startup sound enabled";
            startupSoundBox.SetBounds(14, 22, 170, 18);
            startupSoundBox.BackColor = Color.Transparent;

            shellSoundBox = new CheckBox();
            shellSoundBox.Text = "Shell sound effects enabled";
            shellSoundBox.SetBounds(14, 48, 190, 18);
            shellSoundBox.BackColor = Color.Transparent;

            var testStartupButton = BuildQuickButton("Test Startup", 14, 78, delegate { StartupAudio.PlayWin31(true); });
            var testChimeButton = BuildQuickButton("Test Chime", 124, 78, delegate { StartupAudio.PlayChimes(true); });
            var saveAudioButton = new RetroButton();
            saveAudioButton.Text = "Save Audio";
            saveAudioButton.SetBounds(160, 122, 94, 24);
            saveAudioButton.Click += delegate { SaveAudio(); };

            audioBox.Controls.Add(startupSoundBox);
            audioBox.Controls.Add(shellSoundBox);
            audioBox.Controls.Add(testStartupButton);
            audioBox.Controls.Add(testChimeButton);
            audioBox.Controls.Add(saveAudioButton);

            var applyAllButton = new RetroButton();
            applyAllButton.Text = "Apply All Changes";
            applyAllButton.SetBounds(16, 384, 126, 24);
            applyAllButton.Click += delegate { ApplyAllChanges(); };

            var note = new Label();
            note.Text = "Use Settings for desktop background, prompts, and full configuration.";
            note.AutoSize = false;
            note.SetBounds(156, 388, 420, 18);

            Controls.Add(welcomeLabel);
            Controls.Add(subtitle);
            Controls.Add(profileBox);
            Controls.Add(quickToolsBox);
            Controls.Add(displayBox);
            Controls.Add(audioBox);
            Controls.Add(applyAllButton);
            Controls.Add(note);

            LoadSettings();
        }

        private Panel BuildGroup(string title, int left, int top, int width, int height)
        {
            var panel = new Panel();
            panel.SetBounds(left, top, width, height);
            panel.Paint += delegate(object sender, PaintEventArgs e)
            {
                ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, Color.White, ButtonBorderStyle.Solid);
                TextRenderer.DrawText(e.Graphics, title, RetroFont.Ui(), new Point(8, -1), Color.Black);
            };
            return panel;
        }

        private Control BuildQuickButton(string text, int left, int top, EventHandler action)
        {
            var button = new RetroButton();
            button.Text = text;
            button.SetBounds(left, top, 110, 24);
            button.Click += action;
            return button;
        }

        private Panel BuildColorSwatch(int left, int top)
        {
            var panel = new Panel();
            panel.SetBounds(left, top, 54, 22);
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.BackColor = Color.White;
            return panel;
        }

        private void LoadSettings()
        {
            var settings = host.GetDesktopSettings();
            displayNameBox.Text = settings.DisplayName;
            computerBox.Text = settings.ComputerAlias;
            graphicsModeBox.SelectedItem = settings.GraphicsMode.ToString();
            if (graphicsModeBox.SelectedIndex < 0)
            {
                graphicsModeBox.SelectedIndex = 0;
            }

            activeTitleSwatch.BackColor = settings.ActiveTitleColor;
            inactiveTitleSwatch.BackColor = settings.InactiveTitleColor;
            startupSoundBox.Checked = settings.StartupSoundEnabled;
            shellSoundBox.Checked = settings.ShellSoundEffectsEnabled;
            welcomeLabel.Text = "Welcome, " + displayNameBox.Text + " (" + computerBox.Text + ")";
        }

        private void SaveProfile()
        {
            var settings = host.GetDesktopSettings();
            settings.DisplayName = displayNameBox.Text.Trim().Length == 0 ? Environment.UserName : displayNameBox.Text.Trim();
            settings.ComputerAlias = computerBox.Text.Trim().Length == 0 ? Environment.MachineName : computerBox.Text.Trim();
            host.ApplyDesktopSettings(settings);
            LoadSettings();
        }

        private void ApplyDisplay()
        {
            var settings = host.GetDesktopSettings();
            settings.GraphicsMode = (DesktopGraphicsMode)Enum.Parse(typeof(DesktopGraphicsMode), graphicsModeBox.SelectedItem.ToString(), true);
            settings.ActiveTitleColor = activeTitleSwatch.BackColor;
            settings.InactiveTitleColor = inactiveTitleSwatch.BackColor;
            host.ApplyDesktopSettings(settings);
            LoadSettings();
        }

        private void SaveAudio()
        {
            var settings = host.GetDesktopSettings();
            settings.StartupSoundEnabled = startupSoundBox.Checked;
            settings.ShellSoundEffectsEnabled = shellSoundBox.Checked;
            host.ApplyDesktopSettings(settings);
            LoadSettings();
        }

        private void ApplyAllChanges()
        {
            var settings = host.GetDesktopSettings();
            settings.DisplayName = displayNameBox.Text.Trim().Length == 0 ? Environment.UserName : displayNameBox.Text.Trim();
            settings.ComputerAlias = computerBox.Text.Trim().Length == 0 ? Environment.MachineName : computerBox.Text.Trim();
            settings.GraphicsMode = (DesktopGraphicsMode)Enum.Parse(typeof(DesktopGraphicsMode), graphicsModeBox.SelectedItem.ToString(), true);
            settings.ActiveTitleColor = activeTitleSwatch.BackColor;
            settings.InactiveTitleColor = inactiveTitleSwatch.BackColor;
            settings.StartupSoundEnabled = startupSoundBox.Checked;
            settings.ShellSoundEffectsEnabled = shellSoundBox.Checked;
            host.ApplyDesktopSettings(settings);
            LoadSettings();
        }

        private void PickColor(Panel swatch)
        {
            using (var dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.FullOpen = true;
                dialog.Color = swatch.BackColor;
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    swatch.BackColor = dialog.Color;
                }
            }
        }
    }
}
