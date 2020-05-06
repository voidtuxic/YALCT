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

        private readonly RuntimeContext context;

        private UIState previousState;
        private UIState state;
        private readonly Dictionary<UIState, IImGuiComponent> components = new Dictionary<UIState, IImGuiComponent>();

        public RuntimeContext Context => context;
        public UIState State => state;

        public ImGuiController(RuntimeContext context)
        {
            this.context = context;

            // options setup
            RuntimeOptions.Current.Initialize(context);

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
            RuntimeOptions.Current.SubmitUI(context);
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
            GetComponent<FilePicker>().ResourceMode = false;
            GetComponent<FilePicker>().ArchiveMode = false;
            SetState(UIState.FilePicker);
        }

        public void SaveFile()
        {
            GetComponent<FilePicker>().SaveMode = true;
            GetComponent<FilePicker>().ShadertoyMode = false;
            GetComponent<FilePicker>().ResourceMode = false;
            GetComponent<FilePicker>().ArchiveMode = false;
            SetState(UIState.FilePicker);
        }

        public void PackFile()
        {
            GetComponent<FilePicker>().SaveMode = true;
            GetComponent<FilePicker>().ShadertoyMode = false;
            GetComponent<FilePicker>().ResourceMode = false;
            GetComponent<FilePicker>().ArchiveMode = true;
            SetState(UIState.FilePicker);
        }

        public void LoadResource()
        {
            GetComponent<FilePicker>().SaveMode = false;
            GetComponent<FilePicker>().ShadertoyMode = false;
            GetComponent<FilePicker>().ResourceMode = true;
            GetComponent<FilePicker>().ArchiveMode = false;
            SetState(UIState.FilePicker);
        }
    }
}