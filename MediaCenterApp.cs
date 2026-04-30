using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal enum MediaCenterViewMode
    {
        Library,
        NowPlaying,
        NestFiles,
        AudioVideo
    }

    internal class MediaCenterApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly ListView mediaList;
        private readonly Label statusLabel;
        private readonly Label previewTitle;
        private readonly Label previewMeta;
        private readonly TextBox textPreview;
        private readonly PictureBox imagePreview;
        private readonly Panel headerPanel;
        private readonly Panel navPanel;
        private readonly Panel contentPanel;
        private readonly Panel previewPanel;
        private readonly Dictionary<MediaCenterViewMode, Label> navLabels;
        private readonly Button importButton;
        private readonly Button refreshButton;
        private readonly Button openButton;
        private readonly Button playButton;
        private readonly Button stopButton;
        private MediaCenterViewMode currentMode;
        private string nowPlayingPath;

        public MediaCenterApp(IDesktopHost host)
        {
            this.host = host;
            navLabels = new Dictionary<MediaCenterViewMode, Label>();

            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(222, 234, 250);
            Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;

            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 74;
            headerPanel.Paint += PaintHeader;

            var title = new Label();
            title.Text = "Media Center";
            title.Font = new Font("Tahoma", 18.0f, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.White;
            title.BackColor = Color.Transparent;
            title.AutoSize = false;
            title.SetBounds(16, 10, 280, 28);

            var subtitle = new Label();
            subtitle.Text = "NestOS library for media and custom files";
            subtitle.Font = new Font("Tahoma", 8.25f, FontStyle.Bold, GraphicsUnit.Point);
            subtitle.ForeColor = Color.FromArgb(225, 244, 255);
            subtitle.BackColor = Color.Transparent;
            subtitle.AutoSize = false;
            subtitle.SetBounds(18, 42, 320, 18);

            headerPanel.Controls.Add(title);
            headerPanel.Controls.Add(subtitle);

            navPanel = new Panel();
            navPanel.Dock = DockStyle.Left;
            navPanel.Width = 156;
            navPanel.BackColor = Color.FromArgb(168, 206, 116);
            navPanel.Paint += PaintNavPanel;

            navPanel.Controls.Add(BuildNavLabel("Library", MediaCenterViewMode.Library, 18));
            navPanel.Controls.Add(BuildNavLabel("Now Playing", MediaCenterViewMode.NowPlaying, 48));
            navPanel.Controls.Add(BuildNavLabel("Nest Files", MediaCenterViewMode.NestFiles, 78));
            navPanel.Controls.Add(BuildNavLabel("Audio / Video", MediaCenterViewMode.AudioVideo, 108));

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(14, 12, 14, 12);
            contentPanel.BackColor = BackColor;

            mediaList = new ListView();
            mediaList.View = View.Details;
            mediaList.FullRowSelect = true;
            mediaList.GridLines = false;
            mediaList.HideSelection = false;
            mediaList.Font = Font;
            mediaList.BackColor = Color.White;
            mediaList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            mediaList.Columns.Add("Name", 220);
            mediaList.Columns.Add("Type", 80);
            mediaList.Columns.Add("Size", 84);
            mediaList.Columns.Add("Section", 88);
            mediaList.SelectedIndexChanged += delegate { UpdatePreview(); };
            mediaList.DoubleClick += delegate { OpenSelected(); };
            mediaList.KeyDown += HandleMediaListKeyDown;

            previewPanel = new Panel();
            previewPanel.BackColor = Color.White;
            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            previewPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            previewTitle = new Label();
            previewTitle.Font = new Font("Tahoma", 10.0f, FontStyle.Bold, GraphicsUnit.Point);
            previewTitle.AutoSize = false;
            previewTitle.SetBounds(12, 12, 420, 18);

            previewMeta = new Label();
            previewMeta.AutoSize = false;
            previewMeta.SetBounds(12, 36, 420, 48);

            textPreview = new TextBox();
            textPreview.Multiline = true;
            textPreview.ReadOnly = true;
            textPreview.ScrollBars = ScrollBars.Vertical;
            textPreview.BackColor = Color.White;
            textPreview.BorderStyle = BorderStyle.FixedSingle;
            textPreview.Font = Font;
            textPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textPreview.KeyDown += HandleMediaListKeyDown;

            imagePreview = new PictureBox();
            imagePreview.BackColor = Color.FromArgb(240, 247, 255);
            imagePreview.BorderStyle = BorderStyle.FixedSingle;
            imagePreview.SizeMode = PictureBoxSizeMode.Zoom;
            imagePreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            imagePreview.KeyDown += HandleMediaListKeyDown;

            previewPanel.Controls.Add(previewTitle);
            previewPanel.Controls.Add(previewMeta);
            previewPanel.Controls.Add(textPreview);
            previewPanel.Controls.Add(imagePreview);

            importButton = BuildActionButton("Import...", 0, 0);
            importButton.Click += delegate { ImportMedia(); };

            refreshButton = BuildActionButton("Refresh", 0, 0);
            refreshButton.Click += delegate { RefreshLibrary(); };

            openButton = BuildActionButton("Open", 0, 0);
            openButton.Click += delegate { OpenSelected(); };

            playButton = BuildActionButton("Play", 0, 0);
            playButton.Click += delegate { PlaySelectedMedia(); };

            stopButton = BuildActionButton("Stop", 0, 0);
            stopButton.Click += delegate
            {
                MediaPlayback.Stop();
                nowPlayingPath = null;
                statusLabel.Text = "Playback stopped.";
                if (currentMode == MediaCenterViewMode.NowPlaying)
                {
                    RefreshLibrary();
                }
            };

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.BorderStyle = BorderStyle.FixedSingle;
            statusLabel.BackColor = Color.White;
            statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Controls.Add(contentPanel);
            Controls.Add(navPanel);
            Controls.Add(headerPanel);

            contentPanel.Controls.Add(mediaList);
            contentPanel.Controls.Add(previewPanel);
            contentPanel.Controls.Add(importButton);
            contentPanel.Controls.Add(refreshButton);
            contentPanel.Controls.Add(openButton);
            contentPanel.Controls.Add(playButton);
            contentPanel.Controls.Add(stopButton);
            contentPanel.Controls.Add(statusLabel);

            Resize += delegate { ApplyLayout(); };
            currentMode = MediaCenterViewMode.Library;
            ApplyLayout();
            UpdateNavSelection();
            RefreshLibrary();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (imagePreview.Image != null)
                {
                    imagePreview.Image.Dispose();
                }

                MediaPlayback.Stop();
            }

            base.Dispose(disposing);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || base.IsInputKey(keyData);
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            if (mediaList.Items.Count > 0 && mediaList.SelectedItems.Count == 0)
            {
                SelectItem(0);
            }

            mediaList.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (HandleArrowNavigation(e))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        private Label BuildNavLabel(string text, MediaCenterViewMode mode, int top)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Tahoma", 8.25f, FontStyle.Bold, GraphicsUnit.Point);
            label.ForeColor = Color.FromArgb(35, 66, 22);
            label.BackColor = Color.Transparent;
            label.AutoSize = false;
            label.Cursor = Cursors.Hand;
            label.SetBounds(18, top, 116, 22);
            label.Click += delegate
            {
                SetViewMode(mode);
                Focus();
            };
            label.MouseEnter += delegate
            {
                if (currentMode != mode)
                {
                    label.ForeColor = Color.FromArgb(18, 46, 124);
                }
            };
            label.MouseLeave += delegate
            {
                if (currentMode != mode)
                {
                    label.ForeColor = Color.FromArgb(35, 66, 22);
                }
            };

            navLabels[mode] = label;
            return label;
        }

        private Button BuildActionButton(string text, int left, int top)
        {
            var button = new Button();
            button.Text = text;
            button.Font = Font;
            button.UseVisualStyleBackColor = true;
            button.SetBounds(left, top, 74, 24);
            return button;
        }

        private void ApplyLayout()
        {
            var panelWidth = Math.Max(280, contentPanel.ClientSize.Width / 2 - 22);
            var panelHeight = Math.Max(280, contentPanel.ClientSize.Height - 98);
            var rightWidth = Math.Max(260, contentPanel.ClientSize.Width - panelWidth - 26);

            mediaList.SetBounds(14, 12, panelWidth, panelHeight);
            previewPanel.SetBounds(mediaList.Right + 12, 12, rightWidth, panelHeight);

            previewTitle.Width = previewPanel.ClientSize.Width - 24;
            previewMeta.Width = previewPanel.ClientSize.Width - 24;
            textPreview.SetBounds(12, 92, previewPanel.ClientSize.Width - 24, previewPanel.ClientSize.Height - 104);
            imagePreview.SetBounds(12, 92, previewPanel.ClientSize.Width - 24, previewPanel.ClientSize.Height - 104);

            importButton.SetBounds(14, mediaList.Bottom + 12, 82, 24);
            refreshButton.SetBounds(importButton.Right + 8, mediaList.Bottom + 12, 74, 24);
            openButton.SetBounds(refreshButton.Right + 8, mediaList.Bottom + 12, 74, 24);
            playButton.SetBounds(openButton.Right + 8, mediaList.Bottom + 12, 74, 24);
            stopButton.SetBounds(playButton.Right + 8, mediaList.Bottom + 12, 74, 24);
            statusLabel.SetBounds(previewPanel.Left, mediaList.Bottom + 10, previewPanel.Width, 28);
        }

        private void RefreshLibrary()
        {
            mediaList.BeginUpdate();
            mediaList.Items.Clear();

            var files = Directory.GetFiles(SaveSystem.EnsureRoot(), "*.*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < files.Length; i++)
            {
                if (!ShouldShowFile(files[i]))
                {
                    continue;
                }

                var info = new FileInfo(files[i]);
                var item = new ListViewItem(GetDisplayName(info.FullName));
                item.SubItems.Add(Path.GetExtension(files[i]).TrimStart('.').ToUpperInvariant());
                item.SubItems.Add(SystemUsageSnapshot.FormatBytes(info.Length));
                item.SubItems.Add(GetSectionLabel(files[i]));
                item.Tag = info.FullName;
                mediaList.Items.Add(item);
            }

            mediaList.EndUpdate();

            if (mediaList.Items.Count > 0)
            {
                SelectItem(0);
            }
            else
            {
                UpdatePreview();
            }

            statusLabel.Text = BuildStatusText();
        }

        private bool ShouldShowFile(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var isNestFile = extension == ".comw" || extension == ".comu";
            var isMediaFile = extension == ".mp3" || extension == ".mp4" || extension == ".wav";
            if (!isNestFile && !isMediaFile)
            {
                return false;
            }

            switch (currentMode)
            {
                case MediaCenterViewMode.Library:
                    return true;
                case MediaCenterViewMode.NowPlaying:
                    return !string.IsNullOrEmpty(nowPlayingPath) && string.Equals(path, nowPlayingPath, StringComparison.OrdinalIgnoreCase);
                case MediaCenterViewMode.NestFiles:
                    return isNestFile;
                case MediaCenterViewMode.AudioVideo:
                    return isMediaFile;
                default:
                    return true;
            }
        }

        private void UpdatePreview()
        {
            if (imagePreview.Image != null)
            {
                imagePreview.Image.Dispose();
                imagePreview.Image = null;
            }

            textPreview.Visible = false;
            imagePreview.Visible = false;

            if (mediaList.SelectedItems.Count == 0)
            {
                previewTitle.Text = currentMode == MediaCenterViewMode.NowPlaying ? "Nothing is playing" : "Nothing selected";
                previewMeta.Text = BuildEmptyPreviewText();
                textPreview.Text = string.Empty;
                statusLabel.Text = BuildStatusText();
                return;
            }

            var path = mediaList.SelectedItems[0].Tag as string;
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var info = new FileInfo(path);

            previewTitle.Text = Path.GetFileName(path);
            previewMeta.Text = extension.ToUpperInvariant() + "  |  " + SystemUsageSnapshot.FormatBytes(info.Length) + "\r\n" + info.LastWriteTime.ToString("yyyy-MM-dd HH:mm");

            try
            {
                if (extension == ".comw")
                {
                    textPreview.Text = BuildTextPreview(NestExportCodec.ReadTextFile(path));
                    textPreview.Visible = true;
                }
                else if (extension == ".comu")
                {
                    imagePreview.Image = NestExportCodec.ReadBitmapFile(path);
                    imagePreview.Visible = true;
                }
                else
                {
                    textPreview.Text = BuildMediaPreview(path);
                    textPreview.Visible = true;
                }
            }
            catch (Exception ex)
            {
                textPreview.Text = "Preview unavailable.\r\n\r\n" + ex.Message;
                textPreview.Visible = true;
            }
        }

        private void OpenSelected()
        {
            if (mediaList.SelectedItems.Count == 0)
            {
                host.ShowMessage("Media Center", "Pick a file first.");
                return;
            }

            var path = mediaList.SelectedItems[0].Tag as string;
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".comw")
            {
                host.OpenTextEditor(path);
                statusLabel.Text = "Opened " + Path.GetFileName(path) + " in TextPad.";
            }
            else if (extension == ".comu")
            {
                host.OpenPaint(path);
                statusLabel.Text = "Opened " + Path.GetFileName(path) + " in Paint.";
            }
            else
            {
                PlaySelectedMedia();
            }
        }

        private void PlaySelectedMedia()
        {
            if (mediaList.SelectedItems.Count == 0)
            {
                host.ShowMessage("Media Center", "Pick a media file first.");
                return;
            }

            var path = mediaList.SelectedItems[0].Tag as string;
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".mp3" && extension != ".mp4" && extension != ".wav")
            {
                host.ShowMessage("Media Center", "Playback is only for MP3, MP4, and WAV files.");
                return;
            }

            try
            {
                MediaPlayback.Play(path);
                nowPlayingPath = path;
                statusLabel.Text = "Playing " + Path.GetFileName(path) + "...";
                if (currentMode == MediaCenterViewMode.NowPlaying)
                {
                    RefreshLibrary();
                }
            }
            catch (Exception ex)
            {
                host.ShowMessage("Media Center", "Playback failed.\r\n\r\n" + ex.Message);
            }
        }

        private void ImportMedia()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Import Media Into SAVE";
                dialog.Filter = "Media Files|*.mp3;*.mp4;*.wav";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                {
                    return;
                }

                var imported = 0;
                for (var i = 0; i < dialog.FileNames.Length; i++)
                {
                    try
                    {
                        CopyMediaIntoSave(dialog.FileNames[i]);
                        imported++;
                    }
                    catch (Exception ex)
                    {
                        host.ShowMessage("Media Center", "Could not import " + Path.GetFileName(dialog.FileNames[i]) + ".\r\n\r\n" + ex.Message);
                    }
                }

                RefreshLibrary();
                statusLabel.Text = imported == 1 ? "Imported 1 media file into SAVE." : "Imported " + imported + " media files into SAVE.";
            }
        }

        private void CopyMediaIntoSave(string sourcePath)
        {
            var extension = Path.GetExtension(sourcePath);
            var requestedName = Path.GetFileNameWithoutExtension(sourcePath);
            var destination = SaveSystem.BuildPath(requestedName, extension);
            var counter = 2;

            while (File.Exists(destination))
            {
                destination = SaveSystem.BuildPath(requestedName + " (" + counter + ")", extension);
                counter++;
            }

            File.Copy(sourcePath, destination, false);
        }

        private void SetViewMode(MediaCenterViewMode mode)
        {
            currentMode = mode;
            UpdateNavSelection();
            RefreshLibrary();
            mediaList.Focus();
        }

        private void UpdateNavSelection()
        {
            foreach (var pair in navLabels)
            {
                var selected = pair.Key == currentMode;
                pair.Value.BackColor = selected ? Color.FromArgb(74, 126, 202) : Color.Transparent;
                pair.Value.ForeColor = selected ? Color.White : Color.FromArgb(35, 66, 22);
            }
        }

        private void HandleMediaListKeyDown(object sender, KeyEventArgs e)
        {
            HandleArrowNavigation(e);
        }

        private bool HandleArrowNavigation(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                MoveTab(-1);
                e.SuppressKeyPress = true;
                e.Handled = true;
                return true;
            }

            if (e.KeyCode == Keys.Right)
            {
                MoveTab(1);
                e.SuppressKeyPress = true;
                e.Handled = true;
                return true;
            }

            if (e.KeyCode == Keys.Up && mediaList.Focused)
            {
                SelectRelativeItem(-1);
                e.SuppressKeyPress = true;
                e.Handled = true;
                return true;
            }

            if (e.KeyCode == Keys.Down && mediaList.Focused)
            {
                SelectRelativeItem(1);
                e.SuppressKeyPress = true;
                e.Handled = true;
                return true;
            }

            return false;
        }

        private void MoveTab(int offset)
        {
            var values = new[]
            {
                MediaCenterViewMode.Library,
                MediaCenterViewMode.NowPlaying,
                MediaCenterViewMode.NestFiles,
                MediaCenterViewMode.AudioVideo
            };

            var currentIndex = 0;
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == currentMode)
                {
                    currentIndex = i;
                    break;
                }
            }

            currentIndex += offset;
            if (currentIndex < 0)
            {
                currentIndex = values.Length - 1;
            }
            else if (currentIndex >= values.Length)
            {
                currentIndex = 0;
            }

            SetViewMode(values[currentIndex]);
        }

        private void SelectRelativeItem(int offset)
        {
            if (mediaList.Items.Count == 0)
            {
                return;
            }

            var index = mediaList.SelectedIndices.Count > 0 ? mediaList.SelectedIndices[0] : 0;
            index = Math.Max(0, Math.Min(mediaList.Items.Count - 1, index + offset));
            SelectItem(index);
        }

        private void SelectItem(int index)
        {
            if (index < 0 || index >= mediaList.Items.Count)
            {
                return;
            }

            mediaList.SelectedItems.Clear();
            mediaList.Items[index].Selected = true;
            mediaList.Items[index].Focused = true;
            mediaList.EnsureVisible(index);
            UpdatePreview();
        }

        private string BuildStatusText()
        {
            switch (currentMode)
            {
                case MediaCenterViewMode.Library:
                    return mediaList.Items.Count + " items in the full media library.";
                case MediaCenterViewMode.NowPlaying:
                    return mediaList.Items.Count == 0 ? "Nothing is currently playing." : "Now playing view.";
                case MediaCenterViewMode.NestFiles:
                    return mediaList.Items.Count + " NestOS-only files in SAVE.";
                case MediaCenterViewMode.AudioVideo:
                    return mediaList.Items.Count + " audio/video files in SAVE.";
                default:
                    return mediaList.Items.Count + " items.";
            }
        }

        private string BuildEmptyPreviewText()
        {
            switch (currentMode)
            {
                case MediaCenterViewMode.Library:
                    return "Pick a file from the full library.";
                case MediaCenterViewMode.NowPlaying:
                    return "Play an MP3, MP4, or WAV file to populate this section.";
                case MediaCenterViewMode.NestFiles:
                    return "This tab shows .comw and .comu files only.";
                case MediaCenterViewMode.AudioVideo:
                    return "This tab shows MP3, MP4, and WAV files only.";
                default:
                    return "Pick a file from the library.";
            }
        }

        private string BuildMediaPreview(string path)
        {
            var lines = new List<string>();
            lines.Add("This media file can be played from Media Center.");
            lines.Add(string.Empty);
            lines.Add("File: " + path);
            if (!string.IsNullOrEmpty(nowPlayingPath) && string.Equals(nowPlayingPath, path, StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(string.Empty);
                lines.Add("Status: Now Playing");
            }

            return string.Join("\r\n", lines.ToArray());
        }

        private string BuildTextPreview(string text)
        {
            var source = text ?? string.Empty;
            if (source.Length > 2000)
            {
                return source.Substring(0, 2000) + "\r\n\r\n[Preview truncated]";
            }

            return source;
        }

        private string GetSectionLabel(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".comw" || extension == ".comu")
            {
                return "NestOS";
            }

            return "Media";
        }

        private string GetDisplayName(string path)
        {
            var root = SaveSystem.EnsureRoot().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(root.Length);
            }

            return Path.GetFileName(path);
        }

        private void PaintHeader(object sender, PaintEventArgs e)
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(headerPanel.ClientRectangle, Color.FromArgb(76, 127, 215), Color.FromArgb(20, 54, 138), 0.0f))
            {
                e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
            }
        }

        private void PaintNavPanel(object sender, PaintEventArgs e)
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(navPanel.ClientRectangle, Color.FromArgb(191, 226, 135), Color.FromArgb(126, 176, 77), 90.0f))
            {
                e.Graphics.FillRectangle(brush, navPanel.ClientRectangle);
            }
        }
    }
}
