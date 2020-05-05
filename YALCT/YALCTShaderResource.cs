using System;
using System.Numerics;

namespace YALCT
{
    public struct YALCTShaderResource
    {
        public string UID;
        public string Name;
        public Vector2 Size;
        public IntPtr ImguiBinding;

        public YALCTShaderResource(string name, Vector2 size, IntPtr imguiBinding)
        {
            UID = Guid.NewGuid().ToString("N");
            Name = name;
            Size = size;
            ImguiBinding = imguiBinding;
        }
    }
}