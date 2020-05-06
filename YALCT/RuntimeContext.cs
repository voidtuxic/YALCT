using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace YALCT
{
    public class RuntimeContext : IDisposable
    {
        private Sdl2Window window;
        private bool windowResized = false;

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
        private int fragmentHeaderLineCount = -1;
        private bool forceRecreate;

        private readonly List<Texture> textures = new List<Texture>();
        private readonly List<TextureView> textureViews = new List<TextureView>();
        private readonly List<TextureView> imguiTextureViews = new List<TextureView>();
        private readonly List<YALCTShaderResource> imguiTextures = new List<YALCTShaderResource>();

        private ImGuiRenderer imGuiRenderer;
        private ImGuiController uiController;

        public ImGuiRenderer ImGuiRenderer => imGuiRenderer;
        public GraphicsDevice GraphicsDevice => graphicsDevice;
        public int Width => window.Width;
        public int Height => window.Height;
        public Sdl2Window Window => window;
        public int FragmentHeaderLineCount
        {
            get
            {
                if (fragmentHeaderLineCount == -1)
                {
                    fragmentHeaderLineCount = Regex.Split(fragmentHeaderCode, "\r\n|\r|\n").Length;
                }
                return fragmentHeaderLineCount;
            }
        }

        public List<YALCTShaderResource> ImguiTextures => imguiTextures;

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
#if DEBUG
                WindowInitialState = WindowState.Maximized,
#else
                WindowInitialState = WindowState.BorderlessFullScreen,
#endif
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
                swapchainDepthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: true,
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
            string newFragmentShader;
            if (backend == GraphicsBackend.OpenGL)
                newFragmentShader = fragmentHeaderCode + BuildShaderResourceCode() + fragmentCode;
            else
                newFragmentShader = fragmentHeaderCode + BuildShaderResourceCode() + fragmentHeaderNonGLCode + fragmentCode;
            if (!forceRecreate && currentFragmentShader != null && currentFragmentShader.Equals(newFragmentShader))
            {
                return;
            }
            forceRecreate = false;

            // shaders
            if (!isInitialized)
            {
                vertexShaderDesc = CreateShaderDescription(VertexCode, ShaderStages.Vertex);
            }
            currentFragmentShader = newFragmentShader;
            Shader[] newShaders;
            try
            {
                ShaderDescription fragmentShaderDesc = CreateShaderDescription(currentFragmentShader, ShaderStages.Fragment);
                newShaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
                DisposeShaders();
                shaders = newShaders;
                uiController.SetError(null);
            }
            catch (Exception e)
            {
                uiController.SetError(e.Message);
                return;
            }

            // resources
            runtimeDataBuffer?.Dispose();
            runtimeDataBuffer = factory.CreateBuffer(new BufferDescription(
                YALCTRuntimeData.Size,
                BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            runtimeDataBuffer.Name = "YALCT Runtime Data";

            List<ResourceLayoutElementDescription> layoutDescriptions = new List<ResourceLayoutElementDescription>
            {
                new ResourceLayoutElementDescription ("RuntimeData", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription ("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
            };
            List<BindableResource> bindableResources = new List<BindableResource> {
                runtimeDataBuffer,
                graphicsDevice.PointSampler
            };
            for (int i = 0; i < textureViews.Count; i++)
            {
                TextureView view = textureViews[i];
                layoutDescriptions.Add(new ResourceLayoutElementDescription($"InputTex{i}", ResourceKind.TextureReadOnly, ShaderStages.Fragment));
                bindableResources.Add(view);
            }
            resourceLayout?.Dispose();
            resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(layoutDescriptions.ToArray()));
            resourceLayout.Name = "YALCT Resource Layout";

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(resourceLayout, bindableResources.ToArray());
            resourceSet?.Dispose();
            resourceSet = factory.CreateResourceSet(resourceSetDescription);
            resourceSet.Name = "YALCT Resource Set";

            // pipeline
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

            isInitialized = true;
        }

        private string BuildShaderResourceCode()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < textureViews.Count; i++)
            {
                builder.AppendLine($"layout(set = 0, binding = {i + 2}) uniform texture2D InputTex{i};");
            }
            return builder.ToString();
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

        public bool LoadTexture(YALCTFilePickerItem item)
        {
            try
            {
                ImageSharpTexture imageSharpTex = new ImageSharpTexture(item.FullPath);
                Texture texture = imageSharpTex.CreateDeviceTexture(graphicsDevice, factory);
                TextureView textureView = factory.CreateTextureView(texture);
                textureView.Name = item.Name;
                // as per https://github.com/mellinoe/veldrid/issues/188
                TextureView imguiTextureView = factory.CreateTextureView(new TextureViewDescription(texture, PixelFormat.R8_G8_B8_A8_UNorm_SRgb));
                imguiTextureView.Name = item.Name;
                IntPtr imguiBinding = imGuiRenderer.GetOrCreateImGuiBinding(factory, imguiTextureView);
                textures.Add(texture);
                textureViews.Add(textureView);
                imguiTextureViews.Add(imguiTextureView);
                imguiTextures.Add(new YALCTShaderResource(item, new Vector2(texture.Width, texture.Height), imguiBinding));
            }
            catch (Exception e)
            {
                uiController.SetError(e.Message);
                return false;
            }
            return true;
        }

        public void RemoveTexture(YALCTShaderResource resource)
        {
            int resourceIndex = imguiTextures.IndexOf(resource);
            if (resourceIndex != -1)
            {
                imguiTextureViews[resourceIndex].Dispose();
                textureViews[resourceIndex].Dispose();
                textures[resourceIndex].Dispose();

                imguiTextures.RemoveAt(resourceIndex);
                imguiTextureViews.RemoveAt(resourceIndex);
                textureViews.RemoveAt(resourceIndex);
                textures.RemoveAt(resourceIndex);
            }
        }

        public void DisposeTextures()
        {
            for (int i = 0; i < textures.Count; i++)
            {
                Texture texture = textures[i];
                TextureView view = textureViews[i];
                TextureView imguiView = imguiTextureViews[i];
                view.Dispose();
                imguiView.Dispose();
                texture.Dispose();
            }
            textures.Clear();
            textureViews.Clear();
            imguiTextureViews.Clear();
            imguiTextures.Clear();
            forceRecreate = true;
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
            DisposeTextures();
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
        public const string fragmentHeaderCode = @"#version 450
layout(set = 0, binding = 0) uniform RuntimeData
{
    vec4 mouse;
    vec2 resolution;
    float time;
    float deltaTime;
    int frame;
};
layout(set = 0, binding = 1) uniform sampler Sampler;

layout(location = 0) out vec4 out_Color;

// sample helper function
vec4 sample2D(texture2D sampledTexture, vec2 uv) {
    return texture(sampler2D(sampledTexture, Sampler), uv);
}
";

        // considering shadertoy is truth on this
        public const string fragmentHeaderNonGLCode = @"
#define gl_FragCoord vec4(gl_FragCoord.x, resolution.y - gl_FragCoord.y, gl_FragCoord.zw)
";
    }
}