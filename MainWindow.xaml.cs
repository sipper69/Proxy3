using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using System;
using Windows.Graphics;
using IO.Ably;
using IO.Ably.Realtime;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace Proxy3
{
    public sealed partial class MainWindow : Window
    {
        private AblyRealtime? client;
        private IRealtimeChannel? channel;
        public static CH9329? MyCH9329;
        public static bool CH9329_OK = false;
        public static SettingsManager? Settings;
        public MainWindow()
        {
            this.AppWindow.SetIcon(@"Assets\control.ico");
            this.AppWindow.MoveAndResize(new RectInt32(200, 100, 350, 30));
            this.InitializeComponent();

            var presenter = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this))).Presenter as OverlappedPresenter;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;

            Settings = new SettingsManager();
            Settings.LoadSettings();
            ApplySettings();

            this.Closed += Window_Closed;
            webView.Loaded += webView_Loaded;

            if (Settings.AblyAPI != "") { InitAbly(); } else { log("missing Ably API key -> add correct Ably API key!"); }
        }
        private async Task ApplySettings()
        {
            await Settings.GetVideoDevices(vSource);
            vSource.SelectedValue = Settings.vSource;
            vSource.SelectionChanged += (sender, e) => { if (vSource.SelectedItem is ComboBoxItem selectedItem) { Settings.vSource = selectedItem.Tag.ToString(); SettingsManager.SaveSetting("vSource", Settings.vSource); } };

            AblyAPI.Password = Settings.AblyAPI;
            AblyAPI.PasswordChanged += (sender, e) => { Settings.AblyAPI = AblyAPI.Password; SettingsManager.SaveSetting("AblyAPI", Settings.AblyAPI); };

            STUN.Items.Add(new ComboBoxItem { Content = "STUN (Google)", Tag = "Google" });
            STUN.Items.Add(new ComboBoxItem { Content = "TURN (Twilio)", Tag = "Twilio" });
            STUN.SelectedValue = Settings.STUN;
            STUN.SelectionChanged += (sender, e) => { if (STUN.SelectedItem is ComboBoxItem selectedItem) { Settings.STUN = selectedItem.Tag.ToString(); SettingsManager.SaveSetting("STUN", Settings.STUN); TwilioActive(Settings.STUN != "Google"); } };

            TwilioSID.Password = Settings.TwilioSID;
            TwilioSID.PasswordChanged += (sender, e) => { Settings.TwilioSID = TwilioSID.Password; SettingsManager.SaveSetting("TwilioSID", Settings.TwilioSID); };

            TwilioAuth.Password = Settings.TwilioAuth;
            TwilioAuth.PasswordChanged += (sender, e) => { Settings.TwilioAuth = TwilioAuth.Password; SettingsManager.SaveSetting("TwilioAuth", Settings.TwilioAuth); };

            TwilioActive(Settings.STUN != "Google");
        }
        private void Window_Closed(object sender, WindowEventArgs e)
        {
            if (webView.CoreWebView2 != null) { webView.CoreWebView2.PermissionRequested -= webView_PermissionRequested; }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double Scale = this.Content.XamlRoot.RasterizationScale;
            RectInt32 Size = new RectInt32(200, 100, (int)(990 * Scale), (int)(650 * Scale));
            this.AppWindow.MoveAndResize(Size);
        }
        public void log(string message, string id="")
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (id == "") { id = "proxy3"; }
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run { Text = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + id + "\t" + message });
                LogText.Blocks.Insert(0, paragraph);

                // Prevent log from infinite growth, drop older lines > 1000
                if (LogText.Blocks.Count > 1000) { LogText.Blocks.RemoveAt(LogText.Blocks.Count - 1); }
            });
        }
        public void TwilioActive(bool active = true)
        {
            LTwilioSID.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            TwilioSID.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            LTwilioAuth.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            TwilioAuth.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        }

        // Ably session routines
        private void InitAbly()
        {
            client = new AblyRealtime(Settings.AblyAPI);

            client.Connection.On(stateChange =>
            {
                if (stateChange.Current == ConnectionState.Failed)
                {
                    log("wrong Ably API key -> add correct Ably API key!");
                }

                if (stateChange.Current == ConnectionState.Connected)
                {
                    log("connected to Ably");
                    channel = client.Channels.Get("Control3");  // No need to create this on Ably. Just use any app, any channel
                    channel.Subscribe("Status", Ably_MessageReceived);
                    log("subscribed to channel 'Control3'");
                }
            });
        }
        private async void Ably_SendMessage(string str)
        {
            var message = new IO.Ably.Message { Data = str, ClientId = "proxy3" };
            if (channel != null) { await channel.PublishAsync("Status", message); }
        }
        private async void Ably_MessageReceived(IO.Ably.Message message)
        {
            var receivedMessage = message.Data.ToString();
            if (receivedMessage != null)
            {
                JObject jObject = JObject.Parse(receivedMessage);
                string dataValue = jObject["data"].ToString();
                string id = jObject["clientId"].ToString();
                log(dataValue, id);

                if (dataValue == "start")
                {
                    Ably_SendMessage("start WebRTC ");
                    await SetCH9329();
                    DispatcherQueue.TryEnqueue(async () => { await webView.CoreWebView2.ExecuteScriptAsync("startStreaming();"); });
                }

                if (dataValue == "ping")
                {
                    Ably_SendMessage("ready");
                }

                if ((id=="proxy3" && dataValue == "connection state: disconnected") || dataValue == "stop")
                {
                    if (MyCH9329 != null) { MyCH9329.closeSerial(); MyCH9329 = null; log("CH9329 closed"); }
                    DispatcherQueue.TryEnqueue(async () => { await webView.CoreWebView2.ExecuteScriptAsync("stopStreaming();"); });
                    log("videostream closed");
                }
            }
        }

        // WebRTC Session routines
        private void webView_PermissionRequested(CoreWebView2 sender, CoreWebView2PermissionRequestedEventArgs args)  // Allow Camera access
        {
            if (args.PermissionKind == CoreWebView2PermissionKind.Camera) { args.State = CoreWebView2PermissionState.Allow; }
        }
        private async void webView_Loaded(object sender, RoutedEventArgs e)
        {
            log("initializing webView2");
            await webView.EnsureCoreWebView2Async();

            webView.CoreWebView2.PermissionRequested += webView_PermissionRequested;
            webView.CoreWebView2.WebMessageReceived += webView_MessageReceived;
            webView.CoreWebView2.NavigationCompleted += webView_NavigationCompleted;
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(hostName: "control", folderPath: "Assets", accessKind: CoreWebView2HostResourceAccessKind.Allow);
            webView.Source = new Uri("https://control/webrtc.html");
        }
        private void webView_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            log("webView2 - HTTP Status " + args.HttpStatusCode.ToString());
            if (args.IsSuccess)
            {
                string script = $"window.initialize('{Settings.vSource}', '{Settings.AblyAPI}', '{Settings.STUN}', '{Settings.TwilioSID}', '{Settings.TwilioAuth}');";
                DispatcherQueue.TryEnqueue(async () => { await webView.CoreWebView2.ExecuteScriptAsync(script); Ably_SendMessage("ready"); });
            }
        }
        private void webView_MessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            if (!CH9329_OK) { return; }

            ReadOnlySpan<char> cmdSpan = args.TryGetWebMessageAsString().AsSpan();
            if (cmdSpan.Slice(0, 3).SequenceEqual("MMA"))
            {
                if (int.TryParse(cmdSpan.Slice(4, 4), out int xPos) && int.TryParse(cmdSpan.Slice(9, 4), out int yPos) && int.TryParse(cmdSpan.Slice(14, 4), out int xSize) && int.TryParse(cmdSpan.Slice(19, 4), out int ySize)) 
                { MyCH9329.mouseMoveAbs(xPos, yPos, xSize, ySize); }
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("MMR"))
            {
                if (int.TryParse(cmdSpan.Slice(4, 5), out int x) && int.TryParse(cmdSpan.Slice(10, 5), out int y)) 
                { MyCH9329.mouseMoveRel(x, y); }
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("KUP"))
            {
                if (int.TryParse(cmdSpan.Slice(4, 3), out int decoration)) 
                { MyCH9329.keyUp((byte)decoration); }
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("KDN"))
            {
                if (int.TryParse(cmdSpan.Slice(4, 3), out int decoration) && int.TryParse(cmdSpan.Slice(8, 3), out int k1)) 
                { MyCH9329.keyDown((byte)decoration, (byte)k1); }
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("MBD"))
            {
                if (int.TryParse(cmdSpan.Slice(4, 3), out int buttonValue)) 
                { var buttonCode = (CH9329.MouseButtonCode)buttonValue; MyCH9329.mouseButtonDown(buttonCode); }
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("MBU"))
            {
                MyCH9329.mouseButtonUpAll();
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("MSC"))
            {
                if (int.TryParse(cmdSpan.Slice(4, 3), out int scrollCount)) 
                { MyCH9329.mouseScroll(scrollCount); }
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("SQY"))
            {
                if (int.TryParse(cmdSpan.Slice(4, 1), out int International)) 
                { MyCH9329.StringTypeQWERTY(cmdSpan.Slice(6).ToString(), International == 1); }
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("SAY"))
            {
                MyCH9329.StringTypeAZERTY(cmdSpan.Slice(4).ToString());
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("SQZ"))
            {
                MyCH9329.StringTypeQWERTZ(cmdSpan.Slice(4).ToString());
            }
            else if (cmdSpan.Slice(0, 3).SequenceEqual("SHK"))
            {
                MyCH9329.SyngoHotkey();
            }
        }

        // Initialize CH9329 cable
        private async Task SetCH9329()
        {
            CH9329_OK = false;
            log("searching CH9329");
            
            Dictionary<string, string> serialPorts = Settings.GetSerialPorts();

            int[] baudRates = { 9600, 19200, 38400, 57600 };
            foreach (var port in serialPorts)
            {
                foreach (var baudRate in baudRates)
                {
                    CH9329 ch9329 = new CH9329(port.Key, baudRate);
                    if (!ch9329.Error)
                    {
                        await ch9329.getInfo();
                        ch9329.closeSerial();

                        if (ch9329.CHIP_STATUS == 1)
                        {
                            MyCH9329 = new CH9329(port.Key, baudRate);
                            Ably_SendMessage("CH9329 Initialized - " + port.Key + "/" + baudRate);
                            CH9329_OK = true;
                            return;
                        }
                    }
                }
            }
            Ably_SendMessage("CH9329 initialization failed");
        }
    }
}
