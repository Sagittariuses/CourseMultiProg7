using ForntEndMultiprog7.Model;
using ForntEndMultiprog7.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace FrontEndMultiprog7.Windows
{
    /// <summary>
    /// Логика взаимодействия для WndManualMode.xaml
    /// </summary>
    public partial class WndManualMode : Window
    {
        public WndManualMode()
        {
            InitializeComponent();
        }

        private void BtnMinimezeBox_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnCloseBox_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnChooseFwFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opd = new OpenFileDialog();
            opd.Filter = VMPageMain.FileExt;

            Nullable<bool> result = opd.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                VMPageMain.FileFW = opd.FileName;
                MessageBox.Show(VMPageMain.FileFW);
            }
        }
    }
}
