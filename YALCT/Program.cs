using System;

namespace YALCT
{
    class Program
    {
        static void Main(string[] args)
        {
            using (RuntimeContext context = new RuntimeContext(args))
            {
                context.Run();
            }
        }
    }
}
