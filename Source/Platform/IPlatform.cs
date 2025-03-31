using ImGuiNET;
using UnityEngine;

namespace UImGui.Platform
{
	/// <summary>
	/// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
	/// </summary>
	internal interface IPlatform
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="io"></param>
		/// <param name="config"></param>
		/// <param name="platformName"></param>
		/// <param name="platformIO"></param>
		/// <returns></returns>
		bool Initialize(ImGuiIOPtr io, UIOConfig config, string platformName, ImGuiPlatformIOPtr platformIO);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="io"></param>
		/// <param name="platformIOPtr"></param>
		/// <param name="platformIO"></param>
		void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr platformIOPtr);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="io"></param>
		/// <param name="displayRect"></param>
		void PrepareFrame(ImGuiIOPtr io, Rect displayRect);
	}
}