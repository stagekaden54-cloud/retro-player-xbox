using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace RetroPlayerXbox
{
    public sealed partial class MainPage : Page
    {
        private const string HomeUrl = "https://retro-player-xbox.vercel.app/";
        private readonly DispatcherTimer _controllerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        private Gamepad _activeGamepad;
        private GamepadButtons _previousButtons = GamepadButtons.None;
        private bool _webViewReady;
        private bool _sendingInput;
        private int _lastStickX;
        private int _lastStickY;
        private DateTime _lastStickRepeat = DateTime.MinValue;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await PlayerWebView.EnsureCoreWebView2Async();
                var core = PlayerWebView.CoreWebView2;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsZoomControlEnabled = false;
                core.NavigationStarting += Core_NavigationStarting;
                core.NavigationCompleted += Core_NavigationCompleted;
                core.Navigate(HomeUrl);

                _webViewReady = true;
                _controllerTimer.Tick += ControllerTimer_Tick;
                _controllerTimer.Start();
                PlayerWebView.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                StartupOverlay.Visibility = Visibility.Visible;
                StartupStatus.Text = "Could not initialize the Xbox web runtime: " + ex.Message;
            }
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _controllerTimer.Stop();
            _controllerTimer.Tick -= ControllerTimer_Tick;
            _activeGamepad = null;
            _previousButtons = GamepadButtons.None;
        }

        private void Core_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                (uri.Host != "retro-player-xbox.vercel.app" && uri.Host != "ps1-player.vercel.app"))
                args.Cancel = true;
        }

        private async void Core_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                StartupOverlay.Visibility = Visibility.Collapsed;
                await sender.ExecuteScriptAsync("window.RetroPlayerNativeHost=true;");
                await ReportControllerStatusAsync();
                PlayerWebView.Focus(FocusState.Programmatic);
            }
            else
            {
                StartupOverlay.Visibility = Visibility.Visible;
            }
        }

        private async void ControllerTimer_Tick(object sender, object e)
        {
            if (!_webViewReady || PlayerWebView.CoreWebView2 == null || _sendingInput)
                return;

            var gamepad = Gamepad.Gamepads.FirstOrDefault();
            if (gamepad == null)
            {
                if (_activeGamepad != null)
                {
                    _activeGamepad = null;
                    _previousButtons = GamepadButtons.None;
                    _lastStickX = 0;
                    _lastStickY = 0;
                    await ReportControllerStatusAsync();
                }
                return;
            }

            if (!ReferenceEquals(_activeGamepad, gamepad))
            {
                _activeGamepad = gamepad;
                _previousButtons = GamepadButtons.None;
                await ReportControllerStatusAsync();
            }

            var reading = gamepad.GetCurrentReading();
            var current = reading.Buttons;

            // D-pad and analog left-stick navigation share one focus movement path.
            await SendOnPressAsync(current, GamepadButtons.DPadUp, "ArrowUp");
            await SendOnPressAsync(current, GamepadButtons.DPadDown, "ArrowDown");
            await SendOnPressAsync(current, GamepadButtons.DPadLeft, "ArrowLeft");
            await SendOnPressAsync(current, GamepadButtons.DPadRight, "ArrowRight");
            await SendOnPressAsync(current, GamepadButtons.A, "A");
            await SendOnPressAsync(current, GamepadButtons.B, "B");
            await SendOnPressAsync(current, GamepadButtons.X, "X");
            await SendOnPressAsync(current, GamepadButtons.Y, "Y");
            await SendOnPressAsync(current, GamepadButtons.LeftShoulder, "LB");
            await SendOnPressAsync(current, GamepadButtons.RightShoulder, "RB");

            // Analog triggers are exposed as 0..1 values by the UWP Gamepad API.
            await SendTriggerOnPressAsync(reading.LeftTrigger, "LT");
            await SendTriggerOnPressAsync(reading.RightTrigger, "RT");

            var stickX = Math.Abs(reading.LeftThumbstickX) > 0.55 ? Math.Sign(reading.LeftThumbstickX) : 0;
            var stickY = Math.Abs(reading.LeftThumbstickY) > 0.55 ? -Math.Sign(reading.LeftThumbstickY) : 0;
            await SendStickDirectionAsync(stickX, stickY);

            _previousButtons = current;
        }

        private async Task SendOnPressAsync(GamepadButtons current, GamepadButtons button, string action)
        {
            bool isPressed = (current & button) == button;
            bool wasPressed = (_previousButtons & button) == button;
            if (isPressed && !wasPressed)
                await SendActionAsync(action);
        }

        private async Task SendTriggerOnPressAsync(double value, string action)
        {
            bool isPressed = value > 0.65;
            bool wasPressed = action == "LT"
                ? _previousLeftTrigger
                : _previousRightTrigger;
            if (isPressed && !wasPressed)
                await SendActionAsync(action);
            if (action == "LT") _previousLeftTrigger = isPressed;
            else _previousRightTrigger = isPressed;
        }

        private bool _previousLeftTrigger;
        private bool _previousRightTrigger;

        private async Task SendStickDirectionAsync(int x, int y)
        {
            if (x == 0 && y == 0)
            {
                _lastStickX = 0;
                _lastStickY = 0;
                return;
            }

            var now = DateTime.UtcNow;
            bool changed = x != _lastStickX || y != _lastStickY;
            bool repeatDue = (now - _lastStickRepeat).TotalMilliseconds >= 280;
            if (changed || repeatDue)
            {
                _lastStickX = x;
                _lastStickY = y;
                _lastStickRepeat = now;
                if (y < 0) await SendActionAsync("ArrowUp");
                else if (y > 0) await SendActionAsync("ArrowDown");
                if (x < 0) await SendActionAsync("ArrowLeft");
                else if (x > 0) await SendActionAsync("ArrowRight");
            }
        }

        private async Task SendActionAsync(string action)
        {
            if (PlayerWebView.CoreWebView2 == null)
                return;
            _sendingInput = true;
            try
            {
                await PlayerWebView.CoreWebView2.ExecuteScriptAsync(
                    "window.RetroPlayerNativeInput && window.RetroPlayerNativeInput('" + action + "');");
            }
            finally
            {
                _sendingInput = false;
            }
        }

        private async Task ReportControllerStatusAsync()
        {
            if (PlayerWebView.CoreWebView2 == null)
                return;
            var status = _activeGamepad == null ? "Waiting for a controller" : "Connected: Xbox / Windows gamepad";
            await PlayerWebView.CoreWebView2.ExecuteScriptAsync(
                "window.RetroPlayerNativeControllerStatus && window.RetroPlayerNativeControllerStatus('" + status + "');");
        }
    }
}