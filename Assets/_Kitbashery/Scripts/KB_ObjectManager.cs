using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeGizmos;
using System.IO;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// Manages <see cref="KitbashPart"/>s Selection, Materials and Deformations.
    /// </summary>
    public class KB_ObjectManager : MonoBehaviour
    {
        #region Variables:

        public KB_ImportExport importExport;

        /// <summary>
        /// Root container for KitbashParts in the modeling workflow.
        /// </summary>
        [Tooltip("Root container for KitbashParts in the modeling workflow.")]
        public GameObject kitbash;

        /// <summary>
        /// Root container for a KitbashPart being inspected.
        /// </summary>
        [Tooltip("Root container for a KitbashPart being inspected.")]
        public Import inspected;
        [HideInInspector]
        public string inspectedMeshStats;


        [Header("Import Variables:")]
        /// <summary>
        /// Root container for objects in the importing process.
        /// </summary>
        [Tooltip("Root container for objects in the importing process.")]
        public GameObject importRoot;

        [HideInInspector]
        public List<Import> imports = new List<Import>();

        /// <summary>
        /// The index of the current import.
        /// </summary>
        [HideInInspector]
        public string importIndexString;

        [HideInInspector]
        public int importIndex = 0;

        [Header("Selection Variables:")]
        /// <summary>
        /// The current assimp scene/ gameobject the user can adjust import settings for.
        /// </summary>
        [HideInInspector]
        public Import currentImport;
        public List<KitbashPart> parts = new List<KitbashPart>();
        public List<KitbashPart> selectedParts = new List<KitbashPart>();
        private List<KitbashPart> copiedParts = new List<KitbashPart>();
        public bool lockControls = true;

        [Header("Camera Gizmos:")]
        public TransformGizmo gizmo;
        public TransformController transformer;

        [Header("Mesh Inspection Materials:")]
        public Material normalSmoothing;
        public Material wireframe;
        public Material quadWireframe;
        public Material uvWireframe;
        public Material checker;

        [Header("Texture Baking Materials:")]
        public Material height;
        public Material normals;
        public Material normalsFromHeight;
        public Material unwrap;
        public Material dilate;
        public Material matID;

        [Header("Shared Materials:")]
        public Material defaultMat;

        [Header("Material ID Fill:")]
        public Texture2D unwrapTex;
        public int workingResolution = 1024;
        public bool fillModeActive = false;
        public Color fillColor = Color.white;
        public List<Color> matIDColors = new List<Color>();

        #endregion

        #region Initialization & Updates:

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(lockControls == false)
            {
                //press A to select all parts
                if (Input.GetKeyUp(KeyCode.A))
                {
                    if (selectedParts.Count == parts.Count)
                    {
                        foreach (KitbashPart part in parts)
                        {
                            gizmo.RemoveTarget(part.transform);
                        }
                    }
                    else
                    {
                        foreach (KitbashPart part in parts)
                        {
                            gizmo.AddTarget(part.transform);
                        }
                    }
                }

                if ((Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl)) && Input.GetKeyUp(KeyCode.C))
                {
                    CopySelected();
                }

                if (copiedParts.Count > 0 && ((Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl)) && Input.GetKeyUp(KeyCode.V)))
                {
                    PasteSelected();
                }

                //Delete selection.
                if (Input.GetKeyUp(KeyCode.Delete))
                {
                    DestroySelected();
                }
            }


            if (fillModeActive == true && Input.GetKeyUp(KeyCode.Mouse0) == true)
            {
               /* if (hit.collider != null && unwrapTex != null)
                {
                    Vector2 pixelUV = hit.textureCoord * workingResolution;

                    int x = (int)pixelUV.x;
                    int y = (int)pixelUV.y;

                    ScanlineFloodFill(unwrapTex, new Vector2Int(x, y), unwrapTex.GetPixel(x, y), fillColor);
                    rend.material.SetTexture("_BaseMap", unwrapTex);
                }

                */
            }
        }

        #endregion

        #region Part Creation:

        public void ClearImports()
        {
            if (imports.Count > 0)
            {
                for (int i = 0; i < imports.Count; i++)
                {
                    Import import = imports[i];
                    imports.Remove(import);
                    Destroy(import.GO);
                }
            }
        }

        public void AddPartToKitbash(string path)
        {
            importExport.ImportFromLibrary(path, false);
            currentImport.GO.transform.SetParent(transform);

            KitbashPart part = Instantiate(currentImport.GO.GetComponentInChildren<MeshRenderer>().gameObject).AddComponent<KitbashPart>();
            part.transform.SetParent(kitbash.transform);
            part.rend = part.gameObject.GetComponent<MeshRenderer>();
            part.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            part.rend.receiveShadows = false;
            part.rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            Destroy(currentImport.GO);
            // part.gameObject.name.TrimEnd("(clone)".ToCharArray());
            part.filter = part.gameObject.GetComponent<MeshFilter>();
            part.original = part.filter.sharedMesh;
            part.col = part.gameObject.GetComponent<MeshCollider>();
            part.col.sharedMesh = part.original;
            parts.Add(part);

            AddHierarchyItem(part);

            gizmo.transformType = TransformType.Move;
            gizmo.SetTranslatingAxis(TransformType.Move, RuntimeGizmos.Axis.Any);
            gizmo.AddTarget(part.gameObject.transform);
        }

        /// <summary>
        /// Sends the part to the import process.
        /// </summary>
        public void SaveToLibrary()
        {
            //Create a copy of every mesh in the kitbash: (We can't use Instantiate(kitbash) to create a copy because we need unique mesh instances):
            GameObject copy = new GameObject();//Note: may be able to get rid of the parent copy gameobject and transform the original child positions to world space since this partent is only to keep the transform values consistant.

            for (int i = 0; i < kitbash.transform.childCount; i++)
            {
                GameObject childOriginal = kitbash.transform.GetChild(i).gameObject;
                if (childOriginal.activeSelf == true)
                {
                    GameObject childCopy = new GameObject(childOriginal.name.TrimEnd("(Clone)".ToCharArray()));

                    MeshFilter mf = childCopy.AddComponent<MeshFilter>();
                    childCopy.AddComponent<MeshRenderer>().material = defaultMat;
                    childCopy.transform.SetParent(copy.transform);
                    childCopy.transform.position = childOriginal.transform.position;
                    childCopy.transform.rotation = childOriginal.transform.rotation;
                    childCopy.transform.localScale = childOriginal.transform.localScale;

                    Mesh original = childOriginal.GetComponent<MeshFilter>().sharedMesh;
                    Mesh newMesh = new Mesh();
                    newMesh.SetVertices(original.vertices);
                    newMesh.SetUVs(0, original.uv);
                    newMesh.SetNormals(original.normals);
                    newMesh.SetTriangles(original.triangles, 0);
                    newMesh.Optimize();
                    mf.sharedMesh = newMesh;

                    childCopy.SetActive(false);
                    imports.Add(new Import(importExport.GameObjectToAssimpScene(childCopy), childCopy, null, null));
                }
            }

            copy.transform.DetachChildren();
            Destroy(copy);

            //Set current import:
            currentImport = imports[0];
            currentImport.GO.SetActive(true);
            //meshName.text = current.GO.name;
            importIndexString = (importIndex + 1).ToString();
            //importCount.text = "Import " + importIndexString + " of " + imports.Count;
        }

        //ui
        public void AddHierarchyItem(KitbashPart part)
        {
           /* GameObject go = Instantiate(hierarchyItem);
            go.transform.SetParent(hierarchyContainer);
            HierarchyItem item = go.GetComponent<HierarchyItem>();
            item.part = part;
            item.buildControls = this;
            item.SetText();*/
        }

        //ui
        public void RemoveHierarchyItem(KitbashPart part)
        {

        }

        #endregion

        #region Material Functions:

        public void SetInspectedMat(int mat)
        {
            switch(mat)
            {
                case 0:

                    inspected.rend.material = defaultMat;

                    break;

                case 1:

                    inspected.rend.material = normalSmoothing;

                    break;

                case 2:

                    inspected.rend.material = wireframe;

                    break;

                case 3:

                    inspected.rend.material = quadWireframe;

                    break;

                case 4:


                    inspected.rend.material = uvWireframe;

                    break;

                case 5:

                    inspected.rend.material = checker;

                    break;
            }
        }

        #endregion

        #region Selection Functions:

        public string GetSelectionStats()
        {
            int v = 0;
            int t = 0;
            foreach (KitbashPart part in selectedParts)
            {
                v += part.filter.sharedMesh.vertexCount;
                t += (part.filter.sharedMesh.triangles.Length / 3);
            }
            return "Vertices: " + v + " | Triangles: " + t;
        }

        public void CopySelected()
        {
            //Note: may have to store instances of each mesh from each part to avoid the user deleting the original shared mesh and breaking part copies if done via instantiate.
            foreach (KitbashPart part in selectedParts)
            {
                copiedParts.Add(part);
            }
        }

        public void PasteSelected()
        {

        }

        public void DestroySelected()
        {
            for (int i = selectedParts.Count - 1; i >= 0; i--)
            {
                KitbashPart part = selectedParts[i];
                gizmo.RemoveTarget(selectedParts[i].transform);
                //TODO: remove hierarchy item.
                parts.Remove(part);
                Destroy(part.gameObject);
            }
        }

        public void SelectPart(KitbashPart part)
        {
            if (part != null)
            {
                selectedParts.Add(part);
            }
        }

        public void DeselectPart(KitbashPart part)
        {
            if (part != null)
            {
                selectedParts.Remove(part);
            }
        }

        #endregion

        #region MaterialID:

        public void SetFillColor(Color col)
        {
            fillColor = col;
        }

        /// <summary>
        /// Unwraps the mesh to a texture filling all uv islands white and dilating the edges to remove seam lines.
        /// </summary>
        /// <returns></returns>
        public Texture2D Unwrap()
        {
            //Unwrap mesh:
            Texture2D unwrapTex = new Texture2D(workingResolution, workingResolution, TextureFormat.ARGB32, false);
            unwrapTex.filterMode = FilterMode.Point;

            RenderTexture src = RenderTexture.GetTemporary(workingResolution, workingResolution, 0, RenderTextureFormat.ARGB32);
            src.filterMode = FilterMode.Point;

            Graphics.SetRenderTarget(src);
            GL.Clear(true, true, Color.black);
            GL.PushMatrix();
            GL.LoadOrtho();

            unwrap.SetPass(0);
            Graphics.DrawMeshNow(selectedParts[0].filter.sharedMesh, Matrix4x4.identity);
            Graphics.SetRenderTarget(null);

            //dest is the destination texture we blit a material to to fill in the little black gap between uv seams of the unwrapped texture (rt).
            //To "blit" shadergraph materials we have to use a custom render texture.
            /* CustomRenderTexture dest = new CustomRenderTexture(workingResolution, workingResolution, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
             dest.updateMode = CustomRenderTextureUpdateMode.OnDemand;
             dest.material = dilate;
             dest.initializationSource = CustomRenderTextureInitializationSource.Material;
             dest.initializationMaterial = dilate;
             dest.doubleBuffered = true;
             dilate.SetTexture("_MainTex", rt);
             dest.Initialize();*/

            //If you are not using a shader graph shader to dilate the edges of uv islands these 3 lines will work:
            RenderTexture dest = RenderTexture.GetTemporary(workingResolution, workingResolution, 0, RenderTextureFormat.ARGB32);
            dilate.SetTexture("_MainTex", src);
            Graphics.Blit(src, dest, dilate);

            //TextureExporter.SaveRenderTexture(dest, "test2", TextureExporter.SaveTextureFormat.jpg, Application.streamingAssetsPath);//Test output.

            unwrapTex = KB_ImportExport.ToTexture2D(dest);

            RenderTexture.ReleaseTemporary(src);
            RenderTexture.ReleaseTemporary(dest);
            GL.PopMatrix();


            return unwrapTex;
        }

        /// <summary>
        /// [Experimental] Automatically generates a material ID map.
        /// Note: Very RAM intensive for objects with more than 5 UV islands. a GPU floodfill probably needs to be made to reduce CPU load.
        /// Stack overflow?
        /// </summary>
        public Texture2D CreateColorIDMap()
        {
            Texture2D unwrapTex = Unwrap();

            //Get pixels:
            List<Color32> pixels = new List<Color32>();
            foreach (Color32 pixel in unwrapTex.GetPixels32())
            {
                pixels.Add(pixel);
            }

            //Fill each white UV island with a random color:
            int xCoord = 0;
            int yCoord = 0;
            int iterations = 0;
            while (pixels.Contains(Color.white) && iterations <= 25)
            {
                for (int x = xCoord; x < unwrapTex.width; x++)
                {
                    for (int y = yCoord; y < unwrapTex.height; y++)
                    {
                        if (unwrapTex.GetPixel(x, y) == Color.white)
                        {
                            ScanlineFloodFill(unwrapTex, new Vector2Int(x, y), Color.white, UnityEngine.Random.ColorHSV(0.1f, 0.9f, 0.2f, 0.9f, 1, 1, 1, 1));

                            yCoord = y;
                            xCoord = x;
                            iterations++;

                            List<Color32> newPixels = new List<Color32>();
                            foreach (Color32 pixel in unwrapTex.GetPixels32())
                            {
                                newPixels.Add(pixel);
                            }
                            pixels = newPixels;
                        }
                    }
                }
            }

            foreach (Color32 pixel in pixels)
            {
                if (!matIDColors.Contains(pixel))
                {
                    matIDColors.Add(pixel);
                }
            }
            matIDColors.Remove(Color.black);

            return unwrapTex;
        }

        public void GetIDColors()
        {
            matIDColors.Clear();

            foreach (Color32 pixel in unwrapTex.GetPixels32())
            {
                if (!matIDColors.Contains(pixel))
                {
                    matIDColors.Add(pixel);
                }
            }
        }

        private void ScanlineFloodFill(Texture2D tex, Vector2Int pos, Color targetColor, Color replacementColor)
        {
            if (targetColor != replacementColor)
            {
                Stack<Vector2Int> pixels = new Stack<Vector2Int>();

                pixels.Push(pos);
                while (pixels.Count != 0)
                {
                    Vector2Int temp = pixels.Pop();
                    int y1 = temp.y;
                    while (y1 >= 0 && tex.GetPixel(temp.x, y1) == targetColor)
                    {
                        y1--;
                    }
                    y1++;
                    bool spanLeft = false;
                    bool spanRight = false;
                    while (y1 < tex.height && tex.GetPixel(temp.x, y1) == targetColor)
                    {
                        tex.SetPixel(temp.x, y1, replacementColor);

                        if (!spanLeft && temp.x > 0 && tex.GetPixel(temp.x - 1, y1) == targetColor)
                        {
                            pixels.Push(new Vector2Int(temp.x - 1, y1));
                            spanLeft = true;
                        }
                        else if (spanLeft && temp.x - 1 == 0 && tex.GetPixel(temp.x - 1, y1) != targetColor)
                        {
                            spanLeft = false;
                        }
                        if (!spanRight && temp.x < tex.width - 1 && tex.GetPixel(temp.x + 1, y1) == targetColor)
                        {
                            pixels.Push(new Vector2Int(temp.x + 1, y1));
                            spanRight = true;
                        }
                        else if (spanRight && temp.x < tex.width - 1 && tex.GetPixel(temp.x + 1, y1) != targetColor)
                        {
                            spanRight = false;
                        }
                        y1++;
                    }

                }
                tex.Apply();
            }
            else
            {
                Debug.Log("Colors are the same.");
            }
        }

        #endregion
    }
}