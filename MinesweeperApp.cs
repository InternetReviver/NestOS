using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class MinesweeperApp : UserControl
    {
        private const int Columns = 9;
        private const int Rows = 9;
        private const int MineCount = 10;

        private readonly IDesktopHost host;
        private readonly MineCellButton[,] cells;
        private readonly bool[,] mines;
        private readonly bool[,] revealed;
        private readonly bool[,] flagged;
        private readonly Label minesLabel;
        private readonly Label statusLabel;
        private readonly Button resetButton;
        private readonly Random random;
        private bool firstReveal;
        private bool gameOver;

        public MinesweeperApp(IDesktopHost host)
        {
            this.host = host;
            cells = new MineCellButton[Columns, Rows];
            mines = new bool[Columns, Rows];
            revealed = new bool[Columns, Rows];
            flagged = new bool[Columns, Rows];
            random = new Random();
            firstReveal = true;

            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            var header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 42;

            minesLabel = new Label();
            minesLabel.Font = RetroFont.UiBold();
            minesLabel.TextAlign = ContentAlignment.MiddleLeft;
            minesLabel.SetBounds(12, 12, 90, 18);

            resetButton = new Button();
            resetButton.Text = "New";
            resetButton.SetBounds(108, 8, 56, 26);
            resetButton.Image = LegacyIconProvider.TryLoadIconByFileName("SCHDP005.PNG", new Size(18, 18));
            resetButton.ImageAlign = ContentAlignment.MiddleLeft;
            resetButton.TextAlign = ContentAlignment.MiddleRight;
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Click += delegate { ResetGame(); };

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.SetBounds(178, 12, 180, 18);

            header.Controls.Add(minesLabel);
            header.Controls.Add(resetButton);
            header.Controls.Add(statusLabel);

            var boardHost = new Panel();
            boardHost.Dock = DockStyle.Fill;
            boardHost.Padding = new Padding(12);

            var board = new TableLayoutPanel();
            board.ColumnCount = Columns;
            board.RowCount = Rows;
            board.Margin = new Padding(0);
            board.Padding = new Padding(0);
            board.BackColor = RetroPalette.DarkShadow;
            board.Size = new Size(Columns * 26, Rows * 26);

            for (var column = 0; column < Columns; column++)
            {
                board.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
            }

            for (var row = 0; row < Rows; row++)
            {
                board.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            }

            for (var y = 0; y < Rows; y++)
            {
                for (var x = 0; x < Columns; x++)
                {
                    var cell = new MineCellButton();
                    cell.Margin = new Padding(1);
                    cell.Dock = DockStyle.Fill;
                    cell.X = x;
                    cell.Y = y;
                    cell.Font = RetroFont.UiBold();
                    cell.MouseUp += HandleCellMouseUp;
                    cells[x, y] = cell;
                    board.Controls.Add(cell, x, y);
                }
            }

            boardHost.Controls.Add(board);
            Controls.Add(boardHost);
            Controls.Add(header);

            ResetGame();
        }

        private void ResetGame()
        {
            firstReveal = true;
            gameOver = false;

            for (var y = 0; y < Rows; y++)
            {
                for (var x = 0; x < Columns; x++)
                {
                    mines[x, y] = false;
                    revealed[x, y] = false;
                    flagged[x, y] = false;

                    var cell = cells[x, y];
                    cell.Enabled = true;
                    cell.Text = string.Empty;
                    cell.BackColor = RetroPalette.ButtonFace;
                    cell.ForeColor = Color.Black;
                }
            }

            statusLabel.Text = "Clear the field.";
            UpdateMineCounter();
        }

        private void HandleCellMouseUp(object sender, MouseEventArgs e)
        {
            if (gameOver)
            {
                return;
            }

            var cell = sender as MineCellButton;
            if (cell == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                ToggleFlag(cell.X, cell.Y);
            }
            else if (e.Button == MouseButtons.Left)
            {
                RevealCell(cell.X, cell.Y);
            }
        }

        private void ToggleFlag(int x, int y)
        {
            if (revealed[x, y])
            {
                return;
            }

            flagged[x, y] = !flagged[x, y];
            cells[x, y].Text = flagged[x, y] ? "F" : string.Empty;
            cells[x, y].ForeColor = Color.Red;
            UpdateMineCounter();
        }

        private void RevealCell(int startX, int startY)
        {
            if (flagged[startX, startY] || revealed[startX, startY])
            {
                return;
            }

            if (firstReveal)
            {
                PlaceMines(startX, startY);
                firstReveal = false;
            }

            if (mines[startX, startY])
            {
                ShowMine(startX, startY, true);
                RevealAllMines();
                gameOver = true;
                statusLabel.Text = "Boom.";
                host.ShowMessage("Minesweeper", "You hit a mine.");
                return;
            }

            var queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));

            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                var x = point.X;
                var y = point.Y;
                if (revealed[x, y] || flagged[x, y])
                {
                    continue;
                }

                revealed[x, y] = true;
                var adjacent = CountAdjacentMines(x, y);
                PaintSafeCell(x, y, adjacent);

                if (adjacent != 0)
                {
                    continue;
                }

                for (var neighborY = Math.Max(0, y - 1); neighborY <= Math.Min(Rows - 1, y + 1); neighborY++)
                {
                    for (var neighborX = Math.Max(0, x - 1); neighborX <= Math.Min(Columns - 1, x + 1); neighborX++)
                    {
                        if (!revealed[neighborX, neighborY] && !mines[neighborX, neighborY])
                        {
                            queue.Enqueue(new Point(neighborX, neighborY));
                        }
                    }
                }
            }

            if (CheckWin())
            {
                gameOver = true;
                statusLabel.Text = "You win.";
                host.ShowMessage("Minesweeper", "Board cleared.");
            }
            else
            {
                statusLabel.Text = "Keep going.";
            }
        }

        private void PlaceMines(int safeX, int safeY)
        {
            var placed = 0;
            while (placed < MineCount)
            {
                var x = random.Next(Columns);
                var y = random.Next(Rows);
                if (mines[x, y] || (x == safeX && y == safeY))
                {
                    continue;
                }

                mines[x, y] = true;
                placed++;
            }
        }

        private int CountAdjacentMines(int x, int y)
        {
            var count = 0;
            for (var neighborY = Math.Max(0, y - 1); neighborY <= Math.Min(Rows - 1, y + 1); neighborY++)
            {
                for (var neighborX = Math.Max(0, x - 1); neighborX <= Math.Min(Columns - 1, x + 1); neighborX++)
                {
                    if (neighborX == x && neighborY == y)
                    {
                        continue;
                    }

                    if (mines[neighborX, neighborY])
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void PaintSafeCell(int x, int y, int adjacent)
        {
            var cell = cells[x, y];
            cell.Enabled = false;
            cell.BackColor = Color.FromArgb(224, 224, 224);
            cell.Text = adjacent == 0 ? string.Empty : adjacent.ToString();
            cell.ForeColor = GetNumberColor(adjacent);
        }

        private void RevealAllMines()
        {
            for (var y = 0; y < Rows; y++)
            {
                for (var x = 0; x < Columns; x++)
                {
                    if (mines[x, y])
                    {
                        ShowMine(x, y, false);
                    }
                    else if (flagged[x, y] && !revealed[x, y])
                    {
                        cells[x, y].Text = "X";
                        cells[x, y].ForeColor = Color.Maroon;
                    }

                    cells[x, y].Enabled = false;
                }
            }
        }

        private void ShowMine(int x, int y, bool exploded)
        {
            var cell = cells[x, y];
            cell.Text = "*";
            cell.ForeColor = exploded ? Color.Yellow : Color.Black;
            cell.BackColor = exploded ? Color.Firebrick : Color.LightGray;
        }

        private bool CheckWin()
        {
            for (var y = 0; y < Rows; y++)
            {
                for (var x = 0; x < Columns; x++)
                {
                    if (!mines[x, y] && !revealed[x, y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void UpdateMineCounter()
        {
            var flagsUsed = 0;
            for (var y = 0; y < Rows; y++)
            {
                for (var x = 0; x < Columns; x++)
                {
                    if (flagged[x, y])
                    {
                        flagsUsed++;
                    }
                }
            }

            minesLabel.Text = "Mines: " + (MineCount - flagsUsed);
        }

        private Color GetNumberColor(int number)
        {
            switch (number)
            {
                case 1:
                    return Color.Blue;
                case 2:
                    return Color.Green;
                case 3:
                    return Color.Red;
                case 4:
                    return Color.Navy;
                case 5:
                    return Color.Maroon;
                case 6:
                    return Color.Teal;
                case 7:
                    return Color.Black;
                case 8:
                    return Color.Gray;
                default:
                    return Color.Black;
            }
        }
    }

    internal class MineCellButton : Button
    {
        public int X;
        public int Y;

        public MineCellButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = RetroPalette.ButtonFace;
            UseVisualStyleBackColor = false;
            TabStop = false;
        }
    }
}
