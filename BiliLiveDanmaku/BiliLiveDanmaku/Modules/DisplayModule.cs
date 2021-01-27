using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BiliLiveDanmaku.Utils;
using BiliLiveHelper.Bili;

namespace BiliLiveDanmaku.Modules
{
    public class DisplayModule : IModule
    {
        DisplayWindow Window { get; set; }
        DisplayControl Control { get; set; }
        public Dictionary<DisplayConfig.DisplayFilterOptions, bool> OptionDict { get; private set; }

        GiftCacheManager giftCacheManager;

        public DisplayModule()
        {

        }

        public UIElement GetControl()
        {
            return Control;
        }

        public void Init(IModuleConfig config)
        {
            giftCacheManager = new GiftCacheManager(5);

            DisplayConfig displayConfig = (DisplayConfig)config;
            OptionDict = displayConfig.OptionDict;

            Control = new DisplayControl(this);

            DisplayWindow displayWindow = new DisplayWindow();
            Window = displayWindow;
            if (displayConfig.WindowRect.Width != 0 && displayConfig.WindowRect.Height != 0)
            {
                displayWindow.Left = displayConfig.WindowRect.Left;
                displayWindow.Top = displayConfig.WindowRect.Top;
                displayWindow.Width = displayConfig.WindowRect.Width;
                displayWindow.Height = displayConfig.WindowRect.Height;
            }
            Window.Show();

            Control.SetLock(displayConfig.IsLocked);
        }

        public IModuleConfig GetConfig()
        {
            Rect windowRect = new Rect(Window.Left, Window.Top, Window.Width, Window.Height);
            DisplayConfig displayConfig = new DisplayConfig(OptionDict, windowRect, Window.IsLocked);
            return displayConfig;
        }

