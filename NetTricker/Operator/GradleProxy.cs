using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTricker.Operator
{
    class GradleProxy : IProxy
    {
        private string dir;
        private string fileName = "gradle.properties";
        private string backupName = "gradle.properties--backup";


        public string Dir
        {
            get
            {
                return this.dir;
            }
            set
            {
                this.dir = value;
            }
        }

        public string Type
        {
            get
            {
                return "gradle";
            }
        }

        public bool IsProxy
        {
            get
            {
                return isProxy();
            }
        }

        public GradleProxy(string dir)
        {
            this.dir = dir;
        }


        public bool Proxy()
        {
            if (!IsProxy)
            {
                string oldPath = Dir +  backupName;
                string newPath = Dir +  fileName;
                File.Move(oldPath, newPath);
                return true;
            }

            return false;
        }

        public bool UnProxy()
        {
            if (IsProxy)
            {
                string oldPath = Dir + "\\" + fileName;
                string newPath = Dir + "\\" + backupName;
                File.Move(oldPath, newPath);
                return true;
            }

            return false;
        }

        private bool isProxy()
        {
            if (!Directory.Exists(dir))
            {
                throw new Exception("Gradle path not exists, path=" + dir);
            }

            if (File.Exists(dir + fileName))
            {
                return true;
            }
            if (File.Exists(dir + backupName))
            {
                return false;
            }

            throw new Exception("Gradle setting file error, need checK!");
        }

    }
}
