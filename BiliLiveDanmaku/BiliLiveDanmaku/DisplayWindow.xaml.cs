using BiliLiveDanmaku.Speech;
using BiliLiveDanmaku.UI;
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
    /// Interaction logic for DisplayWindow.xaml
    /// </summary>
    public partial class DisplayWindow : Window
    {

        public DisplayWindow()
        {
            InitializeComponent();

            this.Loaded += DisplayWindow_Loaded;
        }

        private void DisplayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            WindowLong.SetWindowLong(windowHandle, WindowLong.GWL_STYLE, (WindowLong.GetWindowLong(windowHandle, WindowLong.GWL_STYLE) | WindowLong.WS_CAPTION));
            WindowLong.SetWindowLong(windowHandle, WindowLong.GWL_EXSTYLE, (WindowLong.GetWindowLong(windowHandle, WindowLong.GWL_EXSTYLE) | WindowLong.WS_EX_TOOLWINDOW));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(this);
            if (hwndSource != null)
            {
                hwndSource.AddHook(new HwndSourceHook(this.WndProc));
            }
        }

        protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case HitTest.WM_NCHITTEST:
                    handled = true;
                    return HitTest.Hit(lParam, this.Top, this.Left, this.ActualHeight, this.ActualWidth);
            }
            return IntPtr.Zero;
        }

        private void DanmakuScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            //DanmakuScrollViewer.ScrollToEnd();
        }

        private void HeaderGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResizeMode resizeMode = this.ResizeMode;
            this.ResizeMode = ResizeMode.NoResize;
            this.DragMove();
            this.ResizeMode = resizeMode;
        }

        private DateTime CleanDanmakuTime { get; set; }
        private Task CleanDanmakuTask { get; set; }

        public void CleanPanel()
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

        public void AppendDanmaku(BiliLiveJsonParser.Danmaku item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new Danmaku(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendSuperChat(BiliLiveJsonParser.SuperChat item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new SuperChat(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendGift(BiliLiveJsonParser.Gift item)
        {
            Dispatcher.Invoke(() =>
            {
                //if (Gift.AppendGiftToExist(item))
                //    return;
                DanmakuPanel.Children.Add(new Gift(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendComboSend(BiliLiveJsonParser.ComboSend item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new ComboSend(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendWelcome(BiliLiveJsonParser.Welcome item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new Welcome(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendWelcomeGuard(BiliLiveJsonParser.WelcomeGuard item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new Welcome(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendGuardBuy(BiliLiveJsonParser.GuardBuy item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new GuardBuy(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendInteractWord(BiliLiveJsonParser.InteractWord item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new InteractWord(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }

        public void AppendRoomBlock(BiliLiveJsonParser.RoomBlock item)
        {
            Dispatcher.Invoke(() =>
            {
                DanmakuPanel.Children.Add(new RoomBlock(item));
                if (!DanmakuScrollViewer.IsMouseOver)
                    DanmakuScrollViewer.ScrollToBottom();
                CleanDanmakuTime = DateTime.UtcNow.AddSeconds(0.2);
            });

            if (CleanDanmakuTask == null || CleanDanmakuTask.IsCompleted)
            {
                CleanDanmakuTask = Task.Factory.StartNew(CleanPanel);
            }
        }





        private DateTime CleanRythmStormTime { get; set; }
        private Task CleanRythmStormTask { get; set; }

        private DateTime HideRythmStormTime { get; set; }
        private Task ShowRythmStormTask { get; set; }

        public void AppendRythmStorm(BiliLiveJsonParser.Danmaku item)
        {
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



        public System.Windows.Media.Imaging.RenderTargetBitmap CaptureSnapshot()
        {
            System.Windows.Media.Imaging.RenderTargetBitmap renderTarget = new System.Windows.Media.Imaging.RenderTargetBitmap((int)DisplayGrid.ActualWidth, (int)DisplayGrid.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(DisplayGrid);
            return renderTarget;
        }


    }
}
