using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class GraspMaskGenerator: MonoBehaviour
{

    public Camera mainCamera;
    public bool isTrain;

    private bool legal;
    private ImageSynthesis imageSynthesis;
    private string baseRoot;
    private string recordFolder;
    private string calibrationFolder;
    private string maskFolder;
    private string normalFolder;
    private string depthFolder;
    private FileInfo[] recordFiles;
    private int counter;
    private int numberOfMasks;
    private string currentImageId;
    private string currentCalibrationFolder;
    private Recorder recorder;
    private List<GameObject> prefabs;
    private int switch_;

    void Start()
    {
        CheckLegality();
        counter = -10;

        recorder = new Recorder();
        prefabs = new List<GameObject>();

        maskFolder = Path.Combine(baseRoot, "mask");
        MakeDirectory(maskFolder);

        depthFolder = Path.Combine(baseRoot, "depth");
        MakeDirectory(depthFolder);

        normalFolder = Path.Combine(baseRoot, "normal");
        MakeDirectory(normalFolder);

        DirectoryInfo recordInfo = new DirectoryInfo(recordFolder);
        recordFiles = recordInfo.GetFiles();
        numberOfMasks = recordFiles.Length;

        switch_ = 0;
        // Application.targetFrameRate = 30;
        currentImageId = "";
        currentCalibrationFolder = "";
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

        if (switch_ == 0)
        {
            string recordFile = recordFiles[counter].Name;
            currentImageId = recordFile.Split(new char[2]{'_', '.'})[1];
            currentCalibrationFolder = Path.Combine(calibrationFolder, currentImageId);
            recordFile = Path.Combine(recordFolder, recordFile);
            ArrangeScene(recordFile);

            imageSynthesis.OnSceneChange();
            imageSynthesis.OnCameraChange();
            
            imageSynthesis.Save("0.png", 512, 512, currentCalibrationFolder, 1);
            imageSynthesis.Save(string.Format("mask_{0}.png", currentImageId), 512, 512, maskFolder, 2);
        }
        else if (switch_ == 1)
        {
            GameObject table = LoadPrefab("Assets/Prefabs/Plane.prefab");
            prefabs.Add(table);
        }
        else if (switch_ == 2)
        {
            imageSynthesis.Save(string.Format("depth_{0}.png", currentImageId), 512, 512, depthFolder, 3);
            imageSynthesis.Save(string.Format("normal_{0}.png", currentImageId), 512, 512, normalFolder, 4);
        }
        else
        {
            DestroyAllObjects();
            counter += 1;
        }
        
        switch_ = (switch_ + 1) % 4;
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
        if (isTrain)
        {
            baseRoot = "C:/Users/jiyu/Desktop/Unity-RefractiveFlowRender/HDRPRefraction/train";
        }
        else
        {
            baseRoot = "C:/Users/jiyu/Desktop/Unity-RefractiveFlowRender/HDRPRefraction/valid";
        }

        // Check record folder
        recordFolder = Path.Combine(baseRoot, "recorder");
        if (!CheckExistence(recordFolder))
        {
            Debug.LogError("No record folder, do you finish generating RGB?");
            legal = false;
        }

        // Check Calibration folder
        calibrationFolder = Path.Combine(baseRoot, "calibration");
        if (!CheckExistence(calibrationFolder))
        {
            Debug.LogError("No calibration folder, do you finish calibrating?");
            legal = false;
        }

        // If calibration/refractive_flow is created, do not cover it.
        string refractiveFlowFolder = Path.Combine(calibrationFolder, "refractive_flow");
        DirectoryInfo refractiveFlowInfo = new DirectoryInfo(refractiveFlowFolder);
        if (refractiveFlowInfo.Exists)
        {
            legal = false;
            Debug.LogError("Refractive flow has been created, please check train or valid!");
        }
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
        mainCamera.transform.localPosition = recorder.cameraPosition;
        mainCamera.transform.localEulerAngles = recorder.cameraRotation;
        int numberOfObjects = recorder.numberOfObjects;
        for (int i = 0; i < numberOfObjects; ++i)
        {
            GameObject prefab = LoadPrefab(recorder.prefabPaths[i]);
            Destroy(prefab.GetComponent<Rigidbody>());
            Destroy(prefab.GetComponent<MeshCollider>());
            prefab.transform.localPosition = recorder.positions[i];
            prefab.transform.localEulerAngles = recorder.rotations[i];
            prefab.transform.localScale = new Vector3(recorder.scales[i], recorder.scales[i], recorder.scales[i]);
            
            prefabs.Add(prefab);
        }
    }

    void CreateCalibrationSubfolder(string recordFile)
    {
        currentImageId = recordFile.Split(new char[2]{'_', '.'})[1];
        currentCalibrationFolder = Path.Combine(calibrationFolder, currentImageId);
        MakeDirectory(currentCalibrationFolder);
    }

    bool CheckExistence(string folderPath)
    {
        DirectoryInfo info = new DirectoryInfo(folderPath);
        return info.Exists;
    }
}