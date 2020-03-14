using System;
using System.Drawing;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace HelloVulkan
{
    public class HelloTriangleApp
    {
        private IVulkanWindow _window;

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

        }

        private void MainLoop()
        {
            _window.Run();
        }

        private void Cleanup()
        {

        }
    }
}
