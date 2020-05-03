using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Veldrid;

namespace YALCT
{
    public class FilePicker : IImGuiComponent
    {
        private const float MENUWIDTH = 700;
        private const float MENUHEIGHT = 500;
        private const int MAXPATHLENGTH = 1000; // still some shitty stuff right here
        private const float ERRORMESSAGEDURATION = 5f;

        internal struct FilePickerItem
        {
            public bool IsUpper;
            public bool IsFolder;
            public string Name;

            public FilePickerItem(bool isFolder, string name, bool isUpper = false)
            {
                IsUpper = isUpper;
                IsFolder = isFolder;
                Name = name;
            }
        }

        private bool open = true;
        private bool showAll = false;
        private bool saveMode = false;
        private bool shadertoyMode = false;
        private string path;
        private string filename = "shader.glsl";
        private readonly FilePickerItem upperItem = new FilePickerItem(true, "..", true);
        private readonly List<FilePickerItem> files = new List<FilePickerItem>();

        private string errorMessage;
        private float errorMessageTime;

        public ImGuiController Controller { get; private set; }
        public bool SaveMode { get => saveMode; set => saveMode = value; }
        public bool ShadertoyMode { get => shadertoyMode; set => shadertoyMode = value; }

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
                files.Add(new FilePickerItem(true, $"{RemoveItemPathMarkup(folder)}/"));
            }

            if (!saveMode)
            {
                string[] currentFiles = Directory.GetFiles(path);
                foreach (string file in currentFiles)
                {
                    if (!showAll && FilePickerHelper.IsIgnoredExtension(file)) continue;
                    files.Add(new FilePickerItem(false, RemoveItemPathMarkup(file)));
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
                if (!saveMode)
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
                        FilePickerItem item = files[i];
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
                ImGui.GetStyle().Alpha = Controller.UiAlpha;
            }
        }

        private void HandleSelection(FilePickerItem item)
        {
            if (item.IsUpper)
            {
                SetPath(Path.Combine(path, @"..\"));
                return;
            }
            string itemPath = Path.Combine(path, item.Name);
            if (item.IsFolder)
            {
                SetPath(itemPath);
                return;
            }

            if (FilePickerHelper.IsBinary(itemPath))
            {
                SetError("That's a binary file, not a shader, can't read it");
                return;
            }
            string content = File.ReadAllText(itemPath, Encoding.UTF8);
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
            SaveShader(editor.FragmentCode);
            Controller.GoBack();
        }

        public void SaveShader(string fragmentCode)
        {
            string filePath = Path.Combine(path, filename);
            File.WriteAllText(filePath, fragmentCode, Encoding.UTF8);
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