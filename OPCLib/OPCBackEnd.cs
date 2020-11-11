using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vlix;

namespace VlixOPC
{
    public class RegisteredTag
    {
        public RegisteredTag() { }
        public RegisteredTag(string Id, int UpdateIntervalInMS)
        {
            this.Id = Id;
            this.UpdateIntervalInMS = UpdateIntervalInMS;
        }
        public string Id { get; set; } = null;
        public int UpdateIntervalInMS { get; set; } = 1000;
        public IComparable Value { get; set; } = null;
        public DateTime LastCalled { get; set; } = DateTime.MinValue;
        public DateTime TSUTC { get; set; } = DateTime.MinValue;
        public DateTime SourceTSUTC { get; set; } = DateTime.MinValue;
        public bool QualityOK { get; set; } = true;
    }

    public class OPCBackEnd
    {
        public static OPCClassicBrowserEngine OPCClassicBrowserEngine;
        public static OPCUABrowserEngine OPCUABrowserEngine;
        public static List<DateTime> TotalAPICalls = new List<DateTime>();
        public static WCFHost<OPCServiceContract, iOPCServiceContract> WCFHost_Pipe = null;
        public static WCFHost<OPCServiceContract, iOPCServiceContract> WCFHost_Tcp = null;
        public static WCFHostWeb<OPCBrowserContract, iOPCBrowserContract> WCFHost_Http = null;
        public static WCFHostWeb<OPCBrowserContract, iOPCBrowserContract> WCFHost_Https = null;

        /// <summary>
        /// THe main Class for the OPC Back End
        /// </summary>
        /// <param name="LocalPipeName">Use for OPC Admin Communication</param>
        public OPCBackEnd(string LocalPipeName = null)
        {
            Logger.ClearLogs();
            if (!Global.Product.TryLoadConfigFile()) throw new CustomException("FATAL ERROR: Unable to load Config File '" + Global.Product.ConfigFilePath + "'");
            OPCBackEnd.Config = (OPCBackEndConfig)Global.Product.Config;               
            Logger.EnableLogger();
            Thread.Sleep(100);
            Logger.Log("***************************************************");
            Thread.Sleep(100);
            Logger.Log("VLIX OPC (BACK END) STARTED");
            Thread.Sleep(100);
            Logger.Log("***************************************************");
            Thread.Sleep(100);
            Logger.Log("Log Path = '" + Global.Product.LogFilePath + "'");
            OPCBackEnd.OPCClassicBrowserEngine = new OPCClassicBrowserEngine();
            OPCBackEnd.OPCUABrowserEngine = new OPCUABrowserEngine();


            //*************************************************
            //   REPORT NUMBER OF CALLS PER MINUTE
            //*************************************************
            (new Thread(async () =>
            {
                while (true)
                {
                    DateTime DTGate = DateTime.UtcNow.AddMinutes(-1);
                    int TotalCallsLastMinute = TotalAPICalls.Count(C => C > DTGate);
                    Logger.Log("Total API Calls Last Minute = " + TotalCallsLastMinute);
                    try { TotalAPICalls.RemoveWhere(C => C < DTGate); } catch { }
                    await Task.Delay(120000);
                }
            })).Start();


            //*************************************************
            //   ENABLE NAMED PIPE AND NET TCP FOR CONFIGURATION FRONT END
            //*************************************************
            if (LocalPipeName.IsNullOrWhiteSpace()) LocalPipeName = "VlixOPC";
            //string LocalPipeName = GlobalWCF.GetLocalPipeName();
            WCFHost_Pipe = new WCFHost<OPCServiceContract, iOPCServiceContract>(LocalPipeName, "");
            WCFHost_Pipe.Start();
            WCFHost_Tcp = new WCFHost<OPCServiceContract, iOPCServiceContract>(OPCBackEnd.Config.Tcp_Port, HostProtocolType.NetTCPSecure, OPCBackEnd.Config.Enable_Tcp_Authentication)
            {
                OnValidatingUsernamePassword = (string UN, string PW, out UserBase user) =>
                {
                    user = null;
                    return (string.Equals(UN, OPCBackEnd.Config.Tcp_Username, StringComparison.OrdinalIgnoreCase) && PW == OPCBackEnd.Config.Tcp_Password);
                }
            };
            WCFHost_Tcp.Start();


            //****************************************
            //   ENABLE HTTP
            //****************************************
            if (OPCBackEnd.Config.Enable_WebAPI_Http) OPCBackEnd.EnableHttp(); else Logger.Log("Http API not Enabled");


            //****************************************
            //   ENABLE HTTPS
            //****************************************
            if (OPCBackEnd.Config.Enable_WebAPI_Https) OPCBackEnd.EnableHttps(); else Logger.Log("Https Secure API not Enabled");
        }

