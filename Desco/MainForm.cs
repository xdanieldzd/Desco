using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Cobalt;
using Cobalt.Mesh;

using Desco.ModelParser;

namespace Desco
{
    public partial class MainForm : Form
    {
        static readonly string projectionMatrixName = "projection_matrix";
        static readonly string modelviewMatrixName = "modelview_matrix";

        Vector3 eye, target;
        float scale;
        Matrix4 modelviewMatrix;

        Camera camera;
        Shader shader;
        Cobalt.Font font;

        ObfBinary obfBinary;

        OpenTK.Input.KeyboardState lastKbd;
        bool wireframe, culling;
        float timer;

        public MainForm()
        {
            InitializeComponent();

            Application.Idle += ((s, e) =>
            {
                if (shader != null)
                {
                    timer += Core.DeltaTime / 16.0f;
                    shader.SetUniform("timer", timer);
                }
            });
        }

        private void renderControl1_Load(object sender, EventArgs e)
        {
            eye = new Vector3(0.0f, 0.0f, 15.0f);
            target = new Vector3(0.0f, 0.0f, 0.0f);
            scale = 0.1f;
            modelviewMatrix = Matrix4.CreateScale(scale) * Matrix4.LookAt(eye, target, Vector3.UnitY);

            camera = new Camera();
            shader = new Shader(
                File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\\VertexShader.glsl")),
                File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\\FragmentShader.glsl")));
            font = new Cobalt.Font("DejaVu Sans");

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1) LoadFile(args[1]);

            lastKbd = OpenTK.Input.Keyboard.GetState();
            wireframe = false;
            culling = false;

            shader.SetUniform("texture", (int)0);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            // "E:\[SSD User Data]\Downloads\disg-BLUS30727\map00107\map00107.obf"
            // "E:\[SSD User Data]\Downloads\disg-BLUS30727\map30001\map30001.obf"

            // "E:\[SSD User Data]\Downloads\disg-BLUS30727\BLUS30727\PS3_GAME\USRDIR\Data\MAP (converted)\mp001\map00101.pac"
        }

        private void LoadFile(string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                string fileExtension = Path.GetExtension(file);

                switch (fileExtension)
                {
                    case ".obf":
                        obfBinary = new ObfBinary(stream);
                        break;

                    case ".pac":
                        {
                            Conversion.PacFile pac = new Conversion.PacFile(stream);
                            Conversion.PacFileEntry pacObfEntry = pac.Files.FirstOrDefault(x => x.Filename.EndsWith(".obf"));
                            if (pacObfEntry == null) break;

                            stream.Seek(pac.DataStartPosition + pacObfEntry.Offset, SeekOrigin.Begin);
                            using (BinaryReader reader = new BinaryReader(stream))
                            {
                                using (MemoryStream subStream = new MemoryStream(reader.ReadBytes((int)pacObfEntry.CalculatedLength)))
                                {
                                    obfBinary = new ObfBinary(subStream);
                                }
                            }
                        }
                        break;

                    default:
                        MessageBox.Show("Unsupported file specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }

                if (obfBinary != null)
                {
                    tscmbAssets.ComboBox.DataSource = obfBinary.Assets.ToList();
                    tscmbAssets.ComboBox.DisplayMember = "Key";
                    tscmbAssets.ComboBox.ValueMember = "Value";
                }
            }
        }

        private void renderControl1_Render(object sender, EventArgs e)
        {
            this.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} - {1} FPS", Application.ProductName, Core.CurrentFramesPerSecond);

            RenderControl renderControl = (sender as RenderControl);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            OpenTK.Input.KeyboardState kbdState = OpenTK.Input.Keyboard.GetState();

            if (renderControl.Focused)
            {
                if (kbdState[OpenTK.Input.Key.Escape] && !lastKbd[OpenTK.Input.Key.Escape])
                    Application.Exit();

                if (kbdState[OpenTK.Input.Key.F1] && !lastKbd[OpenTK.Input.Key.F1])
                    renderControl.VSync = !renderControl.VSync;

                if (kbdState[OpenTK.Input.Key.F2] && !lastKbd[OpenTK.Input.Key.F2])
                    wireframe = !wireframe;

                if (kbdState[OpenTK.Input.Key.F3] && !lastKbd[OpenTK.Input.Key.F3])
                    culling = !culling;
            }

            lastKbd = kbdState;

            if (renderControl.ClientRectangle.Contains(this.PointToClient(Control.MousePosition)))
                camera.Update(Core.DeltaTime);

            if (wireframe)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            else
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            if (culling)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            if (shader != null)
            {
                Matrix4 tempMatrix = modelviewMatrix * camera.GetViewMatrix();
                shader.SetUniformMatrix(modelviewMatrixName, false, tempMatrix);
            }

            if (obfBinary != null)
            {
                if (tsbRenderAll.Checked)
                {
                    obfBinary.RenderAssets(shader);
                }
                else
                {
                    var selection = tscmbAssets.ComboBox.SelectedItem;
                    obfBinary.RenderAsset(((KeyValuePair<short, ICollection<short>>)selection).Key, shader);
                }
            }

            if (font != null)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                StringBuilder builder = new StringBuilder();
                builder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:0} FPS\n", Core.CurrentFramesPerSecond);
                builder.AppendLine();
                builder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "Vsync (F1): {0}\n", renderControl.VSync);
                builder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "Wireframe (F2): {0}\n", wireframe);
                builder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "Culling (F3): {0}\n", culling);

                font.DrawString(8.0f, 8.0f, builder.ToString());
            }
        }

        private void renderControl1_Resize(object sender, EventArgs e)
        {
            RenderControl renderControl = (sender as RenderControl);
            GL.Viewport(0, 0, renderControl.Width, renderControl.Height);

            if (shader != null)
            {
                float aspectRatio = (renderControl.ClientRectangle.Width / (float)(renderControl.ClientRectangle.Height));
                float fovY = MathHelper.PiOver4;
                //fovY = MathHelper.Pi / 16.0f; // game-ish
                shader.SetUniformMatrix(projectionMatrixName, false, Matrix4.CreatePerspectiveFieldOfView(fovY, aspectRatio, 0.1f, 15000.0f));
            }

            if (font != null)
                font.SetScreenSize(renderControl.ClientRectangle.Width, renderControl.ClientRectangle.Height);
        }
    }
}
