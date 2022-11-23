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

        public string iniTemplate =
"[Desc]\n" +
"Title=\"Merged playercommon\"\n" +
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
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
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

            //AddToComboBox("Unmodded");
            //SetComboBoxIndex(0);

            string[] folders = Directory.GetDirectories(GameFolderTextbox.Text + "\\Mods\\");

            foreach (var folder in folders)
            {
                string pacFile = folder + "/raw/character/playercommon.pac";
                if (File.Exists(pacFile))
                {
                    mods.Add(new Mod(folder));
                }
            }

            foreach (var mod in mods)
            {
                AddToComboBox(mod);
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
            //SetAllComboBox.Items.Add(value);

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
            string modFolder = GameFolderTextbox.Text + "Mods\\Merged playercommon";
            string newPacFolder = modFolder + "\\raw\\character\\";

            // Create a folder for the new merged mod in the Mods folder
            if (!Directory.Exists(newPacFolder))
            {
                Directory.CreateDirectory(newPacFolder);
            }

            if (!Directory.Exists(workspace))
            {
                Directory.CreateDirectory(workspace);
            }

            File.Copy(GameFolderTextbox.Text + "image\\x64\\raw\\character\\playercommon.pac", workspace + "playercommon_vanilla.pac", true);

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

            // Create a copy of the vanilla file and put it in the merged mod folder
            // foreach category
            // extract the required mod
            // take binary from the offsets and paste it into the vanilla copy
            // Repack the copy of the vanilla file


        }

        public void ClearDirectory(System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }
    }

    public class Category
    {
        public string name;
        public string id;
        public int offset;
        public int size;
        public ComboBox comboBox;
        public byte[] data;

        public bool HasOffset { get; private set; }

        public Category(string name, string id, StackPanel parent, out ComboBox comboBox)
        {
            this.name = name;
            comboBox = InitComboBox(parent);
            this.comboBox = comboBox;
        }

        public Category(string name, string id, int offset, int size, StackPanel parent)
        {
            this.name = name;
            this.offset = offset;
            this.size = size;
            this.id = id;
            HasOffset = true;

            comboBox = InitComboBox(parent);
        }

        public ComboBox InitComboBox(StackPanel parent)
        {

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.Margin = new Thickness(0, 0, 0, 5);

            TextBlock label = new TextBlock();
            label.Text = name;
            label.Width = 160;
            label.Height = 22;
            label.Margin = new Thickness(0, 0, 5, 0);
            label.TextAlignment = TextAlignment.Right;

            comboBox = new ComboBox();
            comboBox.Width = 240;
            comboBox.Height = 22;
            comboBox.Items.Add("Unmodded");
            comboBox.SelectedIndex = 0;

            panel.Children.Add(label);
            panel.Children.Add(comboBox);

            parent.Children.Add(panel);

            return comboBox;
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class Mod
    {
        public enum ActiveSection { None, Physics, Color }

        public string title;
        public string path;
        public ActiveSection activeSection = ActiveSection.None;

        public Mod(string path)
        {
            IniFile modIni = new IniFile(path + "/mod.ini");

            modIni.KeyExists("Title", "Desc");
            string title = modIni.Read("Title", "Desc");

            this.title = title;
            this.path = path;
            this.activeSection = ActiveSection.None;
        }

        public override string ToString()
        {
            return title;
        }
    }
}
