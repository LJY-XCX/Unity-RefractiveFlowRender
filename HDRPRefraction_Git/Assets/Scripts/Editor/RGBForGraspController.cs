using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.IO;
using System;
using UnityEditor;

public class RGBForGraspController : MonoBehaviour
{
    [SerializeField] Volume volume;
    public Camera mainCamera;
    public bool isTrain;
    public GameObject table;

    public List<string> categories;
    public List<int> numberOfPrefabs;
    public int numberOfSkyboxes;
    public int numberOfTableMaterials;
    public int numberOfObjectsLowerBound;
    public int numberOfObjectsUpperBound;
    public float xLowerBound = -1.5f;
    public float xUpperBound = 1.5f;
    public float zLowerBound = -1.5f;
    public float zUpperBound = 1.5f;
    public float cameraXLowerBound;
    public float cameraXUpperrBound;
    public float cameraYLowerBound;
    public float cameraYUpperrBound;
    public float cameraZLowerBound;
    public float cameraZUpperrBound;
    public float scaleLowerBound = 0.8f;
    public float scaleUpperBound = 1.2f;
    public float rotationLowerBound = -180.0f;
    public float rotationUpperBound = 180.0f;
    public float IORLowerBound = 1.3f;
    public float IORUpperBound = 1.5f;
    public int startImageIndex = 0;
    public float numberOfImages;
    public int numberOfPerspectivesPerLoad = 1;
    public int waitForFallingFrames = 10;
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
    private int waitingForFallingCounter;
    private int perspectiveCounter;
    private Recorder recorder;
    private List<GameObject> prefabs;
    private List<int> hdriIndices;
    private int switch_;
    private int photo_switch_;

    void Start()
    {
        CheckLegality();
        MakeTrainOrValidDirectory();
        PartitionIndex();
        ReadSelectedSkyboxList();

        // counter is used to control frame and produce images
        // I find in HDRP, after running, the very first few frames will be the same
        // i.e. I will save a few repeating images.
        // Thus, I change the counter to negative, so that it can avoid rendering same images.
        counter = startImageIndex - 10;
        waitingForFallingCounter = waitForFallingFrames;
        perspectiveCounter = 0;

        recorder = new Recorder();
        Application.targetFrameRate = 30;
        prefabs = new List<GameObject>();

        // backgroundFolder = Path.Combine(baseRoot, "background");
        rgbFolder = Path.Combine(baseRoot, "RGB");
        recordFolder = Path.Combine(baseRoot, "recorder");
        // MakeDirectory(backgroundFolder);
        MakeDirectory(rgbFolder);
        MakeDirectory(recordFolder);

        // switch_ is used to control saving background or rendered RGB image.
        // If background and rgb are saved in the same frame, rgb images will lose refractive objects.
        // Thus, I use two frames to render a tuple of (rgb, background) respectively.
        switch_ = 0;
        photo_switch_ = 0;

        // Re-calculate the number of images
        // numberOfImages = numberOfImages * numberOfPerspectivesPerLoad;
    }

    // Update is called once per frame
    void FixedUpdate()
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
        
        if (switch_ == 0)
        {
            Resources.UnloadAsset(sky.hdriSky.value);
            RandomizeSkybox();
            RandomizeObjects();
            waitingForFallingCounter = 0;
        }
        else if (switch_ == 1)
        {
            if (waitingForFallingCounter < waitForFallingFrames)
            {
                waitingForFallingCounter += 1;
                // Debug.Log(string.Format("Waiting for falling in counter {0}", counter));
                return;
            }
            foreach (var prefab in prefabs)
            {
                Rigidbody rb = prefab.GetComponent<Rigidbody>();
                Destroy(rb);
            }
        }
        else if (switch_ == 2)
        {
            foreach (var prefab in prefabs)
            {
                recorder.positions.Add(prefab.transform.position);
                recorder.rotations.Add(prefab.transform.eulerAngles);
            }
            perspectiveCounter = 0;
        }
        else if (switch_ == 3)
        {
            if (perspectiveCounter < numberOfPerspectivesPerLoad)
            {
                if (photo_switch_ == 0)
                {
                    RandomizeCamera();
                    
                    // Debug.Log(string.Format("Ready for photo in counter {0}", counter));
                }
                else if (photo_switch_ == 1 || photo_switch_ == 2)
                {
                    recorder.cameraPosition = mainCamera.transform.position;
                    recorder.cameraRotation = mainCamera.transform.eulerAngles;
                    RandomizeTableMaterials();
                }
                else if (photo_switch_ == 100)
                {
                    imageSynthesis.Save(string.Format("rgb_{0}.png", counter * numberOfPerspectivesPerLoad + perspectiveCounter), 512, 512, rgbFolder, 0);
                    string recordFile = Path.Combine(recordFolder, string.Format("record_{0}.txt", counter * numberOfPerspectivesPerLoad + perspectiveCounter));
                    recorder.WriteFileWithoutClear(recordFile);
                    perspectiveCounter += 1;
                }
                photo_switch_ = (photo_switch_ + 1) % 101;
                return;
            }
        }
        else if (switch_ == 10) // This is because we wait for one more frame for photo.
        {
            DestroyRandomizedObjects();
            recorder.Clear();
            counter += 1;
        }
        
