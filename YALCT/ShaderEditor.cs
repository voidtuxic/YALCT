using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using Newtonsoft.Json;
using Veldrid;

namespace YALCT
{

    public class ShaderEditor : IImGuiComponent
    {
        public const int MAXEDITORSTRINGLENGTH = 1000000; // man this is shitty tho
        private const float AUTOAPPLYINTERVAL = 1f;
        private const float FPSUPDATEINTERVAL = 0.25f;
        private const float HIDEUIHELPTEXTDURATION = 5f;
        private const float RESOURCESWINDOWWIDTH = 300f;
        private const float RESOURCESWINDOWHEIGHT = 600f;
        private const float MAXTEXPREVIEWSIZE = 512f;
        private const float METADATAWINDOWWIDTH = 300f;
        private const float METADATAWINDOWHEIGHT = 300f;

        private bool showUI = true;
        private float hideUIHelpTextDelta = 0;
        private bool autoApply = true;
        private float autoApplyCurrentInterval = 0;

        private bool basicMode = false;
        private int editorSelectedLineIndex = -1;
        private string editorSelectedLineContent = null;
        private int editorSelectedLineCursorPosition = -1;

        private string fps = "";
        private float fpsUpdateCurrentInterval = 0;

        private string previousError = null;
        private readonly List<string> errorMessages = new List<string>();

        private YALCTShaderMetadata shaderMetadata = YALCTShaderMetadata.Default();
        private string fragmentCode = @"// Available inputs
// mouse (vec4) : x,y => position, z => mouse 1 down, z => mouse 2 down
// resolution (vec2) : x,y => pixel size of the render window
// time (float) : total time in seconds since start
// deltaTime (float) : time in seconds since last frame
// frame (int) : current frame
// use sample2D(InputTexN, uv) helper to sample an input texture

void main()
{
    float x = gl_FragCoord.x / resolution.x;
    float y = gl_FragCoord.y / resolution.y;
    out_Color = vec4(0,x,y,1);
}";
        private readonly List<string> fragmentCodeLines = new List<string>();

        private bool showMetadata;
        private bool showResources = true;

        public ImGuiController Controller { get; private set; }
        public string FragmentCode => fragmentCode;
        public YALCTShaderMetadata ShaderMetadata => shaderMetadata;

        public ShaderEditor(ImGuiController controller)
        {
            Controller = controller;
        }

        public void Initialize()
        {
            SplitLines();
            Apply();
        }

