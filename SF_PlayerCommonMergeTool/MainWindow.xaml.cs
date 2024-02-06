using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Text.Json;
using System.Diagnostics;

using ComboBox = System.Windows.Controls.ComboBox;
using Path = System.IO.Path;
using System.Windows.Input;
using static SF_PlayerCommonMergeTool.MainWindow;
using System.Security.Policy;

namespace SF_PlayerCommonMergeTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string titleName = "MergeTool";
        private static string applicationName = "PlayerCommonMergeTool";
        private static string updateServerURL = @"https://raw.githubusercontent.com/keanine/SF_PlayerCommonMerge/main/UpdateServer/";
        private static string internalUpdateServerURL = @"https://raw.githubusercontent.com/keanine/SF_PlayerCommonMerge/main/InternalUpdateServer/";
        private static string devUpdateServerURL = @"https://raw.githubusercontent.com/keanine/SF_PlayerCommonMerge/development/DevUpdateServer/";
        private static string versionFileName = "version.ini";
        private static string updateListFileName = "updatelist.txt";
        private static string executableFileName = "SF_PlayerCommonMergeTool.exe";

        public enum Characters { Sonic, Tails, Knuckles, Amy }

        public Dictionary<Characters, Character> characters = new Dictionary<Characters, Character>()
        {
            { Characters.Sonic, new Character() },
            { Characters.Tails, new Character() },
            { Characters.Knuckles, new Character() },
            { Characters.Amy, new Character() }
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
            "HedgeArcPack.exe"
            //"PlayerCommonUpdaterV2.exe"
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

        private void InitCharacters()
        {
            foreach (var key in characters.Keys)
            {
                characters[key].name = Enum.GetName(typeof(Characters), key);
            }

            characters[Characters.Sonic].Init(CategoryStackPanel, "player_common.rfl");
            characters[Characters.Tails].Init(TailsCategoryStackPanel, "tails_common.rfl");
            characters[Characters.Knuckles].Init(KnucklesCategoryStackPanel, "knuckles_common.rfl");
            characters[Characters.Amy].Init(AmyCategoryStackPanel, "amy_common.rfl");
        }

        public MainWindow()
        {
            InitializeComponent();

            Preferences.appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SonicFrontiersModding\\SF_PlayerCommonMerge\\";
            Preferences.Initialize();
            Preferences.AllowUpdatingPac = false;

            InitCharacters();
            SetTitleUpdateMessage("");

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
                SetTitleUpdateMessage("updates disabled");
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

        private void SetTitleUpdateMessage(string message)
        {
            //Window.GetWindow(this).Title = titleName + " v" + Preferences.ToolVersion.ToString("0.0") + " | Frontiers v1.41 | " + message;
            Window.GetWindow(this).Title = $"{titleName} v{Preferences.ToolVersion.ToString("0.0")} | Frontiers v1.41 | {message}";
        }

        private void CheckForUpdates(bool wait)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitleUpdateMessage("checking for updates")));

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
                    else
                    {
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitleUpdateMessage("update available")));
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetTitleUpdateMessage("up-to-date")));
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
            string json = JsonSerializer.Serialize(storedData, new JsonSerializerOptions { WriteIndented = true });
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

        private void CreateBuiltinAddonFiles()
        {
            // Sonic
            Character character = characters[Characters.Sonic];

            Category sonicCyloop = new Category("Cyloop", "sonic_cyloop", 6, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x61F8, 0x1460, "Cyloop Effect"));
            SerializeCategory(sonicCyloop);

            Category sonicParry = new Category("Parry", "sonic_parry", 7, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x89D8, 0x60, "Parry"));
            SerializeCategory(sonicParry);

            Category sonicSpinDash = new Category("Spin Dash", "sonic_spindash", 8, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x8D10, 0xF8, "Open Zone 3D"),
                new CategoryChunk(0x9DA0, 0xF8, "Cyberspace 3D"),
                new CategoryChunk(0xAC80, 0xF8, "Cyberspace 2D"));
            SerializeCategory(sonicSpinDash);


            // Tails
            character = characters[Characters.Tails];

            Category tailsCyloop = new Category("Cyloop", "tails_cyloop", 5, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x4E98, 0x1460, "Cyloop Effect"));
            SerializeCategory(tailsCyloop);

            Category tailsParry = new Category("Parry", "tails_parry", 6, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x6AF8, 0x60, "Parry"),
                new CategoryChunk(0x4E90, 0x8, "Parry Debuff"));
            SerializeCategory(tailsParry);

            Category tailsSpinDash = new Category("Spin Dash", "tails_spindash", 7, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x6E58, 0xF8, "3D Gameplay"),
                new CategoryChunk(0x7CB8, 0xF8, "2D Gameplay"));
            SerializeCategory(tailsSpinDash);


            // Knuckles
            character = characters[Characters.Knuckles];

            Category knucklesCyloop = new Category("Cyloop", "knuckles_cyloop", 5, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x4F78, 0x1460, "Cyloop Effect"));
            SerializeCategory(knucklesCyloop);

            Category knucklesParry = new Category("Parry", "knuckles_parry", 6, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x6BD8, 0x60, "Parry"),
                new CategoryChunk(0x4F70, 0x8, "Parry Debuff"));
            SerializeCategory(knucklesParry);

            Category knucklesSpinDash = new Category("Spin Dash", "knuckles_spindash", 7, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x6EF8, 0xF8, "3D Gameplay"),
                new CategoryChunk(0x7CB8, 0xF8, "2D Gameplay"));
            SerializeCategory(knucklesSpinDash);


            // Amy
            character = characters[Characters.Amy];

            Category amyCyloop = new Category("Cyloop", "amy_cyloop", 5, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x4EA0, 0x1460, "Cyloop Effect"));
            SerializeCategory(amyCyloop);

            Category amyParry = new Category("Parry", "amy_parry", 6, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x6AF8, 0x60, "Parry"),
                new CategoryChunk(0x4E90, 0x8, "Parry Debuff"));
            SerializeCategory(amyParry);

            Category amySpinDash = new Category("Spin Dash", "amy_spindash", 7, character.name, Preferences.AddonFormat,
                new CategoryChunk(0x6F38, 0xF8, "3D Gameplay"),
                new CategoryChunk(0x7E18, 0xF8, "2D Gameplay"));
            SerializeCategory(amySpinDash);
        }

        private void Load(bool isReload = false)
        {
            if (storedData.installLocation != string.Empty && Directory.Exists(storedData.installLocation))
            {
                ExtractVanillaPac();

                foreach (Character character in characters.Values)
                {
                    character.stackPanel.Children.Clear();
                    character.categories.Clear();

                    if (!isReload)
                        character.addonCategories.Clear();
                }
                mods.Clear();

                characters[Characters.Sonic].InitSetAllComboBox("set_all");
                characters[Characters.Tails].InitSetAllComboBox("set_all_tails");
                characters[Characters.Knuckles].InitSetAllComboBox("set_all_knuckles");
                characters[Characters.Amy].InitSetAllComboBox("set_all_amy");

                // Initialize built in categories
                Character selectedCharacter;

                // Sonic //
                selectedCharacter = characters[Characters.Sonic];
                selectedCharacter.AddCategory("Open Zone 3D Physics", "sonic_openzone", 0x81E0, 0xEE0, 1, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Cyberspace 3D Physics", "sonic_cyber3d", 0x9270, 0xEE0, 2, Preferences.AddonFormat);
                selectedCharacter.AddCategory("2D Physics", "sonic_cyber2d", 0xA150, 0xEE0, 3, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Water Physics", "sonic_water", 0x90C0, 0x1A8, 4, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Combat & Misc", "sonic_gameplay", 0x40, 0x81A0, 5, Preferences.AddonFormat);

                // Tails //
                selectedCharacter = characters[Characters.Tails];
                selectedCharacter.AddCategory("3D Physics", "tails_openzone", 0x6300, 0xCB0, 1, Preferences.AddonFormat);
                selectedCharacter.AddCategory("2D Physics", "tails_cyber2d", 0x7160, 0xCB0, 2, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Water Physics", "tails_water", 0x6FB0, 0x1A8, 3, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Combat & Misc", "tails_gameplay", 0x40, 0x62C0, 4, Preferences.AddonFormat);

                // Knuckles //
                selectedCharacter = characters[Characters.Knuckles];
                selectedCharacter.AddCategory("3D Physics", "knuckles_openzone", 0x63E0, 0xC10, 1, Preferences.AddonFormat);
                selectedCharacter.AddCategory("2D Physics", "knuckles_cyber2d", 0x71A0, 0xC10, 2, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Water Physics", "knuckles_water", 0x6FF0, 0x1A8, 3, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Combat & Misc", "knuckles_gameplay", 0x40, 0x63A0, 4, Preferences.AddonFormat);

                // Amy //
                selectedCharacter = characters[Characters.Amy];
                selectedCharacter.AddCategory("3D Physics", "amy_openzone", 0x6300, 0xD30, 1, Preferences.AddonFormat);
                selectedCharacter.AddCategory("2D Physics", "amy_cyber2d", 0x71E0, 0xD30, 2, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Water Physics", "amy_water", 0x7030, 0x1A8, 3, Preferences.AddonFormat);
                selectedCharacter.AddCategory("Combat & Misc", "amy_gameplay", 0x40, 0x62C0, 4, Preferences.AddonFormat);

                CreateBuiltinAddonFiles();

                // Load addon categories
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

                // Add all mods to the mod list
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

                // Set each dropdown to your previously saved value, or Vanilla by default
                LoadDropdownDefaults(characters[Characters.Sonic], storedData.categorySelectionsSonic);
                LoadDropdownDefaults(characters[Characters.Tails], storedData.categorySelectionsTails);
                LoadDropdownDefaults(characters[Characters.Knuckles], storedData.categorySelectionsKnuckles);
                LoadDropdownDefaults(characters[Characters.Amy], storedData.categorySelectionsAmy);
                Debugging.WriteToLog("Finished Loading");
            }
        }

        private void LoadDropdownDefaults(Character character, List<StoredData.CategorySelection> categorySelection)
        {
            if (categorySelection.Count > 0)
            {
                List<Category> mergedCategories = new List<Category>();
                mergedCategories.AddRange(character.categories);
                mergedCategories.AddRange(character.addonCategories);

                foreach (var selection in categorySelection)
                {
                    bool success = false;
                    foreach (var category in mergedCategories)
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
                    LoadAddonCategoryFromFile(file, character);
                }
            }
        }
        private void LoadCategoryFromFile(string fileName, Character character)
        {
            string jsonCategory = File.ReadAllText(fileName);
            Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));
            category.InitComboBox(character.stackPanel);
            category.DeserializeAllChunkValues();
            character.addonCategories.Add(category);
        }
        private void LoadAddonCategoryFromFile(string fileName, Character character)
        {
            string jsonCategory = File.ReadAllText(fileName);
            Category category = (Category)JsonSerializer.Deserialize(jsonCategory, typeof(Category));

            List<StoredData.CategorySelection> addonSelections;
            
            switch (character.name)
            {
                case "Tails":
                    addonSelections = storedData.addonCategorySelectionsTails;
                    break;
                case "Knuckles":
                    addonSelections = storedData.addonCategorySelectionsKnuckles;
                    break;
                case "Amy":
                    addonSelections = storedData.addonCategorySelectionsAmy;
                    break;
                case "Sonic":
                default:
                    addonSelections = storedData.addonCategorySelectionsSonic;
                    break;
            }

            foreach(var selection in addonSelections)
            {
                if (selection.id == category.id)
                {
                    category.InitComboBox(character.stackPanel);
                    category.DeserializeAllChunkValues();
                    character.addonCategories.Add(category);
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
            NewMerge(characters[Characters.Sonic]);
        }

        private void MergeButtonTails_Click(object sender, RoutedEventArgs e)
        {
            NewMerge(characters[Characters.Tails]);
        }

        private void MergeButtonKnuckles_Click(object sender, RoutedEventArgs e)
        {
            NewMerge(characters[Characters.Knuckles]);
        }

        private void MergeButtonAmy_Click(object sender, RoutedEventArgs e)
        {
            NewMerge(characters[Characters.Amy]);
        }

        private string GetMD5(string pac)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(pac))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
        }
        private bool CompareMD5(string pac1, string pac2)
        {
            return GetMD5(pac1) == GetMD5(pac2);
        }
        private bool CheckMD5(string pac)
        {
            return GetMD5(pac) == Preferences.LatestPlayercommonHash;
        }


        string[] characterRFLs =
        {
            "player_common.rfl",
            "tails_common.rfl",
            "knuckles_common.rfl",
            "amy_common.rfl"
        };
        string[] otherPacFileEntries =
        {
            "!DEPENDENCIES.txt",
            "effect_database.rfl",
            "parry_area.rfl",
            "player.vib",
            "player_camera.rfl",
            "playercommon.level"
        };

        private void ExtractVanillaPac()
        {
            if (storedData.installLocation != string.Empty)
            {
                string vanillaDirectory = Path.Combine(Preferences.appData, "vanilla_files");
                string moddedDirectory = Path.Combine(Preferences.appData, "modded_files");
                string pacPath = $"{vanillaDirectory}/playercommon_vanilla.pac";
                Directory.CreateDirectory(vanillaDirectory);
                Directory.CreateDirectory(moddedDirectory);

                bool needsExtraction = false;

                if (!CheckMD5(storedData.installLocation + "\\image\\x64\\raw\\character\\playercommon.pac"))
                {
                    throw new System.Exception($"The playercommon.pac in \"" + storedData.installLocation + "\\image\\x64\\raw\\character\\\"" + " is not for the correct version of Sonic Frontiers or has been modified. Please make sure your game is up to date with v" + Preferences.NameOfGameUpdate + ".");
                }

                if (!File.Exists(pacPath) || !CompareMD5(pacPath, storedData.installLocation + "\\image\\x64\\raw\\character\\playercommon.pac"))
                {
                    needsExtraction = true;
                }

                if (!needsExtraction)
                {
                    foreach (var file in characterRFLs)
                    {
                        if (!File.Exists(Path.Combine(vanillaDirectory, file)))
                        {
                            needsExtraction = true;
                            break;
                        }
                        if (!File.Exists(Path.Combine(moddedDirectory, file)))
                        {
                            needsExtraction = true;
                            break;
                        }
                    }
                }

                if (!needsExtraction)
                {
                    // If all the files already exist, then skip this function
                    return;
                }

                Debugging.WriteToLog($"Copying vanilla playercommon.pac file");
                File.Copy(storedData.installLocation + $"\\image\\x64\\raw\\character\\playercommon.pac", pacPath, true);

                Debugging.WriteToLog($"Extracting vanilla file to {vanillaDirectory}");
                RunCMD("tools/HedgeArcPack.exe", pacPath, $"\"{vanillaDirectory}\"", "-E", "-T=rangers");

                foreach (var file in characterRFLs)
                {
                    if (!File.Exists(Path.Combine(moddedDirectory, file)))
                    {
                        File.Copy(Path.Combine(vanillaDirectory, file), Path.Combine(moddedDirectory, file));
                    }
                }

                foreach (var file in otherPacFileEntries)
                {
                    File.Move(Path.Combine(vanillaDirectory, file), Path.Combine(moddedDirectory, file));
                }

                Debugging.WriteToLog($"Extracting and cleanup complete");
            }
        }

        private void NewMerge(Character character)
        {
            // Get the vanilla file for the current character, and the modded files for the other characters
            string vanillaDirectory = Path.Combine(Preferences.appData, "vanilla_files");
            string moddedDirectory = Path.Combine(Preferences.appData, "modded_files");

            string characterRFL = character.rflName;
            List<string> otherRFLs = new List<string>();
            otherRFLs.AddRange(characterRFLs);
            otherRFLs.Remove(characterRFL);

            // Combine the categories and addons into mergedCategories
            List<Category> mergedCategories = new List<Category>();
            mergedCategories.AddRange(character.categories);
            mergedCategories.AddRange(character.addonCategories);

            Debugging.WriteToLog("Running Merge for " + character.name);

            // Create the folders for the mod
            string modName = "MergeTool Mod";
            string modFolder = modsFolder + modName;
            string newPacFolder = modFolder + "\\raw\\character\\";

            if (!Directory.Exists(newPacFolder))
                Directory.CreateDirectory(newPacFolder);
            if (!Directory.Exists(workspace))
                Directory.CreateDirectory(workspace);

            ExtractPacsToWorkspace(mergedCategories);

            Debugging.WriteToLog($"Read vanilla RFL");
            string vanillaRFL = Path.Combine(vanillaDirectory, characterRFL);
            byte[] fileBytes = File.ReadAllBytes(vanillaRFL);
            Debugging.WriteToLog($"Read vanilla RFL successfully");

            foreach (var category in mergedCategories)
            {
                if (category.comboBox.SelectedIndex >= 0)
                {
                    string debuggingFile = $"{workspace}\\out_{category.id}\\{characterRFL}";
                    bool exists = File.Exists(debuggingFile);
                    if (category.HasOffset && File.Exists($"{workspace}\\out_{category.id}\\{characterRFL}"))
                    {
                        Debugging.WriteToLog($"Merging bytes from {category.id} RFL");
                        byte[] categoryFile = File.ReadAllBytes($"{workspace}\\out_{category.id}\\{characterRFL}");

                        foreach (var chunk in category.chunks)
                        {
                            byte[] data = categoryFile.ToList().GetRange(chunk.offsetValue, chunk.sizeValue).ToArray();

                            for (int i = 0; i < chunk.sizeValue; i++)
                            {
                                fileBytes[i + chunk.offsetValue] = data[i];
                            }
                        }

                        Debugging.WriteToLog($"Successfully merged bytes from {category.id} {characterRFL}");
                    }

                }
                else
                {
                    Debugging.WriteToLog($"ComboBox for {category.id} was set to Vanilla so nothing was adjusted");
                }
            }

            Debugging.WriteToLog($"Writing all merged bytes to the modded {characterRFL}");
            File.WriteAllBytes(Path.Combine(moddedDirectory, characterRFL), fileBytes);

            Debugging.WriteToLog($"Packing the new pac file");
            RunCMD("tools/HedgeArcPack.exe", $"\"{moddedDirectory}\"", $"\"{newPacFolder}playercommon.pac\"", "-P", "-T=rangers");
            File.WriteAllText(modFolder + "\\mod.ini", iniTemplate);

            Debugging.WriteToLog($"Cleaning Workspace");
            ClearDirectory(new DirectoryInfo(workspace));
            Directory.Delete(workspace);

            switch (character.name)
            {
                case "Sonic":
                    storedData.categorySelectionsSonic.Clear();
                    break;
                case "Tails":
                    storedData.categorySelectionsTails.Clear();
                    break;
                case "Knuckles":
                    storedData.categorySelectionsKnuckles.Clear();
                    break;
                case "Amy":
                    storedData.categorySelectionsAmy.Clear();
                    break;
            }

            foreach (var category in mergedCategories)
            {
                Mod mod = (category.comboBox.SelectedItem as Mod);

                if (mod != null)
                {
                    switch (character.name)
                    {
                        case "Sonic":
                            storedData.categorySelectionsSonic.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                            break;
                        case "Tails":
                            storedData.categorySelectionsTails.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                            break;
                        case "Knuckles":
                            storedData.categorySelectionsKnuckles.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                            break;
                        case "Amy":
                            storedData.categorySelectionsAmy.Add(new StoredData.CategorySelection(category.id, (category.comboBox.SelectedItem as Mod).title));
                            break;
                    }
                }
            }
            SaveStoredData();

            Debugging.WriteToLog($"Merge Successful!");
            MessageBox.Show($"You can now close this tool, open Hedge Mod Manager and enable the newly generated mod \"{modName}\"", "Merge complete!");
        }

        public void SaveAddonCategorySelections()
        {
            foreach (var character in characters.Values)
            {
                switch (character.name)
                {
                    case "Sonic":
                        storedData.addonCategorySelectionsSonic.Clear();
                        break;
                    case "Tails":
                        storedData.addonCategorySelectionsTails.Clear();
                        break;
                    case "Knuckles":
                        storedData.addonCategorySelectionsKnuckles.Clear();
                        break;
                    case "Amy":
                        storedData.addonCategorySelectionsAmy.Clear();
                        break;
                }

                foreach (var category in character.addonCategories)
                {
                    switch (character.name)
                    {
                        case "Sonic":
                            storedData.addonCategorySelectionsSonic.Add(new StoredData.CategorySelection(category.id, ""));
                            break;
                        case "Tails":
                            storedData.addonCategorySelectionsTails.Add(new StoredData.CategorySelection(category.id, ""));
                            break;
                        case "Knuckles":
                            storedData.addonCategorySelectionsKnuckles.Add(new StoredData.CategorySelection(category.id, ""));
                            break;
                        case "Amy":
                            storedData.addonCategorySelectionsAmy.Add(new StoredData.CategorySelection(category.id, ""));
                            break;
                    }
                }
            }

            SaveStoredData();
        }

        private void ExtractPacsToWorkspace(List<Category> categories)
        {
            foreach (var category in categories)
            {
                if (category.HasOffset && category.comboBox.SelectedIndex > 0)
                {
                    Mod mod = mods[category.comboBox.SelectedIndex - 1];
                    string modPac = mod.path + "\\raw\\character\\playercommon.pac";

                    Debugging.WriteToLog($"Extracting: \"{modPac}\" to \"{workspace}out_{category.id}\"");
                    RunCMD("tools/HedgeArcPack.exe", $"\"{modPac}\"", $"\"{workspace}out_{category.id}\"", "-E", "-T=rangers");

                }
                else if (category.comboBox.SelectedIndex == 0)
                {
                    //Just skip this because we're using vanilla as a base anyway
                }
            }
            Debugging.WriteToLog($"Extracted all category files");
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

            if (categoryWindow.ShowDialog() == true)
            {
                Load(true);
            }

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
