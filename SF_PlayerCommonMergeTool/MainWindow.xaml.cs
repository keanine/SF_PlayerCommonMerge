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
using System.Windows.Input;
using static SF_PlayerCommonMergeTool.MainWindow;

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

        public class Character
        {
            public string name;
            public ComboBox SetAllComboBox;
            public StackPanel stackPanel;
            public List<Category> categories = new List<Category>();
            public List<Category> addonCategories = new List<Category>();

            public Character()
            {

            }

            public void InitSetAllComboBox(string id)
            {
                Category category = new Category("Set All", id, name);
                categories.Add(category);

                SetAllComboBox = category.InitComboBox(stackPanel);
                SetAllComboBox.SelectionChanged += SetAllComboBox_SelectionChanged;

                TextBlock space = new TextBlock();
                space.Height = 10;
                stackPanel.Children.Add(space);
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

            public void AddCategory(string name, string id, int offset, int size, int order)
            {
                Category category = new Category(name, id, offset, size, order, this.name);
                category.InitComboBox(stackPanel);
                categories.Add(category);
            }
        }

        public Dictionary<string, Character> characters = new Dictionary<string, Character>()
        {
            { "Sonic", new Character() },
            { "Amy", new Character() },
            { "Knuckles", new Character() },
            { "Tails", new Character() }
        };

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

            foreach (var key in characters.Keys)
            {
                characters[key].name = key;
            }
            characters["Sonic"].stackPanel = CategoryStackPanel;
            characters["Tails"].stackPanel = TailsCategoryStackPanel;
            characters["Knuckles"].stackPanel = KnucklesCategoryStackPanel;
            characters["Amy"].stackPanel = AmyCategoryStackPanel;


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

        private void CreateBuiltinAddons()
        {
            Character character = characters["Sonic"];


            // Sonic
            Category sonicParry = new Category("Parry", "sonic_parry", 7, character.name,
                new CategoryChunk(0x89D8, 0x60, "Parry"));
            SerializeCategory(sonicParry);

            Category sonicSpinDash = new Category("Spin Dash", "sonic_spindash", 8, character.name,
                new CategoryChunk(0x8D10, 0xF8, "Open Zone 3D"),
                new CategoryChunk(0x9DA0, 0xF8, "Cyberspace 3D"),
                new CategoryChunk(0xAC80, 0xF8, "Cyberspace 2D"));
            SerializeCategory(sonicSpinDash);


            // Tails
            character = characters["Tails"];

            Category tailsParry = new Category("Parry", "tails_parry", 6, character.name,
                new CategoryChunk(0x6AF8, 0x60, "Parry"),
                new CategoryChunk(0x4E90, 0x8, "Parry Debuff"));
            SerializeCategory(tailsParry);

            Category tailsSpinDash = new Category("Spin Dash", "tails_spindash", 7, character.name,
                new CategoryChunk(0x6E58, 0xF8, "3D Gameplay"),
                new CategoryChunk(0x7CB8, 0xF8, "2D Gameplay"));
            SerializeCategory(tailsSpinDash);


            // Knuckles
            character = characters["Knuckles"];

            Category knucklesParry = new Category("Parry", "knuckles_parry", 6, character.name,
                new CategoryChunk(0x6BD8, 0x60, "Parry"),
                new CategoryChunk(0x4F70, 0x8, "Parry Debuff"));
            SerializeCategory(knucklesParry);

            Category knucklesSpinDash = new Category("Spin Dash", "knuckles_spindash", 7, character.name,
                new CategoryChunk(0x6EF8, 0xF8, "3D Gameplay"),
                new CategoryChunk(0x7CB8, 0xF8, "2D Gameplay"));
            SerializeCategory(knucklesSpinDash);


            // Amy
            character = characters["Amy"];

            Category amyParry = new Category("Parry", "amy_parry", 6, character.name,
                new CategoryChunk(0x6AF8, 0x60, "Parry"),
                new CategoryChunk(0x4E90, 0x8, "Parry Debuff"));
            SerializeCategory(amyParry);

            Category amySpinDash = new Category("Spin Dash", "amy_spindash", 7, character.name,
                new CategoryChunk(0x6F38, 0xF8, "3D Gameplay"),
                new CategoryChunk(0x7E18, 0xF8, "2D Gameplay"));
            SerializeCategory(amySpinDash);
        }

        private void Load(bool isReload = false)
        {
            if (storedData.installLocation != string.Empty && Directory.Exists(storedData.installLocation))
            {
                foreach (Character character in characters.Values)
                {
                    character.stackPanel.Children.Clear();
                    character.categories.Clear();

                    if (!isReload)
                        character.addonCategories.Clear();
                }
                mods.Clear();

                characters["Sonic"].InitSetAllComboBox("set_all");
                characters["Tails"].InitSetAllComboBox("set_all_tails");
                characters["Knuckles"].InitSetAllComboBox("set_all_knuckles");
                characters["Amy"].InitSetAllComboBox("set_all_amy");

                // Game Update v1.41 ===================================================================================================================

                Character selectedCharacter;

                // Sonic //
                selectedCharacter = characters["Sonic"];
                selectedCharacter.AddCategory("Open Zone 3D Physics", "openzone", 0x81E0, 0xEE0, 1);
                selectedCharacter.AddCategory("Cyberspace 3D Physics", "cyber3d", 0x9270, 0xEE0, 2);
                selectedCharacter.AddCategory("2D Physics", "cyber2d", 0xA150, 0xEE0, 3);
                selectedCharacter.AddCategory("Water Physics", "water", 0x90C0, 0x1A8, 4);
                selectedCharacter.AddCategory("Combat & Misc", "gameplay", 0x40, 0x81A0, 5);
                selectedCharacter.AddCategory("Cyloop", "cyloop", 0x61F8, 0x1460, 6);

                // Tails //
                selectedCharacter = characters["Tails"];
                selectedCharacter.AddCategory("3D Physics", "openzone", 0x6300, 0xCB0, 1);
                selectedCharacter.AddCategory("2D Physics", "cyber2d", 0x7160, 0xCB0, 2);
                selectedCharacter.AddCategory("Water Physics", "water", 0x6FB0, 0x1A8, 3);
                selectedCharacter.AddCategory("Combat & Misc", "gameplay", 0x40, 0x62C0, 4);
                selectedCharacter.AddCategory("Cyloop", "cyloop", 0x4E98, 0x1460, 5);

                // Knuckles //
                selectedCharacter = characters["Knuckles"];
                selectedCharacter.AddCategory("3D Physics", "openzone", 0x63E0, 0xC10, 1);
                selectedCharacter.AddCategory("2D Physics", "cyber2d", 0x71A0, 0xC10, 2);
                selectedCharacter.AddCategory("Water Physics", "water", 0x6FF0, 0x1A8, 3);
                selectedCharacter.AddCategory("Combat & Misc", "gameplay", 0x40, 0x63A0, 4);
                selectedCharacter.AddCategory("Cyloop", "cyloop", 0x4F78, 0x1460, 5);

                // Amy //
                selectedCharacter = characters["Amy"];
                selectedCharacter.AddCategory("3D Physics", "openzone", 0x6300, 0xD30, 1);
                selectedCharacter.AddCategory("2D Physics", "cyber2d", 0x71E0, 0xD30, 2);
                selectedCharacter.AddCategory("Water Physics", "water", 0x7030, 0x1A8, 3);
                selectedCharacter.AddCategory("Combat & Misc", "gameplay", 0x40, 0x62C0, 4);
                selectedCharacter.AddCategory("Cyloop", "cyloop", 0x4EA0, 0x1460, 5);

                CreateBuiltinAddons();

                if (isReload)
                {
                    foreach (var character in characters.Values)
                    {
                        foreach (var addon in character.addonCategories)
                        {
                            addon.InitComboBox(character.stackPanel);
                        }
                    }
                }
                else
                {
                    LoadSelectedAddonCategoriesFromDirectory();
                }
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

                LoadDropdownDefaults("Sonic", storedData.categorySelection, characters["Sonic"].categories);
                LoadDropdownDefaults("Tails", storedData.categorySelectionTails, characters["Tails"].categories);
                LoadDropdownDefaults("Knuckles", storedData.categorySelectionKnuckles, characters["Knuckles"].categories);
                LoadDropdownDefaults("Amy", storedData.categorySelectionAmy, characters["Amy"].categories);
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
            foreach (var character in characters.Values)
            {
                foreach (var category in character.categories)
                {
                    SerializeCategory(category);
                }
            }
        }
        private void SerializeCategory(Category category)
        {
            string directory = Path.Combine(Preferences.appData, "categories", category.character);
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

        private void LoadAllAddonCategoriesFromDirectory()
        {
            foreach (var character in characters.Values)
            {
                character.addonCategories.Clear();
                string directory = Path.Combine(Preferences.appData, "categories", character.name);
                foreach (var file in Directory.GetFiles(directory))
                {
                    LoadCategoryFromFile(file, character);
                }
            }
        }
        private void LoadSelectedAddonCategoriesFromDirectory()
        {
            foreach (var character in characters.Values)
            {
                character.addonCategories.Clear();
                string directory = Path.Combine(Preferences.appData, "categories", character.name);
                foreach (var file in Directory.GetFiles(directory))
                {
                    //LoadCategoryFromFile(file, character);
                }
            }
        }
        private void LoadCategoryFromFile(string fileName, Character character)
        {
            string jsonCategory = File.ReadAllText(fileName);
            Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));
            category = new Category(category.name, category.id, category.order, character.name, category.chunks);
            category.InitComboBox(character.stackPanel);
            category.DeserializeAllChunkValues();
            character.addonCategories.Add(category);
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
            foreach (var character in characters.Values)
            {
                foreach (var category in character.categories)
                {
                    category.comboBox.Items.Add(value);
                }
                foreach (var category in character.addonCategories)
                {
                    category.comboBox.Items.Add(value);
                }
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
            Merge(characters["Sonic"], "player_common");
        }

        private void MergeButtonTails_Click(object sender, RoutedEventArgs e)
        {
            Merge(characters["Tails"], "tails_common");
        }

        private void MergeButtonKnuckles_Click(object sender, RoutedEventArgs e)
        {
            Merge(characters["Knuckles"], "knuckles_common");
        }

        private void MergeButtonAmy_Click(object sender, RoutedEventArgs e)
        {
            Merge(characters["Amy"], "amy_common");
        }

        private void Merge(Character character, string playerCommonRFL)
        {
            if (storedData.installLocation != string.Empty)
            {
                string pacFile = "playercommon";

                List<Category> mergeCategories = new List<Category>(character.categories);
                mergeCategories.AddRange(character.addonCategories);

                Debugging.WriteToLog("Running Merge for " + character.name);

                string modFolder = modsFolder + "Merged" + character.name;
                string newPacFolder = modFolder + "\\raw\\character\\";

                if (!Directory.Exists(newPacFolder))
                {
                    Directory.CreateDirectory(newPacFolder);
                }

                if (!Directory.Exists(workspace))
                {
                    Directory.CreateDirectory(workspace);
                }

                Debugging.WriteToLog($"Copying vanilla {character.name} file");

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
                            byte[] data = categoryFile.ToList().GetRange(chunk.offsetValue, chunk.sizeValue).ToArray();

                            for (int i = 0; i < chunk.sizeValue; i++)
                            {
                                file[i + chunk.offsetValue] = data[i];
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
                        switch (character.name)
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
                MessageBox.Show($"You can now close this tool, open Hedge Mod Manager and enable the newly generated mod \"Merged {character.name}\"", "Merge complete!");
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

        private void RefreshStackPanels()
        {
            foreach (Character character in characters.Values)
            {
                character.stackPanel.Children.Clear();
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

            Load(true);
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
