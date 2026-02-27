using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.System;
using Windows.UI;

namespace modterm
{
    public sealed partial class MainWindow : Window
    {
        private FontFamily          _currentFont = new FontFamily("Cascadia Mono");
        private double              _currentFontSize = 15.5;
        private SolidColorBrush     _inputBrush = new SolidColorBrush(Color.FromArgb(255, 0, 238, 255));
        private SolidColorBrush     _outputBrush = new SolidColorBrush(Color.FromArgb(255, 80, 255, 140));
        private Color               _inputColor = Color.FromArgb(255, 0, 238, 255);
        private Color               _outputColor = Color.FromArgb(255, 80, 255, 140);
        private float               _blurAmount = 0F;

        private string              _commandLine = "";
        private List<string>        _bufferLines = new List<string>();

        private void TerminalCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_commandLine != null)
            {
                // Create command list and draw command line in a blur
                using (var commandList = new CanvasCommandList(sender))
                { 
                    using (var clds = commandList.CreateDrawingSession())
                    {
                        // Draw the command line input at the bottom
                        clds.DrawText("> " + _commandLine, 10, (float)(sender.ActualHeight - _currentFontSize - 20), _inputColor,
                            new CanvasTextFormat { FontFamily = _currentFont.Source, FontSize = (float)_currentFontSize });

                        // Draw the buffer lines above the command line
                        double _y = sender.ActualHeight - _currentFontSize * 2 - 25; // Start above the command line
                        for (int i = _bufferLines.Count - 1; i >= 0; i--)
                        {
                            var line = _bufferLines[i];

                            var color = _outputColor;
                            if (line != string.Empty)
                            {
                                if (line.StartsWith(">"))
                                {
                                    color = _inputColor;
                                }
                            }

                            clds.DrawText(line, 10, (float)_y, color,
                                new CanvasTextFormat { FontFamily = _currentFont.Source, FontSize = (float)_currentFontSize });
                            _y -= _currentFontSize + 5; // Move up for the next line
                            if (_y < 0) break; // Stop if we run out of space
                        }
                    }

                    // Apply blur effect
                    var blurEffect = new GaussianBlurEffect
                    {
                        Source = commandList,
                        BlurAmount = _blurAmount
                    };

                    args.DrawingSession.DrawImage(blurEffect);
                }

                // Draw the command line input at the bottom
                args.DrawingSession.DrawText("> " + _commandLine, 10, (float)(sender.ActualHeight - _currentFontSize - 20), _inputColor,
                    new CanvasTextFormat { FontFamily = _currentFont.Source, FontSize = (float)_currentFontSize });

                // Draw the buffer lines above the command line
                double y = sender.ActualHeight - _currentFontSize * 2 - 25; // Start above the command line
                for (int i = _bufferLines.Count - 1; i >= 0; i--)
                {
                    var line = _bufferLines[i];

                    var color = _outputColor;
                    if (line != string.Empty)
                    {
                        if (line.StartsWith(">"))
                        {
                            color = _inputColor;
                        }
                    }

                    args.DrawingSession.DrawText(line, 10, (float)y, color,
                        new CanvasTextFormat { FontFamily = _currentFont.Source, FontSize = (float)_currentFontSize });
                    y -= _currentFontSize + 5; // Move up for the next line
                    if (y < 0) break; // Stop if we run out of space
                }
            }
        }

        private char? GetCharFromVirtualKey(Windows.System.VirtualKey key, KeyRoutedEventArgs e)
        {
            Debug.Write("Key: " + key);
            Windows.UI.Core.CoreVirtualKeyStates shiftState =
                    Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                        Windows.System.VirtualKey.Shift);

            bool isShiftPressed = shiftState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            // handle a-z
            if (key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z)
            {
                char baseChar = (char)('a' + (key - Windows.System.VirtualKey.A));
                if (isShiftPressed)
                {
                    baseChar = char.ToUpper(baseChar);
                }
                Debug.WriteLine(" -> " + baseChar);
                return baseChar;
            }
            // handle 0-9
            if (key >= Windows.System.VirtualKey.Number0 && key <= Windows.System.VirtualKey.Number9)
            {
                char baseChar = (char)('0' + (key - Windows.System.VirtualKey.Number0));
                if (isShiftPressed)
                {
                    // Handle shifted number keys for common symbols
                    switch (baseChar)
                    {
                        case '1': baseChar = '!'; break;
                        case '2': baseChar = '@'; break;
                        case '3': baseChar = '#'; break;
                        case '4': baseChar = '$'; break;
                        case '5': baseChar = '%'; break;
                        case '6': baseChar = '^'; break;
                        case '7': baseChar = '&'; break;
                        case '8': baseChar = '*'; break;
                        case '9': baseChar = '('; break;
                        case '0': baseChar = ')'; break;
                    }
                }
                Debug.WriteLine(" -> " + baseChar);
                return baseChar;
            } else
            {
                // Handle some common punctuation keys
                switch (key)
                {
                    case Windows.System.VirtualKey.Space: return ' ';
                    case (VirtualKey)188: return isShiftPressed ? '<' : ',';
                    case (VirtualKey)190: return isShiftPressed ? '>' : '.';
                    case (VirtualKey)189: return isShiftPressed ? '_' : '-';
                    case (VirtualKey)187: return isShiftPressed ? '+' : '=';
                    case (VirtualKey)191: return isShiftPressed ? '?' : '/';
                    case (VirtualKey)186: return isShiftPressed ? ':' : ';';
                    case (VirtualKey)222: return isShiftPressed ? '"' : '\'';
                    case (VirtualKey)219: return isShiftPressed ? '{' : '[';
                    case (VirtualKey)221: return isShiftPressed ? '}' : ']';
                    case (VirtualKey)220: return isShiftPressed ? '|' : '\\';
                    case (VirtualKey)192: return isShiftPressed ? '~' : '`';
                }
            }
            return null;
        }

        private void TerminalCanvas_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"Key pressed: {e.Key}");

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Enter:
                    Debug.WriteLine($"Command entered: {_commandLine}");
                    _terminal.WriteInput(_commandLine + "\n");
                    _bufferLines.Add("> " + _commandLine);
                    _commandLine = "";
                    break;
                case Windows.System.VirtualKey.Back:
                    if (_commandLine.Length > 0)
                        _commandLine = _commandLine.Substring(0, _commandLine.Length - 1);
                    break;
                default:
                    // Handle character input
                    var keyChar = GetCharFromVirtualKey(e.Key, e);
                    if (keyChar != null)
                        _commandLine += keyChar;
                    break;
            }

            TerminalCanvas.Invalidate(); // Trigger redraw to show updated command line
        }

        private void TerminalCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
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
                item.Click += (_, __) => { SetTransparency(pct); TerminalCanvas.Invalidate(); };
                    transSub.Items.Add(item);
            }
            flyout.Items.Add(transSub);

            var tintSub = new MenuFlyoutSubItem { Text = "Tint" };
            var tintOptions = new (string, Color)[] {
                ("Transparent", Colors.Transparent), ("Snow White", Colors.White),
                ("Pitch Black", Colors.Black),
                ("Violet", Color.FromArgb(255,153,0,255)), ("Azure", Colors.Blue),
                ("Verdant", Colors.Lime), ("Sunny", Colors.Yellow),
                ("Citrus", Color.FromArgb(255,255,153,0)), ("Ember", Colors.Red)
            };
            foreach (var (label, tint) in tintOptions)
            {
                var item = new MenuFlyoutItem { Text = label };
                item.Click += (_, __) => { SetTint(tint); TerminalCanvas.Invalidate(); };
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
                item.Click += (_, __) => { _currentFont = new FontFamily(f); TerminalCanvas.Invalidate(); };
                fontSub.Items.Add(item);
            }
            flyout.Items.Add(fontSub);

            // === NEW: Font Size ===
            var sizeSub = new MenuFlyoutSubItem { Text = "Font Size" };
            var sizes = new[] { 13.5, 14.5, 15.5, 16.5, 17.5 };
            foreach (var s in sizes)
            {
                var item = new MenuFlyoutItem { Text = $"{s} pt" };
                item.Click += (_, __) => { _currentFontSize = s; TerminalCanvas.Invalidate(); };
                sizeSub.Items.Add(item);
            }
            flyout.Items.Add(sizeSub);

            // === NEW: Font Glow ===
            var glowSub = new MenuFlyoutSubItem { Text = "Font Glow" };
            var glowSubAmts = new[] { 0F, 1F, 2.5F, 5F, 7.5F, 10F, 15F };
            foreach (var s in glowSubAmts)
            {
                var item = new MenuFlyoutItem { Text = $"{s} radius" };
                item.Click += (_, __) => { _blurAmount = s; TerminalCanvas.Invalidate(); };
                glowSub.Items.Add(item);
            }
            flyout.Items.Add(glowSub);

            // === NEW: Input Color ===
            var inputColorSub = new MenuFlyoutSubItem { Text = "Input Color" };
            foreach (var (name, col) in ColorOptions)
            {
                var item = new MenuFlyoutItem { Text = name };
                item.Click += (_, __) => { _inputColor = col; TerminalCanvas.Invalidate(); };
                inputColorSub.Items.Add(item);
            }
            flyout.Items.Add(inputColorSub);

            // === NEW: Output Color ===
            var outputColorSub = new MenuFlyoutSubItem { Text = "Output Color" };
            foreach (var (name, col) in ColorOptions)
            {
                var item = new MenuFlyoutItem { Text = name };
                item.Click += (_, __) => { _outputColor = col; TerminalCanvas.Invalidate(); };
                outputColorSub.Items.Add(item);
            }
            flyout.Items.Add(outputColorSub);

            flyout.ShowAt(TerminalCanvas, e.GetPosition(TerminalCanvas));
        }

        private readonly (string Name, Color Color)[] ColorOptions = new[]
        {
            ("White", Colors.White),
            ("OG", Color.FromArgb(255, 0, 238, 255)),
            ("Cyan", Colors.Cyan),
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
    }
}
