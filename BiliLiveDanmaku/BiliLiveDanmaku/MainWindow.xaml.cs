using BiliLiveDanmaku.Speech;
using BiliLiveDanmaku.UI;
using BiliLiveDanmaku.Utils;
using BiliLiveHelper.Bili;
using Frame;
using JsonUtil;
using Speech;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;

namespace BiliLiveDanmaku
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
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

        private DisplayWindow displayWindow;

        public MainWindow()
        {
            InitializeComponent();

            displayWindow = new DisplayWindow();

            IsConnected = false;

            Config config = LoadConfig();

            if(config != null)
            {
                RoomIdBox.Text = config.RoomId;
                if (config.WindowRect.Width != 0 && config.WindowRect.Height != 0)
                {
                    displayWindow.Left = config.WindowRect.Left;
                    displayWindow.Top = config.WindowRect.Top;
                    displayWindow.Width = config.WindowRect.Width;
                    displayWindow.Height = config.WindowRect.Height;
                }
            }

            displayWindow.Show();

            InitFilterOptions(config);

            InitSpeech(config);

            InitCounters();

            GiftCache.CacheExpired += GiftCache_CacheExpired;

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
            else
            {
                VolumeSlider.Value = 1;
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

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveConfig();
            if (IsConnected)
            {
                LiveListener.Disconnect();

                // Wait for all Dispatcher tasks finished.
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => { }));
            }
            displayWindow.Close();
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
                                if (FilterValueDict[FilterOptions.Danmaku])
                                    displayWindow.AppendDanmaku(danmaku);
                                if (FilterValueDict[FilterOptions.DanmakuSpeech])
                                    SpeakDanmaku(danmaku);
                            }
                            else
                                if (FilterValueDict[FilterOptions.RythmStorm])
                                displayWindow.AppendRythmStorm(danmaku);
                            break;
                        case BiliLiveJsonParser.Cmds.SUPER_CHAT_MESSAGE:
                            BiliLiveJsonParser.SuperChat superChat = (BiliLiveJsonParser.SuperChat)item;
                            if (FilterValueDict[FilterOptions.SuperChat])
                                displayWindow.AppendSuperChat(superChat);
                            if (FilterValueDict[FilterOptions.SuperChatSpeech])
                                SpeakSuperChat(superChat);
                            break;
                        case BiliLiveJsonParser.Cmds.SEND_GIFT:
                            BiliLiveJsonParser.Gift gift = (BiliLiveJsonParser.Gift)item;
                            if (!GiftCache.AppendToExist(gift))
                            {
                                if (gift.CoinType == "gold" && FilterValueDict[FilterOptions.GoldenGift])
                                    displayWindow.AppendGift(gift);
                                else if (gift.CoinType == "silver" && FilterValueDict[FilterOptions.SilverGift])
                                    displayWindow.AppendGift(gift);
                                //SpeakGift(gift);
                            }
                            break;
                        case BiliLiveJsonParser.Cmds.COMBO_SEND:
                            BiliLiveJsonParser.ComboSend comboSend = (BiliLiveJsonParser.ComboSend)item;
                            if (FilterValueDict[FilterOptions.ComboSend])
                                displayWindow.AppendComboSend(comboSend);
                            if (FilterValueDict[FilterOptions.ComboSendSpeech])
                                SpeakComboSend(comboSend);
                            break;
                        case BiliLiveJsonParser.Cmds.WELCOME:
                            BiliLiveJsonParser.Welcome welcome = (BiliLiveJsonParser.Welcome)item;
                            if (FilterValueDict[FilterOptions.Welcome])
                                displayWindow.AppendWelcome(welcome);
                            break;
                        case BiliLiveJsonParser.Cmds.WELCOME_GUARD:
                            BiliLiveJsonParser.WelcomeGuard welcomeGuard = (BiliLiveJsonParser.WelcomeGuard)item;
                            if (FilterValueDict[FilterOptions.WelcomeGuard])
                                displayWindow.AppendWelcomeGuard(welcomeGuard);
                            break;
                        case BiliLiveJsonParser.Cmds.GUARD_BUY:
                            BiliLiveJsonParser.GuardBuy guardBuy = (BiliLiveJsonParser.GuardBuy)item;
                            if (!FilterValueDict[FilterOptions.GuardBuy])
                                displayWindow.AppendGuardBuy(guardBuy);
                            break;
                        case BiliLiveJsonParser.Cmds.INTERACT_WORD:
                            BiliLiveJsonParser.InteractWord interactWord = (BiliLiveJsonParser.InteractWord)item;
                            if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Enter && FilterValueDict[FilterOptions.InteractEnter])
                                displayWindow.AppendInteractWord(interactWord);
                            else if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Follow && FilterValueDict[FilterOptions.InteractFollow])
                                displayWindow.AppendInteractWord(interactWord);
                            else if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Share && FilterValueDict[FilterOptions.InteractShare])
                                displayWindow.AppendInteractWord(interactWord);
                            break;
                        case BiliLiveJsonParser.Cmds.ROOM_BLOCK_MSG:
                            BiliLiveJsonParser.RoomBlock roomBlock = (BiliLiveJsonParser.RoomBlock)item;
                            if (FilterValueDict[FilterOptions.RoomBlock])
                                displayWindow.AppendRoomBlock(roomBlock);
                            break;
                    }
                }
            });
            
        }

        #endregion

        #region Speech

        private void SpeakDanmaku(BiliLiveJsonParser.Danmaku item)
        {
            if (SpeechUtil.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.Danmaku(item.Sender.Name, item.Message);
                SpeechUtil.Speak(ssmlDoc);
            }
        }

        private void SpeakSuperChat(BiliLiveJsonParser.SuperChat item)
        {
            if (SpeechUtil.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.SuperChat(item.User.Name, item.Message);
                SpeechUtil.Speak(ssmlDoc);
            }
        }

        private void GiftCache_CacheExpired(object sender, GiftCache e)
        {
            SpeakGift(e);
        }

        private void SpeakGift(GiftCache gift)
        {
            if (gift.CoinType == "gold" && !FilterValueDict[FilterOptions.GoldenGiftSpeech])
                return;
            if (gift.CoinType == "silver" && !FilterValueDict[FilterOptions.SilverGiftSpeech])
                return;

            if (SpeechUtil.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.Gift(gift.Username, gift.Number, gift.GiftName, gift.Action);
                SpeechUtil.Speak(ssmlDoc);
            }
        }

        private void SpeakComboSend(BiliLiveJsonParser.ComboSend item)
        {
            if (SpeechUtil.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.Gift(item.Sender.Name, item.Number, item.GiftName, item.Action);
                SpeechUtil.Speak(ssmlDoc);
            }
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

        private void CaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Imaging.RenderTargetBitmap renderTarget = displayWindow.CaptureSnapshot();
            System.Windows.Media.Imaging.BitmapFrame bitmapFrame = System.Windows.Media.Imaging.BitmapFrame.Create(renderTarget);
            System.Windows.Media.Imaging.PngBitmapEncoder pngEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            pngEncoder.Frames.Add(bitmapFrame);
            using (FileStream fileStream = new FileStream("capture.png", FileMode.Create, FileAccess.Write))
            {
                pngEncoder.Save(fileStream);
            }
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
