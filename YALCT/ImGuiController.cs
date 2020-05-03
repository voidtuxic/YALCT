using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Veldrid;

namespace YALCT
{
    public class ImGuiController
    {
        private const int OPTIONSWIDTH = 150;
        private const int OPTIONSHEIGHT = 170;

        private readonly RuntimeContext context;
        private readonly ImFontPtr mainFont;
        private readonly ImFontPtr editorFont;

        private UIState previousState;
        private UIState state;
        private readonly Dictionary<UIState, IImGuiComponent> components = new Dictionary<UIState, IImGuiComponent>();

        private bool showOptions = false;
#if DEBUG
        private bool fullscreen = false;
#else
        private bool fullscreen = true;
#endif
        private bool vsync = true;
        private bool invertMouseY = false;
        private float uiAlpha = 0.75f;

        public RuntimeContext Context => context;
        public ImFontPtr MainFont => mainFont;
        public ImFontPtr EditorFont => editorFont;
        public bool ShowOptions { get => showOptions; set => showOptions = value; }
        public bool InvertMouseY => invertMouseY;
        public float UiAlpha { get => uiAlpha; set => uiAlpha = value; }
        public UIState State => state;

        public ImGuiController(RuntimeContext context)
        {
            this.context = context;

            // font setup
            ImGui.EndFrame();
            var io = ImGui.GetIO();
            io.Fonts.Clear();
            mainFont = io.Fonts.AddFontFromFileTTF(Path.Combine(Directory.GetCurrentDirectory(), "fonts/OpenSans-Regular.ttf"), 16.0f);
            editorFont = io.Fonts.AddFontFromFileTTF(Path.Combine(Directory.GetCurrentDirectory(), "fonts/FiraCode-Regular.ttf"), 16.0f);
            context.GraphicsDevice.WaitForIdle();
            context.ImGuiRenderer.RecreateFontDeviceTexture();
            ImGui.NewFrame();

            // style setup
            ImGui.StyleColorsDark();
            ImGui.GetStyle().Alpha = uiAlpha;

            // component setup
            components.Add(UIState.StartMenu, new StartMenu(this));
            components.Add(UIState.Editor, new ShaderEditor(this));
            components.Add(UIState.FilePicker, new FilePicker(this));
        }

        public void Initialize()
        {
            // show default shader
            components[UIState.Editor].Initialize();
            SetState(UIState.StartMenu);
        }

        public void GoBack()
        {
            SetState(previousState);
        }

        public void SetState(UIState newState)
        {
            if (!components.ContainsKey(newState))
            {
                throw new NotSupportedException($"No state {newState} mapped to a UI component");
            }
            previousState = state;
            state = newState;
            GetCurrentComponent().Initialize();
        }

        public T GetComponent<T>()
            where T : IImGuiComponent
        {
            IImGuiComponent selectedComponent = null;
            foreach (IImGuiComponent component in components.Values)
            {
                if (component.GetType().Equals(typeof(T)))
                {
                    selectedComponent = component;
                    break;
                }
            }
            if (selectedComponent == null)
            {
                throw new NullReferenceException($"No UI component of type {typeof(T).FullName}");
            }
            return (T)selectedComponent;
        }

        public IImGuiComponent GetCurrentComponent()
        {
            if (!components.ContainsKey(state))
            {
                throw new NotSupportedException($"No state {state} mapped to a UI component");
            }
            return components[state];
        }

        public void SubmitUI(float deltaTime, InputSnapshot inputSnapshot)
        {
            GetCurrentComponent().SubmitUI(deltaTime, inputSnapshot);

            if (ShowOptions)
                SubmitOptions(inputSnapshot);
        }

        private void SubmitOptions(InputSnapshot inputSnapshot)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.SetNextWindowSize(new Vector2(OPTIONSWIDTH, OPTIONSHEIGHT));
            if (ImGui.Begin("Options", ref showOptions, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Checkbox("Fullscreen", ref fullscreen))
                {
                    Context.Window.WindowState = fullscreen ? WindowState.BorderlessFullScreen : WindowState.Maximized;
                }
                if (ImGui.Checkbox("VSync", ref vsync))
                {
                    Context.GraphicsDevice.SyncToVerticalBlank = vsync;
                }
                ImGui.Checkbox("Invert Mouse Y", ref invertMouseY);
                ImGui.Text("UI Opacity");
                ImGui.SetNextItemWidth(OPTIONSHEIGHT - 15);
                if (ImGui.SliderFloat("", ref uiAlpha, 0.2f, 1))
                {
                    ImGui.GetStyle().Alpha = uiAlpha;
                }
            }
            ImGui.PopStyleVar();
        }

        public void Update(float deltaTime)
        {
            GetCurrentComponent().Update(deltaTime);
        }

        public void SetError(string errorMessage)
        {
            GetCurrentComponent().SetError(errorMessage);
        }

        public void LoadFile(bool shadertoy = false)
        {
            GetComponent<FilePicker>().SaveMode = false;
            GetComponent<FilePicker>().ShadertoyMode = shadertoy;
            SetState(UIState.FilePicker);
        }

        public void SaveFile()
        {
            GetComponent<FilePicker>().SaveMode = true;
            GetComponent<FilePicker>().ShadertoyMode = false;
            SetState(UIState.FilePicker);
        }
    }
}