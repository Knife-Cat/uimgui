using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ImGuiNET;
using UImGui.Assets;
using UImGui.Events;
using UImGui.Platform;
using UImGui.Renderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace UImGui
{
    // TODO: Check Multithread run.
    public class UImGui : MonoBehaviour
    {
        private Context _context;
        private IRenderer _renderer;
        private IPlatform _platform;
        private CommandBuffer _renderCommandBuffer;
        
        
        [SerializeField] private Camera _camera = null;

        private RenderImGui _renderFeature = null;

        [SerializeField] private RenderType _rendererType = RenderType.Mesh;

        [SerializeField] private InputType _platformType = InputType.InputManager;

        [Tooltip("Null value uses default imgui.ini file.")] [SerializeField]
        private IniSettingsAsset _iniSettings = null;

        [Header("Configuration")] [SerializeField]
        private UIOConfig _initialConfiguration = new UIOConfig
        {
            ImGuiConfig = ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable,

            DoubleClickTime = 0.30f,
            DoubleClickMaxDist = 6.0f,

            DragThreshold = 6.0f,

            KeyRepeatDelay = 0.250f,
            KeyRepeatRate = 0.050f,

            FontGlobalScale = 1.0f,
            FontAllowUserScaling = false,

            DisplayFramebufferScale = Vector2.one,

            MouseDrawCursor = false,
            TextCursorBlink = false,

            ResizeFromEdges = true,
            MoveFromTitleOnly = true,
            ConfigMemoryCompactTimer = 1f,
        };

        [SerializeField] private FontInitializerEvent _fontCustomInitializer = new FontInitializerEvent();

        [SerializeField] private FontAtlasConfigAsset _fontAtlasConfiguration = null;

        [Header("Customization")] [SerializeField]
        private ShaderResourcesAsset _shaders = null;

        [SerializeField] private StyleAsset _style = null;

        [SerializeField] private CursorShapesAsset _cursorShapes = null;

        [SerializeField] private bool _doGlobalEvents = true; // Do global/default Layout event too.

        private bool _isChangingCamera = false;

        public CommandBuffer CommandBuffer => _renderCommandBuffer;

        #region Events

        public event System.Action<UImGui> Layout;
        public event System.Action<UImGui> OnInitialize;
        public event System.Action<UImGui> OnDeinitialize;

        #endregion

        public void Reload()
        {
            OnDisable();
            OnEnable();
        }

        public void SetUserData(System.IntPtr userDataPtr)
        {
            _initialConfiguration.UserData = userDataPtr;
            ImGuiIOPtr io = ImGui.GetIO();
            _initialConfiguration.ApplyTo(io);
        }

        public void SetCamera(Camera camera)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == _camera)
            {
                Debug.LogWarning($"Trying to change to same camera. Camera: {camera}", camera);
                return;
            }

            _camera = camera;
            _isChangingCamera = true;
        }

        private void Awake()
        {
        }


        private void OnDestroy()
        {
            UImGuiUtility.DestroyContext(_context);
        }

        private void OnEnable()
        {
            void Fail(string reason)
            {
                enabled = false;
                throw new System.Exception($"Failed to start: {reason}.");
            }

            _context = UImGuiUtility.CreateContext();
            
            if (RenderUtility.IsUsingURP())
            {
                //TODO: This is shitty as fuck, but i dont think its worth it to manually get the rendering features all the time.
                //Change this to a slightly better system.
                var renderingFeatures = Camera.main.GetComponent<UniversalAdditionalCameraData>();
                var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (property == null)
                {
                    Fail("Failed to get scriptable render data.");
                    return;
                }
                
                List<ScriptableRendererFeature> features =
                    property.GetValue(renderingFeatures.scriptableRenderer) as List<ScriptableRendererFeature>;

                if (features == null)
                {
                    Fail("Failed to get render features from pipeline.");
                }
                
                
                foreach (var feature in features)
                {
                    if (feature is RenderImGui imguiFeature)
                    {
                        _renderFeature = imguiFeature;
                    }
                    
                }
                
                if (_renderFeature == null)
                {
                    Debug.LogError("Add the imgui render feature to the render pipeline asset!!");
                }
            }

          //  _renderCommandBuffer = RenderUtility.GetCommandBuffer(Constants.UImGuiCommandBuffer);

            if (RenderUtility.IsUsingURP())
            {
#if HAS_URP
                _renderFeature.Camera = _camera ?? Camera.main;
                _renderFeature._Imgui = this;
#endif
                _renderFeature.CommandBuffer = _renderCommandBuffer;
                
                
            }
            else if (!RenderUtility.IsUsingHDRP())
            {
                _camera.AddCommandBuffer(CameraEvent.AfterEverything, _renderCommandBuffer);
            }

            UImGuiUtility.SetCurrentContext(_context);

            ImGuiIOPtr io = ImGui.GetIO();
            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();

            _initialConfiguration.ApplyTo(io);
            _style?.ApplyTo(ImGui.GetStyle());

            _context.TextureManager.BuildFontAtlas(io, _fontAtlasConfiguration, _fontCustomInitializer);
            _context.TextureManager.Initialize(io);

            IPlatform platform = PlatformUtility.Create(_platformType, _cursorShapes, _iniSettings);
            SetPlatform(platform, io, platformIO);
            if (_platform == null)
            {
                Fail(nameof(_platform));
            }

            SetRenderer(RenderUtility.Create(_rendererType, _shaders, _context.TextureManager), io);
            if (_renderer == null)
            {
                Fail(nameof(_renderer));
            }

            if (_doGlobalEvents)
            {
                UImGuiUtility.DoOnInitialize(this);  
            }

            OnInitialize?.Invoke(this);
        }

        private void OnDisable()
        {
            UImGuiUtility.SetCurrentContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();
            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();


            SetRenderer(null, io);
            SetPlatform(null, io, platformIO);

            UImGuiUtility.SetCurrentContext(null);

            _context.TextureManager.Shutdown();
            _context.TextureManager.DestroyFontAtlas(io);

            if (RenderUtility.IsUsingURP())
            {
                if (_renderFeature != null)
                {
#if HAS_URP
                    _renderFeature.Camera = null;
                    _renderFeature._Imgui = null;
#endif
                    _renderFeature.CommandBuffer = null;
                }
            }
            else if (!RenderUtility.IsUsingHDRP())
            {
                if (_camera != null)
                {
                    _camera.RemoveCommandBuffer(CameraEvent.AfterEverything, _renderCommandBuffer);
                }
            }

            if (_renderCommandBuffer != null)
            {
                RenderUtility.ReleaseCommandBuffer(_renderCommandBuffer);
            }

            _renderCommandBuffer = null;

            if (_doGlobalEvents)
            {
                UImGuiUtility.DoOnDeinitialize(this);
            }

            OnDeinitialize?.Invoke(this);

            Layout = null;
        }

        private void Update()
        {
            
            if (RenderUtility.IsUsingHDRP() || RenderUtility.IsUsingURP())
                return; // skip update call in hdrp
        }

        

        internal void DoUpdate(RasterCommandBuffer cmd, Rect pixelRect)
        {
            if (_platform == null) return;
            UImGuiUtility.SetCurrentContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();

            Constants.PrepareFrameMarker.Begin(this);
            _platform.PrepareFrame(io, pixelRect);
            _context.TextureManager.PrepareFrame(io);
            ImGui.NewFrame();
#if !UIMGUI_REMOVE_IMGUIZMO
            ImGuizmoNET.ImGuizmo.BeginFrame();
#endif
            Constants.PrepareFrameMarker.End();

            Constants.LayoutMarker.Begin(this);
            try
            {
                if (_doGlobalEvents)
                {
                    UImGuiUtility.DoLayout(this);
                }

                Layout?.Invoke(this);
            }
            finally
            {
                ImGui.Render();
                Constants.LayoutMarker.End();
            }

            Constants.DrawListMarker.Begin(this);
           // _renderCommandBuffer.Clear();
            _renderer.RenderDrawLists(cmd, ImGui.GetDrawData());
            Constants.DrawListMarker.End();

            if (_isChangingCamera)
            {
                _isChangingCamera = false;
                Reload();
            }
        }

        private void SetRenderer(IRenderer renderer, ImGuiIOPtr io)
        {
            _renderer?.Shutdown(io);
            _renderer = renderer;
            _renderer?.Initialize(io);
        }

        private void SetPlatform(IPlatform platform, ImGuiIOPtr io, ImGuiPlatformIOPtr platformIO)
        {
            _platform?.Shutdown(io, platformIO);
            _platform = platform;
            _platform?.Initialize(io, _initialConfiguration, "Unity " + _platformType.ToString(), platformIO);
        }
    }
}