using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class GenerateRigidbodyPrefab: MonoBehaviour
{
    static private string prefabFolder = "Assets/Prefabs";
    static private string prefabSuffix = ".prefab";
    static private List<string> legalExtensions = new List<string>{".obj", ".dae", ".fbx"};
    static int counter = 0;
    static private bool CreatePrefabFromPath(string meshPath, string category, string prefabName="")
    {
        string targetFolder = Path.Combine(prefabFolder, category);
        DirectoryInfo targetFolderInfo = new DirectoryInfo(targetFolder);
        if (!targetFolderInfo.Exists)
        {
            targetFolderInfo.Create();
        }

        // Get file extension and prefab name
        string meshExtension = Path.GetExtension(meshPath);
        if (prefabName == "")
        {
            prefabName = Path.GetFileNameWithoutExtension(meshPath);
        }

        // Load mesh as prefab and instantiate it.
        GameObject go = AssetDatabase.LoadAssetAtPath(meshPath, typeof(UnityEngine.Object)) as GameObject;
        GameObject gameObject = Instantiate(go);
        
        // check the legality of mesh, i.e. whether it has MeshFilter Component.
        MeshFilter meshFilter = null;
        if (meshExtension == ".obj")
        {
            // .obj mesh will automatically generate a parent-child relation,
            // we do not want it, so we only extract the child.
            meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
            if (meshFilter == null)
            {
                return false;
            }
        }
        else if (meshExtension == ".dae" || meshExtension == ".fbx")
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return false;
            }
        }
        else
        {
            Debug.LogError("The file extension is illegal!");
            return false;
        }

        // Replace all materials to Glass
        MeshRenderer meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
        // Debug.Log(meshRenderer.sharedMaterials.Length);
        if (meshRenderer == null)
        {
            return false;
        }
        // int numMaterials = meshRenderer.sharedMaterials.Length;
        // Material[] sharedMaterials = new Material[numMaterials];
        // Material glass = Resources.Load("Glass", typeof(Material)) as Material;
        // for (int j = 0; j < numMaterials; ++j)
        // {
        //     sharedMaterials[j] = glass;
        // }
        // meshRenderer.sharedMaterials = sharedMaterials;

        string prefabPath = Path.Combine(targetFolder, prefabName + prefabSuffix);
        PrefabUtility.SaveAsPrefabAsset(meshFilter.gameObject, prefabPath);
        DestroyImmediate(gameObject);
        AssetDatabase.Refresh();
        Debug.Log(string.Format("Save mesh {0} as prefab at {1}.", meshPath, prefabPath));

        return true;
    }

    // [MenuItem("Assets/Create Prefabs/Create Prefabs From Selected Meshes")]
    // // You can select one or more meshes together.
    // static void CreatePrefabsFromSelectedMeshes()
    // {
    //     if (Selection.assetGUIDs.Length == 0)
    //     {
    //         Debug.Log("Didn't create any prefabs; Nothing was selected!");
    //         return;
    //     }
    //     foreach (string guid in Selection.assetGUIDs)
    //     {
    //         string meshPath = AssetDatabase.GUIDToAssetPath(guid);
    //         if (!GenerateRigidbodyPrefab.CreatePrefabFromPath(meshPath))
    //         {
    //             Debug.LogError(string.Format("{0} is not a valid mesh, please check!", meshPath));
    //         }
    //     }
    // }
    
    [MenuItem("Assets/Create Prefabs/Create Prefabs With Subfolders")]
    // The file system should be like this:
    // ROOT (this is what you select)
    // |-- MeshName1
    // |   |-- xxx.obj (or xxx.dae, xxx.fbx, ...)
    // |   |-- xxx.mtl (if needed)
    // |   `-- ...
    // |-- MeshName2
    // |   |-- xxx.obj (or xxx.dae, xxx.fbx, ...)
    // |   `-- ...
    // `-- ...
    // By default, there's only one mesh under each 'MeshNameX' folder.
    static void CreatePrefabsFromSelectedFolder()
    {
        counter = 1;
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
        // Debug.Log(rootInfo.Name);
        string category = rootInfo.Name;

        // Get all MeshNames and deal with them.
        DirectoryInfo[] meshNames = rootInfo.GetDirectories();
        foreach (DirectoryInfo meshNameInfo in meshNames)
        {
            // Debug.Log(meshNameInfo.Name);
            FileInfo[] files = meshNameInfo.GetFiles();
            foreach (FileInfo fileInfo in files)
            {
                if (!legalExtensions.Contains(fileInfo.Extension))
                {
                    continue;
                }
                string meshPath = Path.Combine(rootFolderPath, meshNameInfo.Name, fileInfo.Name);
                if (GenerateRigidbodyPrefab.CreatePrefabFromPath(meshPath, category, category + "_" + counter.ToString()))
                {
                    counter += 1;
                    break;
                }
                else
                {
                    Debug.Log(string.Format("Convertion on {0} failed!", meshPath));
                }
            }
        }
    }

    [MenuItem("Assets/Create Prefabs/Create Prefabs Without Subfolders")]
    // The file system should be like this:
    // ROOT (this is what you select)
    // |-- xxx.obj (or xxx.dae, xxx.fbx, ...)
    // |-- xxx.obj (or xxx.dae, xxx.fbx, ...)
    // `-- ...
    static void CreatePrefabsFromSelectedFolderWOSubfolder()
    {
        counter = 1;
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
        // Debug.Log(rootInfo.Name);
        string category = rootInfo.Name;

        // Get all MeshNames and deal with them.
        FileInfo[] meshNames = rootInfo.GetFiles();
        foreach (FileInfo fileInfo in meshNames)
        {
            // Debug.Log(meshNameInfo.Name);
            if (!legalExtensions.Contains(fileInfo.Extension))
            {
                continue;
            }
            string meshPath = Path.Combine(rootFolderPath, fileInfo.Name);
            if (GenerateRigidbodyPrefab.CreatePrefabFromPath(meshPath, category))
            {
                counter += 1;
            }
            else
            {
                Debug.Log(string.Format("Convertion on {0} failed!", meshPath));
            }
        }
    }
}
