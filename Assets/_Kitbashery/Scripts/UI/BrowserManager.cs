using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the loading of information of the clickable browser "items" that store mesh and thumbnail information. And selection of browser items.
/// </summary>
public class BrowserManager : MonoBehaviour
{
    #region Variables:

    [SerializeField]
    public List<BrowserItem> items = new List<BrowserItem>();

    public Color selectedUIColor = new Color(0, 1, 1, 0.5f);

    [HideInInspector]
    public BrowserItem selectedItem;

    [HideInInspector]
    public Catagory selectedCatagory;

    public GameObject deletePopup;

    public GameObject changeCatagoryPopup;

    public TMP_Dropdown changeCatagoryDropdown;

    public TMP_InputField searchbar;

    public GameObject nextPageButton;

    public GameObject previousPageButton;

    public TMP_InputField goToPage;

    public TMP_Text pageCountText;

    public GameObject selectedItemOptions;

    public GameObject pageOptions;

    /// <summary>
    /// The container for the UI, this should be the content of the scrollview.
    /// </summary>
    [Tooltip("The container for the UI, this should be the content of the scrollview.")]
    public Transform uiContainer;

    public MeshInspector meshInspector;

    public CatagoryManager catagoryManager;

    public LiveLink liveLink;

    public BuildModeUI buildControls;

    [HideInInspector]
    public int currentPage = 1;

    /// <summary>
    /// The total number of pages for our search results.
    /// </summary>
    private int pageCount = 0;
    /// <summary>
    /// The amount of items up until the current page.
    /// </summary>
    private int itemTotal = 0;

    private SortedDictionary<string, string> filteredCatagoryResults = new SortedDictionary<string, string>();
    private Dictionary<string, string> filteredSearchResults = new Dictionary<string, string>();

    #endregion

    #region Initialization & Updates:

    private void Start()
    {
        SetItemManager();
        catagoryManager.allCatagory.catagoryLabel.color = selectedUIColor;
        FilterByCatagory(catagoryManager.allCatagory);
        PopulateBrowser();
    }

    #endregion

    #region Grid Population Functions:

    /// <summary>
    /// Populates the browser grid with object info based on the user's search filters.
    /// </summary>
    public void PopulateBrowser()
    {
        DeselectItem();

        FilterBySearch();

        if (filteredSearchResults.Count > 0)
        {
            //Only 1 page of results:
            if (filteredSearchResults.Count <= items.Count)
            {
                currentPage = 1;
                pageCount = 1;
                itemTotal = 0;
            }
            else //Multiple pages of results:
            {
                pageCount = (filteredSearchResults.Count / items.Count);
                if(filteredSearchResults.Count % items.Count > 0)
                {
                    pageCount++;
                }

                if(currentPage == 1)//First page:
                {
                    itemTotal = 0;
                }
                else//Final page:
                {
                    itemTotal = (currentPage - 1) * items.Count;
                }
            }

            //Set page number text:
            goToPage.text = currentPage.ToString();
            pageCountText.text = "/ " + pageCount;


            //Display page navigation buttons:
            if (currentPage > 1)
            {
                previousPageButton.SetActive(true);
            }
            else
            {
                previousPageButton.SetActive(false);
            }

            if(currentPage < pageCount)
            {
                nextPageButton.SetActive(true);
            }
            else
            {
                nextPageButton.SetActive(false);
            }

            //Populate page:
            for (int i = 0; i < items.Count; i++)
            {
                items[i].thumbPath = string.Empty;
                items[i].meshPath = string.Empty;
                items[i].thumbnail.texture = null;

                if ((itemTotal + i) < filteredSearchResults.Keys.Count)
                {
                    SetBrowserItemPaths(items[i], filteredSearchResults.Keys.ElementAt(itemTotal + i));
                }

                if(items[i].thumbPath != string.Empty)
                {
                    LoadThumbnail(items[i].thumbnail, items[i].thumbPath);
                    if(items[i].isActiveAndEnabled != true)
                    {
                        ToggleHideBrowserItem(items[i], true);
                    }
                }
                else
                {
                    ToggleHideBrowserItem(items[i], false);
                }
            }
        }
        else
        {
            //If no results then hide all items:
            foreach(BrowserItem item in items)
            {
                ToggleHideBrowserItem(item, false);
            }

            previousPageButton.SetActive(false);
            goToPage.text = "0";
            pageCountText.text = "/ 0";
        }
    }

