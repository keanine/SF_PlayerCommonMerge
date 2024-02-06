using System.Collections.Generic;
using System.Windows.Controls;

using ComboBox = System.Windows.Controls.ComboBox;

namespace SF_PlayerCommonMergeTool
{
    public partial class MainWindow
    {
        public class Character
        {
            public string name;
            public ComboBox SetAllComboBox;
            public StackPanel stackPanel;
            public List<Category> categories = new List<Category>();
            public List<Category> addonCategories = new List<Category>();
            public string rflName;

            public Character()
            {

            }

            public void Init(StackPanel stackPanel, string rflName)
            {
                this.stackPanel = stackPanel;
                this.rflName = rflName;
            }

            public void InitSetAllComboBox(string id)
            {
                Category category = new Category("Set All", id, name, Preferences.AddonFormat);
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

            public void AddCategory(string name, string id, int offset, int size, int order, int format)
            {
                Category category = new Category(name, id, offset, size, order, this.name, format);
                category.InitComboBox(stackPanel);
                categories.Add(category);
            }
        }
    }
}
