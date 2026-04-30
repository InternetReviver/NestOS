using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class MahjongApp : UserControl
    {
        private readonly Dictionary<Point, string> tiles;
        private readonly Dictionary<Point, Button> buttons;
        private readonly Label statusLabel;
        private readonly Random random;
        private Point selected;

        public MahjongApp(IDesktopHost host)
        {
            tiles = new Dictionary<Point, string>();
            buttons = new Dictionary<Point, Button>();
            random = new Random();
            selected = new Point(-1, -1);

            BackColor = RetroPalette.WindowBackground;
            Dock = DockStyle.Fill;

            var toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 40;

            var newGameButton = new RetroButton();
            newGameButton.Text = "New Layout";
            newGameButton.SetBounds(6, 8, 92, 24);
            newGameButton.Click += delegate { BuildGame(); };

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.SetBounds(112, 10, 520, 20);
            statusLabel.Font = RetroFont.UiBold();

            toolbar.Controls.Add(newGameButton);
            toolbar.Controls.Add(statusLabel);

            var board = new Panel();
            board.Dock = DockStyle.Fill;
            board.BackColor = Color.FromArgb(16, 100, 88);

            Controls.Add(board);
            Controls.Add(toolbar);

            var positions = GetLayout();
            for (var i = 0; i < positions.Count; i++)
            {
                var point = positions[i];
                var button = new Button();
                button.Font = RetroFont.UiBold();
                button.Size = new Size(54, 36);
                button.Location = new Point(24 + (point.X * 56), 24 + (point.Y * 40));
                button.Tag = point;
                button.Click += HandleTileClick;
                buttons[point] = button;
                board.Controls.Add(button);
            }

            BuildGame();
        }

        private void BuildGame()
        {
            selected = new Point(-1, -1);
            statusLabel.Text = string.Empty;
            tiles.Clear();

            var positions = GetLayout();
            var remaining = new List<Point>(positions);
            var symbols = BuildSymbols(positions.Count / 2);

            for (var pairIndex = 0; pairIndex < symbols.Count; pairIndex++)
            {
                var free = GetFreePositions(remaining);
                if (free.Count < 2)
                {
                    free = new List<Point>(remaining);
                }

                var firstIndex = random.Next(free.Count);
                var first = free[firstIndex];
                free.RemoveAt(firstIndex);
                remaining.Remove(first);

                free = GetFreePositions(remaining);
                if (free.Count == 0)
                {
                    free = new List<Point>(remaining);
                }

                var second = free[random.Next(free.Count)];
                remaining.Remove(second);

                tiles[first] = symbols[pairIndex];
                tiles[second] = symbols[pairIndex];
            }

            RefreshBoard();
        }

        private void HandleTileClick(object sender, EventArgs e)
        {
            var point = (Point)((Control)sender).Tag;
            if (!tiles.ContainsKey(point))
            {
                return;
            }

            if (!IsFree(point))
            {
                statusLabel.Text = "That tile is blocked. Pick an edge tile.";
                return;
            }

            if (selected.X < 0)
            {
                selected = point;
                statusLabel.Text = "Selected " + tiles[point] + ". Pick a matching free tile.";
                RefreshBoard();
                return;
            }

            if (selected == point)
            {
                selected = new Point(-1, -1);
                statusLabel.Text = "Selection cleared.";
                RefreshBoard();
                return;
            }

            if (tiles[selected] == tiles[point])
            {
                tiles.Remove(selected);
                tiles.Remove(point);
                selected = new Point(-1, -1);

                if (tiles.Count == 0)
                {
                    statusLabel.Text = "Board cleared. You win.";
                }
                else
                {
                    statusLabel.Text = (tiles.Count / 2).ToString() + " pairs left.";
                }
            }
            else
            {
                selected = point;
                statusLabel.Text = "No match. New tile selected.";
            }

            RefreshBoard();
        }

        private void RefreshBoard()
        {
            var positions = GetLayout();
            for (var i = 0; i < positions.Count; i++)
            {
                var point = positions[i];
                var button = buttons[point];
                if (tiles.ContainsKey(point))
                {
                    button.Visible = true;
                    button.Text = tiles[point];
                    button.BackColor = IsFree(point) ? Color.Beige : Color.Silver;
                    button.ForeColor = Color.Black;

                    if (selected == point)
                    {
                        button.BackColor = Color.Gold;
                    }
                }
                else
                {
                    button.Visible = false;
                }
            }

            if (tiles.Count > 0 && statusLabel.Text.Length == 0)
            {
                statusLabel.Text = (tiles.Count / 2).ToString() + " pairs left.";
            }
        }

        private List<Point> GetLayout()
        {
            var layout = new List<Point>();
            var rows = new string[]
            {
                "....XXXX....",
                "..XXXXXXXX..",
                ".XXXXXXXXXX.",
                "XXXXXXXXXXXX",
                ".XXXXXXXXXX.",
                "..XXXXXXXX..",
                "....XXXX...."
            };

            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows[y].Length; x++)
                {
                    if (rows[y][x] == 'X')
                    {
                        layout.Add(new Point(x, y));
                    }
                }
            }

            return layout;
        }

        private List<string> BuildSymbols(int pairCount)
        {
            var symbols = new List<string>();
            var groups = new string[] { "B", "C", "D", "E", "F", "G", "H" };
            var number = 1;
            var groupIndex = 0;

            for (var i = 0; i < pairCount; i++)
            {
                symbols.Add(groups[groupIndex] + number.ToString());
                groupIndex++;
                if (groupIndex >= groups.Length)
                {
                    groupIndex = 0;
                    number++;
                }
            }

            for (var i = symbols.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                var temp = symbols[i];
                symbols[i] = symbols[swap];
                symbols[swap] = temp;
            }

            return symbols;
        }

        private List<Point> GetFreePositions(List<Point> remaining)
        {
            var free = new List<Point>();

            for (var i = 0; i < remaining.Count; i++)
            {
                var point = remaining[i];
                if (IsFree(point, remaining))
                {
                    free.Add(point);
                }
            }

            return free;
        }

        private bool IsFree(Point point)
        {
            return IsFree(point, new List<Point>(tiles.Keys));
        }

        private bool IsFree(Point point, List<Point> remaining)
        {
            var left = new Point(point.X - 1, point.Y);
            var right = new Point(point.X + 1, point.Y);

            return !remaining.Contains(left) || !remaining.Contains(right);
        }
    }
}
