using System;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace YALCT
{
    public class RuntimeContext : IDisposable
    {
        Sdl2Window window;
        bool windowResized = false;

        private GraphicsBackend backend;
        private bool isInitialized;

        private GraphicsDevice graphicsDevice;
        private ResourceFactory factory;
        private Swapchain swapchain;

        private CommandList commandList;
        private Pipeline pipeline;

        private DeviceBuffer vertexBuffer;
        private DeviceBuffer indexBuffer;
        private DeviceBuffer runtimeDataBuffer;
        private ResourceLayout resourceLayout;
        private ResourceSet resourceSet;

        private YALCTRuntimeData runtimeData;

        private ShaderDescription vertexShaderDesc;
        private Shader[] shaders;
        private string currentFragmentShader;

        private ImGuiRenderer imGuiRenderer;
        private ImGuiController uiController;

        public ImGuiRenderer ImGuiRenderer => imGuiRenderer;
        public GraphicsDevice GraphicsDevice => graphicsDevice;
        public int Width => window.Width;
        public int Height => window.Height;

        public RuntimeContext(GraphicsBackend backend)
        {
            this.backend = backend;
            Initialize();
        }

        public void Initialize()
        {
            isInitialized = false;
            // SDL init
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                WindowInitialState = WindowState.Maximized,
                WindowTitle = $"Yet Another Live Coding Tool ({backend})",
                WindowWidth = 200,
                WindowHeight = 200
            };
            window = VeldridStartup.CreateWindow(ref windowCI);
            window.Resized += () =>
            {
                windowResized = true;
            };

            // Veldrid init
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: PixelFormat.R32_Float,
                syncToVerticalBlank: false,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);
#if DEBUG
            options.Debug = true;
#endif
            graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options, backend);
            factory = graphicsDevice.ResourceFactory;
            swapchain = graphicsDevice.MainSwapchain;
            swapchain.Name = "YALTC Main Swapchain";
            CreateResources();
        }

        public void Quit()
        {
            window.Close();
        }

        public void CreateResources()
        {
            CreateRenderQuad();
            commandList = factory.CreateCommandList();
            CreateImGui();
        }

        private void CreateRenderQuad()
        {
            VertexPosition[] quadVertices =
               {
                new VertexPosition(new Vector3 (-1, 1, 0)),
                new VertexPosition(new Vector3 (1, 1, 0)),
                new VertexPosition(new Vector3 (-1, -1, 0)),
                new VertexPosition(new Vector3 (1, -1, 0))
            };
            uint[] quadIndices = new uint[]
            {
                0,
                1,
                2,
                1,
                3,
                2
            };
            vertexBuffer = factory.CreateBuffer(new BufferDescription(4 * VertexPosition.SizeInBytes, BufferUsage.VertexBuffer));
            vertexBuffer.Name = "YALCT Vertex Buffer";
            indexBuffer = factory.CreateBuffer(new BufferDescription(6 * sizeof(uint), BufferUsage.IndexBuffer));
            indexBuffer.Name = "YALCT Index Buffer";
            graphicsDevice.UpdateBuffer(vertexBuffer, 0, quadVertices);
            graphicsDevice.UpdateBuffer(indexBuffer, 0, quadIndices);
        }

        public void CreateDynamicResources(string fragmentCode)
        {
            // shaders
            string newFragmentShader = fragmentHeaderCode + fragmentCode;
            if (currentFragmentShader != null && currentFragmentShader.Equals(newFragmentShader))
            {
                uiController.SetError(null);
                return;
            }
            if (!isInitialized)
            {
                vertexShaderDesc = CreateShaderDescription(VertexCode, ShaderStages.Vertex);
            }
            Shader[] newShaders;
            try
            {
                ShaderDescription fragmentShaderDesc = CreateShaderDescription(newFragmentShader, ShaderStages.Fragment);
                newShaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
                DisposeShaders();
                shaders = newShaders;
                currentFragmentShader = newFragmentShader;
                uiController.SetError(null);
            }
            catch (Exception e)
            {
                uiController.SetError(e.Message);
                return;
            }

            // pipeline
            ResourceLayoutElementDescription[] layoutDescriptions = new ResourceLayoutElementDescription[]
            {
                new ResourceLayoutElementDescription ("RuntimeData", ResourceKind.UniformBuffer, ShaderStages.Fragment),
            };
            resourceLayout?.Dispose();
            resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(layoutDescriptions));
            resourceLayout.Name = "YALCT Resource Layout";
            pipeline?.Dispose();
            pipeline = factory.CreateGraphicsPipeline(
                   new GraphicsPipelineDescription(
                       BlendStateDescription.SingleOverrideBlend,
                       new DepthStencilStateDescription(
                           depthTestEnabled: false,
                           depthWriteEnabled: false,
                           comparisonKind: ComparisonKind.Always),
                       new RasterizerStateDescription(
                           cullMode: FaceCullMode.Back,
                           fillMode: PolygonFillMode.Solid,
                           frontFace: FrontFace.Clockwise,
                           depthClipEnabled: false,
                           scissorTestEnabled: false),
                       PrimitiveTopology.TriangleList,
                       new ShaderSetDescription(
                           vertexLayouts: new VertexLayoutDescription[] {
                            new VertexLayoutDescription(
                                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3)
                            )
                           },
                           shaders: shaders),
                       new ResourceLayout[] { resourceLayout },
                       swapchain.Framebuffer.OutputDescription)
               );
            pipeline.Name = "YALCT Fullscreen Pipeline";

            runtimeDataBuffer?.Dispose();
            runtimeDataBuffer = factory.CreateBuffer(new BufferDescription(
                YALCTRuntimeData.Size,
                BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            runtimeDataBuffer.Name = "YALCT Runtime Data";
            BindableResource[] bindableResources = { runtimeDataBuffer };
            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(resourceLayout, bindableResources);
            resourceSet?.Dispose();
            resourceSet = factory.CreateResourceSet(resourceSetDescription);
            resourceSet.Name = "YALCT Resource Set";
            isInitialized = true;
        }

        private void CreateImGui()
        {
            imGuiRenderer = new ImGuiRenderer(graphicsDevice,
                                              swapchain.Framebuffer.OutputDescription,
                                              window.Width,
                                              window.Height,
                                              ColorSpaceHandling.Linear);
            uiController = new ImGuiController(this);
            uiController.Initialize();
        }

        private ShaderDescription CreateShaderDescription(string code, ShaderStages stage)
        {
            byte[] data = Encoding.UTF8.GetBytes(code);
            return new ShaderDescription(stage, data, "main");
        }

        public void Run()
        {
            DateTime previousTime = DateTime.Now;
            while (window.Exists)
            {
                DateTime newTime = DateTime.Now;
                float deltaTime = (float)(newTime - previousTime).TotalSeconds;

                if (window.Exists)
                {
                    if (windowResized)
                    {
                        Resize();
                    }
                    Update(deltaTime);
                    Render(deltaTime);
                }

                previousTime = newTime;
            }
            graphicsDevice.WaitForIdle();
        }

        private void Update(float deltaTime)
        {
            InputSnapshot inputSnapshot = window.PumpEvents();
            runtimeData.Update(window, inputSnapshot, deltaTime);

            imGuiRenderer.Update(deltaTime, inputSnapshot);
            SubmitImGui(deltaTime, inputSnapshot);
        }

        private void SubmitImGui(float deltaTime, InputSnapshot inputSnapshot)
        {
            uiController.SubmitUI(deltaTime, inputSnapshot);
            uiController.Update(deltaTime);
        }

        private void Render(float deltaTime)
        {
            RenderShader();
            RenderImGui();
            graphicsDevice.WaitForIdle();

            // doing a final check if window was closed in middle of rendering
            if (window.Exists)
            {
                graphicsDevice.SwapBuffers(swapchain);
            }
        }

        private void RenderShader()
        {
            commandList.Begin();

            commandList.SetFramebuffer(swapchain.Framebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Black);

            if (isInitialized)
            {
                commandList.UpdateBuffer(runtimeDataBuffer, 0, runtimeData);
                commandList.SetPipeline(pipeline);
                commandList.SetGraphicsResourceSet(0, resourceSet);
                commandList.SetVertexBuffer(0, vertexBuffer);
                commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt32);

                commandList.DrawIndexed(
                    indexCount: 6,
                    instanceCount: 1,
                    indexStart: 0,
                    vertexOffset: 0,
                    instanceStart: 0);
            }
            commandList.End();

            graphicsDevice.SubmitCommands(commandList);
        }

        private void RenderImGui()
        {
            commandList.Begin();
            commandList.SetFramebuffer(swapchain.Framebuffer);
            imGuiRenderer.Render(graphicsDevice, commandList);
            commandList.End();

            graphicsDevice.SubmitCommands(commandList);
        }

        private void Resize()
        {
            windowResized = false;
            graphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
            imGuiRenderer.WindowResized(window.Width, window.Height);
        }

        private void DisposeShaders()
        {
            if (shaders == null) return;
            foreach (Shader shader in shaders)
            {
                shader.Dispose();
            }
        }

        public void Dispose()
        {
            imGuiRenderer.Dispose();
            pipeline?.Dispose();
            DisposeShaders();
            resourceSet?.Dispose();
            resourceLayout?.Dispose();
            commandList?.Dispose();
            runtimeDataBuffer?.Dispose();
            indexBuffer.Dispose();
            vertexBuffer.Dispose();
            graphicsDevice.Dispose();
        }

        private const string VertexCode = @"
#version 450

layout(location = 0) in vec3 Position;

void main()
{
    gl_Position = vec4(Position, 1);
}";
        private const string fragmentHeaderCode = @"
#version 450

layout(set = 0, binding = 0) uniform RuntimeData
{
    vec4 mouse;
    vec2 resolution;
    float time;
    float deltaTime;
    int frame;
};

layout(location = 0) out vec4 out_Color;";

    }
}