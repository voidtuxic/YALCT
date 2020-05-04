using System.Numerics;
using ImGuiNET;
using Veldrid;

namespace YALCT
{
    public class StartMenu : IImGuiComponent
    {
        private const float MENUWIDTH = 200;
        private const float MENUHEIGHT = 255;

        public ImGuiController Controller { get; private set; }

        public StartMenu(ImGuiController controller)
        {
            Controller = controller;
        }

        public void Initialize()
        {
        }

        public void SetError(string errorMessage)
        {
        }

        public void SubmitUI(float deltaTime, InputSnapshot inputSnapshot)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            Vector2 size = RuntimeOptions.Current.GetScaledSize(MENUWIDTH, MENUHEIGHT);
            ImGui.SetNextWindowSize(size);
            ImGui.SetNextWindowPos(new Vector2(Controller.Context.Width / 2 - size.X / 2,
                                               Controller.Context.Height / 2 - size.Y / 2));
            if (ImGui.Begin("Yet Another Live Coding Tool", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                Vector2 buttonSize = new Vector2(size.X - 16, 40 * RuntimeOptions.Current.UiScale);
                if (ImGui.Button("Create", buttonSize))
                {
                    Controller.SetState(UIState.Editor);
                }
                if (ImGui.Button("Load", buttonSize))
                {
                    Controller.LoadFile();
                }
                if (ImGui.Button("Import Shadertoy", buttonSize))
                {
                    Controller.LoadFile(true);
                }
                if (ImGui.Button("Options", buttonSize))
                {
                    RuntimeOptions.Current.ShowOptions = true;
                }
                if (ImGui.Button("Quit", buttonSize))
                {
                    Controller.Context.Quit();
                }
                ImGui.End();
            }
            ImGui.PopStyleVar();
        }

        public void Update(float deltaTime)
        {
        }
    }
}