using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class Calibrator : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject plane;
    public bool isTrain;

    private bool legal;
    private ImageSynthesis imageSynthesis;
    private string materialFormat;
    private string baseRoot;
    private string recordFolder;
    private string calibrationFolder;
    private FileInfo[] recordFiles;
    private int counter;
    private int numberOfCalibrations;
    private int calibrationCounter;
    private string currentImageId;
    private string currentCalibrationFolder;
    private Recorder recorder;
    private List<GameObject> prefabs;
    private bool switch_;

    void Start()
    {
        CheckLegality();

        // counter is used to control frame and produce images
        // I find in HDRP, after running, the very first few frames will be the same
        // i.e. I will save a few repeating images.
        // Thus, I change the counter to negative, so that it can avoid rendering same images.
        counter = -10;
        calibrationCounter = 0;

        recorder = new Recorder();
        // recorder.ParseFile("train/recorder/record_10.txt");
        // recorder.Show();
        materialFormat = "graycode_512_512/Materials/graycode_";
        prefabs = new List<GameObject>();
        
        calibrationFolder = Path.Combine(baseRoot, "calibration");
        MakeDirectory(calibrationFolder);

        // If calibration/refractive_flow is created, do not cover it.
        string refractiveFlowFolder = Path.Combine(calibrationFolder, "refractive_flow");
        DirectoryInfo refractiveFlowInfo = new DirectoryInfo(refractiveFlowFolder);
        if (refractiveFlowInfo.Exists)
        {
            legal = false;
            Debug.LogError("Refractive flow has been created, please check train or valid!");
        }

        DirectoryInfo recordInfo = new DirectoryInfo(recordFolder);
        recordFiles = recordInfo.GetFiles();
        // foreach (FileInfo info in recordFiles)
        // {
        //     Debug.Log(info.Name);
        // }
        numberOfCalibrations = recordFiles.Length;

        switch_ = false;
        Application.targetFrameRate = 30;
        currentImageId = "";
        currentCalibrationFolder = "";
    }

    void LateUpdate()
    {
        if (!legal || counter >= numberOfCalibrations)
        {
            return;
        }
        else if (counter < 0)
        {
            counter += 1;
            return;
        }

        if (calibrationCounter == 0)
        {
            string recordFile = recordFiles[counter].Name;
            recordFile = Path.Combine(recordFolder, recordFile);
            ArrangeScene(recordFile);
            CreateCalibrationSubfolder(recordFile);
            calibrationCounter += 1;
            return;
        }
        else if (calibrationCounter >= 1 && calibrationCounter <= 19)
        {
            if (!switch_)
            {
                Material graycode = Resources.Load(materialFormat + (calibrationCounter - 1).ToString(), typeof(Material)) as Material;
                plane.GetComponent<MeshRenderer>().material = graycode;
            }
            else
            {
                imageSynthesis.Save(
                    string.Format("{0}.png", calibrationCounter),
                    512, 512, currentCalibrationFolder, 0
                    );
                calibrationCounter += 1;
            }

            switch_ = !switch_;
            return;
        }
        else
        {
            DestroyAllObjects();
            calibrationCounter = 0;
            counter += 1;
            switch_ = false;
        }
    }

    GameObject LoadPrefab(string path)
    {
        print(path);
        GameObject prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        if (prefab == null)
        {
            print(path);
        }

        return Instantiate(prefab);
    }

    Material LoadGlassMaterial(int idx, float IOR)
    {
        string materialPath = "GlassMaterials/Glass" + idx.ToString();
        Material material = Resources.Load(materialPath, typeof(Material)) as Material;
        material.SetFloat("_Ior", IOR);

        return material;
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
            baseRoot = "train";
        }
        else
        {
            baseRoot = "valid";
        }
        recordFolder = Path.Combine(baseRoot, "recorder");
        DirectoryInfo info = new DirectoryInfo(recordFolder);
        if (!info.Exists)
        {
            Debug.LogError("No record folder, do you finish generating RGB?");
            legal = false;
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
            
            // Material glassMat = LoadGlassMaterial(i + 1, recorder.IORs[i]);
            // MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
            // int length = meshRenderer.materials.Length;
            // Material[] materials = new Material[length];
            // for (int j = 0; j < length; ++j)
            // {
            //     materials[j] = glassMat;
            // }
            // meshRenderer.materials = materials;

            prefabs.Add(prefab);
        }
    }

    void CreateCalibrationSubfolder(string recordFile)
    {
        currentImageId = recordFile.Split(new char[2]{'_', '.'})[1];
        currentCalibrationFolder = Path.Combine(calibrationFolder, currentImageId);
        MakeDirectory(currentCalibrationFolder);
    }
}
