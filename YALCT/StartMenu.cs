using System.Numerics;
using ImGuiNET;
using Veldrid;

namespace YALCT
{
    public class StartMenu : IImGuiComponent
    {
        private const float MENUWIDTH = 200;
        private const float MENUHEIGHT = 400;

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
            ImGui.SetNextWindowSize(new Vector2(MENUWIDTH, MENUHEIGHT));
            ImGui.SetNextWindowPos(new Vector2(Controller.Context.Width / 2 - MENUWIDTH / 2,
                                               Controller.Context.Height / 2 - MENUHEIGHT / 2));
            if (ImGui.Begin("Yet Another Live Coding Tool", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Button("Create", new Vector2(MENUWIDTH - 15, 40)))
                {
                    Controller.SetState(UIState.Editor);
                }
                if (ImGui.Button("Load", new Vector2(MENUWIDTH - 15, 40)))
                {
                    Controller.LoadFile();
                }
                if (ImGui.Button("Options", new Vector2(MENUWIDTH - 15, 40)))
                {
                    Controller.ShowOptions = true;
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