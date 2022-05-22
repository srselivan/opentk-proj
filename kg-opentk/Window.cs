using kg_opentk.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

namespace kg_opentk
{
    
    public class Window : GameWindow
    {
        private struct ObjectBuffers
        {
            public int _vertexBufferObject;
            public int _vertexArrayObject;
            public int _elementBufferObject;

            public ObjectBuffers()
            {
                _vertexBufferObject = GL.GenBuffer();
                _vertexArrayObject = GL.GenVertexArray();
                _elementBufferObject = GL.GenBuffer();
            }
        }

        private struct SphereTextures
        {
            public Texture _diffuseMap;
            public Texture _specularMap;

            public SphereTextures(string diffusePath, string specularPath)
            {
                _diffuseMap = Texture.LoadFromFile(diffusePath);
                _specularMap = Texture.LoadFromFile(specularPath);
            }

            public void Use()
            {
                _diffuseMap.Use(TextureUnit.Texture0);
                _specularMap.Use(TextureUnit.Texture1);
            }
        }

        Sphere _earth;
        Sphere _sun;

        private readonly Vector3 _lightPos = new Vector3(0.0f, 0.0f, 0.0f);

        private Shader _shader;

        SphereTextures _earthTexture;
        ObjectBuffers _earthBuffers;

        SphereTextures _sunTexture;
        ObjectBuffers _sunBuffers;

        private Camera _camera;
        private bool _firstMove = true;
        private Vector2 _lastPos;

        private double _time = 0;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            VSync = VSyncMode.On;
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            _camera = new Camera(Vector3.UnitZ * 4, Size.X / (float)Size.Y);
            CursorGrabbed = true;

            _shader = new Shader(
                "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Shaders/shader.vert",
                "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Shaders/lighting.frag");
            SetShader();

            {
                _sun = new Sphere();
                _sunTexture = new SphereTextures(
                    "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Resources/sun.jpg",
                    "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Resources/sun_specular.jpg"
                    );
                _sunBuffers = new ObjectBuffers();
                GL.BindBuffer(BufferTarget.ArrayBuffer, _sunBuffers._vertexBufferObject);
                GL.BindVertexArray(_sunBuffers._vertexArrayObject);

                var sphereVert = _sun.GetSphere();
                GL.NamedBufferStorage(
                   _sunBuffers._vertexBufferObject,
                   sphereVert.Length * sizeof(float),        // the size needed by this buffer
                   sphereVert,                               // data to initialize with
                   BufferStorageFlags.MapWriteBit);          // at this point we will only write to the buffer

                GL.EnableVertexArrayAttrib(_sunBuffers._vertexArrayObject, 0);
                GL.VertexArrayVertexBuffer(_sunBuffers._vertexArrayObject, 0, _sunBuffers._vertexArrayObject, IntPtr.Zero, 8 * sizeof(float));

                var indices = _sun.GetIndices();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _sunBuffers._elementBufferObject);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            }

            {
                _earth = new Sphere();
                _earthTexture = new SphereTextures(
                    "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Resources/earth.jpg",
                    "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Resources/earth_specular.jpg"
                    );
                _earthBuffers = new ObjectBuffers();
                GL.BindVertexArray(_earthBuffers._vertexArrayObject);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _earthBuffers._vertexBufferObject);

                var sphereVert = _earth.GetSphere();
                GL.NamedBufferStorage(
                   _earthBuffers._vertexBufferObject,
                   sphereVert.Length * sizeof(float),        // the size needed by this buffer
                   sphereVert,                           // data to initialize with
                   BufferStorageFlags.MapWriteBit);    // at this point we will only write to the buffer

                GL.EnableVertexArrayAttrib(_earthBuffers._vertexArrayObject, 0);

                GL.VertexArrayVertexBuffer(_earthBuffers._vertexArrayObject, 0, _earthBuffers._vertexArrayObject, IntPtr.Zero, 8 * sizeof(float));

                var indices = _earth.GetIndices();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _earthBuffers._elementBufferObject);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            }

        }

        private void SetShader()
        {
            _shader.SetInt("material.diffuse", 0);
            _shader.SetInt("material.specular", 1);
            _shader.SetVector3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            _shader.SetFloat("material.shininess", 100000.0f);
            _shader.SetVector3("light.position", _lightPos);
            _shader.SetFloat("light.constant", 0.01f);
            _shader.SetFloat("light.linear", 0.09f);
            _shader.SetFloat("light.quadratic", 0.01f);
            _shader.SetVector3("light.ambient", new Vector3(0.2f));
            _shader.SetVector3("light.diffuse", new Vector3(0.5f));
            _shader.SetVector3("light.specular", new Vector3(1.0f));
        }

        private void SetShaderAttr()
        {
                var positionLocation = _shader.GetAttribLocation("aPos");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

                var normalLocation = _shader.GetAttribLocation("aNormal");
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

                var texCoordLocation = _shader.GetAttribLocation("aTexCoords");
                GL.EnableVertexAttribArray(texCoordLocation);
                GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _time += e.Time;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            {
                GL.BindVertexArray(_earthBuffers._vertexArrayObject);

                SetShaderAttr();
                _earthTexture.Use();

                _shader.SetMatrix4("model",
                    Matrix4.Identity
                    * Matrix4.CreateRotationZ((float)_time)
                    * Matrix4.CreateScale(0.4f)
                    * Matrix4.CreateTranslation(new Vector3(3.0f, 0.0f, 0.0f))
                    * Matrix4.CreateRotationZ((float)_time / 1.5f)
                    * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90.0f))
                    );
                _shader.SetMatrix4("view", _camera.GetViewMatrix());
                _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
                _shader.SetVector3("viewPos", _camera.Position);
                _shader.Use();

                GL.DrawElements(PrimitiveType.Triangles, _earth.GetIndices().Length, DrawElementsType.UnsignedInt, 0);
            }

            {
                GL.BindVertexArray(_sunBuffers._vertexArrayObject);

                SetShaderAttr();
                _sunTexture.Use();

                _shader.SetMatrix4("model",
                    Matrix4.Identity
                    * Matrix4.CreateRotationZ((float)_time / 10.0f) 
                    * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90.0f))
                    ); 
                _shader.SetMatrix4("view", _camera.GetViewMatrix());
                _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
                _shader.SetVector3("viewPos", _camera.Position);
                _shader.Use();

                GL.DrawElements(PrimitiveType.Triangles, _sun.GetIndices().Length, DrawElementsType.UnsignedInt, 0);
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            const float cameraSpeed = 1.5f;
            const float sensitivity = 0.2f;

            if (input.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
            }
            if (input.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
            }
            if (input.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
            }
            if (input.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
            }
            if (input.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
            }

            var mouse = MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _camera.Fov -= e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }
    }
}
