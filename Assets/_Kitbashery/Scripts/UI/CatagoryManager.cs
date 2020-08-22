using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Dynamically creates catagories based on folder structure or user input.
/// </summary>
public class CatagoryManager : MonoBehaviour
{
    #region Variables:

    /// <summary>
    /// The prefab for a catagory dropdown.
    /// </summary>
    [Tooltip("The prefab for a catagory dropdown.")]
    public GameObject catagoryUI;

    /// <summary>
    /// The container of the catagory UI prefabs in the scroll view.
    /// </summary>
    [Tooltip("The container of the catagory UI prefabs in the scroll view.")]
    public RectTransform catagoryContainer;

    /// <summary>
    /// Reference to the browser manager to pass on to the <see cref="Catagory"/> components when instantiating the ui.
    /// </summary>
    public BrowserManager browserManager;

    public GameObject catagoryAddPopup;

    public GameObject catagoryDeletePopup;

    [HideInInspector]
    public List<Catagory> catagories = new List<Catagory>();

    [HideInInspector]
    public List<TMP_Dropdown.OptionData> dropdownOptions = new List<TMP_Dropdown.OptionData>();

    [HideInInspector]
    public int modelsCount = 0;

    public Catagory allCatagory;

    [HideInInspector]
    public string modelsPath;
    private string uncatagorizedDirPath;

    #endregion

    private void Awake()
    {
        //Make sure the models folder exists:
        modelsPath = Application.streamingAssetsPath + "/models/";
        if(!Directory.Exists(modelsPath))
        {
            Directory.CreateDirectory(modelsPath);
        }

        //Make sure the uncatagoirzed catagory folder exists:
        uncatagorizedDirPath = Application.streamingAssetsPath + "/models/Uncatagorized";
        if (!Directory.Exists(uncatagorizedDirPath))
        {
            Directory.CreateDirectory(uncatagorizedDirPath);
        } 
    }

    // Start is called before the first frame update
    private void Start()
    {
        catagories.Add(allCatagory);
        GetCatagories();
        allCatagory.catagoryPath = modelsPath;
        allCatagory.browserManager = browserManager;
        browserManager.selectedCatagory = allCatagory;
    }

    public void GetCatagories()
    {
        if(catagories.Count > 1)
        {
            //Clear catagories (except the "All" catagory):
            foreach (Catagory catagory in catagories)
            {
                if(catagory != allCatagory)
                {
                    Destroy(catagory.gameObject);
                }
            }
            catagories.Clear();
            catagories.Add(allCatagory);
            modelsCount = 0;
        }

        //Create new catagories:
        foreach (string dir in Directory.GetDirectories(modelsPath))
        {
            GameObject ui = Instantiate(catagoryUI, catagoryContainer);
            Catagory catagory = ui.GetComponent<Catagory>();
            catagories.Add(catagory);
            catagory.catagoryPath = dir;
            catagory.browserManager = browserManager;

            int modelCount = Directory.GetDirectories(dir).Length;
            modelsCount += modelCount;
            catagory.catagoryLabel.text = Path.GetFileName(dir) + " (" + modelCount + ")";
        }

        allCatagory.catagoryLabel.text = "All (" + modelsCount + ")";
    }

    public void UpdateAssetCounts()
    {
        foreach(Catagory catagory in catagories)
        {
            if(catagory == allCatagory)
            {
                catagory.catagoryLabel.text =  "All (" + modelsCount + ")";
            }
            else
            {
                catagory.catagoryLabel.text = Path.GetFileName(catagory.catagoryPath) + " (" + Directory.GetDirectories(catagory.catagoryPath).Length + ")";
            }
        }

        browserManager.PopulateBrowser();
    }

    public void ShowDeleteCatagoryPopup()
    {
        if(browserManager.selectedCatagory != allCatagory && browserManager.selectedCatagory.catagoryPath != uncatagorizedDirPath)
        {
            catagoryDeletePopup.SetActive(true);
        }
    }

    public void DeleteCatagory()
    {
        if(browserManager.selectedCatagory != allCatagory && browserManager.selectedCatagory.catagoryPath != uncatagorizedDirPath)
        {
                foreach (string dir in Directory.GetDirectories(browserManager.selectedCatagory.catagoryPath)) //Note: Enumeration though directory/file info may be faster for large amounts of files. 
                {
                    string dest = uncatagorizedDirPath + "/" + Path.GetFileName(dir);
                    if(!Directory.Exists(dest))
                    {
                        Directory.CreateDirectory(dir);
                    }
#if UNITY_EDITOR
                File.Move(dir + ".meta", dest + ".meta");
#endif
                Directory.Move(dir, dest);
                }

#if UNITY_EDITOR
            File.Delete(browserManager.selectedCatagory.catagoryPath + ".meta");
#endif
            Directory.Delete(browserManager.selectedCatagory.catagoryPath);
                GetCatagories();
                UpdateAssetCounts();
                UpdateCatagoryDropdownOptions();
        }

        catagoryDeletePopup.SetActive(false);
    }

    public void AddNewCatagory(TMP_InputField newCatagory)
    {
        if (newCatagory.text != string.Empty)
        {
            //Enforce capital first letter because it looks pretty:
            newCatagory.text = newCatagory.text.First().ToString().ToUpper() + newCatagory.text.Substring(1);

            Directory.CreateDirectory(modelsPath + newCatagory.text);//Note: Make sure the input field is set to alphanumeric to avoid invalid characters.
            newCatagory.text = string.Empty;
            UpdateCatagoryDropdownOptions();
            GetCatagories();

            catagoryAddPopup.SetActive(false);
        }
    }

    public void UpdateCatagoryDropdownOptions()
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
