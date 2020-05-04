using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;

namespace YALCT
{
    [StructLayout(LayoutKind.Sequential)]
    public struct YALCTRuntimeData
    {
        // size must be 16 multiple limitation
        public static uint Size => 48;// (uint)Unsafe.SizeOf<YALCTRuntimeData>();

        public Vector4 mouse;
        public Vector2 resolution;
        public float time;
        public float deltaTime;
        public int frame;

        public void Update(Sdl2Window window, InputSnapshot input, float newDeltaTime)
        {
            Vector2 mousePosition = RuntimeOptions.Current.InvertMouseY ? input.MousePosition : new Vector2(input.MousePosition.X, window.Height - input.MousePosition.Y);
            mouse = new Vector4(mousePosition,
                                input.IsMouseDown(MouseButton.Left) ? 1 : 0,
                                input.IsMouseDown(MouseButton.Right) ? 1 : 0);
            resolution = new Vector2(window.Width, window.Height);
            time += newDeltaTime;
            deltaTime = newDeltaTime;
            frame++;
        }
    }
}