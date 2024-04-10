using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrayRemap : MonoBehaviour
{   
    public AnimationCurve remapCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [ExecuteInEditMode]
    private void OnValidate()
    {
        Texture2D remapTex = EncodeCurveToTexture(remapCurve, 256);
        Shader.SetGlobalTexture("_RemapTex", remapTex);
    }

    Texture2D EncodeCurveToTexture(AnimationCurve curve, int width)
    {
        Texture2D texture = new Texture2D(width, 1, TextureFormat.ARGB32, false);

        for (int i = 0; i < width; i++)
        {
            float t = i / (float)width;
            float value = curve.Evaluate(t);
            texture.SetPixel(i, 0, new Color(value, value, value, 1));
        }

        texture.Apply();
        return texture;
    }
}
