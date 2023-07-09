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
        public List<Category> categories = new List<Category>();
        public List<Category> addonCategories = new List<Category>();
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

            if (Directory.Exists("tools"))
            {
                foreach (var tool in requiredTools)
                {
                    if (!File.Exists(Path.Combine("tools/", tool)))
                    {
                        MessageBox.Show($"Could not find {tool} in the local tools folder");
                        Close();
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

        private void CheckForUpdates(bool wait)
        {
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
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => AddToTitle(" [up-to-date]")));
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
                categories.Clear();
                addonCategories.Clear();
                mods.Clear();

                categories.Add(new Category("Set All", "set_all", CategoryStackPanel, out SetAllComboBox));
                SetAllComboBox.SelectionChanged += SetAllComboBox_SelectionChanged;

                TextBlock space = new TextBlock();
                space.Height = 10;
                CategoryStackPanel.Children.Add(space);

                // Game Update v1.1
                //categories.Add(new Category("Open Zone Physics", "openzone", 0x72E0, 0xB20, CategoryStackPanel));
                //categories.Add(new Category("Cyberspace 3D Physics", "cyber3d", 0x7F80, 0xB20, CategoryStackPanel));
                //categories.Add(new Category("Cyberspace 2D Physics", "cyber2d", 0x8AA0, 0xB20, CategoryStackPanel));
                //categories.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x72A0, CategoryStackPanel));
                //categories.Add(new Category("Parry", "parry", 0x7Cd0, 0x24, CategoryStackPanel));
                //categories.Add(new Category("Cyloop", "cyloop", 0x5250, 0x1440, CategoryStackPanel));

                // Game Update v1.3
                categories.Add(new Category("Open Zone Physics", "openzone", 0x73B0, 0xDE8, 1, CategoryStackPanel)); //Includes water physics
                categories.Add(new Category("Cyberspace 3D Physics", "cyber3d", 0x81A0, 0xC40, 2, CategoryStackPanel));
                categories.Add(new Category("Cyberspace 2D Physics", "cyber2d", 0x8DE0, 0xC40, 3, CategoryStackPanel));
                categories.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x7370, 4, CategoryStackPanel));
                categories.Add(new Category("Cyloop", "cyloop", 0x5410, 0x1440, 5, CategoryStackPanel));


                Category categoryParry = new Category("Parry", "parry", 6, null, new CategoryChunk(0x7B54, 0x24));
                Category categorySpinDash = new Category("Spin Dash", "spindash", 7, null,
                    new CategoryChunk(0x7E50, 0xF8),
                    new CategoryChunk(0x8C40, 0xF8),
                    new CategoryChunk(0x9880, 0xF8));
                SerializeCategory(categoryParry);
                SerializeCategory(categorySpinDash);

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

                string modFolder = modsFolder + "MergedPlayerCommon";

                if (Directory.Exists(modFolder))
                {
                    if (storedData.categorySelection.Count > 0)
                    {
                        foreach (var selection in storedData.categorySelection)
                        {
                            bool success = false;
                            foreach (var category in categories)
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
                    storedData.categorySelection.Clear();
                    SaveStoredData();
                    Debugging.WriteToLog("No mods folder found");
                }
                Debugging.WriteToLog("Finished Loading");
            }
        }

        private void SerializeCategories()
        {
            foreach (var category in categories)
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
            addonCategories.Clear();
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
            addonCategories.Add(category);
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
            foreach (var category in categories)
            {
                category.comboBox.Items.Add(value);
            }
            foreach (var category in addonCategories)
            {
                category.comboBox.Items.Add(value);
            }
        }

        private void SetAllComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var cateogry in categories)
            {
                cateogry.comboBox.SelectedIndex = (sender as ComboBox).SelectedIndex;
            }
            foreach (var cateogry in addonCategories)
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
            if (storedData.installLocation != string.Empty)
            {
                List<Category> mergeCategories = new List<Category>(categories);
                mergeCategories.AddRange(addonCategories);

                Debugging.WriteToLog("Running Merge");

                string modFolder = modsFolder + "MergedPlayerCommon";
                string newPacFolder = modFolder + "\\raw\\character\\";

                if (!Directory.Exists(newPacFolder))
                {
                    Directory.CreateDirectory(newPacFolder);
                }

                if (!Directory.Exists(workspace))
                {
                    Directory.CreateDirectory(workspace);
                }

                Debugging.WriteToLog($"Copying vanilla file");

                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = File.OpenRead(storedData.installLocation + "\\image\\x64\\raw\\character\\playercommon.pac"))
                    {
                        var hash = md5.ComputeHash(stream);
                        if (Preferences.PlayercommonHash != BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant())
                        {
                            MessageBox.Show("The playercommon.pac in \"" + storedData.installLocation + "\\image\\x64\\raw\\character\\\"" + " is not for the correct version of Sonic Frontiers or has been modified. Please make sure your game is up to date with " + Preferences.NameOfGameUpdate + ". The merge operation has been cancelled.", "Error");
                            return;
                        }
                    }
                }

                File.Copy(storedData.installLocation + "\\image\\x64\\raw\\character\\playercommon.pac", workspace + "playercommon_vanilla.pac", true);

                Debugging.WriteToLog($"Extracting: \"{workspace}playercommon_vanilla.pac\" to \"{workspace}out_vanilla\"");
                RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}playercommon_vanilla.pac\"", $"\"{workspace}out_vanilla\"", "-E", "-T=rangers");

                foreach (var category in mergeCategories)
                {
                    if (category.HasOffset && category.comboBox.SelectedIndex > 0)
                    {
                        Mod mod = mods[category.comboBox.SelectedIndex - 1];
                        string copyOfPac = workspace + "playercommon_" + category.id + ".pac";

                        // Update the pac file
                        UpdatePac(mod.path + "\\raw\\character\\playercommon.pac", copyOfPac);

                        Debugging.WriteToLog($"Extracting: \"{copyOfPac}\" to \"{workspace}out_{category.id}\"");
                        RunCMD("tools/HedgeArcPack.exe", $"\"{copyOfPac}\"", $"\"{workspace}out_{category.id}\"", "-E", "-T=rangers");

                    }
                    else if (category.comboBox.SelectedIndex == 0)
                    {
                        Debugging.WriteToLog($"Extracting: \"{workspace}playercommon_vanilla.pac\" to \"{workspace}out_{category.id}\"");
                        RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}playercommon_vanilla.pac\"", $"\"{workspace}out_{category.id}\"", "-E", "-T=rangers");
                    }
                }
                Debugging.WriteToLog($"Extracted all category files");

                CleanUpPlayerCommonUpdater();

                Debugging.WriteToLog($"Read vanilla RFL");
                string rfl = $"{workspace}\\out_vanilla\\player_common.rfl";
                byte[] file = File.ReadAllBytes(rfl);
                Debugging.WriteToLog($"Read vanilla RFL successfully");

                foreach (var category in mergeCategories)
                {
                    if (category.HasOffset && category.comboBox.SelectedIndex >= 0)
                    {
                        Debugging.WriteToLog($"Merging bytes from {category.id} RFL");
                        byte[] categoryFile = File.ReadAllBytes($"{workspace}\\out_{category.id}\\player_common.rfl");

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

                File.Move($"{workspace}\\out_vanilla.pac", newPacFolder + "playercommon.pac", true);
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
                        storedData.categorySelection.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                    }
                }
                SaveStoredData();

                Debugging.WriteToLog($"Merge Successful!");
                MessageBox.Show("You can now close this tool, open Hedge Mod Manager and enable the new mod MergedPlayerCommon", "Merge complete!");
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
            MessageBox.Show("Merge Tool created by Keanine. HedgeArcPack created by Radfordhound. Update Tool created by Blurro", "About");
        }
    }
}
