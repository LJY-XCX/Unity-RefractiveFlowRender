using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.IO;
using Unity.Collections;
using System.Diagnostics.Contracts;
using RFUniverse.Attributes;

public class HDRPImageGenerator : MonoBehaviour
{
    public HDRPCameraAttr cameraAttr;
    public ImageSynthesis imageSynthesis;
    public Volume volume;
    public Camera mainCamera;
    public ActiveLightSensorAttr activeLightSensor;
    public MeshRenderer table;
    public MeshRenderer grayCodePlane;
    public bool isTrain;

    public bool enablePathTracing = true;
    public string baseDir => $"{Path.GetDirectoryName(Application.dataPath)}/{(isTrain ? "train" : "valid")}";
    public string rgbDir => $"{baseDir}/RGB";
    public string depthDir => $"{baseDir}/depth";
    public string normalDir => $"{baseDir}/normal";
    public string calibrationDir => $"{baseDir}/calibration";
    public string maskDir => $"{baseDir}/mask";
    public string recorderDir => $"{baseDir}/recorder";

    public string irLeftDir => $"{baseDir}/ir_left";

    public string irRightDir => $"{baseDir}/ir_right";



    public GameObject[] prefabs;
    public Material[] tableMaterials;
    public Cubemap[] skyboxs;
    public Material[] grayCodes;
    public Material[] OpaqueMaterials;
    public GameObject[] goblets;

    public Vector2Int objectCountRange = new Vector2Int(2, 4);
    public float xLowerBound = -1.5f;
    public float xUpperBound = 1.5f;
    public float zLowerBound = -1.5f;
    public float zUpperBound = 1.5f;
    public Transform center1;
    public Vector2 cameraRotateBound = new Vector2();
    public Transform center2;
    public Vector2 cameraPitchBound = new Vector2();
    public Vector2 cameraDistanceBound = new Vector2();
    public Vector3 tableLowerScale = new Vector3();
    public Vector3 tableUpperScale = new Vector3();
    public Vector2 skyMulBound = new Vector2();
    public float scaleLowerBound = 0.15f;
    public float scaleUpperBound = 0.25f;
    public float rotationLowerBound = -180.0f;
    public float rotationUpperBound = 180.0f;
    public float IORLowerBound = 1.05f;
    public float IORUpperBound = 1.2f;
    public float MetallicLowerBound = 0.04f;
    public float MetallicUpperBound = 0.1f;
    public int startImageIndex = 0;
    public float numberOfImages = 50;
    public int OpaqueRatio = 10;

    public int waitForFallingFrames = 10;
    public int waitForRenderingFrames = 30;

    private HDRISky sky;
    private PathTracing pathTracing;
    private VisualEnvironment env;

    private Recorder recorder;

