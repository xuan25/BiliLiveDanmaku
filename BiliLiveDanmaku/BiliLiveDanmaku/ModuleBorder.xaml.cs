using BiliLiveDanmaku.Modules;
using System;
using System.Collections.Generic;
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
