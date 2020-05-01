using System;
using System.Runtime.InteropServices;
using Veldrid;

namespace YALCT
{
    class Program
    {
        static int Main(string[] args)
        {
            GraphicsBackend backend = GraphicsBackend.OpenGL;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // MacOS gets no choice
                backend = GraphicsBackend.Metal;
            }
            else if (args.Length > 0 && !Enum.TryParse(args[0], true, out backend))
            {
                Console.WriteLine($"Unknown backend type {args[0]} !");
                ShowCLIInfo();
                return 1;
            }

            // technically it works but y'know
            if (backend == GraphicsBackend.OpenGLES)
            {
                Console.WriteLine("Cannot use OpenGL ES !");
                ShowCLIInfo();
                return 1;
            }
            if (backend == GraphicsBackend.Direct3D11 && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Cannot use Direct3D11 on non-windows OS !");
                ShowCLIInfo();
                return 1;
            }

            using (RuntimeContext context = new RuntimeContext(backend))
            {
                context.Run();
            }
            return 0;
        }

        private static void ShowCLIInfo()
        {
            Console.WriteLine("Usage : YALCT <backend>");
            Console.WriteLine("Available options : OpenGL (default), Vulkan, Direct3D11 (Windows only)");
            Console.WriteLine("Not case sensitive");
        }
    }
}
