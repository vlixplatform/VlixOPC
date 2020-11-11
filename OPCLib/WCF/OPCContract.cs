using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Vlix;

namespace VlixOPC
{

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class OPCServiceContract : OPCBrowserContract, iOPCServiceContract
    {
        //This is for IKeepAlive

        public bool TryGetLogs(DateTime GetLogsNewerThanAndExcludingThisDateTime_InUtc, out List<LogStruct> LogList, string MACAddress = "")
        {
            return Logger.TryGetLogs(GetLogsNewerThanAndExcludingThisDateTime_InUtc, out LogList);
        }

        public bool TryGetLatestLogs(int NumberOfLogs, out List<LogStruct> LogList, string MACAddress = "")
        {
            return Logger.TryGetLatestLogs(NumberOfLogs, out LogList);
        }

        public Task<ResTryGetSettings> TryGetSettings()
        {
            OPCBackEndConfig OPCBackEndConfig = null;
            try
            {
                OPCBackEndConfig = (OPCBackEndConfig)Global.Product.Config;
                return Task.FromResult(new ResTryGetSettings(OPCBackEndConfig));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResTryGetSettings(ex.ToString()));
            }
        }

        public Task<Res> TrySaveSettings(OPCBackEndConfig OPCBackEndConfig)
        {
            try
            {
                Logger.Log("Saving and applying OPC Service Back End Configuration...");
                bool ChangeOccured = false;
                OPCBackEndConfig CurrentOPCBackEndConfig = (OPCBackEndConfig)Global.Product.Config;

                //HTTP Settings
                if (CurrentOPCBackEndConfig.Enable_WebAPI_Http != OPCBackEndConfig.Enable_WebAPI_Http
                    || CurrentOPCBackEndConfig.Http_Port != OPCBackEndConfig.Http_Port)
                {
                    ChangeOccured = true;
                    if (OPCBackEndConfig.Enable_WebAPI_Http) { OPCBackEnd.WCFHost_Http?.Stop(); OPCBackEnd.EnableHttp(); }
                    else OPCBackEnd.WCFHost_Http?.Stop();
                }

                //HTTPS Settings
                if (CurrentOPCBackEndConfig.Enable_WebAPI_Https != OPCBackEndConfig.Enable_WebAPI_Https
                    || CurrentOPCBackEndConfig.Https_Port != OPCBackEndConfig.Https_Port
                    || CurrentOPCBackEndConfig.Enable_WebAPI_BasicAuthentication != OPCBackEndConfig.Enable_WebAPI_BasicAuthentication
                    || CurrentOPCBackEndConfig.WebAPI_Username != OPCBackEndConfig.WebAPI_Username
                    || CurrentOPCBackEndConfig.WebAPI_Password != OPCBackEndConfig.WebAPI_Password
                    || CurrentOPCBackEndConfig.SubjectAlternativeNames_ForHTTPSCert != OPCBackEndConfig.SubjectAlternativeNames_ForHTTPSCert)
                {
                    ChangeOccured = true;
                    if (OPCBackEndConfig.Enable_WebAPI_Https) { OPCBackEnd.WCFHost_Https?.Stop(); OPCBackEnd.EnableHttps(); }
                    else OPCBackEnd.WCFHost_Https?.Stop();
                }

                //Tcp Settings
                if (CurrentOPCBackEndConfig.Enable_Tcp_Authentication != OPCBackEndConfig.Enable_Tcp_Authentication
                    || CurrentOPCBackEndConfig.Tcp_Port != OPCBackEndConfig.Tcp_Port
                    || CurrentOPCBackEndConfig.Tcp_Username != OPCBackEndConfig.Tcp_Username
                    || CurrentOPCBackEndConfig.Tcp_Password != OPCBackEndConfig.Tcp_Password)
                {
                    ChangeOccured = true;
                    OPCBackEnd.WCFHost_Tcp?.Stop(); 
                    OPCBackEnd.WCFHost_Tcp.ListeningPort = OPCBackEndConfig.Tcp_Port; 
                    OPCBackEnd.WCFHost_Tcp.EnableUserAuthentication = OPCBackEndConfig.Enable_Tcp_Authentication && !(OPCBackEndConfig.Tcp_Username.IsNullOrEmpty() && OPCBackEndConfig.Tcp_Password.IsNullOrEmpty()); 
                    
                    OPCBackEnd.WCFHost_Tcp.Start();
                }

                //OPC Settings
                if (CurrentOPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit != OPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit)                  
                {
                    ChangeOccured = true;
                    OPCBackEnd.OPCClassicBrowserEngine.OPCGroupSizeLimit = OPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit.ToInt(40);
                }
                if (CurrentOPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit != OPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit)
                {
                    ChangeOccured = true;
                    OPCBackEnd.OPCUABrowserEngine.OPCGroupSizeLimit = OPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit.ToInt(40);
                }
                if (ChangeOccured)
                {
                    OPCBackEnd.Config = OPCBackEndConfig;
                    Global.Product.Config = OPCBackEndConfig;
                    Global.Product.Config.TrySaveConfigFile(out string ExStrC);
                    Logger.Log("Successfully saved OPC Service Back End Configuration at '" + Global.Product.ConfigFilePath + "'");
                }
                else
                {
                    Logger.Log("Save did not execute as there were no changes made!");
                }
                return Task.FromResult(new Res());
            }
            catch (Exception ex)
            {
                return Task.FromResult(new Res(ex.ToString())); ;
            }
            
        }

        Task iKeepAlive.TrySendKeepAlivePulse()
        {
            return Task.CompletedTask;
        }
    }

    
}
