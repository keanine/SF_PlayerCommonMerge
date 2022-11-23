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

namespace SF_PlayerCommonMergeTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ComboBox SetAllComboBox;
        public List<Category> categories = new List<Category>();
        public List<Mod> mods = new List<Mod>();

        public string workspace = "temp\\";

        string appdata = string.Empty;

        public StoredData storedData = new StoredData();

        public string iniTemplate =
"[Desc]\n" +
"Title=\"MergedPlayerCommon\"\n" +
"Description=\"\"\n" +
"Version=1.0\n" +
"Date=\"2022-11-18\"\n" +
"Author=\"Keanine\"\n" +
"AuthorURL=\"\"\n" +

"[Main]" +
"UpdateServer=\"\"\n" +
"SaveFile=\"\"\n" +
"ID=\"C1031F2D\"\n" +
"IncludeDir0=\".\"\n" +
"IncludeDirCount=1\n" +
"DependsCount=0\n" +
"DLLFile=\"\"\n" +
"CodeFile=\"\"\n" +
"ConfigSchemaFile=\"\"\n";

        public MainWindow()
        {
            InitializeComponent();

            appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SonicFrontiersModding\\SF_PlayerCommonMerge\\";
            if (File.Exists(appdata + "\\storedData.json"))
            {
                LoadStoredData();
                GameFolderTextbox.Text = storedData.installLocation;
                Load();
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

        private void Load()
        {
            if (storedData.installLocation != string.Empty)
            {
                CategoryStackPanel.Children.Clear();
                categories.Clear();
                mods.Clear();

                categories.Add(new Category("Set All", "set_all", CategoryStackPanel, out SetAllComboBox));
                SetAllComboBox.SelectionChanged += SetAllComboBox_SelectionChanged;

                TextBlock space = new TextBlock();
                space.Height = 10;
                CategoryStackPanel.Children.Add(space);

                categories.Add(new Category("Open Zone Physics", "openzone", 0x72E0, 0xB20, CategoryStackPanel));
                categories.Add(new Category("Cyberspace 3D Physics", "cyber3d", 0x7F80, 0xB20, CategoryStackPanel));
                categories.Add(new Category("Cyberspace 2D Physics", "cyber2d", 0x8AA0, 0xB20, CategoryStackPanel));
                categories.Add(new Category("Gameplay", "gameplay", 0x40, 0x72A0, CategoryStackPanel));
                categories.Add(new Category("Cyloop", "cyloop", 0x5250, 0x1440, CategoryStackPanel));

                string[] folders = Directory.GetDirectories(GameFolderTextbox.Text + "\\Mods\\");

                foreach (var folder in folders)
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

                foreach (var mod in mods)
                {
                    AddToComboBox(mod);
                }

                string modFolder = GameFolderTextbox.Text + "\\Mods\\MergedPlayerCommon";

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

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            if (storedData.installLocation != String.Empty)
            {
                string modFolder = GameFolderTextbox.Text + "\\Mods\\MergedPlayerCommon";
                string newPacFolder = modFolder + "\\raw\\character\\";

                if (!Directory.Exists(newPacFolder))
                {
                    Directory.CreateDirectory(newPacFolder);
                }

                if (!Directory.Exists(workspace))
                {
                    Directory.CreateDirectory(workspace);
                }

                File.Copy(GameFolderTextbox.Text + "\\image\\x64\\raw\\character\\playercommon.pac", workspace + "playercommon_vanilla.pac", true);

                string strCmdText;
                strCmdText = $"\"{workspace}playercommon_vanilla.pac\" {workspace}\\out_vanilla -E -T=rangers";
                System.Diagnostics.Process.Start("HedgeArcPackFrontiers.exe", strCmdText);

                foreach (var category in categories)
                {
                    if (category.HasOffset && category.comboBox.SelectedIndex > 0)
                    {
                        Mod mod = mods[category.comboBox.SelectedIndex - 1];
                        string copyOfPac = workspace + "playercommon_" + category.id + ".pac";
                        File.Copy(mod.path + "\\raw\\character\\playercommon.pac", copyOfPac, true);

                        strCmdText = string.Empty;
                        strCmdText = $"\"{copyOfPac}\" {workspace}\\out_{category.id} -E -T=rangers";
                        System.Diagnostics.Process.Start("HedgeArcPackFrontiers.exe", strCmdText);

                    }
                }

                Thread.Sleep(200);

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

                strCmdText = $"\"{workspace}out_vanilla\" {workspace}\\out_vanilla.pac -P -T=rangers";
                System.Diagnostics.Process.Start("HedgeArcPackFrontiers.exe", strCmdText);

                Thread.Sleep(200);

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
            }
        }

        public void ClearDirectory(DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog browserDialog = new OpenFileDialog();
            browserDialog.Filter = "EXE files (*.exe)|*.exe";

            while (true)
            {
                if (browserDialog.ShowDialog() == true)
                {
                    string folder = System.IO.Path.GetDirectoryName(browserDialog.FileName);
                    if (folder != string.Empty && Directory.Exists(folder) && File.Exists(folder + "\\image\\x64\\raw\\character\\sonic.pac"))
                    {
                        storedData.installLocation = folder;

                        GameFolderTextbox.Text = folder;
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
                        MessageBoxResult messageResult = MessageBox.Show("Please select Sonic Frontiers.exe", "Error", MessageBoxButton.OKCancel);
                        if (messageResult == MessageBoxResult.Cancel)
                        {
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
    }
}
