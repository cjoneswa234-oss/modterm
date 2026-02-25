using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;
using WinUIEx;

namespace ModernTerminal
{
    public sealed partial class MainWindow : Window
    {
        private ConPTYTerminal _terminal;
        private Grid _currentInputRow;
        private TextBox _currentInputBox;
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;

        private byte _currentOpacity = 0;
        private Color _currentTint = Colors.Transparent;
        private SolidColorBrush _backgroundBrush = new SolidColorBrush(Colors.Transparent);

        private FontFamily _currentFont = new FontFamily("Cascadia Mono");
        private double _currentFontSize = 15.5;

        private SolidColorBrush _inputBrush = new SolidColorBrush(Color.FromArgb(255, 0, 238, 255));
        private SolidColorBrush _outputBrush = new SolidColorBrush(Color.FromArgb(255, 80, 255, 140));

        private readonly (string Name, Color Color)[] ColorOptions = new[]
        {
            ("White", Colors.White),
            ("Bright Violet", Color.FromArgb(255, 187, 68, 255)),
            ("Dim Violet", Color.FromArgb(255, 136, 0, 204)),
            ("Bright Azure", Color.FromArgb(255, 68, 153, 255)),
            ("Dim Azure", Color.FromArgb(255, 0, 102, 204)),
            ("Bright Verdant", Color.FromArgb(255, 85, 255, 136)),
            ("Dim Verdant", Color.FromArgb(255, 0, 170, 85)),
            ("Bright Sunny", Color.FromArgb(255, 255, 255, 102)),
            ("Dim Sunny", Color.FromArgb(255, 204, 204, 0)),
            ("Bright Citrus", Color.FromArgb(255, 255, 187, 85)),
            ("Dim Citrus", Color.FromArgb(255, 204, 136, 34)),
            ("Bright Ember", Color.FromArgb(255, 255, 102, 102)),
            ("Dim Ember", Color.FromArgb(255, 204, 51, 51))
        };

        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            this.SetTitleBar(AppTitleBar);

            RootGrid.Background = _backgroundBrush;

            TerminalOutput.RightTapped += TerminalOutput_RightTapped;
            this.SizeChanged += MainWindow_SizeChanged;
            this.Closed += (s, e) => _terminal?.Dispose();

            this.Activated += (s, e) =>
            {
                if (_terminal == null) StartConPTY();
            };
        }

        private void StartConPTY()
        {
            string appPath = "C:\\Program Files\\Git\\usr\\bin\\bash.exe";
            _terminal = new ConPTYTerminal(appPath, "");
            _terminal.OutputReceived += OnOutputReceived;
            _terminal.Start();

            AddHistoryLine("Modern Terminal • Seamless Glassmorphism", Color.FromArgb(255, 136, 238, 255));
            AddHistoryLine("────────────────────────────────────────────────────────────", Colors.Gray);
            AddHistoryLine("Right-click → Font / Size / Input Color / Output Color", Color.FromArgb(255, 170, 204, 255));
            AddHistoryLine("", Colors.Gray);

            AddNewInputLine();
        }

