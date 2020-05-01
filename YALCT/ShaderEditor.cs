using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Veldrid;

namespace YALCT
{

    public class ShaderEditor : IImGuiComponent
    {
        private const int MAXEDITORSTRINGLENGTH = 1000000;
        private const float AUTOAPPLYINTERVAL = 1f;
        private const float HIDEUIHELPTEXTDURATION = 5f;

        private bool showUI = true;
        private float hideUIHelpTextDelta = 0;
        private bool autoApply = true;
        private float autoApplyCurrentInterval = 0;

        private string errorMessage;

        private string fragmentCode = @"// Available inputs
// mouse (vec4) : x,y => position, z => mouse 1 down, z => mouse 2 down
// resolution (vec2) : x,y => pixel size of the render window
// time (float) : total time in seconds since start
// deltaTime (float) : time in seconds since last frame
// frame (int) : current frame

void main()
{
    float x = gl_FragCoord.x / resolution.x;
    float y = gl_FragCoord.y / resolution.y;
    out_Color = vec4(0,x,y,1);
}";

        public ImGuiController Controller { get; private set; }

        public ShaderEditor(ImGuiController controller)
        {
            Controller = controller;
        }

        public void Initialize()
        {
            Apply();
        }

        public void SubmitUI(float deltaTime, InputSnapshot inputSnapshot)
        {
            ImGui.PushFont(Controller.MainFont);
            if (showUI)
            {
                SubmitMainMenu(deltaTime);
                SubmitEditorWindow();
            }
            else
            {
                if (hideUIHelpTextDelta < HIDEUIHELPTEXTDURATION)
                {
                    hideUIHelpTextDelta += deltaTime;
                    ImGui.GetStyle().Alpha = 1;
                    ImGui.BeginTooltip();
                    ImGui.Text("Press space to show UI");
                    ImGui.EndTooltip();
                }
                foreach (KeyEvent keyEvent in inputSnapshot.KeyEvents)
                {
                    if (keyEvent.Down && keyEvent.Key == Key.Space)
                    {
                        ImGui.GetStyle().Alpha = Controller.UiAlpha;
                        showUI = true;
                        break;
                    }
                }
            }
            ImGui.PopFont();
        }

        private void SubmitMainMenu(float deltaTime)
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.MenuItem("Hide UI"))
                {
                    showUI = false;
                    hideUIHelpTextDelta = 0;
                }

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20);
                if (ImGui.MenuItem("Apply"))
                {
                    Apply();
                }

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20);
                if (ImGui.Checkbox("Auto Apply", ref autoApply))
                {
                    autoApplyCurrentInterval = 0;
                }

                // ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20);
                // ImGui.SetNextItemWidth(100);
                // if (ImGui.SliderFloat("UI Alpha", ref Controller.UiAlpha, 0.2f, 1))
                // {
                //     ImGui.GetStyle().Alpha = Controller.UiAlpha;
                // }

                string fps = $"{(int)MathF.Round(1f / deltaTime)}";
                Vector2 fpsSize = ImGui.CalcTextSize(fps);
                ImGui.SameLine(ImGui.GetWindowWidth() - fpsSize.X - 20);
                ImGui.Text(fps);
                ImGui.EndMainMenuBar();
            }
        }

        private void SubmitEditorWindow()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            if (ImGui.Begin("Shader Editor"))
            {
                ImGui.PushFont(Controller.EditorFont);
                Vector2 editorWindowSize = ImGui.GetWindowSize();
                float bottomMargin = 40;
                if (errorMessage != null)
                {
                    ImGui.PushTextWrapPos();
                    ImGui.TextColored(RgbaFloat.Red.ToVector4(), errorMessage);
                    ImGui.PopTextWrapPos();
                    Vector2 errorSize = ImGui.GetItemRectSize();
                    bottomMargin = errorSize.Y * 2f + 15f; // sshh no tears
                }
                ImGui.InputTextMultiline("",
                                         ref fragmentCode,
                                         MAXEDITORSTRINGLENGTH,
                                         new Vector2(editorWindowSize.X - 15, editorWindowSize.Y - bottomMargin),
                                         ImGuiInputTextFlags.AllowTabInput);
                ImGui.PopFont();
                ImGui.End();
            }
            ImGui.PopStyleVar();
        }

        public void Update(float deltaTime)
        {
            if (autoApply)
            {
                autoApplyCurrentInterval += deltaTime;
                if (autoApplyCurrentInterval >= AUTOAPPLYINTERVAL)
                {
                    Apply();
                    autoApplyCurrentInterval = 0;
                }
            }
        }

        public void SetError(string error)
        {
            errorMessage = error;
        }

        public void Apply()
        {
            Controller.Context.CreateDynamicResources(fragmentCode);
        }
    }
}