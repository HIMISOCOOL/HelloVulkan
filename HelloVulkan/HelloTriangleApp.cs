using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;
using Silk.NET.Vulkan.Extensions.EXT;
using HelloVulkan.extensions;
using Silk.NET.Vulkan.Extensions.KHR;

namespace HelloVulkan
{
    public class HelloTriangleApp
    {
        #region Consts
        private const int WIDTH = 800;
        private const int HEIGHT = 600;
        private readonly string[] _validationLayers = { "VK_LAYER_KHRONOS_validation" };
        private readonly string[] _instanceExtensions = { ExtDebugUtils.ExtensionName };
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
        private SurfaceKHR _surface;

        private PhysicalDevice _physicalDevice;
        private Device _device;
        private Queue _graphicsQueue;
        private Queue _presentQueue;
        private ExtDebugUtils _debugUtils;
        private Vk _vk;
        private KhrSurface _vkSurface;
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

            _window.Initialize();
        }

        private void InitVulkan()
        {
            CreateInstance();
            SetupDebugMessenger();
            CreateSurface();
            PickPhysicalDevice();
            CreateLogicalDevice();
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
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
                ApplicationVersion = Vk.MakeVersion(1, 0),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
                EngineVersion = Vk.MakeVersion(1, 0),
                ApiVersion = Vk.Version11
            };

            var extensions = (byte**)_window.VkSurface.GetRequiredExtensions(out var extCount);
            byte** newExtensions = stackalloc byte*[(int)(extCount + _instanceExtensions.Length)];
            for (int i = 0; i < extCount; i++)
            {
                newExtensions[i] = extensions[i];
            }

            for (var i = 0; i < _instanceExtensions.Length; i++)
            {
                newExtensions[extCount + i] = (byte*)SilkMarshal.MarshalStringToPtr(_instanceExtensions[i]);
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
                createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.MarshalStringArrayToPtr(_validationLayers);
                createInfo.PNext = &debugCreateInfo;
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            fixed (Instance* instance = &_instance)
            {
                Result result = _vk.CreateInstance(&createInfo, null, instance);
                if (result != Result.Success)
                {
                    throw new Exception("Failed to create instance!");
                }
            }
            _vk.CurrentInstance = _instance;

            if (!_vk.TryGetInstanceExtension(_instance, out _vkSurface))
            {
                throw new NotSupportedException("KHR_surface extensions not found");
            }
        }

        private unsafe bool CheckValidationLayerSupport()
        {
            uint layerCount = 0;
            _vk.EnumerateInstanceLayerProperties(&layerCount, (LayerProperties*)null);

            var availableLayers = new LayerProperties[layerCount];
            fixed (LayerProperties* availableLayersPtr = availableLayers)
            {
                _vk.EnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
            }

            foreach (string layerName in _validationLayers)
            {
                var layerFound = false;
                foreach (LayerProperties layerProperties in availableLayers)
                {
                    if (layerName == Marshal.PtrToStringAnsi((IntPtr)layerProperties.LayerName))
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

            fixed (DebugUtilsMessengerEXT* debugMessenger = &_debugMessenger)
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
            string message = Marshal.PtrToStringAnsi((IntPtr)pCallbackData->PMessage);
            string log = $"Severity: [{sev}]\nType: {type}\nMessage: {message}";
            Console.WriteLine(log);

            return Vk.False;
        }
        #endregion

        private unsafe void CreateSurface()
        {
            _surface = _window.VkSurface.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();
        }

        #region Query PhysicalDevice
        private unsafe void PickPhysicalDevice()
        {
            uint deviceCount = 0;
            _vk.EnumeratePhysicalDevices(_instance, &deviceCount, (PhysicalDevice*)null);

            if (deviceCount == 0)
            {
                throw new NotSupportedException("Failed to find GPUs with Vulkan support!");
            }
            PhysicalDevice* devices = stackalloc PhysicalDevice[(int)deviceCount];
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
            var indices = new QueueFamilyIndices();
            uint queueFamilyCount = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, (QueueFamilyProperties*)null);

            QueueFamilyProperties* queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

            for (uint i = 0; i < queueFamilyCount; i++)
            {
                QueueFamilyProperties queueFamily = queueFamilies[i];
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
                {
                    indices.GraphicsFamily = i;
                }

                _vkSurface.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out Bool32 presentSupport);

                if (presentSupport == Vk.True)
                {
                    indices.PresentFamily = i;
                }

                if (indices.IsComplete)
                {
                    break;
                }
            }

            return indices;
        }
        #endregion

        #region LogicalDevice
        private unsafe void CreateLogicalDevice()
        {
            QueueFamilyIndices indices = FindQueueFamilies(_physicalDevice);

            uint[] uniqueQueueFamilies = new[] { indices.GraphicsFamily.Value, indices.PresentFamily.Value };
            var queueCreateInfos = stackalloc DeviceQueueCreateInfo[uniqueQueueFamilies.Length];

            float queuePriority = 1.0f;
            for (int i = 0; i < uniqueQueueFamilies.Length; i++)
            {
                uint queueFamily = uniqueQueueFamilies[i];
                var queueCreateInfo = new DeviceQueueCreateInfo
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = queueFamily,
                    QueueCount = 1,
                    PQueuePriorities = &queuePriority
                };
                queueCreateInfos[i] = queueCreateInfo;
            }

            var deviceFeatures = new PhysicalDeviceFeatures();

            var createInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                PQueueCreateInfos = queueCreateInfos,
                QueueCreateInfoCount = (uint) uniqueQueueFamilies.Length,
                PEnabledFeatures = &deviceFeatures,
                EnabledExtensionCount = 0
            };

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.MarshalStringArrayToPtr(_validationLayers);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
            }

            fixed (Device* device = &_device)
            {
                Result result = _vk.CreateDevice(_physicalDevice, &createInfo, (AllocationCallbacks*)null, device);
                if (result != Result.Success)
                {
                    throw new Exception("Failed to create logical device!");
                }
            }

            fixed (Queue* graphicsQueue = &_graphicsQueue)
            {
                _vk.GetDeviceQueue(_device, indices.GraphicsFamily.Value, 0, graphicsQueue);
            }

            fixed (Queue* presentQueue = &_presentQueue)
            {
                _vk.GetDeviceQueue(_device, indices.PresentFamily.Value, 0, presentQueue);
            }
        }
        #endregion

        private void MainLoop()
        {
            _window.Run();
        }

        private unsafe void Cleanup()
        {
            _vk.DestroyDevice(_device, (AllocationCallbacks*)null);
            if (EnableValidationLayers)
            {
                _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, (AllocationCallbacks*)null);
            }
            _vkSurface.DestroySurface(_instance, _surface, (AllocationCallbacks*)null);
            _vk.DestroyInstance(_instance, (AllocationCallbacks*)null);
        }
    }
}
