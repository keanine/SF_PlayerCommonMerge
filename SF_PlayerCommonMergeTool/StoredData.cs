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
        public List<CategorySelection> categorySelectionsSonic { get; set; }
        public List<CategorySelection> categorySelectionsTails { get; set; }
        public List<CategorySelection> categorySelectionsKnuckles { get; set; }
        public List<CategorySelection> categorySelectionsAmy { get; set; }

        public List<CategorySelection> addonCategorySelectionsSonic { get; set; }
        public List<CategorySelection> addonCategorySelectionsTails { get; set; }
        public List<CategorySelection> addonCategorySelectionsKnuckles { get; set; }
        public List<CategorySelection> addonCategorySelectionsAmy { get; set; }

        public StoredData()
        {
            installLocation = string.Empty;
            categorySelectionsSonic = new List<CategorySelection>();
            categorySelectionsTails = new List<CategorySelection>();
            categorySelectionsKnuckles = new List<CategorySelection>();
            categorySelectionsAmy = new List<CategorySelection>();

            addonCategorySelectionsSonic = new List<CategorySelection>();
            addonCategorySelectionsTails = new List<CategorySelection>();
            addonCategorySelectionsKnuckles = new List<CategorySelection>();
            addonCategorySelectionsAmy = new List<CategorySelection>();
        }

        [System.Serializable]
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
