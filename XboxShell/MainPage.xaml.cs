using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Windows.Gaming.Input;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Data.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;

namespace RetroPlayerXbox
{
    public sealed partial class MainPage : Page
    {
        private const string HomeUrl = "https://retro-player-xbox.vercel.app/";
        private const string LocalHostName = "retroplayer.local";
        private readonly DispatcherTimer _controllerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        private readonly Dictionary<string, StorageFile> _pendingImports = new Dictionary<string, StorageFile>();
        private Gamepad _activeGamepad;
        private GamepadButtons _previousButtons = GamepadButtons.None;
        private bool _webViewReady;
        private bool _sendingInput;
        private bool _previousLeftTrigger;
        private bool _previousRightTrigger;
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
                core.WebMessageReceived += Core_WebMessageReceived;
                core.SetVirtualHostNameToFolderMapping(
                    LocalHostName,
                    ApplicationData.Current.LocalFolder.Path,
                    CoreWebView2HostResourceAccessKind.Allow);

                core.Navigate(HomeUrl);
                _webViewReady = true;
                _controllerTimer.Tick += ControllerTimer_Tick;
                _controllerTimer.Start();
                UpdateShellStatus();
                PlayerWebView.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                ShowStartupError("Could not initialize the Xbox web runtime: " + ex.Message);
            }
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _controllerTimer.Stop();
            _controllerTimer.Tick -= ControllerTimer_Tick;
            _activeGamepad = null;
            _previousButtons = GamepadButtons.None;
        }

        private void ShowStartupError(string message)
        {
            StartupOverlay.Visibility = Visibility.Visible;
            StartupStatus.Text = message;
            RetryButton.Visibility = Visibility.Visible;
            ShellStatus.Text = "Xbox interface unavailable";
        }

        private void UpdateShellStatus()
        {
            var connected = NetworkInformation.GetInternetConnectionProfile() != null;
            ShellStatus.Text = connected ? "Native Xbox shell ready · Online" : "Native Xbox shell ready · Offline";
        }

        private void Core_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                (uri.Host != "retro-player-xbox.vercel.app" && uri.Host != "ps1-player.vercel.app"))
                args.Cancel = true;
            StartupStatus.Text = "Connecting to Retro Player…";
        }

        private async void Core_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                StartupOverlay.Visibility = Visibility.Collapsed;
                RetryButton.Visibility = Visibility.Collapsed;
                await sender.ExecuteScriptAsync("window.RetroPlayerNativeHost=true;");
                await ReportControllerStatusAsync();
                PlayerWebView.Focus(FocusState.Programmatic);
                ShellStatus.Text = "Native Xbox shell ready · Interface connected";
            }
            else
            {
                ShowStartupError("The Retro Player interface could not be reached. Check the Xbox network connection and retry.");
            }
        }

        private async void Core_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            string message;
            try { message = args.TryGetWebMessageAsString(); }
            catch { return; }

            if (String.IsNullOrWhiteSpace(message))
                return;

            const string completedPrefix = "import-complete:";
            const string failedPrefix = "import-failed:";
            string token = message.StartsWith(completedPrefix, StringComparison.Ordinal)
                ? message.Substring(completedPrefix.Length)
                : message.StartsWith(failedPrefix, StringComparison.Ordinal)
                    ? message.Substring(failedPrefix.Length)
                    : null;

            if (token == null || !_pendingImports.ContainsKey(token))
                return;

            var stagedFile = _pendingImports[token];
            _pendingImports.Remove(token);
            try { await stagedFile.DeleteAsync(); }
            catch { /* A leftover staging copy is harmless; the library copy is already independent. */ }
        }

        private async void ImportGame_Click(object sender, RoutedEventArgs e)
        {
            if (!_webViewReady || PlayerWebView.CoreWebView2 == null)
            {
                ShowStartupError("Wait for the Retro Player interface to finish loading before importing a game.");
                return;
            }

            try
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                foreach (var extension in new[] { ".bin", ".iso", ".img", ".pbp", ".chd", ".z64", ".n64", ".v64" })
                    picker.FileTypeFilter.Add(extension);

                var selectedFile = await picker.PickSingleFileAsync();
                if (selectedFile == null)
                    return;

                var token = Guid.NewGuid().ToString("N");
                var extensionPart = System.IO.Path.GetExtension(selectedFile.Name);
                var stagedName = token + extensionPart;
                var stagedFile = await selectedFile.CopyAsync(
                    ApplicationData.Current.LocalFolder,
                    stagedName,
                    NameCollisionOption.GenerateUniqueName);

                _pendingImports[token] = stagedFile;
                var message = new JsonObject();
                message.Add("type", JsonValue.CreateStringValue("native-import-file"));
                message.Add("token", JsonValue.CreateStringValue(token));
                message.Add("fileName", JsonValue.CreateStringValue(selectedFile.Name));
                message.Add("url", JsonValue.CreateStringValue(
                    "https://" + LocalHostName + "/" + Uri.EscapeDataString(stagedFile.Name)));
                PlayerWebView.CoreWebView2.PostWebMessageAsJson(message.ToString());
                ShellStatus.Text = "Importing " + selectedFile.Name + "…";
            }
            catch (Exception ex)
            {
                await new MessageDialog("The Xbox file picker could not import that file. " + ex.Message,
                    "Import game").ShowAsync();
                UpdateShellStatus();
            }
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerWebView.CoreWebView2 != null)
                PlayerWebView.CoreWebView2.Navigate(HomeUrl);
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerWebView.CoreWebView2 != null)
            {
                StartupOverlay.Visibility = Visibility.Visible;
                RetryButton.Visibility = Visibility.Collapsed;
                StartupStatus.Text = "Reconnecting to Retro Player…";
                PlayerWebView.CoreWebView2.Reload();
            }
            else
            {
                MainPage_Loaded(this, e);
            }
        }

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var connected = NetworkInformation.GetInternetConnectionProfile() != null;
            var controller = _activeGamepad == null ? "No controller detected" : "Xbox / Windows gamepad connected";
            var details = "Retro Player Xbox native shell\nVersion 0.2.0\n\n" +
                "Network: " + (connected ? "Connected" : "Offline") + "\n" +
                "Controller: " + controller + "\n\n" +
                "The native app provides file picking, controller input, and the Xbox host interface. " +
                "The emulator runtime is loaded by the Retro Player web interface.";
            await new MessageDialog(details, "About Retro Player Xbox").ShowAsync();
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
            bool wasPressed = action == "LT" ? _previousLeftTrigger : _previousRightTrigger;
            if (isPressed && !wasPressed)
                await SendActionAsync(action);
            if (action == "LT") _previousLeftTrigger = isPressed;
            else _previousRightTrigger = isPressed;
        }

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
                var safeAction = action.Replace("\\", "").Replace("'", "");
                await PlayerWebView.CoreWebView2.ExecuteScriptAsync(
                    "window.RetroPlayerNativeInput && window.RetroPlayerNativeInput('" + safeAction + "');");
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
            var status = _activeGamepad == null ? "Waiting for a controller" : "Connected: Xbox controller";
            ControllerStatus.Text = _activeGamepad == null ? "Controller: searching" : "Controller: connected";
            await PlayerWebView.CoreWebView2.ExecuteScriptAsync(
                "window.RetroPlayerNativeControllerStatus && window.RetroPlayerNativeControllerStatus('" + status + "');");
        }
    }
}