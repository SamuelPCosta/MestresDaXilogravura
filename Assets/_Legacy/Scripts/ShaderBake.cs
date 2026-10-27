using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;

public class ShaderBaker : MonoBehaviour
{
    public GameObject objectToBake;
    public Color backgroundColor;
    public bool bake;

    public Vector2Int textureDim;
    public Vector2Int textureCrop;
    public Vector3 textureOfset;

    void Start()
    {
        bake = false;
    }

    void Update()
    {
        if (bake)
            captureTexture();
    }

    public void captureTexture()
    {
        Mesh M = objectToBake.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices;
        vertices = M.vertices;

        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] += textureOfset;
        }
        Mesh modifiedMesh = Instantiate(M);

        modifiedMesh.vertices = vertices;
        modifiedMesh.RecalculateBounds();

        RenderTexture rt = RenderTexture.GetTemporary(textureDim.x, textureDim.y);
            
        Graphics.SetRenderTarget(rt);
        GL.Clear(true, true, backgroundColor);
        GL.PushMatrix();
        GL.LoadOrtho();

        //Captura o material no momento atual
        Renderer rendererObject = objectToBake.GetComponent<Renderer>();
        Material material = rendererObject.materials[0];
        material.SetPass(0);

        Graphics.DrawMeshNow(modifiedMesh, Matrix4x4.identity);
        SaveTexture(rt);
        RenderTexture.ReleaseTemporary(rt);
        GL.PopMatrix();
        if (bake)
            bake = false;
    }
    private void SaveTexture(RenderTexture rt)
    {
        DateTime currentTime = DateTime.Now;
        String mergeTime = currentTime.Year.ToString() + currentTime.Month.ToString() + currentTime.Day.ToString() + currentTime.Hour.ToString() + currentTime.Minute.ToString();
        string fullPath = Application.dataPath + "/../SavedImages/" + mergeTime + ".png";
        byte[] _bytes = toTexture2D(rt).EncodeToPNG();
        File.Delete(fullPath);
        File.WriteAllBytes(fullPath, _bytes);
        print("Salvo em SavedImages");
    }

    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width - textureCrop.x, rTex.height - textureCrop.y, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0); 
        tex.Apply();
        return tex;
    }
}