        switch_ = (switch_ + 1) % 11;
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

        skyboxIndexLowerBound = 0;
        skyboxIndexUpperBound = numberOfSkyboxes;
        for (int i = 0; i < length; ++i)
        {
            prefabsIndexLowerBound.Add(1);
            prefabsIndexUpperBound.Add(numberOfPrefabs[i]);
        }
    }

    void ReadSelectedSkyboxList()
    {
        hdriIndices = new List<int>();
        char[] splitChars = new char[2]{'\n', ' '};
        string[] splitWords;
        using (StreamReader sr = new StreamReader("Assets/Resources/HDRIImages/selected_hdri.txt"))
        {
            for (int i = 0; i < 50; ++i)
            {
                splitWords = sr.ReadLine().Split(splitChars);
                int idx = Convert.ToInt32(splitWords[0]);
                hdriIndices.Add(idx);
            }
        }
    }

    GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        // print(path);
        if (prefab == null)
        {
            print(path);
        }

        return Instantiate(prefab);
    }

    Material LoadGlassMaterial(float IOR)
    {
        string materialPath = "Glass";
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
        position.y = 0.1f;
        position.z = RandomFloat(zLowerBound, zUpperBound);

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
        string skyboxPath = Path.Combine("HDRIImages", "skybox_" + hdriIndices[skyboxId].ToString());
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

    void RandomizeTableMaterials()
    {
        int tableMaterialId = RandomInt(0, numberOfTableMaterials);
        Material table_mat = Resources.Load("TableMaterials/table_mat_" + tableMaterialId.ToString(), typeof(Material)) as Material;
        MeshRenderer meshRenderer = table.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        materials[0] = table_mat;
        meshRenderer.materials = materials;
    }

    void RandomizeCamera()
    {
        // Randomize camera position
        Vector3 position = Vector3.zero;
        position.x = RandomFloat(cameraXLowerBound, cameraXUpperrBound);
        position.y = RandomFloat(cameraYLowerBound, cameraYUpperrBound);
        position.z = RandomFloat(cameraZLowerBound, cameraZUpperrBound);

        mainCamera.transform.localPosition = position;
        mainCamera.transform.LookAt(new Vector3(0, 0, 0));

        Debug.Log(position);
    }

    void RandomizeObjects()
    {
        int numberOfObjects = RandomInt(numberOfObjectsLowerBound, numberOfObjectsUpperBound);
        recorder.numberOfObjects = numberOfObjects;
        float planewidth = 0.7f;

        for (int i = 0; i < numberOfObjects; ++i)
        {
            // Random category, prefabId 
            int categoryId = RandomInt(0, categories.Count);
            string category = categories[categoryId];
            int PrefabId = RandomInt(prefabsIndexLowerBound[categoryId], prefabsIndexUpperBound[categoryId]);
            string prefabPath = Path.Combine(prefabFolder, category, category + "." + PrefabId.ToString().PadLeft(3, '0') + ".prefab");
            // string recordPrefabPath = Path.Combine(prefabFolder, category + "_non_rigid", category + "." + PrefabId.ToString().PadLeft(3, '0') + ".prefab");
            GameObject prefab = LoadPrefab(prefabPath);

            Vector3 PrefabShape = prefab.GetComponent<Renderer>().bounds.size;
            float maxshape = Mathf.Max(PrefabShape.x, PrefabShape.y, PrefabShape.z);
            float minshape = Mathf.Min(PrefabShape.x, PrefabShape.y, PrefabShape.z);
            float scale = Mathf.Min(planewidth / maxshape * RandomFloat(scaleLowerBound, scaleUpperBound), planewidth * 0.2f / minshape);

            // Randomize object position, rotation, scale
            prefab.transform.localPosition = RandomConstrainedPosition();
            prefab.transform.localEulerAngles = RandomRotation();
            prefab.transform.localScale = new Vector3(scale, scale, scale);

            // Randomize IOR
            float IOR = RandomFloat(IORLowerBound, IORUpperBound);
            MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();

            int length = meshRenderer.materials.Length;
            Material[] materials = new Material[length];
            Material glassMat = LoadGlassMaterial(IOR);
            for (int j = 0; j < length; ++j)
            {
                materials[j] = glassMat;
            }
            meshRenderer.materials = materials;

            recorder.categories.Add(category);
            recorder.prefabPaths.Add(prefabPath);
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
