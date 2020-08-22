using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Category : MonoBehaviour
{
    public TMP_Text categoryLabel;

    /// <summary>
    /// Assigned by the category manager.
    /// </summary>
    [HideInInspector]
    public string categoryPath;

    /// <summary>
    /// Assigned by the category manager.
    /// </summary>
    [HideInInspector]
    public BrowserManager browserManager;

    /// <summary>
    /// Really only used for the "ALL" category.
    /// </summary>
    public Button button;

    /// <summary>
    /// What happens when this category is selected. This function should be assigned to the UnityEvent of the button in the prefab.
    /// </summary>
    public void Select()
    {
        if (this != browserManager.selectedCategory)
        {
            if (browserManager.selectedCategory != null)
            {
                browserManager.selectedCategory.categoryLabel.color = Color.black;
            }

            categoryLabel.color = browserManager.selectedUIColor;
            browserManager.selectedCategory = this;
            browserManager.FilterByCategory(this);
            browserManager.currentPage = 1;
            browserManager.PopulateBrowser();
        } 
    }
}
