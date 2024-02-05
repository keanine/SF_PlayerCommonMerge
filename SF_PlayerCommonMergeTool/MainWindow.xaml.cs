using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Diagnostics;

using ComboBox = System.Windows.Controls.ComboBox;
using Path = System.IO.Path;

namespace SF_PlayerCommonMergeTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string applicationName = "PlayerCommonMergeTool";
        private static string updateServerURL = @"https://raw.githubusercontent.com/keanine/SF_PlayerCommonMerge/main/UpdateServer/";
        private static string internalUpdateServerURL = @"https://raw.githubusercontent.com/keanine/SF_PlayerCommonMerge/main/InternalUpdateServer/";
        private static string devUpdateServerURL = @"https://raw.githubusercontent.com/keanine/SF_PlayerCommonMerge/development/DevUpdateServer/";
        private static string versionFileName = "version.ini";
        private static string updateListFileName = "updatelist.txt";
        private static string executableFileName = "SF_PlayerCommonMergeTool.exe";

        public ComboBox SetAllComboBox;
        public ComboBox SetAllComboBoxTails;
        public ComboBox SetAllComboBoxKnuckles;
        public ComboBox SetAllComboBoxAmy;

        public List<Category> categoriesSonic = new List<Category>();
        public List<Category> addonCategoriesSonic = new List<Category>();

        public List<Category> categoriesTails = new List<Category>();
        public List<Category> addonCategoriesTails = new List<Category>();

        public List<Category> categoriesKnuckles = new List<Category>();
        public List<Category> addonCategoriesKnuckles = new List<Category>();

        public List<Category> categoriesAmy = new List<Category>();
        public List<Category> addonCategoriesAmy = new List<Category>();

        public List<Mod> mods = new List<Mod>();

        public string workspace = "MergeTemp\\";
        string modsFolder = string.Empty;

        public StoredData storedData = new StoredData();

        public bool storedInstallFolderExists = false;

        private string[] playerCommonUpdaterCleanup =
        {
            //"playercommon_PC_OLD.pac",
            //"playercommon_PC_u2.pac",
            //"u0.pos",
            //"u2_u0.pos"
        };

        private string[] requiredTools =
        {
            "HedgeArcPack.exe",
            "PlayerCommonUpdaterV2.exe"
        };

        public string iniTemplate =
@"[Desc]
Title=""MergedPlayerCommon""
Description=""None""
Version=1.0
Date=""2023-07-04""
Author=""Keanine""
AuthorURL=""https://github.com/keanine/SF_PlayerCommonMerge""

