using System.IO;
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
        private const int OPTIONSHEIGHT = 220;
        private const float FONTSIZE = 16.0f;
        private ImFontPtr mainFont;
        private ImFontPtr editorFont;

        private bool showOptions = false;
#if DEBUG
        private bool fullscreen = false;
#else
        private bool fullscreen = true;
#endif
        private bool vsync = true;
        private bool invertMouseY = false;
        private float uiAlpha = 0.75f;
        private float uiScale = 1f;

        public ImFontPtr MainFont => mainFont;
        public ImFontPtr EditorFont => editorFont;

        public bool ShowOptions { get => showOptions; set => showOptions = value; }
        public bool InvertMouseY => invertMouseY;
        public float UiAlpha => uiAlpha;
        public float UiScale => uiScale;

        private RuntimeOptions()
        {

        }

        public void Initialize(RuntimeContext context)
        {
            // TODO load and save options
            BuildFonts(context);
            Apply(context);
        }

        private void BuildFonts(RuntimeContext context)
        {
            ImGui.EndFrame();
            var io = ImGui.GetIO();
            io.Fonts.Clear();
            mainFont = io.Fonts.AddFontFromFileTTF(Path.Combine(Directory.GetCurrentDirectory(), "fonts/OpenSans-Regular.ttf"), FONTSIZE * uiScale);
            editorFont = io.Fonts.AddFontFromFileTTF(Path.Combine(Directory.GetCurrentDirectory(), "fonts/FiraCode-Regular.ttf"), FONTSIZE * uiScale);
            context.GraphicsDevice.WaitForIdle();
            context.ImGuiRenderer.RecreateFontDeviceTexture();
            ImGui.NewFrame();
        }

        private Vector2 GetOptionsSize()
        {
            return GetScaledSize(OPTIONSWIDTH, OPTIONSHEIGHT);
        }

        public Vector2 GetScaledSize(float width, float height)
        {
            return new Vector2(width, height) * uiScale;
        }

        public void Apply(RuntimeContext context)
        {
            // app context
            SetFullscreen(context);
            SetVSync(context);

            // imgui context
            SetUIScale(context);
            SetOpacity();
        }

        public void SubmitUI(RuntimeContext context)
        {
            if (!showOptions) return;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            Vector2 size = GetOptionsSize();
            ImGui.SetNextWindowSize(size);
            if (ImGui.Begin("Options", ref showOptions, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Checkbox("Fullscreen", ref fullscreen))
                {
                    SetFullscreen(context);
                }
                if (ImGui.Checkbox("VSync", ref vsync))
                {
                    SetVSync(context);
                }
                ImGui.Checkbox("Invert Mouse Y", ref invertMouseY);
                ImGui.Text("UI Scale");
                float itemWidth = size.X - 16;
                ImGui.SetNextItemWidth(itemWidth);
                if (ImGui.InputFloat("uiscale", ref uiScale, 0.1f, 1f, "%.3f", ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (uiScale < 1f) uiScale = 1f;
                    if (uiScale > 4f) uiScale = 4f;
                    SetUIScale(context);
                    return;
                }
                ImGui.Text("UI Opacity");
                ImGui.SetNextItemWidth(itemWidth);
                if (ImGui.SliderFloat("uiopacity", ref uiAlpha, 0.2f, 1f))
                {
                    SetOpacity();
                }
            }
            ImGui.PopStyleVar();
        }

        private void SetOpacity()
        {
            ImGui.GetStyle().Alpha = uiAlpha;
        }

        private void SetUIScale(RuntimeContext context)
        {
            BuildFonts(context);
            // ImGui.StyleColorsDark();
            // ImGui.GetIO().FontGlobalScale = uiScale;
            // ImGui.GetStyle().ScaleAllSizes(uiScale);
        }

        private void SetVSync(RuntimeContext context)
        {
            context.GraphicsDevice.SyncToVerticalBlank = vsync;
        }

        private void SetFullscreen(RuntimeContext context)
        {
            context.Window.WindowState = fullscreen ? WindowState.BorderlessFullScreen : WindowState.Maximized;
        }
    }
}