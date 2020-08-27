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
        public List<GameObject> imports = new List<GameObject>();

        /// <summary>
        /// The index of the current import.
        /// </summary>
        [HideInInspector]
        public string index;

        /// <summary>
        /// The current gameobject the user can adjust import settings for.
        /// </summary>
        [HideInInspector]
        public GameObject current;

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
            // Browse for .obj files:
            ExtensionFilter[] extensions = new[] { new ExtensionFilter("3D File", "obj", "fbx", "ply", "dae", "3ds", "stl", "ase", "mdl", "dxf", "xml") };//TODO: Get assimp supported file extensions.
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
            current.transform.rotation = Quaternion.identity;
            current.transform.position = Vector3.zero;
            current.transform.localScale = Vector3.one;
            current.name = nameInput.text;

            string savePath = Application.streamingAssetsPath + "/models/" + categoryList.options[categoryList.value].text + "/" + nameInput.text; //Create a folder with the name of the mesh under the selected category directory.

            //If a directory with the same name already exists add a unique number on the end.
            if (Directory.Exists(savePath))
            {
                UnityEngine.Random.InitState(UnityEngine.Random.Range(0, 1000));
                savePath = savePath + (UnityEngine.Random.Range(0, 10000));
            }

            Directory.CreateDirectory(savePath);
            OBJExport.Export(current, savePath);

            SaveThumbnail(savePath);

            categoryManager.modelsCount++;

            ImportNext();
        }

        public void SaveThumbnail(string savePath)
        {
            if (thumbnailCam.targetTexture != null)
            {
                //Convert to Texture2D:
                Texture2D tex = new Texture2D(thumbnailCam.targetTexture.width, thumbnailCam.targetTexture.height, TextureFormat.RGB24, false);
                RenderTexture.active = thumbnailCam.targetTexture;
                tex.ReadPixels(new Rect(0, 0, thumbnailCam.targetTexture.width, thumbnailCam.targetTexture.height), 0, 0);
                tex.Apply();

                //Shrink:
                TextureScale.Bilinear(tex, thumbnailResolution, thumbnailResolution);

                //Debug.Log(tex.EncodeToJPG().Length + " < jpg, png > " +tex.EncodeToPNG().Length); //What is smaller for the size, jpg or png?

                //Save:
                byte[] bytes = tex.EncodeToJPG(100);
                File.WriteAllBytes(savePath + "/" + current.name + "_Thumbnail.jpg", bytes);
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
            Destroy(current);

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
                nameInput.text = current.name;
                current.SetActive(true);
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
                if (current.name != string.Empty)
                {
                    nameInput.text = current.name;
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
            current.transform.localScale = Vector3.one * sizeSlider.value;//TODO: the min/max value of the slider could be determined by the bounds of the imported mesh.
        }

        public void RotateX()
        {
            //https://gamedev.stackexchange.com/questions/136174/im-rotating-an-object-on-two-axes-so-why-does-it-keep-twisting-around-the-thir

            current.transform.eulerAngles = new Vector3(rotXSlider.value, current.transform.eulerAngles.y, current.transform.eulerAngles.z);
            rotYSlider.value = current.transform.eulerAngles.y;
            rotZSlider.value = current.transform.eulerAngles.z;
        }

        public void RotateY()
        {
            current.transform.eulerAngles = new Vector3(current.transform.eulerAngles.x, rotYSlider.value, current.transform.eulerAngles.z);
            rotXSlider.value = current.transform.eulerAngles.x;
            rotZSlider.value = current.transform.eulerAngles.z;
        }

        public void RotateZ()
        {
            current.transform.eulerAngles = new Vector3(current.transform.eulerAngles.x, current.transform.eulerAngles.y, rotZSlider.value);
            rotYSlider.value = current.transform.eulerAngles.y;
            rotXSlider.value = current.transform.eulerAngles.x;
        }

        public void OffsetX()
        {
            current.transform.position = new Vector3(posXSlider.value, current.transform.position.y, current.transform.position.z);
        }

        public void OffsetY()
        {
            current.transform.position = new Vector3(current.transform.position.x, posYSlider.value, current.transform.position.z);
        }

        #endregion
    }
}