using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using System.IO;

public class FloodFill3D : MonoBehaviour
{
    public Image colorPicker;
    public Slider tolerance;
    public bool randomFillColor = false;

    public Material targetMat;
    public RenderTexture renderedColorMap;
    private Vector2 pixelUV;
    private Texture2D filledTex;
    private Vector3 viewOffset = new Vector3(0.5f, 0.5f, 0);

    private int x = 0;
    private int y = 0;

    private void Awake()
    {
        filledTex = new Texture2D(renderedColorMap.width, renderedColorMap.height);
    }

    void FixedUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0) == true)
        {
            if (targetMat.mainTexture != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    if (hit.collider != null)
                    {
                        RenderTexture.active = renderedColorMap;
                        filledTex.ReadPixels(new Rect(0, 0, renderedColorMap.width, renderedColorMap.height), 0, 0);
                        filledTex.Apply();

                        //Note: tex.Texture dimentions might not ever change and this could be cached.
                        pixelUV = hit.textureCoord;
                        pixelUV.x *= renderedColorMap.width;
                        pixelUV.y *= renderedColorMap.height;

                        x = (int)pixelUV.x;
                        y = (int)pixelUV.y;

                        Color selectedPixel = filledTex.GetPixel(x, y);
                        if (selectedPixel != colorPicker.color)//Don't fill if the same color
                        {
                            if (randomFillColor == true)
                            {
                                TextureExtension.FloodFillArea(filledTex, x, y, Random.ColorHSV(0.1f, 0.9f), tolerance.value);
                            }
                            else
                            {
                                TextureExtension.FloodFillArea(filledTex, x, y, colorPicker.color, tolerance.value);
                            }

                        }
                        filledTex.Apply();

                        targetMat.mainTexture = filledTex;
                    }
                }
            }
        }
    }

    public void SaveTexture(string mname)
    {
        mname += "_MaterialID";
        ExtensionFilter[] extensions = new[] { new ExtensionFilter("Save Material ID", "png") };
      
        string fullPath = StandaloneFileBrowser.SaveFilePanel(mname, "", "new_MaterialID", extensions);
        if (fullPath != null)
        {
            //byte[] _bytes = toTexture2D(rt).EncodeToPNG();
           // File.Delete(fullPath);
           // File.WriteAllBytes(fullPath, _bytes);
        }
    }

    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

   /* public void BakeUV()
    {
        RenderTexture rt = RenderTexture.GetTemporary(1024, 1024);

        Graphics.SetRenderTarget(rt);
        GL.Clear(true, true, Color.black);
        GL.PushMatrix();
        GL.LoadOrtho();
        uvMaterial.SetPass(0);
        Graphics.DrawMeshNow(mf.mesh, Matrix4x4.identity);
        Graphics.SetRenderTarget(null);
        RenderTexture rt2 = RenderTexture.GetTemporary(textureDim.x, textureDim.y);
        Graphics.Blit(rt, rt2, dilateMat);
        paintable.Texture = rt2;
        paintable.Replace();
        RenderTexture.ReleaseTemporary(rt);
        //RenderTexture.ReleaseTemporary(rt2);
        GL.PopMatrix();
    }*/

}
