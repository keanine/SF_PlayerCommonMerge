using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace SF_PlayerCommonMergeTool
{
    /// <summary>
    /// Interaction logic for WindowCategories.xaml
    /// </summary>
    public partial class WindowCategories : Window
    {
        public WindowCategories()
        {
            InitializeComponent();

            //Load addonCategories.json if available
            //Checkmark categories that are currently selected in the json
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            //Add all checked categories into MainWindow.addonCategories using category.order to sort them
            //Save addonCategories to json

            //Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", System.IO.Path.Combine(Preferences.appData, "categories"));
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            //download from UpdateServer/categories
            //Refresh StackPanel
        }
    }
}
