using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Kitbashery
{

    /// <summary>
    /// Dynamically creates categories based on folder structure or user input.
    /// </summary>
    public class CategoryManager : MonoBehaviour
    {
        #region Variables:

        /// <summary>
        /// The prefab for a category dropdown.
        /// </summary>
        [Tooltip("The prefab for a category dropdown.")]
        public GameObject categoryUI;

        /// <summary>
        /// The container of the category UI prefabs in the scroll view.
        /// </summary>
        [Tooltip("The container of the category UI prefabs in the scroll view.")]
        public RectTransform categoryContainer;

        /// <summary>
        /// Reference to the browser manager to pass on to the <see cref="Category"/> components when instantiating the ui.
        /// </summary>
        public BrowserManager browserManager;

        public GameObject categoryAddPopup;

        public GameObject categoryDeletePopup;

        [HideInInspector]
        public List<Category> categories = new List<Category>();

        [HideInInspector]
        public List<TMP_Dropdown.OptionData> dropdownOptions = new List<TMP_Dropdown.OptionData>();

        [HideInInspector]
        public int modelsCount = 0;

        public Category allCategory;

        [HideInInspector]
        public string modelsPath;
        private string uncategorizedDirPath;

        #endregion

        private void Awake()
        {
            //Make sure the models folder exists:
            modelsPath = Application.streamingAssetsPath + "/models/";
            if (!Directory.Exists(modelsPath))
            {
                Directory.CreateDirectory(modelsPath);
            }

            //Make sure the uncatagoirzed category folder exists:
            uncategorizedDirPath = Application.streamingAssetsPath + "/models/Uncategorized";
            if (!Directory.Exists(uncategorizedDirPath))
            {
                Directory.CreateDirectory(uncategorizedDirPath);
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            categories.Add(allCategory);
            GetCatagories();
            allCategory.categoryPath = modelsPath;
            allCategory.browserManager = browserManager;
            browserManager.selectedCategory = allCategory;
        }

        public void GetCatagories()
        {
            if (categories.Count > 1)
            {
                //Clear categories (except the "All" category):
                foreach (Category category in categories)
                {
                    if (category != allCategory)
                    {
                        Destroy(category.gameObject);
                    }
                }
                categories.Clear();
                categories.Add(allCategory);
                modelsCount = 0;
            }

            //Create new categories:
            foreach (string dir in Directory.GetDirectories(modelsPath))
            {
                GameObject ui = Instantiate(categoryUI, categoryContainer);
                Category category = ui.GetComponent<Category>();
                categories.Add(category);
                category.categoryPath = dir;
                category.browserManager = browserManager;

                int modelCount = Directory.GetDirectories(dir).Length;
                modelsCount += modelCount;
                category.categoryLabel.text = Path.GetFileName(dir) + " (" + modelCount + ")";
            }

            allCategory.categoryLabel.text = "All (" + modelsCount + ")";
        }

        public void UpdateAssetCounts()
        {
            foreach (Category category in categories)
            {
                if (category == allCategory)
                {
                    category.categoryLabel.text = "All (" + modelsCount + ")";
                }
                else
                {
                    category.categoryLabel.text = Path.GetFileName(category.categoryPath) + " (" + Directory.GetDirectories(category.categoryPath).Length + ")";
                }
            }

            browserManager.PopulateBrowser();
        }

        public void ShowDeleteCategoryPopup()
        {
            if (browserManager.selectedCategory != allCategory && browserManager.selectedCategory.categoryPath != uncategorizedDirPath)
            {
                categoryDeletePopup.SetActive(true);
            }
        }

        public void DeleteCategory()
        {
            if (browserManager.selectedCategory != allCategory && browserManager.selectedCategory.categoryPath != uncategorizedDirPath)
            {
                foreach (string dir in Directory.GetDirectories(browserManager.selectedCategory.categoryPath)) //Note: Enumeration though directory/file info may be faster for large amounts of files. 
                {
                    string dest = uncategorizedDirPath + "/" + Path.GetFileName(dir);
                    if (!Directory.Exists(dest))
                    {
                        Directory.CreateDirectory(dir);
                    }
#if UNITY_EDITOR
                    File.Move(dir + ".meta", dest + ".meta");
#endif
                    Directory.Move(dir, dest);
                }

#if UNITY_EDITOR
                File.Delete(browserManager.selectedCategory.categoryPath + ".meta");
#endif
                Directory.Delete(browserManager.selectedCategory.categoryPath);
                GetCatagories();
                UpdateAssetCounts();
                UpdateCategoryDropdownOptions();
            }

            categoryDeletePopup.SetActive(false);
        }

        public void AddNewCategory(TMP_InputField newCategory)
        {
            if (newCategory.text != string.Empty)
            {
                //Enforce capital first letter because it looks pretty:
                newCategory.text = newCategory.text.First().ToString().ToUpper() + newCategory.text.Substring(1);

                Directory.CreateDirectory(modelsPath + newCategory.text);//Note: Make sure the input field is set to alphanumeric to avoid invalid characters.
                newCategory.text = string.Empty;
                UpdateCategoryDropdownOptions();
                GetCatagories();

                categoryAddPopup.SetActive(false);
            }
        }

        public void UpdateCategoryDropdownOptions()
        {
            dropdownOptions.Clear();
            foreach (string dir in Directory.GetDirectories(modelsPath))
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
                option.text = Path.GetFileName(dir);
                dropdownOptions.Add(option);
            }
        }
    }

}