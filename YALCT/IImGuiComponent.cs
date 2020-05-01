using Veldrid;

namespace YALCT
{
    public interface IImGuiComponent
    {
        ImGuiController Controller { get; }
        void Initialize();
        void SubmitUI(float deltaTime, InputSnapshot inputSnapshot);
        void Update(float deltaTime);
        void SetError(string errorMessage); // meh
    }
}