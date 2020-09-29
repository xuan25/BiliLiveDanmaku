using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BiliLiveDanmaku.Modules
{
    /// <summary>
    /// Interaction logic for DisplayControl.xaml
    /// </summary>
    public partial class SpeechControl : UserControl
    {
        private SpeechModule Module { get; set; }

        public SpeechControl(SpeechModule SpeechModule, string defaultOutputDevice)
        {
            InitializeComponent();

            Module = SpeechModule;

            foreach (SpeechConfig.SpeechFilterOptions filterOption in Enum.GetValues(typeof(SpeechConfig.SpeechFilterOptions)))
            {
                bool initValue = true;
                if (SpeechModule.OptionDict.ContainsKey(filterOption))
                {
                    initValue = SpeechModule.OptionDict[filterOption];
                }
                else
                {
                    SpeechModule.OptionDict.Add(filterOption, initValue);
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
                    Margin = new Thickness(4),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = filterOption
                };
                checkBox.Checked += ShowOptionCkb_Checked;
                checkBox.Unchecked += ShowOptionCkb_Unchecked;
                OptionPanel.Children.Add(checkBox);
            }


            OutputDeviceCombo.Items.Add(new ComboBoxItem() { Content = "默认输出设备", Tag = -1 });
            OutputDeviceCombo.SelectedIndex = 0;
            int deviceCount = Wave.WaveOut.DeviceCount;
            for (int i = 0; i < deviceCount; i++)
            {
                Wave.MmeInterop.WaveOutCapabilities waveOutCapabilities = Wave.WaveOut.GetCapabilities(i);
                ComboBoxItem comboBoxItem = new ComboBoxItem() { Content = waveOutCapabilities.ProductName, Tag = i };
                OutputDeviceCombo.Items.Add(comboBoxItem);
                if (waveOutCapabilities.ProductName == defaultOutputDevice)
                {
                    OutputDeviceCombo.SelectedItem = comboBoxItem;
                }
            }

            VolumeSlider.Value = Module.Volume;
        }

        public string GetOutputDeviceName()
        {
            if (OutputDeviceCombo.SelectedItem != null)
                return ((ComboBoxItem)OutputDeviceCombo.SelectedItem).Content.ToString();
            return null;
        }

        public void SetSynthesizeQueueCount(int num)
        {
            Dispatcher.Invoke(() =>
            {
                SynthesizeQueueCountBox.Text = num.ToString();
            });
        }

        private void ShowOptionCkb_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;
            CheckBox checkBox = (CheckBox)sender;
            SpeechConfig.SpeechFilterOptions filterOptions = (SpeechConfig.SpeechFilterOptions)checkBox.Tag;
            Module.OptionDict[filterOptions] = true;
        }

        private void ShowOptionCkb_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;
            CheckBox checkBox = (CheckBox)sender;
            SpeechConfig.SpeechFilterOptions filterOptions = (SpeechConfig.SpeechFilterOptions)checkBox.Tag;
            Module.OptionDict[filterOptions] = false;
        }

        private DateTime VolumeTipCloseTime;
        private Task VolumeTipCloseTask;

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized)
                return;
            Module.SetVolume(e.NewValue);

            VolumeTip.Content = $"{e.NewValue * 100:0}%";
            if (IsLoaded)
            {
                if (VolumeSlider.IsMouseOver)
                {
                    Point point = Mouse.GetPosition(VolumeSlider);
                    VolumeTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
                    VolumeTip.PlacementTarget = VolumeSlider;
                    VolumeTip.HorizontalOffset = point.X + 10;
                    VolumeTip.VerticalOffset = VolumeTip.ActualHeight;
                    VolumeTip.IsOpen = true;
                }
                else
                {
                    VolumeTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
                    VolumeTip.PlacementTarget = VolumeSlider;
                    VolumeTip.HorizontalOffset = VolumeSlider.ActualWidth;
                    VolumeTip.VerticalOffset = VolumeTip.ActualHeight;
                    VolumeTip.IsOpen = true;
                    VolumeTipCloseTime = DateTime.Now.AddSeconds(1);
                    if (VolumeTipCloseTask == null)
                    {
                        VolumeTipCloseTask = Task.Factory.StartNew(() =>
                        {
                            Dispatcher.Invoke(async () =>
                            {
                                while (DateTime.Now < VolumeTipCloseTime)
                                {
                                    await Task.Delay(100);
                                }
                                if (!VolumeSlider.IsMouseOver)
                                {
                                    VolumeTip.IsOpen = false;
                                }
                                VolumeTipCloseTask = null;
                            });
                        });
                    }
                }
            }
        }

        private void VolumeSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            VolumeTip.IsOpen = false;
        }

        private void OutputDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
                return;
            ComboBoxItem comboBoxItem = (ComboBoxItem)((ComboBox)sender).SelectedItem;
            if (comboBoxItem != null)
            {
                Module.SetOutputDeviceId((int)comboBoxItem.Tag);
            }
            else
            {
                Module.SetOutputDeviceId(-1);
            }

        }

        private void ClearSpeechQueueBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;
            Module.ClearQueue();
        }

    }
}
