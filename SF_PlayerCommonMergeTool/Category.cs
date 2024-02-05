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
        [JsonIgnore] public int offsetValue { get; private set; }
        [JsonIgnore] public int sizeValue { get; private set; }
        [JsonInclude] public string offset = string.Empty;
        [JsonInclude] public string size = string.Empty;

        public CategoryChunk()
        {

        }

        public CategoryChunk(int offset, int size)
        {
            this.offsetValue = offset;
            this.sizeValue = size;
            SerializeHex();
        }
        public CategoryChunk(string offset, string size)
        {
            this.offset = offset;
            this.size = size;
            DeserializeHex();
        }

        public void SerializeHex()
        {
            offset = "0x" + offsetValue.ToString("X");
            size = "0x" + sizeValue.ToString("X");
        }

        public void DeserializeHex()
        {
            offsetValue = Convert.ToInt32(offset, 16);
            sizeValue = Convert.ToInt32(size, 16);
        }
    }

    [System.Serializable]
    public class Category
    {
        [JsonInclude] public string name;
        [JsonInclude] public string id;
        [JsonInclude] public string character;
        [JsonInclude] public CategoryChunk[] chunks;
        [JsonInclude] public int order;
        public ComboBox comboBox;

        [JsonIgnore] public bool HasOffset { get; private set; }

        public Category()
        {

        }

        public Category(string name, string id, StackPanel parent, string character, out ComboBox comboBox)
        {
            this.name = name;
            this.id = id;
            comboBox = InitComboBox(parent);
            this.comboBox = comboBox;
            this.order = 0;
            this.character = character;
        }

        public Category(string name, string id, int offset, int size, int order, StackPanel parent, string character)
        {
            this.name = name;
            this.id = id;
            this.order = order;
            this.character = character;
            HasOffset = true;

            chunks = new CategoryChunk[1];
            chunks[0] = new CategoryChunk(offset, size);

            comboBox = InitComboBox(parent);
        }

        public Category(string name, string id, int order, StackPanel? parent, string character, params CategoryChunk[] chunks)
        {
            this.name = name;
            this.id = id;
            this.order = order;
            HasOffset = true;
            this.chunks = chunks;
            this.character = character;

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

        public void DeserializeAllChunkValues()
        {
            foreach (var chunk in chunks)
            {
                chunk.DeserializeHex();
            }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
