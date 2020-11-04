using System;
using System.Collections.Generic;
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


        public iVlixOPCContract WCFChannel;
        public DateTime LastConsoleLogRead;
        bool PausePeriodicLogRefresh = false;
        WCFClient<iVlixOPCContract> wCFClient;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            gdVlixOPC.Visibility = Visibility.Collapsed;
            //Connect to Open Plant OPC Service
            ThreadPool.QueueUserWorkItem(delegate
            {
                string LocalPipeName = "VlixOPC";
                this.OPCClassic_Browser.ConnectToBackEndViaLocalHostPipe(LocalPipeName);
                this.OPCUA_Browser.ConnectToBackEndViaLocalHostPipe(LocalPipeName);
                (wCFClient = new WCFClient<iVlixOPCContract>(LocalPipeName, "",500)
                {
                    OnWCFConnected = (Channel, OPConnection) =>
                    {
                        WCFChannel = (iVlixOPCContract)Channel;
                        this.Dispatcher.BeginInvoke(new Action(() => { gdVlixOPC.Visibility = Visibility.Visible; }));
                    },
                    OnWCFDisconnected = () =>
                    {
                        WCFChannel = null;
                        this.Dispatcher.BeginInvoke(new Action(() => gdVlixOPC.Visibility = Visibility.Hidden));
                    }
                }).Start();
                if (wCFClient.ConnectionOK) this.Dispatcher.BeginInvoke(new Action(() => gdVlixOPC.Visibility = Visibility.Visible));
                else this.Dispatcher.BeginInvoke(new Action(() => gdVlixOPC.Visibility = Visibility.Hidden));
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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                    if (WCFChannel != null && WCFChannel.TryGetSettings(out OPCBackEndConfig oPCBackEndConfig))
                    {
                        SettingsVM settingsVM = new SettingsVM(oPCBackEndConfig);
                        tiSettings.DataContext = settingsVM;
                        pbHTTPSAuthentication.Password = settingsVM.Password_ForAPIBasicAuthentication;
                    }
                }
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

        private void oPBSettings_Save_Click(object sender, RoutedEventArgs e)
        {
            cmSettings.ShowMessageProcess();
            ((SettingsVM)tiSettings.DataContext).Password_ForAPIBasicAuthentication = pbHTTPSAuthentication.Password;
            OPCBackEndConfig oPCBackEndConfig = ((SettingsVM)tiSettings.DataContext).ToOPCBackEndConfig();
            ThreadPool.QueueUserWorkItem(async delegate
            {
                if (WCFChannel != null && WCFChannel.TrySaveSettings(oPCBackEndConfig))
                {
                    await Task.Delay(1000);
                    await this.Dispatcher.BeginInvoke(new Action(() => 
                    {
                        SettingsVM settingsVM = new SettingsVM(oPCBackEndConfig);
                        tiSettings.DataContext = settingsVM;
                        cmSettings.ShowMessageSuccess();
                    }));                    
                }
            });            
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
