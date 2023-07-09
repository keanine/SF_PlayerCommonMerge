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

namespace SF_PlayerCommonMergeTool
{
    /// <summary>
    /// Interaction logic for WindowPreferences.xaml
    /// </summary>
    public partial class WindowPreferences : Window
    {
        public WindowPreferences()
        {
            InitializeComponent();

            chkLogDebugInfo.IsChecked = Preferences.LogDebugInformation;
            chkCheckForUpdates.IsChecked = Preferences.AllowCheckingForUpdates;
            chkUpdatePacFiles.IsChecked = Preferences.AllowUpdatingPac;
            cmbUpdateBranch.SelectedValue = Preferences.UpdateBranch;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            Preferences.LogDebugInformation = (bool)chkLogDebugInfo.IsChecked;
            Preferences.AllowCheckingForUpdates = (bool) chkCheckForUpdates.IsChecked;
            Preferences.UpdateBranch = cmbUpdateBranch.SelectedValue.ToString();
            Preferences.AllowUpdatingPac = (bool)chkUpdatePacFiles.IsChecked;

            Preferences.Save();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
