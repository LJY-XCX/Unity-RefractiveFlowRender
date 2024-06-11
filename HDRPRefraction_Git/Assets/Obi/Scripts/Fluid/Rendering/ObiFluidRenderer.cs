using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;


namespace Obi
{

	/**
	 * High-quality fluid rendering, supports both 2D and 3D. Performs depth testing against the scene, 
	 * considers reflection, refraction, lighting, transmission, and foam.
	 */
	public class ObiFluidRenderer : ObiBaseFluidRenderer
	{
        public FluidRenderingUtils.FluidRendererSettings settings;

        // materials
        private Material depth_BlurMaterial;
		private Material normal_ReconstructMaterial;
		private Material thickness_Material;
        private Material color_Material;
        private Material fluid_Material;
        private FluidRenderingUtils.FluidRenderTargets renderTargets;

        protected override void Setup()
        {
	
			GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

            if (depth_BlurMaterial == null)
				depth_BlurMaterial = CreateMaterial(Shader.Find("Hidden/ScreenSpaceCurvatureFlow"));
	
			if (normal_ReconstructMaterial == null)
				normal_ReconstructMaterial = CreateMaterial(Shader.Find("Hidden/NormalReconstruction"));
	
			if (thickness_Material == null)
				thickness_Material = CreateMaterial(Shader.Find("Hidden/FluidThickness"));

            if(color_Material == null)
                color_Material = CreateMaterial(Shader.Find("Obi/Fluid/Colors/FluidColorsBlend"));

            if(fluid_Material == null)
                fluid_Material = CreateMaterial(Shader.Find("Obi/Fluid/FluidShading"));

            bool shadersSupported = depth_BlurMaterial && normal_ReconstructMaterial && thickness_Material && color_Material && fluid_Material;
	
			if (!shadersSupported || 
				!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth) ||
				!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.RFloat) ||
				!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.ARGBHalf)
	 			)
	        {
	            enabled = false;
				Debug.LogWarning("Obi Fluid Renderer not supported in this platform.");
	            return;
	        }

            renderTargets = new FluidRenderingUtils.FluidRenderTargets();
            renderTargets.refraction = Shader.PropertyToID("_Refraction");
            renderTargets.foam = Shader.PropertyToID("_Foam");
            renderTargets.depth = Shader.PropertyToID("_FluidDepthTexture");
            renderTargets.thickness1 = Shader.PropertyToID("_FluidThickness1");
            renderTargets.thickness2 = Shader.PropertyToID("_FluidThickness2");
            renderTargets.smoothDepth = Shader.PropertyToID("_FluidSurface"); //smoothed depth
            renderTargets.normals = Shader.PropertyToID("_FluidNormals");

            Shader.SetGlobalMatrix("_Camera_to_World",currentCam.cameraToWorldMatrix);
			Shader.SetGlobalMatrix("_World_to_Camera",currentCam.worldToCameraMatrix);
			Shader.SetGlobalMatrix("_InvProj",currentCam.projectionMatrix.inverse);  
	
			float fovY = currentCam.fieldOfView;
	        float far = currentCam.farClipPlane;
	        float y = currentCam.orthographic ? 2 * currentCam.orthographicSize: 2 * Mathf.Tan (fovY * Mathf.Deg2Rad * 0.5f) * far;
	        float x = y * currentCam.aspect;
			Shader.SetGlobalVector("_FarCorner",new Vector3(x,y,far));
	
			depth_BlurMaterial.SetFloat("_BlurScale",currentCam.orthographic ? 1 : currentCam.pixelWidth/currentCam.aspect * (1.0f/Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f)));
			depth_BlurMaterial.SetFloat("_BlurRadiusWorldspace", settings.blurRadius);
		}

		protected override void Cleanup()
		{
			if (depth_BlurMaterial != null)
				DestroyImmediate (depth_BlurMaterial);
			if (normal_ReconstructMaterial != null)
				DestroyImmediate (normal_ReconstructMaterial);
			if (thickness_Material != null)
				DestroyImmediate (thickness_Material);
            if (color_Material)
                DestroyImmediate(color_Material);
            if (fluid_Material)
                DestroyImmediate(fluid_Material);
        }
	
		public override void UpdateFluidRenderingCommandBuffer()
		{
			renderFluid.Clear();
	
			if (particleRenderers == null || fluid_Material == null || color_Material == null)
				return;

            fluid_Material.SetInt("_BlendSrc", (int)settings.blendSource);
            fluid_Material.SetInt("_BlendDst", (int)settings.blendDestination);
            fluid_Material.SetFloat("_ThicknessCutoff", settings.thicknessCutoff);

            color_Material.SetInt("_BlendSrc", (int)settings.particleBlendSource);
            color_Material.SetInt("_BlendDst", (int)settings.particleBlendDestination);
            color_Material.SetInt("_ZWrite", settings.particleZWrite?1:0);

            // generate color / thickness buffer:
            renderFluid.GetTemporaryRT(renderTargets.thickness1, -settings.thicknessDownsample, -settings.thicknessDownsample, 16, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
            renderFluid.GetTemporaryRT(renderTargets.thickness2, -settings.thicknessDownsample, -settings.thicknessDownsample, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);

            renderFluid.SetRenderTarget(renderTargets.thickness1);
            renderFluid.ClearRenderTarget(true, true, FluidRenderingUtils.thicknessBufferClear);

            FluidRenderingUtils.VolumeReconstruction(renderFluid,
                                               renderTargets,
                                               thickness_Material,
                                               color_Material,
                                               particleRenderers);

            renderFluid.Blit(renderTargets.thickness1, renderTargets.thickness2, thickness_Material, 1);
            renderFluid.Blit(renderTargets.thickness2, renderTargets.thickness1, thickness_Material, 2);

            // surface reconstruction and dependant effects (lighting/reflection/refraction/foam)
            if (settings.generateSurface)
            {
                // normals/depth buffers:
                renderFluid.GetTemporaryRT(renderTargets.depth, -settings.surfaceDownsample, -settings.surfaceDownsample, 24, FilterMode.Point, RenderTextureFormat.Depth);
                renderFluid.GetTemporaryRT(renderTargets.smoothDepth, -settings.surfaceDownsample, -settings.surfaceDownsample, 0, FilterMode.Bilinear, RenderTextureFormat.RFloat);
                renderFluid.GetTemporaryRT(renderTargets.normals, -settings.surfaceDownsample, -settings.surfaceDownsample, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);

                renderFluid.SetRenderTarget(renderTargets.depth); // fluid depth
                renderFluid.ClearRenderTarget(true, true, Color.clear); //clear

                FluidRenderingUtils.SurfaceReconstruction(renderFluid,
                                                          renderTargets,
                                                          depth_BlurMaterial,
                                                          normal_ReconstructMaterial,
                                                          particleRenderers);

                // blur fluid surface / reconstruct normals from smoothed depth:
                renderFluid.Blit(renderTargets.depth, renderTargets.smoothDepth, depth_BlurMaterial);
                renderFluid.Blit(renderTargets.smoothDepth, renderTargets.normals, normal_ReconstructMaterial);

                fluid_Material.SetInt("_ZTest", (int)CompareFunction.LessEqual);

                if (settings.lighting)
                {
                    fluid_Material.EnableKeyword("FLUID_LIGHTING");
                    fluid_Material.SetFloat("_Smoothness", settings.smoothness);
                    fluid_Material.SetFloat("_AmbientMultiplier", settings.ambientMultiplier);
                }
                else
                    fluid_Material.DisableKeyword("FLUID_LIGHTING");

                if (settings.generateReflection)
                {
                    fluid_Material.EnableKeyword("FLUID_REFLECTION");
                    fluid_Material.SetFloat("_ReflectionCoeff", settings.reflection);
                    fluid_Material.SetFloat("_Metalness", settings.metalness);
                }
                else
                    fluid_Material.DisableKeyword("FLUID_REFLECTION");

                if (settings.generateRefraction)
                {
                    renderFluid.GetTemporaryRT(renderTargets.refraction, -settings.refractionDownsample, -settings.refractionDownsample, 0, FilterMode.Bilinear);
                    FluidRenderingUtils.Refraction(renderFluid, BuiltinRenderTextureType.CameraTarget, renderTargets);
                    fluid_Material.EnableKeyword("FLUID_REFRACTION");
                    fluid_Material.SetFloat("_Transparency", settings.transparency);
                    fluid_Material.SetFloat("_AbsorptionCoeff", settings.absorption);
                    fluid_Material.SetFloat("_RefractionCoeff", settings.refraction);
                }
                else
                {
                    fluid_Material.DisableKeyword("FLUID_REFRACTION");
                }
            }
            else
            {
                // no depth buffer, so always pass ztest.
                fluid_Material.SetInt("_ZTest", (int)CompareFunction.Always);

                fluid_Material.DisableKeyword("FLUID_LIGHTING");
                fluid_Material.DisableKeyword("FLUID_REFLECTION");
                fluid_Material.DisableKeyword("FLUID_REFRACTION");
            }

            if (settings.generateFoam)
            {
                renderFluid.GetTemporaryRT(renderTargets.foam, -settings.foamDownsample, -settings.foamDownsample, 0, FilterMode.Bilinear);

                renderFluid.SetRenderTarget(renderTargets.foam);
                renderFluid.ClearRenderTarget(true, true, Color.clear);

                FluidRenderingUtils.Foam(renderFluid, renderTargets, particleRenderers);
                fluid_Material.EnableKeyword("FLUID_FOAM");
            }
            else
            {
                fluid_Material.DisableKeyword("FLUID_FOAM");
            }

            // final pass (shading):
            renderFluid.Blit(renderTargets.thickness1, BuiltinRenderTextureType.CameraTarget, fluid_Material);
		}	
	
	}
}