[Main]
UpdateServer=""""
SaveFile=""""
ID=""""
IncludeDir0="".""
IncludeDirCount=1
DependsCount=0
DLLFile=""""
CodeFile=""""
ConfigSchemaFile=""""";

        public MainWindow()
        {
            InitializeComponent();


            Preferences.appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SonicFrontiersModding\\SF_PlayerCommonMerge\\";
            Preferences.Initialize();

            this.Title = "PlayerCommon Merge Tool v" + Preferences.ToolVersion.ToString("0.0");

            if (Directory.Exists("tools"))
            {
                foreach (var tool in requiredTools)
                {
                    if (!File.Exists(Path.Combine("tools/", tool)))
                    {
                        if (tool == "PlayerCommonUpdaterV2.exe")
                        {
                            MessageBox.Show($"Could not find {tool} in the local tools folder. This could be because of a false-positive virus warning. Pac updating will be disabled until you restart the tool", "Warning");
                            Preferences.AllowUpdatingPac = false;
                        }
                        else
                        { 
                            MessageBox.Show($"Could not find {tool} in the local tools folder", "Error");
                            Close();
                        } 
                    }
                }
            }
            else
            {
                MessageBox.Show($"Could not find the local tools folder. This is required for operation of the Merge Tool");
            }


            Debugging.WriteToLog($"\n=== Launched Merge Tool [{DateTime.Now.ToString()}] ===");

            if (Preferences.AllowCheckingForUpdates)
            {
                Thread updateThread = new Thread(() => CheckForUpdates(true));
                updateThread.Start();
            }
            else
            {
                SetTitle("PlayerCommon Merge Tool v" + Preferences.ToolVersion + " [updates disabled]");
            }

            if (File.Exists(Preferences.appData + "\\storedData.json"))
            {
                LoadStoredData();
                GameFolderTextbox.Text = storedData.installLocation;

                storedInstallFolderExists = Directory.Exists(storedData.installLocation);
                if (storedInstallFolderExists)
                {
                    IniUtility utility = new IniUtility(storedData.installLocation + "\\cpkredir.ini");
                    modsFolder = Path.GetDirectoryName(utility.Read("ModsDbIni", "CPKREDIR")) + "\\";

                    Load();
                }
                else
                {
                    MessageBox.Show("Your game folder has moved or does not exist. Please update it's location to continue using this tool");
                }

            }
        }

        private void AddToTitle(string message)
        {
            Window.GetWindow(this).Title += message;
        }

        private void SetTitle(string message)
        {
            Window.GetWindow(this).Title = message;
        }

        private void CheckForUpdates(bool wait)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitle("PlayerCommon Merge Tool v" + Preferences.ToolVersion + " [checking for updates]")));

            if (wait)
            {
                Thread.Sleep(2000);
            }
            
            try
            {
                string selectedUpdateServerURL = string.Empty;
                switch (Preferences.UpdateBranch)
                {
                    case "Main":
                        selectedUpdateServerURL = updateServerURL;
                        break;
                    case "Development":
                        selectedUpdateServerURL = devUpdateServerURL;
                        break;
                    default:
                        selectedUpdateServerURL = updateServerURL;
                        break;
                }
                

                if (AutoUpdaterLib.Updater.CheckForUpdates(applicationName, selectedUpdateServerURL, versionFileName))
                {
                    MessageBoxResult result = MessageBox.Show("A new update has been found. Do you want to update?", "Update Found", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var proc1 = new ProcessStartInfo();
                        proc1.UseShellExecute = true;
                        proc1.CreateNoWindow = false;
                        proc1.WorkingDirectory = @"";
                        proc1.Arguments = $"\"autoupdater.dll\" \"{applicationName}\" \"{selectedUpdateServerURL}\" \"{versionFileName}\" \"{updateListFileName}\" \"{executableFileName}\"";
                        proc1.FileName = "dotnet.exe";
                        Process.Start(proc1);

                        Environment.Exit(1);
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitle("PlayerCommon Merge Tool v" + Preferences.ToolVersion + " [up-to-date]")));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }
        }

        public void LoadStoredData()
        {
            string json = File.ReadAllText(Preferences.appData + "storedData.json");
            storedData = (StoredData)JsonSerializer.Deserialize(json, typeof(StoredData));
        }

        public void SaveStoredData()
        {
            string json = JsonSerializer.Serialize(storedData);
            File.WriteAllText(Preferences.appData + "storedData.json", json);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private void TryAddMod(string folder)
        {
            string pacFile = folder + "/raw/character/playercommon.pac";
            if (File.Exists(pacFile))
            {
                Mod mod = new Mod(folder);
                if (mod.title != "MergedPlayerCommon")
                {
                    mods.Add(mod);
                }
            }
        }

        private void Load()
        {
            if (storedData.installLocation != string.Empty && Directory.Exists(storedData.installLocation))
            {
                CategoryStackPanel.Children.Clear();
                categoriesSonic.Clear();
                addonCategoriesSonic.Clear();

                categoriesTails.Clear();
                mods.Clear();

                categoriesSonic.Add(new Category("Set All", "set_all", CategoryStackPanel, out SetAllComboBox));
                SetAllComboBox.SelectionChanged += SetAllComboBox_SelectionChanged;

                TextBlock space = new TextBlock();
                space.Height = 10;
                CategoryStackPanel.Children.Add(space);

                categoriesTails.Add(new Category("Set All", "set_all_tails", TailsCategoryStackPanel, out SetAllComboBoxTails));
                SetAllComboBoxTails.SelectionChanged += SetAllComboBox_SelectionChanged;

                space = new TextBlock();
                space.Height = 10;
                TailsCategoryStackPanel.Children.Add(space);


                //// Game Update v1.1 ===================================================================================================================
                //categories.Add(new Category("Open Zone Physics", "openzone", 0x72E0, 0xB20, CategoryStackPanel));
                //categories.Add(new Category("Cyberspace 3D Physics", "cyber3d", 0x7F80, 0xB20, CategoryStackPanel));
                //categories.Add(new Category("Cyberspace 2D Physics", "cyber2d", 0x8AA0, 0xB20, CategoryStackPanel));
                //categories.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x72A0, CategoryStackPanel));
                //categories.Add(new Category("Parry", "parry", 0x7Cd0, 0x24, CategoryStackPanel));
                //categories.Add(new Category("Cyloop", "cyloop", 0x5250, 0x1440, CategoryStackPanel));
                //// ====================================================================================================================================

                //// Game Update v1.3 ===================================================================================================================
                //categories.Add(new Category("Open Zone Physics", "openzone", 0x73B0, 0xDE8, 1, CategoryStackPanel)); //Includes water physics
                //categories.Add(new Category("Cyberspace 3D Physics", "cyber3d", 0x81A0, 0xC40, 2, CategoryStackPanel));
                //categories.Add(new Category("Cyberspace 2D Physics", "cyber2d", 0x8DE0, 0xC40, 3, CategoryStackPanel));
                //categories.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x7370, 4, CategoryStackPanel));
                //categories.Add(new Category("Cyloop", "cyloop", 0x5410, 0x1440, 5, CategoryStackPanel));
                //
                //// TEMP. These should be dynamically loaded, hence why they are serialized and then loaded from the directory
                //Category categoryParry = new Category("Parry", "parry", 6, null, new CategoryChunk(0x7B54, 0x24));
                //Category categorySpinDash = new Category("Spin Dash", "spindash", 7, null,
                //    new CategoryChunk(0x7E50, 0xF8),
                //    new CategoryChunk(0x8C40, 0xF8),
                //    new CategoryChunk(0x9880, 0xF8));
                //SerializeCategory(categoryParry);
                //SerializeCategory(categorySpinDash);
                //// ====================================================================================================================================

                // Game Update v1.4 ===================================================================================================================

                // Sonic
                categoriesSonic.Add(new Category("Open Zone Physics", "openzone", 0x8080, 0xEE0, 1, CategoryStackPanel));
                categoriesSonic.Add(new Category("Cyberspace 3D Physics", "cyber3d", 0x9110, 0xEE0, 2, CategoryStackPanel));
                categoriesSonic.Add(new Category("2D Physics", "cyber2d", 0x9FF0, 0xEE0, 3, CategoryStackPanel));
                categoriesSonic.Add(new Category("Water Physics", "water", 0x8F60, 0x1A8, 4, CategoryStackPanel));
                categoriesSonic.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x8040, 5, CategoryStackPanel));
                categoriesSonic.Add(new Category("Cyloop", "cyloop", 0x6098, 0x1460, 6, CategoryStackPanel));

                // Sonic TEMP. These should be dynamically loaded, hence why they are serialized and then loaded from the directory
                Category categoryParry = new Category("Parry", "parry", 7, null, new CategoryChunk(0x8878, 0x60));
                Category categorySpinDash = new Category("Spin Dash", "spindash", 8, null,
                    new CategoryChunk(0x8BB0, 0xF8),
                    new CategoryChunk(0x9C40, 0xF8),
                    new CategoryChunk(0xAB20, 0xF8));
                SerializeCategory(categoryParry);
                SerializeCategory(categorySpinDash);
                // ====================================================================================================================================

                // Tails
                categoriesTails.Add(new Category("Open Zone Physics", "openzone", 0x61A0, 0xCB0, 1, TailsCategoryStackPanel));
                categoriesTails.Add(new Category("2D Physics", "cyber2d", 0x7000, 0xCB0, 2, TailsCategoryStackPanel));
                categoriesTails.Add(new Category("Water Physics", "water", 0x6E50, 0x1A8, 3, TailsCategoryStackPanel));
                categoriesTails.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x6160, 4, TailsCategoryStackPanel));
                categoriesTails.Add(new Category("Cyloop", "cyloop", 0x4D38, 0x1460, 5, TailsCategoryStackPanel));

                // Knuckles
                //categories.Add(new Category("Open Zone Physics", "openzone", 0x6260, 0xC00, 1, CategoryStackPanel));
                //categories.Add(new Category("2D Physics", "cyber2d", 0x7010, 0xC00, 2, CategoryStackPanel));
                //categories.Add(new Category("Water Physics", "water", 0x6E60, 0x1A8, 3, CategoryStackPanel));
                //categories.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x6220, 4, CategoryStackPanel));
                //categories.Add(new Category("Cyloop", "cyloop", 0x4DF8, 0x1460, 5, CategoryStackPanel));

                // Amy
                //categories.Add(new Category("Open Zone Physics", "openzone", 0x61A0, 0xD30, 1, CategoryStackPanel));
                //categories.Add(new Category("2D Physics", "cyber2d", 0x7080, 0xD30, 2, CategoryStackPanel));
                //categories.Add(new Category("Water Physics", "water", 0x6ED0, 0x1A8, 3, CategoryStackPanel));
                //categories.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x6160, 4, CategoryStackPanel));
                //categories.Add(new Category("Cyloop", "cyloop", 0x4D40, 0x1460, 5, CategoryStackPanel));

                Debugging.WriteToLog("Temporarily loading all addon categories as the system has not been fully implemented");
                LoadAllCategoriesFromDirectory();
                Debugging.WriteToLog("Loaded categories");

                //SerializeCategories();

                string[] folders = Directory.GetDirectories(modsFolder);

                foreach (var folder in folders)
                {
                    if (Directory.Exists(folder + "/raw/"))
                    {
                        TryAddMod(folder);
                    }
                    else
                    {
                        foreach (var configuration in Directory.GetDirectories(folder))
                        {
                            TryAddMod(configuration);
                        }
                    }
                }

                foreach (var mod in mods)
                {
                    AddToComboBox(mod);
                }

                LoadDropdownDefaults("Sonic", storedData.categorySelection, categoriesSonic);
                LoadDropdownDefaults("Tails", storedData.categorySelectionTails, categoriesTails);
                LoadDropdownDefaults("Knuckles", storedData.categorySelectionKnuckles, categoriesKnuckles);
                LoadDropdownDefaults("Amy", storedData.categorySelectionAmy, categoriesAmy);
                Debugging.WriteToLog("Finished Loading");
            }
        }

        private void LoadDropdownDefaults(string id, List<StoredData.CategorySelection> categorySelection, List<Category> characterCategories)
        {
            string modFolder = modsFolder + "Merged" + id;

            if (Directory.Exists(modFolder))
            {
                if (categorySelection.Count > 0)
                {
                    foreach (var selection in categorySelection)
                    {
                        bool success = false;
                        foreach (var category in characterCategories)
                        {
                            if (category.HasOffset)
                            {
                                if (selection.id == category.id)
                                {
                                    for (int i = 1; i < category.comboBox.Items.Count; i++)
                                    {
                                        var item = category.comboBox.Items[i];
                                        if ((item as Mod).title == selection.modTitle)
                                        {
                                            category.comboBox.SelectedItem = item;
                                            success = true;
                                        }
                                        if (success)
                                            break;
                                    }
                                }
                                if (success)
                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                switch (id)
                {
                    case "Sonic":
                        storedData.categorySelection.Clear();
                        break;
                    case "Tails":
                        storedData.categorySelectionTails.Clear();
                        break;
                    case "Knuckles":
                        storedData.categorySelectionKnuckles.Clear();
                        break;
                    case "Amy":
                        storedData.categorySelectionAmy.Clear();
                        break;
                }
                SaveStoredData();
                Debugging.WriteToLog("No mods folder found");
            }
        }

        private void SerializeCategories()
        {
            foreach (var category in categoriesSonic)
            {
                SerializeCategory(category);
            }
        }
        private void SerializeCategory(Category category)
        {
            string directory = Path.Combine(Preferences.appData, "categories");
            string filePath = Path.Combine(directory, $"{category.id}.json");
            Directory.CreateDirectory(directory);

            if (!File.Exists(filePath))
            {
                if (category.HasOffset)
                {
                    string jsonCategory = JsonSerializer.Serialize(category, new JsonSerializerOptions() { WriteIndented = true });
                    File.WriteAllText(filePath, jsonCategory);
                }
            }
        }

        private void LoadAllCategoriesFromDirectory()
        {
            addonCategoriesSonic.Clear();
            string directory = Path.Combine(Preferences.appData, "categories");
            foreach (var file in Directory.GetFiles(directory))
            {
                LoadCategoryFromFile(file);
            }
        }
        private void LoadCategoryFromFile(string fileName)
        {
            string jsonCategory = File.ReadAllText(fileName);
            Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));
            category = new Category(category.name, category.id, category.order, CategoryStackPanel, category.chunks);
            addonCategoriesSonic.Add(category);
        }

        public void LoadComboBox(ComboBox comboBox, string[] modFolders)
        {
            comboBox.Items.Clear();

            foreach (var modFolder in modFolders)
            {
                comboBox.Items.Add(modFolder);
            }
        }

        private void AddToComboBox(object value)
        {
            foreach (var category in categoriesSonic)
            {
                category.comboBox.Items.Add(value);
            }
            foreach (var category in addonCategoriesSonic)
            {
                category.comboBox.Items.Add(value);
            }
        }

        private void SetAllComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var cateogry in categoriesSonic)
            {
                cateogry.comboBox.SelectedIndex = (sender as ComboBox).SelectedIndex;
            }
            foreach (var cateogry in addonCategoriesSonic)
            {
                cateogry.comboBox.SelectedIndex = (sender as ComboBox).SelectedIndex;
            }
        }

        private void CleanUpPlayerCommonUpdater()
        {
            foreach (string file in playerCommonUpdaterCleanup)
            {
                if (File.Exists("tools/" + file))
                {
                    File.Delete("tools/" + file);
                }
            }
            File.Delete("tools/playercommon.pac");
        }

        private void UpdatePac(string modPac, string destination)
        {
            Debugging.WriteToLog("Running UpdatePac on: " + destination);
            File.Copy(modPac, destination, true);

            CleanUpPlayerCommonUpdater();
            RunCMD("tools/PlayerCommonUpdaterV2.exe", $"\"{destination}\"", "-2", "-nowait");

            if (File.Exists("tools/playercommon.pac"))
            {
                File.Copy("tools/playercommon.pac", destination, true);
            }
        }

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            Merge(categoriesSonic, addonCategoriesSonic, "Sonic", "player_common");
        }

        private void MergeButtonTails_Click(object sender, RoutedEventArgs e)
        {
            Merge(categoriesTails, addonCategoriesTails, "Tails", "tails_common");
        }

        private void MergeButtonKnuckles_Click(object sender, RoutedEventArgs e)
        {
            Merge(categoriesKnuckles, addonCategoriesKnuckles, "Knuckles", "knuckles_common");
        }

        private void MergeButtonAmy_Click(object sender, RoutedEventArgs e)
        {
            Merge(categoriesAmy, addonCategoriesAmy, "Amy", "amy_common");
        }

        private void Merge(List<Category> characterCategories, List<Category> characterAddonCategories, string id, string playerCommonRFL)
        {
            if (storedData.installLocation != string.Empty)
            {
                string pacFile = "playercommon";

                List<Category> mergeCategories = new List<Category>(characterCategories);
                mergeCategories.AddRange(characterAddonCategories);

                Debugging.WriteToLog("Running Merge for " + id);

                string modFolder = modsFolder + "Merged" + id;
                string newPacFolder = modFolder + "\\raw\\character\\";

                if (!Directory.Exists(newPacFolder))
                {
                    Directory.CreateDirectory(newPacFolder);
                }

                if (!Directory.Exists(workspace))
                {
                    Directory.CreateDirectory(workspace);
                }

                Debugging.WriteToLog($"Copying vanilla {id} file");

                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = File.OpenRead(storedData.installLocation + "\\image\\x64\\raw\\character\\" + pacFile + ".pac"))
                    {
                        var hash = md5.ComputeHash(stream);
                        if (Preferences.LatestPlayercommonHash != BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant())
                        {
                            MessageBox.Show($"The {pacFile}.pac in \"" + storedData.installLocation + "\\image\\x64\\raw\\character\\\"" + " is not for the correct version of Sonic Frontiers or has been modified. Please make sure your game is up to date with " + Preferences.NameOfGameUpdate + ". The merge operation has been cancelled.", "Error");
                            return;
                        }
                    }
                }

                File.Copy(storedData.installLocation + $"\\image\\x64\\raw\\character\\{pacFile}.pac", workspace + $"{pacFile}_vanilla.pac", true);

                Debugging.WriteToLog($"Extracting: \"{workspace}{pacFile}_vanilla.pac\" to \"{workspace}out_vanilla\"");
                RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}{pacFile}_vanilla.pac\"", $"\"{workspace}out_vanilla\"", "-E", "-T=rangers");

                if (Preferences.AllowUpdatingPac)
                {
                    if (!File.Exists("tools/PlayerCommonUpdaterV2.exe"))
                    {
                        MessageBoxResult result = MessageBox.Show("tools/PlayerCommonUpdaterV2.exe could not be found. This could be because of a false-positive virus warning that has removed the file. Pac updating will be disabled until you restart the tool and restore the file. Continue?", "Warning", MessageBoxButton.OKCancel);
                        
                        if (result == MessageBoxResult.Cancel)
                        {
                            return;
                        }
                        else
                        {
                            Preferences.AllowUpdatingPac = false;
                        }
                    }
                }
                else
                {
                    Debugging.WriteToLog(".pac updates disabled in Preferences");
                }

                foreach (var category in mergeCategories)
                {
                    if (category.HasOffset && category.comboBox.SelectedIndex > 0)
                    {
                        Mod mod = mods[category.comboBox.SelectedIndex - 1];
                        string copyOfPac = workspace + $"{pacFile}_" + category.id + ".pac";

                        if (Preferences.AllowUpdatingPac)
                        {
                            UpdatePac(mod.path + $"\\raw\\character\\{pacFile}.pac", copyOfPac);
                        }

                        Debugging.WriteToLog($"Extracting: \"{copyOfPac}\" to \"{workspace}out_{category.id}\"");
                        RunCMD("tools/HedgeArcPack.exe", $"\"{copyOfPac}\"", $"\"{workspace}out_{category.id}\"", "-E", "-T=rangers");

                    }
                    else if (category.comboBox.SelectedIndex == 0)
                    {
                        Debugging.WriteToLog($"Extracting: \"{workspace}{pacFile}_vanilla.pac\" to \"{workspace}out_{category.id}\"");
                        RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}{pacFile}_vanilla.pac\"", $"\"{workspace}out_{category.id}\"", "-E", "-T=rangers");
                    }
                }
                Debugging.WriteToLog($"Extracted all category files");

                CleanUpPlayerCommonUpdater();

                Debugging.WriteToLog($"Read vanilla RFL");
                string rfl = $"{workspace}\\out_vanilla\\{playerCommonRFL}.rfl";
                byte[] file = File.ReadAllBytes(rfl);
                Debugging.WriteToLog($"Read vanilla RFL successfully");

                foreach (var category in mergeCategories)
                {
                    if (category.HasOffset && category.comboBox.SelectedIndex >= 0)
                    {
                        Debugging.WriteToLog($"Merging bytes from {category.id} RFL");
                        byte[] categoryFile = File.ReadAllBytes($"{workspace}\\out_{category.id}\\{playerCommonRFL}.rfl");

                        foreach (var chunk in category.chunks)
                        {
                            byte[] data = categoryFile.ToList().GetRange(chunk.offset, chunk.size).ToArray();

                            for (int i = 0; i < chunk.size; i++)
                            {
                                file[i + chunk.offset] = data[i];
                            }
                        }
                        Debugging.WriteToLog($"Successfully merged bytes from {category.id} RFL");
                    }
                }

                Debugging.WriteToLog($"Writing all merged bytes to output RFL");
                File.WriteAllBytes(rfl, file);

                Debugging.WriteToLog($"Packing merged RFL");
                RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}out_vanilla\"", $"\"{workspace}out_vanilla.pac\"", "-P", "-T=rangers");
                Debugging.WriteToLog($"Successfully packed RFL");

                File.Move($"{workspace}\\out_vanilla.pac", newPacFolder + $"{pacFile}.pac", true);
                File.WriteAllText(modFolder + "\\mod.ini", iniTemplate);
                Debugging.WriteToLog($"Created merged mod");

                ClearDirectory(new DirectoryInfo(workspace));
                Directory.Delete(workspace);
                Debugging.WriteToLog($"Workspace cleaned up");


                storedData.categorySelection.Clear();
                
                foreach (var category in mergeCategories)
                {
                    Mod mod = (category.comboBox.SelectedItem as Mod);

                    if (mod != null)
                    {
                        switch (id)
                        {
                            case "Sonic":
                                storedData.categorySelection.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                                break;
                            case "Tails":
                                storedData.categorySelectionTails.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                                break;
                            case "Knuckles":
                                storedData.categorySelectionKnuckles.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                                break;
                            case "Amy":
                                storedData.categorySelectionAmy.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                                break;
                        }
                    }
                }
                SaveStoredData();

                Debugging.WriteToLog($"Merge Successful!");
                MessageBox.Show($"You can now close this tool, open Hedge Mod Manager and enable the newly generated mod \"Merged {id}\"", "Merge complete!");
            }
        }

        public void ClearDirectory(DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserForWPF.Dialog browserDialog = new FolderBrowserForWPF.Dialog();
            browserDialog.Title = "Select your SonicFrontiers root folder";

            while (true)
            {
                if (browserDialog.ShowDialog() == true)
                {
                    string folder = browserDialog.FileName;
                    if (folder != string.Empty && Directory.Exists(folder))
                    {
                        if (!File.Exists(folder + "\\image\\x64\\raw\\character\\playercommon.pac"))
                        {
                            MessageBoxResult messageResult = MessageBox.Show("playercommon.pac could not be found. Please select the SonicFrontiers root folder. This is the folder containing the image folder.", "Error", MessageBoxButton.OK);
                            
                            if (storedData.installLocation == folder)
                                MergeButton.IsEnabled = false;
                            break;
                        }
                        else if (!File.Exists(folder + "\\cpkredir.ini"))
                        {
                            MessageBoxResult messageResult = MessageBox.Show("cpkredir.ini could not be found. Please make sure you have HedgeModManager installed.", "Error", MessageBoxButton.OK);
                            if (storedData.installLocation == folder)
                                MergeButton.IsEnabled = false;
                            break;
                        }

                        storedData.installLocation = folder;
                        GameFolderTextbox.Text = folder;

                        IniUtility utility = new IniUtility(storedData.installLocation + "\\cpkredir.ini");
                        modsFolder = Path.GetDirectoryName(utility.Read("ModsDbIni", "CPKREDIR")) + "\\";
                        
                        MergeButton.IsEnabled = true;

                        if (!Directory.Exists(Preferences.appData))
                        {
                            Directory.CreateDirectory(Preferences.appData);
                        }

                        SaveStoredData();
                        Load();
                        break;
                    }
                    else
                    {
                        MessageBoxResult messageResult = MessageBox.Show("This directory does not exist?", "Error", MessageBoxButton.OKCancel);
                        if (messageResult == MessageBoxResult.Cancel)
                        {
                            if (storedData.installLocation == folder)
                                MergeButton.IsEnabled = false;
                            break;
                        }

                    }
                }
                else
                {
                    break;
                }
            }
        }

        private static void RunCMD(string process, params string[] args)
        {
            string allArgs = string.Empty;
            foreach (string arg in args)
            {
                allArgs += arg + " ";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = process,
                Arguments = allArgs,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            var cmd = Process.Start(startInfo);
            string errors = cmd.StandardError.ReadToEnd();
            cmd.WaitForExit();
            if (errors != string.Empty)
            {
                throw new Exception(errors);
            }
        }

        private void mnuPreferences_Click(object sender, RoutedEventArgs e)
        {
            WindowPreferences preferenceWindow = new WindowPreferences();
            preferenceWindow.Owner = this;
            preferenceWindow.ShowDialog();
        }

        private void mnuCategories_Click(object sender, RoutedEventArgs e)
        {
            WindowCategories categoryWindow = new WindowCategories();
            categoryWindow.Owner = this;
            categoryWindow.ShowDialog();
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mnuCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            Thread updateThread = new Thread(() => CheckForUpdates(false));
            updateThread.Start();
        }

        private void mnuContact_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Contact @keanine on Discord if you need help.", "Contact");
        }

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Merge Tool created by Keanine.\n" +
                "HedgeArcPack created by Radfordhound.\n" +
                "Update Tool created by Blurro.", "About");
        }
    }
}
