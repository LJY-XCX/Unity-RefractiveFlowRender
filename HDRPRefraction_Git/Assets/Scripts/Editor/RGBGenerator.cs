using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.IO;
using System;
using UnityEditor;

public class RGBGenerator : MonoBehaviour
{
    [SerializeField] Volume volume;
    public Camera mainCamera;
    public List<GameObject> objects;
    public bool isTrain;
    public float trainWeight;
    public List<string> categories;
    public List<int> numberOfPrefabs;
    public int numberOfSkyboxes;
    public int numberOfObjectsLowerBound;
    public int numberOfObjectsUpperBound;
    public float xLowerBound = -1.5f;
    public float xUpperBound = 1.5f;
    public float yLowerBound = -1.5f;
    public float yUpperBound = 1.5f;
    public float scaleLowerBound = 0.8f;
    public float scaleUpperBound = 1.2f;
    public float rotationLowerBound = -180.0f;
    public float rotationUpperBound = 180.0f;
    public float IORLowerBound = 1.3f;
    public float IORUpperBound = 1.5f;
    public int startImageIndex = 0;
    public float numberOfImages;
    public string prefabFolder = "Assets/Prefabs";

    private bool legal;
    private ImageSynthesis imageSynthesis;
    private HDRISky sky;
    private string baseRoot;
    private string backgroundFolder;
    private string rgbFolder;
    private string recordFolder;
    private List<int> prefabsIndexLowerBound;
    private List<int> prefabsIndexUpperBound;
    private int skyboxIndexLowerBound;
    private int skyboxIndexUpperBound;
    private int counter;
    private Recorder recorder;
    private List<GameObject> prefabs;
    private bool switch_;

    void Start()
    {
        CheckLegality();
        MakeTrainOrValidDirectory();
        PartitionIndex();

        // counter is used to control frame and produce images
        // I find in HDRP, after running, the very first few frames will be the same
        // i.e. I will save a few repeating images.
        // Thus, I change the counter to negative, so that it can avoid rendering same images.
        counter = startImageIndex - 10;

        recorder = new Recorder();
        Application.targetFrameRate = 30;
        prefabs = new List<GameObject>();

        backgroundFolder = Path.Combine(baseRoot, "background");
        rgbFolder = Path.Combine(baseRoot, "RGB");
        recordFolder = Path.Combine(baseRoot, "recorder");
        MakeDirectory(backgroundFolder);
        MakeDirectory(rgbFolder);
        MakeDirectory(recordFolder);

        // switch_ is used to control saving background or rendered RGB image.
        // If background and rgb are saved in the same frame, rgb images will lose refractive objects.
        // Thus, I use two frames to render a tuple of (rgb, background) respectively.
        switch_ = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!legal || counter >= numberOfImages)
        {
            return;
        }
        else if (counter < startImageIndex)
        {
            counter += 1;
            return;
        }
        
        if (!switch_)
        {
            Resources.UnloadAsset(sky.hdriSky.value);
            RandomizeSkybox();
            RandomizeCamera();
            RandomizeObjects();
            imageSynthesis.Save(string.Format("rgb_{0}.png", counter), 512, 512, rgbFolder, 0);
        }
        if (switch_)
        {
            DestroyRandomizedObjects();
            imageSynthesis.Save(string.Format("background_{0}.png", counter), 512, 512, backgroundFolder, 0);
            string recordFile = Path.Combine(recordFolder, string.Format("record_{0}.txt", counter));
            recorder.WriteFile(recordFile);
            counter += 1;
        }
        
        switch_ = !switch_;
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

        // Check sky
        volume.profile.TryGet(out sky);
        if (sky == null)
        {
            Debug.LogError("The volume is not sepicied with a HDRI sky.");
            legal = false;
        }

        // Check category and numberOfPrefabs
        if (categories.Count != numberOfPrefabs.Count)
        {
            Debug.LogError("List categories and numberOfPrefabs must have same length.");
            legal = false;
        }

