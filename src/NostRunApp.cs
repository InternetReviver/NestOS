using System;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class NostRunApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly TextBox commandBox;
        private readonly ListBox appList;

        public NostRunApp(IDesktopHost host, string initialCommand)
        {
            this.host = host;
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            var intro = new Label();
            intro.Text = "Type the name of a built-in app, or pick one from the list.";
            intro.AutoSize = false;
            intro.SetBounds(10, 10, 340, 18);

            var label = new Label();
            label.Text = "Open:";
            label.SetBounds(10, 38, 36, 18);

            commandBox = new TextBox();
            commandBox.Font = RetroFont.Ui();
            commandBox.SetBounds(50, 36, 298, 22);
            commandBox.Text = initialCommand ?? string.Empty;
            commandBox.KeyDown += HandleCommandKeyDown;

            appList = new ListBox();
            appList.Font = RetroFont.Ui();
            appList.SetBounds(10, 68, 338, 118);
            appList.Items.AddRange(new object[]
            {
                "paint",
                "textpad",
                "file cabinet",
                "nestdos",
                "nost64/32",
                "analog clock",
                "digital clock",
                "solitaire",
                "mahjong",
                "chess",
                "version"
            });
            appList.DoubleClick += delegate { RunSelection(); };
            appList.SelectedIndexChanged += delegate
            {
                if (appList.SelectedItem != null)
                {
                    commandBox.Text = appList.SelectedItem.ToString();
                }
            };

            var runButton = new RetroButton();
            runButton.Text = "Run";
            runButton.SetBounds(196, 196, 72, 24);
            runButton.Click += delegate { RunSelection(); };

            var cancelButton = new RetroButton();
            cancelButton.Text = "Close";
            cancelButton.SetBounds(276, 196, 72, 24);
            cancelButton.Click += delegate { CloseContainingWindow(); };

            Controls.Add(intro);
            Controls.Add(label);
            Controls.Add(commandBox);
            Controls.Add(appList);
            Controls.Add(runButton);
            Controls.Add(cancelButton);
        }

        private void HandleCommandKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                RunSelection();
                e.SuppressKeyPress = true;
            }
        }

        private void RunSelection()
        {
            var command = commandBox.Text.Trim();
            if (!host.RunShellCommand(command))
            {
                host.ShowMessage("Nost64/32", "Unknown app: " + command);
                return;
            }

            CloseContainingWindow();
        }

        private void CloseContainingWindow()
        {
            Control current = this;
            while (current != null)
            {
                var retroWindow = current as RetroWindow;
                if (retroWindow != null)
                {
                    retroWindow.CloseWindow();
                    return;
                }

                current = current.Parent;
            }
        }
    }
}
