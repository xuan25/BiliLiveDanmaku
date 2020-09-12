using BiliLiveDanmaku.UI;
using BiliLiveHelper.Bili;
using JsonUtil;
using Speech;
using Speech.Lexicon;
using Speech.Template;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace BiliLiveDanmaku
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        [Serializable]
        public enum FilterOptions
        {
            [Description("弹幕显示")]
            Danmaku,
            [Description("醒目留言显示")]
            SuperChat,
            [Description("金瓜子礼物显示")]
            GoldenGift,
            [Description("银瓜子礼物显示")]
            SilverGift,
            [Description("礼物连击显示")]
            ComboSend,
            [Description("节奏风暴显示")]
            RythmStorm,
            [Description("上舰显示")]
            GuardBuy,
            [Description("舰长欢迎显示")]
            WelcomeGuard,
            [Description("老爷欢迎显示")]
            Welcome,
            [Description("进入直播间显示")]
            InteractEnter,
            [Description("关注直播间显示")]
            InteractFollow,
            [Description("分享直播间显示")]
            InteractShare,
            [Description("直播间禁言显示")]
            RoomBlock,
            [Description("弹幕播报")]
            DanmakuSpeech,
            [Description("醒目留言播报")]
            SuperChatSpeech,
            [Description("金瓜子礼物播报")]
            GoldenGiftSpeech,
            [Description("银瓜子礼物播报")]
            SilverGiftSpeech,
            [Description("礼物连击播报")]
            ComboSendSpeech,
        }

        private Dictionary<FilterOptions, bool> FilterValueDict;

        private bool IsConnected;

        [Serializable]
        private class Config
        {
            public Rect WindowRect { get; set; }
            public string RoomId { get; set; }
            public string OutputDevice { get; set; }
            public double Volume { get; set; }
            public Dictionary<FilterOptions, bool> FilterValueDict { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            IsConnected = false;

            Config config = LoadConfig();

            if(config != null)
            {
                RoomIdBox.Text = config.RoomId;
                if (this.Width != 0 && this.Height != 0)
                {
                    this.Left = config.WindowRect.Left;
                    this.Top = config.WindowRect.Top;
                    this.Width = config.WindowRect.Width;
                    this.Height = config.WindowRect.Height;
                }
            }

            InitFilterOptions(config);

            InitSpeech(config);

            InitCounters();

            this.Closing += MainWindow_Closing;
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
            Config config = new Config()
            {
                WindowRect = new Rect(this.Left, this.Top, this.Width, this.Height),
                RoomId = RoomIdBox.Text,
                OutputDevice = ((ComboBoxItem)OutputDeviceCombo.SelectedItem).Content.ToString(),
                Volume = VolumeSlider.Value,
                FilterValueDict = FilterValueDict
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

        private void InitFilterOptions(Config config)
        {
            FilterValueDict = new Dictionary<FilterOptions, bool>();
            foreach (FilterOptions filterOption in Enum.GetValues(typeof(FilterOptions)))
            {
                bool initValue = true;
                if (config != null && config.FilterValueDict != null)
                {
                    if (config.FilterValueDict.ContainsKey(filterOption))
                    {
                        initValue = config.FilterValueDict[filterOption];
                    }
                }

                DescriptionAttribute[] attributes = (DescriptionAttribute[])filterOption
                   .GetType()
                   .GetField(filterOption.ToString())
                   .GetCustomAttributes(typeof(DescriptionAttribute), false);
                string description = attributes.Length > 0 ? attributes[0].Description : string.Empty;

                CheckBox checkBox = new CheckBox
                {
                    Content = description,
                    IsChecked = initValue,
                    Foreground = Brushes.White,
                    Margin = new Thickness(4),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = filterOption
                };
                checkBox.Checked += ShowOptionCkb_Checked;
                checkBox.Unchecked += ShowOptionCkb_Unchecked;
                OptionPanel.Children.Add(checkBox);

                FilterValueDict.Add(filterOption, initValue);
            }
        }

        private void InitSpeech(Config config)
        {
            FileInfo fileInfo = new FileInfo("./config/speech.xml");
            Stream speechConfigStream = null;
            try
            {
                if (fileInfo.Exists)
                {
                    speechConfigStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    speechConfigStream = Application.GetResourceStream(new Uri("Config/Speech.xml", UriKind.RelativeOrAbsolute)).Stream;
                }
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(speechConfigStream);
                XmlNode enableAttr = xmlDocument.SelectSingleNode("speech/@enable");
                string enable = enableAttr != null ? enableAttr.Value : "true";
                if (enable.ToLower() != "false")
                {
                    XmlNode tokenEndpointNode = xmlDocument.SelectSingleNode("speech/token_endpoint/text()");
                    XmlNode ttsEndpointNode = xmlDocument.SelectSingleNode("speech/tts_endpoint/text()");
                    XmlNode apiKeyNode = xmlDocument.SelectSingleNode("speech/api_key/text()");
                    string tokenEndpoint = tokenEndpointNode.Value;
                    string ttsEndpoint = ttsEndpointNode.Value;
                    string apiKey = apiKeyNode.Value;
                    SpeechUtil.Init(tokenEndpoint, ttsEndpoint, apiKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (speechConfigStream != null)
                    speechConfigStream.Close();
            }

            OutputDeviceCombo.Items.Add(new ComboBoxItem() { Content = "默认输出设备", Tag = -1 });
            OutputDeviceCombo.SelectedIndex = 0;
            int deviceCount = Wave.WaveOut.DeviceCount;
            for (int i = 0; i < deviceCount; i++)
            {
                Wave.MmeInterop.WaveOutCapabilities waveOutCapabilities = Wave.WaveOut.GetCapabilities(i);
                ComboBoxItem comboBoxItem = new ComboBoxItem() { Content = waveOutCapabilities.ProductName, Tag = i };
                OutputDeviceCombo.Items.Add(comboBoxItem);
                if(config != null)
                {
                    if (waveOutCapabilities.ProductName == config.OutputDevice)
                    {
                        OutputDeviceCombo.SelectedItem = comboBoxItem;
                    }
                }
            }

            if (config != null)
            {
                VolumeSlider.Value = config.Volume;
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

            SpeechUtil.QueueChanged += Synthesizer_QueueChanged;
        }

        private void Synthesizer_QueueChanged(object sender, int e)
        {
            Dispatcher.Invoke(() =>
            {
                SynthesizeQueueCountBox.Text = e.ToString();
            });
        }

        private void ClearSpeechQueueBtn_Click(object sender, RoutedEventArgs e)
        {
            SpeechUtil.ClearQueue();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsConnected)
            {
                LiveListener.Disconnect();

                // Wait for all Dispatcher tasks finished.
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => { }));
            }
            SaveConfig();
        }

        private void ShowOptionCkb_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            FilterOptions filterOptions = (FilterOptions)checkBox.Tag;
            FilterValueDict[filterOptions] = true;
        }

        private void ShowOptionCkb_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            FilterOptions filterOptions = (FilterOptions)checkBox.Tag;
            FilterValueDict[filterOptions] = false;
        }

        #region Listener

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
                    switch (item.Cmd)
                    {
                        case BiliLiveJsonParser.Cmds.DANMU_MSG:
                            BiliLiveJsonParser.Danmaku danmaku = (BiliLiveJsonParser.Danmaku)item;
                            if (danmaku.Type == 0)
                            {
                                AppendDanmaku(danmaku);
                                SpeakDanmaku(danmaku);
                            }
                            else
                                AppendRythmStorm(danmaku);
                            break;
                        case BiliLiveJsonParser.Cmds.SUPER_CHAT_MESSAGE:
                            BiliLiveJsonParser.SuperChat superChat = (BiliLiveJsonParser.SuperChat)item;
                            AppendSuperChat(superChat);
                            SpeakSuperChat(superChat);
                            break;
                        case BiliLiveJsonParser.Cmds.SEND_GIFT:
                            BiliLiveJsonParser.Gift gift = (BiliLiveJsonParser.Gift)item;
                            AppendGift(gift);
                            SpeakGift(gift);
                            break;
                        case BiliLiveJsonParser.Cmds.COMBO_SEND:
                            BiliLiveJsonParser.ComboSend comboSend = (BiliLiveJsonParser.ComboSend)item;
                            AppendComboSend(comboSend);
                            SpeakComboSend(comboSend);
                            break;
                        case BiliLiveJsonParser.Cmds.WELCOME:
                            BiliLiveJsonParser.Welcome welcome = (BiliLiveJsonParser.Welcome)item;
                            AppendWelcome(welcome);
                            break;
                        case BiliLiveJsonParser.Cmds.WELCOME_GUARD:
                            BiliLiveJsonParser.WelcomeGuard welcomeGuard = (BiliLiveJsonParser.WelcomeGuard)item;
                            AppendWelcomeGuard(welcomeGuard);
                            break;
                        case BiliLiveJsonParser.Cmds.GUARD_BUY:
                            BiliLiveJsonParser.GuardBuy guardBuy = (BiliLiveJsonParser.GuardBuy)item;
                            AppendGuardBuy(guardBuy);
                            break;
                        case BiliLiveJsonParser.Cmds.INTERACT_WORD:
                            BiliLiveJsonParser.InteractWord interactWord = (BiliLiveJsonParser.InteractWord)item;
                            AppendInteractWord(interactWord);
                            break;
                        case BiliLiveJsonParser.Cmds.ROOM_BLOCK_MSG:
                            BiliLiveJsonParser.RoomBlock roomBlock = (BiliLiveJsonParser.RoomBlock)item;
                            AppendRoomBlock(roomBlock);
                            break;
                    }
                }
            });
            
        }

        #endregion

        #region Danmaku

        private DateTime CleanDanmakuTime { get; set; }
        private Task CleanDanmakuTask { get; set; }

        public void CleanDanmaku()
        {
            while (DateTime.UtcNow < CleanDanmakuTime)
                Thread.Sleep(200);

            uint count = 0;
            Dispatcher.Invoke(() =>
            {
                double offset = 0;
                while (DanmakuPanel.Children.Count > 5000)
                {
                    FrameworkElement frameworkElement = (FrameworkElement)DanmakuPanel.Children[0];
                    offset += frameworkElement.ActualHeight;
                    DanmakuPanel.Children.RemoveAt(0);
                    count++;
                }
                double verticalOffset = DanmakuScrollViewer.VerticalOffset - offset;
                if (verticalOffset < 0)
                    verticalOffset = 0;
                DanmakuFluidMove.Duration = new Duration(TimeSpan.FromSeconds(0));
                DanmakuScrollViewer.ScrollToVerticalOffset(verticalOffset);

                DanmakuScrollViewer.InvalidateArrange();
                DanmakuScrollViewer.UpdateLayout();

                DanmakuFluidMove.Duration = new Duration(TimeSpan.FromSeconds(0.2));
            });

            Console.WriteLine("Danmaku cleaned : {0}", count);
        }

        private void AppendDanmaku(BiliLiveJsonParser.Danmaku item)
        {
            if (!FilterValueDict[FilterOptions.Danmaku])
                return;

            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new Danmaku(item));
                if(!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if(CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void SpeakDanmaku(BiliLiveJsonParser.Danmaku item)
        {
            if (!FilterValueDict[FilterOptions.DanmakuSpeech])
                return;

            if (SpeechUtil.IsAvalable)
            {
                string template = TemplateManager.DanmakuSpeechTemplate;
                string ssmlDoc = template.Replace("{User}", SecurityElement.Escape(item.Sender.Name)).Replace("{Message}", LexiconUtil.MakeText(SecurityElement.Escape(item.Message)));
                SpeechUtil.Speak(ssmlDoc);
            }
        }

        private void AppendSuperChat(BiliLiveJsonParser.SuperChat item)
        {
            if (!FilterValueDict[FilterOptions.SuperChat])
                return;

            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new SuperChat(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void SpeakSuperChat(BiliLiveJsonParser.SuperChat item)
        {
            if (!FilterValueDict[FilterOptions.SuperChatSpeech])
                return;

            if (SpeechUtil.IsAvalable)
            {
                string template = TemplateManager.SuperChatTemplate;
                string ssmlDoc = template.Replace("{User}", SecurityElement.Escape(item.User.Name)).Replace("{Message}", LexiconUtil.MakeText(SecurityElement.Escape(item.Message)));
                SpeechUtil.Speak(ssmlDoc);
            }
        }

        private void AppendGift(BiliLiveJsonParser.Gift item)
        {
            if (item.CoinType == "gold" && !FilterValueDict[FilterOptions.GoldenGift])
                return;
            if (item.CoinType == "silver" && !FilterValueDict[FilterOptions.SilverGift])
                return;

            Dispatcher.Invoke(() =>
            {
                if (Gift.AppendGiftToExist(item))
                    return;
                DanmakuPanel.Children.Add(new Gift(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void SpeakGift(BiliLiveJsonParser.Gift item)
        {
            if (item.CoinType == "gold" && !FilterValueDict[FilterOptions.GoldenGiftSpeech])
                return;
            if (item.CoinType == "silver" && !FilterValueDict[FilterOptions.SilverGiftSpeech])
                return;

            if (SpeechUtil.IsAvalable)
            {
                string template = TemplateManager.GiftSpeechTemplate;
                string ssmlDoc = template.Replace("{User}", SecurityElement.Escape(item.Sender.Name)).Replace("{Count}", SecurityElement.Escape(item.Number.ToString())).Replace("{Gift}", SecurityElement.Escape(item.GiftName));
                SpeechUtil.Speak(ssmlDoc);
            }
        }

        private void AppendComboSend(BiliLiveJsonParser.ComboSend item)
        {
            if (!FilterValueDict[FilterOptions.ComboSend])
                return;
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new ComboSend(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void SpeakComboSend(BiliLiveJsonParser.ComboSend item)
        {
            if (!FilterValueDict[FilterOptions.ComboSendSpeech])
                return;

            if (SpeechUtil.IsAvalable)
            {
                string template = TemplateManager.GiftSpeechTemplate;
                string ssmlDoc = template.Replace("{User}", SecurityElement.Escape(item.Sender.Name)).Replace("{Count}", SecurityElement.Escape(item.Number.ToString())).Replace("{Gift}", SecurityElement.Escape(item.GiftName));
                SpeechUtil.Speak(ssmlDoc);
            }
        }

        private void AppendWelcome(BiliLiveJsonParser.Welcome item)
        {
            if (!FilterValueDict[FilterOptions.Welcome])
                return;

            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new Welcome(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void AppendWelcomeGuard(BiliLiveJsonParser.WelcomeGuard item)
        {
            if (!FilterValueDict[FilterOptions.WelcomeGuard])
                return;

            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new Welcome(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void AppendGuardBuy(BiliLiveJsonParser.GuardBuy item)
        {
            if (!FilterValueDict[FilterOptions.GuardBuy])
                return;

            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new GuardBuy(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void AppendInteractWord(BiliLiveJsonParser.InteractWord item)
        {
            if (item.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Enter && !FilterValueDict[FilterOptions.InteractEnter])
                return;
            if (item.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Follow && !FilterValueDict[FilterOptions.InteractFollow])
                return;
            if (item.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Share && !FilterValueDict[FilterOptions.InteractShare])
                return;

            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new InteractWord(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        private void AppendRoomBlock(BiliLiveJsonParser.RoomBlock item)
        {
            if (!FilterValueDict[FilterOptions.RoomBlock])
                return;

            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new RoomBlock(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanDanmaku);
            }
        }

        #endregion

        #region RythmStorm

        private DateTime CleanRythmStormTime { get; set; }
        private Task CleanRythmStormTask { get; set; }

        private DateTime HideRythmStormTime { get; set; }
        private Task ShowRythmStormTask { get; set; }

        private void AppendRythmStorm(BiliLiveJsonParser.Danmaku item)
        {
            if (!FilterValueDict[FilterOptions.RythmStorm])
                return;

            Dispatcher.Invoke(() =>
            {
                RythmStormPanel.Children.Add(new Danmaku(item));
                if (!RythmStormScrollViewer.IsMouseOver)
                    RythmStormScrollViewer.ScrollToBottom();
                CleanRythmStormTime = DateTime.UtcNow.AddSeconds(0.2);
                HideRythmStormTime = DateTime.UtcNow.AddSeconds(5);
            });

            if (CleanRythmStormTask == null || CleanRythmStormTask.IsCompleted)
            {
                CleanRythmStormTask = Task.Factory.StartNew(CleanRythmStorm);
            }
            if (ShowRythmStormTask == null || ShowRythmStormTask.IsCompleted)
            {
                ShowRythmStormTask = Task.Factory.StartNew(ShowRythmStorm);
            }
        }

        public void CleanRythmStorm()
        {
            while (DateTime.UtcNow < CleanRythmStormTime)
                Thread.Sleep(200);
            Dispatcher.Invoke(() =>
            {
                double offset = 0;
                while (RythmStormPanel.Children.Count > 2)
                {
                    FrameworkElement frameworkElement = (FrameworkElement)RythmStormPanel.Children[0];
                    offset += frameworkElement.ActualHeight;
                    RythmStormPanel.Children.RemoveAt(0);
                }
                double verticalOffset = RythmStormScrollViewer.VerticalOffset - offset;
                if (verticalOffset < 0)
                    verticalOffset = 0;
                RythmStormFluidMove.Duration = new Duration(TimeSpan.FromSeconds(0));
                RythmStormScrollViewer.ScrollToVerticalOffset(verticalOffset);
            });
            Thread.Sleep(1);
            Dispatcher.Invoke(() =>
            {
                RythmStormFluidMove.Duration = new Duration(TimeSpan.FromSeconds(0.2));
            });
            
            Console.WriteLine("Rythm storm cleaned");
        }

        public void ShowRythmStorm()
        {
            Dispatcher.Invoke(() =>
            {
                ((Storyboard)RythmStormBorder.Resources["ShowRythmStorm"]).Begin();
            });
            while (DateTime.UtcNow < HideRythmStormTime)
                Thread.Sleep(200);
            Dispatcher.Invoke(() =>
            {
                ((Storyboard)RythmStormBorder.Resources["HideRythmStorm"]).Begin();
            });
        }

        #endregion

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    string jsonStr = "{\"cmd\":\"DANMU_MSG\",\"info\":[[0,1,25,16777215,1585742017782,1585738896,0,\"3d6e8048\",0,0,0],\"" + Guid.NewGuid().ToString() + "\",[" + random.Next(10000) + ",\"Akisou\",0,0,0,10000,1,\"\"],[9,\"祭丝\",\"夏色祭Official\",13946381,10512625,\"union\",0],[12,0,6406234,\">50000\"],[\"\",\"\"],0,0,null,{\"ts\":1585742017,\"ct\":\"40AF176\"},0,0,null,null,0]}";
                    Json.Value json = Json.Parser.Parse(jsonStr);
                    BiliLiveJsonParser.Danmaku danmaku = new BiliLiveJsonParser.Danmaku(json);
                    BiliLiveListener_ItemsRecieved(new BiliLiveJsonParser.IItem[] { danmaku });
                    Thread.Sleep(1);
                }
            });
        }

        private void DanmakuScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            //DanmakuScrollViewer.ScrollToEnd();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SpeechUtil.Volume = (float)e.NewValue;
        }

        private void OutputDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem comboBoxItem = (ComboBoxItem)((ComboBox)sender).SelectedItem;
            if (comboBoxItem != null)
            {
                SpeechUtil.OutputDeviceId = (int)comboBoxItem.Tag;
            }
            else
            {
                SpeechUtil.OutputDeviceId = -1;
            }
            
        }
    }
}
