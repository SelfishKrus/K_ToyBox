using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class TemplateRendererFeature : ScriptableRendererFeature
{   

    //////////////
    // Settings // 
    //////////////

    [System.Serializable]
    public class TemplateSettings
    {   
        [Header("Render Pass")]
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Range(1,32)]
        public int downSample = 1;
    }

    //////////////////////
    // Renderer Feature // 
    //////////////////////

    public TemplateSettings settings = new TemplateSettings();
    Material m_Material;
    TemplateRenderPass m_RenderPass = null;

    public override void Create()
    {
        m_RenderPass = new TemplateRenderPass(settings);
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

    internal class TemplateRenderPass : ScriptableRenderPass
    {   
        ProfilingSampler m_profilingSampler = new ProfilingSampler("Template");
        Material m_material;
        RTHandle m_cameraColorTarget;
        RTHandle rtTempColor0, rtTempColor1;
        TemplateSettings m_settings;

        public TemplateRenderPass(TemplateSettings settings)
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
            rtDesc.height /= m_settings.downSample;
            rtDesc.width /= m_settings.downSample;

            // set target
            m_cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Set up temporary color buffer (for blit)
            RenderingUtils.ReAllocateIfNeeded(ref rtTempColor0, rtDesc, name: "_RTTempColor0");
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

        #endregion
    }

}