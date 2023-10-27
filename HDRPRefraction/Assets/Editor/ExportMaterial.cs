using System.IO;
using UnityEngine;
using UnityEditor;

public class ExtractMaterials : EditorWindow
{
    [MenuItem("Assets/一键生成材质球", false, 1)]
    static void CreateMaterialsFromFBX()
    {
        UnityEngine.Object[] gameObjects = Selection.objects;
        string[] strs = Selection.assetGUIDs;

        if (gameObjects.Length > 0)
        {
            int gameNum = gameObjects.Length;
            for (int i = 0; i < gameNum; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(strs[i]);
                //Debug.Log(assetPath); //具体到fbx的路径
                string materialFolder = Path.GetDirectoryName(assetPath) + "/Materials";
                // 如果不存在该文件夹则创建一个新的
                if (!AssetDatabase.IsValidFolder(materialFolder))
                {
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(assetPath), "Materials");
                }
                // 获取assetPath下所有资源
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool isCreate = false;
                foreach (Object item in assets)
                {
                    if (typeof(Material) == item?.GetType())//找到fbx里面的材质
                    {
                        Debug.Log("找到材质文件：" + item);
                        string path = System.IO.Path.Combine(materialFolder, item.name) + ".mat";//提取后的名字
                        if (System.IO.File.Exists(path))
                        {
                            Debug.Log("该材质已存在");

                            var assetImporter = AssetImporter.GetAtPath(assetPath);
                            var clone = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                            assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(item), clone);
                            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                        }
                        else
                        {
                            path = AssetDatabase.GenerateUniqueAssetPath(path);
                            string value = AssetDatabase.ExtractAsset(item, path);
                            if (string.IsNullOrEmpty(value))
                            {
                                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                                isCreate = true;
                            }
                        }
                    }
                }

                AssetDatabase.Refresh();
                if (isCreate)
                    Debug.Log("自动创建材质球成功：" + materialFolder);
            }
        }
        else
        {
            Debug.LogError("请选中需要一键生成材质球的模型");
        }
    }
}