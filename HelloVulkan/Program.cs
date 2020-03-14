using System;

namespace HelloVulkan
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new HelloTriangleApp();
            try
            {
                app.Run();
            }
            catch (System.Exception e)
            {

                throw;
            }
        }
    }
}
