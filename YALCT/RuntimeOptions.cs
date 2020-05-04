using System.Numerics;
using ImGuiNET;
using Veldrid;

namespace YALCT
{
    public class RuntimeOptions
    {
        #region singleton
        private static readonly RuntimeOptions current = new RuntimeOptions();
        public static RuntimeOptions Current { get { return current; } }
        #endregion

        private const int OPTIONSWIDTH = 150;
        private const int OPTIONSHEIGHT = 170;

        private bool showOptions = false;
#if DEBUG
        private bool fullscreen = false;
#else
        private bool fullscreen = true;
#endif
        private bool vsync = true;
        private bool invertMouseY = false;
        private float uiAlpha = 0.75f;

        public bool InvertMouseY => invertMouseY;
        public float UiAlpha { get => uiAlpha; set => uiAlpha = value; }
        public bool ShowOptions { get => showOptions; set => showOptions = value; }

        private RuntimeOptions()
        {

        }

        private Vector2 GetOptionsSize()
        {
            return new Vector2(OPTIONSWIDTH, OPTIONSHEIGHT);
        }

        public void Apply(RuntimeContext context)
        {
            // app context
            context.Window.WindowState = fullscreen ? WindowState.BorderlessFullScreen : WindowState.Maximized;
            context.GraphicsDevice.SyncToVerticalBlank = vsync;

            // imgui context
            ImGui.GetStyle().Alpha = uiAlpha;
        }

        public void SubmitUI(RuntimeContext context)
        {
            if (!showOptions) return;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.SetNextWindowSize(GetOptionsSize());
            if (ImGui.Begin("Options", ref showOptions, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Checkbox("Fullscreen", ref fullscreen))
                {
                    Apply(context);
                }
                if (ImGui.Checkbox("VSync", ref vsync))
                {
                    Apply(context);
                }
                ImGui.Checkbox("Invert Mouse Y", ref invertMouseY);
                ImGui.Text("UI Opacity");
                ImGui.SetNextItemWidth(OPTIONSHEIGHT - 15);
                if (ImGui.SliderFloat("", ref uiAlpha, 0.2f, 1))
                {
                    Apply(context);
                }
            }
            ImGui.PopStyleVar();
        }
    }
}