using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class ManualCalibrator : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject plane;
    public GameObject prefab;
    public float IOR;

    private int randomRotationCount = 1;
    private bool legal;
    private ImageSynthesis imageSynthesis;
    private string materialFormat;
    private string baseRoot;
    private string recordFolder;
    private string calibrationFolder;
    private int counter;
    private int calibrationCounter;
    private string currentCalibrationFolder;
    private Recorder recorder;
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

        switch_ = false;
        Application.targetFrameRate = 30;
        currentCalibrationFolder = "";
    }

    void LateUpdate()
    {
        if (!legal || counter >= randomRotationCount)
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
            ArrangeScene();
            CreateCalibrationSubfolder();
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
            calibrationCounter = 0;
            string recordFile = Path.Combine(recordFolder, string.Format("record_{0}.txt", counter));
            recorder.WriteFile(recordFile);
            counter += 1;
            switch_ = false;
        }
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
        baseRoot = Path.Combine("manual", prefab.name);
        DirectoryInfo info = new DirectoryInfo(baseRoot);
        if (info.Exists)
        {
            Debug.LogError(string.Format("Folder {0} exists, please check!", baseRoot));
            legal = false;
        }
        else
        {
            info.Create();
        }

        recordFolder = Path.Combine(baseRoot, "recorder");
        DirectoryInfo recordInfo = new DirectoryInfo(recordFolder);
        if (recordInfo.Exists)
        {
            Debug.LogError(string.Format("Folder {0} exists, please check!", recordFolder));
            legal = false;
        }
        else
        {
            recordInfo.Create();
        }
    }

    void ArrangeScene()
    {
        Material glassMat = LoadGlassMaterial(1, IOR);
        MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
        int length = meshRenderer.materials.Length;
        Material[] materials = new Material[length];
        for (int j = 0; j < length; ++j)
        {
            materials[j] = glassMat;
        }
        meshRenderer.materials = materials;

        recorder.cameraRotation = mainCamera.transform.eulerAngles;
        recorder.numberOfObjects = 1;
        recorder.categories.Add("manual");
        recorder.prefabPaths.Add(Path.Combine("Assets/Prefabs/manual", prefab.name + ".prefab"));
        recorder.positions.Add(prefab.transform.position);
        recorder.rotations.Add(prefab.transform.eulerAngles);
        recorder.scales.Add(prefab.transform.localScale[0]);
        recorder.IORs.Add(IOR);
    }

    void CreateCalibrationSubfolder()
    {
        currentCalibrationFolder = Path.Combine(calibrationFolder, counter.ToString());
        MakeDirectory(currentCalibrationFolder);
    }
}
