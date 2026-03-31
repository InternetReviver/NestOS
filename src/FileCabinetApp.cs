using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal class FileCabinetApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly ListView list;
        private readonly Label pathLabel;

        public FileCabinetApp(IDesktopHost host)
        {
            this.host = host;
            BackColor = RetroPalette.WindowBackground;
            Dock = DockStyle.Fill;

            var toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 40;

            var openButton = CreateButton("Open", 6);
            openButton.Click += delegate { OpenSelected(); };

            var refreshButton = CreateButton("Refresh", 84);
            refreshButton.Click += delegate { RefreshListing(); };

            var renameButton = CreateButton("Rename", 162);
            renameButton.Click += delegate { RenameSelected(); };

            var deleteButton = CreateButton("Delete", 240);
            deleteButton.Click += delegate { DeleteSelected(); };

            pathLabel = new Label();
            pathLabel.AutoSize = false;
            pathLabel.SetBounds(324, 10, 420, 20);
            pathLabel.Text = SaveSystem.EnsureRoot();

            toolbar.Controls.Add(openButton);
            toolbar.Controls.Add(refreshButton);
            toolbar.Controls.Add(renameButton);
            toolbar.Controls.Add(deleteButton);
            toolbar.Controls.Add(pathLabel);

            list = new ListView();
            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.GridLines = true;
            list.MultiSelect = false;
            list.Columns.Add("Name", 220);
            list.Columns.Add("Type", 80);
            list.Columns.Add("Size", 90);
            list.Columns.Add("Modified", 180);
            list.DoubleClick += delegate { OpenSelected(); };

            Controls.Add(list);
            Controls.Add(toolbar);

            RefreshListing();
        }

        private RetroButton CreateButton(string text, int left)
        {
            var button = new RetroButton();
            button.Text = text;
            button.SetBounds(left, 8, 72, 24);
            return button;
        }

        private void RefreshListing()
        {
            var root = SaveSystem.EnsureRoot();
            list.Items.Clear();

            var directory = new DirectoryInfo(root);
            var files = directory.GetFiles();
            Array.Sort(files, delegate(FileInfo a, FileInfo b) { return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); });

            for (var i = 0; i < files.Length; i++)
            {
                var item = new ListViewItem(files[i].Name);
                item.SubItems.Add(files[i].Extension.TrimStart('.').ToUpperInvariant());
                item.SubItems.Add((files[i].Length / 1024.0).ToString("0.0") + " KB");
                item.SubItems.Add(files[i].LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                item.Tag = files[i].FullName;
                list.Items.Add(item);
            }
        }

        private void OpenSelected()
        {
            if (list.SelectedItems.Count == 0)
            {
                host.ShowMessage("File Cabinet", "Pick a file first.");
                return;
            }

            var path = list.SelectedItems[0].Tag as string;
            if (path == null)
            {
                return;
            }

            OpenPath(path);
        }

        private void OpenPath(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".txt")
            {
                host.OpenTextEditor(path);
            }
            else if (extension == ".bmp" || extension == ".png")
            {
                host.OpenPaint(path);
            }
            else
            {
                host.ShowMessage("File Cabinet", "No viewer is registered for " + extension + ".");
            }
        }

        private void RenameSelected()
        {
            if (list.SelectedItems.Count == 0)
            {
                host.ShowMessage("File Cabinet", "Pick a file first.");
                return;
            }

            var path = list.SelectedItems[0].Tag as string;
            if (path == null)
            {
                return;
            }

            var newName = PromptDialog.ShowDialog(FindForm(), "Rename File", "Enter a new file name:", Path.GetFileNameWithoutExtension(path));
            if (newName == null || newName.Trim().Length == 0)
            {
                return;
            }

            var newPath = SaveSystem.BuildPath(newName, Path.GetExtension(path));
            if (string.Equals(path, newPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            File.Move(path, newPath);
            RefreshListing();
        }

        private void DeleteSelected()
        {
            if (list.SelectedItems.Count == 0)
            {
                host.ShowMessage("File Cabinet", "Pick a file first.");
                return;
            }

            var path = list.SelectedItems[0].Tag as string;
            if (path == null)
            {
                return;
            }

            if (host.Confirm("File Cabinet", "Delete " + Path.GetFileName(path) + "?"))
            {
                File.Delete(path);
                RefreshListing();
            }
        }
    }
}
