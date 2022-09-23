//------------------------------------------------------------------------------------------------------------------
// Volumetric Lights
// Created by Kronnect
//------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VolumetricLights {

    public class VolumetricLightsRenderFeature : ScriptableRendererFeature {

        static class ShaderParams {
            public static int LightBuffer = Shader.PropertyToID("_LightBuffer");
            public static int MainTex = Shader.PropertyToID("_MainTex");
            public static int BlurRT = Shader.PropertyToID("_BlurTex");
            public static int BlurRT2 = Shader.PropertyToID("_BlurTex2");
            public static int BlendDest = Shader.PropertyToID("_BlendDest");
            public static int BlendSrc = Shader.PropertyToID("_BlendSrc");
            public static int MiscData = Shader.PropertyToID("_MiscData");
            public static int ForcedInvisible = Shader.PropertyToID("_ForcedInvisible");
            public static int DownsampledDepth = Shader.PropertyToID("_DownsampledDepth");
            public static int BlueNoiseTexture = Shader.PropertyToID("_BlueNoise");

            public const string SKW_DITHER = "DITHER";
        }

        static int GetScaledSize(int size, float factor) {
            size = (int)(size / factor);
            size /= 2;
            if (size < 1)
                size = 1;
            return size * 2;
        }

        class VolumetricLightsRenderPass : ScriptableRenderPass {

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent, -1);
            readonly List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
            const int renderingLayer = 1 << 50;
            const string m_ProfilerTag = "Volumetric Lights Buffer Rendering";
            VolumetricLightsRenderFeature settings;

            public void Setup(VolumetricLightsRenderFeature settings) {
                this.settings = settings;
                renderPassEvent = settings.renderPassEvent;
                shaderTagIdList.Clear();
                shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
                RenderTextureDescriptor lightBufferDesc = cameraTextureDescriptor;
                int size = GetScaledSize(cameraTextureDescriptor.width, settings.downscaling);
                lightBufferDesc.width = size;
                lightBufferDesc.height = size;
                lightBufferDesc.depthBufferBits = 0;
                lightBufferDesc.useMipMap = false;
                lightBufferDesc.msaaSamples = 1;
                cmd.GetTemporaryRT(ShaderParams.LightBuffer, lightBufferDesc, FilterMode.Bilinear);
                RenderTargetIdentifier lightBuffer = new RenderTargetIdentifier(ShaderParams.LightBuffer, 0, CubemapFace.Unknown, -1);
                ConfigureTarget(lightBuffer);
                ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
                cmd.SetGlobalFloat(ShaderParams.ForcedInvisible, 0);
                context.ExecuteCommandBuffer(cmd);

                if (settings.downscaling <= 1f && settings.blurPasses < 1) {
                    CommandBufferPool.Release(cmd);
                    return;
                }

                foreach (VolumetricLight vl in VolumetricLight.volumetricLights) {
                    if (vl != null) {
                        vl.meshRenderer.renderingLayerMask = renderingLayer;
                    }
                }

                cmd.Clear();

                var sortFlags = SortingCriteria.CommonTransparent;
                var drawSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, sortFlags);
                var filterSettings = filteringSettings;
                filterSettings.renderingLayerMask = renderingLayer;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);

                RenderTargetIdentifier lightBuffer = new RenderTargetIdentifier(ShaderParams.LightBuffer, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalTexture(ShaderParams.LightBuffer, lightBuffer);

                CommandBufferPool.Release(cmd);

            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd) {
            }
        }



        class BlurRenderPass : ScriptableRenderPass {

            enum Pass {
                BlurHorizontal = 0,
                BlurVertical = 1,
                BlurVerticalAndBlend = 2,
                Blend = 3,
                DownscaleDepth = 4,
                BlurVerticalFinal = 5
            }


            ScriptableRenderer renderer;
            Material mat;
            RenderTextureDescriptor rtSourceDesc;
            static Matrix4x4 matrix4x4identity = Matrix4x4.identity;
            VolumetricLightsRenderFeature settings;

            public void Setup(Shader shader, ScriptableRenderer renderer, VolumetricLightsRenderFeature settings) {
                this.settings = settings;
                this.renderPassEvent = settings.renderPassEvent;
                this.renderer = renderer;
                if (mat == null) {
                    mat = CoreUtils.CreateEngineMaterial(shader);
                    Texture2D noiseTex = Resources.Load<Texture2D>("Textures/blueNoiseVL128");
                    mat.SetTexture(ShaderParams.BlueNoiseTexture, noiseTex);
                }

                switch (settings.blendMode) {
                    case BlendMode.Additive:
                        mat.SetInt(ShaderParams.BlendSrc, (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt(ShaderParams.BlendDest, (int)UnityEngine.Rendering.BlendMode.One);
                        break;
                    case BlendMode.Blend:
                        mat.SetInt(ShaderParams.BlendSrc, (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt(ShaderParams.BlendDest, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;
                    case BlendMode.PreMultiply:
                        mat.SetInt(ShaderParams.BlendSrc, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt(ShaderParams.BlendDest, (int)UnityEngine.Rendering.BlendMode.One);
                        break;
                }
                mat.SetVector(ShaderParams.MiscData, new Vector4(settings.ditherStrength * 0.1f, settings.brightness, settings.blurSpread, 0));
                if (settings.ditherStrength > 0) {
                    mat.EnableKeyword(ShaderParams.SKW_DITHER);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_DITHER);
                }
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
                rtSourceDesc = cameraTextureDescriptor;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

                if (settings.downscaling <= 1f && settings.blurPasses < 1) {
                    Cleanup();
                    return;
                }

                RenderTargetIdentifier source = renderer.cameraColorTarget;

                var cmd = CommandBufferPool.Get("Volumetric Lights Render Feature");

                cmd.SetGlobalFloat(ShaderParams.ForcedInvisible, 1);

                RenderTextureDescriptor rtBlurDesc = rtSourceDesc;
                int size = GetScaledSize(rtSourceDesc.width, settings.downscaling);
                rtBlurDesc.width = size;
                rtBlurDesc.height = size;
                rtBlurDesc.useMipMap = false;
                rtBlurDesc.msaaSamples = 1;
                rtBlurDesc.depthBufferBits = 0;

                bool usingDownscaling = settings.downscaling > 1f;
                if (usingDownscaling) {
                    RenderTextureDescriptor rtDownscaledDepth = rtBlurDesc;
                    rtDownscaledDepth.colorFormat = RenderTextureFormat.RHalf;
                    cmd.GetTemporaryRT(ShaderParams.DownsampledDepth, rtDownscaledDepth, FilterMode.Bilinear);
                    FullScreenBlit(cmd, source, ShaderParams.DownsampledDepth, mat, (int)Pass.DownscaleDepth);
                }

                if (settings.blurPasses < 1) {
                    FullScreenBlit(cmd, ShaderParams.LightBuffer, source, mat, (int)Pass.Blend);
                } else {
                    rtBlurDesc.width = GetScaledSize(rtSourceDesc.width, settings.blurDownscaling);
                    rtBlurDesc.height = GetScaledSize(rtSourceDesc.height, settings.blurDownscaling);
                    cmd.GetTemporaryRT(ShaderParams.BlurRT, rtBlurDesc, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(ShaderParams.BlurRT2, rtBlurDesc, FilterMode.Bilinear);
                    FullScreenBlit(cmd, ShaderParams.LightBuffer, ShaderParams.BlurRT, mat, (int)Pass.BlurHorizontal);
                    for (int k = 0; k < settings.blurPasses - 1; k++) {
                        FullScreenBlit(cmd, ShaderParams.BlurRT, ShaderParams.BlurRT2, mat, (int)Pass.BlurVertical);
                        FullScreenBlit(cmd, ShaderParams.BlurRT2, ShaderParams.BlurRT, mat, (int)Pass.BlurHorizontal);
                    }
                    if (usingDownscaling) {
                        FullScreenBlit(cmd, ShaderParams.BlurRT, ShaderParams.BlurRT2, mat, (int)Pass.BlurVerticalFinal);
                        FullScreenBlit(cmd, ShaderParams.BlurRT2, source, mat, (int)Pass.Blend);
                    } else {
                        FullScreenBlit(cmd, ShaderParams.BlurRT, source, mat, (int)Pass.BlurVerticalAndBlend);
                    }

                    cmd.ReleaseTemporaryRT(ShaderParams.BlurRT2);
                    cmd.ReleaseTemporaryRT(ShaderParams.BlurRT);
                }
                cmd.ReleaseTemporaryRT(ShaderParams.LightBuffer);
                if (usingDownscaling) {
                    cmd.ReleaseTemporaryRT(ShaderParams.DownsampledDepth);
                }
                context.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
            }

            void FullScreenBlit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex) {
                destination = new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetRenderTarget(destination);
                cmd.SetGlobalTexture(ShaderParams.MainTex, source);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, matrix4x4identity, material, 0, passIndex);
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd) {
            }


            public void Cleanup() {
                CoreUtils.Destroy(mat);
                Shader.SetGlobalFloat(ShaderParams.ForcedInvisible, 0);
            }
        }

        [SerializeField, HideInInspector]
        Shader shader;
        VolumetricLightsRenderPass vlRenderPass;
        BlurRenderPass blurRenderPass;
        public static bool installed;

        public BlendMode blendMode = BlendMode.Additive;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        [Range(1, 4)]
        public float downscaling = 1;
        [Range(0, 4)]
        public int blurPasses = 1;
        [Range(1, 4)]
        public float blurDownscaling = 1;
        [Range(1f, 4)]
        public float blurSpread = 1f;

        public float brightness = 1f;

        public float ditherStrength;


        void OnDisable() {
            installed = false;
            if (blurRenderPass != null) {
                blurRenderPass.Cleanup();
            }
        }

        private void OnValidate() {
            brightness = Mathf.Max(0, brightness);
            ditherStrength = Mathf.Max(0, ditherStrength);
        }

        public override void Create() {
            name = "Volumetric Lights";
            vlRenderPass = new VolumetricLightsRenderPass();
            blurRenderPass = new BlurRenderPass();
            shader = Shader.Find("Hidden/VolumetricLights/Blur");
            if (shader == null) {
                return;
            }
        }

        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            Camera cam = renderingData.cameraData.camera;
            if (cam.targetTexture != null && cam.targetTexture.format == RenderTextureFormat.Depth) return; // ignore occlusion cams!

            vlRenderPass.Setup(this);
            blurRenderPass.Setup(shader, renderer, this);
            renderer.EnqueuePass(vlRenderPass);
            renderer.EnqueuePass(blurRenderPass);
            installed = true;
        }
    }
}
