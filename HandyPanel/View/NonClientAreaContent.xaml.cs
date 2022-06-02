using Microsoft.Toolkit.Mvvm.Messaging;
using HandyPanel.ViewModel;
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
using HandyPanel.Util;

namespace HandyPanel.View
{
    /// <summary>
    /// NonClientAreaContent.xaml 的交互逻辑
    /// </summary>
    public partial class NonClientAreaContent : UserControl
    {
        public NonClientAreaContent()
        {
            InitializeComponent();
        }

        private void HandleSettinsButtonClick(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send( new SimpleMessage(SimpleMessageDefine.OpenSettingsMesssage) );
        }
    }
}
