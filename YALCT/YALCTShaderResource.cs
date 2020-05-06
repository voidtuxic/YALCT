using System;
using System.Numerics;

namespace YALCT
{
    public struct YALCTShaderResource
    {
        public string UID;
        public string Name;
        public YALCTFilePickerItem FileItem;
        public Vector2 Size;
        public IntPtr ImguiBinding;

        public YALCTShaderResource(YALCTFilePickerItem fileItem, Vector2 size, IntPtr imguiBinding)
        {
            UID = Guid.NewGuid().ToString("N");
            Name = fileItem.Name;
            FileItem = fileItem;
            Size = size;
            ImguiBinding = imguiBinding;
        }
    }
}