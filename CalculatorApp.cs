using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NestsOS
{
    internal class CalculatorApp : UserControl
    {
        private readonly TextBox display;
        private double storedValue;
        private string pendingOperator;
        private bool replaceDisplay;
        private bool resetOnNextDigit;

        public CalculatorApp()
        {
            Dock = DockStyle.Fill;
            BackColor = RetroPalette.WindowBackground;

            display = new TextBox();
            display.ReadOnly = true;
            display.TextAlign = HorizontalAlignment.Right;
            display.Font = RetroFont.Create(14.0f, FontStyle.Bold);
            display.Text = "0";
            display.SetBounds(10, 10, 234, 28);

            Controls.Add(display);

            var labels = new[,]
            {
                { "CE", "C", "BS", "/" },
                { "7", "8", "9", "*" },
                { "4", "5", "6", "-" },
                { "1", "2", "3", "+" },
                { "+/-", "0", ".", "=" }
            };

            for (var row = 0; row < 5; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    var text = labels[row, col];
                    var button = new RetroButton();
                    button.Text = text;
                    button.SetBounds(10 + (col * 58), 50 + (row * 42), 52, 34);
                    button.Click += HandleButtonClick;
                    Controls.Add(button);
                }
            }
        }

        private void HandleButtonClick(object sender, EventArgs e)
        {
            var text = ((Button)sender).Text;
            switch (text)
            {
                case "0":
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    AppendDigit(text);
                    break;
                case ".":
                    AppendDecimal();
                    break;
                case "+":
                case "-":
                case "*":
                case "/":
                    ApplyOperator(text);
                    break;
                case "=":
                    EvaluatePending();
                    pendingOperator = null;
                    replaceDisplay = true;
                    break;
                case "C":
                    storedValue = 0;
                    pendingOperator = null;
                    display.Text = "0";
                    replaceDisplay = true;
                    resetOnNextDigit = false;
                    break;
                case "CE":
                    display.Text = "0";
                    replaceDisplay = true;
                    resetOnNextDigit = false;
                    break;
                case "BS":
                    Backspace();
                    break;
                case "+/-":
                    ToggleSign();
                    break;
            }
        }

        private void AppendDigit(string digit)
        {
            if (replaceDisplay || resetOnNextDigit || display.Text == "Error")
            {
                display.Text = digit;
                replaceDisplay = false;
                resetOnNextDigit = false;
                return;
            }

            display.Text = display.Text == "0" ? digit : display.Text + digit;
        }

        private void AppendDecimal()
        {
            if (replaceDisplay || resetOnNextDigit || display.Text == "Error")
            {
                display.Text = "0.";
                replaceDisplay = false;
                resetOnNextDigit = false;
                return;
            }

            if (display.Text.IndexOf('.') < 0)
            {
                display.Text += ".";
            }
        }

        private void ApplyOperator(string op)
        {
            if (!replaceDisplay)
            {
                if (pendingOperator != null)
                {
                    EvaluatePending();
                }
                else
                {
                    storedValue = ParseDisplay();
                }
            }

            pendingOperator = op;
            replaceDisplay = true;
            resetOnNextDigit = false;
        }

        private void EvaluatePending()
        {
            var current = ParseDisplay();
            if (pendingOperator == null)
            {
                storedValue = current;
                return;
            }

            switch (pendingOperator)
            {
                case "+":
                    storedValue += current;
                    break;
                case "-":
                    storedValue -= current;
                    break;
                case "*":
                    storedValue *= current;
                    break;
                case "/":
                    if (current == 0)
                    {
                        display.Text = "Error";
                        storedValue = 0;
                        pendingOperator = null;
                        replaceDisplay = true;
                        resetOnNextDigit = true;
                        return;
                    }

                    storedValue /= current;
                    break;
            }

            display.Text = FormatValue(storedValue);
        }

        private void Backspace()
        {
            if (replaceDisplay || resetOnNextDigit || display.Text == "Error")
            {
                display.Text = "0";
                replaceDisplay = true;
                resetOnNextDigit = false;
                return;
            }

            if (display.Text.Length <= 1 || (display.Text.Length == 2 && display.Text.StartsWith("-", StringComparison.Ordinal)))
            {
                display.Text = "0";
            }
            else
            {
                display.Text = display.Text.Substring(0, display.Text.Length - 1);
            }
        }

        private void ToggleSign()
        {
            if (display.Text == "0" || display.Text == "Error")
            {
                return;
            }

            display.Text = display.Text.StartsWith("-", StringComparison.Ordinal)
                ? display.Text.Substring(1)
                : "-" + display.Text;
        }

        private double ParseDisplay()
        {
            double value;
            if (!double.TryParse(display.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return 0;
            }

            return value;
        }

        private string FormatValue(double value)
        {
            return value.ToString("0.############", CultureInfo.InvariantCulture);
        }
    }
}
