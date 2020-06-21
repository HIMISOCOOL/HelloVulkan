namespace HelloVulkan
{
    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily;

        public uint? PresentFamily;

        public bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}