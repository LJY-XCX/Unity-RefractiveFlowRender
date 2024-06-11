using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

public class HDRPCameraAttr : MonoBehaviour
{
    new protected Camera camera = null;
    public Camera Camera
    {
        get
        {
            if (camera == null)
                camera = GetComponent<Camera>();
            return camera;
        }
    }
    public static Material replace = null;

    public static Material cameraDepthMaterial = null;
    public static Material cameraNormalMaterial = null;
    public static Material cameraIDMaterial = null;
    HDAdditionalCameraData cameraHD;
    CustomPassVolume volume = null;
    DrawRenderersCustomPass pass = null;

    protected Texture2D tex = null;
    public void Awake()
    {
        tex = new Texture2D(1, 1);

        Camera.depth = -100;
        Camera.allowMSAA = true;
        Camera.allowHDR = false;
        Camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
        if (cameraDepthMaterial == null)
            cameraDepthMaterial = Resources.Load<Material>("HDRPCameraDepth");
        if (cameraNormalMaterial == null)
            cameraNormalMaterial = Resources.Load<Material>("HDRPCameraNormal");
        if (cameraIDMaterial == null)
            cameraIDMaterial = Resources.Load<Material>("HDRPCameraID");
        if (replace == null)
            replace = Resources.Load<Material>("replaceT");


        cameraHD = GetComponent<HDAdditionalCameraData>() ?? gameObject.AddComponent<HDAdditionalCameraData>();
        var mask = new FrameSettingsOverrideMask();
        mask.mask = ~(new BitArray128());
        cameraHD.customRenderingSettings = true;
        cameraHD.renderingPathCustomFrameSettingsOverrideMask = mask;
        cameraHD.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.CustomPass, true);
        cameraHD.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
        cameraHD.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        volume = GetComponent<CustomPassVolume>();
        if (volume == null)
            volume = gameObject.AddComponent<CustomPassVolume>();
        volume.isGlobal = false;
        volume.targetCamera = Camera;
        volume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
        pass = (DrawRenderersCustomPass)volume.AddPassOfType<DrawRenderersCustomPass>();
        pass.targetColorBuffer = CustomPass.TargetBuffer.Camera;
        pass.targetDepthBuffer = CustomPass.TargetBuffer.Camera;
        pass.clearFlags = ClearFlag.All;
        pass.renderQueueType = CustomPass.RenderQueueType.All;
        pass.layerMask = Camera.cullingMask;
        pass.overrideMaterialPassName = "ForwardOnly";
        pass.overrideDepthState = true;
        pass.depthCompareFunction = CompareFunction.LessEqual;
        pass.depthWrite = true;
        pass.sortingCriteria = SortingCriteria.RenderQueue;
    }


    public Texture2D GetRGB(int width, int height, float? unPhysicalFov = 60)
    {
        Debug.Log("GetRGB");
        if (unPhysicalFov != null)
        {
            Camera.usePhysicalProperties = false;
            Camera.fieldOfView = unPhysicalFov.Value;
        }
        cameraHD.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
        Camera.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);
        cameraHD.customRenderingSettings = false;
        pass.enabled = false;
        Camera.Render();

        RenderTexture.active = Camera.targetTexture;
        tex.Reinitialize(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        
        RenderTexture.ReleaseTemporary(Camera.targetTexture);
        Camera.targetTexture = null;
        return tex;
    }

    public Texture2D GetGrayCode(int width, int height, float? unPhysicalFov = 60)
    {
        Debug.Log("GetGrayCode");
        if (unPhysicalFov != null)
        {
            Camera.usePhysicalProperties = false;
            Camera.fieldOfView = unPhysicalFov.Value;
        }
        cameraHD.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        cameraHD.backgroundColorHDR = new Color(0, 0, 0, 1);
        cameraHD.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        Camera.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear, 1);
        cameraHD.customRenderingSettings = false;
        pass.enabled = false;
        Camera.Render();

        RenderTexture.active = Camera.targetTexture;
        tex.Reinitialize(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        
        RenderTexture.ReleaseTemporary(Camera.targetTexture);
        Camera.targetTexture = null;
        cameraHD.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        return tex;
    }
    public Texture2D GetNormal(int width, int height, float? unPhysicalFov = 60)
    {
        Debug.Log("GetNormal");
        if (unPhysicalFov != null)
        {
            Camera.usePhysicalProperties = false;
            Camera.fieldOfView = unPhysicalFov.Value;
        }
        cameraHD.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        cameraHD.backgroundColorHDR = new Color(0, 0, 0, 1);
        Camera.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear, 1);
        cameraHD.customRenderingSettings = true;

        pass.enabled = true;
        pass.overrideMaterial = cameraNormalMaterial;
        Camera.Render();

        RenderTexture.active = Camera.targetTexture;
        tex.Reinitialize(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        
        RenderTexture.ReleaseTemporary(Camera.targetTexture);
        Camera.targetTexture = null;
        pass.enabled = false;
        return tex;
    }

    public Texture2D GetID(int width, int height, float? unPhysicalFov = 60)
    {
        Debug.Log("GetID");
        if (unPhysicalFov != null)
        {
            Camera.usePhysicalProperties = false;
            Camera.fieldOfView = unPhysicalFov.Value;
        }
        cameraHD.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        cameraHD.backgroundColorHDR = new Color(0, 0, 0, 1);
        Camera.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear, 1);
        cameraHD.customRenderingSettings = true;
        pass.enabled = true;
        pass.overrideMaterial = cameraIDMaterial;
        Camera.Render();

        RenderTexture.active = Camera.targetTexture;
        tex.Reinitialize(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        
        RenderTexture.ReleaseTemporary(Camera.targetTexture);
        Camera.targetTexture = null;
        pass.enabled = false;
        return tex;
    }

    public Texture2D GetDepth(int width, int height, float near, float far, float? unPhysicalFov = 60)
    {
        Debug.Log("GetDepth");
        if (unPhysicalFov != null)
        {
            Camera.usePhysicalProperties = false;
            Camera.fieldOfView = unPhysicalFov.Value;
        }
        cameraHD.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        cameraHD.backgroundColorHDR = new Color(0, 0, 0, 1);
        Camera.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.R16, RenderTextureReadWrite.Linear, 1);
        cameraHD.customRenderingSettings = true;
        pass.enabled = true;
        pass.overrideMaterial = cameraDepthMaterial;

        //pass.overrideMaterial = replace;
        //Shader.SetGlobalInt("_OutputMode", 3);

        cameraDepthMaterial.SetFloat("_CameraZeroDis", near);
        cameraDepthMaterial.SetFloat("_CameraOneDis", far);
        Camera.Render();

        RenderTexture.active = Camera.targetTexture;
        tex.Reinitialize(width, height, TextureFormat.R16, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        
        RenderTexture.ReleaseTemporary(Camera.targetTexture);
        Camera.targetTexture = null;
        pass.enabled = false;
        return tex;
    }
    public Texture2D GetDepthEXR(int width, int height, float near, float far, float? unPhysicalFov = 60)
    {
        Debug.Log("GetDepthEXR");
        if (unPhysicalFov != null)
        {
            Camera.usePhysicalProperties = false;
            Camera.fieldOfView = unPhysicalFov.Value;
        }
        cameraHD.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        cameraHD.backgroundColorHDR = new Color(0, 0, 0, 1);
        Camera.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1);
        cameraHD.customRenderingSettings = true;
        pass.enabled = true;
        pass.overrideMaterial = cameraDepthMaterial;

        //pass.overrideMaterial = replace;
        //Shader.SetGlobalInt("_OutputMode", 3);

        cameraDepthMaterial.SetFloat("_CameraZeroDis", near);
        cameraDepthMaterial.SetFloat("_CameraOneDis", far);
        Camera.Render();

        RenderTexture.active = Camera.targetTexture;
        tex.Reinitialize(width, height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        RenderTexture.ReleaseTemporary(Camera.targetTexture);
        Camera.targetTexture = null;
        pass.enabled = false;
        return tex;
    }

    public Texture2D GetDepthEXR(int width, int height , float? unPhysicalFov = 60)
    {
        Debug.Log("GetDepthEXR");
        if (unPhysicalFov != null)
        {
            Camera.usePhysicalProperties = false;
            Camera.fieldOfView = unPhysicalFov.Value;
        }
        cameraHD.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        cameraHD.backgroundColorHDR = new Color(0, 0, 0, 1);
        Camera.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1);
        cameraHD.customRenderingSettings = true;
        pass.enabled = true;
        pass.overrideMaterial = cameraDepthMaterial;

        //pass.overrideMaterial = replace;
        //Shader.SetGlobalInt("_OutputMode", 3);

        cameraDepthMaterial.SetFloat("_CameraZeroDis", 0);
        cameraDepthMaterial.SetFloat("_CameraOneDis", 1);
        Camera.Render();

        RenderTexture.active = Camera.targetTexture;
        tex.Reinitialize(width, height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        
        RenderTexture.ReleaseTemporary(Camera.targetTexture);
        Camera.targetTexture = null;
        pass.enabled = false;
        return tex;
    }
}
