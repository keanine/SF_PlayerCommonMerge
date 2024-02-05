using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

                string directory = Path.Combine(Preferences.appData, "categories", character.name);
                foreach (var file in Directory.GetFiles(directory))
                {
                    LoadCategoryFromFile(file, character, panel);
                }
            }

        }

        private void LoadCategoryFromFile(string fileName, Character character, StackPanel panel)
        {
            string jsonCategory = File.ReadAllText(fileName);
            Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));
            category = new Category(category.name, category.id, category.order, character.name, category.chunks);
            category.DeserializeAllChunkValues();
            CreateCheckbox(panel, category);
        }

        //private void LoadCategoryFromFile(string fileName, Character character, StackPanel panel)
        //{
        //    string jsonCategory = File.ReadAllText(fileName);
        //    Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));
        //    category = new Category(category.name, category.id, category.order, panel, character.name, category.chunks);
        //    category.DeserializeAllChunkValues();
        //    character.addonCategories.Add(category);
        //}

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            //Add all checked categories into MainWindow.addonCategories using category.order to sort them
            //Save addonCategories to json

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

            Close();
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

        private void CreateCheckbox(StackPanel panel, Category category)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Content = category.name;
            checkBox.IsChecked = false;
            panel.Children.Add(checkBox);

            addonCategories.Add(checkBox, category);
        }
    }
}
