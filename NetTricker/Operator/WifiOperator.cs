using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NativeWifi;

namespace NetTricker.Operator
{
    class WifiOperator : IProxy
    {
        private string wifi;
        private string wifi_key;
        private string proxyWifi;
        private string proxyWifi_key;
        public string Type
        {
            get
            {
                return "wifi";
            }
        }

        public bool IsProxy
        {
            get
            {
                return GetCurrentConnection() == proxyWifi;
            }
        }

        public string currentSSID
        {
            get
            {
                return GetCurrentConnection();
            }
        }

        public WifiOperator(string wifi, string wifi_key, string proxy, string proxy_key)
        {
            this.wifi = wifi;
            this.wifi_key = wifi_key;
            this.proxyWifi = proxy;
            this.proxyWifi_key = proxy_key;
        }


        public bool Proxy()
        {
            if (!IsProxy)
            {
                WIFISSID ssid = GetWIFISSID(proxyWifi);
                ConnectToSSID(ssid, proxyWifi_key);
                return true;
            }
            return false;
        }

        public bool UnProxy()
        {
            if (IsProxy)
            {
                WIFISSID ssid = GetWIFISSID(wifi);
                ConnectToSSID(ssid, wifi_key);
                return true;
            }

            return false;
        }



        private static List<WIFISSID> ListSSID()
        {
            List<WIFISSID> ssids = new List<WIFISSID>();

            WlanClient client = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
                // Lists all networks with WEP security
                Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in networks)
                {
                    WIFISSID targetSSID = new WIFISSID();

                    targetSSID.wlanInterface = wlanIface;
                    targetSSID.wlanSignalQuality = (int)network.wlanSignalQuality;
                    targetSSID.SSID = GetStringForSSID(network.dot11Ssid);
                    targetSSID.dot11DefaultAuthAlgorithm = network.dot11DefaultAuthAlgorithm.ToString();
                    targetSSID.dot11DefaultCipherAlgorithm = network.dot11DefaultCipherAlgorithm.ToString();
                    ssids.Add(targetSSID);
                }
            }

            return ssids;
        }

        private static string GetCurrentConnection()
        {
            WlanClient client = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
                if (wlanIface.InterfaceState == Wlan.WlanInterfaceState.Connected &&
                        wlanIface.CurrentConnection.isState == Wlan.WlanInterfaceState.Connected)
                {
                    return wlanIface.CurrentConnection.profileName;
                }
            }

            return string.Empty;
        }

        private static WIFISSID GetWIFISSID(string name)
        {
            List<WIFISSID> ssids = ListSSID();
            foreach (WIFISSID ssid in ssids)
            {
                if (ssid.SSID == name)
                {
                    return ssid;
                }
            }
            return null;
        }

        private static void ConnectToSSID(WIFISSID ssid, string key)
        {
            try
            {
                String auth = string.Empty;
                String cipher = string.Empty;
                bool isNoKey = false;
                String keytype = string.Empty;
                switch (ssid.dot11DefaultAuthAlgorithm)
                {
                    case "IEEE80211_Open":
                        auth = "open"; break;
                    case "RSNA":
                        auth = "WPA2PSK"; break;
                    case "RSNA_PSK":
                        auth = "WPA2PSK"; break;
                    case "WPA":
                        auth = "WPAPSK"; break;
                    case "WPA_None":
                        auth = "WPAPSK"; break;
                    case "WPA_PSK":
                        auth = "WPAPSK"; break;
                }
                switch (ssid.dot11DefaultCipherAlgorithm)
                {
                    case "CCMP":
                        cipher = "AES";
                        keytype = "passPhrase";
                        break;
                    case "TKIP":
                        cipher = "TKIP";
                        keytype = "passPhrase";
                        break;
                    case "None":
                        cipher = "none"; keytype = "";
                        isNoKey = true;
                        break;
                    case "WWEP":
                        cipher = "WEP";
                        keytype = "networkKey";
                        break;
                    case "WEP40":
                        cipher = "WEP";
                        keytype = "networkKey";
                        break;
                    case "WEP104":
                        cipher = "WEP";
                        keytype = "networkKey";
                        break;
                }

                if (isNoKey && !string.IsNullOrEmpty(key))
                {
                    Console.WriteLine(">>>>>>>>>>>>>>>>>无法连接网络！");
                    return;
                }
                if (!isNoKey && string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("无法连接网络！");
                    return;
                }
                string profileName = ssid.SSID;
                string mac = StringToHex(profileName);
                string profileXml = string.Empty;
                if (!string.IsNullOrEmpty(key))
                {
                    profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><autoSwitch>false</autoSwitch><MSM><security><authEncryption><authentication>{2}</authentication><encryption>{3}</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>{4}</keyType><protected>false</protected><keyMaterial>{5}</keyMaterial></sharedKey><keyIndex>0</keyIndex></security></MSM></WLANProfile>",
                        profileName, mac, auth, cipher, keytype, key);
                }
                else
                {
                    profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><autoSwitch>false</autoSwitch><MSM><security><authEncryption><authentication>{2}</authentication><encryption>{3}</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>",
                        profileName, mac, auth, cipher, keytype);
                }

                ssid.wlanInterface.SetProfile(Wlan.WlanProfileFlags.AllUser, profileXml, true);

                bool success = ssid.wlanInterface.ConnectSynchronously(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, profileName, 15000);
                if (!success)
                {
                    Console.WriteLine("连接网络失败！");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("连接网络失败！");
                return;
            }
        }

        private static string StringToHex(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.Default.GetBytes(str); //默认是System.Text.Encoding.Default.GetBytes(str)
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(Convert.ToString(byStr[i], 16));
            }

            return (sb.ToString().ToUpper());
        }

        static string GetStringForSSID(Wlan.Dot11Ssid ssid)
        {
            return Encoding.UTF8.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }
    }

    class WIFISSID
    {
        public string SSID = "NONE";
        public string dot11DefaultAuthAlgorithm = "";
        public string dot11DefaultCipherAlgorithm = "";
        public bool networkConnectable = true;
        public string wlanNotConnectableReason = "";
        public int wlanSignalQuality = 0;
        public WlanClient.WlanInterface wlanInterface = null;
    }
}
