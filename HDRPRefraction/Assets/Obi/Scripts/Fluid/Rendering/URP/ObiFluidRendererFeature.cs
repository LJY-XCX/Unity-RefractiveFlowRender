#if UNITY_2019_2_OR_NEWER && SRP_UNIVERSAL
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Obi
{

    public class ObiFluidRendererFeature : ScriptableRendererFeature
    {

        public FluidRenderingUtils.FluidRendererSettings settings = new FluidRenderingUtils.FluidRendererSettings();
        private FluidRenderingUtils.FluidRenderTargets renderTargets;

        private ThicknessBufferPass m_ThicknessPass;
        private SurfaceReconstruction m_SurfacePass;
        private FoamPass m_FoamPass;
        private RenderFluidPass m_RenderFluidPass;

        public override void Create()
        {
            renderTargets = new FluidRenderingUtils.FluidRenderTargets();
            renderTargets.foam = Shader.PropertyToID("_Foam");
            renderTargets.depth = Shader.PropertyToID("_FluidDepthTexture");
            renderTargets.thickness1 = Shader.PropertyToID("_FluidThickness1");
            renderTargets.thickness2 = Shader.PropertyToID("_FluidThickness2");
            renderTargets.smoothDepth = Shader.PropertyToID("_FluidSurface"); //smoothed depth
            renderTargets.normals = Shader.PropertyToID("_FluidNormals");

            m_ThicknessPass = new ThicknessBufferPass();
            m_ThicknessPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents +1;

            m_SurfacePass = new SurfaceReconstruction();
            m_SurfacePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents +1;

            m_FoamPass = new FoamPass();
            m_FoamPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents + 1;

            m_RenderFluidPass = new RenderFluidPass(RenderTargetHandle.CameraTarget);
            m_RenderFluidPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents +1;

        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            ObiParticleRenderer[] particleRenderers = GameObject.FindObjectsOfType<ObiParticleRenderer>();

            m_ThicknessPass.Setup(settings, renderTargets, particleRenderers);
            renderer.EnqueuePass(m_ThicknessPass);

            if (settings.generateSurface)
            {
                m_SurfacePass.Setup(settings, renderTargets, particleRenderers);
                renderer.EnqueuePass(m_SurfacePass);
            }

            if (settings.generateFoam)
            {
                m_FoamPass.Setup(settings, renderTargets, particleRenderers);
                renderer.EnqueuePass(m_FoamPass);
            }

            m_RenderFluidPass.Setup(settings, renderTargets);
            renderer.EnqueuePass(m_RenderFluidPass);
        }

    }

    public class ThicknessBufferPass : ScriptableRenderPass
    {
        const string k_RenderGrabPassTag = "FluidThicknessPass";
        private ProfilingSampler m_Thickness_Profile = new ProfilingSampler(k_RenderGrabPassTag);

        private FluidRenderingUtils.FluidRendererSettings settings;
        private FluidRenderingUtils.FluidRenderTargets renderTargets;

        private Material thickness_Material;
        private Material color_Material;

        private ObiParticleRenderer[] renderers;

        public void Setup(FluidRenderingUtils.FluidRendererSettings settings, FluidRenderingUtils.FluidRenderTargets renderTargets, ObiParticleRenderer[] renderers)
        {
            // Copy settings;
            this.settings = settings;
            this.renderTargets = renderTargets;
            this.renderers = renderers;

            if (thickness_Material == null)
                thickness_Material = FluidRenderingUtils.CreateMaterial(Shader.Find("Hidden/FluidThickness"));

            if (color_Material == null)
                color_Material = FluidRenderingUtils.CreateMaterial(Shader.Find("Obi/Fluid/Colors/FluidColorsBlend"));

            bool shadersSupported = thickness_Material && color_Material;

            if (!shadersSupported ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth)  ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                Debug.LogWarning("Obi Fluid Renderer not supported in this platform.");
                return;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(renderTargets.thickness1, cameraTextureDescriptor.width / settings.thicknessDownsample, cameraTextureDescriptor.height / settings.thicknessDownsample, 16, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
            cmd.GetTemporaryRT(renderTargets.thickness2, cameraTextureDescriptor.width / settings.thicknessDownsample, cameraTextureDescriptor.height / settings.thicknessDownsample, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);

            ConfigureTarget(renderTargets.thickness1);
            ConfigureClear(ClearFlag.All, FluidRenderingUtils.thicknessBufferClear);

        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (color_Material == null) 
                return;

            color_Material.SetInt("_BlendSrc", (int)settings.particleBlendSource);
            color_Material.SetInt("_BlendDst", (int)settings.particleBlendDestination);
            color_Material.SetInt("_ZWrite", settings.particleZWrite ? 1 : 0);

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderGrabPassTag);
            using (new ProfilingScope(cmd, m_Thickness_Profile))
            {
                // generate color / thickness buffer:
                FluidRenderingUtils.VolumeReconstruction(cmd,
                                                        renderTargets,
                                                        thickness_Material,
                                                        color_Material,
                                                        renderers);

                Blit(cmd,renderTargets.thickness1, renderTargets.thickness2, thickness_Material, 1);
                Blit(cmd,renderTargets.thickness2, renderTargets.thickness1, thickness_Material, 2);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(renderTargets.thickness1);
            cmd.ReleaseTemporaryRT(renderTargets.thickness2);
        }
    }

    public class SurfaceReconstruction : ScriptableRenderPass
    {
        const string k_RenderGrabPassTag = "FluidSurfaceReconstruction";

        private FluidRenderingUtils.FluidRendererSettings settings;
        private FluidRenderingUtils.FluidRenderTargets renderTargets;

        private Material depth_BlurMaterial;
        private Material normal_ReconstructMaterial;

        private ObiParticleRenderer[] renderers;

        public void Setup(FluidRenderingUtils.FluidRendererSettings settings, FluidRenderingUtils.FluidRenderTargets renderTargets, ObiParticleRenderer[] renderers)
        {
            // Copy settings;
            this.settings = settings;
            this.renderTargets = renderTargets;
            this.renderers = renderers;

            if (depth_BlurMaterial == null)
                depth_BlurMaterial = FluidRenderingUtils.CreateMaterial(Shader.Find("Hidden/ScreenSpaceCurvatureFlow"));

            if (normal_ReconstructMaterial == null)
                normal_ReconstructMaterial = FluidRenderingUtils.CreateMaterial(Shader.Find("Hidden/NormalReconstruction"));

            bool shadersSupported = depth_BlurMaterial && normal_ReconstructMaterial;

            if (!shadersSupported ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                Debug.LogWarning("Obi Fluid Renderer not supported in this platform.");
                return;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // normals/depth buffers:
            cmd.GetTemporaryRT(renderTargets.depth, cameraTextureDescriptor.width / settings.surfaceDownsample, cameraTextureDescriptor.height / settings.surfaceDownsample, 24, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.GetTemporaryRT(renderTargets.smoothDepth, cameraTextureDescriptor.width / settings.surfaceDownsample, cameraTextureDescriptor.height / settings.surfaceDownsample, 0, FilterMode.Bilinear, RenderTextureFormat.RFloat);
            cmd.GetTemporaryRT(renderTargets.normals, cameraTextureDescriptor.width / settings.surfaceDownsample, cameraTextureDescriptor.height / settings.surfaceDownsample, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);

            // use normals as a placeholder, we're only interested in the depth attachment.
            ConfigureTarget(renderTargets.normals, renderTargets.depth);
            ConfigureClear(ClearFlag.Depth, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (depth_BlurMaterial == null || normal_ReconstructMaterial == null)
                return;

            float blurScale = FluidRenderingUtils.SetupFluidCamera(renderingData.cameraData.camera);

            depth_BlurMaterial.SetFloat("_BlurScale", blurScale);
            depth_BlurMaterial.SetFloat("_BlurRadiusWorldspace", settings.blurRadius);


            CommandBuffer cmd = CommandBufferPool.Get(k_RenderGrabPassTag);
            using (new ProfilingSample(cmd, k_RenderGrabPassTag))
            {

                // surface reconstruction and dependant effects (lighting/reflection/refraction/foam)
                if (settings.generateSurface)
                {
                    // normals/depth buffers:
                    FluidRenderingUtils.SurfaceReconstruction(cmd,
                                                              renderTargets,
                                                              depth_BlurMaterial,
                                                              normal_ReconstructMaterial,
                                                              renderers);

                    // blur fluid surface / reconstruct normals from smoothed depth:
                    cmd.Blit(renderTargets.depth, renderTargets.smoothDepth, depth_BlurMaterial);
                    cmd.Blit(renderTargets.smoothDepth, renderTargets.normals, normal_ReconstructMaterial);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(renderTargets.normals);
            cmd.ReleaseTemporaryRT(renderTargets.depth);
            cmd.ReleaseTemporaryRT(renderTargets.smoothDepth);
        }
    }

    public class FoamPass : ScriptableRenderPass
    {
        const string k_RenderGrabPassTag = "FoamPass";
        private ProfilingSampler m_Thickness_Profile = new ProfilingSampler(k_RenderGrabPassTag);

        private FluidRenderingUtils.FluidRendererSettings settings;
        private FluidRenderingUtils.FluidRenderTargets renderTargets;

        private ObiParticleRenderer[] renderers;

        public void Setup(FluidRenderingUtils.FluidRendererSettings settings, FluidRenderingUtils.FluidRenderTargets renderTargets, ObiParticleRenderer[] renderers)
        {
            // Copy settings;
            this.settings = settings;
            this.renderTargets = renderTargets;
            this.renderers = renderers;

            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                Debug.LogWarning("Obi Fluid Renderer not supported in this platform.");
                return;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(renderTargets.foam, cameraTextureDescriptor.width / settings.foamDownsample, cameraTextureDescriptor.height / settings.foamDownsample, 0, FilterMode.Bilinear);

            ConfigureTarget(renderTargets.foam);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderGrabPassTag);
            using (new ProfilingScope(cmd, m_Thickness_Profile))
            {
                // TODO: DrawRenderer does not seem to work in URP, so switch to DrawRenderers().
                FluidRenderingUtils.Foam(cmd, renderTargets, renderers);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(renderTargets.foam);
        }
    }

    public class RenderFluidPass : ScriptableRenderPass
    {
        const string k_RenderGrabPassTag = "RenderFluidPass";

        private FluidRenderingUtils.FluidRendererSettings settings;

        private Material fluid_Material;

        private FluidRenderingUtils.FluidRenderTargets renderTargets;

        public RenderFluidPass(RenderTargetHandle colorHandle)
        {
        }

        public void Setup(FluidRenderingUtils.FluidRendererSettings settings, FluidRenderingUtils.FluidRenderTargets renderTargets)
        {
            // Copy settings;
            this.settings = settings;
            this.renderTargets = renderTargets;

            if (fluid_Material == null)
                fluid_Material = FluidRenderingUtils.CreateMaterial(Shader.Find("Obi/URP/Fluid/FluidShading"));

            bool shadersSupported = fluid_Material;

            if (!shadersSupported ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                Debug.LogWarning("Obi Fluid Renderer not supported in this platform.");
                return;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (fluid_Material == null)
                return;

            fluid_Material.SetInt("_BlendSrc", (int)settings.blendSource);
            fluid_Material.SetInt("_BlendDst", (int)settings.blendDestination);
            fluid_Material.SetFloat("_ThicknessCutoff", settings.thicknessCutoff);


            CommandBuffer cmd = CommandBufferPool.Get(k_RenderGrabPassTag);
            using (new ProfilingSample(cmd, k_RenderGrabPassTag))
            {
                ObiParticleRenderer[] particleRenderers = GameObject.FindObjectsOfType<ObiParticleRenderer>();

                // surface reconstruction and dependant effects (lighting/reflection/refraction/foam)
                if (settings.generateSurface)
                {
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
                    fluid_Material.EnableKeyword("FLUID_FOAM");
                }
                else
                {
                    fluid_Material.DisableKeyword("FLUID_FOAM");
                }

                Camera camera = renderingData.cameraData.camera;

                cmd.SetGlobalTexture("_Volume", renderTargets.thickness1);

                // Draw a quad manually, as in gameview, target does not have a depth buffer.
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, fluid_Material);
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }
    }
}


#endif
