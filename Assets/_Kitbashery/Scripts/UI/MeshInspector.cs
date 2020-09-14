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
    /// UI controls for changing the appearence of an inspected mesh.
    /// </summary>
    public class MeshInspector : MonoBehaviour
    {
        public CameraOrbit orbitCam;
        public GameObject browserUI;
        public GameObject stampUI;
        public GameObject createNormalPopup;
        public GameObject createMatIDPopup;
        public GameObject normalMapEditorUI;
        public GameObject matIDMapEditorUI;
        public KitbasheryMeshImporter importer;

        public MeshRenderer rend;
        public MeshFilter filter;
        public PaintableObject paintable;

        [HideInInspector]
        public Texture2D matID;
        [HideInInspector]
        public Texture2D normal;


        [Header("Inspector UI:")]
        public TMP_Text vertCount;
        public Toggle useQuads;
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
        public RenderTexture stampRendTex;
        public RenderTexture normalRendTex;
        public RenderTexture matIDRendTex;

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
                importer.ImportSingle(meshPath);
                GetMeshTextures(meshPath);

                orbitCam.distance = filter.sharedMesh.bounds.size.x + 5;
            }
        }

        public void GetMeshTextures(string meshPath)
        {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(meshPath));
            if(files.Length > 2)
            {
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains("_Normal"))
                    {
                        byte[] byteArray = File.ReadAllBytes(file);
                        Texture2D sampleTexture = new Texture2D(2, 2);
                        bool isLoaded = sampleTexture.LoadImage(byteArray);
                        if (isLoaded == true)
                        {
                            normal = sampleTexture;
                        }
                        else
                        {
                            Debug.LogError("Failed to load normal map at path: " + file);
                        }
                    }

                    if (Path.GetFileName(file).Contains("_MatID"))
                    {
                        byte[] byteArray = File.ReadAllBytes(file);
                        Texture2D sampleTexture = new Texture2D(2, 2);
                        bool isLoaded = sampleTexture.LoadImage(byteArray);
                        if (isLoaded == true)
                        {
                            matID = sampleTexture;
                        }
                        else
                        {
                            Debug.LogError("Failed to load material ID at path: " + file);
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

            //Reset cam constraints:
            //orbitCam.transform.position = Vector3.zero;
            //orbitCam.transform.localRotation = Quaternion.identity;
            orbitCam.distance = 5;
            if (orbitCam.mouseYConstraint == 0)
            {
                orbitCam.mouseYConstraint = 360;
            }
        }

        public void ViewDefault()
        {
            SetCamerUVView(false);
            rend.material = defaultMat;
        }

        public void ViewWireframe()
        {
            SetCamerUVView(false);

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
            rend.material = uvWireframeMat;
        }

        public void ViewSmoothing()
        {
            SetCamerUVView(false);
            rend.material = smoothingMat;
        }

        public void ViewUVChecker()
        {
            SetCamerUVView(false);
            rend.material = uvCheckerMat;
        }

        public void ViewNormals()
        {
            SetCamerUVView(false);

            rend.material = normalsMat;
            if (normal != null)
            {
                rend.material.SetTexture("_NormalMap", normal);
            }
            else
            {
                createNormalPopup.SetActive(true);
            }
        }

        public void ViewMaterialID()
        {
            SetCamerUVView(false);

            rend.material = matIDMat;
            if (matID != null)
            {
                rend.material.SetTexture("_BaseMap", matID);
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

        public void DisplayNormalMapEditor()
        {
            ToggleInspectorUI(false);

            normalMapEditorUI.SetActive(true);
            paintable.enabled = true;
            paintable.SetPaintMode(PaintableObject.PaintMode.Stamp);
        }

        public void EndNormalMapEdit()
        {
            normalMapEditorUI.SetActive(false);
            paintable.enabled = false;
            ToggleInspectorUI(true);
        }

        public void DisplayMaterialIDEditor()
        {
            ToggleInspectorUI(false);

            matIDMapEditorUI.SetActive(true);
            paintable.enabled = true;
            paintable.SetPaintMode(PaintableObject.PaintMode.IDFill);
        }

        public void EndMaterialIDMapEdit()
        {
            matIDMapEditorUI.SetActive(false);
            paintable.enabled = false;
            ToggleInspectorUI(true);
        }
    }

}