﻿using System.Collections.Generic;
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
    }
}