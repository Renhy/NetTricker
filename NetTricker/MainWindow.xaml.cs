using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetTricker
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            test();
        }


        private void test()
        {
            string name = WifiWrappar.GetCurrentConnection();

            WIFISSID ssid = WifiWrappar.GetWIFISSID("IToIP");
            WifiWrappar.ConnectToSSID(ssid, "h3c-wlan");


        }

    }
}
