using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NestsOS
{
    internal class SolitaireApp : UserControl
    {
        private readonly Label statusLabel;
        private readonly SolitaireBoard board;

        public SolitaireApp(IDesktopHost host)
        {
            BackColor = RetroPalette.WindowBackground;
            Dock = DockStyle.Fill;

            var toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 40;

            var newGameButton = new RetroButton();
            newGameButton.Text = "New Deal";
            newGameButton.SetBounds(6, 8, 88, 24);

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.SetBounds(108, 10, 540, 20);
            statusLabel.Font = RetroFont.UiBold();

            toolbar.Controls.Add(newGameButton);
            toolbar.Controls.Add(statusLabel);

            board = new SolitaireBoard();
            board.Dock = DockStyle.Fill;
            board.StatusChanged += delegate(object sender, string message) { statusLabel.Text = message; };

            newGameButton.Click += delegate { board.StartNewGame(); };

            Controls.Add(board);
            Controls.Add(toolbar);

            board.StartNewGame();
        }
    }

    internal class SolitaireBoard : Control
    {
        private const int CardWidth = 72;
        private const int CardHeight = 96;
        private const int MarginX = 20;
        private const int TopY = 20;
        private const int TableauY = 145;
        private const int ColumnGap = 110;
        private const int FaceDownOffset = 14;
        private const int FaceUpOffset = 22;

        private readonly List<Card> stock;
        private readonly List<Card> waste;
        private readonly List<Card>[] foundations;
        private readonly List<Card>[] tableau;
        private readonly Random random;
        private Selection selection;

        public SolitaireBoard()
        {
            stock = new List<Card>();
            waste = new List<Card>();
            foundations = new List<Card>[4];
            tableau = new List<Card>[7];
            random = new Random();
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            for (var i = 0; i < foundations.Length; i++)
            {
                foundations[i] = new List<Card>();
            }

            for (var i = 0; i < tableau.Length; i++)
            {
                tableau[i] = new List<Card>();
            }
        }

        public event EventHandler<string> StatusChanged;

        public void StartNewGame()
        {
            stock.Clear();
            waste.Clear();
            selection = Selection.None();

            for (var i = 0; i < foundations.Length; i++)
            {
                foundations[i].Clear();
            }

            for (var i = 0; i < tableau.Length; i++)
            {
                tableau[i].Clear();
            }

            var deck = new List<Card>();
            for (var suit = 0; suit < 4; suit++)
            {
                for (var rank = 1; rank <= 13; rank++)
                {
                    deck.Add(new Card(rank, suit, false));
                }
            }

            for (var i = deck.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                var temp = deck[i];
                deck[i] = deck[swap];
                deck[swap] = temp;
            }

            var deckIndex = 0;
            for (var pile = 0; pile < 7; pile++)
            {
                for (var count = 0; count <= pile; count++)
                {
                    var card = deck[deckIndex++];
                    card.FaceUp = count == pile;
                    tableau[pile].Add(card);
                }
            }

            for (; deckIndex < deck.Count; deckIndex++)
            {
                stock.Add(deck[deckIndex]);
            }

            SetStatus("New deal ready.");
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(RetroPalette.FeltGreen);

            DrawPileSlot(g, GetStockRect());
            DrawPileSlot(g, GetWasteRect());
            DrawLabel(g, "Stock", GetStockRect());
            DrawLabel(g, "Waste", GetWasteRect());

            for (var i = 0; i < 4; i++)
            {
                var foundationRect = GetFoundationRect(i);
                DrawPileSlot(g, foundationRect);
                DrawLabel(g, "F" + (i + 1).ToString(), foundationRect);
            }

            if (stock.Count > 0)
            {
                DrawCardBack(g, GetStockRect());
            }

            if (waste.Count > 0)
            {
                DrawCard(g, waste[waste.Count - 1], GetWasteRect(), selection.Kind == SelectionKind.Waste);
            }

            for (var i = 0; i < 4; i++)
            {
                if (foundations[i].Count > 0)
                {
                    DrawCard(g, foundations[i][foundations[i].Count - 1], GetFoundationRect(i), false);
                }
            }

            for (var pile = 0; pile < 7; pile++)
            {
                if (tableau[pile].Count == 0)
                {
                    DrawPileSlot(g, GetTableauSlot(pile));
                    continue;
                }

                for (var index = 0; index < tableau[pile].Count; index++)
                {
                    var rect = GetTableauCardRect(pile, index);
                    var card = tableau[pile][index];
                    var selectedCard = selection.Kind == SelectionKind.Tableau && selection.Pile == pile && index == selection.Index;

                    if (card.FaceUp)
                    {
                        DrawCard(g, card, rect, selectedCard);
                    }
                    else
                    {
                        DrawCardBack(g, rect);
                    }
                }
            }

            base.OnPaint(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (GetStockRect().Contains(e.Location))
            {
                HandleStockClick();
                return;
            }

            if (GetWasteRect().Contains(e.Location))
            {
                HandleWasteClick();
                return;
            }

            for (var i = 0; i < 4; i++)
            {
                if (GetFoundationRect(i).Contains(e.Location))
                {
                    HandleFoundationClick(i);
                    return;
                }
            }

            for (var pile = 0; pile < 7; pile++)
            {
                if (GetTableauSlotBounds(pile).Contains(e.Location))
                {
                    HandleTableauClick(pile, e.Location);
                    return;
                }
            }

            selection = Selection.None();
            Invalidate();
            base.OnMouseDown(e);
        }

        private void HandleStockClick()
        {
            selection = Selection.None();
            if (stock.Count > 0)
            {
                var card = stock[stock.Count - 1];
                stock.RemoveAt(stock.Count - 1);
                card.FaceUp = true;
                waste.Add(card);
                SetStatus("Drew a card.");
            }
            else if (waste.Count > 0)
            {
                for (var i = waste.Count - 1; i >= 0; i--)
                {
                    waste[i].FaceUp = false;
                    stock.Add(waste[i]);
                }

                waste.Clear();
                SetStatus("Recycled the waste pile.");
            }
            else
            {
                SetStatus("No more cards to draw.");
            }

            Invalidate();
        }

        private void HandleWasteClick()
        {
            if (waste.Count == 0)
            {
                return;
            }

            selection = selection.Kind == SelectionKind.Waste ? Selection.None() : Selection.FromWaste();
            SetStatus(selection.Kind == SelectionKind.Waste ? "Waste card selected." : "Selection cleared.");
            Invalidate();
        }

        private void HandleFoundationClick(int foundationIndex)
        {
            if (selection.Kind == SelectionKind.Waste)
            {
                var card = waste[waste.Count - 1];
                if (CanMoveToFoundation(card, foundationIndex))
                {
                    waste.RemoveAt(waste.Count - 1);
                    foundations[foundationIndex].Add(card);
                    selection = Selection.None();
                    AfterMove();
                }
            }
            else if (selection.Kind == SelectionKind.Tableau)
            {
                var card = tableau[selection.Pile][selection.Index];
                if (selection.Index == tableau[selection.Pile].Count - 1 && CanMoveToFoundation(card, foundationIndex))
                {
                    tableau[selection.Pile].RemoveAt(selection.Index);
                    foundations[foundationIndex].Add(card);
                    RevealTableauTop(selection.Pile);
                    selection = Selection.None();
                    AfterMove();
                }
            }
        }

        private void HandleTableauClick(int pile, Point location)
        {
            var index = HitTestTableauCard(pile, location);
            if (index >= 0)
            {
                var clicked = tableau[pile][index];
                if (!clicked.FaceUp)
                {
                    if (index == tableau[pile].Count - 1)
                    {
                        clicked.FaceUp = true;
                        SetStatus("Card revealed.");
                        Invalidate();
                    }

                    return;
                }

                if (selection.Kind == SelectionKind.None)
                {
                    if (IsValidSequence(pile, index))
                    {
                        selection = Selection.FromTableau(pile, index);
                        SetStatus("Selected tableau stack.");
                        Invalidate();
                    }
                    return;
                }

                if (TryMoveSelectionToTableau(pile))
                {
                    return;
                }

                if (IsValidSequence(pile, index))
                {
                    selection = Selection.FromTableau(pile, index);
                    SetStatus("Selected tableau stack.");
                    Invalidate();
                }

                return;
            }

            if (selection.Kind != SelectionKind.None)
            {
                TryMoveSelectionToTableau(pile);
            }
        }

        private bool TryMoveSelectionToTableau(int pile)
        {
            if (selection.Kind == SelectionKind.None)
            {
                return false;
            }

            Card moving;
            if (selection.Kind == SelectionKind.Waste)
            {
                moving = waste[waste.Count - 1];
            }
            else
            {
                moving = tableau[selection.Pile][selection.Index];
            }

            if (!CanPlaceOnTableau(moving, pile))
            {
                SetStatus("That move is not allowed.");
                Invalidate();
                return false;
            }

            if (selection.Kind == SelectionKind.Waste)
            {
                waste.RemoveAt(waste.Count - 1);
                tableau[pile].Add(moving);
            }
            else
            {
                if (selection.Pile == pile)
                {
                    selection = Selection.None();
                    Invalidate();
                    return false;
                }

                var movingCards = tableau[selection.Pile].GetRange(selection.Index, tableau[selection.Pile].Count - selection.Index);
                tableau[selection.Pile].RemoveRange(selection.Index, tableau[selection.Pile].Count - selection.Index);
                tableau[pile].AddRange(movingCards);
                RevealTableauTop(selection.Pile);
            }

            selection = Selection.None();
            AfterMove();
            return true;
        }

        private void AfterMove()
        {
            if (IsWon())
            {
                SetStatus("You won Solitaire.");
            }
            else
            {
                SetStatus("Move completed.");
            }

            Invalidate();
        }

        private bool IsWon()
        {
            for (var i = 0; i < foundations.Length; i++)
            {
                if (foundations[i].Count != 13)
                {
                    return false;
                }
            }

            return true;
        }

        private void RevealTableauTop(int pile)
        {
            if (tableau[pile].Count > 0)
            {
                tableau[pile][tableau[pile].Count - 1].FaceUp = true;
            }
        }

        private bool IsValidSequence(int pile, int index)
        {
            for (var i = index; i < tableau[pile].Count - 1; i++)
            {
                var current = tableau[pile][i];
                var next = tableau[pile][i + 1];
                if (!current.FaceUp || !next.FaceUp)
                {
                    return false;
                }

                if (Card.IsRed(current.Suit) == Card.IsRed(next.Suit) || current.Rank != next.Rank + 1)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanMoveToFoundation(Card card, int foundationIndex)
        {
            var pile = foundations[foundationIndex];
            if (pile.Count == 0)
            {
                return card.Rank == 1;
            }

            var top = pile[pile.Count - 1];
            return top.Suit == card.Suit && card.Rank == top.Rank + 1;
        }

        private bool CanPlaceOnTableau(Card card, int pile)
        {
            if (tableau[pile].Count == 0)
            {
                return card.Rank == 13;
            }

            var top = tableau[pile][tableau[pile].Count - 1];
            if (!top.FaceUp)
            {
                return false;
            }

            return Card.IsRed(top.Suit) != Card.IsRed(card.Suit) && top.Rank == card.Rank + 1;
        }

        private int HitTestTableauCard(int pile, Point point)
        {
            for (var index = tableau[pile].Count - 1; index >= 0; index--)
            {
                var rect = GetTableauCardRect(pile, index);
                if (rect.Contains(point))
                {
                    return index;
                }
            }

            return -1;
        }

        private Rectangle GetStockRect()
        {
            return new Rectangle(MarginX, TopY, CardWidth, CardHeight);
        }

        private Rectangle GetWasteRect()
        {
            return new Rectangle(MarginX + ColumnGap, TopY, CardWidth, CardHeight);
        }

        private Rectangle GetFoundationRect(int index)
        {
            return new Rectangle(MarginX + (3 + index) * ColumnGap, TopY, CardWidth, CardHeight);
        }

        private Rectangle GetTableauSlot(int pile)
        {
            return new Rectangle(MarginX + pile * ColumnGap, TableauY, CardWidth, CardHeight);
        }

        private Rectangle GetTableauSlotBounds(int pile)
        {
            var height = CardHeight;
            for (var i = 0; i < tableau[pile].Count - 1; i++)
            {
                height += tableau[pile][i].FaceUp ? FaceUpOffset : FaceDownOffset;
            }

            return new Rectangle(MarginX + pile * ColumnGap, TableauY, CardWidth, Math.Max(height, CardHeight));
        }

        private Rectangle GetTableauCardRect(int pile, int index)
        {
            var y = TableauY;
            for (var i = 0; i < index; i++)
            {
                y += tableau[pile][i].FaceUp ? FaceUpOffset : FaceDownOffset;
            }

            return new Rectangle(MarginX + pile * ColumnGap, y, CardWidth, CardHeight);
        }

        private void DrawPileSlot(Graphics g, Rectangle rect)
        {
            using (var pen = new Pen(Color.FromArgb(180, 220, 180)))
            {
                g.DrawRectangle(pen, rect);
            }
        }

        private void DrawLabel(Graphics g, string text, Rectangle rect)
        {
            var labelRect = new Rectangle(rect.X, rect.Y + rect.Height + 4, rect.Width, 14);
            TextRenderer.DrawText(g, text, RetroFont.Ui(), labelRect, Color.White, TextFormatFlags.HorizontalCenter);
        }

        private void DrawCard(Graphics g, Card card, Rectangle rect, bool selectedCard)
        {
            g.FillRectangle(Brushes.White, rect);
            g.DrawRectangle(selectedCard ? Pens.Gold : Pens.Black, rect);

            var color = Card.IsRed(card.Suit) ? Brushes.Red : Brushes.Black;
            g.DrawString(card.RankLabel + card.SuitLabel, RetroFont.UiBold(), color, rect.X + 6, rect.Y + 6);
            using (var suitFont = RetroFont.Create(18.0f, FontStyle.Bold))
            {
                g.DrawString(card.SuitLabel, suitFont, color, rect.X + 20, rect.Y + 34);
            }
        }

        private void DrawCardBack(Graphics g, Rectangle rect)
        {
            using (var brush = new SolidBrush(Color.Navy))
            {
                g.FillRectangle(brush, rect);
            }

            g.DrawRectangle(Pens.White, rect);

            for (var y = rect.Y + 6; y < rect.Bottom - 6; y += 8)
            {
                g.DrawLine(Pens.LightBlue, rect.X + 6, y, rect.Right - 6, y);
            }
        }

        private void SetStatus(string message)
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, message);
            }
        }

        private sealed class Card
        {
            public Card(int rank, int suit, bool faceUp)
            {
                Rank = rank;
                Suit = suit;
                FaceUp = faceUp;
            }

            public int Rank;
            public int Suit;
            public bool FaceUp;

            public string RankLabel
            {
                get
                {
                    switch (Rank)
                    {
                        case 1: return "A";
                        case 11: return "J";
                        case 12: return "Q";
                        case 13: return "K";
                        default: return Rank.ToString();
                    }
                }
            }

            public string SuitLabel
            {
                get
                {
                    switch (Suit)
                    {
                        case 0: return "C";
                        case 1: return "D";
                        case 2: return "H";
                        default: return "S";
                    }
                }
            }

            public static bool IsRed(int suit)
            {
                return suit == 1 || suit == 2;
            }
        }

        private struct Selection
        {
            public SelectionKind Kind;
            public int Pile;
            public int Index;

            public static Selection None()
            {
                var selection = new Selection();
                selection.Kind = SelectionKind.None;
                selection.Pile = -1;
                selection.Index = -1;
                return selection;
            }

            public static Selection FromWaste()
            {
                var selection = new Selection();
                selection.Kind = SelectionKind.Waste;
                selection.Pile = -1;
                selection.Index = -1;
                return selection;
            }

            public static Selection FromTableau(int pile, int index)
            {
                var selection = new Selection();
                selection.Kind = SelectionKind.Tableau;
                selection.Pile = pile;
                selection.Index = index;
                return selection;
            }
        }

        private enum SelectionKind
        {
            None,
            Waste,
            Tableau
        }
    }
}
