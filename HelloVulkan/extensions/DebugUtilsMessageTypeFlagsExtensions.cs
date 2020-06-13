using Silk.NET.Vulkan;

namespace HelloVulkan.extensions
{
    public static class DebugUtilsMessageTypeFlagsExtensions
    {
        public static string ToReadableString(this DebugUtilsMessageTypeFlagsEXT messageType) {
            switch (messageType)
            {
                case DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt:
                    return "General";
                case DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt:
                    return "Validation";
                case DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt:
                    return "Performance";
                default:
                    return messageType.ToString();
            }
        }
    }
}