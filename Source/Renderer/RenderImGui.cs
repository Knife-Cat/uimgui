using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if HAS_URP
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.UI;
#endif

namespace UImGui.Renderer
{
#if HAS_URP
	public class RenderImGui : ScriptableRendererFeature
	{
		private class CommandBufferPass : ScriptableRenderPass
		{
			public UImGui imguiHandle;

			class PassData
			{
				internal UImGui _imguiHandle;
				internal Rect _cameraPixelRect;
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				
				using(var builder = renderGraph.AddRasterRenderPass<PassData>("Dear IMGUI Pass", out var data))
				{
					UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
					UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

					data._imguiHandle = imguiHandle;
					data._cameraPixelRect = cameraData.camera.pixelRect;
					
					builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
					
					builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
					{
						passData._imguiHandle?.DoUpdate(context.cmd, data._cameraPixelRect);
					});
				}
			}
		}

		[HideInInspector]
		public Camera Camera;
		public CommandBuffer CommandBuffer;
		[HideInInspector]
		public UImGui _Imgui;
		public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

		private CommandBufferPass _commandBufferPass;

		public override void Create()
		{
			_commandBufferPass = new CommandBufferPass()
			{
				renderPassEvent = RenderPassEvent,
			};
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			//if (CommandBuffer == null) return;
			//if (Camera != renderingData.cameraData.camera) return;

			_commandBufferPass.renderPassEvent = RenderPassEvent;
			_commandBufferPass.imguiHandle = _Imgui;
			renderer.EnqueuePass(_commandBufferPass);
		}
	}
#else
	public class RenderImGui : UnityEngine.ScriptableObject
	{
		public CommandBuffer CommandBuffer;
	}
#endif
}
