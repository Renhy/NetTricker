using NetTricker.Operator;
using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        #region fields
        private log4net.ILog log;

        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        private int hideDelay;
        private bool isMouseIn;
        private long mouseActionTick;
        private double cacheHeight;

        private List<IProxy> proxies;
        private WifiOperator wifiProxy;

        #endregion


        public MainWindow()
        {
            InitializeComponent();

            ApplicationInit();
        }

        private void ApplicationInit()
        {
            #region log configuration
            System.IO.FileInfo file = new System.IO.FileInfo("log.config");
            log4net.Config.XmlConfigurator.Configure(file);
            log = log4net.LogManager.GetLogger("net_tricker.Logging");
            log.Info("log configuration finished.");
            #endregion

            #region notify icon init
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = "agent";
            notifyIcon.Icon = new System.Drawing.Icon(System.Windows.Forms.Application.StartupPath + "\\Resources\\tricker.ico");
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);

            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("exit");
            exit.Click += new EventHandler(exit_Click);

            System.Windows.Forms.MenuItem[] items = new System.Windows.Forms.MenuItem[] {exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(items);
            #endregion

            mouseActionTick = DateTime.Now.Ticks;
            hideDelay = Properties.Settings.Default.hide_delay;

            log.Info("Application initial finished.");
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            loadProperties();
            updateWifiInfo();

            cacheHeight = this.Height;
        }

        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.Visibility != Visibility.Visible)
                {
                    this.Visibility = Visibility.Visible;
                    this.Activate();
                }
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要关闭吗?",
                                               "退出",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question,
                                                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                notifyIcon.Dispose();
                Application.Current.Shutdown();
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            log.Debug("in windows closing");
           
        }

        private void loadProperties()
        {
            if (proxies == null)
            {
                proxies = new List<IProxy>();
            }
            else
            {
                proxies.Clear();
            }

            wifiProxy = new WifiOperator(
                Properties.Settings.Default.wifi, 
                Properties.Settings.Default.wifi_key,
                Properties.Settings.Default.wifi_proxy,
                Properties.Settings.Default.wifi_proxy_key);
            proxies.Add(wifiProxy);

            if (Properties.Settings.Default.gradle_enable)
            {
                IProxy gradle = new GradleProxy(
                    Properties.Settings.Default.gradle_home);
                proxies.Add(gradle);
            }
            if (Properties.Settings.Default.maven_enable)
            {
                IProxy maven = new MavenProxy(
                    Properties.Settings.Default.maven_home);
                proxies.Add(maven);
            }
            if (Properties.Settings.Default.lan_enable)
            {
                IProxy lan = new LanProxy(
                    Properties.Settings.Default.lan_config);
                proxies.Add(lan);
            }
        }

        #region ui event
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            isMouseIn = true;
            mouseActionTick = DateTime.Now.Ticks;
            this.Height = cacheHeight;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            isMouseIn = false;
            mouseActionTick = DateTime.Now.Ticks;
            new Thread(() => {
                Thread.Sleep(hideDelay);
                if (!isMouseIn && DateTime.Now.Ticks - mouseActionTick > hideDelay)
                {
                    int count = 30;
                    double delta = (cacheHeight - 15) / count; 
                    for (int i = 0; i < count; i++)
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            if (this.Height - delta > 10)
                            {
                                this.Height -= delta;
                            }
                        }));
                        Thread.Sleep(10);
                    }
                    this.Dispatcher.Invoke(new Action(() => {
                        this.Height = 10;
                    }));
                }
            }).Start();
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            foreach (System.Windows.Forms.Screen screen in 
                System.Windows.Forms.Screen.AllScreens)
            {
                if (this.Left >= screen.Bounds.Left && this.Left < screen.Bounds.Right)
                {
                    if (Math.Abs(this.Top - screen.Bounds.Top) < 20)
                    {
                        this.Top = screen.Bounds.Top;
                    }
                }
            }
        }

        private void proxyButton_Click(object sender, RoutedEventArgs e)
        {
            proxyButton.IsEnabled = false;
            this.Cursor = Cursors.Wait;
            infoLabel.Content = "start connecting, setting proxy";

            new Thread(() =>
            {
                foreach (IProxy proxy in proxies)
                {
                    try
                    {
                        log.InfoFormat("type = {0}, proxy.IsProxy = {1}", proxy.Type, proxy.IsProxy);
                        proxy.Proxy();
                        log.InfoFormat("over, proxy.IsProxy = {0}", proxy.IsProxy);
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex);
                    }
                }

                Thread.Sleep(5000);
                this.Dispatcher.Invoke(
                    new Action(() => {
                        updateWifiInfo();
                        this.Cursor = Cursors.Arrow;
                    }));
            }).Start();
        }

        private void unProxyButton_Click(object sender, RoutedEventArgs e)
        {
            unProxyButton.IsEnabled = false;
            this.Cursor = Cursors.Wait;
            infoLabel.Content = "start connecting, setting unProxy";

            new Thread(() =>
            {
                foreach (IProxy proxy in proxies)
                {
                    try
                    {
                        log.InfoFormat("type = {0}, proxy.IsProxy = {1}", proxy.Type, proxy.IsProxy);
                        proxy.UnProxy();
                        log.InfoFormat("over, proxy.IsProxy = {0}", proxy.IsProxy);
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex);
                    }
                }

                Thread.Sleep(5000);
                this.Dispatcher.Invoke(
                    new Action(() => {
                        updateWifiInfo();
                        this.Cursor = Cursors.Arrow;
                    }));
            }).Start();
        }

        #endregion


        private void updateWifiInfo()
        {
            proxyButton.IsEnabled = !wifiProxy.IsProxy;
            unProxyButton.IsEnabled = wifiProxy.IsProxy;

            if (wifiProxy.IsProxy)
            {
                infoLabel.Content = wifiProxy.currentSSID + " connected，with proxy";
            }
            else
            {
                infoLabel.Content = wifiProxy.currentSSID + " connected，without proxy";
            }

        }

    }
}
