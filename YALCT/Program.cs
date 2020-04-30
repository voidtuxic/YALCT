using System;

namespace YALCT
{
    class Program
    {
        static void Main(string[] args)
        {
            using (RuntimeContext context = new RuntimeContext(args, Veldrid.GraphicsBackend.Direct3D11))
            {
                context.Run();
            }
        }
    }
}
