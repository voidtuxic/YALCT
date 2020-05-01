using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using Veldrid;

namespace YALCT
{
    public class ImGuiController
    {
        private readonly RuntimeContext context;
        private readonly ImFontPtr mainFont;
        private readonly ImFontPtr editorFont;

        private UIState state;
        private readonly Dictionary<UIState, IImGuiComponent> components = new Dictionary<UIState, IImGuiComponent>();

        private float uiAlpha = 0.5f;

        public RuntimeContext Context => context;
        public ImFontPtr MainFont => mainFont;
        public ImFontPtr EditorFont => editorFont;
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
        }

        public void Initialize()
        {
            // show default shader
            components[UIState.Editor].Initialize();
            SetState(UIState.StartMenu);
        }

        public void SetState(UIState newState)
        {
            if (!components.ContainsKey(newState))
            {
                throw new NotSupportedException($"No state {newState} mapped to a UI component");
            }
            state = newState;
            GetCurrentComponent().Initialize();
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
        }

        public void Update(float deltaTime)
        {
            GetCurrentComponent().Update(deltaTime);
        }

        internal void SetError(string errorMessage)
        {
            GetCurrentComponent().SetError(errorMessage);
        }
    }
}