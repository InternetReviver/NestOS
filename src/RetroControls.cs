using System;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class RetroButton : Button
    {
        public RetroButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = RetroPalette.ButtonFace;
            ForeColor = Color.Black;
            Font = RetroFont.Ui();
            TabStop = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.Clear(BackColor);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Capture && ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                ControlPaint.DrawButton(g, rect, ButtonState.Pushed);
            }
            else
            {
                ControlPaint.DrawButton(g, rect, ButtonState.Normal);
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rect,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal class RetroWindowButton : Control
    {
        private bool pressed;

        public event EventHandler ButtonPressed;

        public RetroWindowButton()
        {
            Size = new Size(18, 18);
            BackColor = RetroPalette.ButtonFace;
            Font = RetroFont.UiBold();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            var invoke = pressed && ClientRectangle.Contains(e.Location);
            pressed = false;
            Invalidate();

            if (invoke && ButtonPressed != null)
            {
                ButtonPressed(this, EventArgs.Empty);
            }

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            ControlPaint.DrawButton(g, new Rectangle(0, 0, Width - 1, Height - 1), pressed ? ButtonState.Pushed : ButtonState.Normal);

            var textRect = new Rectangle(0, pressed ? 1 : 0, Width, Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textRect,
                Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal class RetroWindow : UserControl
    {
        private readonly Panel titleBar;
        private readonly Label titleLabel;
        private readonly Panel contentHost;
        private readonly Control resizeGrip;
        private readonly RetroWindowButton minimizeButton;
        private readonly RetroWindowButton maximizeButton;
        private readonly RetroWindowButton closeButton;
        private bool dragging;
        private bool resizing;
        private Point dragOffset;
        private Point resizeStartScreen;
        private Size resizeStartSize;
        private Rectangle restoreBounds;
        private bool maximized;

        public event EventHandler WindowClosed;
        public event EventHandler WindowMinimized;
        public event EventHandler Activated;

        public RetroWindow()
        {
            BackColor = RetroPalette.WindowBackground;
            Font = RetroFont.Ui();
            Padding = new Padding(3, 22, 3, 3);
            MinimumSize = new Size(260, 200);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            titleBar = new Panel();
            titleBar.SetBounds(3, 3, 200, 18);
            titleBar.BackColor = RetroPalette.TitleBlue;
            titleBar.MouseDown += BeginDrag;
            titleBar.MouseMove += DragWindow;
            titleBar.MouseUp += EndDrag;
            titleBar.Click += FocusWindow;

            titleLabel = new Label();
            titleLabel.AutoSize = false;
            titleLabel.ForeColor = Color.White;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.Font = RetroFont.Title();
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.SetBounds(4, 1, 120, 16);
            titleLabel.MouseDown += BeginDrag;
            titleLabel.MouseMove += DragWindow;
            titleLabel.MouseUp += EndDrag;
            titleLabel.Click += FocusWindow;

            minimizeButton = new RetroWindowButton();
            minimizeButton.Text = "-";
            minimizeButton.ButtonPressed += delegate { if (WindowMinimized != null) WindowMinimized(this, EventArgs.Empty); };

            maximizeButton = new RetroWindowButton();
            maximizeButton.Text = "□";
            maximizeButton.ButtonPressed += delegate { ToggleMaximize(); };

            closeButton = new RetroWindowButton();
            closeButton.Text = "X";
            closeButton.ButtonPressed += delegate { if (WindowClosed != null) WindowClosed(this, EventArgs.Empty); };

            titleBar.Controls.Add(titleLabel);
            Controls.Add(titleBar);
            Controls.Add(minimizeButton);
            Controls.Add(maximizeButton);
            Controls.Add(closeButton);

            contentHost = new Panel();
            contentHost.BackColor = RetroPalette.WindowBackground;
            contentHost.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(contentHost);

            resizeGrip = new Control();
            resizeGrip.Size = new Size(16, 16);
            resizeGrip.BackColor = RetroPalette.WindowBackground;
            resizeGrip.Cursor = Cursors.SizeNWSE;
            resizeGrip.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            resizeGrip.MouseDown += BeginResize;
            resizeGrip.MouseMove += ContinueResize;
            resizeGrip.MouseUp += EndResize;
            resizeGrip.Paint += PaintResizeGrip;
            Controls.Add(resizeGrip);
            resizeGrip.BringToFront();

            Resize += delegate { LayoutChrome(); };
            MouseDown += FocusWindow;
        }

        public string Title
        {
            get { return titleLabel.Text; }
            set { titleLabel.Text = value; }
        }

        public Control Content
        {
            get
            {
                if (contentHost.Controls.Count == 0)
                {
                    return null;
                }

                return contentHost.Controls[0];
            }
            set
            {
                contentHost.Controls.Clear();
                if (value != null)
                {
                    value.Dock = DockStyle.Fill;
                    contentHost.Controls.Add(value);
                }
            }
        }

        public void SetActive(bool isActive)
        {
            titleBar.BackColor = isActive ? RetroPalette.TitleBlue : RetroPalette.TitleInactive;
        }

        public void RestoreFromMinimize()
        {
            Visible = true;
            BringToFront();
            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }
        }

        public void CloseWindow()
        {
            if (WindowClosed != null)
            {
                WindowClosed(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            ControlPaint.DrawBorder(
                e.Graphics,
                new Rectangle(0, 0, Width - 1, Height - 1),
                RetroPalette.Highlight,
                2,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                2,
                ButtonBorderStyle.Solid,
                RetroPalette.Highlight,
                2,
                ButtonBorderStyle.Solid,
                RetroPalette.DarkShadow,
                2,
                ButtonBorderStyle.Solid);

            base.OnPaint(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }

            base.OnMouseDown(e);
        }

        private void LayoutChrome()
        {
            titleBar.SetBounds(4, 4, Width - 8 - 60, 18);
            closeButton.Location = new Point(Width - 22, 4);
            maximizeButton.Location = new Point(Width - 42, 4);
            minimizeButton.Location = new Point(Width - 62, 4);
            contentHost.SetBounds(4, 24, Width - 8, Height - 28);
            resizeGrip.Location = new Point(Width - resizeGrip.Width - 4, Height - resizeGrip.Height - 4);
            resizeGrip.Visible = !maximized;
        }

        private void BeginDrag(object sender, MouseEventArgs e)
        {
            if (maximized)
            {
                return;
            }

            dragging = true;
            dragOffset = e.Location;
            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (!dragging || Parent == null)
            {
                return;
            }

            var previousBounds = Bounds;
            var point = Parent.PointToClient(Cursor.Position);
            Left = Math.Max(0, Math.Min(Parent.ClientSize.Width - Width, point.X - dragOffset.X - 4));
            Top = Math.Max(0, Math.Min(Parent.ClientSize.Height - Height, point.Y - dragOffset.Y - 4));
            RefreshParentSurface(previousBounds);
        }

        private void EndDrag(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void BeginResize(object sender, MouseEventArgs e)
        {
            if (maximized || Parent == null)
            {
                return;
            }

            resizing = true;
            resizeStartScreen = Cursor.Position;
            resizeStartSize = Size;

            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }
        }

        private void ContinueResize(object sender, MouseEventArgs e)
        {
            if (!resizing || Parent == null)
            {
                return;
            }

            var previousBounds = Bounds;
            var cursor = Cursor.Position;
            var deltaX = cursor.X - resizeStartScreen.X;
            var deltaY = cursor.Y - resizeStartScreen.Y;

            var newWidth = Math.Max(MinimumSize.Width, resizeStartSize.Width + deltaX);
            var newHeight = Math.Max(MinimumSize.Height, resizeStartSize.Height + deltaY);

            newWidth = Math.Min(newWidth, Parent.ClientSize.Width - Left);
            newHeight = Math.Min(newHeight, Parent.ClientSize.Height - Top);

            Size = new Size(newWidth, newHeight);
            RefreshParentSurface(previousBounds);
        }

        private void EndResize(object sender, MouseEventArgs e)
        {
            resizing = false;
        }

        private void FocusWindow(object sender, EventArgs e)
        {
            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }
        }

        private void ToggleMaximize()
        {
            if (Parent == null)
            {
                return;
            }

            if (!maximized)
            {
                var previousBounds = Bounds;
                restoreBounds = Bounds;
                Bounds = new Rectangle(0, 0, Parent.ClientSize.Width, Parent.ClientSize.Height);
                maximized = true;
                RefreshParentSurface(previousBounds);
            }
            else
            {
                var previousBounds = Bounds;
                Bounds = restoreBounds;
                maximized = false;
                RefreshParentSurface(previousBounds);
            }

            BringToFront();
            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }
        }

        private void RefreshParentSurface(Rectangle previousBounds)
        {
            if (Parent == null)
            {
                return;
            }

            var dirty = Rectangle.Union(previousBounds, Bounds);
            dirty.Inflate(8, 8);
            Parent.Invalidate(dirty, true);
            Parent.Update();
        }

        private void PaintResizeGrip(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            using (var shadowPen = new Pen(RetroPalette.Shadow))
            using (var lightPen = new Pen(RetroPalette.Highlight))
            {
                e.Graphics.DrawLine(shadowPen, 4, 11, 11, 4);
                e.Graphics.DrawLine(shadowPen, 7, 11, 11, 7);
                e.Graphics.DrawLine(shadowPen, 10, 11, 11, 10);

                e.Graphics.DrawLine(lightPen, 5, 11, 11, 5);
                e.Graphics.DrawLine(lightPen, 8, 11, 11, 8);
            }
        }
    }
}
