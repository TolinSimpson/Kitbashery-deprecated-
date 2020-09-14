using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// This provides means of using a mesh to generate an image or "stamp" for use in 3D texturing.
    /// </summary>
    public class StampCreatorUI : MonoBehaviour
    {
        #region Variables:

        public CategoryManager categoryManager;

        public Camera stampCam;

        public int stampResolution = 256;

        public GameObject inspectedMesh;

        public TMP_Dropdown categoryList;

        public TMP_InputField newCategory;

        public TMP_InputField stampName;

        public TMP_Dropdown resolutionDropdown;


        #endregion

        private void OnEnable()
        {
            stampCam.gameObject.SetActive(true);
            categoryManager.UpdateCategoryDropdownOptions();
            categoryList.options = categoryManager.dropdownOptions;
        }


        public void CloseStampUI()
        {
            //Hide UI:
            gameObject.SetActive(false);
            stampCam.gameObject.SetActive(false);
        }

        public void SaveStamp(string savePath)
        {
            if (stampCam.targetTexture != null)
            {
                Texture2D tex = TextureExporter.ToTexture2D(stampCam.targetTexture);

                switch(resolutionDropdown.value)
                {
                    case 1:

                        stampResolution = 256;

                        break;

                    case 2:

                        stampResolution = 512;

                        break;

                    case 3:

                        stampResolution = 1024;

                        break;

                    case 4:

                        stampResolution = 2048;

                        break;

                    case 5:

                        stampResolution = 4096;

                        break;
                }

                //Shrink:
                TextureScale.Bilinear(tex, stampResolution, stampResolution);

                TextureExporter.SaveTexture2D(tex, inspectedMesh.name + "_Stamp.jpg", TextureExporter.SaveTextureFormat.png, savePath);
            }
            else
            {
                Debug.LogError("Exporting stamp failed: Stamp texture was null.");
            }

        }

        /// <summary>
        /// Called by OnEditEnd() of the name input field in the stamp settings UI.
        /// </summary>
        public void ValidateStampName()
        {
            //Make sure the name is not empty:
            if (stampName.text == string.Empty)
            {
                if (inspectedMesh.name != string.Empty)
                {
                    stampName.text = inspectedMesh.name;
                }
                else
                {
                    stampName.text = "Stamp" + inspectedMesh.GetHashCode();
                }
            }

            //Note: The input field should be set to alphanumeric.
        }


        public void AddNewCategory()
        {
            categoryManager.AddNewCategory(newCategory);
            UpdateCategoryDropdown();
        }

        public void UpdateCategoryDropdown()
        {
            categoryList.options.Clear();
            categoryManager.UpdateCategoryDropdownOptions();
            categoryList.options = categoryManager.dropdownOptions;
        }
    }
}