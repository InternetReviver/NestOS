using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class ChessApp : UserControl
    {
        private readonly Label statusLabel;
        private readonly Button[,] squares;
        private readonly char[,] board;
        private bool whiteTurn;
        private Point selected;
        private List<Point> legalMoves;
        private bool gameOver;

        public ChessApp()
        {
            squares = new Button[8, 8];
            board = new char[8, 8];
            legalMoves = new List<Point>();
            selected = new Point(-1, -1);

            BackColor = RetroPalette.WindowBackground;
            Dock = DockStyle.Fill;

            var toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 40;

            var newGameButton = new RetroButton();
            newGameButton.Text = "New Game";
            newGameButton.SetBounds(6, 8, 88, 24);
            newGameButton.Click += delegate { StartGame(); };

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.SetBounds(108, 10, 420, 18);
            statusLabel.Font = RetroFont.UiBold();

            toolbar.Controls.Add(newGameButton);
            toolbar.Controls.Add(statusLabel);

            var grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 8;
            grid.RowCount = 8;
            grid.Padding = new Padding(12);

            for (var i = 0; i < 8; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5f));
            }

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    var square = new Button();
                    square.Dock = DockStyle.Fill;
                    square.Margin = new Padding(0);
                    square.Font = RetroFont.UiBold();
                    square.Tag = new Point(x, y);
                    square.Click += HandleSquareClick;
                    squares[x, y] = square;
                    grid.Controls.Add(square, x, y);
                }
            }

            Controls.Add(grid);
            Controls.Add(toolbar);

            StartGame();
        }

        private void StartGame()
        {
            var first = "rnbqkbnr";
            var pawns = "pppppppp";

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    board[x, y] = '.';
                }
            }

            for (var i = 0; i < 8; i++)
            {
                board[i, 0] = first[i];
                board[i, 1] = pawns[i];
                board[i, 6] = char.ToUpperInvariant(pawns[i]);
                board[i, 7] = char.ToUpperInvariant(first[i]);
            }

            whiteTurn = true;
            gameOver = false;
            selected = new Point(-1, -1);
            legalMoves.Clear();
            UpdateStatus();
            RefreshBoard();
        }

        private void HandleSquareClick(object sender, EventArgs e)
        {
            if (gameOver)
            {
                return;
            }

            var point = (Point)((Control)sender).Tag;
            var piece = board[point.X, point.Y];
            var ownPiece = piece != '.' && IsWhite(piece) == whiteTurn;

            if (selected.X >= 0 && ContainsMove(point))
            {
                ExecuteMove(selected, point);
                return;
            }

            if (ownPiece)
            {
                selected = point;
                legalMoves = GetLegalMoves(point.X, point.Y);
            }
            else
            {
                selected = new Point(-1, -1);
                legalMoves.Clear();
            }

            RefreshBoard();
        }

        private void ExecuteMove(Point from, Point to)
        {
            var moving = board[from.X, from.Y];
            var captured = board[to.X, to.Y];

            board[to.X, to.Y] = moving;
            board[from.X, from.Y] = '.';

            if (moving == 'P' && to.Y == 0)
            {
                board[to.X, to.Y] = 'Q';
            }
            else if (moving == 'p' && to.Y == 7)
            {
                board[to.X, to.Y] = 'q';
            }

            selected = new Point(-1, -1);
            legalMoves.Clear();

            if (captured == 'k' || captured == 'K')
            {
                gameOver = true;
                statusLabel.Text = whiteTurn ? "White wins." : "Black wins.";
                RefreshBoard();
                return;
            }

            whiteTurn = !whiteTurn;

            if (!HasAnyLegalMove(whiteTurn))
            {
                gameOver = true;
                if (IsInCheck(whiteTurn))
                {
                    statusLabel.Text = whiteTurn ? "Checkmate. Black wins." : "Checkmate. White wins.";
                }
                else
                {
                    statusLabel.Text = "Stalemate.";
                }
            }
            else
            {
                UpdateStatus();
            }

            RefreshBoard();
        }

        private void UpdateStatus()
        {
            var text = whiteTurn ? "White to move" : "Black to move";
            if (IsInCheck(whiteTurn))
            {
                text += " - check";
            }

            statusLabel.Text = text;
        }

        private void RefreshBoard()
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    var button = squares[x, y];
                    var light = ((x + y) % 2) == 0;
                    button.BackColor = light ? Color.FromArgb(240, 217, 181) : Color.FromArgb(181, 136, 99);
                    button.ForeColor = IsWhite(board[x, y]) ? Color.White : Color.Black;
                    button.Text = GetPieceLabel(board[x, y]);

                    if (selected.X == x && selected.Y == y)
                    {
                        button.BackColor = Color.Gold;
                    }
                    else if (ContainsMove(new Point(x, y)))
                    {
                        button.BackColor = Color.LightGreen;
                    }
                }
            }
        }

        private bool ContainsMove(Point point)
        {
            for (var i = 0; i < legalMoves.Count; i++)
            {
                if (legalMoves[i] == point)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyLegalMove(bool white)
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    if (board[x, y] != '.' && IsWhite(board[x, y]) == white && GetLegalMoves(x, y).Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private List<Point> GetLegalMoves(int x, int y)
        {
            var pseudo = GetPseudoMoves(x, y, false);
            var legal = new List<Point>();

            for (var i = 0; i < pseudo.Count; i++)
            {
                var target = pseudo[i];
                var fromPiece = board[x, y];
                var toPiece = board[target.X, target.Y];

                board[target.X, target.Y] = fromPiece;
                board[x, y] = '.';
                var inCheck = IsInCheck(IsWhite(fromPiece));
                board[x, y] = fromPiece;
                board[target.X, target.Y] = toPiece;

                if (!inCheck)
                {
                    legal.Add(target);
                }
            }

            return legal;
        }

        private bool IsInCheck(bool white)
        {
            var king = white ? 'K' : 'k';
            var kingPoint = new Point(-1, -1);

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    if (board[x, y] == king)
                    {
                        kingPoint = new Point(x, y);
                        break;
                    }
                }
            }

            if (kingPoint.X < 0)
            {
                return true;
            }

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    var piece = board[x, y];
                    if (piece != '.' && IsWhite(piece) != white)
                    {
                        var attacks = GetPseudoMoves(x, y, true);
                        for (var i = 0; i < attacks.Count; i++)
                        {
                            if (attacks[i] == kingPoint)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private List<Point> GetPseudoMoves(int x, int y, bool attackOnly)
        {
            var moves = new List<Point>();
            var piece = board[x, y];
            if (piece == '.')
            {
                return moves;
            }

            var white = IsWhite(piece);
            var lower = char.ToLowerInvariant(piece);

            if (lower == 'p')
            {
                var direction = white ? -1 : 1;
                var startRow = white ? 6 : 1;
                var nextY = y + direction;

                if (InBounds(x - 1, nextY) && IsEnemy(x - 1, nextY, white))
                {
                    moves.Add(new Point(x - 1, nextY));
                }

                if (InBounds(x + 1, nextY) && IsEnemy(x + 1, nextY, white))
                {
                    moves.Add(new Point(x + 1, nextY));
                }

                if (!attackOnly && InBounds(x, nextY) && board[x, nextY] == '.')
                {
                    moves.Add(new Point(x, nextY));
                    var jumpY = y + (direction * 2);
                    if (y == startRow && board[x, jumpY] == '.')
                    {
                        moves.Add(new Point(x, jumpY));
                    }
                }
            }
            else if (lower == 'n')
            {
                AddKnightMove(moves, x, y, -2, -1, white);
                AddKnightMove(moves, x, y, -2, 1, white);
                AddKnightMove(moves, x, y, -1, -2, white);
                AddKnightMove(moves, x, y, -1, 2, white);
                AddKnightMove(moves, x, y, 1, -2, white);
                AddKnightMove(moves, x, y, 1, 2, white);
                AddKnightMove(moves, x, y, 2, -1, white);
                AddKnightMove(moves, x, y, 2, 1, white);
            }
            else if (lower == 'b')
            {
                AddSlides(moves, x, y, 1, 1, white);
                AddSlides(moves, x, y, 1, -1, white);
                AddSlides(moves, x, y, -1, 1, white);
                AddSlides(moves, x, y, -1, -1, white);
            }
            else if (lower == 'r')
            {
                AddSlides(moves, x, y, 1, 0, white);
                AddSlides(moves, x, y, -1, 0, white);
                AddSlides(moves, x, y, 0, 1, white);
                AddSlides(moves, x, y, 0, -1, white);
            }
            else if (lower == 'q')
            {
                AddSlides(moves, x, y, 1, 0, white);
                AddSlides(moves, x, y, -1, 0, white);
                AddSlides(moves, x, y, 0, 1, white);
                AddSlides(moves, x, y, 0, -1, white);
                AddSlides(moves, x, y, 1, 1, white);
                AddSlides(moves, x, y, 1, -1, white);
                AddSlides(moves, x, y, -1, 1, white);
                AddSlides(moves, x, y, -1, -1, white);
            }
            else if (lower == 'k')
            {
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        var nx = x + offsetX;
                        var ny = y + offsetY;
                        if (InBounds(nx, ny) && !IsOwn(nx, ny, white))
                        {
                            moves.Add(new Point(nx, ny));
                        }
                    }
                }
            }

            return moves;
        }

        private void AddKnightMove(List<Point> moves, int x, int y, int offsetX, int offsetY, bool white)
        {
            var nx = x + offsetX;
            var ny = y + offsetY;
            if (InBounds(nx, ny) && !IsOwn(nx, ny, white))
            {
                moves.Add(new Point(nx, ny));
            }
        }

        private void AddSlides(List<Point> moves, int x, int y, int stepX, int stepY, bool white)
        {
            var nx = x + stepX;
            var ny = y + stepY;

            while (InBounds(nx, ny))
            {
                if (board[nx, ny] == '.')
                {
                    moves.Add(new Point(nx, ny));
                }
                else
                {
                    if (IsWhite(board[nx, ny]) != white)
                    {
                        moves.Add(new Point(nx, ny));
                    }

                    break;
                }

                nx += stepX;
                ny += stepY;
            }
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && x < 8 && y >= 0 && y < 8;
        }

        private bool IsOwn(int x, int y, bool white)
        {
            return board[x, y] != '.' && IsWhite(board[x, y]) == white;
        }

        private bool IsEnemy(int x, int y, bool white)
        {
            return board[x, y] != '.' && IsWhite(board[x, y]) != white;
        }

        private bool IsWhite(char piece)
        {
            return piece != '.' && char.IsUpper(piece);
        }

        private string GetPieceLabel(char piece)
        {
            switch (piece)
            {
                case 'P': return "WP";
                case 'R': return "WR";
                case 'N': return "WN";
                case 'B': return "WB";
                case 'Q': return "WQ";
                case 'K': return "WK";
                case 'p': return "BP";
                case 'r': return "BR";
                case 'n': return "BN";
                case 'b': return "BB";
                case 'q': return "BQ";
                case 'k': return "BK";
                default: return string.Empty;
            }
        }
    }
}