        public void ProcessItem(BiliLiveJsonParser.IItem item)
        {
            if (Window == null)
                return;

            switch (item.Cmd)
            {
                case BiliLiveJsonParser.Cmds.DANMU_MSG:
                    BiliLiveJsonParser.Danmaku danmaku = (BiliLiveJsonParser.Danmaku)item;
                    if (danmaku.Type == 0)
                    {
                        if (OptionDict[DisplayConfig.DisplayFilterOptions.Danmaku])
                            Window.AppendDanmaku(danmaku);
                    }
                    else
                        if (OptionDict[DisplayConfig.DisplayFilterOptions.RythmStorm])
                        Window.AppendRythmStorm(danmaku);
                    break;
                case BiliLiveJsonParser.Cmds.SUPER_CHAT_MESSAGE:
                    BiliLiveJsonParser.SuperChat superChat = (BiliLiveJsonParser.SuperChat)item;
                    if (OptionDict[DisplayConfig.DisplayFilterOptions.SuperChat])
                        Window.AppendSuperChat(superChat);
                    break;
                case BiliLiveJsonParser.Cmds.SEND_GIFT:
                    BiliLiveJsonParser.Gift gift = (BiliLiveJsonParser.Gift)item;
                    if (!giftCacheManager.AppendToExist(gift))
                    {
                        GiftCacheManager.GiftCache giftCache = giftCacheManager.AppendCache(gift);
                        if (gift.CoinType == "gold" && OptionDict[DisplayConfig.DisplayFilterOptions.GoldenGift])
                            Window.AppendGift(giftCache);
                        else if (gift.CoinType == "silver" && OptionDict[DisplayConfig.DisplayFilterOptions.SilverGift])
                            Window.AppendGift(giftCache);
                    }
                    break;
                case BiliLiveJsonParser.Cmds.COMBO_SEND:
                    BiliLiveJsonParser.ComboSend comboSend = (BiliLiveJsonParser.ComboSend)item;
                    if (OptionDict[DisplayConfig.DisplayFilterOptions.ComboSend])
                        Window.AppendComboSend(comboSend);
                    break;
                case BiliLiveJsonParser.Cmds.WELCOME:
                    BiliLiveJsonParser.Welcome welcome = (BiliLiveJsonParser.Welcome)item;
                    if (OptionDict[DisplayConfig.DisplayFilterOptions.Welcome])
                        Window.AppendWelcome(welcome);
                    break;
                case BiliLiveJsonParser.Cmds.WELCOME_GUARD:
                    BiliLiveJsonParser.WelcomeGuard welcomeGuard = (BiliLiveJsonParser.WelcomeGuard)item;
                    if (OptionDict[DisplayConfig.DisplayFilterOptions.WelcomeGuard])
                        Window.AppendWelcomeGuard(welcomeGuard);
                    break;
                case BiliLiveJsonParser.Cmds.GUARD_BUY:
                    BiliLiveJsonParser.GuardBuy guardBuy = (BiliLiveJsonParser.GuardBuy)item;
                    if (OptionDict[DisplayConfig.DisplayFilterOptions.GuardBuy])
                        Window.AppendGuardBuy(guardBuy);
                    break;
                case BiliLiveJsonParser.Cmds.INTERACT_WORD:
                    BiliLiveJsonParser.InteractWord interactWord = (BiliLiveJsonParser.InteractWord)item;
                    if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Entry && OptionDict[DisplayConfig.DisplayFilterOptions.InteractEntry])
                        Window.AppendInteractWord(interactWord);
                    else if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Attention && OptionDict[DisplayConfig.DisplayFilterOptions.InteractAttention])
                        Window.AppendInteractWord(interactWord);
                    else if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.Share && OptionDict[DisplayConfig.DisplayFilterOptions.InteractShare])
                        Window.AppendInteractWord(interactWord);
                    else if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.SpecialAttention && OptionDict[DisplayConfig.DisplayFilterOptions.InteractSpecialAttention])
                        Window.AppendInteractWord(interactWord);
                    else if (interactWord.MessageType == BiliLiveJsonParser.InteractWord.MessageTypes.MutualAttention && OptionDict[DisplayConfig.DisplayFilterOptions.InteractMutualAttention])
                        Window.AppendInteractWord(interactWord);
                    break;
                case BiliLiveJsonParser.Cmds.ROOM_BLOCK_MSG:
                    BiliLiveJsonParser.RoomBlock roomBlock = (BiliLiveJsonParser.RoomBlock)item;
                    if (OptionDict[DisplayConfig.DisplayFilterOptions.RoomBlock])
                        Window.AppendRoomBlock(roomBlock);
                    break;
            }
        }

        public void Close()
        {
            Window.Close();
            Window = null;
        }

        public void CaptureSnapshot()
        {
            RenderTargetBitmap renderTarget = Window.CaptureSnapshot();
            
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "BMP files (*.bmp)|*.bmp|GIF files (*.gif)|*.gif|JPEG files (*.jpg)|*.jpg|PNG files (*.png)|*.png|TIFF files (*.tif)|*.tif|WMP files (*.wmp)|*.wmp|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 4;
            saveFileDialog.DefaultExt = "png";
            saveFileDialog.FileName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog(Window) == false) 
            {
                return;
            }

            BitmapFrame bitmapFrame = BitmapFrame.Create(renderTarget);
            BitmapEncoder bitmapEncoder = null;
            switch (Path.GetExtension(saveFileDialog.FileName).Substring(1).ToLower())
            {
                case "bmp":
                    bitmapEncoder = new BmpBitmapEncoder();
                    break;
                case "gif":
                    bitmapEncoder = new GifBitmapEncoder();
                    break;
                case "jpg":
                case "jpeg":
                    bitmapEncoder = new JpegBitmapEncoder();
                    break;
                case "png":
                    bitmapEncoder = new PngBitmapEncoder();
                    break;
                case "tif":
                case "tiff":
                    bitmapEncoder = new TiffBitmapEncoder();
                    break;
                case "wmp":
                    bitmapEncoder = new WmpBitmapEncoder();
                    break;
            }
            if(bitmapEncoder == null)
            {
                MessageBox.Show("不支持的格式");
                return;
            }
            bitmapEncoder.Frames.Add(bitmapFrame);
            using (FileStream fileStream = new FileStream("capture.png", FileMode.Create, FileAccess.Write))
            {
                bitmapEncoder.Save(fileStream);
            }
        }

        public void LockWindow(bool value)
        {
            Window.Lock(value);
        }
    }
}
