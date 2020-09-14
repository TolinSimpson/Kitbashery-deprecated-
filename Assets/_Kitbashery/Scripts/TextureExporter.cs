using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    public static class TextureExporter
    {
        public enum SaveTextureFormat { png, jpg, tga, exr }

        public static void SaveRenderTexture(RenderTexture rt, string texName, SaveTextureFormat format, string savePath)
        {
            savePath += "/" + texName;

            byte[] _bytes = new byte[0];

            switch (format)
            {
                case SaveTextureFormat.jpg:

                    _bytes = ToTexture2D(rt).EncodeToJPG(100);

                    savePath += ".jpg";

                    break;

                case SaveTextureFormat.png:

                    _bytes = ToTexture2D(rt).EncodeToPNG();

                    savePath += ".png";

                    break;

                case SaveTextureFormat.tga:

                    _bytes = ToTexture2D(rt).EncodeToTGA();

                    savePath += ".tga";

                    break;

                case SaveTextureFormat.exr:

                    savePath += ".exr";

                    _bytes = ToTexture2D(rt).EncodeToEXR();

                    break;
            }

            File.Delete(savePath);
            File.WriteAllBytes(savePath, _bytes);
        }

        public static void SaveTexture2D(Texture2D tex2D, string texName, SaveTextureFormat format, string savePath)
        {

            savePath += "/" + texName;

            byte[] _bytes = new byte[0];

            switch (format)
            {
                case SaveTextureFormat.jpg:

                    _bytes = tex2D.EncodeToJPG(100);

                    savePath += ".jpg";

                    break;

                case SaveTextureFormat.png:


                    _bytes = tex2D.EncodeToPNG();

                    savePath += ".png";

                    break;

                case SaveTextureFormat.tga:

                    _bytes = tex2D.EncodeToTGA();

                    savePath += ".tga";

                    break;

                case SaveTextureFormat.exr:

                    _bytes = tex2D.EncodeToEXR();

                    savePath += ".exr";

                    break;
            }

            File.Delete(savePath);
            File.WriteAllBytes(savePath, _bytes);
        }


        public static Texture2D ToTexture2D(RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }
    }
}