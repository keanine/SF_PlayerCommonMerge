using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static SF_PlayerCommonMergeTool.MainWindow;

namespace SF_PlayerCommonMergeTool
{
    /// <summary>
    /// Interaction logic for WindowCategories.xaml
    /// </summary>
    public partial class WindowCategories : Window
    {
        MainWindow mainWindow;
        Dictionary<CheckBox, Category> addonCategories = new Dictionary<CheckBox, Category>();

        public WindowCategories()
        {
            InitializeComponent();

            mainWindow = (Application.Current.MainWindow as MainWindow);

            InitPanels();
        }

        private StackPanel GetCharacterPanel(Character character)
        {
            StackPanel panel;
            switch (character.name)
            {
                case "Sonic":
                    panel = stackSonicCategories;
                    break;
                case "Tails":
                    panel = stackTailsCategories;
                    break;
                case "Knuckles":
                    panel = stackKnucklesCategories;
                    break;
                case "Amy":
                    panel = stackAmyCategories;
                    break;
                default:
                    throw new Exception("Unknown Character: " + character.name);
            }

            return panel;
        }

        private void InitPanels()
        {
            //Load addonCategories.json if available
            //Checkmark categories that are currently selected in the json

            foreach (var character in mainWindow.characters.Values)
            {
                StackPanel panel = GetCharacterPanel(character);
                character.addonCategories.Clear();

                string directory = Path.Combine(Preferences.appData, "categories", character.name);
                foreach (var file in Directory.GetFiles(directory))
                {
                    LoadAddonCategoryFromFile(file, character, panel);
                }
            }

        }

        private void LoadAddonCategoryFromFile(string fileName, Character character, StackPanel panel)
        {
            string jsonCategory = File.ReadAllText(fileName);
            Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));
            category.DeserializeAllChunkValues();

            List<StoredData.CategorySelection> addonSelections;

            switch (character.name)
            {
                case "Tails":
                    addonSelections = mainWindow.storedData.addonCategorySelectionsTails;
                    break;
                case "Knuckles":
                    addonSelections = mainWindow.storedData.addonCategorySelectionsKnuckles;
                    break;
                case "Amy":
                    addonSelections = mainWindow.storedData.addonCategorySelectionsAmy;
                    break;
                case "Sonic":
                default:
                    addonSelections = mainWindow.storedData.addonCategorySelectionsSonic;
                    break;
            }

            bool isTicked = false;
            foreach (var selection in addonSelections)
            {
                if (selection.id == category.id)
                {
                    isTicked = true;
                    break;
                }
            }

            CreateCheckbox(panel, category, isTicked);
        }
        //private void LoadCategoryFromFile(string fileName, Character character, StackPanel panel)
        //{
        //    string jsonCategory = File.ReadAllText(fileName);
        //    Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));
        //    category = new Category(category.name, category.id, category.order, character.name, category.chunks);
        //    category.DeserializeAllChunkValues();
        //    CreateCheckbox(panel, category);
        //}

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            //Add all checked categories into MainWindow.addonCategories using category.order to sort them
            //Save addonCategories to json

            MessageBoxResult result = MessageBox.Show("Any unmerged changes will be discarded, continue?", "Warning", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var character in mainWindow.characters.Values)
                {
                    StackPanel panel = GetCharacterPanel(character);
                    character.addonCategories.Clear();

                    foreach (var item in panel.Children)
                    {
                        CheckBox categoryCheckbox = (CheckBox)item;

                        if (categoryCheckbox.IsChecked == true)
                        {
                            character.addonCategories.Add(addonCategories[categoryCheckbox]);
                        }
                    }
                }
                mainWindow.SaveAddonCategorySelections();

                Close();
            }
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

        private void CreateCheckbox(StackPanel panel, Category category, bool isChecked)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Content = category.name;
            checkBox.IsChecked = isChecked;
            panel.Children.Add(checkBox);

            addonCategories.Add(checkBox, category);
        }


        //private void DownloadOfficialAddonCategories(bool wait)
        //{
        //    //Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitleUpdateMessage("checking for updates")));
        //    try
        //    {



        //        if (AutoUpdaterLib.Updater.CheckForUpdates(applicationName, selectedUpdateServerURL, versionFileName))
        //        {
        //            MessageBoxResult result = MessageBox.Show("A new update has been found. Do you want to update?", "Update Found", MessageBoxButton.YesNo, MessageBoxImage.Information);

        //            if (result == MessageBoxResult.Yes)
        //            {
        //                var proc1 = new ProcessStartInfo();
        //                proc1.UseShellExecute = true;
        //                proc1.CreateNoWindow = false;
        //                proc1.WorkingDirectory = @"";
        //                proc1.Arguments = $"\"autoupdater.dll\" \"{applicationName}\" \"{selectedUpdateServerURL}\" \"{versionFileName}\" \"{updateListFileName}\" \"{executableFileName}\"";
        //                proc1.FileName = "dotnet.exe";
        //                Process.Start(proc1);

        //                Environment.Exit(1);
        //            }
        //            else
        //            {
        //                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitleUpdateMessage("update available")));
        //            }
        //        }
        //        else
        //        {
        //            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitleUpdateMessage("up-to-date")));
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(e.Message, "Error");
        //    }
        //}
    }
}
