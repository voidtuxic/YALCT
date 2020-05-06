using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Newtonsoft.Json;
using Veldrid;

namespace YALCT
{
    public class FilePicker : IImGuiComponent
    {
        private const float MENUWIDTH = 700;
        private const float MENUHEIGHT = 500;
        private const int MAXPATHLENGTH = 1000; // still some shitty stuff right here
        private const float ERRORMESSAGEDURATION = 5f;

        private bool open = true;
        private bool showAll = false;
        private bool saveMode = false;
        private bool shadertoyMode = false;
        private bool resourceMode = false;
        private string path;
        private string filename = "shader.glsl";
        private readonly YALCTFilePickerItem upperItem = new YALCTFilePickerItem(true, "..", "that ain't it chief", true);
        private readonly List<YALCTFilePickerItem> files = new List<YALCTFilePickerItem>();

        private string errorMessage;
        private float errorMessageTime;

        public ImGuiController Controller { get; private set; }
        public bool SaveMode { get => saveMode; set => saveMode = value; }
        public bool ShadertoyMode { get => shadertoyMode; set => shadertoyMode = value; }
        public bool ResourceMode { get => resourceMode; set => resourceMode = value; }

        public FilePicker(ImGuiController controller)
        {
            Controller = controller;
        }

        public void Initialize()
        {
            open = true;
            path = Path.Combine(Directory.GetCurrentDirectory(), "shaders");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            LoadFileList();
        }

        private string RemoveItemPathMarkup(string item)
        {
            return item.Replace(path, "").Replace(@"\", "").Replace(@"/", "");
        }

        private void SetPath(string newPath)
        {
            path = Path.GetFullPath(newPath);
            LoadFileList();
        }

        private void LoadFileList()
        {
            files.Clear();
            files.Add(upperItem);

            string[] currentFolders = Directory.GetDirectories(path);
            foreach (string folder in currentFolders)
            {
                files.Add(new YALCTFilePickerItem(true, $"{RemoveItemPathMarkup(folder)}/", Path.Combine(path, folder)));
            }

            if (!saveMode)
            {
                string[] currentFiles = Directory.GetFiles(path);
                foreach (string file in currentFiles)
                {
                    if (!resourceMode && !showAll && FilePickerHelper.IsIgnoredExtension(file)) continue;
                    if (resourceMode && !FilePickerHelper.IsResourceExtension(file)) continue;
                    files.Add(new YALCTFilePickerItem(false, RemoveItemPathMarkup(file), Path.Combine(path, file)));
                }
            }
        }

        public void SetError(string error)
        {
            errorMessageTime = 0;
            errorMessage = error;
        }

        public void SubmitUI(float deltaTime, InputSnapshot inputSnapshot)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.SetNextWindowSize(new Vector2(MENUWIDTH, MENUHEIGHT));
            ImGui.SetNextWindowPos(new Vector2(Controller.Context.Width / 2 - MENUWIDTH / 2,
                                               Controller.Context.Height / 2 - MENUHEIGHT / 2));
            string windowName = saveMode ? "Save" : shadertoyMode ? "Import shadertoy" : "Load";
            if (ImGui.Begin($"{windowName} file", ref open, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                string tmpPath = path;
                ImGui.SetNextItemWidth(MENUWIDTH - 90);
                if (ImGui.InputText("Current path", ref tmpPath, MAXPATHLENGTH, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (Directory.Exists(tmpPath))
                    {
                        SetPath(tmpPath);
                    }
                }
                if (!saveMode && !resourceMode)
                {
                    if (ImGui.Checkbox("Show all", ref showAll))
                    {
                        LoadFileList();
                    }
                    if (shadertoyMode)
                    {
                        ImGui.SameLine(100);
                        ImGui.TextColored(RgbaFloat.Red.ToVector4(), "Warning: Shadertoy import isn't guaranteed to succeed !");
                    }
                }
                if (saveMode)
                {
                    ImGui.SetNextItemWidth(MENUWIDTH - 190);
                    if (ImGui.InputText("Saved file name", ref filename, MAXPATHLENGTH, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        string filePath = Path.Combine(path, filename);
                        if (File.Exists(filePath))
                        {
                            // not really an error but user should know
                            SetError("File already exists");
                        }
                    }
                    ImGui.SameLine(MENUWIDTH - 80);
                    if (ImGui.Button("Save shader"))
                    {
                        SaveShader();
                    }
                }
                if (ImGui.BeginChild("currentfiles", Vector2.Zero, true, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        YALCTFilePickerItem item = files[i];
                        if (ImGui.Selectable(item.Name))
                        {
                            HandleSelection(item);
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.End();
            }
            ImGui.PopStyleVar();
            if (errorMessage != null && errorMessageTime < ERRORMESSAGEDURATION)
            {
                errorMessageTime += deltaTime;
                ImGui.GetStyle().Alpha = 1;
                ImGui.BeginTooltip();
                ImGui.TextColored(RgbaFloat.Red.ToVector4(), errorMessage);
                ImGui.EndTooltip();
                ImGui.GetStyle().Alpha = RuntimeOptions.Current.UiAlpha;
            }
        }

        private void HandleSelection(YALCTFilePickerItem item)
        {
            if (item.IsUpper)
            {
                SetPath(Path.Combine(path, @"../"));
                return;
            }
            if (item.IsFolder)
            {
                SetPath(item.FullPath);
                return;
            }

            if (resourceMode)
            {
                LoadResource(item);
            }
            else
            {
                LoadShader(item);
            }
        }

        private bool LoadResource(YALCTFilePickerItem item, bool goBack = true)
        {
            // TODO probably add some file error handling

            if (Controller.Context.LoadTexture(item))
            {
                if (goBack) Controller.GoBack();
                return true;
            }

            return false;
        }

        private void LoadShader(YALCTFilePickerItem item)
        {
            if (FilePickerHelper.IsBinary(item.FullPath))
            {
                SetError("That's a binary file, not a shader, can't read it");
                return;
            }
            string content = File.ReadAllText(item.FullPath, Encoding.UTF8);
            if (content.Length > ShaderEditor.MAXEDITORSTRINGLENGTH)
            {
                SetError("Woah there, that's a pretty long file mate, can't read it");
                return;
            }
            SetError(null);
            filename = item.Name;
            LoadShader(content);
        }

        public void LoadShader(string shaderContent)
        {
            ShaderEditor editor = Controller.GetComponent<ShaderEditor>();
            if (shadertoyMode)
            {
                shaderContent = FilePickerHelper.ConvertShadertoy(shaderContent);
            }
            editor.LoadShader(shaderContent);
            Controller.SetState(UIState.Editor);
        }


        private void SaveShader()
        {
            ShaderEditor editor = Controller.GetComponent<ShaderEditor>();
            SaveShader(editor);
            Controller.GoBack();
        }

        public void SaveShader(ShaderEditor editor)
        {
            string filePath = Path.Combine(path, filename);
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("/*");
            headerBuilder.Append(JsonConvert.SerializeObject(editor.ShaderMetadata, Formatting.Indented));
            headerBuilder.Append("*/");
            // add a few lines of breathing space
            headerBuilder.AppendLine();
            headerBuilder.AppendLine();
            string resourceHeader = headerBuilder.ToString();
            File.WriteAllText(filePath, resourceHeader + editor.FragmentCode, Encoding.UTF8);
        }

        public void Update(float deltaTime)
        {
            if (!open)
            {
                Controller.GoBack();
            }
        }
    }
}