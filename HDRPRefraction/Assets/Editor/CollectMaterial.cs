using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class CollectMaterial : MonoBehaviour
{
    [MenuItem("Assets/Collect Materials In This Folder")]
    static void CollectMaterialsInThisFolder()
    {
        if (Selection.assetGUIDs.Length == 0)
        {
            Debug.Log("Didn't create any prefabs; Nothing was selected!");
            return;
        }
        else if (Selection.assetGUIDs.Length > 1)
        {
            Debug.Log("Please select one folder!");
            return;
        }

        // Check validation of ROOT
        string rootFolderPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
        DirectoryInfo rootInfo = new DirectoryInfo(rootFolderPath);
        if (!rootInfo.Exists)
        {
            Debug.Log("You are not selecting a folder!");
            return;
        }
        string category = rootInfo.Name;
        
        string materialFolder = Path.Combine("Assets", "Resources", "TableMaterials");
        DirectoryInfo materialFolderInfo = new DirectoryInfo(materialFolder);
        int idx = materialFolderInfo.GetFiles().Length / 2;

        DirectoryInfo[] MeshNames = rootInfo.GetDirectories();
        foreach (DirectoryInfo meshName in MeshNames)
        {
            // Debug.Log(meshName.Name);
            string imageFolderPath = Path.Combine(rootFolderPath, meshName.Name, "images");
            // Debug.Log(imageFolderPath);
            DirectoryInfo imageFolderInfo = new DirectoryInfo(imageFolderPath);
            if (!imageFolderInfo.Exists)
            {
                // Debug.Log(meshName.Name);
                continue;
            }
            FileInfo[] images = imageFolderInfo.GetFiles();
            foreach (FileInfo image in images)
            {
                // Debug.Log(image.Extension);
                if (image.Extension == ".png" || image.Extension == ".jpg")
                {
                    // Debug.Log(meshName.Name);
                    string imagePath = Path.Combine(imageFolderPath, image.Name);
                    Texture img = AssetDatabase.LoadAssetAtPath(imagePath, typeof(Texture)) as Texture;
                    if (img == null)
                    {
                        Debug.Log("Null");
                        return;
                    }
                    Material mat = new Material(Shader.Find("HDRP/Lit"));
                    mat.SetTexture("_BaseColorMap", img);
                    AssetDatabase.CreateAsset(mat, Path.Combine(materialFolder, string.Format("table_mat_{0}.mat", idx)));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    idx += 1;
                }
            }
        }
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        if (prefab == null)
        {
            print(path);
        }

        return Instantiate(prefab);
    }
}
