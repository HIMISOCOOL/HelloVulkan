using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace HelloVulkan
{
    public class HelloTriangleApp
    {
        private const int WIDTH = 800;
        private const int HEIGHT = 600;
        private string[] _validationLayers = {"VK_LAYER_KHRONOS_validation"};

        private IVulkanWindow _window;
        private Vk _vk;
        private Instance _instance;

#if DEBUG
        private readonly bool EnableValidationLayers = true;
#else
        private readonly bool EnableValidationLayers = false;
#endif

        public void Run()
        {
            InitWindow();
            InitVulkan();
            MainLoop();
            Cleanup();
        }

        private void InitWindow()
        {
            var opts = WindowOptions.DefaultVulkan;
            opts.Size = new Size(WIDTH, HEIGHT);
            opts.WindowBorder = WindowBorder.Fixed;
            opts.Title = "Vulkan";
            opts.API = GraphicsAPI.DefaultVulkan;
            _window = Window.Create(opts) as IVulkanWindow;
            if (_window == null || !_window.IsVulkanSupported)
            {
                throw new NotSupportedException("Windowing does not Support Vulkan");
            }
        }

        private void InitVulkan()
        {
            CreateInstance();
        }

        private unsafe void CreateInstance()
        {
            _vk = Vk.GetApi();

            if (EnableValidationLayers && !CheckValidationLayerSupport())
            {
                throw new NotSupportedException("Validation layers requested but not available!");
            }

            var appInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName =  (byte*) Marshal.StringToHGlobalAnsi("Hello Triangle"),
                ApplicationVersion = Vk.MakeVersion(1, 0),
                PEngineName = (byte*) Marshal.StringToHGlobalAnsi("No Engine"),
                EngineVersion = Vk.MakeVersion(1, 0),
                ApiVersion = Vk.Version10
            };

            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            char** extensions = _window.GetRequiredExtensions(out var extCount);
            createInfo.EnabledExtensionCount = extCount;
            createInfo.PpEnabledExtensionNames = (byte**) extensions;

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint) _validationLayers.Length;
                createInfo.PpEnabledLayerNames =  (byte**) SilkMarshal.MarshalStringArrayToPtr(_validationLayers);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
            }

            fixed(Instance* instance = &_instance)
            {
                var result = _vk.CreateInstance(&createInfo, null, instance);
                if (result != Result.Success)
                {
                    throw new Exception("Failed to create instance!");
                }
            }
            _vk.CurrentInstance = _instance;
        }

        private unsafe bool CheckValidationLayerSupport()
        {
            uint layerCount = 0;
            _vk.EnumerateInstanceLayerProperties(&layerCount, (LayerProperties*) 0);

            var availableLayers = new LayerProperties[layerCount];
            fixed(LayerProperties* availableLayersPtr = availableLayers)
            {
                _vk.EnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
            }

            foreach (string layerName in _validationLayers)
            {
                var layerFound = false;
                foreach (LayerProperties layerProperties in availableLayers)
                {
                    if (layerName == Marshal.PtrToStringAnsi((IntPtr) layerProperties.LayerName))
                    {
                        layerFound = true;
                        break;
                    }
                }

                if (!layerFound)
                {
                    return false;
                }
            }

            return true;
        }

        private void MainLoop()
        {
            _window.Run();
        }

        private unsafe void Cleanup()
        {
            _vk.DestroyInstance(_instance, (AllocationCallbacks*) null);
        }
    }
}