        public void SubmitUI(float deltaTime, InputSnapshot inputSnapshot)
        {
            if (showUI)
            {
                SubmitMainMenu(deltaTime);
                SubmitEditorWindow();
                SubmitResourcesWindow();
                SubmitMetadataWindow();
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
                        ImGui.GetStyle().Alpha = RuntimeOptions.Current.UiAlpha;
                        showUI = true;
                        break;
                    }
                }
            }
        }

        private void SubmitMetadataWindow()
        {
            if (!showMetadata) return;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            Vector2 size = RuntimeOptions.Current.GetScaledSize(METADATAWINDOWWIDTH, METADATAWINDOWHEIGHT);
            float quarterWidth = size.X / 4;
            ImGui.SetNextWindowSize(size);
            if (ImGui.Begin("Metadata", ref showMetadata, ImGuiWindowFlags.NoResize))
            {
                ImGui.InputText("Name", ref shaderMetadata.Name, 1000);
                ImGui.InputText("Credit", ref shaderMetadata.Credit, 1000);
                ImGui.InputText("Version", ref shaderMetadata.Version, 1000);
                ImGui.Text("Description");
                ImGui.InputTextMultiline(
                    "Description_input",
                    ref shaderMetadata.Description,
                    1000, new Vector2(size.X - 16, 160 * RuntimeOptions.Current.UiScale));
                ImGui.End();
            }
        }

        private void SubmitResourcesWindow()
        {
            if (!showResources) return;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            Vector2 size = RuntimeOptions.Current.GetScaledSize(RESOURCESWINDOWWIDTH, RESOURCESWINDOWHEIGHT);
            float quarterWidth = size.X / 4;
            ImGui.SetNextWindowSize(size);
            if (ImGui.Begin("Resources", ref showResources, ImGuiWindowFlags.NoResize))
            {
                if (ImGui.Button("Add resource", new Vector2(size.X - 16, 30 * RuntimeOptions.Current.UiScale)))
                {
                    Controller.LoadResource();
                }
                ImGui.PushFont(RuntimeOptions.Current.EditorFont);
                if (ImGui.BeginChild("Resource list", Vector2.Zero, false))
                {
                    for (int i = 0; i < Controller.Context.ImguiTextures.Count; i++)
                    {
                        YALCTShaderResource resource = Controller.Context.ImguiTextures[i];
                        if (ImGui.BeginChild($"resprops_{resource.UID}", new Vector2(0, quarterWidth), false))
                        {
                            ImGui.Image(resource.ImguiBinding, new Vector2(quarterWidth, quarterWidth));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.GetStyle().Alpha = 1;
                                ImGui.BeginTooltip();
                                float scaler = resource.Size.X > resource.Size.Y ?
                                    resource.Size.X / MAXTEXPREVIEWSIZE : resource.Size.Y / MAXTEXPREVIEWSIZE;
                                ImGui.Image(resource.ImguiBinding, resource.Size / scaler);
                                ImGui.EndTooltip();
                                ImGui.GetStyle().Alpha = RuntimeOptions.Current.UiAlpha;
                            }
                            ImGui.SameLine(quarterWidth + 5);
                            if (ImGui.BeginChild($"resdata_{resource.UID}", new Vector2(0, quarterWidth), false))
                            {
                                ImGui.Text($"InputTex{i}");
                                ImGui.Text($"{resource.Size.X}x{resource.Size.Y}");
                                if (ImGui.Button("Remove"))
                                {
                                    Controller.Context.RemoveTexture(resource);
                                    return;
                                }
                                ImGui.EndChild();
                            }
                            ImGui.EndChild();
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.PopFont();
            }
            ImGui.PopStyleVar();
        }

        private void SubmitMainMenu(float deltaTime)
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load"))
                    {
                        Controller.LoadFile();
                    }
                    if (ImGui.MenuItem("Import Shadertoy"))
                    {
                        Controller.LoadFile(true);
                    }
                    if (ImGui.MenuItem("Save"))
                    {
                        Controller.GetComponent<FilePicker>().SaveShader(this);
                    }
                    if (ImGui.MenuItem("Save as..."))
                    {
                        Controller.SaveFile();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Start menu"))
                    {
                        Controller.SetState(UIState.StartMenu);
                    }
                    if (ImGui.MenuItem("Options"))
                    {
                        RuntimeOptions.Current.ShowOptions = true;
                    }
                    if (ImGui.MenuItem("Quit"))
                    {
                        Controller.Context.Quit();
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("Hide UI"))
                {
                    showUI = false;
                    hideUIHelpTextDelta = 0;
                }

                if (ImGui.MenuItem("Apply"))
                {
                    Apply();
                }

                if (ImGui.MenuItem("Resources"))
                {
                    showResources = true;
                }

                if (ImGui.MenuItem("Metadata"))
                {
                    showMetadata = true;
                }

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20);
                if (ImGui.Checkbox("Auto Apply", ref autoApply))
                {
                    autoApplyCurrentInterval = 0;
                }
                fpsUpdateCurrentInterval += deltaTime;
                if (fpsUpdateCurrentInterval >= FPSUPDATEINTERVAL)
                {
                    fpsUpdateCurrentInterval = 0;
                    fps = $"{(int)MathF.Round(1f / deltaTime)}";
                }
                Vector2 fpsSize = ImGui.CalcTextSize(fps);
                ImGui.SameLine(ImGui.GetWindowWidth() - fpsSize.X - 20);
                ImGui.Text(fps);
                ImGui.EndMainMenuBar();
            }
        }

        private unsafe void SubmitEditorWindow()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.SetNextWindowSizeConstraints(Vector2.One * 500 * RuntimeOptions.Current.UiScale, Vector2.One * Controller.Context.Width);
            if (ImGui.Begin("Shader Editor"))
            {
                ImGui.PushFont(RuntimeOptions.Current.EditorFont);
                if (ImGui.BeginTabBar("editor mode"))
                {
                    if (ImGui.BeginTabItem("Advanced"))
                    {
                        basicMode = false;
                        SubmitAdvancedEditor();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Basic"))
                    {
                        basicMode = true;
                        if (ImGui.BeginChild("editor basic", Vector2.Zero, true))
                        {
                            Vector2 editorWindowSize = ImGui.GetWindowSize();
                            float textSize = ImGui.CalcTextSize(fragmentCode).Y + 32;
                            ImGui.PushItemWidth(-1);
                            ImGui.InputTextMultiline("",
                                                     ref fragmentCode,
                                                     MAXEDITORSTRINGLENGTH,
                                                     new Vector2(editorWindowSize.X - 16, textSize > editorWindowSize.Y ? textSize : editorWindowSize.Y - 16),
                                                     ImGuiInputTextFlags.AllowTabInput);
                            ImGui.PopItemWidth();
                        }
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
                ImGui.PopFont();
                ImGui.End();
            }
            if (errorMessages.Count != 0)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(500, 16), new Vector2(500, 500));
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos();
                for (int i = 0; i < errorMessages.Count; i++)
                {
                    string errorMessage = errorMessages[i];
                    if (string.IsNullOrWhiteSpace(errorMessage)) continue;
                    ImGui.TextColored(RgbaFloat.Red.ToVector4(), errorMessage);
                }
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
            ImGui.PopStyleVar();
        }

        private unsafe void SubmitAdvancedEditor()
        {
            if (ImGui.BeginChild("editor", Vector2.Zero, true))
            {
                // handle basic input
                if (editorSelectedLineIndex != -1)
                {
                    if ((editorSelectedLineCursorPosition == 0 && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.LeftArrow), false))
                        || ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.UpArrow), true))
                    {
                        SetSelectedLine(editorSelectedLineIndex - 1);
                    }
                    if ((editorSelectedLineCursorPosition == editorSelectedLineContent.Length && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.RightArrow), false))
                        || ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.DownArrow), true))
                    {
                        SetSelectedLine(editorSelectedLineIndex + 1);
                    }
                    if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Enter), true))
                    {
                        string newLineContent = "";
                        if (editorSelectedLineCursorPosition != editorSelectedLineContent.Length)
                        {
                            fragmentCodeLines[editorSelectedLineIndex] = editorSelectedLineContent.Take(editorSelectedLineCursorPosition).ToSystemString();
                            newLineContent = editorSelectedLineContent.Skip(editorSelectedLineCursorPosition).ToSystemString();
                        }
                        fragmentCodeLines.Insert(editorSelectedLineIndex + 1, newLineContent);
                        SetSelectedLine(editorSelectedLineIndex + 1);
                    }
                    if (editorSelectedLineIndex > 0)
                    {
                        if (editorSelectedLineCursorPosition == 0 && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Backspace), true))
                        {
                            if (!string.IsNullOrEmpty(editorSelectedLineContent))
                            {
                                fragmentCodeLines[editorSelectedLineIndex - 1] += editorSelectedLineContent;
                            }
                            fragmentCodeLines.RemoveAt(editorSelectedLineIndex);
                            SetSelectedLine(editorSelectedLineIndex - 1);
                        }
                    }
                }

                // draw lines
                for (int i = 0; i < fragmentCodeLines.Count; i++)
                {
                    string line = fragmentCodeLines[i];
                    string lineNumber = $"{i + Controller.Context.FragmentHeaderLineCount}";
                    bool isError = errorMessages.Any(msg => msg.StartsWith($"{lineNumber}:"));
                    bool isEdited = i == editorSelectedLineIndex;
                    ImGui.TextColored(
                        isEdited ? RgbaFloat.Green.ToVector4() : isError ? RgbaFloat.Red.ToVector4() : RgbaFloat.LightGrey.ToVector4(),
                        lineNumber);
                    ImGui.SameLine(50);
                    if (isError)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, RgbaFloat.Red.ToVector4());
                    }
                    if (isEdited)
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                        ImGui.PushItemWidth(-1);
                        // taken from https://github.com/mellinoe/ImGui.NET/blob/0b9c9ea07d720ac0c4e382deb8f08de30703a9a3/src/ImGui.NET.SampleProgram/MemoryEditor.cs#L128
                        // which is not ideal
                        ImGuiInputTextCallback callback = (data) =>
                        {
                            int* p_cursor_pos = (int*)data->UserData;

                            if (ImGuiNative.ImGuiInputTextCallbackData_HasSelection(data) == 0)
                                *p_cursor_pos = data->CursorPos;
                            return 0;
                        };
                        int cursorPos = -1;
                        const ImGuiInputTextFlags flags = ImGuiInputTextFlags.AllowTabInput | ImGuiInputTextFlags.CallbackAlways;
                        if (ImGui.InputText(lineNumber,
                                            ref editorSelectedLineContent,
                                            1000,
                                            flags,
                                            callback,
                                            (IntPtr)(&cursorPos)))
                        {
                            fragmentCodeLines[editorSelectedLineIndex] = editorSelectedLineContent;
                        }
                        ImGui.PopItemWidth();
                        ImGui.PopStyleVar(1);
                        editorSelectedLineCursorPosition = cursorPos;
                    }
                    else if (ImGui.Selectable(line))
                    {
                        SetSelectedLine(i);
                    }
                    if (isError)
                    {
                        ImGui.PopStyleColor(1);
                    }
                }
                ImGui.EndChild();
            }
        }

        private void SetSelectedLine(int i)
        {
            if (i >= 0 && i < fragmentCodeLines.Count)
            {
                editorSelectedLineIndex = i;
                ImGui.SetKeyboardFocusHere(1);
                editorSelectedLineContent = fragmentCodeLines[i];
            }
        }

        private void SplitLines()
        {
            editorSelectedLineIndex = -1;
            editorSelectedLineContent = null;
            fragmentCodeLines.Clear();
            fragmentCodeLines.AddRange(Regex.Split(fragmentCode, "\r\n|\r|\n"));
        }

        private void MergeLines()
        {
            fragmentCode = string.Join("\n", fragmentCodeLines);
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
            if (error == previousError) return;
            errorMessages.Clear();
            if (error != null)
            {
                errorMessages.AddRange(Regex.Split(
                    error.Replace("Compilation failed: ", "")
                        .Replace("<veldrid-spirv-input>:", ""),
                    "\r\n|\r|\n"));
            }
            previousError = error;
        }

        public void Apply()
        {
            if (basicMode)
            {
                SplitLines();
            }
            else
            {
                MergeLines();
            }
            Controller.Context.CreateDynamicResources(fragmentCode);
        }

        public void LoadShader(string shaderContent)
        {
            string[] shaderParts = shaderContent.Split("*/");
            if (shaderParts.Length > 1)
            {
                string metadataJson = shaderParts[0].Skip(2).ToSystemString();
                shaderMetadata = JsonConvert.DeserializeObject<YALCTShaderMetadata>(metadataJson);
                // TODO sketchy, might not work on unix systems
                fragmentCode = shaderParts[1].Skip(4).ToSystemString();
            }
            else
            {
                shaderMetadata = YALCTShaderMetadata.Default();
                fragmentCode = shaderContent;
            }
            SplitLines();
            Apply();
        }
    }
}