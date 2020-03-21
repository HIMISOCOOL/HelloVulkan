using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace HelloVulkan
{
    public class HelloTriangleApp
    {
        private IVulkanWindow _window;
        private Vk _vk;
        private Instance _instance;

        public void Run()
        {
            initWindow();
            InitVulkan();
            MainLoop();
            Cleanup();
        }

        private void initWindow()
        {
            var opts = WindowOptions.DefaultVulkan;
            opts.Size = new Size(800, 600);
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
            createInfo.EnabledLayerCount = 0;

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
