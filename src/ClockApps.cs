using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NestsOS
{
    internal class AnalogClockApp : UserControl
    {
        private readonly Timer timer;
        private readonly Panel facePanel;
        private readonly Label caption;

        public AnalogClockApp()
        {
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            caption = new Label();
            caption.AutoSize = false;
            caption.Font = RetroFont.UiBold();
            caption.TextAlign = ContentAlignment.MiddleCenter;
            caption.SetBounds(12, 10, 300, 18);

            facePanel = new Panel();
            facePanel.SetBounds(24, 38, 280, 280);
            facePanel.BackColor = RetroPalette.WindowBackground;
            facePanel.Paint += PaintClock;

            Controls.Add(caption);
            Controls.Add(facePanel);

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += delegate
            {
                caption.Text = DateTime.Now.ToString("dddd, MMM dd yyyy  HH:mm:ss");
                facePanel.Invalidate();
            };

            caption.Text = DateTime.Now.ToString("dddd, MMM dd yyyy  HH:mm:ss");
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void PaintClock(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(RetroPalette.WindowBackground);

            var bounds = new Rectangle(8, 8, facePanel.Width - 16, facePanel.Height - 16);
            var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            var radius = Math.Min(bounds.Width, bounds.Height) / 2;

            g.FillEllipse(Brushes.White, bounds);
            g.DrawEllipse(new Pen(Color.Black, 2), bounds);

            for (var i = 0; i < 12; i++)
            {
                var angle = (Math.PI / 6.0 * i) - Math.PI / 2.0;
                var outer = new Point(
                    center.X + (int)(Math.Cos(angle) * (radius - 12)),
                    center.Y + (int)(Math.Sin(angle) * (radius - 12)));
                var inner = new Point(
                    center.X + (int)(Math.Cos(angle) * (radius - (i % 3 == 0 ? 30 : 22))),
                    center.Y + (int)(Math.Sin(angle) * (radius - (i % 3 == 0 ? 30 : 22))));
                g.DrawLine(new Pen(Color.Black, i % 3 == 0 ? 2 : 1), inner, outer);
            }

            var now = DateTime.Now;
            DrawHand(g, center, radius - 72, (now.Hour % 12 + (now.Minute / 60.0)) * 30.0, 5, Color.Black);
            DrawHand(g, center, radius - 48, (now.Minute + (now.Second / 60.0)) * 6.0, 3, Color.Black);
            DrawHand(g, center, radius - 32, now.Second * 6.0, 1, Color.Red);
            g.FillEllipse(Brushes.Black, center.X - 4, center.Y - 4, 8, 8);
        }

        private void DrawHand(Graphics g, Point center, int length, double degrees, int thickness, Color color)
        {
            var radians = (Math.PI / 180.0 * degrees) - Math.PI / 2.0;
            var end = new Point(
                center.X + (int)(Math.Cos(radians) * length),
                center.Y + (int)(Math.Sin(radians) * length));

            using (var pen = new Pen(color, thickness))
            {
                g.DrawLine(pen, center, end);
            }
        }
    }

    internal class DigitalClockApp : UserControl
    {
        private readonly Timer timer;
        private readonly Label timeLabel;
        private readonly Label dateLabel;

        public DigitalClockApp()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.Black;

            timeLabel = new Label();
            timeLabel.ForeColor = Color.Lime;
            timeLabel.BackColor = Color.Black;
            timeLabel.Font = new Font("Consolas", 30.0f, FontStyle.Bold, GraphicsUnit.Point);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.SetBounds(18, 34, 360, 56);

            dateLabel = new Label();
            dateLabel.ForeColor = Color.White;
            dateLabel.BackColor = Color.Black;
            dateLabel.Font = RetroFont.UiBold();
            dateLabel.TextAlign = ContentAlignment.MiddleCenter;
            dateLabel.SetBounds(18, 98, 360, 20);

            Controls.Add(timeLabel);
            Controls.Add(dateLabel);

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += delegate { UpdateClock(); };
            UpdateClock();
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            timeLabel.Text = now.ToString("HH:mm:ss");
            dateLabel.Text = now.ToString("dddd, MMMM dd yyyy");
        }
    }
}
