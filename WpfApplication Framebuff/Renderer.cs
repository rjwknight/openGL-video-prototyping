using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WpfApplication1
{
    // Renderer from FreakinPenguin's viaFramebuffer demo
    public class Renderer
    {
        #region Fields

        private float angle;

        private int displayList;

        private Size size;

        #endregion

        #region Constructors and Destructors

        public Renderer(Size size)
        {
            this.size = size;
        }

        #endregion

        #region Public Methods and Operators

        public void Render()
        {
            if (displayList <= 0)
            {
                displayList = GL.GenLists(1);
                GL.NewList(displayList, OpenTK.Graphics.OpenGL.ListMode.Compile);

                GL.Color3(System.Drawing.Color.Red);

                GL.Begin(OpenTK.Graphics.OpenGL.PrimitiveType.Points);

                Random rnd = new Random();
                for (int i = 0; i < 10000; i++)
                {
                    float factor = 0.2f;
                    Vector3 position = new Vector3(
                        rnd.Next(-1000, 1000) * factor,
                        rnd.Next(-1000, 1000) * factor,
                        rnd.Next(-1000, 1000) * factor);
                    GL.Vertex3(position);

                    position.Normalize();
                    GL.Normal3(position);
                }

                GL.End();

                GL.EndList();
            }

            GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Lighting);
            GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Light0);
            GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
            GL.BlendFunc(OpenTK.Graphics.OpenGL.BlendingFactorSrc.SrcAlpha, OpenTK.Graphics.OpenGL.BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest);

            GL.ClearColor(System.Drawing.Color.LightBlue);
            GL.Clear(OpenTK.Graphics.OpenGL.ClearBufferMask.DepthBufferBit | OpenTK.Graphics.OpenGL.ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Modelview);
            GL.LoadIdentity();

            float halfWidth = this.size.Width * 0.5f;
            this.angle += 1f;
            GL.Rotate(this.angle, Vector3.UnitZ);
            GL.Rotate(this.angle, Vector3.UnitY);
            GL.Rotate(this.angle, Vector3.UnitX);
            GL.Translate(0.5f, 0, 0);

            GL.CallList(this.displayList);
        }

        #endregion
    }

}
