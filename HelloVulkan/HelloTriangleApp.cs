using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;
using Silk.NET.Vulkan.Extensions.EXT;
using HelloVulkan.extensions;

namespace HelloVulkan
{
    public class HelloTriangleApp
    {
#region Cosnts
        private const int WIDTH = 800;
        private const int HEIGHT = 600;
        private readonly string[] _validationLayers = {"VK_LAYER_KHRONOS_validation"};
        private readonly string[] _instanceExtensions = {ExtDebugUtils.ExtensionName};
    #if DEBUG
        private readonly bool EnableValidationLayers = true;
    #else
        private readonly bool EnableValidationLayers = false;
    #endif
#endregion

#region  Instance Variables
        private IWindow _window;
        private Instance _instance;
        private DebugUtilsMessengerEXT _debugMessenger;
        private PhysicalDevice _physicalDevice;
        private ExtDebugUtils _debugUtils;
        private Vk _vk;
#endregion

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
            _window = Window.Create(opts);
            if (_window == null || _window.VkSurface == null)
            {
                throw new NotSupportedException("Windowing does not Support Vulkan");
            }
        }

        private void InitVulkan()
        {
            CreateInstance();
            SetupDebugMessenger();
            PickPhysicalDevice();
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
                ApiVersion = Vk.Version11
            };

            var extensions = (byte**) _window.VkSurface.GetRequiredExtensions(out var extCount);
            byte** newExtensions = stackalloc byte*[(int)(extCount + _instanceExtensions.Length)];
            for (int i = 0; i < extCount; i++)
            {
                newExtensions[i] = extensions[i];
            }

            for (var i = 0; i < _instanceExtensions.Length; i++)
            {
                newExtensions[extCount + i] = (byte*) SilkMarshal.MarshalStringToPtr(_instanceExtensions[i]);
            }

            extCount += (uint)_instanceExtensions.Length;

            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = extCount,
                PpEnabledExtensionNames = newExtensions
            };

            // debug info is here to make sure it doesnt get destroyed before vk.CreateInstance
            var debugCreateInfo = new DebugUtilsMessengerCreateInfoEXT();
            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint) _validationLayers.Length;
                createInfo.PpEnabledLayerNames =  (byte**) SilkMarshal.MarshalStringArrayToPtr(_validationLayers);
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

        private unsafe bool CheckValidationLayerSupport()
        {
            uint layerCount = 0;
            _vk.EnumerateInstanceLayerProperties(&layerCount, (LayerProperties*) null);

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

#region DebugMessenger
        private unsafe void SetupDebugMessenger()
        {
            if (!EnableValidationLayers || !_vk.TryGetInstanceExtension(_instance, out _debugUtils))
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
            DebugUtilsMessageSeverityFlagsEXT messageSeverity,
            DebugUtilsMessageTypeFlagsEXT messageTypes,
            DebugUtilsMessengerCallbackDataEXT* pCallbackData,
            void* pUserData
        )
        {
            string sev = messageSeverity.ToReadableString();
            string type = messageTypes.ToReadableString();
            string message = Marshal.PtrToStringAnsi((IntPtr) pCallbackData->PMessage);
            string log = $"Severity: [{sev}]\nType: {type}\nMessage: {message}";
            Console.WriteLine(log);

            return Vk.False;
        }
#endregion

#region Query PhysicalDevice
        private unsafe void PickPhysicalDevice()
        {
            uint deviceCount = 0;
            _vk.EnumeratePhysicalDevices(_instance, &deviceCount, (PhysicalDevice*) null);

            if (deviceCount == 0) {
                throw new NotSupportedException("Failed to find GPUs with Vulkan support!");
            }
            PhysicalDevice* devices = stackalloc PhysicalDevice[(int) deviceCount];
            _vk.EnumeratePhysicalDevices(_instance, &deviceCount, devices);

            for (int i = 0; i < deviceCount; i++)
            {
                PhysicalDevice device = devices[i];
                if (IsDeviceSuitable(device))
                {
                    _physicalDevice = device;
                    return;
                }
            }
            throw new Exception("No suitable device.");
        }

        private unsafe bool IsDeviceSuitable(PhysicalDevice device)
        {
            QueueFamilyIndices indices = FindQueueFamilies(device);

            return indices.IsComplete;
        }

        private unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            QueueFamilyIndices indices = new QueueFamilyIndices();
            uint queueFamilyCount = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, (QueueFamilyProperties*) null);

            QueueFamilyProperties* queueFamilies = stackalloc QueueFamilyProperties[(int) queueFamilyCount];
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

            for (uint i = 0; i < queueFamilyCount; i++)
            {
                QueueFamilyProperties queueFamily = queueFamilies[i];
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
                {
                    indices.GraphicsFamily = i;
                }

                if (indices.IsComplete)
                {
                    break;
                }
            }

            return indices;
        }
#endregion

        private void MainLoop()
        {
            _window.Run();
        }

        private unsafe void Cleanup()
        {
            if (EnableValidationLayers)
            {
                _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, (AllocationCallbacks*) null);
            }

            _vk.DestroyInstance(_instance, (AllocationCallbacks*) null);
        }
    }
}
