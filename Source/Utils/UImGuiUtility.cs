using ImGuiNET;
using System;
using System.Linq;
using System.Reflection;
using UImGui.Texture;
using UnityEngine;
using UTexture = UnityEngine.Texture;

namespace UImGui
{
    public static class UImGuiUtility
    {
        public static IntPtr GetTextureId(UTexture texture) =>
            Context?.TextureManager.GetTextureId(texture) ?? IntPtr.Zero;

        internal static SpriteInfo GetSpriteInfo(Sprite sprite) =>
            Context?.TextureManager.GetSpriteInfo(sprite) ?? null;

        internal static Context Context;

        #region Events

        public static event Action<UImGui> Layout;
        public static event Action<UImGui> OnInitialize;
        public static event Action<UImGui> OnDeinitialize;
        internal static void DoLayout(UImGui uimgui) => Layout?.Invoke(uimgui);

        internal static void DoOnInitialize(UImGui uimgui)
        {
            GatherFunctions<ImguiLayoutAttribute>(ref Layout);
            GatherFunctions<ImGuiInitializeAttribute>(ref OnInitialize);
            GatherFunctions<ImGuiDeinitializeAttribute>(ref OnDeinitialize);
            OnInitialize?.Invoke(uimgui);
        }

        internal static void DoOnDeinitialize(UImGui uimgui) => OnDeinitialize?.Invoke(uimgui);

        #endregion


        internal static unsafe Context CreateContext()
        {
            return new Context
            {
                ImGuiContext = ImGui.CreateContext(),
#if !UIMGUI_REMOVE_IMPLOT
                ImPlotContext = ImPlotNET.ImPlot.CreateContext(),
#endif
                TextureManager = new TextureManager()
            };
        }

        internal static void DestroyContext(Context context)
        {
#if !UIMGUI_REMOVE_IMPLOT
            ImPlotNET.ImPlot.DestroyContext(context.ImPlotContext);
#endif
            ImGui.DestroyContext(context.ImGuiContext);
        }

        internal static void SetCurrentContext(Context context)
        {
            Context = context;
            ImGui.SetCurrentContext(context?.ImGuiContext ?? IntPtr.Zero);

#if !UIMGUI_REMOVE_IMPLOT
            ImPlotNET.ImPlot.SetImGuiContext(context?.ImGuiContext ?? IntPtr.Zero);
            ImPlotNET.ImPlot.SetCurrentContext(context?.ImPlotContext ?? IntPtr.Zero);
#endif
#if !UIMGUI_REMOVE_IMGUIZMO
            ImGuizmoNET.ImGuizmo.SetImGuiContext(context?.ImGuiContext ?? IntPtr.Zero);
#endif
#if !UIMGUI_REMOVE_IMNODES

#endif
        }

        private static void GatherFunctions<TAttribute>(ref Action<UImGui> imgui)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (assemblyName.StartsWith("System") ||
                    assemblyName.StartsWith("Unity") ||
                    assemblyName.StartsWith("UnityEditor") ||
                    assemblyName.StartsWith("UnityEngine"))
                {
                    continue;
                }

                var methods = assembly.GetTypes()
                    .SelectMany(ty => ty.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                    BindingFlags.Instance))
                    .Where(meth => meth.GetCustomAttributes(typeof(TAttribute), false).Length > 0)
                    .ToArray();
                
                foreach (var methodInfo in methods)
                {
                    if (methodInfo.IsStatic)
                    {
                        try
                        {
                            Layout += methodInfo.CreateDelegate(typeof(Action<UImGui>)) as Action<UImGui>;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(
                                $"Imgui Method \"{methodInfo.Name}\" has caused an exception. Ensure that the function is in the following signature: \"static void MyFunc(UImgui imgui)\"");
                            Debug.LogError($"Thrown Exception : {ex}");
                        }
                    }
                    else
                    {
                        Debug.LogError(
                            $"Method \"{methodInfo.Name}\" with the attribute [{typeof(TAttribute).Name}] is an instance method but not set to static. Please set the function as a static. This function will be ignored.");
                    }
                }
            }
        }
    }
}