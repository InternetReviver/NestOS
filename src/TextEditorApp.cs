using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal class TextEditorApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly TextBox fileNameBox;
        private readonly TextBox editor;
        private string currentPath;

        public TextEditorApp(IDesktopHost host, string path)
        {
            this.host = host;
            BackColor = RetroPalette.WindowBackground;
            Dock = DockStyle.Fill;

            var toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 36;
            toolbar.BackColor = RetroPalette.WindowBackground;
            toolbar.Padding = new Padding(6);

            var newButton = CreateButton("New", 6);
            newButton.Click += delegate
            {
                currentPath = null;
                fileNameBox.Text = "untitled";
                editor.Clear();
            };

            var saveButton = CreateButton("Save", 84);
            saveButton.Click += delegate { SaveDocument(); };

            var filesButton = CreateButton("Files", 162);
            filesButton.Click += delegate { host.OpenFileCabinet(); };

            var fileLabel = new Label();
            fileLabel.Text = "File:";
            fileLabel.AutoSize = false;
            fileLabel.TextAlign = ContentAlignment.MiddleLeft;
            fileLabel.SetBounds(246, 9, 28, 20);

            fileNameBox = new TextBox();
            fileNameBox.SetBounds(278, 8, 220, 22);
            fileNameBox.Font = RetroFont.Ui();

            toolbar.Controls.Add(newButton);
            toolbar.Controls.Add(saveButton);
            toolbar.Controls.Add(filesButton);
            toolbar.Controls.Add(fileLabel);
            toolbar.Controls.Add(fileNameBox);

            editor = new TextBox();
            editor.Multiline = true;
            editor.AcceptsReturn = true;
            editor.AcceptsTab = true;
            editor.ScrollBars = ScrollBars.Both;
            editor.WordWrap = false;
            editor.Font = new Font("Consolas", 10f, FontStyle.Regular, GraphicsUnit.Point);
            editor.Dock = DockStyle.Fill;

            Controls.Add(editor);
            Controls.Add(toolbar);

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                LoadFile(path);
            }
            else
            {
                fileNameBox.Text = "untitled";
            }
        }

        private RetroButton CreateButton(string text, int left)
        {
            var button = new RetroButton();
            button.Text = text;
            button.SetBounds(left, 6, 72, 24);
            return button;
        }

        private void LoadFile(string path)
        {
            currentPath = path;
            fileNameBox.Text = Path.GetFileNameWithoutExtension(path);
            editor.Text = File.ReadAllText(path);
        }

        private void SaveDocument()
        {
            var requestedName = fileNameBox.Text;
            if (requestedName.Trim().Length == 0)
            {
                requestedName = PromptDialog.ShowDialog(FindForm(), "Save Text File", "Enter a file name:", "untitled");
                if (requestedName == null)
                {
                    return;
                }
            }

            currentPath = SaveSystem.BuildPath(requestedName, ".txt");
            File.WriteAllText(currentPath, editor.Text);
            fileNameBox.Text = Path.GetFileNameWithoutExtension(currentPath);
            host.ShowMessage("TextPad", "Saved to " + currentPath);
        }
    }
}
