using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "Rendering/URP/Grab Pass Feature")]
public class GrabPassFeature : ScriptableRendererFeature
{
    class GrabPass : ScriptableRenderPass
    {
        // Use RTHandle for modern URP versions
        private RTHandle m_GrabTextureHandle;
        private readonly string m_TextureName;

        public GrabPass(string textureName)
        {
            this.m_TextureName = textureName;
        }

        // This method is called before executing the render pass.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Get a descriptor that matches the camera's color target.
            var cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTextureDescriptor.depthBufferBits = 0; // We don't need a depth buffer.

            // Allocate the RTHandle and set it as a global texture
            m_GrabTextureHandle = RTHandles.Alloc(cameraTextureDescriptor, name: m_TextureName);
            cmd.SetGlobalTexture(m_TextureName, m_GrabTextureHandle);

            // Declare the source texture.
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // The source is the camera's color buffer, automatically provided by ConfigureInput.
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get(m_TextureName);
            
            // Blit from the source to our grab texture
            Blit(cmd, source, m_GrabTextureHandle);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup allocated resources.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (m_GrabTextureHandle != null)
            {
                RTHandles.Release(m_GrabTextureHandle);
            }
        }
    }

    GrabPass m_ScriptablePass;
    public string TextureName = "_GrabTexture";
    public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingOpaques;

    public override void Create()
    {
        m_ScriptablePass = new GrabPass(TextureName);
        m_ScriptablePass.renderPassEvent = PassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}