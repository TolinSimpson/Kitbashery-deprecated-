using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeGizmos;
using TMPro;
using SFB;
using System.IO;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// Manages UI and some scene objects.
    /// </summary>
    public class KB_UIManager : MonoBehaviour
    {
        #region Variables:

        public enum Workflows { None, Model, Texture }

        [Header("Managers:")]
        public KB_ObjectManager objectManager;
        public KB_CameraManager cameras;
        public KB_ImportExport importExport;
        public CategoryManager categoryManager;

        [Header("Other Refrences:")]
        public GameObject probuilderGizmos;
        public TransformGizmo gizmo;
        public TransformController transformer;
        public BrowserManager browserManager;


        [Header("Import options:")]
        public int thumbnailResolution = 256;
        public TMP_Dropdown categoryList;

        public TMP_InputField newCategory;

        public TMP_InputField meshName;

        /// <summary>
        /// The index number of the mesh in the objectManager.currentImport import.
        /// </summary>
        public TMP_Text importCount;

        public TMP_InputField nameInput;


        [Header("UI Menu Refrences:")]
        public GameObject inspectedMeshOptions;
        public TMP_Text inspectedMeshStats;
        public GameObject objectBrowserUI;
        public GameObject selectedItemOptions;

        #endregion

        #region Initialization & Updates:

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        #endregion

        #region Core Functions:

        public void SetWorkflow(Workflows workflow)
        {
            switch (workflow)
            {
                case Workflows.Model:

                    ToggleKitbashingMode();

                    break;

                case Workflows.Texture:



                    break;
            }
        }

        public void ToggleKitbashingMode()
        {
            objectManager.kitbash.SetActive(!objectManager.kitbash.activeSelf);
            probuilderGizmos.SetActive(!objectManager.kitbash.activeSelf);
        }

        #endregion

        #region Mesh Inspection Functions:

        public void InspectMesh()
        {
            importExport.ImportFromLibrary(browserManager.selectedItem.meshPath, true);
            inspectedMeshStats.text = objectManager.inspectedMeshStats;
            inspectedMeshOptions.SetActive(true);
            selectedItemOptions.SetActive(false);
            objectBrowserUI.SetActive(false);

            ToggleKitbashingMode();
        }

        public void EndMeshInspection()
        {
            Destroy(objectManager.inspected.GO);
            objectManager.inspected = null;
            inspectedMeshOptions.SetActive(false);
            objectBrowserUI.SetActive(true);
            selectedItemOptions.SetActive(true);

            ToggleKitbashingMode();
        }

        #endregion

        #region Import UI Functions:


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
                importExport.ImportExternalMesh(paths);
            }
            else
            {
                CloseImportUI();
                Debug.Log("No .obj files selected, closing UI");
            }
        }

        public void SaveImport()
        {
            objectManager.currentImport.GO.transform.rotation = Quaternion.identity;
            objectManager.currentImport.GO.transform.position = Vector3.zero;
            objectManager.currentImport.GO.transform.localScale = Vector3.one;
            objectManager.currentImport.GO.name = nameInput.text;

            string savePath = Application.streamingAssetsPath + "/models/" + categoryList.options[categoryList.value].text + "/" + nameInput.text; //Create a folder with the name of the mesh under the selected category directory.

            //If a directory with the same name already exists add a unique number on the end.
            if (Directory.Exists(savePath))
            {
                UnityEngine.Random.InitState(UnityEngine.Random.Range(0, 1000));
                savePath = savePath + (UnityEngine.Random.Range(0, 10000));
            }

            Directory.CreateDirectory(savePath);
            //importer.ExportOBJ(objectManager.currentImport.scene, savePath);
            KB_ImportExport.ExportOBJ(objectManager.currentImport.GO, savePath);

            SaveThumbnail(savePath);

            categoryManager.modelsCount++;

            ImportNext();
        }

        public void SaveThumbnail(string savePath)
        {
            /*if (thumbnailCam.targetTexture != null)
            {
                //Convert to Texture2D:
                Texture2D tex = importExport.ToTexture2D(thumbnailCam.targetTexture);

                //Shrink:
                TextureScale.Bilinear(tex, thumbnailResolution, thumbnailResolution);

                //Save:
                KB_ImportExport.SaveTexture2D(tex, objectManager.currentImport.GO.name + "_Thumbnail.jpg", KB_ImportExport.SaveTextureFormat.jpg, savePath);
            }
            else
            {
                Debug.LogError("Exporting thumbnail failed: Thumbnail texture was null.");
            }*/
        }

        public void CloseImportUI()
        {
            //Hide UI:
            gameObject.SetActive(false);
            //thumbnailCam.gameObject.SetActive(false);//todo

            //Clear imports:
            if (objectManager.imports.Count > 0)
            {
                //Update Browser:
                categoryManager.GetCatagories();
                categoryManager.browserManager.FilterByCategory(categoryManager.browserManager.selectedCategory);
                categoryManager.browserManager.PopulateBrowser();//BUG: browser doesnt appear populated this may just be the "all" tab or there needs to be a wait/check untill files have been created. This may also just be in the Unity editor.
                categoryList.value = 0;

                objectManager.importIndex = 0;
                objectManager.imports.Clear();
            }
        }

        /// <summary>
        /// Allow the user to edit import settings for the next mesh if more than one were imported. This is also used to skip over imports with the discard button.
        /// </summary>
        public void ImportNext()
        {
            Destroy(objectManager.currentImport.GO);

            //Update text:
            objectManager.importIndex++;
            objectManager.importIndexString = (objectManager.importIndex + 1).ToString();
            importCount.text = "Import " + objectManager.importIndexString + " of " + objectManager.imports.Count;

            //If there are not anymore objects to import clear imports and reset:
            if (objectManager.importIndex > objectManager.imports.Count - 1)
            {
                objectManager.importIndex = 0;
                importCount.text = string.Empty;

                objectManager.imports.Clear();

                categoryManager.UpdateAssetCounts();
                CloseImportUI();
            }
            else
            {
                //Set new Import:
                objectManager.currentImport = objectManager.imports[objectManager.importIndex];
                nameInput.text = objectManager.currentImport.GO.name;
                objectManager.currentImport.GO.SetActive(true);
            }
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
                if (objectManager.currentImport.GO.name != string.Empty)
                {
                    nameInput.text = objectManager.currentImport.GO.name;
                }
                else
                {
                    nameInput.text = "KitbashPart" + objectManager.currentImport.GetHashCode();
                }
            }

            //Note: The input field should be set to alphanumeric.
        }

        #endregion

    }
}