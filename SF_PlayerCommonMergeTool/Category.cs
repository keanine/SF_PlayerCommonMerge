using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SF_PlayerCommonMergeTool
{
    [System.Serializable]
    public class CategoryChunk
    {
        [JsonInclude] public int offset;
        [JsonInclude] public int size;

        public CategoryChunk(int offset, int size) 
        { 
            this.offset = offset;
            this.size = size;
        }
    }

    [System.Serializable]
    public class Category
    {
        [JsonInclude] public string name;
        [JsonInclude] public string id;
        [JsonInclude] public CategoryChunk[] chunks;
        [JsonInclude] public int order;
        public ComboBox comboBox;

        [JsonIgnore] public bool HasOffset { get; private set; }

        public Category()
        {

        }

        public Category(string name, string id, StackPanel parent, out ComboBox comboBox)
        {
            this.name = name;
            this.id = id;
            comboBox = InitComboBox(parent);
            this.comboBox = comboBox;
            this.order = 0;
        }

        public Category(string name, string id, int offset, int size, int order, StackPanel parent)
        {
            this.name = name;
            this.id = id;
            this.order = order;
            HasOffset = true;

            chunks = new CategoryChunk[1];
            chunks[0] = new CategoryChunk(offset, size);

            comboBox = InitComboBox(parent);
        }

        public Category(string name, string id, int order, StackPanel? parent, params CategoryChunk[] chunks)
        {
            this.name = name;
            this.id = id;
            this.order = order;
            HasOffset = true;
            this.chunks = chunks;

            if (parent != null)
            {
                comboBox = InitComboBox(parent);
            }
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
            comboBox.Width = 320;
            comboBox.Height = 22;
            comboBox.Items.Add("Vanilla");
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