        // ====================== Output ======================
        private void OnOutputReceived(object sender, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            DispatcherQueue.TryEnqueue(() =>
            {
                var lines = line.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var l in lines)
                {
                    var tb = CreateOutputTextBlock(l);
                    int idx = _currentInputRow != null ? TerminalOutput.Children.IndexOf(_currentInputRow) : TerminalOutput.Children.Count;
                    TerminalOutput.Children.Insert(idx, tb);
                }
                TerminalScroll.ChangeView(null, TerminalScroll.ExtentHeight, null);
            });
        }

        private TextBlock CreateOutputTextBlock(string text)
        {
            var tb = new TextBlock
            {
                FontFamily = _currentFont,
                FontSize = _currentFontSize,
                TextWrapping = TextWrapping.NoWrap,
                Foreground = _outputBrush
            };
            ApplyAnsiToTextBlock(text, tb);
            return tb;
        }

        private void AddHistoryLine(string text, Color color)
        {
            var tb = new TextBlock
            {
                FontFamily = _currentFont,
                FontSize = _currentFontSize,
                Foreground = new SolidColorBrush(color),
                TextWrapping = TextWrapping.NoWrap,
                Text = text
            };
            TerminalOutput.Children.Add(tb);
        }

        // ====================== Input line ======================
        private void AddNewInputLine()
        {
            if (_currentInputRow != null && TerminalOutput.Children.Contains(_currentInputRow))
                TerminalOutput.Children.Remove(_currentInputRow);

            _currentInputRow = new Grid();
            _currentInputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _currentInputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var prompt = new TextBlock
            {
                Text = "❯ ",
                FontFamily = _currentFont,
                FontSize = _currentFontSize,
                Foreground = _inputBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(prompt, 0);

            _currentInputBox = new TextBox
            {
                FontFamily = _currentFont,
                FontSize = _currentFontSize,
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                AcceptsReturn = false
            };
            _currentInputBox.KeyDown += InputBox_KeyDown;
            Grid.SetColumn(_currentInputBox, 1);

            _currentInputRow.Children.Add(prompt);
            _currentInputRow.Children.Add(_currentInputBox);

            TerminalOutput.Children.Add(_currentInputRow);
            TerminalScroll.ChangeView(null, TerminalScroll.ExtentHeight, null);
            _currentInputBox.Focus(FocusState.Programmatic);
        }

        private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // History (↑/↓) unchanged from last version
            if (e.Key == Windows.System.VirtualKey.Up)
            {
                if (_commandHistory.Count == 0) return;
                _historyIndex = Math.Min(_historyIndex + 1, _commandHistory.Count - 1);
                _currentInputBox.Text = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                _currentInputBox.SelectAll();
                e.Handled = true; return;
            }
            if (e.Key == Windows.System.VirtualKey.Down)
            {
                _historyIndex = Math.Max(_historyIndex - 1, -1);
                _currentInputBox.Text = _historyIndex >= 0 ? _commandHistory[_commandHistory.Count - 1 - _historyIndex] : "";
                _currentInputBox.SelectAll();
                e.Handled = true; return;
            }

            if (e.Key == Windows.System.VirtualKey.Enter && sender is TextBox tb)
            {
                string cmd = tb.Text.Trim();
                int index = TerminalOutput.Children.IndexOf(_currentInputRow);
                if (index >= 0)
                {
                    var history = new TextBlock
                    {
                        FontFamily = _currentFont,
                        FontSize = _currentFontSize,
                        Foreground = _inputBrush,
                        TextWrapping = TextWrapping.NoWrap,
                        Text = "❯ " + cmd
                    };
                    TerminalOutput.Children[index] = history;
                }

                if (!string.IsNullOrEmpty(cmd))
                {
                    if (_commandHistory.Count == 0 || _commandHistory[^1] != cmd)
                        _commandHistory.Add(cmd);
                    _historyIndex = -1;
                    _terminal.WriteInput(cmd + "\r\n");
                }
                AddNewInputLine();
                e.Handled = true;
            }
        }

        // ====================== Resize ======================
        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            _terminal?.Resize((short)(args.Size.Width / 8.8), (short)(args.Size.Height / 19.5));
        }

        // ====================== Context Menu ======================
        private void TerminalOutput_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem { Text = "Copy" });
            flyout.Items.Add(new MenuFlyoutItem { Text = "Paste" });
            flyout.Items.Add(new MenuFlyoutSeparator());

            // Transparency + Tint (unchanged from last time)
            var transSub = new MenuFlyoutSubItem { Text = "Transparency" };
            for (int i = 0; i <= 10; i++)
            {
                byte pct = (byte)(i * 10);
                var item = new MenuFlyoutItem { Text = i == 0 ? "Transparent (0%)" : $"{pct}%" };
                item.Click += (_, __) => SetTransparency(pct);
                transSub.Items.Add(item);
            }
            flyout.Items.Add(transSub);

            var tintSub = new MenuFlyoutSubItem { Text = "Tint" };
            var tintOptions = new (string, Color)[] {
                ("Transparent", Colors.Transparent), ("Clear (White)", Colors.White),
                ("Violet", Color.FromArgb(255,153,0,255)), ("Azure", Colors.Blue),
                ("Verdant", Colors.Lime), ("Sunny", Colors.Yellow),
                ("Citrus", Color.FromArgb(255,255,153,0)), ("Ember", Colors.Red)
            };
            foreach (var (label, tint) in tintOptions)
            {
                var item = new MenuFlyoutItem { Text = label };
                item.Click += (_, __) => SetTint(tint);
                tintSub.Items.Add(item);
            }
            flyout.Items.Add(tintSub);

            flyout.Items.Add(new MenuFlyoutSeparator());

            // === NEW: Font Family ===
            var fontSub = new MenuFlyoutSubItem { Text = "Font Family" };
            var fonts = new[] { "Cascadia Mono", "Consolas", "Courier New", "Lucida Console", "Segoe UI Mono" };
            foreach (var f in fonts)
            {
                var item = new MenuFlyoutItem { Text = f };
                item.Click += (_, __) => { _currentFont = new FontFamily(f); UpdateAllFonts(); };
                fontSub.Items.Add(item);
            }
            flyout.Items.Add(fontSub);

            // === NEW: Font Size ===
            var sizeSub = new MenuFlyoutSubItem { Text = "Font Size" };
            var sizes = new[] { 13.5, 14.5, 15.5, 16.5, 17.5 };
            foreach (var s in sizes)
            {
                var item = new MenuFlyoutItem { Text = $"{s} pt" };
                item.Click += (_, __) => { _currentFontSize = s; UpdateAllFonts(); };
                sizeSub.Items.Add(item);
            }
            flyout.Items.Add(sizeSub);

            // === NEW: Input Color ===
            var inputColorSub = new MenuFlyoutSubItem { Text = "Input Color" };
            foreach (var (name, col) in ColorOptions)
            {
                var item = new MenuFlyoutItem { Text = name };
                item.Click += (_, __) => { _inputBrush = new SolidColorBrush(col); UpdateAllInputLines(); };
                inputColorSub.Items.Add(item);
            }
            flyout.Items.Add(inputColorSub);

            // === NEW: Output Color ===
            var outputColorSub = new MenuFlyoutSubItem { Text = "Output Color" };
            foreach (var (name, col) in ColorOptions)
            {
                var item = new MenuFlyoutItem { Text = name };
                item.Click += (_, __) => { _outputBrush = new SolidColorBrush(col); UpdateAllOutputLines(); };
                outputColorSub.Items.Add(item);
            }
            flyout.Items.Add(outputColorSub);

            flyout.ShowAt(TerminalOutput, e.GetPosition(TerminalOutput));
        }

        private void UpdateAllFonts()
        {
            foreach (var child in TerminalOutput.Children)
            {
                if (child is TextBlock tb)
                {
                    tb.FontFamily = _currentFont;
                    tb.FontSize = _currentFontSize;
                }
                else if (child is Grid g && g.Children.Count == 2)
                {
                    foreach (var c in g.Children)
                    {
                        if (c is TextBlock p) { p.FontFamily = _currentFont; p.FontSize = _currentFontSize; }
                        if (c is TextBox box) { box.FontFamily = _currentFont; box.FontSize = _currentFontSize; }
                    }
                }
            }
            if (_currentInputBox != null) _currentInputBox.Focus(FocusState.Programmatic);
        }

        private void UpdateAllInputLines()
        {
            foreach (var child in TerminalOutput.Children)
            {
                if (child is Grid g && g.Children.Count == 2)
                {
                    foreach (var c in g.Children)
                        if (c is TextBlock p) p.Foreground = _inputBrush;
                }
            }
        }

        private void UpdateAllOutputLines()
        {
            foreach (var child in TerminalOutput.Children)
            {
                if (child is TextBlock tb && tb.Text != "❯ " && !tb.Text.StartsWith("❯ "))
                    tb.Foreground = _outputBrush;
            }
        }

        private void SetTransparency(byte percent) { _currentOpacity = (byte)(percent * 2.55); UpdateBackground(); }
        private void SetTint(Color tint) { _currentTint = tint; UpdateBackground(); }
        private void UpdateBackground()
        {
            var final = _currentTint == Colors.Transparent
                ? Colors.Transparent
                : Color.FromArgb(_currentOpacity, _currentTint.R, _currentTint.G, _currentTint.B);
            _backgroundBrush.Color = final;
        }

        private void ApplyAnsiToTextBlock(string text, TextBlock tb)
        {
            var runs = Regex.Split(text, @"(\x1B\[[0-9;]*[a-zA-Z])");
            var currentColor = _outputBrush.Color;   // respect chosen output color

            foreach (var part in runs)
            {
                if (part.StartsWith("\x1B["))
                {
                    if (part.Contains("31")) currentColor = Color.FromArgb(255, 255, 85, 85);
                    else if (part.Contains("32")) currentColor = _outputBrush.Color;
                    else if (part.Contains("33")) currentColor = Color.FromArgb(255, 255, 200, 80);
                    else if (part.Contains("34")) currentColor = Color.FromArgb(255, 80, 180, 255);
                    else if (part.Contains("35")) currentColor = Color.FromArgb(255, 200, 100, 255);
                    else if (part.Contains("36")) currentColor = Color.FromArgb(255, 80, 240, 240);
                    else if (part.Contains("0m") || part.Contains("39m")) currentColor = _outputBrush.Color;
                }
                else if (!string.IsNullOrEmpty(part))
                {
                    tb.Inlines.Add(new Run { Text = part, Foreground = new SolidColorBrush(currentColor) });
                }
            }
        }
    }
}