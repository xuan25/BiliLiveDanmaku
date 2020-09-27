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
    public partial class DisplayControl : UserControl
    {
        private DisplayModule Module { get; set; }

        public DisplayControl(DisplayModule displayModule)
        {
            Module = displayModule;

            InitializeComponent();

            foreach (DisplayConfig.DisplayFilterOptions filterOption in Enum.GetValues(typeof(DisplayConfig.DisplayFilterOptions)))
            {
                bool initValue = true;
                if (displayModule.OptionDict.ContainsKey(filterOption))
                {
                    initValue = displayModule.OptionDict[filterOption];
                }
                else
                {
                    displayModule.OptionDict.Add(filterOption, initValue);
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
        }

        private void ShowOptionCkb_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            DisplayConfig.DisplayFilterOptions filterOptions = (DisplayConfig.DisplayFilterOptions)checkBox.Tag;
            Module.OptionDict[filterOptions] = true;
        }

        private void ShowOptionCkb_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            DisplayConfig.DisplayFilterOptions filterOptions = (DisplayConfig.DisplayFilterOptions)checkBox.Tag;
            Module.OptionDict[filterOptions] = false;
        }

        private void CaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            Module.CaptureSnapshot();
        }
    }
}
