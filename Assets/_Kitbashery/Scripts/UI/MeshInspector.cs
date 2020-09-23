using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// UI controls for changing the appearence of an inspected mesh.
    /// </summary>
    public class MeshInspector : MonoBehaviour
    {
        #region Variables:

        [Header("Object Refrences:")]
        public CameraOrbit orbitCam;
        public GameObject browserUI;
        public GameObject stampUI;
        public GameObject createNormalPopup;
        public GameObject createMatIDPopup;
        public GameObject normalMapEditorUI;
        public GameObject matIDMapEditorUI;
        public GameObject imageSavedPopup;
        public GameObject unsavedMatIDPopup;
        public GameObject unsavedNormalMapPopup;
        public KitbasheryMeshImporter importer;

        [Header("Inspected Mesh:")]
        public MeshRenderer rend;
        public MeshFilter filter;
        public MeshCollider col;
        public PaintableObject paintable;

        private string meshFolderPath = "";


        [Header("Inspector UI:")]
        public TMP_Text vertCount;
        public Toggle useQuads;
        public GameObject editMatID;
        public GameObject returnButton;
        public GameObject meshOptions;

        [Header("Materials:")]
        public Material defaultMat;
        public Material wireframeMat;
        public Material uvWireframeMat;
        public Material quadWireframeMat;
        public Material smoothingMat;
        public Material uvCheckerMat;
        public Material normalsMat;
        public Material matIDMat;
        public Material stampMat;

        [Header("Rendering:")]
        public RawImage matIDPreview;
        public RawImage normalMapPreview;
        public RenderTexture stampRendTex;

        #endregion

        /// <summary>
        /// Tells the manager to show the mesh inspector GUI and loads the mesh.
        /// </summary>
        public void InspectMesh(string meshPath)
        {
            if (!File.Exists(meshPath))
            {
                Debug.LogError("File doesn't exist.");
            }
            else
            {
                importer.mode = KitbasheryMeshImporter.kitbasheryUIMode.inspector;
                paintable.gameObject.name = Path.GetFileNameWithoutExtension(meshPath);
                importer.ImportSingle(meshPath);
                col.sharedMesh = filter.sharedMesh;
                meshFolderPath = Path.GetDirectoryName(meshPath);
                GetMeshTextures();

                orbitCam.distance = filter.sharedMesh.bounds.size.x + 5;
            }
        }

        public void GetMeshTextures()
        {
            string[] files = Directory.GetFiles(meshFolderPath);
            if(files.Length > 2)
            {
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (!fileName.Contains(".meta"))
                    {
                        if (fileName.Contains("_Normal"))
                        {
                            byte[] byteArray = File.ReadAllBytes(file);
                            Texture2D sampleTexture = new Texture2D(2, 2);
                            bool isLoaded = sampleTexture.LoadImage(byteArray, false);
                            if (isLoaded == true)
                            {
                                paintable.normalMap = sampleTexture;
                            }
                            else
                            {
                                Debug.LogError("Failed to load normal map at path: " + file);
                            }
                        }

                        if (fileName.Contains("_MatID"))
                        {
                            byte[] byteArray = File.ReadAllBytes(file);
                            Texture2D sampleTexture = new Texture2D(2, 2);
                            bool isLoaded = sampleTexture.LoadImage(byteArray, false);
                            if (isLoaded == true)
                            {
                                paintable.matID = sampleTexture;
                            }
                            else
                            {
                                Debug.LogError("Failed to load material ID at path: " + file);
                            }
                        }
                    }                  
                }
            }
        }

        public void EndMeshInspection()
        {
            orbitCam.enabled = false;
            orbitCam.transform.position = Vector3.zero;
            orbitCam.transform.rotation = Quaternion.identity;
            browserUI.SetActive(true);
            gameObject.SetActive(false);
            filter.sharedMesh = null;
            useQuads.isOn = false;
            meshFolderPath = string.Empty;
            paintable.matID = null;
            paintable.normalMap = null;

            //Reset cam constraints:
            //orbitCam.transform.position = Vector3.zero;
            //orbitCam.transform.localRotation = Quaternion.identity;
            orbitCam.distance = 5;
            if (orbitCam.mouseYConstraint == 0)
            {
                orbitCam.mouseYConstraint = 360;
            }

        }

        public void SetCamerUVView(bool uvView)
        {
            orbitCam.uvView = uvView;
            orbitCam.orbitCam.orthographic = uvView;

            if (uvView == true)
            {
                orbitCam.mouseYConstraint = 0;
                orbitCam.distance = 0.6f;
                if (filter.sharedMesh.uv.Length < 3)//Note: this might not ever happen if the exporter adds empty UV coords.
                {
                    vertCount.text = "Mesh does not have UV0";//Tempting to change this to "Do you even unwrap bro?"
                    //Note: if we ever get the xatlas integration working we might be able to just unwrap the mesh.
                }
                else
                {
                    vertCount.text = "UV0";
                }

            }
            else
            {
                if (orbitCam.mouseYConstraint == 0)
                {
                    orbitCam.distance = filter.sharedMesh.bounds.size.x + 5;
                    orbitCam.mouseYConstraint = 360;
                }
                vertCount.text = "Vertices: " + filter.sharedMesh.vertexCount + " | Triangles: " + (filter.sharedMesh.triangles.Length / 3);
            }

        }

        public void ToggleInspectorUI(bool toggle)
        {
            returnButton.SetActive(toggle);
            vertCount.gameObject.SetActive(toggle);
            meshOptions.SetActive(toggle);
        }

        #region View Modes:

        public void ViewDefault()
        {
            SetCamerUVView(false);
            useQuads.gameObject.SetActive(false);
            editMatID.SetActive(false);
            rend.material = defaultMat;
        }

        public void ViewWireframe()
        {
            SetCamerUVView(false);
            useQuads.gameObject.SetActive(true);
            editMatID.SetActive(false);

            if (useQuads.isOn == true)
            {
                rend.material = quadWireframeMat;
            }
            else
            {
                rend.material = wireframeMat;
            }
        }

        public void ViewUVWireframe()
        {
            SetCamerUVView(true);
            useQuads.gameObject.SetActive(false);
            editMatID.SetActive(false);
            rend.material = uvWireframeMat;
        }

        public void ViewSmoothing()
        {
            SetCamerUVView(false);
            useQuads.gameObject.SetActive(false);
            editMatID.SetActive(false);
            rend.material = smoothingMat;
        }

        public void ViewUVChecker()
        {
            SetCamerUVView(false);
            useQuads.gameObject.SetActive(false);
            editMatID.SetActive(false);
            rend.material = uvCheckerMat;
        }

        public void ViewNormals()
        {
            SetCamerUVView(false);
            useQuads.gameObject.SetActive(false);
            editMatID.SetActive(false);

            rend.material = normalsMat;
            if (paintable.normalMap != null)
            {
                rend.material.SetTexture("_NormalMap", paintable.normalMap);
            }
            else
            {
                createNormalPopup.SetActive(true);
            }
        }

        public void ViewMaterialID()
        {
            SetCamerUVView(false);
            useQuads.gameObject.SetActive(false);

            rend.material = matIDMat;
            if (paintable.matID != null)
            {
                editMatID.SetActive(true);
                rend.material.SetTexture("_BaseMap", paintable.matID);
            }
            else
            {
                createMatIDPopup.SetActive(true);
            }
        }

        public void ViewStamp()
        {
            SetCamerUVView(false);
            rend.material = stampMat;
        }

        #endregion

        #region Normal Map Editor:

        public void DisplayNormalMapEditor()
        {
            ToggleInspectorUI(false);

            normalMapEditorUI.SetActive(true);
            paintable.enabled = true;
            paintable.SetPaintMode(PaintableObject.PaintMode.Stamp);
            normalMapPreview.texture = paintable.normalMap;
        }

        public void CheckForUnsavedNormalMap()
        {
            if (paintable.savedChanges == false)
            {
                unsavedNormalMapPopup.SetActive(true);
            }
            else
            {
                CloseNormalMapEditor();
            }
        }

        public void CloseNormalMapEditor()
        {
            normalMapEditorUI.SetActive(false);
            paintable.enabled = false;
            ToggleInspectorUI(true);
        }

        #endregion

        #region Material ID Editor:

        public void DisplayMaterialIDEditor()
        {
            ToggleInspectorUI(false);

            if(paintable.matID == null)
            {
                paintable.matID = paintable.Unwrap();               
            }
            rend.material.SetTexture("_BaseMap", paintable.matID);

            matIDMapEditorUI.SetActive(true);
            paintable.enabled = true;
            paintable.SetPaintMode(PaintableObject.PaintMode.IDFill);
            matIDPreview.texture = paintable.matID;
        }

        public void CheckForUnsavedMatID()
        {
            if (paintable.savedChanges == false)
            {
                unsavedMatIDPopup.SetActive(true);
            }
            else
            {
                CloseMaterialIDEditor();
            }
        }

        public void CloseMaterialIDEditor()
        {
            matIDMapEditorUI.SetActive(false);
            paintable.enabled = false;
            ToggleInspectorUI(true);
        }

        public void SaveMatIDToLibrary()
        {
            TextureExporter.SaveTexture2D(paintable.matID, paintable.gameObject.name + "_MatID", TextureExporter.SaveTextureFormat.png, meshFolderPath);
            paintable.savedChanges = true;
            imageSavedPopup.SetActive(true);
        }

        public void ExportMatID()
        {
            ExtensionFilter[] extensions = new[] { new ExtensionFilter("Image Files", "png", "jpg", "tga", "exr") };
            TextureExporter.SaveTexture2DFullPath(paintable.matID, StandaloneFileBrowser.SaveFilePanel("Save Material ID", string.Empty, paintable.gameObject.name + "_MatID", extensions));
            paintable.savedChanges = true;
            imageSavedPopup.SetActive(true);
        }

        #endregion
    }

}