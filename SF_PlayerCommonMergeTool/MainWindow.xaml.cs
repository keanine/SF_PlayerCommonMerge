using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Text.Json;
using ComboBox = System.Windows.Controls.ComboBox;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Reflection;

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
        private static string devUpdateServerURL = @"https://raw.githubusercontent.com/keanine/SF_PlayerCommonMerge/main/DevUpdateServer/";
        private static string versionFileName = "version.ini";
        private static string updateListFileName = "updatelist.txt";
        private static string executableFileName = "SF_PlayerCommonMergeTool.exe";

        public ComboBox SetAllComboBox;
        public List<Category> categories = new List<Category>();
        public List<Mod> mods = new List<Mod>();

        public string workspace = "MergeTemp\\";

        string appdata = string.Empty;
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

            if (!File.Exists("noupdate.txt"))
            {
                Thread updateThread = new Thread(CheckForUpdates);
                updateThread.Start();
            }

            appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SonicFrontiersModding\\SF_PlayerCommonMerge\\";
            if (File.Exists(appdata + "\\storedData.json"))
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

        private void CheckForUpdates()
        {
            Thread.Sleep(2000);
            try
            {
                if (AutoUpdaterLib.Updater.CheckForUpdates(applicationName, updateServerURL, versionFileName))
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show("A new update has been found. Do you want to update?", "Update Found", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var proc1 = new ProcessStartInfo();
                        proc1.UseShellExecute = true;
                        proc1.CreateNoWindow = false;
                        proc1.WorkingDirectory = @"";
                        proc1.Arguments = $"\"autoupdater.dll\" \"{applicationName}\" \"{updateServerURL}\" \"{versionFileName}\" \"{updateListFileName}\" \"{executableFileName}\"";
                        proc1.FileName = "dotnet.exe";
                        Process.Start(proc1);

                        System.Environment.Exit(1);
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
            string json = File.ReadAllText(appdata + "storedData.json");
            storedData = (StoredData)JsonSerializer.Deserialize(json, typeof(StoredData));
        }

        public void SaveStoredData()
        {
            string json = JsonSerializer.Serialize(storedData);
            File.WriteAllText(appdata + "storedData.json", json);
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
                categories.Add(new Category("Open Zone Physics", "openzone", 0x73B0, 0xDE8, CategoryStackPanel)); //Includes water physics
                categories.Add(new Category("Cyberspace 3D Physics", "cyber3d", 0x81A0, 0xC40, CategoryStackPanel));
                categories.Add(new Category("Cyberspace 2D Physics", "cyber2d", 0x8DE0, 0xC40, CategoryStackPanel));
                categories.Add(new Category("Combat & Misc", "gameplay", 0x40, 0x7370, CategoryStackPanel));
                categories.Add(new Category("Parry", "parry", 0x7B54, 0x24, CategoryStackPanel));
                categories.Add(new Category("Cyloop", "cyloop", 0x5410, 0x1440, CategoryStackPanel));

                categories.Add(new Category("Spin Dash", "spinboost", 0x7E50, 0xF8, CategoryStackPanel));
                categories.Add(new Category("Spin Dash (Cyber 3D)", "spinboostcy3d", 0x8C40, 0xF8, CategoryStackPanel));
                categories.Add(new Category("Spin Dash (Cyber 2D)", "spinboostcy2d", 0x9880, 0xF8, CategoryStackPanel));

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
                }
            }
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
            foreach (var cateogry in categories)
            {
                cateogry.comboBox.Items.Add(value);
            }
        }

        private void SetAllComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var cateogry in categories)
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

                File.Copy(storedData.installLocation + "\\image\\x64\\raw\\character\\playercommon.pac", workspace + "playercommon_vanilla.pac", true);

                RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}playercommon_vanilla.pac\"", $"\"{workspace}out_vanilla\"", "-E", "-T=rangers");

                foreach (var category in categories)
                {
                    if (category.HasOffset && category.comboBox.SelectedIndex > 0)
                    {
                        Mod mod = mods[category.comboBox.SelectedIndex - 1];
                        string copyOfPac = workspace + "playercommon_" + category.id + ".pac";

                        // Update the pac file
                        UpdatePac(mod.path + "\\raw\\character\\playercommon.pac", copyOfPac);

                        RunCMD("tools/HedgeArcPack.exe", $"\"{copyOfPac}\"", $"\"{workspace}out_{category.id}\"", "-E", "-T=rangers");

                    }
                    else if (category.comboBox.SelectedIndex == 0)
                    {
                        RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}playercommon_vanilla.pac\"", $"\"{workspace}out_{category.id}\"", "-E", "-T=rangers");
                    }
                }

                CleanUpPlayerCommonUpdater();

                string rfl = $"{workspace}\\out_vanilla\\player_common.rfl";
                byte[] file = File.ReadAllBytes(rfl);

                foreach (var category in categories)
                {
                    if (category.HasOffset && category.comboBox.SelectedIndex > 0)
                    {
                        byte[] categoryFile = File.ReadAllBytes($"{workspace}\\out_{category.id}\\player_common.rfl");
                        category.data = categoryFile.ToList().GetRange(category.offset, category.size).ToArray();

                        for (int i = 0; i < category.size; i++)
                        {
                            file[i + category.offset] = category.data[i];
                        }
                    }
                }

                File.WriteAllBytes(rfl, file);

                RunCMD("tools/HedgeArcPack.exe", $"\"{workspace}out_vanilla\"", $"\"{workspace}out_vanilla.pac\"", "-P", "-T=rangers");

                File.Move($"{workspace}\\out_vanilla.pac", newPacFolder + "playercommon.pac", true);
                File.WriteAllText(modFolder + "\\mod.ini", iniTemplate);

                ClearDirectory(new DirectoryInfo(workspace));
                Directory.Delete(workspace);


                storedData.categorySelection.Clear();
                
                foreach (var category in categories)
                {
                    Mod mod = (category.comboBox.SelectedItem as Mod);

                    if (mod != null)
                    {
                        storedData.categorySelection.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                    }
                }
                SaveStoredData();

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

                        if (!Directory.Exists(appdata))
                        {
                            Directory.CreateDirectory(appdata);
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
    }
}
