using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;
using WinUIEx;

namespace modterm
{
    public sealed partial class MainWindow : Window
    {
        private byte            _currentOpacity = 0;
        private Color           _currentTint = Colors.Transparent;
        private SolidColorBrush _backgroundBrush = new SolidColorBrush(Colors.Transparent);
        private ConPTYTerminal  _terminal;
        
        private Dictionary<string, string> _shellEnv = new Dictionary<string, string>()
            {
                { "cmd", "C:\\Windows\\System32\\cmd.exe" },
                { "powershell", "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe" },
                { "pwsh", "C:\\Program Files\\PowerShell\\7\\pwsh.exe" },
                { "bash", "C:\\Program Files\\Git\\usr\\bin\\bash.exe" }
            };

        private string _currentShell;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            this.SetTitleBar(AppTitleBar);
            this.SizeChanged += MainWindow_SizeChanged;
            this.Closed += (s, e) => _terminal?.Dispose();
            this.Activated += (s, e) =>
            {
                if (_terminal == null) StartConPTY();
            };

            RootGrid.Background = _backgroundBrush;

            RootGrid.KeyDown += TerminalCanvas_KeyDown;
            
            TerminalCanvas.Draw += this.TerminalCanvas_Draw;
            //TerminalCanvas.KeyDown += TerminalCanvas_KeyDown;
            TerminalCanvas.RightTapped += TerminalCanvas_RightTapped;

            _currentShell = _shellEnv["bash"];
        }

        private void StartConPTY()
        {
             string appPath = _currentShell;
            _terminal = new ConPTYTerminal(appPath, "");
            _terminal.OutputReceived += OnOutputReceived;
            _terminal.Start();
        }

        private void OnOutputReceived(object sender, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            var lines = line.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var l in lines)
            {
                Debug.WriteLine($"Output: {l}");
                _bufferLines.Add(l);

                // draw the output to the canvas - for now we just invalidate the canvas and redraw everything, but ideally we'd want to parse the output and only redraw the parts that changed
                TerminalCanvas.Invalidate();

            }
            
        }


        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            _terminal?.Resize((short)(args.Size.Width / 8.8), (short)(args.Size.Height / 19.5));
        }

        private void SetTransparency(byte percent) 
        {
            _currentOpacity = (byte)(percent * 2.55); UpdateBackground(); 
        }
        
        private void SetTint(Color tint) 
        {
            _currentTint = tint; UpdateBackground(); 
        }
        
        private void UpdateBackground()
        {
            var final = _currentTint == Colors.Transparent
                ? Colors.Transparent
                : Color.FromArgb(_currentOpacity, _currentTint.R, _currentTint.G, _currentTint.B);
            _backgroundBrush.Color = final;
        }

    }
}