    public void LoadThumbnail(RawImage destination, string thumbnailPath)
    {
        Texture2D thumb = new Texture2D(2, 2);//image size does not matter, LoadImage will replace it with incoming image size.
        thumb.LoadImage(File.ReadAllBytes(thumbnailPath));
        destination.texture = thumb;
    }

    #endregion

    #region Browser Item Functions:

    /// <summary>
    /// Show or hide a browser item.
    /// </summary>
    /// <param name="item">The item to enable or disable.</param>
    /// <param name="enabled">Enable or disable the item.</param>
    public void ToggleHideBrowserItem(BrowserItem item, bool enabled)
    {
        if(enabled == false)//This if statement was to fix a bug, unsure what the source actually was but tooltips were being toggled on when they should have been.
        {
            item.tooltip.SetActive(false);
        }
        item.thumbnail.enabled = enabled;
        item.enabled = enabled;
    }

    public void SetBrowserItemPaths(BrowserItem item, string rootDirectoryName)
    {
        if(Directory.Exists(filteredSearchResults[rootDirectoryName]))
        {
            string[] files = Directory.GetFiles(filteredSearchResults[rootDirectoryName]);//Note: may be able to get .jpg and .obj files using search pattern instead of below with Path.GetExtension.
            foreach (string path in files)
            {
                if (File.Exists(path))
                {
                    string extension = Path.GetExtension(path);

                    if (extension == ".jpg")
                    {
                        item.thumbPath = path;
                    }
                    else if (extension == ".obj")
                    {
                        item.meshPath = path;
                    }

                    item.tooltipText.text = rootDirectoryName;
                }
            }
        }   
    }

    private void SetItemManager()
    {
        foreach(BrowserItem item in items)
        {
            item.manager = this;
        }
    }

    public void DeleteItem()
    {
        string dirPath = Path.GetDirectoryName(selectedItem.meshPath);
        if (Directory.Exists(dirPath))
        {
#if UNITY_EDITOR
            File.Delete(dirPath + ".meta");
#endif
            Directory.Delete(dirPath, true);
            selectedItem.meshPath = string.Empty;
            selectedItem.thumbPath = string.Empty;

            catagoryManager.modelsCount--;
            catagoryManager.UpdateAssetCounts();
            FilterByCatagory(selectedCatagory);
            PopulateBrowser();
        }
        deletePopup.SetActive(false);
    }


    public void MoveItemToCatagory(TMP_Dropdown dropdown)
    {
        if(dropdown.options[dropdown.value].text != selectedCatagory.catagoryLabel.text)//If the selected catagory The catagory the selected item is already in.
        {
            string catagoryPath = catagoryManager.modelsPath + dropdown.options[dropdown.value].text + "/" + Path.GetFileName(Path.GetDirectoryName(selectedItem.meshPath));
            /*if (!Directory.Exists(catagoryPath))
            {
                Debug.LogError("Path invalid");//Note: Path may be invalid if the dropdown options text is not the proper case such as the first letter is capital when the file folder's is not.
            }*/
            string dirPath = Path.GetDirectoryName(selectedItem.meshPath);
#if UNITY_EDITOR
            File.Move(dirPath + ".meta", catagoryPath + ".meta");
#endif
            Directory.Move(dirPath, catagoryPath);
            catagoryManager.UpdateAssetCounts();
            PopulateBrowser();
        }
       
        changeCatagoryPopup.SetActive(false);
    }

    #endregion

    #region Item Filter Functions:

