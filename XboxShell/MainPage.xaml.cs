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
        private bool _controllerTickRunning;
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
            bool completed = message.StartsWith(completedPrefix, StringComparison.Ordinal);
            ShellStatus.Text = completed ? "Game imported · Ready to play" : "Game import did not complete";
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
            if (!_webViewReady || PlayerWebView.CoreWebView2 == null || _controllerTickRunning)
                return;

            _controllerTickRunning = true;
            try
            {
                var gamepad = Gamepad.Gamepads.FirstOrDefault();
                if (gamepad == null)
                {
                    if (_activeGamepad != null)
                    {
                        _activeGamepad = null;
                        _previousButtons = GamepadButtons.None;
                        _previousLeftTrigger = false;
                        _previousRightTrigger = false;
                        _lastStickX = 0;
                        _lastStickY = 0;
                        _lastStickRepeat = DateTime.MinValue;
                        await ReportControllerStatusAsync();
                    }
                    return;
                }

                if (!ReferenceEquals(_activeGamepad, gamepad))
                {
                    _activeGamepad = gamepad;
                    _previousButtons = GamepadButtons.None;
                    _previousLeftTrigger = false;
                    _previousRightTrigger = false;
                    _lastStickX = 0;
                    _lastStickY = 0;
                    await ReportControllerStatusAsync();
                }

                var reading = gamepad.GetCurrentReading();
                var current = reading.Buttons;
                var actions = new List<string>();
                AddOnPress(actions, current, GamepadButtons.DPadUp, "ArrowUp");
                AddOnPress(actions, current, GamepadButtons.DPadDown, "ArrowDown");
                AddOnPress(actions, current, GamepadButtons.DPadLeft, "ArrowLeft");
                AddOnPress(actions, current, GamepadButtons.DPadRight, "ArrowRight");
                AddOnPress(actions, current, GamepadButtons.A, "A");
                AddOnPress(actions, current, GamepadButtons.B, "B");
                AddOnPress(actions, current, GamepadButtons.X, "X");
                AddOnPress(actions, current, GamepadButtons.Y, "Y");
                AddOnPress(actions, current, GamepadButtons.LeftShoulder, "LB");
                AddOnPress(actions, current, GamepadButtons.RightShoulder, "RB");
                AddTriggerOnPress(actions, reading.LeftTrigger, "LT");
                AddTriggerOnPress(actions, reading.RightTrigger, "RT");

                var stickX = Math.Abs(reading.LeftThumbstickX) > 0.55 ? Math.Sign(reading.LeftThumbstickX) : 0;
                var stickY = Math.Abs(reading.LeftThumbstickY) > 0.55 ? -Math.Sign(reading.LeftThumbstickY) : 0;
                AddStickDirection(actions, stickX, stickY);
                _previousButtons = current;

                if (actions.Count > 0)
                    await SendActionsAsync(actions);
            }
            finally
            {
                _controllerTickRunning = false;
            }
        }

        private void AddOnPress(List<string> actions, GamepadButtons current, GamepadButtons button, string action)
        {
            bool isPressed = (current & button) == button;
            bool wasPressed = (_previousButtons & button) == button;
            if (isPressed && !wasPressed)
                actions.Add(action);
        }

        private void AddTriggerOnPress(List<string> actions, double value, string action)
        {
            bool isPressed = value > 0.65;
            bool wasPressed = action == "LT" ? _previousLeftTrigger : _previousRightTrigger;
            if (isPressed && !wasPressed)
                actions.Add(action);
            if (action == "LT") _previousLeftTrigger = isPressed;
            else _previousRightTrigger = isPressed;
        }

        private void AddStickDirection(List<string> actions, int x, int y)
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
                if (y < 0) actions.Add("ArrowUp");
                else if (y > 0) actions.Add("ArrowDown");
                if (x < 0) actions.Add("ArrowLeft");
                else if (x > 0) actions.Add("ArrowRight");
            }
        }

        private async Task SendActionsAsync(List<string> actions)
        {
            if (PlayerWebView.CoreWebView2 == null || actions == null || actions.Count == 0)
                return;

            var jsonActions = new JsonArray();
            foreach (var action in actions)
                jsonActions.Add(JsonValue.CreateStringValue(action));

            await PlayerWebView.CoreWebView2.ExecuteScriptAsync(
                "window.RetroPlayerNativeInputBatch && window.RetroPlayerNativeInputBatch(" + jsonActions.ToString() + ");");
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