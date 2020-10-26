using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;

namespace Kitbashery
{
    /// <summary>
    /// Manages the UI for importing meshes into Kitbashery.
    /// </summary>
    public class ImportUIManager : MonoBehaviour
    {
        #region Variables:

        [HideInInspector]
        public List<Import> imports = new List<Import>();

        /// <summary>
        /// The index of the current import.
        /// </summary>
        [HideInInspector]
        public string index;

        /// <summary>
        /// The current assimp scene/ gameobject the user can adjust import settings for.
        /// </summary>
        [HideInInspector]
        public Import current;

        [HideInInspector]
        public int importIndex = 0;

        public CategoryManager categoryManager;

        [Header("Import Settings UI:")]

        public TMP_InputField nameInput;

        public Camera thumbnailCam;

        public Slider sizeSlider;

        public Slider rotXSlider;

        public Slider rotYSlider;

        public Slider rotZSlider;

        public Slider posXSlider;

        public Slider posYSlider;

        public TMP_Dropdown categoryList;

        public TMP_InputField newCategory;

        public TMP_InputField meshName;

        /// <summary>
        /// The index number of the mesh in the current import.
        /// </summary>
        public TMP_Text importCount;

        public KitbasheryMeshImporter importer;

        public int thumbnailResolution = 256;

        #endregion

        #region Initialization & Updates:

        private void Start()
        {
            UpdateCategoryDropdown();

            gameObject.SetActive(false);
            thumbnailCam.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            thumbnailCam.gameObject.SetActive(true);
            categoryManager.UpdateCategoryDropdownOptions();
            categoryList.options = categoryManager.dropdownOptions;
        }

        #endregion

        /// <summary>
        /// Opens the native file browser if a mesh file is selected it will be imported and the import settings UI displayed.
        /// </summary>
        public void OpenFileBrowser()
        {
            // Browse for mesh files:
            ExtensionFilter[] extensions = new[] { new ExtensionFilter("3D File", "obj", "fbx", "ply", "dae", "3ds", "stl", "ase", "mdl", "dxf", "xml") };
            //ExtensionFilter[] extensions = new[] { new ExtensionFilter("3D File", importer.supportedImportFormats) };//Not all formats are fully supported.
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Import", "", extensions, true);

            // Load each .obj at path:
            if (paths.Length > 0)
            {
                importer.mode = KitbasheryMeshImporter.kitbasheryUIMode.import;
                importer.ImportMultiple(paths);
            }
            else
            {
                CloseImportUI();
                Debug.Log("No .obj files selected, closing UI");
            }
        }

        public void SaveImport()
        {
            current.GO.transform.rotation = Quaternion.identity;
            current.GO.transform.position = Vector3.zero;
            current.GO.transform.localScale = Vector3.one;
            current.GO.name = nameInput.text;

            string savePath = Application.streamingAssetsPath + "/models/" + categoryList.options[categoryList.value].text + "/" + nameInput.text; //Create a folder with the name of the mesh under the selected category directory.

            //If a directory with the same name already exists add a unique number on the end.
            if (Directory.Exists(savePath))
            {
                UnityEngine.Random.InitState(UnityEngine.Random.Range(0, 1000));
                savePath = savePath + (UnityEngine.Random.Range(0, 10000));
            }

            Directory.CreateDirectory(savePath);
            //importer.ExportOBJ(current.scene, savePath);
            OBJExport.Export(current.GO, savePath);

            SaveThumbnail(savePath);

            categoryManager.modelsCount++;

            ImportNext();
        }

        public void SaveThumbnail(string savePath)
        {
            if (thumbnailCam.targetTexture != null)
            {
                //Convert to Texture2D:
                Texture2D tex = TextureExporter.ToTexture2D(thumbnailCam.targetTexture);

                //Shrink:
                TextureScale.Bilinear(tex, thumbnailResolution, thumbnailResolution);

                //Save:
                TextureExporter.SaveTexture2D(tex, current.GO.name + "_Thumbnail.jpg", TextureExporter.SaveTextureFormat.jpg, savePath);
            }
            else
            {
                Debug.LogError("Exporting thumbnail failed: Thumbnail texture was null.");
            }
        }

