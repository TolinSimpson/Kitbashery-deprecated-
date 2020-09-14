using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
        public Color fillColor = Color.red;
        private RaycastHit hit;

        public enum PaintMode { Stamp, IDFill, Pattern, None }
        private PaintMode mode = PaintMode.IDFill;

       // [Header("UI:")]
       // public TMP_Text hitCoords;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void FixedUpdate()
        {
            switch (mode)
            {
                case PaintMode.IDFill:

                    Raycast();

                    if (Input.GetKeyUp(KeyCode.Mouse0) == true)
                    {
                        if (hit.collider != null)
                        {
                            Vector2 pixelUV = hit.textureCoord * workingResolution;

                            int x = (int)pixelUV.x;
                            int y = (int)pixelUV.y;

                            if(matID == null)
                            {
                                CreateColorIDMap();
                            }
                            else
                            {
                                Color selectedPixel = matID.GetPixel(x, y);
                                if (selectedPixel != fillColor)//Don't fill if the same color
                                {
                                    TextureExtension.FloodFillArea(matID, x, y, fillColor, 1);
                                }
                                matID.Apply();
                                rend.material.SetTexture("_BaseMap", matID);
                                GetIDColors();
                            }
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

        public RaycastHit Raycast()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
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

        /// <summary>
        /// Automatically generates a material ID map.
        /// Note: Very RAM intensive for objects with more than 5 UV islands. a GPU floodfill probably needs to be made to reduce CPU load.
        /// </summary>
        public void CreateColorIDMap()
        {
            //Unwrap mesh:
            Texture2D unwrapTex = new Texture2D(workingResolution, workingResolution);

            RenderTexture rt = RenderTexture.GetTemporary(workingResolution, workingResolution);

            Graphics.SetRenderTarget(rt);
            GL.Clear(true, true, Color.black);
            GL.PushMatrix();
            GL.LoadOrtho();

            unwrap.SetPass(0);
            Graphics.DrawMeshNow(filter.sharedMesh, Matrix4x4.identity);
            Graphics.SetRenderTarget(null);
            RenderTexture rt2 = RenderTexture.GetTemporary(workingResolution, workingResolution);
            Graphics.Blit(rt, rt2, dilate);

            unwrapTex = TextureExporter.ToTexture2D(rt2);

            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.ReleaseTemporary(rt2);
            GL.PopMatrix();


            //Get pixels:
            List<Color32> pixels = new List<Color32>();
            foreach (Color32 pixel in unwrapTex.GetPixels32())
            {
                pixels.Add(pixel);
            }

            //Fill each white UV island with a random color:
            int xCoord = 0;
            int yCoord = 0;
            while (pixels.Contains(Color.white))
            {
                for (int x = xCoord; x < unwrapTex.width; x++)
                {
                    for (int y = yCoord; y < unwrapTex.height; y++)
                    {
                        if (unwrapTex.GetPixel(x, y) == Color.white)
                        {
                            TextureExtension.FloodFillArea(unwrapTex, xCoord, yCoord, Random.ColorHSV(0.1f, 0.9f, 0.2f, 0.9f, 1, 1, 1, 1), 1f);
                            unwrapTex.Apply();

                            yCoord = y;
                            xCoord = x;

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
    }

}