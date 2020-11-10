using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    public class NormalStampUI : MonoBehaviour
    {
        public PaintableObject paintable;
        public Texture2D defaultStamp;

        [Header("UI Elements:")]
        public RawImage normalStampPreview;
        public Slider normalStrengthSlider;
        public Slider normalOffsetSlider;
        public Slider brushSize;
        public Toggle looping;

        public void Awake()
        {
            paintable.stampNormalsMat.SetTexture("_Stamp", defaultStamp);
            paintable.stampNormalsMat.SetFloat("_Strength", 2);
            paintable.stampNormalsMat.SetFloat("_Offset", 1);
            paintable.stampNormalsMat.SetFloat("_BrushSize", 0.5f);
            paintable.stampNormalsMat.DisableKeyword("_Looping");
        }

        public void SetBrushSize(float value)
        {
            paintable.stampNormalsMat.SetFloat("_BrushSize", value);
            UpdateNormalStampPreview();
        }

        public void ToggleLooping(bool toggle)
        {
            if(toggle == true)
            {
                paintable.stampNormalsMat.EnableKeyword("_Looping");
            }
            else
            {
                paintable.stampNormalsMat.DisableKeyword("_Looping");
            }
            UpdateNormalStampPreview();

        }

        public void SetStamp(Texture2D tex)
        {
            paintable.stampNormalsMat.SetTexture("_Stamp", tex);
            UpdateNormalStampPreview();
        }

        public void SetNormalOffset(float value)
        {
            paintable.stampNormalsMat.SetFloat("_Offset", value);
            UpdateNormalStampPreview();
        }

        public void SetNormalStrength(float value)
        {
            paintable.stampNormalsMat.SetFloat("_Strength", value);
            UpdateNormalStampPreview();
        }

        public void SaveNormalMapToLibrary()
        {
            //TextureExporter.SaveTexture2D(paintable.normalMap, )
        }

        public void ExportNormalStamp()
        {

        }

        public void UpdateNormalStampPreview()
        {
            Graphics.Blit(normalStampPreview.texture, paintable.stampNormalsMat);
        }
    }
}