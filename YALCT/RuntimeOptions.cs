using System;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly string optionsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "options.ini");

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
            LoadOptions();
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
                    SetFullscreen(context, true);
                }
                if (ImGui.Checkbox("VSync", ref vsync))
                {
                    SetVSync(context, true);
                }
                ImGui.Checkbox("Invert Mouse Y", ref invertMouseY);
                ImGui.Text("UI Scale");
                float itemWidth = size.X - 16;
                ImGui.SetNextItemWidth(itemWidth);
                if (ImGui.InputFloat("uiscale", ref uiScale, 0.1f, 1f, "%.3f", ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (uiScale < 1f) uiScale = 1f;
                    if (uiScale > 4f) uiScale = 4f;
                    SetUIScale(context, true);
                    return;
                }
                ImGui.Text("UI Opacity");
                ImGui.SetNextItemWidth(itemWidth);
                if (ImGui.SliderFloat("uiopacity", ref uiAlpha, 0.2f, 1f))
                {
                    SetOpacity(true);
                }
            }
            ImGui.PopStyleVar();
        }

        private void SetOpacity(bool save = false)
        {
            ImGui.GetStyle().Alpha = uiAlpha;
            if (save)
                SaveOptions();
        }

        private void SetUIScale(RuntimeContext context, bool save = false)
        {
            BuildFonts(context);
            if (save)
                SaveOptions();
        }

        private void SetVSync(RuntimeContext context, bool save = false)
        {
            context.GraphicsDevice.SyncToVerticalBlank = vsync;
            if (save)
                SaveOptions();
        }

        private void SetFullscreen(RuntimeContext context, bool save = false)
        {
            context.Window.WindowState = fullscreen ? WindowState.BorderlessFullScreen : WindowState.Maximized;
            if (save)
                SaveOptions();
        }

        private string GenerateOptionsFileContent()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"fullscreen={fullscreen}");
            builder.AppendLine($"vsync={vsync}");
            builder.AppendLine($"invertMouseY={invertMouseY}");
            builder.AppendLine($"uiScale={uiScale}");
            builder.AppendLine($"uiAlpha={uiAlpha}");
            return builder.ToString();
        }

        private void LoadOptions()
        {
            if (!File.Exists(optionsFilePath))
            {
                // Save defaults
                SaveOptions();
            }

            string[] options = File.ReadAllLines(optionsFilePath);

            foreach (string option in options)
            {
                string[] components = option.Split('=');
                if (components.Length != 2) continue;
                switch (components[0])
                {
                    case "fullscreen":
                        fullscreen = bool.Parse(components[1]);
                        break;
                    case "vsync":
                        vsync = bool.Parse(components[1]);
                        break;
                    case "invertMouseY":
                        invertMouseY = bool.Parse(components[1]);
                        break;
                    case "uiScale":
                        uiScale = float.Parse(components[1]);
                        break;
                    case "uiAlpha":
                        uiAlpha = float.Parse(components[1]);
                        break;
                }
            }
        }

        private void SaveOptions()
        {
            File.WriteAllText(optionsFilePath, GenerateOptionsFileContent());
        }
    }
}