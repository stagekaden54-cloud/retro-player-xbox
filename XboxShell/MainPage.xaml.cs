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
        private GamepadButtons _previousButtons = GamepadButtons.None;
        private bool _webViewReady;

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
        }

        private void Core_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                (uri.Host != "retro-player-xbox.vercel.app" && uri.Host != "ps1-player.vercel.app"))
                args.Cancel = true;
        }

        private void Core_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                StartupOverlay.Visibility = Visibility.Collapsed;
                PlayerWebView.Focus(FocusState.Programmatic);
            }
            else
            {
                StartupOverlay.Visibility = Visibility.Visible;
            }
        }

        private async void ControllerTimer_Tick(object sender, object e)
        {
            if (!_webViewReady || PlayerWebView.CoreWebView2 == null)
                return;

            var gamepad = Gamepad.Gamepads.FirstOrDefault();
            if (gamepad == null)
            {
                _previousButtons = GamepadButtons.None;
                return;
            }

            var current = gamepad.GetCurrentReading().Buttons;
            await SendOnPressAsync(current, GamepadButtons.DPadUp, "ArrowUp");
            await SendOnPressAsync(current, GamepadButtons.DPadDown, "ArrowDown");
            await SendOnPressAsync(current, GamepadButtons.DPadLeft, "ArrowLeft");
            await SendOnPressAsync(current, GamepadButtons.DPadRight, "ArrowRight");
            await SendOnPressAsync(current, GamepadButtons.A, "A");
            await SendOnPressAsync(current, GamepadButtons.B, "B");
            _previousButtons = current;
        }

        private async Task SendOnPressAsync(GamepadButtons current, GamepadButtons button, string action)
        {
            bool isPressed = (current & button) == button;
            bool wasPressed = (_previousButtons & button) == button;
            if (!isPressed || wasPressed)
                return;

            await PlayerWebView.CoreWebView2.ExecuteScriptAsync(
                "(() => { const a = '" + action + "'; if (window.RetroPlayerNativeInput) { window.RetroPlayerNativeInput(a); return; } if (a === 'A') { document.activeElement?.click(); return; } if (a === 'B') { if (history.length > 1) history.back(); else document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true})); return; } document.dispatchEvent(new KeyboardEvent('keydown',{key:a,bubbles:true,cancelable:true})); })();");
        }

    }
}