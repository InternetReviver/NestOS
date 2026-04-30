using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal class ExportApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly ListView list;
        private readonly Label detailsLabel;
        private readonly RetroButton exportButton;

        public ExportApp(IDesktopHost host)
        {
            this.host = host;
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            var title = new Label();
            title.Text = "Export to NestOS-Only Files";
            title.Font = RetroFont.UiBold();
            title.AutoSize = false;
            title.SetBounds(12, 12, 260, 18);

            var intro = new Label();
            intro.Text = "Export .txt files as .comw and .bmp files as .comu. These formats only open inside NestOS.";
            intro.AutoSize = false;
            intro.SetBounds(12, 36, 500, 34);

            list = new ListView();
            list.View = View.Details;
            list.FullRowSelect = true;
            list.GridLines = true;
            list.MultiSelect = false;
            list.SetBounds(12, 78, 500, 192);
            list.Columns.Add("Name", 250);
            list.Columns.Add("Source Type", 110);
            list.Columns.Add("Exports To", 110);
            list.SelectedIndexChanged += delegate { UpdateSelectionState(); };
            list.DoubleClick += delegate { ExportSelected(); };

            detailsLabel = new Label();
            detailsLabel.BorderStyle = BorderStyle.FixedSingle;
            detailsLabel.BackColor = Color.White;
            detailsLabel.AutoSize = false;
            detailsLabel.SetBounds(12, 280, 340, 52);

            var refreshButton = new RetroButton();
            refreshButton.Text = "Refresh";
            refreshButton.SetBounds(360, 280, 72, 24);
            refreshButton.Click += delegate { RefreshListing(); };

            exportButton = new RetroButton();
            exportButton.Text = "Export";
            exportButton.SetBounds(440, 280, 72, 24);
            exportButton.Click += delegate { ExportSelected(); };

            Controls.Add(title);
            Controls.Add(intro);
            Controls.Add(list);
            Controls.Add(detailsLabel);
            Controls.Add(refreshButton);
            Controls.Add(exportButton);

            RefreshListing();
        }

        private void RefreshListing()
        {
            var root = SaveSystem.EnsureRoot();
            list.Items.Clear();

            var files = new DirectoryInfo(root).GetFiles();
            Array.Sort(files, delegate(FileInfo left, FileInfo right) { return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase); });

            for (var i = 0; i < files.Length; i++)
            {
                var extension = files[i].Extension.ToLowerInvariant();
                if (extension != ".txt" && extension != ".bmp")
                {
                    continue;
                }

                var item = new ListViewItem(files[i].Name);
                item.SubItems.Add(extension == ".txt" ? "Text" : "Bitmap");
                item.SubItems.Add(extension == ".txt" ? ".comw" : ".comu");
                item.Tag = files[i].FullName;
                list.Items.Add(item);
            }

            UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            if (list.SelectedItems.Count == 0)
            {
                detailsLabel.Text = list.Items.Count == 0
                    ? "No exportable .txt or .bmp files are in SAVE yet."
                    : "Select a .txt or .bmp file to export it into a NestOS-only format.";
                exportButton.Enabled = false;
                exportButton.Text = "Export";
                return;
            }

            var path = list.SelectedItems[0].Tag as string;
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var targetExtension = extension == ".txt" ? ".comw" : ".comu";
            detailsLabel.Text = "Selected: " + Path.GetFileName(path) + "\r\nTarget type: " + targetExtension;
            exportButton.Enabled = true;
            exportButton.Text = "To " + targetExtension;
        }

        private void ExportSelected()
        {
            if (list.SelectedItems.Count == 0)
            {
                host.ShowMessage("Export", "Pick a .txt or .bmp file first.");
                return;
            }

            var sourcePath = list.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                host.ShowMessage("Export", "That source file is no longer available.");
                RefreshListing();
                return;
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            var targetExtension = extension == ".txt" ? ".comw" : ".comu";
            var requestedName = PromptDialog.ShowDialog(FindForm(), "Export File", "Enter the NestOS export name:", Path.GetFileNameWithoutExtension(sourcePath));
            if (requestedName == null)
            {
                return;
            }

            var destinationPath = SaveSystem.BuildPath(requestedName, targetExtension);
            if (File.Exists(destinationPath) && !host.Confirm("Export", "Replace " + Path.GetFileName(destinationPath) + "?"))
            {
                return;
            }

            try
            {
                if (extension == ".txt")
                {
                    NestExportCodec.ExportTextFile(sourcePath, destinationPath);
                }
                else if (extension == ".bmp")
                {
                    NestExportCodec.ExportBitmapFile(sourcePath, destinationPath);
                }
                else
                {
                    host.ShowMessage("Export", "Only .txt and .bmp files can be exported.");
                    return;
                }

                host.ShowMessage("Export", "Exported to " + destinationPath);
                RefreshListing();
            }
            catch (Exception ex)
            {
                host.ShowMessage("Export", "Export failed.\r\n\r\n" + ex.Message);
            }
        }
    }
}
