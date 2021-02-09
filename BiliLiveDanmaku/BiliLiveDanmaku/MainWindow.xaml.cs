using BiliLive;
using BiliLiveDanmaku.Common;
using BiliLiveDanmaku.Modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BiliLiveDanmaku
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool IsConnected;

        private List<IModule> Modules;

        [Serializable]
        private class Config
        {
            public Rect WindowRect { get; set; }
            public string RoomId { get; set; }
            public List<IModuleConfig> ModuleConfigs { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            IsConnected = false;

            InitCounters();

            Config config = LoadConfig();

            if (config != null)
            {
                RoomIdBox.Text = config.RoomId;
                if (config.WindowRect.Width != 0 && config.WindowRect.Height != 0)
                {
                    this.Left = config.WindowRect.Left;
                    this.Top = config.WindowRect.Top;
                    this.Width = config.WindowRect.Width;
                    this.Height = config.WindowRect.Height;
                }
            }

            List<IModuleConfig> moduleConfigs = null;
            if (config != null && config.ModuleConfigs != null)
            {
                moduleConfigs = config.ModuleConfigs;
            }
            else
            {
                moduleConfigs = new List<IModuleConfig>();
                moduleConfigs.Add(new DanmakuShowConfig());
                moduleConfigs.Add(new DanmakuSpeechConfig());
            }
            Modules = new List<IModule>();
            foreach (IModuleConfig moduleConfig in moduleConfigs)
            {
                AddModule(moduleConfig);
            }

            ContextMenu appendModuleContextMenu = (ContextMenu)AddModuleBtn.Resources["AppendModuleContextMenu"];
            ModuleLoader moduleLoader = new ModuleLoader();
            List<IModule> modules = moduleLoader.LoadModules();
            foreach(IModule module in modules)
            {
                IModuleConfig defaultModuleConfig = module.CreateDefaultConfig();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = module.Description;
                menuItem.Tag = defaultModuleConfig;
                menuItem.Click += AppendModuleMenuItem_Click;
                appendModuleContextMenu.Items.Add(menuItem);
            }


            this.Closing += MainWindow_Closing;
        }

        private void AppendModuleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            IModuleConfig defaultModuleConfig = (IModuleConfig)item.Tag;
            AddModule(defaultModuleConfig);
        }

        private Config LoadConfig()
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileInfo fileInfo = new FileInfo("./config/settings.dat");
            if (!fileInfo.Exists)
                return null;
            try
            {
                using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Config config = (Config)binaryFormatter.Deserialize(fileStream);
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        private void SaveConfig()
        {
            List<IModuleConfig> moduleConfigs = new List<IModuleConfig>();
            foreach (IModule module in Modules)
            {
                IModuleConfig moduleConfig = module.GetConfig();
                moduleConfigs.Add(moduleConfig);
            }

            Config config = new Config()
            {
                WindowRect = new Rect(this.Left, this.Top, this.Width, this.Height),
                RoomId = RoomIdBox.Text,
                ModuleConfigs = moduleConfigs
            };
            FileInfo fileInfo = new FileInfo("./config/settings.dat");
            try
            {
                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }
                using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(fileStream, config);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void InitCounters()
        {
            FaceLoader.CachedCountChanged += (int count) =>
            {
                Dispatcher.Invoke(() =>
                {
                    CachedCountBox.Text = count.ToString();
                });
            };

            FaceLoader.QueueCountChanged += (int count) =>
            {
                Dispatcher.Invoke(() =>
                {
                    CachedQueueCountBox.Text = count.ToString();
                });
            };
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveConfig();
            if (IsConnected)
            {
                LiveListener.Disconnect();

                // Wait for all Dispatcher tasks finished.
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => { }));
            }
            foreach (IModule module in Modules)
            {
                module.Close();
            }
        }

        private void AddModule(IModuleConfig moduleConfig)
        {
            IModule module = moduleConfig.CreateModule();
            ModuleBorder moduleBorder = new ModuleBorder(module);
            moduleBorder.Closing += ModuleBorder_Closing;
            ModulesPanel.Children.Add(moduleBorder);
            Modules.Add(module);
        }

        private void ModuleBorder_Closing(object sender, IModule e)
        {
            Modules.Remove(e);
            ModulesPanel.Children.Remove((ModuleBorder)sender);
        }

        private BiliLiveListener LiveListener { get; set; }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                uint roomId = uint.Parse(RoomIdBox.Text);
                LiveListener = new BiliLiveListener(roomId, BiliLiveListener.Protocols.Tcp);
                LiveListener.ItemsRecieved += BiliLiveListener_ItemsRecieved;
                LiveListener.PopularityRecieved += LiveListener_PopularityRecieved;
                LiveListener.Connected += BiliLiveListener_Connected;
                LiveListener.Disconnected += BiliLiveListener_Disconnected;
                LiveListener.ConnectionFailed += LiveListener_ConnectionFailed;
                LiveListener.Connect();
                ConnectBtn.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void LiveListener_ConnectionFailed(string message)
        {
            Console.WriteLine("Reconnecting");
            Task.Factory.StartNew(() =>
            {
                Task.Delay(1000);
                if (IsConnected)
                {
                    Dispatcher.Invoke(() =>
                    {
                        LiveListener.Connect();
                    });
                }
            });
        }

        private void LiveListener_PopularityRecieved(uint popularity)
        {
            Dispatcher.Invoke(() =>
            {
                PopularityBox.Text = string.Format("{0}", popularity.ToString());
            });
        }

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            DisconnectBtn.IsEnabled = false;
            LiveListener.Disconnect();
        }

        private void BiliLiveListener_Connected()
        {
            DisconnectBtn.IsEnabled = true;
            IsConnected = true;
        }

        private void BiliLiveListener_Disconnected()
        {
            ConnectBtn.IsEnabled = true;
            IsConnected = false;
            Dispatcher.Invoke(() =>
            {
                PopularityBox.Text = string.Empty;
            });
        }

        private void BiliLiveListener_ItemsRecieved(BiliLiveJsonParser.IItem[] items)
        {
            Task.Factory.StartNew(() =>
            {
                DateTime? lastTime = null;
                foreach (BiliLiveJsonParser.IItem item in items)
                {
                    switch (item.Cmd)
                    {
                        case BiliLiveJsonParser.Cmds.DANMU_MSG:
                        case BiliLiveJsonParser.Cmds.SUPER_CHAT_MESSAGE:
                        case BiliLiveJsonParser.Cmds.SEND_GIFT:
                        case BiliLiveJsonParser.Cmds.GUARD_BUY:
                            BiliLiveJsonParser.ITimeStampedItem timeStampedItem = (BiliLiveJsonParser.ITimeStampedItem)item;
                            if (lastTime == null)
                            {
                                lastTime = timeStampedItem.TimeStamp;
                            }
                            else
                            {
                                TimeSpan timeSpan = timeStampedItem.TimeStamp - (DateTime)lastTime;
                                int sleepTime = (int)timeSpan.TotalMilliseconds;
                                if (sleepTime < 0)
                                    sleepTime = 0;
                                Thread.Sleep(sleepTime);
                                lastTime = timeStampedItem.TimeStamp;
                            }
                            break;
                    }

                    foreach (IModule module in Modules)
                    {
                        module.ProcessItem(item);
                    }


                }
            });

        }

        private void AddModuleBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = (ContextMenu)AddModuleBtn.Resources["AppendModuleContextMenu"];
            contextMenu.PlacementTarget = sender as Button;
            contextMenu.IsOpen = true;
        }

        private void AddDisplayBtn_Click(object sender, RoutedEventArgs e)
        {
            AddModule(new DanmakuShowConfig());
        }

        private void AddSpeechBtn_Click(object sender, RoutedEventArgs e)
        {
            AddModule(new DanmakuSpeechConfig());
        }
    }
}