        // Check ImageSyntesis
        imageSynthesis = mainCamera.GetComponent<ImageSynthesis>();
        if (imageSynthesis == null)
        {
            Debug.LogError("Main camera doesn't have component imageSynthesis.");
            legal = false;
        }
    }

    void MakeTrainOrValidDirectory()
    {
        if (isTrain)
        {
            baseRoot = "train";
        }
        else
        {
            baseRoot = "valid";
        }
        MakeDirectory(baseRoot);
    }

    void MakeDirectory(string path)
    {
        DirectoryInfo info = new DirectoryInfo(path);
        if (!info.Exists)
        {
            info.Create();
        }
    }

    void PartitionIndex()
    {
        int length = categories.Count;
        prefabsIndexLowerBound = new List<int>(length);
        prefabsIndexUpperBound = new List<int>(length);
        
        // For training set, we use the first trainWeight percentage objects.
        // For validation set, we use the last (1 - trainWeight) percentage objects.
        if (isTrain)
        {
            skyboxIndexLowerBound = 1;
            skyboxIndexUpperBound = Convert.ToInt32((float)numberOfSkyboxes * trainWeight);
            for (int i = 0; i < length; ++i)
            {
                prefabsIndexLowerBound.Add(1);
                prefabsIndexUpperBound.Add(Convert.ToInt32((float)numberOfPrefabs[i] * trainWeight));
            }
        }
        else
        {
            skyboxIndexLowerBound = Convert.ToInt32((float)numberOfSkyboxes * trainWeight) + 1;
            skyboxIndexUpperBound = numberOfSkyboxes;
            for (int i = 0; i < length; ++i)
            {
                prefabsIndexLowerBound.Add(Convert.ToInt32((float)numberOfPrefabs[i] * trainWeight) + 1);
                prefabsIndexUpperBound.Add(numberOfPrefabs[i]);
            }
        }
        // print(skyboxIndexLowerBound);
        // print(skyboxIndexUpperBound);
        // for (int i = 0; i < length; ++i)
        // {
        //     print(prefabsIndexLowerBound[i]);
        //     print(prefabsIndexUpperBound[i]);
        // }
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

    Vector3 RandomRotation()
    {
        Vector3 rotation = new Vector3();
        rotation.x = RandomFloat(rotationLowerBound, rotationUpperBound);
        rotation.y = RandomFloat(rotationLowerBound, rotationUpperBound);
        rotation.z = RandomFloat(rotationLowerBound, rotationUpperBound);

        return rotation;
    }

    Vector3 RandomConstrainedPosition()
    {
        Vector3 position = new Vector3();
        position.x = RandomFloat(xLowerBound, xUpperBound);
        position.y = RandomFloat(yLowerBound, yUpperBound);
        position.z = objects[0].transform.localPosition.z;

        return position;
    }

    int RandomInt(int minValue, int maxValue)
    {
        return UnityEngine.Random.Range(minValue, maxValue);
    }

    float RandomFloat(float minValue, float maxValue)
    {
        return UnityEngine.Random.Range(minValue, maxValue);
    }

    void RandomizeSkybox()
    {
        int skyboxId = RandomInt(skyboxIndexLowerBound, skyboxIndexUpperBound);
        string skyboxPath = Path.Combine("HDRIImages", "skybox_" + skyboxId.ToString());
        Cubemap cubemap = Resources.Load(skyboxPath, typeof(Cubemap)) as Cubemap;
        if (cubemap != null)
        {
            sky.hdriSky.value = cubemap;
            recorder.skyboxPath = skyboxPath;
        }
        else
        {
            Debug.LogError(string.Format("No skybox named {0}.", skyboxPath));
            legal = false;
        }
    }

    void RandomizeCamera()
    {
        Vector3 eularAngles = Vector3.zero;
        eularAngles.x = RandomFloat(rotationLowerBound / 4, rotationUpperBound / 4);
        eularAngles.y = RandomFloat(rotationLowerBound, rotationUpperBound);
        mainCamera.transform.localEulerAngles = eularAngles;

        recorder.cameraRotation = mainCamera.transform.eulerAngles;
    }

    void RandomizeObjects()
    {
        int numberOfObjects = RandomInt(numberOfObjectsLowerBound, numberOfObjectsUpperBound);
        recorder.numberOfObjects = numberOfObjects;
        for (int i = 0; i < numberOfObjects; ++i)
        {
            // Random category, prefabId 
            int categoryId = RandomInt(0, categories.Count);
            string category = categories[categoryId];
            int PrefabId = RandomInt(prefabsIndexLowerBound[categoryId], prefabsIndexUpperBound[categoryId]);
            string prefabPath = Path.Combine(prefabFolder, category, category + "_" + PrefabId.ToString() + ".prefab");
            GameObject prefab = LoadPrefab(prefabPath);

            // Randomize object position, rotation, scale
            objects[i].transform.localPosition = RandomConstrainedPosition();
            prefab.transform.localPosition = objects[i].transform.position;
            prefab.transform.localEulerAngles = RandomRotation();
            float scale = RandomFloat(scaleLowerBound, scaleUpperBound);
            prefab.transform.localScale = new Vector3(scale, scale, scale);

            // Randomize IOR
            float IOR = RandomFloat(IORLowerBound, IORUpperBound);
            MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();

            int length = meshRenderer.materials.Length;
            Material[] materials = new Material[length];
            Material glassMat = LoadGlassMaterial(i + 1, IOR);
            for (int j = 0; j < length; ++j)
            {
                materials[j] = glassMat;
            }
            meshRenderer.materials = materials;

            recorder.categories.Add(category);
            recorder.prefabPaths.Add(prefabPath);
            recorder.positions.Add(prefab.transform.position);
            recorder.rotations.Add(prefab.transform.eulerAngles);
            recorder.scales.Add(scale);
            recorder.IORs.Add(IOR);

            prefabs.Add(prefab);
        }
    }

    void DestroyRandomizedObjects()
    {
        foreach (GameObject gameObject in prefabs)
        {
            Destroy(gameObject);
        }
        prefabs.Clear();
    }
}
