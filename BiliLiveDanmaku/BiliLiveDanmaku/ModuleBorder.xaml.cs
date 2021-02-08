using BiliLiveDanmaku.Modules;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BiliLiveDanmaku
{
    /// <summary>
    /// Interaction logic for ModuleBorder.xaml
    /// </summary>
    public partial class ModuleBorder : UserControl
    {
        public IModule Module;

        public event EventHandler<IModule> Closing;

        public ModuleBorder(IModule module)
        {
            Module = module;

            InitializeComponent();

            UIElement uIElement = module.GetControl();
            ContentArea.Child = uIElement;
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            Closing?.Invoke(this, Module);
            Module.Close();
        }
    }
}
