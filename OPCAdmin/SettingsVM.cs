using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vlix;

namespace VlixOPC
{
    public class SettingsVM
    {
        #region BoilerPlate
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, string propertyName, string P2 = null, string P3 = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            if (P2 != null) OnPropertyChanged(P2);
            if (P3 != null) OnPropertyChanged(P3);
            return true;
        }

        #endregion

        public SettingsVM() { }

        public OPCBackEndConfig ToOPCBackEndConfig()
        {
            OPCBackEndConfig oPCBackEndConfig = new OPCBackEndConfig()
            {
                Enable_WebAPI_Http = this.EnableAPI_Http,
                Enable_WebAPI_Https = this.EnableAPI_Https,
                Https_Port = this.Https_Port,
                Http_Port = this.Http_Port,
                OPCClassic_SubscriptionGroupSizeLimit = this.OPCClassic_SubscriptionGroupSizeLimit,
                OPCCUA_SubscriptionGroupSizeLimit = this.OPCUA_SubscriptionGroupSizeLimit,
                WebAPI_Password = this.Password_ForAPIBasicAuthentication,
                WebAPI_Username = this.Username_ForAPIBasicAuthentication,
                Enable_WebAPI_BasicAuthentication = this.Enable_APIBasicAuthentication,
                SubjectAlternativeNames_ForHTTPSCert = this.SubjectAlternativeNames_ForHTTPSCert,
                Tcp_Port = this.Tcp_Port,
                Enable_Tcp_Authentication = this.Enable_Tcp_Authentication,
                Tcp_Username = this.Tcp_Username,
                Tcp_Password = this.Tcp_Password
            };
            return oPCBackEndConfig;
        }
        public SettingsVM(OPCBackEndConfig oPCBackEndConfig)
        {
            this.EnableAPI_Http = oPCBackEndConfig.Enable_WebAPI_Http;
            this.EnableAPI_Https = oPCBackEndConfig.Enable_WebAPI_Https;
            this.Https_Port = oPCBackEndConfig.Https_Port;
            this.Http_Port = oPCBackEndConfig.Http_Port;
            this.OPCClassic_SubscriptionGroupSizeLimit = oPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit;
            this.OPCUA_SubscriptionGroupSizeLimit = oPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit;
            this.Username_ForAPIBasicAuthentication = oPCBackEndConfig.WebAPI_Username;
            this.Password_ForAPIBasicAuthentication = oPCBackEndConfig.WebAPI_Password;
            this.Enable_APIBasicAuthentication = oPCBackEndConfig.Enable_WebAPI_BasicAuthentication;
            this.SubjectAlternativeNames_ForHTTPSCert = oPCBackEndConfig.SubjectAlternativeNames_ForHTTPSCert;

            this.Tcp_Port = oPCBackEndConfig.Tcp_Port;
            this.Enable_Tcp_Authentication = oPCBackEndConfig.Enable_Tcp_Authentication;
            this.Tcp_Username = oPCBackEndConfig.Tcp_Username;
            this.Tcp_Password = oPCBackEndConfig.Tcp_Password;


        }

        private bool _SettingsError = false; public bool SettingsError { get { return _SettingsError; } set { SetField(ref _SettingsError, value, "SettingsError"); } }
        private int _Tcp_Port = 33192; public int Tcp_Port { get { return _Tcp_Port; } set { SetField(ref _Tcp_Port, value, "Tcp_Port"); } }
        private bool _Enable_Tcp_Authentication = false; public bool Enable_Tcp_Authentication { get { return _Enable_Tcp_Authentication; } set { SetField(ref _Enable_Tcp_Authentication, value, "Enable_Tcp_Authentication"); } }
        private string _Tcp_Username = ""; public string Tcp_Username { get { return _Tcp_Username; } set { SetField(ref _Tcp_Username, value, "Tcp_Username"); } }
        private string _Tcp_Password = ""; public string Tcp_Password { get { return _Tcp_Password; } set { SetField(ref _Tcp_Password, value, "Tcp_Password"); } }
        private bool _EnableAPI_Https = false; public bool EnableAPI_Https { get { return _EnableAPI_Https; } set { SetField(ref _EnableAPI_Https, value, "EnableAPI_Https"); } }
        private bool _EnableAPI_Http = false; public bool EnableAPI_Http { get { return _EnableAPI_Http; } set { SetField(ref _EnableAPI_Http, value, "EnableAPI_Http"); } }
        private bool _Enable_APIBasicAuthentication = false; public bool Enable_APIBasicAuthentication { get { return _Enable_APIBasicAuthentication; } set { SetField(ref _Enable_APIBasicAuthentication, value, "Enable_APIBasicAuthentication"); } }
        private int _Https_Port = 33176; public int Https_Port { get { return _Https_Port; } set { SetField(ref _Https_Port, value, "Https_Port"); } }
        private int _Http_Port = 33177; public int Http_Port { get { return _Http_Port; } set { SetField(ref _Http_Port, value, "Http_Port"); } }
        private string _SubjectAlternativeNames_ForHTTPSCert = "localhost,127.0.0.1"; public string SubjectAlternativeNames_ForHTTPSCert { get { return _SubjectAlternativeNames_ForHTTPSCert; } set { SetField(ref _SubjectAlternativeNames_ForHTTPSCert, value, "SubjectAlternativeNames_ForHTTPSCert"); } }
        private string _Username_ForAPIBasicAuthentication = ""; public string Username_ForAPIBasicAuthentication { get { return _Username_ForAPIBasicAuthentication; } set { SetField(ref _Username_ForAPIBasicAuthentication, value, "Username_ForAPIBasicAuthentication"); } }
        private string _Password_ForAPIBasicAuthentication = ""; public string Password_ForAPIBasicAuthentication { get { return _Password_ForAPIBasicAuthentication; } set { SetField(ref _Password_ForAPIBasicAuthentication, value, "Password_ForAPIBasicAuthentication"); } }
        private int _OPCClassic_SubscriptionGroupSizeLimit = 40; public int OPCClassic_SubscriptionGroupSizeLimit { get { return _OPCClassic_SubscriptionGroupSizeLimit; } set { SetField(ref _OPCClassic_SubscriptionGroupSizeLimit, value, "OPCClassic_SubscriptionGroupSizeLimit"); } }
        private int _OPCUA_SubscriptionGroupSizeLimit = 40; public int OPCUA_SubscriptionGroupSizeLimit { get { return _OPCUA_SubscriptionGroupSizeLimit; } set { SetField(ref _OPCUA_SubscriptionGroupSizeLimit, value, "OPCUA_SubscriptionGroupSizeLimit"); } }

    }
}
