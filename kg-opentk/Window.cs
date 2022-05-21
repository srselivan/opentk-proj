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
        Sphere _earth;
        //Sphere _sun;

        private readonly Vector3 _lightPos = new Vector3(0.0f, 0.0f, 0.0f);

        private int _vertexBufferObject;
        private int _vertexArrayObject;
        private int _elementBufferObject;

        private Shader _shader;

        private Texture _diffuseMap;
        private Texture _specularMap;

        private Camera _camera;

        private double _time = 0;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            VSync = VSyncMode.On;

            _earth = new Sphere(0.5f, 2.0f, 0.0f, 0.0f);
            //_sun = new Sphere(1.0f, 0.0f, 0.0f, 0.0f);

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            _vertexBufferObject = GL.GenBuffer();
            _vertexArrayObject = GL.GenVertexArray();
            _elementBufferObject = GL.GenBuffer();
            GL.BindVertexArray(_vertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);

            _shader = new Shader(
                "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Shaders/shader.vert",
                "C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Shaders/lighting.frag");
            SetShader();

            var positionLocation = _shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            var normalLocation = _shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            var texCoordLocation = _shader.GetAttribLocation("aTexCoords");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            var sphereVert = _earth.GetSphere();
            GL.NamedBufferStorage(
               _vertexBufferObject,
               sphereVert.Length * sizeof(float),        // the size needed by this buffer
               sphereVert,                           // data to initialize with
               BufferStorageFlags.MapWriteBit);    // at this point we will only write to the buffer

            GL.EnableVertexArrayAttrib(_vertexArrayObject, 0);

            GL.VertexArrayVertexBuffer(_vertexArrayObject, 0, _vertexArrayObject, IntPtr.Zero, 8 * sizeof(float));

            var indices = _earth.GetIndices();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

            _diffuseMap = Texture.LoadFromFile("C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Resources/earth.jpg");
            _specularMap = Texture.LoadFromFile("C:/Users/Sergey/source/repos/opentk-proj/kg-opentk/Resources/earth_specular.jpg");

            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

        }

        private void SetShader()
        {
            _shader.SetInt("material.diffuse", 0);
            _shader.SetInt("material.specular", 1);
            _shader.SetVector3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            _shader.SetFloat("material.shininess", 100000.0f);
            _shader.SetVector3("light.position", _lightPos);
            _shader.SetFloat("light.constant", 0.1f);
            _shader.SetFloat("light.linear", 0.09f);
            _shader.SetFloat("light.quadratic", 0.032f);
            _shader.SetVector3("light.ambient", new Vector3(0.2f));
            _shader.SetVector3("light.diffuse", new Vector3(0.5f));
            _shader.SetVector3("light.specular", new Vector3(1.0f));
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _time += e.Time;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.BindVertexArray(_vertexArrayObject);

            _diffuseMap.Use(TextureUnit.Texture0);
            _specularMap.Use(TextureUnit.Texture1);

            _shader.SetMatrix4("model", 
                Matrix4.Identity 
                * Matrix4.CreateRotationZ((float)_time)
                * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90.0f))
                );
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.SetVector3("viewPos", _camera.Position);
            _shader.Use();

            GL.DrawElements(PrimitiveType.Triangles, _earth.GetIndices().Length, DrawElementsType.UnsignedInt, 0);


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
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        protected override void OnUnload()
        {
            // Unbind all the resources by binding the targets to 0/null.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            // Delete all the resources.
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);

            GL.DeleteProgram(_diffuseMap.Handle);
            GL.DeleteProgram(_specularMap.Handle);

            base.OnUnload();
        }
    }
}
