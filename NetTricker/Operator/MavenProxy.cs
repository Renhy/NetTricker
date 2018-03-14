using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTricker.Operator
{
    class MavenProxy : IProxy
    {
        log4net.ILog log = log4net.LogManager.GetLogger("net_tricker.Logging");

        private string fileName = "settings.xml";
        private string proxyFileName = "settings-proxy.xml";
        private string backupSuffix = "-backup";
        private string dir;
        public string Type
        {
            get
            {
                return "maven";
            }
        }

        public bool IsProxy
        {
            get
            {
                FileInfo file = new FileInfo(dir + fileName);
                FileInfo fileBack = new FileInfo(dir + fileName + backupSuffix);
                FileInfo proxyBack = new FileInfo(dir + proxyFileName + backupSuffix);
                if (file.Length == proxyBack.Length)
                {
                    return true;
                }
                if (file.Length == fileBack.Length)
                {
                    return false;
                }
                return false;
            }
        }

        public MavenProxy(string dir)
        {
            this.dir = dir;
        }


        public bool Proxy()
        {
            try
            {
                File.Delete(dir + fileName);
                File.Copy(dir + proxyFileName + backupSuffix, dir + fileName);
                return true;
            }
            catch (Exception e)
            {
                log.Error(e);
                return false;
            }
        }

        public bool UnProxy()
        {
            try
            {
                File.Delete(dir + fileName);
                File.Copy(dir + fileName + backupSuffix, dir + fileName);
                return true;
            }
            catch (Exception e)
            {
                log.Error(e);
                return false;
            }
        }


    }
}
