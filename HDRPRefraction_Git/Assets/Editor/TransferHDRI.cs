using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public class TransferHDRI
{
    [MenuItem("Assets/Transfer HDR images to Cubemap")]
    static void TransferHDR2Cubemap()
    {
        if (Selection.assetGUIDs.Length == 0)
        {
            Debug.Log("No selection.");
            return;
        }
        string textureGUID = Selection.assetGUIDs[0];
        string path = AssetDatabase.GUIDToAssetPath(textureGUID);
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        textureImporter.textureShape = TextureImporterShape.TextureCube;
        AssetDatabase.ImportAsset(path);

        Material mat = new Material(Shader.Find("Skybox/Cubemap"));
        string fileName = Path.GetFileNameWithoutExtension(path);
        Cubemap cubemap = Resources.Load(fileName, typeof(Cubemap)) as Cubemap;
        if (cubemap == null)
        {
            Debug.Log("NULL");
        }
        mat.SetTexture("_Tex", (Texture)cubemap);
        AssetDatabase.CreateAsset(mat, "Assets/Resources/" + fileName + ".mat");
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/Transfer HDRI Folder")]
    static void TransferHDRIFolder()
    {
        string textureGUID = Selection.assetGUIDs[0];
        string directoryPath = AssetDatabase.GUIDToAssetPath(textureGUID);
        DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
        FileInfo[] files = directoryInfo.GetFiles();

        foreach (FileInfo fileInfo in files)
        {
            if (fileInfo.Name.Contains(".meta"))
            {
                continue;
            }
            string path = Path.Combine(directoryPath, fileInfo.Name);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            textureImporter.textureShape = TextureImporterShape.TextureCube;
            AssetDatabase.ImportAsset(path);

            // Material mat = new Material(Shader.Find("Skybox/Cubemap"));
            // string fileName = Path.GetFileNameWithoutExtension(path);
            // Cubemap cubemap = Resources.Load(fileName, typeof(Cubemap)) as Cubemap;
            // if (cubemap == null)
            // {
            //     Debug.Log("NULL");
            // }
            // mat.SetTexture("_Tex", (Texture)cubemap);
            // AssetDatabase.CreateAsset(mat, "Assets/Resources/HDRIMats/" + fileName + ".mat");
            // AssetDatabase.SaveAssets();

            // return;
        }
    }

    [MenuItem("Assets/Change HDRI name")]
    static void ChangeHDRIName()
    {
        int counter = 1;
        string directoryGUID = Selection.assetGUIDs[0];
        string directoryPath = AssetDatabase.GUIDToAssetPath(directoryGUID);
        DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
        FileInfo[] files = directoryInfo.GetFiles();

        foreach (FileInfo fileInfo in files)
        {
            if (fileInfo.Name.Contains(".meta"))
            {
                continue;
            }
            string path = Path.Combine(directoryPath, fileInfo.Name);
            Debug.Log(path);
            string info = AssetDatabase.RenameAsset(path, "skybox_" + counter.ToString() + ".hdr");
            // Debug.Log(info);
            counter += 1;
            // AssetDatabase.Refresh();
            // break;
        }
    }
}
