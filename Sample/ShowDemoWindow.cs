using ImGuiNET;
#if !UIMGUI_REMOVE_IMNODES
using imnodesNET;
#endif
#if !UIMGUI_REMOVE_IMPLOT
using ImPlotNET;
using System.Linq;
#endif
#if !UIMGUI_REMOVE_IMGUIZMO
using ImGuizmoNET;
#endif
using UnityEngine;

namespace UImGui
{
	public class ShowDemoWindow : MonoBehaviour
	{
		
		[ImguiLayout]
		private static void OnLayout(UImGui uImGui)
		{
			ImGui.ShowDemoWindow();

			ImGui.Begin("Test");
			ImGui.Button("This button.");
			ImGui.End();
		}
		
	}
}

