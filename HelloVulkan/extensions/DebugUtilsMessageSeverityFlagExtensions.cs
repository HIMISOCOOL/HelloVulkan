using Silk.NET.Vulkan;

namespace HelloVulkan.extensions
{
    public static class DebugUtilsMessageSeverityFlagExtensions
    {
        public static string ToReadableString(this DebugUtilsMessageSeverityFlagsEXT severity) {
            switch (severity)
            {
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt:
                    return "Verbose";
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt:
                    return "Information";
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt:
                    return "Warning";
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt:
                    return "Error";
                default:
                    return severity.ToString();
            }
        }
    }
}