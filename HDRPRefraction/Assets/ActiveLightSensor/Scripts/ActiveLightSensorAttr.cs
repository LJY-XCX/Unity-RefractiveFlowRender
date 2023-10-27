using System;
using System.Collections.Generic;
using UnityEngine;

namespace RFUniverse.Attributes
{
    public class ActiveLightSensorAttr: MonoBehaviour
    {
        public Light irLight;
        public Camera leftCamera;
        public Camera rightCamera;
        Texture2D tex;

        public void Awake()
        {
            tex = new Texture2D(1, 1);
            irLight.enabled = false;
            leftCamera.enabled = false;
            rightCamera.enabled = false;
        }

        public Texture2D GetCameraIR(float[,] intrinsicMatrix, bool leftOrRight)
        {
            Vector2Int size = SetCameraIntrinsicMatrix(leftOrRight ? leftCamera : rightCamera, intrinsicMatrix);
            return GetCameraIR(size.x, size.y, leftOrRight ? leftCamera : rightCamera);
        }

        public Vector2Int SetCameraIntrinsicMatrix(Camera set_camera, float[,] intrinsicMatrix)
        {
            set_camera.usePhysicalProperties = true;
            float focal = 35;
            float ax, ay, sizeX, sizeY;
            float x0, y0, shiftX, shiftY;
            ax = intrinsicMatrix[0, 0];
            ay = intrinsicMatrix[1, 1];
            x0 = intrinsicMatrix[0, 2];
            y0 = intrinsicMatrix[1, 2];
            int width = (int)x0 * 2;
            int height = (int)y0 * 2;
            sizeX = focal * width / ax;
            sizeY = focal * height / ay;
            shiftX = -(x0 - width / 2.0f) / width;
            shiftY = (y0 - height / 2.0f) / height;
            set_camera.sensorSize = new Vector2(sizeX, sizeY);
            set_camera.focalLength = focal;
            set_camera.lensShift = new Vector2(shiftX, shiftY);
            return new Vector2Int(width, height);
        }
        public Texture2D GetCameraIR(int width, int height, Camera cam)
        {
            irLight.enabled = true;
            UnityEngine.Rendering.AmbientMode tempMode = RenderSettings.ambientMode;
            Color tempColor = RenderSettings.ambientLight;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            cam.targetTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);
            cam.Render();
            RenderTexture.active = cam.targetTexture;
            tex.Reinitialize(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.ReleaseTemporary(cam.targetTexture);
            cam.targetTexture = null;
            irLight.enabled = false;
            RenderSettings.ambientMode = tempMode;
            RenderSettings.ambientLight = tempColor;
            return tex;
        }
    }
}