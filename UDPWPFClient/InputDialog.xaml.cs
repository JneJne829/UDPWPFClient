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
using System.Windows.Shapes;

namespace UDPWPFClient
{
    /// <summary>
    /// InputDialog.xaml 的互動邏輯
    /// </summary>
    public partial class InputDialog : Window
    {
        public string ResponseText
        {
            get { return InputTextBox.Text; }
        }
        public InputDialog()
        {
            InitializeComponent();
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 只有當用戶點擊並拖曳Grid時，才移動窗口
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; 
            this.Close();
        }

    }
}
