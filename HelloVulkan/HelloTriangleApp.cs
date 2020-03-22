using System;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;
using Silk.NET.Vulkan.Extensions.EXT;

namespace HelloVulkan
{
    public class HelloTriangleApp
    {
        private const int WIDTH = 800;
        private const int HEIGHT = 600;
        private string[] _validationLayers = {"VK_LAYER_KHRONOS_validation"};

        private IVulkanWindow _window;
        private Vk _vk;
        private ExtDebugUtils _debugUtils;
        private Instance _instance;
        private DebugUtilsMessengerEXT _debugMessenger;
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
            SetupDebugMessenger();
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

            char** extensions = GetRequiredExtensions(out uint extCount);

            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = extCount,
                PpEnabledExtensionNames = (byte**) extensions
            };

            // debug info is here to make sure it doesnt get destroyed before vk.CreateInstance
            var debugCreateInfo = new DebugUtilsMessengerCreateInfoEXT();
            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint) _validationLayers.Length;
                createInfo.PpEnabledLayerNames =  (byte**) SilkMarshal.MarshalStringArrayToPtr(_validationLayers);

                PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                createInfo.PNext = &debugCreateInfo;
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            fixed(Instance* instance = &_instance)
            {
                Result result = _vk.CreateInstance(&createInfo, null, instance);
                if (result != Result.Success)
                {
                    throw new Exception("Failed to create instance!");
                }
            }
            _vk.CurrentInstance = _instance;
        }

        /// <summary>
        /// Gets the list of required extensions.
        /// Appends debug utils if validation layers are enabled
        /// </summary>
        /// <param name="glfwExtensionCount">An out param with the count of extensions</param>
        /// <returns>The array of extensions</returns>
        private unsafe char** GetRequiredExtensions(out uint glfwExtensionCount) {
            glfwExtensionCount = 0;
            char** glfwExtensions = _window.GetRequiredExtensions(out glfwExtensionCount);

            if (EnableValidationLayers)
            {
                // TODO: find where the constant for VK_EXT_DEBUG_UTILS_EXTENSION_NAME lives
                string[] glfwExtensionsArray = SilkMarshal.MarshalPtrToStringArray((IntPtr)glfwExtensions, (int)glfwExtensionCount).Append("VK_EXT_debug_utils").ToArray();
                return (char**)SilkMarshal.MarshalStringArrayToPtr(glfwExtensionsArray.ToArray());
            }

            return glfwExtensions;
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

        private unsafe void SetupDebugMessenger()
        {
            if (!EnableValidationLayers || !_vk.TryGetExtension(out _debugUtils))
            {
                return;
            }

            var createInfo = new DebugUtilsMessengerCreateInfoEXT();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            fixed(DebugUtilsMessengerEXT* debugMessenger = &_debugMessenger)
            {
                Result result = _debugUtils.CreateDebugUtilsMessenger(_instance, &createInfo, null, debugMessenger);
                if (result != Result.Success)
                {
                    throw new SystemException("Failed to setup Debug Messenger");
                }
            }
        }

        private unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
            createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt
            | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt
            | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt;
            createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt
            | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt
            | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt;
            createInfo.PfnUserCallback = FuncPtr.Of<DebugUtilsMessengerCallbackFunctionEXT>(DebugCallback);
        }

        private unsafe uint DebugCallback(
            DebugReportFlagsEXT debugFlags,
            DebugReportObjectTypeEXT debugObjectType,
            ulong @object,
            UIntPtr location,
            int messageCode,
            char* pLayerPrefix,
            char* pMessage,
            void* pUserData)
        {
            string flags = debugFlags.ToString().Replace("DebugReport", string.Empty);
            string layerPrefix = Marshal.PtrToStringAnsi((IntPtr) pLayerPrefix);
            string objectType = debugObjectType.ToString().Replace("DebugReportObjectType", string.Empty);
            string message = SilkMarshal.MarshalPtrToString((IntPtr) pLayerPrefix);
            string log = $"Flags: [{flags}]\nObjectType: {objectType}\nCode: {messageCode} Layer Prefix: {layerPrefix}/\nMessage: {message}";
            Console.WriteLine();

            return Vk.False;
        }

        private void MainLoop()
        {
            _window.Run();
        }

        private unsafe void Cleanup()
        {
            if (EnableValidationLayers)
            {
                _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
            }

            _vk.DestroyInstance(_instance, null);
        }
    }
}
