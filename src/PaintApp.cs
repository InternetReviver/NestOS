using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace NestsOS
{
    internal class PaintApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly PaintSurface canvas;
        private readonly TextBox fileNameBox;
        private string currentPath;

        public PaintApp(IDesktopHost host, string path)
        {
            this.host = host;
            BackColor = RetroPalette.WindowBackground;
            Dock = DockStyle.Fill;

            var toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 72;
            toolbar.BackColor = RetroPalette.WindowBackground;

            var newButton = CreateButton("New", 6, 6);
            newButton.Click += delegate
            {
                currentPath = null;
                fileNameBox.Text = "canvas";
                canvas.ClearSurface();
            };

            var saveButton = CreateButton("Save", 84, 6);
            saveButton.Click += delegate { SaveCanvas(); };

            var filesButton = CreateButton("Files", 162, 6);
            filesButton.Click += delegate { host.OpenFileCabinet(); };

            var sizeLabel = new Label();
            sizeLabel.Text = "Brush:";
            sizeLabel.SetBounds(246, 10, 38, 18);

            var sizeBox = new ComboBox();
            sizeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sizeBox.SetBounds(288, 8, 76, 22);
            sizeBox.Items.AddRange(new object[] { "1", "2", "4", "8", "12", "18" });
            sizeBox.SelectedIndex = 2;
            sizeBox.SelectedIndexChanged += delegate
            {
                canvas.BrushSize = Convert.ToInt32(sizeBox.SelectedItem);
            };

            var fileLabel = new Label();
            fileLabel.Text = "File:";
            fileLabel.SetBounds(378, 10, 28, 18);

            fileNameBox = new TextBox();
            fileNameBox.SetBounds(410, 8, 180, 22);

            toolbar.Controls.Add(newButton);
            toolbar.Controls.Add(saveButton);
            toolbar.Controls.Add(filesButton);
            toolbar.Controls.Add(sizeLabel);
            toolbar.Controls.Add(sizeBox);
            toolbar.Controls.Add(fileLabel);
            toolbar.Controls.Add(fileNameBox);

            var colors = new Color[]
            {
                Color.Black, Color.White, Color.Red, Color.Blue, Color.Green, Color.Yellow,
                Color.Orange, Color.Purple, Color.Brown, Color.Gray, Color.DeepPink, Color.Cyan
            };

            var left = 6;
            for (var i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                var swatch = new Panel();
                swatch.BackColor = color;
                swatch.BorderStyle = BorderStyle.FixedSingle;
                swatch.SetBounds(left, 40, 22, 22);
                swatch.Cursor = Cursors.Hand;
                swatch.Click += delegate { canvas.CurrentColor = color; };
                toolbar.Controls.Add(swatch);
                left += 26;
            }

            var scroll = new Panel();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.BackColor = Color.FromArgb(96, 96, 96);

            canvas = new PaintSurface();
            canvas.Location = new Point(10, 10);
            scroll.Controls.Add(canvas);

            Controls.Add(scroll);
            Controls.Add(toolbar);

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                LoadCanvas(path);
            }
            else
            {
                fileNameBox.Text = "canvas";
            }
        }

        private RetroButton CreateButton(string text, int left, int top)
        {
            var button = new RetroButton();
            button.Text = text;
            button.SetBounds(left, top, 72, 24);
            return button;
        }

        private void LoadCanvas(string path)
        {
            currentPath = path;
            fileNameBox.Text = Path.GetFileNameWithoutExtension(path);
            canvas.LoadSurface(path);
        }

        private void SaveCanvas()
        {
            var requestedName = fileNameBox.Text;
            if (requestedName.Trim().Length == 0)
            {
                requestedName = PromptDialog.ShowDialog(FindForm(), "Save Paint File", "Enter a file name:", "canvas");
                if (requestedName == null)
                {
                    return;
                }
            }

            currentPath = SaveSystem.BuildPath(requestedName, ".bmp");
            canvas.SaveSurface(currentPath);
            fileNameBox.Text = Path.GetFileNameWithoutExtension(currentPath);
            host.ShowMessage("Paint", "Saved to " + currentPath);
        }
    }

    internal class PaintSurface : Control
    {
        private Bitmap surface;
        private bool drawing;
        private Point previousPoint;

        public PaintSurface()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Size = new Size(1024, 720);
            CurrentColor = Color.Black;
            BrushSize = 4;
            ResetBitmap();
        }

        public Color CurrentColor { get; set; }

        public int BrushSize { get; set; }

        public void ClearSurface()
        {
            ResetBitmap();
            Invalidate();
        }

        public void SaveSurface(string path)
        {
            surface.Save(path, ImageFormat.Bmp);
        }

        public void LoadSurface(string path)
        {
            if (surface != null)
            {
                surface.Dispose();
            }

            using (var bmp = new Bitmap(path))
            {
                surface = new Bitmap(Math.Max(bmp.Width, Width), Math.Max(bmp.Height, Height));
                using (var g = Graphics.FromImage(surface))
                {
                    g.Clear(Color.White);
                    g.DrawImageUnscaled(bmp, 0, 0);
                }
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.DarkGray, ClientRectangle);
            e.Graphics.DrawImageUnscaled(surface, 0, 0);
            ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Sunken);
            base.OnPaint(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            drawing = true;
            previousPoint = e.Location;
            DrawPoint(e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (drawing)
            {
                using (var g = Graphics.FromImage(surface))
                using (var pen = new Pen(CurrentColor, BrushSize))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawLine(pen, previousPoint, e.Location);
                }

                previousPoint = e.Location;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            drawing = false;
            base.OnMouseUp(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && surface != null)
            {
                surface.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ResetBitmap()
        {
            if (surface != null)
            {
                surface.Dispose();
            }

            surface = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(surface))
            {
                g.Clear(Color.White);
            }
        }

        private void DrawPoint(Point point)
        {
            using (var g = Graphics.FromImage(surface))
            using (var brush = new SolidBrush(CurrentColor))
            {
                var size = Math.Max(BrushSize, 1);
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            }

            Invalidate();
        }
    }
}
