﻿using ImGuiNET;
using System;
using UImGui.Assets;
using UnityEngine;
using UnityEngine.Assertions;

namespace UImGui.Platform
{
    /// <summary>
    /// TODO: Write all methods a this base class usage.
    /// </summary>
    internal class PlatformBase : IPlatform
    {
        protected readonly IniSettingsAsset _iniSettings;
        protected readonly CursorShapesAsset _cursorShapes;

        protected readonly PlatformCallbacks _callbacks = new PlatformCallbacks();

        protected ImGuiMouseCursor _lastCursor = ImGuiMouseCursor.COUNT;

        internal PlatformBase(CursorShapesAsset cursorShapes, IniSettingsAsset iniSettings)
        {
            _cursorShapes = cursorShapes;
            _iniSettings = iniSettings;
        }

        public virtual bool Initialize(ImGuiIOPtr io, UIOConfig config, string platformName,
            ImGuiPlatformIOPtr platformIO)
        {
            io.SetBackendPlatformName("Unity Input System");
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

            io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
            io.WantSetMousePos = true;


            unsafe
            {
                PlatformCallbacks.SetClipboardFunctions(PlatformCallbacks.GetClipboardTextCallback,
                    PlatformCallbacks.SetClipboardTextCallback);
            }

            _callbacks.Assign(platformIO);

            if (_iniSettings != null)
            {
                io.SetIniFilename(null);
                ImGui.LoadIniSettingsFromMemory(_iniSettings.Load());
            }

            return true;
        }

        public virtual void PrepareFrame(ImGuiIOPtr io, Rect displayRect)
        {
            Assert.IsTrue(io.Fonts.IsBuilt(),
                "Font atlas not built! Generally built by the renderer. Missing call to renderer NewFrame() function?");

            io.DisplaySize = displayRect.size; // TODO: dpi aware, scale, etc.

            io.DeltaTime = Time.unscaledDeltaTime;

            if (_iniSettings != null && io.WantSaveIniSettings)
            {
                _iniSettings.Save(ImGui.SaveIniSettingsToMemory());
                io.WantSaveIniSettings = false;
            }
        }

        public virtual void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr platformIOPtr)
        {
            io.SetBackendPlatformName(null);

            _callbacks.Unset(platformIOPtr);
        }

        protected void UpdateCursor(ImGuiIOPtr io, ImGuiMouseCursor cursor)
        {
            if (io.MouseDrawCursor)
            {
                cursor = ImGuiMouseCursor.None;
            }

            if (_lastCursor == cursor) return;
            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) return;

            _lastCursor = cursor;
            Cursor.visible =
                cursor != ImGuiMouseCursor.None; // Hide cursor if ImGui is drawing it or if it wants no cursor.
            if (_cursorShapes != null)
            {
                Cursor.SetCursor(_cursorShapes[cursor].Texture, _cursorShapes[cursor].Hotspot, CursorMode.Auto);
            }
        }
    }
}