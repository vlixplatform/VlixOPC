using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Vlix;

namespace VlixOPC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            Logger.DisableLogger();            
            this.Visibility = Visibility.Collapsed;
            InitializeComponent();            
        }


        public iOPCServiceContract WCFChannel;
        public DateTime LastConsoleLogRead;
        bool PausePeriodicLogRefresh = false;
        WCFClient<iOPCServiceContract> wCFClient;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            gdOPCService.Visibility = Visibility.Collapsed;
            //Connect to Open Plant OPC Service
            ThreadPool.QueueUserWorkItem(delegate
            {
                string LocalPipeName = "VlixOPC";
                this.OPCClassic_Browser.ConnectToBackEndViaLocalHostPipe(LocalPipeName);
                this.OPCUA_Browser.ConnectToBackEndViaLocalHostPipe(LocalPipeName);
                (wCFClient = new WCFClient<iOPCServiceContract>(LocalPipeName, "",500)
                {
                    OnWCFConnected = (Channel, OPConnection) =>
                    {
                        WCFChannel = (iOPCServiceContract)Channel;
                        this.Dispatcher.BeginInvoke(new Action(() => { gdOPCService.Visibility = Visibility.Visible; }));
                    },
                    OnWCFDisconnected = () =>
                    {
                        WCFChannel = null;
                        this.Dispatcher.BeginInvoke(new Action(() => gdOPCService.Visibility = Visibility.Hidden));
                    }
                }).Start();
                if (wCFClient.ConnectionOK) this.Dispatcher.BeginInvoke(new Action(() => gdOPCService.Visibility = Visibility.Visible));
                else this.Dispatcher.BeginInvoke(new Action(() => gdOPCService.Visibility = Visibility.Hidden));
            });

            ThreadPool.QueueUserWorkItem(delegate
            {
                while (true)
                {
                    if (!PausePeriodicLogRefresh && WCFChannel != null)
                    {
                        try
                        {
                            if (WCFChannel.TryGetLogs(LastConsoleLogRead, out List<Vlix.LogStruct> LogList))
                            {
                                LogConsole.AddLogs(LogList);
                                LastConsoleLogRead = LogList.Last().TimeStampInUTC;
                            }
                        }
                        catch { }
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tc)
            {
                //Activity Logs Tab
                if (tc.SelectedIndex == 0)
                {
                    PausePeriodicLogRefresh = false;
                }
                else PausePeriodicLogRefresh = true;

                //Settings Tab
                if (tc.SelectedIndex == 1)
                {
                    try
                    {
                        await LoadSettings();
                    }
                    catch
                    {
                        ((SettingsVM)tiSettings.DataContext).SettingsError = true;
                        cmSettings.ShowMessageError("Failed to obtains settings!");
                    }
                }
            }
        }

        private async Task LoadSettings()
        {
            try
            {
                var res = await WCFChannel.TryGetSettings();
                if (res.Success)
                {
                    SettingsVM settingsVM = new SettingsVM(res.OPCBackEndConfig);
                    settingsVM.SettingsError = false;
                    tiSettings.DataContext = settingsVM;
                    pbHTTPSAuthentication.Password = settingsVM.Password_ForAPIBasicAuthentication;
                    pbTCPAuthentication.Password = settingsVM.Tcp_Password;
                }
                else
                {
                    ((SettingsVM)tiSettings.DataContext).SettingsError = true;
                    cmSettings.ShowMessageError("Load Failed!");
                    Logger.Log("Load failed\r\n" + res.ExStr, true);
                }
            }
            catch (Exception ex)
            {
                ((SettingsVM)tiSettings.DataContext).SettingsError = true;
                cmSettings.ShowMessageError("Load Failed!");
                Logger.Log("Load failed\r\n" + ex.ToString(), true);
            }
        }
        


        private void OPC_Browser_OKClick(object sender, RoutedEventArgs e)
        {
            if (sender is OPCBrowser OB)
            {
                string ClipboardText = "";
                if (OB.OPCBrowserType == OPCBrowserType.OPCUA) foreach (var T in OB.SelectedTags) ClipboardText += T.NodeId + ",";
                if (OB.OPCBrowserType == OPCBrowserType.OPCClassic)  foreach (var T in OB.SelectedTags) ClipboardText += T.Name + ",";
                ClipboardText = ClipboardText.RemoveLastCharacter();
                Clipboard.SetText(ClipboardText);
                MessageBox.Show("This is just a test. The " + OB.SelectedTags.Count + " selected items have been copied to Clipboard!");
            }
        }

        private async void oPBSettings_Save_Click(object sender, RoutedEventArgs e)
        {
            cmSettings.ShowMessageProcess();
            ((SettingsVM)tiSettings.DataContext).Password_ForAPIBasicAuthentication = pbHTTPSAuthentication.Password;
            ((SettingsVM)tiSettings.DataContext).Tcp_Password = pbTCPAuthentication.Password;
            OPCBackEndConfig oPCBackEndConfig = ((SettingsVM)tiSettings.DataContext).ToOPCBackEndConfig();
            try
            {
                var res = await WCFChannel.TrySaveSettings(oPCBackEndConfig);
                if (res.Success)
                {
                    await Task.Delay(500);
                    SettingsVM settingsVM = new SettingsVM(oPCBackEndConfig);                        
                    tiSettings.DataContext = settingsVM;
                    cmSettings.ShowMessageSuccess();
                }
                else
                {
                    cmSettings.ShowMessageError("Save Failed!");
                    Logger.Log("Save settings failed\r\n" + res.ExStr, true);
                }
            }
            catch (Exception ex)
            {
                cmSettings.ShowMessageError("Save Failed!");
                Logger.Log("Save settings failed\r\n" + ex.ToString(), true);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private async void oPBSettings_Retry_Click(object sender, RoutedEventArgs e)
        {
            await LoadSettings();
        }
    }


    public class BoolORToFalseMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            for (int nV1 = 0; nV1 < values.Count(); nV1++)
            {
                for (int nV2 = nV1; nV2 < values.Count(); nV2++)
                {
                    if (values[nV1] is bool v1 && values[nV2] is bool v2)
                    {
                        if (v1 || v2) return false;
                    }
                }
            }
            return true;
            // if (values[0] is bool v1 && values[1] is bool v2) return v1 || v2; else return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
