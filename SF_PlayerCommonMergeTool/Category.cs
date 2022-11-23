using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SF_PlayerCommonMergeTool
{

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
}
