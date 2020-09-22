using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// This component holds reference to the imported mesh and methods for modifying its material.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class PaintableObject : MonoBehaviour
    {
        [Header("Components:")]
        public MeshRenderer rend;
        public MeshFilter filter;
        public MeshRenderer cloneRend;

        [Header("Materials:")]
        public Material unwrap;
        public Material dilate;
        public Material materialIDMat;
        public Material paintNormalsMat;
        public Material viewNormalsMat;

        [Header("Variables:")]
        public int workingResolution = 1024;
        public Texture2D matID;
        public List<Color> matIDColors = new List<Color>();
        public Color fillColor = Color.white;
        public LayerMask mask = 8;
        private RaycastHit hit;

        public bool lockControls = false;

        public enum PaintMode { Stamp, IDFill, Pattern, None }
        private PaintMode mode = PaintMode.IDFill;

        /// <summary>
        /// Has the user saved their changes?
        /// </summary>
        [HideInInspector]
        public bool savedChanges = false;

       // [Header("UI:")]
       // public TMP_Text hitCoords;

        // Start is called before the first frame update
        void Start()
        {

        }

        private void Update()
        {
            if(lockControls == false)
            {
                switch (mode)
                {
                    case PaintMode.IDFill:

                        Raycast();

                        if (Input.GetKeyUp(KeyCode.Mouse0) == true)
                        {
                            if (hit.collider != null && matID != null)
                            {
                                Vector2 pixelUV = hit.textureCoord * workingResolution;

                                int x = (int)pixelUV.x;
                                int y = (int)pixelUV.y;

                                ScanlineFloodFill(matID, new Vector2Int(x, y), matID.GetPixel(x, y), fillColor);
                                rend.material.SetTexture("_BaseMap", matID);
                                savedChanges = false;
                                // GetIDColors();
                            }
                        }


                        break;

                    case PaintMode.Stamp:

                        Raycast();
                        // rend.material.
                        //Vector2 pixelUV = hit.textureCoord

                        break;

                    case PaintMode.Pattern:

                        break;

                    case PaintMode.None:

                        break;
                }
            }        
        }

        public RaycastHit Raycast()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
            {

               // hitCoords.text = "Pixel Coordinates: ( " + Mathf.RoundToInt(hit.textureCoord.x * workingResolution) + ", " + Mathf.RoundToInt(hit.textureCoord.y * workingResolution) + " )";

            }
            else
            {
               // hitCoords.text = string.Empty;
            }

            return hit;
        }

        public void SetPaintMode(PaintMode paintMode)
        {
            mode = paintMode;
            switch(mode)
            {
                case PaintMode.IDFill:


                    break;

                case PaintMode.Stamp:

                    break;

                case PaintMode.Pattern:

                    break;


                case PaintMode.None:

                    break;
            }
        }

        public void SetFillColor(Color col)
        {
            fillColor = col;
        }

        public void ChangeResolution(int newResolution)
        {
            if(newResolution != workingResolution)
            {
                if(matID != null)
                {
                    TextureScale.Point(matID, newResolution, newResolution);
                }
                //todo do check for normal map to resize too.

                savedChanges = false;
            }
        }

        #region Material ID Mapping:

        public Texture2D Unwrap()
        {
            //Unwrap mesh:
            Texture2D unwrapTex = new Texture2D(workingResolution, workingResolution);
            unwrapTex.filterMode = FilterMode.Point;

            RenderTexture rt = RenderTexture.GetTemporary(workingResolution, workingResolution);
            rt.filterMode = FilterMode.Point;

            Graphics.SetRenderTarget(rt);
            GL.Clear(true, true, Color.black);
            GL.PushMatrix();
            GL.LoadOrtho();

            unwrap.SetPass(0);
            Graphics.DrawMeshNow(filter.sharedMesh, Matrix4x4.identity);
            Graphics.SetRenderTarget(null);

            //rt2 is the destination texture we blit a material to to fill in the little black gap between uv seams of the unwrapped texture (rt).
            //To "blit" shadergraph materials we have to use a custom render texture.
           /* CustomRenderTexture rt2 = new CustomRenderTexture(workingResolution, workingResolution, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            rt2.updateMode = CustomRenderTextureUpdateMode.OnDemand;
            rt2.material = dilate;
            rt2.initializationSource = CustomRenderTextureInitializationSource.Material;
            rt2.initializationMaterial = dilate;
            rt2.doubleBuffered = true;
            dilate.SetTexture("_MainTex", rt);
            rt2.Initialize();*/

            //If you are not using a shader graph shader to dilate the edges of uv islands these 3 lines will work:
            RenderTexture rt2 = RenderTexture.GetTemporary(workingResolution, workingResolution);
            dilate.SetTexture("_MainTex", rt);
            Graphics.Blit(rt, rt2, dilate);

            //TextureExporter.SaveRenderTexture(rt2, "test2", TextureExporter.SaveTextureFormat.jpg, Application.streamingAssetsPath);//Test output.

            unwrapTex = TextureExporter.ToTexture2D(rt2);

            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.ReleaseTemporary(rt2);
            GL.PopMatrix();

            return unwrapTex;
        }

        /// <summary>
        /// [Experimental] Automatically generates a material ID map.
        /// Note: Very RAM intensive for objects with more than 5 UV islands. a GPU floodfill probably needs to be made to reduce CPU load.
        /// Stack overflow?
        /// </summary>
        public void CreateColorIDMap()
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

            matID = unwrapTex;
            rend.material.SetTexture("_BaseMap", matID);
        }

        public void GetIDColors()
        {
            matIDColors.Clear();

            foreach (Color32 pixel in matID.GetPixels32())
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
                Debug.Log("Color is already set.");
            }
        }

        #endregion
    }

}