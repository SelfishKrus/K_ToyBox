using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class AsciiRendererFeature : ScriptableRendererFeature
{   

    //////////////
    // Settings // 
    //////////////

    [System.Serializable]
    public class AsciiSettings
    {   
        [Header("Render Pass")]
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Range(1,128)]
        public int downSample = 1;
    }

    //////////////////////
    // Renderer Feature // 
    ////////////////////// 

    public AsciiSettings settings = new AsciiSettings();
    Material m_Material;
    AsciiRenderPass m_RenderPass = null;

    public override void Create()
    {
        m_RenderPass = new AsciiRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {       
        renderer.EnqueuePass(m_RenderPass);
        //m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        //m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Normal);

    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {

    }

    protected override void Dispose(bool disposing)
    {   
        base.Dispose(disposing);
        m_RenderPass.Dispose();
    }

    //////////////////////
    //   Renderer Pass  // 
    //////////////////////

    internal class AsciiRenderPass : ScriptableRenderPass
    {   
        ProfilingSampler m_profilingSampler = new ProfilingSampler("Ascii");
        Material m_material;
        RTHandle m_cameraColorTarget;
        RTHandle rtTempColor0, rtTempColor1;
        AsciiSettings m_settings;

        public AsciiRenderPass(AsciiSettings settings)
        {   
            this.m_settings = settings;
            renderPassEvent = m_settings.renderPassEvent;
        }

        public void SetTarget(RTHandle colorHandle)
        {
            m_cameraColorTarget = colorHandle;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {   

            Dispose();

            var rtDesc = renderingData.cameraData.cameraTargetDescriptor;
            rtDesc.colorFormat = RenderTextureFormat.ARGB32;
            rtDesc.depthBufferBits = 0;

            var downSampleDesc = rtDesc;
            downSampleDesc.width /= m_settings.downSample;
            downSampleDesc.height /= m_settings.downSample;

            // set target
            m_cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Set up temporary color buffer (for blit)
            RenderingUtils.ReAllocateIfNeeded(ref rtTempColor0, downSampleDesc, name: "_RTTempColor0");
            RenderingUtils.ReAllocateIfNeeded(ref rtTempColor1, rtDesc, name: "_RTTempColor1");

            m_material = m_settings.material;

            ConfigureTarget(m_cameraColorTarget);
            ConfigureTarget(rtTempColor0);
            ConfigureTarget(rtTempColor1);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_material == null)
                return;

            if (m_cameraColorTarget.rt == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("");
            using (UnityEngine.Rendering.ProfilingScope profilingScope = new UnityEngine.Rendering.ProfilingScope(cmd, m_profilingSampler))
            {   
                // main
                PassShaderData(m_material, m_settings);

                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, rtTempColor0);
                Blitter.BlitCameraTexture(cmd, rtTempColor0, rtTempColor1, m_material, 0);
                Blitter.BlitCameraTexture(cmd, rtTempColor1, m_cameraColorTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //base.OnCameraCleanup(cmd);
            //Dispose();
        }

        public void Dispose()
        {   
            if (rtTempColor0 != null) rtTempColor0.Release();
            if (rtTempColor1 != null) rtTempColor1.Release();
        }

        #region PRIVATE_METHODS

        void PassShaderData(Material material, AsciiSettings settings)
        {
            material.SetInt("_DownSample", settings.downSample);
        }

        #endregion
    }

}