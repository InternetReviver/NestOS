using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NestsOS
{
    internal class NestDosApp : UserControl
    {
        private readonly IDesktopHost host;
        private readonly RichTextBox output;
        private readonly TextBox input;
        private readonly List<string> history;
        private int historyIndex;

        public NestDosApp(IDesktopHost host)
        {
            this.host = host;
            history = new List<string>();
            historyIndex = 0;

            Dock = DockStyle.Fill;
            BackColor = Color.Black;

            output = new RichTextBox();
            output.Dock = DockStyle.Fill;
            output.ReadOnly = true;
            output.BorderStyle = BorderStyle.None;
            output.BackColor = Color.Black;
            output.ForeColor = Color.Lime;
            output.Font = RetroFont.Dos();

            input = new TextBox();
            input.Dock = DockStyle.Bottom;
            input.BorderStyle = BorderStyle.FixedSingle;
            input.BackColor = Color.Black;
            input.ForeColor = Color.Lime;
            input.Font = RetroFont.Dos();
            input.KeyDown += HandleInputKeyDown;

            Controls.Add(output);
            Controls.Add(input);

            WriteLine("NestDOS 3.1");
            WriteLine("SAVE root: " + SaveSystem.EnsureRoot());
            WriteLine("Type HELP for commands.");
            WritePrompt();
        }

        private void HandleInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var command = input.Text.Trim();
                history.Add(command);
                historyIndex = history.Count;
                output.SelectionColor = Color.White;
                output.AppendText("C:\\NESTS\\SAVE> " + command + Environment.NewLine);
                input.Clear();
                Execute(command);
                WritePrompt();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (history.Count > 0)
                {
                    historyIndex = Math.Max(0, historyIndex - 1);
                    input.Text = history[historyIndex];
                    input.SelectionStart = input.Text.Length;
                }

                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (history.Count > 0)
                {
                    historyIndex = Math.Min(history.Count, historyIndex + 1);
                    input.Text = historyIndex >= history.Count ? string.Empty : history[historyIndex];
                    input.SelectionStart = input.Text.Length;
                }

                e.SuppressKeyPress = true;
            }
        }

        private void Execute(string commandLine)
        {
            if (commandLine.Length == 0)
            {
                return;
            }

            var parts = SplitArguments(commandLine);
            if (parts.Count == 0)
            {
                return;
            }

            var command = parts[0].ToLowerInvariant();
            switch (command)
            {
                case "help":
                    WriteLine("HELP, DIR, CLS, ECHO, TYPE <file>, DEL <file>, TIME, VER");
                    WriteLine("PAINT, EDIT [file], FILES, SOLITAIRE, MAHJONG, CHESS");
                    WriteLine("ANALOG, DIGITAL, VERSION, RUN");
                    break;
                case "dir":
                    ExecuteDir();
                    break;
                case "cls":
                    output.Clear();
                    break;
                case "echo":
                    WriteLine(commandLine.Substring(Math.Min(5, commandLine.Length)));
                    break;
                case "type":
                    if (parts.Count < 2)
                    {
                        WriteLine("Usage: TYPE <file>");
                    }
                    else
                    {
                        ExecuteType(parts[1]);
                    }
                    break;
                case "del":
                    if (parts.Count < 2)
                    {
                        WriteLine("Usage: DEL <file>");
                    }
                    else
                    {
                        ExecuteDelete(parts[1]);
                    }
                    break;
                case "time":
                    WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    break;
                case "ver":
                    WriteLine("NestsOS 3.1 single-exe desktop shell");
                    break;
                case "version":
                    host.OpenVersionInfo();
                    WriteLine("Launching Version...");
                    break;
                case "run":
                    host.OpenNostRun(parts.Count > 1 ? parts[1] : string.Empty);
                    WriteLine("Launching Nost64/32...");
                    break;
                case "paint":
                    host.OpenPaint(null);
                    WriteLine("Launching Paint...");
                    break;
                case "edit":
                    host.OpenTextEditor(parts.Count > 1 ? ResolveFile(parts[1], ".txt", false) : null);
                    WriteLine("Launching TextPad...");
                    break;
                case "files":
                    host.OpenFileCabinet();
                    WriteLine("Launching File Cabinet...");
                    break;
                case "solitaire":
                    host.OpenSolitaire();
                    WriteLine("Launching Solitaire...");
                    break;
                case "mahjong":
                    host.OpenMahjong();
                    WriteLine("Launching Mahjong...");
                    break;
                case "chess":
                    host.OpenChess();
                    WriteLine("Launching Chess...");
                    break;
                case "analog":
                case "clock":
                    host.OpenAnalogClock();
                    WriteLine("Launching Analog Clock...");
                    break;
                case "digital":
                    host.OpenDigitalClock();
                    WriteLine("Launching Digital Clock...");
                    break;
                default:
                    WriteLine("Bad command or file name.");
                    break;
            }
        }

        private void ExecuteDir()
        {
            var root = SaveSystem.EnsureRoot();
            var files = Directory.GetFiles(root);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            if (files.Length == 0)
            {
                WriteLine("No files in SAVE yet.");
                return;
            }

            for (var i = 0; i < files.Length; i++)
            {
                var info = new FileInfo(files[i]);
                WriteLine(info.Name.PadRight(26) + " " + info.Length.ToString().PadLeft(8) + " bytes");
            }
        }

        private void ExecuteType(string requested)
        {
            var path = ResolveFile(requested, ".txt", true);
            if (path == null)
            {
                WriteLine("File not found.");
                return;
            }

            WriteLine(File.ReadAllText(path));
        }

        private void ExecuteDelete(string requested)
        {
            var path = ResolveFile(requested, string.Empty, true);
            if (path == null)
            {
                WriteLine("File not found.");
                return;
            }

            File.Delete(path);
            WriteLine("Deleted " + Path.GetFileName(path));
        }

        private string ResolveFile(string requested, string defaultExtension, bool mustExist)
        {
            var root = SaveSystem.EnsureRoot();
            var name = requested;
            if (defaultExtension.Length > 0 && Path.GetExtension(name).Length == 0)
            {
                name += defaultExtension;
            }

            var path = Path.Combine(root, name);
            if (mustExist && !File.Exists(path))
            {
                return null;
            }

            return path;
        }

        private List<string> SplitArguments(string commandLine)
        {
            var list = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < commandLine.Length; i++)
            {
                var c = commandLine[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        list.Add(current.ToString());
                        current.Length = 0;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                list.Add(current.ToString());
            }

            return list;
        }

        private void WritePrompt()
        {
            output.SelectionColor = Color.Lime;
            output.AppendText("C:\\NESTS\\SAVE> ");
            output.ScrollToCaret();
        }

        private void WriteLine(string text)
        {
            output.SelectionColor = Color.Lime;
            output.AppendText(text + Environment.NewLine);
            output.ScrollToCaret();
        }
    }
}