    /// <summary>
    /// Gets all directories under a catagories path and stores their name for search filtering along with the directory path as reference for loading thumbnails and meshes.
    /// </summary>
    /// <param name="filter"></param>
    public void FilterByCatagory(Catagory filter)
    {
        filteredCatagoryResults.Clear();

        if(filter == catagoryManager.allCatagory)
        {
            //Make sure catagories have been generated:
            if(catagoryManager.catagories.Count == 0)
            {
                catagoryManager.GetCatagories();
            }

            //Filter through all catagories:
            foreach (Catagory catagory in catagoryManager.catagories)
            {
                if(catagory != catagoryManager.allCatagory)
                {
                    foreach (string dir in Directory.GetDirectories(catagory.catagoryPath))
                    {
                        //Add filename (directory folder name) for filtering (should be the same as the mesh name) and make sure it has a unique name if another file in another directory has the same name.
                        string filename = Path.GetFileName(dir);
                        if (!filteredCatagoryResults.Keys.Contains(filename))
                        {
                            filteredCatagoryResults.Add(filename, dir);
                        }
                        else
                        {
                            filteredCatagoryResults.Add(filename + filteredCatagoryResults.GetHashCode(), dir); //Note: the hashcode should make it unique but will mess with the searchbar filter.
                        }
                    }
                }
            }
        }
        else
        {
            //Filter through just the input filter catagory:
            foreach (string dir in Directory.GetDirectories(filter.catagoryPath))
            {
                string filename = Path.GetFileName(dir);
                if (!filteredCatagoryResults.Keys.Contains(filename))
                {
                    filteredCatagoryResults.Add(filename, dir);
                }
                else
                {
                    filteredCatagoryResults.Add(filename + filteredCatagoryResults.GetHashCode(), dir);
                }
            }
        }
    }

    /// <summary>
    /// Filters items to display by the searchbar filter (filters through filtered catagories)
    /// </summary>
    private void FilterBySearch()
    {
        filteredSearchResults.Clear();

        //TODO: Is a check if filteredCatagoryResults.Count > 0 nessesary?

        if (searchbar.text != string.Empty)
        {
            foreach (string str in filteredCatagoryResults.Keys)
            {
                if (str.StartsWith(searchbar.text, StringComparison.OrdinalIgnoreCase)) //str.Contains(searchbar.text))
                {
                    if (!filteredSearchResults.ContainsKey(str))
                    {
                        filteredSearchResults.Add(str, filteredCatagoryResults[str]);
                    }
                }
            }
        }
        else
        {
            //Note: it seems you cant set a dictionary to another dictionary without linking them so iterate:
            foreach(string str in filteredCatagoryResults.Keys)
            {
                filteredSearchResults.Add(str, filteredCatagoryResults[str]);
            }
        }
    }

    #endregion

    #region Selected Item Functions:

    public void SelectItem(BrowserItem item)
    {
        if(selectedItem != null)
        {
            DeselectItem();
        }

        selectedItem = item;
        selectedItem.thumbnail.color = selectedUIColor;
        selectedItemOptions.SetActive(true);
        pageOptions.SetActive(false);
    }

    public void DeselectItem()
    {
        if(selectedItem != null)
        {
            selectedItem.thumbnail.color = Color.white;
            selectedItemOptions.SetActive(false);
            selectedItem = null;
            pageOptions.SetActive(true);
        }
    }

    public void InspectSelectedItem()
    {
        meshInspector.gameObject.SetActive(true);
        meshInspector.InspectMesh(selectedItem.meshPath);
    }

    public void ShowDeletePopup()
    {
        deletePopup.SetActive(true);
    }

    public void ShowChangeCatagoryPopup()
    {
        changeCatagoryDropdown.options.Clear();
        if(catagoryManager.dropdownOptions.Count == 0)
        {
            catagoryManager.UpdateCatagoryDropdownOptions();
        }
        changeCatagoryDropdown.options = catagoryManager.dropdownOptions;
        changeCatagoryPopup.SetActive(true);
    }

    public void SendLiveLink()
    {
        if (selectedItem != null)
        {
            if (selectedItem.meshPath != string.Empty)
            {
                //liveLink.DebugJson(selectedItem.meshPath);
                liveLink.meshPath = selectedItem.meshPath;
                liveLink.Invoke("Link", 0);
            }
            else
            {
                Debug.LogError("Mesh path on the selected item was null, failed to send over livelink.");
            }
        }
        else
        {
            Debug.LogError("The is not a selected item to send over a livelink");
        }
    }

    public void AddItemToKitbash()
    {
        buildControls.AddPart(selectedItem.meshPath);
    }

    #endregion

    #region Page Functions:

    public void NextPage()
    {
        currentPage++;
        PopulateBrowser();
    }

    public void PreviousPage()
    {
        currentPage--;
        PopulateBrowser();
    }

    public void GoToPage()
    {
        int page = int.Parse(goToPage.text);
        if(page <= (filteredSearchResults.Count / items.Count) && page > 0)
        {
            currentPage = page;
            PopulateBrowser();
        }
        else //User input was outside of bounds, default to first page.
        {
            currentPage = 1;
            PopulateBrowser();
        }
    }

    #endregion
}