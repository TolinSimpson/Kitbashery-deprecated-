using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using RuntimeGizmos;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    public class BuildModeUI : MonoBehaviour
    {
        #region Variables:

        /// <summary>
        /// The container of the imported kitbashing parts.
        /// </summary>
        [Tooltip("The container of the imported kitbashing parts.")]
        public GameObject kitbash;
        [Space]

        [Header("UI Elements:")]
        public GameObject browser;
        public GameObject kitbashUI;
        public Transform hierarchyContainer;

        [Header("UI Prefabs:")]
        public GameObject hierarchyItem;

        [Header("Camera Gizmos:")]
        public CameraOrbit orbitCam;
        public TransformGizmo gizmo;
        public TransformController transformer;
        public GameObject gizmos;


        [Header("Other Refrences:")]
        public KitbasheryMeshImporter importer;
        public ImportUIManager importUI;
        public BrowserManager browserManager;

        [Header("Variables:")]
        [HideInInspector]
        public GameObject currentImport;
        public List<KitbashPart> parts = new List<KitbashPart>();
        public List<KitbashPart> selectedParts = new List<KitbashPart>();
        private List<KitbashPart> copiedParts = new List<KitbashPart>();
        public Material defaultMat;

        #endregion

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
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

            if((Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl)) && Input.GetKeyUp(KeyCode.C))
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

        public void ToggleBuildMode(bool toggle)
        {
            orbitCam.enabled = toggle;
            transformer.enabled = toggle;
            gizmo.enabled = toggle;

            gizmos.SetActive(toggle);
            gameObject.SetActive(toggle);
            browser.SetActive(!toggle);

            kitbash.gameObject.SetActive(toggle);

            if (toggle == false)
            {
                orbitCam.target = null;
                selectedParts.Clear();
            }
            else
            {
                browserManager.DeselectItem();

                //Make sure if the user was in uv mode during the last mesh inspection that the settings are reset for build mode.
                //Note: not sure why MeshInspector.EndMeshInspection() does not fix this when return is pressed from mesh inspect mode, might be able to change the code there to the code here.
                if (orbitCam.uvView == true)
                {
                    orbitCam.uvView = false;
                    orbitCam.orbitCam.orthographic = false;
                    orbitCam.distance = 5;
                    orbitCam.mouseYConstraint = 360;
                }
            }
        }

        public void AddHierarchyItem(KitbashPart part)
        {
            GameObject go = Instantiate(hierarchyItem);
            go.transform.SetParent(hierarchyContainer);
            HierarchyItem item = go.GetComponent<HierarchyItem>();
            item.part = part;
            item.buildControls = this;
            item.SetText();
        }

        public void RemoveHierarchyItem(KitbashPart part)
        {

        }

        public void AddPart(string path)
        {
            importer.mode = KitbasheryMeshImporter.kitbasheryUIMode.buildmode;
            importer.ImportSingle(path);
            currentImport.transform.SetParent(transform);

            KitbashPart part = Instantiate(currentImport.GetComponentInChildren<MeshRenderer>().gameObject).AddComponent<KitbashPart>();
            part.transform.SetParent(kitbash.transform);
            part.rend = part.gameObject.GetComponent<MeshRenderer>();
            part.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            part.rend.receiveShadows = false;
            part.rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            Destroy(currentImport);
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
            //orbit.target = part.go.transform;
            ToggleBuildMode(true);
        }


        public void CombineSelection()
        {
            if (selectedParts.Count > 1)
            {
                List<MeshFilter> filters = new List<MeshFilter>();
                foreach (KitbashPart part in selectedParts)
                {
                    filters.Add(part.filter);
                }

                if (filters.Count > 0)
                {
                    Mesh combine = MeshCombiner.CombineMeshes(filters, false);

                    combine.Optimize();
                    combine.RecalculateBounds();

                    DestroySelected();

                    //Create part:
                    GameObject go = new GameObject("Combined Part");
                    KitbashPart part = go.AddComponent<KitbashPart>();
                    part.transform.SetParent(kitbash.transform);

                    //Create renderer:
                    part.rend = part.gameObject.AddComponent<MeshRenderer>();
                    part.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    part.rend.receiveShadows = false;
                    part.rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    part.rend.material = defaultMat;
                    part.rend.allowOcclusionWhenDynamic = false;

                    //Assign mesh:
                    part.filter = go.AddComponent<MeshFilter>();
                    part.filter.sharedMesh = combine;

                    //Merge vertices if the mesh combine did not preserver welded vertices:
                    //https://answers.unity.com/questions/44208/merge-vertices-at-runtime.html
                    //Apply mesh corrections via probuilder:               
                    /*ProBuilderMesh pbm = go.AddComponent<ProBuilderMesh>();
                    MeshImporter importer = new MeshImporter(pbm);
                    importer.Import(part.filter.sharedMesh);
                    pbm.ToMesh();

                    //pbm.WeldVertices(pbm.selectedFaceIndexes, 0.001f); //Only welds one corner of each triangle

                    pbm.Refresh();*/



                    part.original = combine;
                    part.col = part.gameObject.AddComponent<MeshCollider>();
                    part.col.sharedMesh = combine;

                    parts.Add(part);
                    AddHierarchyItem(part);

                    gizmo.transformType = TransformType.Move;
                    gizmo.SetTranslatingAxis(TransformType.Move, RuntimeGizmos.Axis.Any);
                    gizmo.AddTarget(part.gameObject.transform);
                }
                else
                {
                    Debug.LogError("No filters were added to combine!");
                }
            }
            else
            {
                Debug.Log("Not enough parts selected.");
            }
        }

        public void CopySelected()
        {
            //Note: may have to store instances of each mesh from each part to avoid the user deleting the original shared mesh and breaking part copies if done via instantiate.
            foreach(KitbashPart part in selectedParts)
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
                    importUI.imports.Add(childCopy);
                }
            }

            copy.transform.DetachChildren();
            Destroy(copy);

            //Set current import:
            importUI.current = importUI.imports[0].gameObject;
            importUI.current.SetActive(true);
            importUI.meshName.text = importUI.current.name;
            importUI.index = (importUI.importIndex + 1).ToString();
            importUI.importCount.text = "Import " + importUI.index + " of " + importUI.imports.Count;


            ToggleBuildMode(false);
            importUI.gameObject.SetActive(true);
        }

        public void FlipNormals()
        {
          //  OBJExport.FlipNormals()
        }

        public void ExportMesh()
        {

        }
    }
}
