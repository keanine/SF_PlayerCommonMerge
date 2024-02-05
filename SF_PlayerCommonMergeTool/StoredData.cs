using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SF_PlayerCommonMergeTool
{
    [System.Serializable]
    public class StoredData
    {
        public string installLocation { get; set; }
        public List<CategorySelection> categorySelection { get; set; }
        public List<CategorySelection> categorySelectionTails { get; set; }
        public List<CategorySelection> categorySelectionKnuckles { get; set; }
        public List<CategorySelection> categorySelectionAmy { get; set; }

        public StoredData()
        {
            installLocation = string.Empty;
            categorySelection = new List<CategorySelection>();
            categorySelectionTails = new List<CategorySelection>();
            categorySelectionKnuckles = new List<CategorySelection>();
            categorySelectionAmy = new List<CategorySelection>();
        }

        public struct CategorySelection
        {
            public string id { get; set; }
            public string modTitle { get; set; }

            public CategorySelection(string id, string modTitle)
            {
                this.id = id;
                this.modTitle = modTitle;
                
            }
        }
    }
}
