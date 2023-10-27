using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ManualMaskGenerator: MonoBehaviour
{

    public Camera mainCamera;

    private bool legal;
    private ImageSynthesis imageSynthesis;
    private string baseRoot;
    private string recordFolder;
    private DirectoryInfo[] objectFolders;
    private int counter;
    private int numberOfMasks;
    private Recorder recorder;
    private List<GameObject> prefabs;
    private bool switch_;

    void Start()
    {
        CheckLegality();
        counter = -10;

        recorder = new Recorder();
        prefabs = new List<GameObject>();

        DirectoryInfo baseRootInfo = new DirectoryInfo(baseRoot);
        objectFolders = baseRootInfo.GetDirectories();
        numberOfMasks = objectFolders.Length;

        // print(objectFolders[0].Name);

        switch_ = false;
    }

    void LateUpdate()
    {
        if (!legal || counter >= numberOfMasks)
        {
            return;
        }
        else if (counter < 0)
        {
            counter += 1;
            return;
        }

        if (!switch_)
        {
            string objectName = objectFolders[counter].Name;
            string recordFile = Path.Combine(baseRoot, objectName, "recorder", "record_0.txt");
            string currentCalibrationFolder = Path.Combine(baseRoot, objectName, "calibration", "0");
            ArrangeScene(recordFile);

            imageSynthesis.OnSceneChange();
            imageSynthesis.OnCameraChange();
            
            imageSynthesis.Save("0.png", 512, 512, currentCalibrationFolder, 1);
        }
        else
        {
            DestroyAllObjects();
            counter += 1;
        }
        
        switch_ = !switch_;
    }

    GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        if (prefab == null)
        {
            print(path);
        }

        return Instantiate(prefab);
    }

    void MakeDirectory(string path)
    {
        DirectoryInfo info = new DirectoryInfo(path);
        if (!info.Exists)
        {
            info.Create();
        }
    }

    void CheckLegality()
    {
        legal = true;

        // Check camera
        if (mainCamera == null)
        {
            Debug.LogError("The main camera is null!");
            legal = false;
        }

        // Check ImageSyntesis
        imageSynthesis = mainCamera.GetComponent<ImageSynthesis>();
        if (imageSynthesis == null)
        {
            Debug.LogError("Main camera doesn't have component imageSynthesis.");
            legal = false;
        }

        // Check base root
        baseRoot = "D:/A_haoyuan/Unity-RefractiveFlowRender/HDRPRefraction/manual";

    }
    void DestroyAllObjects()
    {
        foreach (GameObject gameObject in prefabs)
        {
            Destroy(gameObject);
        }
        prefabs.Clear();
    }

    void ArrangeScene(string recordFile)
    {
        recorder.ParseFile(recordFile);
        mainCamera.transform.localEulerAngles = recorder.cameraRotation;
        int numberOfObjects = recorder.numberOfObjects;
        for (int i = 0; i < numberOfObjects; ++i)
        {
            GameObject prefab = LoadPrefab(recorder.prefabPaths[i]);
            prefab.transform.localPosition = recorder.positions[i];
            prefab.transform.localEulerAngles = recorder.rotations[i];
            prefab.transform.localScale = new Vector3(recorder.scales[i], recorder.scales[i], recorder.scales[i]);
            
            prefabs.Add(prefab);
        }
    }

    bool CheckExistence(string folderPath)
    {
        DirectoryInfo info = new DirectoryInfo(folderPath);
        return info.Exists;
    }
}