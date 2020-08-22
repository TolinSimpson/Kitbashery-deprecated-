using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Catagory : MonoBehaviour
{
    public TMP_Text catagoryLabel;

    /// <summary>
    /// Assigned by the catagory manager.
    /// </summary>
    [HideInInspector]
    public string catagoryPath;

    /// <summary>
    /// Assigned by the catagory manager.
    /// </summary>
    [HideInInspector]
    public BrowserManager browserManager;

    /// <summary>
    /// Really only used for the "ALL" catagory.
    /// </summary>
    public Button button;

    /// <summary>
    /// What happens when this catagory is selected. This function should be assigned to the UnityEvent of the button in the prefab.
    /// </summary>
    public void Select()
    {
        if (this != browserManager.selectedCatagory)
        {
            if (browserManager.selectedCatagory != null)
            {
                browserManager.selectedCatagory.catagoryLabel.color = Color.black;
            }

            catagoryLabel.color = browserManager.selectedUIColor;
            browserManager.selectedCatagory = this;
            browserManager.FilterByCatagory(this);
            browserManager.currentPage = 1;
            browserManager.PopulateBrowser();
        } 
    }
}