        static object Http_EnableOneAtATime = new object();
        public static void EnableHttp()
        {
            lock (Http_EnableOneAtATime)
            {
                Logger.Log("Enabling Http Host on Port " + OPCBackEnd.Config.Http_Port + "...");
                WCFHost_Http = new WCFHostWeb<OPCBrowserContract, iOPCBrowserContract>(OPCBackEnd.Config.Http_Port, HostProtocolType.HTTP, OPCBackEnd.Config.Enable_WebAPI_BasicAuthentication);
                if (OPCBackEnd.Config.Enable_WebAPI_BasicAuthentication)
                {
                    Logger.Log("Enabling Username and Password Authentication for Http Host...");
                    WCFHost_Http.EnableUserAuthentication = true;
                    WCFHost_Http.OnValidatingUsernamePassword = (string UN, string PW, out UserBase User) =>
                    {
                        User = null;
                        return (string.Equals(UN, OPCBackEnd.Config.WebAPI_Username,StringComparison.OrdinalIgnoreCase) && PW == OPCBackEnd.Config.WebAPI_Password);
                    };
                }
                WCFHost_Http.Start();
            }
        }

        static object Https_EnableOneAtATime = new object();
        public static void EnableHttps()
        {
            lock (Https_EnableOneAtATime)
            {
                Logger.Log("Enabling Https (Secure) Host on Port " + OPCBackEnd.Config.Https_Port + "...");
                WCFHost_Https = new WCFHostWeb<OPCBrowserContract, iOPCBrowserContract>(OPCBackEnd.Config.Https_Port, HostProtocolType.HTTPS, OPCBackEnd.Config.Enable_WebAPI_BasicAuthentication);
                if (OPCBackEnd.Config.Enable_WebAPI_BasicAuthentication)
                {
                    Logger.Log("Enabling Username and Password Authentication for Https Secure Host...");
                    WCFHost_Https.EnableUserAuthentication = true;
                    WCFHost_Https.OnValidatingUsernamePassword = (string UN, string PW, out UserBase User) =>
                    {
                        User = null;
                        return (string.Equals(UN, OPCBackEnd.Config.WebAPI_Username, StringComparison.OrdinalIgnoreCase) && PW == OPCBackEnd.Config.WebAPI_Password);
                    };
                }
                WCFHost_Https.Start();
            }
        }

        Serializer ConfigSerializer = new Serializer();
        //public string ProgramDataDir { get; set; } = null;
        //public string LogFilePath { get; set; } = null;
        //public string ConfigFilePath { get; set; } = null;
        public static OPCBackEndConfig Config { get; set; } = new OPCBackEndConfig();


        public void Stop()
        {
            Logger.Log("Stopping Vlix OPC Server...");
            OPCBackEnd.OPCClassicBrowserEngine?.Disconnect();
            OPCBackEnd.OPCUABrowserEngine?.Disconnect();
            OPCBackEnd.WCFHost_Pipe?.Stop();
            OPCBackEnd.WCFHost_Http?.Stop();
            OPCBackEnd.WCFHost_Https?.Stop();
            Logger.Log("Successfully stopped Vlix OPC Server!");
        }
    }
    
}