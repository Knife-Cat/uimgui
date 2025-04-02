using System;

namespace UImGui
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ImguiLayoutAttribute : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ImGuiInitializeAttribute : Attribute
    {
        
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class ImGuiDeinitializeAttribute : Attribute
    {
        
    }
}