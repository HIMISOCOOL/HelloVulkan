namespace HelloVulkan
{
    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily;

        public bool IsComplete => GraphicsFamily.HasValue;
    }
}