    void Start()
    {
        if (!Directory.Exists(baseDir))
            Directory.CreateDirectory(baseDir);
        if (!Directory.Exists(rgbDir))
            Directory.CreateDirectory(rgbDir);
        if (!Directory.Exists(depthDir))
            Directory.CreateDirectory(depthDir);
        if (!Directory.Exists(normalDir))
            Directory.CreateDirectory(normalDir);
        if (!Directory.Exists(calibrationDir))
            Directory.CreateDirectory(calibrationDir);
        if (!Directory.Exists(maskDir))
            Directory.CreateDirectory(maskDir);
        if (!Directory.Exists(recorderDir))
            Directory.CreateDirectory(recorderDir);
        if (!Directory.Exists(irLeftDir))
            Directory.CreateDirectory(irLeftDir);
        if (!Directory.Exists(irRightDir))
            Directory.CreateDirectory(irRightDir);

        volume.profile.TryGet(out sky);
        volume.profile.TryGet(out pathTracing);
        volume.profile.TryGet(out env);
        recorder = new Recorder();

        StartCoroutine(Go());
    }
    // this is for the active sensor
    // fx=fy=400 is around 65 fov
    // 500 ~= 54.6 deg fov
    float[,] intrinsicMatrix = new float[,]
                {
                    { 700, 0, 512 },
                    { 0, 700, 512 },
                    { 0, 0, 1}};
    // Update is called once per frame
    IEnumerator Go()
    {
        for (int i = startImageIndex; i < startImageIndex + numberOfImages; i++)
        {
            recorder.Clear();
            RandomizeSkybox();
            RandomizeCamera();
            RandomizeObjects();
            RandomizeTableMaterials();
            for (int frame = 0; frame < waitForFallingFrames; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            foreach (var obj in currentObj)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                Destroy(rb);
            }
            if (enablePathTracing)
            {
                pathTracing.active = true;
                for (int frame = 0; frame < waitForRenderingFrames; frame++)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            else
                pathTracing.active = false;
            Texture2D tex = cameraAttr.GetRGB(512, 512);
            File.WriteAllBytes($"{rgbDir}/rgb_{i}.png", tex.EncodeToPNG());
            pathTracing.active = false;

            if (!Directory.Exists($"{calibrationDir}/{i}"))
                Directory.CreateDirectory($"{calibrationDir}/{i}");
            table.gameObject.SetActive(false);
            grayCodePlane.gameObject.SetActive(true);

            env.active = false;
            for (int code = 0; code < grayCodes.Length; code++)
            {
                grayCodePlane.material = grayCodes[code];
                tex = cameraAttr.GetGrayCode(512, 512);
                File.WriteAllBytes($"{calibrationDir}/{i}/{code + 1}.png", tex.EncodeToPNG());
            }
            table.gameObject.SetActive(true);
            grayCodePlane.gameObject.SetActive(false);
            env.active = true;

            tex = cameraAttr.GetID(512, 512);
            File.WriteAllBytes($"{maskDir}/mask_{i}.png", tex.EncodeToPNG());
            tex = cameraAttr.GetDepth(512, 512, 0.0f, 3.0f);
            File.WriteAllBytes($"{depthDir}/depth_{i}.png", tex.EncodeToPNG());
            tex = cameraAttr.GetNormal(512, 512);
            File.WriteAllBytes($"{normalDir}/normal_{i}.png", tex.EncodeToPNG());

            env.active = false;

            tex = activeLightSensor.GetCameraIR(intrinsicMatrix, true);
            File.WriteAllBytes($"{irLeftDir}/ir_left_{i}.png", tex.EncodeToPNG());
            tex = activeLightSensor.GetCameraIR(intrinsicMatrix, false);
            File.WriteAllBytes($"{irRightDir}/ir_right_{i}.png", tex.EncodeToPNG());
            env.active = true;

            recorder.WriteFileWithoutClear($"{recorderDir}/record_{i}.txt");
        }
        yield break;
    }


    Material LoadGlassMaterial(float IOR, float Metallic)
    {
        string materialPath = "Glass";
        Material material = Resources.Load(materialPath, typeof(Material)) as Material;
        material.SetFloat("_Ior", IOR);
        material.SetFloat("_Metallic", Metallic);

        return material;
    }

    Material LoadOpaqueMaterial()
    {
        int randomIndex = Random.Range(0, OpaqueMaterials.Length);
       
        Material material = OpaqueMaterials[randomIndex];

        // Pick a random material from the loaded materials

        return material;

    }


    Vector3 RandomConstrainedPosition()
    {
        Vector3 position = new Vector3();
        position.x = Random.Range(xLowerBound, xUpperBound);
        position.y = 0.1f;
        position.z = Random.Range(zLowerBound, zUpperBound);

        return position;
    }


    void RandomizeSkybox()
    {
        int skyboxIndex = Random.Range(0, skyboxs.Length);
        Cubemap cubemap = skyboxs[skyboxIndex];
        sky.hdriSky.value = cubemap;
        sky.multiplier.value = Random.Range(skyMulBound[0], skyMulBound[1]);

        recorder.skyboxPath = cubemap.name;
        recorder.skyMul = sky.multiplier.value;
    }

    void RandomizeTableMaterials()
    {
        int materialIndex = Random.Range(0, tableMaterials.Length);
        table.material = tableMaterials[materialIndex];
        table.transform.localScale = Vector3.Lerp(tableLowerScale, tableUpperScale, Random.value);

        recorder.tableMat = table.material.name;
        recorder.tableScale = table.transform.localScale;
    }

    void RandomizeCamera()
    {
        // Randomize camera position
        Vector3 position = Vector3.zero;
        center1.localEulerAngles = new Vector3(0, Random.Range(cameraRotateBound[0], cameraRotateBound[1]), 0);
        center2.localEulerAngles = new Vector3(Random.Range(cameraPitchBound[0], cameraPitchBound[1]), 0, 0);
        mainCamera.transform.localPosition = new Vector3(0, 0, Random.Range(cameraDistanceBound[0], cameraDistanceBound[1]));

        recorder.cameraPosition = mainCamera.transform.position;
        recorder.cameraRotation = mainCamera.transform.eulerAngles;
    }

    void RandomizeObjects()
    {
        DestroyRandomizedObjects();
        int numberOfObjects = UnityEngine.Random.Range(objectCountRange.x, objectCountRange.y);
        recorder.numberOfObjects = numberOfObjects;
        float planewidth = 0.7f;

        for (int i = 0; i < numberOfObjects; i++)
        {
            // Random category, prefabId
            int categoryIndex = UnityEngine.Random.Range(0, prefabs.Length); // + 8);
            GameObject sourceCategory = null;
            if (categoryIndex < prefabs.Length)
                sourceCategory = prefabs[categoryIndex];
            else
                sourceCategory = goblets[UnityEngine.Random.Range(0, goblets.Length)];

            // int categoryIndex = UnityEngine.Random.Range(0, prefabs.Length);
            // GameObject sourceCategory = prefabs[categoryIndex];

            GameObject obj = Instantiate(sourceCategory);

            obj.name = obj.name.Replace("(Clone)", "");
            Vector3 PrefabShape = obj.GetComponent<Renderer>().bounds.size;
            float maxshape = Mathf.Max(PrefabShape.x, PrefabShape.y, PrefabShape.z);
            float minshape = Mathf.Min(PrefabShape.x, PrefabShape.y, PrefabShape.z);
            float scale = Mathf.Min(planewidth / maxshape * UnityEngine.Random.Range(scaleLowerBound, scaleUpperBound), planewidth * 0.2f / minshape);

            // Randomize object position, rotation, scale
            obj.transform.localPosition = RandomConstrainedPosition();
            obj.transform.localRotation = UnityEngine.Random.rotation;
            obj.transform.localScale = new Vector3(scale, scale, scale);

            Collider collider = obj.GetComponent<Collider>();
            if (collider == null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.convex = true; // If you want a convex mesh collider
                // meshCollider.isTrigger = true; // If you want it to be a trigger collider
            }
            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody =  obj.AddComponent<Rigidbody>();
            }
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            Color color;
            

            // Randomize IOR
            float IOR = UnityEngine.Random.Range(IORLowerBound, IORUpperBound);
            float Metallic = UnityEngine.Random.Range(MetallicLowerBound, MetallicUpperBound);
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();

            int length = meshRenderer.materials.Length;
            Material[] materials = new Material[length];

            Material glassMat = LoadGlassMaterial(IOR, Metallic);
            int material_idx = UnityEngine.Random.Range(0, 100);
            if (material_idx < OpaqueRatio)
            {
                for (int j = 0; j < length; j++)
                    materials[j] = LoadOpaqueMaterial();
                color = Color.red;
            }
            else
            {
                for (int j = 0; j < length; j++)
                    materials[j] = glassMat;
                color = Color.green;
            }


            mpb.SetColor("_IDColor", color);
            foreach (var item in obj.GetComponentsInChildren<Renderer>())
            {
                item.SetPropertyBlock(mpb);
            }

            meshRenderer.materials = materials;


            recorder.positions.Add(obj.transform.position);
            recorder.rotations.Add(obj.transform.eulerAngles);
            recorder.categories.Add(obj.name);
            recorder.prefabPaths.Add(obj.name);
            recorder.scales.Add(scale);
            recorder.IORs.Add(IOR);

            currentObj.Add(obj);
        }
    }
    List<GameObject> currentObj = new List<GameObject>();

    void DestroyRandomizedObjects()
    {
        foreach (GameObject item in currentObj)
        {
            Destroy(item);
        }
        currentObj.Clear();
    }
}