        public void CloseImportUI()
        {
            //Hide UI:
            gameObject.SetActive(false);
            thumbnailCam.gameObject.SetActive(false);

            //Clear imports:
            if (imports.Count > 0)
            {
                //Update Browser:
                categoryManager.GetCatagories();
                categoryManager.browserManager.FilterByCategory(categoryManager.browserManager.selectedCategory);
                categoryManager.browserManager.PopulateBrowser();//BUG: browser doesnt appear populated this may just be the "all" tab or there needs to be a wait/check untill files have been created. This may also just be in the Unity editor.
                categoryList.value = 0;

                importIndex = 0;
                imports.Clear();
            }
        }

        /// <summary>
        /// Allow the user to edit import settings for the next mesh if more than one were imported. This is also used to skip over imports with the discard button.
        /// </summary>
        public void ImportNext()
        {
            Destroy(current.GO);

            //Update text:
            importIndex++;
            index = (importIndex + 1).ToString();
            importCount.text = "Import " + index + " of " + imports.Count;

            //If there are not anymore objects to import clear imports and reset:
            if (importIndex > imports.Count - 1)
            {
                importIndex = 0;
                importCount.text = string.Empty;

                imports.Clear();

                categoryManager.UpdateAssetCounts();
                CloseImportUI();
            }
            else
            {
                //Set new current:
                current = imports[importIndex];
                nameInput.text = current.GO.name;
                current.GO.SetActive(true);
            }

            ResetSliders();
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

        /// <summary>
        /// Called by OnEditEnd() of the name input field for the import settings.
        /// </summary>
        public void ValidateImportName()
        {
            //Make sure the name is not empty:
            if (nameInput.text == string.Empty)
            {
                if (current.GO.name != string.Empty)
                {
                    nameInput.text = current.GO.name;
                }
                else
                {
                    nameInput.text = "KitbashPart" + current.GetHashCode();
                }
            }

            //Note: The input field should be set to alphanumeric.
        }

        #region Slider Controls:

        public void ResetSliders()
        {
            sizeSlider.value = 1;
            rotXSlider.value = 0;
            rotYSlider.value = 0;
            rotZSlider.value = 0;
            posXSlider.value = 0;
            posYSlider.value = 0;
        }

        public void ScaleCurrentSize()
        {
            current.GO.transform.localScale = Vector3.one * sizeSlider.value;//TODO: the min/max value of the slider could be determined by the bounds of the imported mesh.
        }

        public void RotateX()
        {
            //https://gamedev.stackexchange.com/questions/136174/im-rotating-an-object-on-two-axes-so-why-does-it-keep-twisting-around-the-thir

            current.GO.transform.eulerAngles = new Vector3(rotXSlider.value, current.GO.transform.eulerAngles.y, current.GO.transform.eulerAngles.z);
            rotYSlider.value = current.GO.transform.eulerAngles.y;
            rotZSlider.value = current.GO.transform.eulerAngles.z;
        }

        public void RotateY()
        {
            current.GO.transform.eulerAngles = new Vector3(current.GO.transform.eulerAngles.x, rotYSlider.value, current.GO.transform.eulerAngles.z);
            rotXSlider.value = current.GO.transform.eulerAngles.x;
            rotZSlider.value = current.GO.transform.eulerAngles.z;
        }

        public void RotateZ()
        {
            current.GO.transform.eulerAngles = new Vector3(current.GO.transform.eulerAngles.x, current.GO.transform.eulerAngles.y, rotZSlider.value);
            rotYSlider.value = current.GO.transform.eulerAngles.y;
            rotXSlider.value = current.GO.transform.eulerAngles.x;
        }

        public void OffsetX()
        {
            current.GO.transform.position = new Vector3(posXSlider.value, current.GO.transform.position.y, current.GO.transform.position.z);
        }

        public void OffsetY()
        {
            current.GO.transform.position = new Vector3(current.GO.transform.position.x, posYSlider.value, current.GO.transform.position.z);
        }

        #endregion
    }
}