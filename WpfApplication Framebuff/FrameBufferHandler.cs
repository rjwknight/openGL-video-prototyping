using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK;
using OpenTK.Graphics;
using FramebufferAttachment = OpenTK.Graphics.OpenGL.FramebufferAttachment;
using FramebufferErrorCode = OpenTK.Graphics.OpenGL.FramebufferErrorCode;
using FramebufferTarget = OpenTK.Graphics.OpenGL.FramebufferTarget;
using GL = OpenTK.Graphics.OpenGL.GL;
using PixelFormat = OpenTK.Graphics.PixelFormat;
using PixelInternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat;
using PixelType = OpenTK.Graphics.OpenGL.PixelType;
using RenderbufferStorage = OpenTK.Graphics.OpenGL.RenderbufferStorage;
using RenderbufferTarget = OpenTK.Graphics.OpenGL.RenderbufferTarget;
using Size = System.Drawing.Size;
using TextureMagFilter = OpenTK.Graphics.OpenGL.TextureMagFilter;
using TextureMinFilter = OpenTK.Graphics.OpenGL.TextureMinFilter;
using TextureParameterName = OpenTK.Graphics.OpenGL.TextureParameterName;
using TextureTarget = OpenTK.Graphics.OpenGL.TextureTarget;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

namespace WpfApplication1
{
    // FrameBufferHandler from FreakinPenguin's viaFrameBuffer demo
    internal class FrameBufferHandler
    {
        #region Fields

        private int depthbufferId;

        private int framebufferId;

        private GLControl glControl;

        private bool loaded;

        private Size size;

        private int textureId;

        #endregion

        #region Constructors and Destructors

        public FrameBufferHandler()
        {
            this.loaded = false;
            this.size = Size.Empty;
            this.framebufferId = -1;

            this.glControl = new GLControl(new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 16, 0, 4, 0, 2, false));
            this.glControl.MakeCurrent();
        }

        #endregion

        #region Methods

        internal void Cleanup(ref WriteableBitmap backbuffer)
        {
            if (backbuffer == null || backbuffer.Width != this.size.Width || backbuffer.Height != this.size.Height)
            {
                backbuffer = new WriteableBitmap(
                    this.size.Width,
                    this.size.Height,
                    96,
                    96,
                    PixelFormats.Pbgra32,
                    BitmapPalettes.WebPalette);
            }

            backbuffer.Lock();

            OpenTK.Graphics.OpenGL.GL.ReadPixels(
                0,
                0,
                this.size.Width,
                this.size.Height,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                OpenTK.Graphics.OpenGL.PixelType.UnsignedByte,
                backbuffer.BackBuffer);

            backbuffer.AddDirtyRect(new Int32Rect(0, 0, (int)backbuffer.Width, (int)backbuffer.Height));
            backbuffer.Unlock();
        }

        internal void Prepare(Size framebuffersize)
        {
            if (GraphicsContext.CurrentContext != this.glControl.Context)
            {
                this.glControl.MakeCurrent();
            }

            if (framebuffersize != this.size || this.loaded == false)
            {
                this.size = framebuffersize;
                this.CreateFramebuffer();
            }

            OpenTK.Graphics.OpenGL.GL.BindFramebuffer(OpenTK.Graphics.OpenGL.FramebufferTarget.Framebuffer, this.framebufferId);
        }

        private void CreateFramebuffer()
        {
            this.glControl.MakeCurrent();

            if (this.framebufferId > 0)
            {
                OpenTK.Graphics.OpenGL.GL.DeleteFramebuffer(this.framebufferId);
            }

            if (this.depthbufferId > 0)
            {
                OpenTK.Graphics.OpenGL.GL.DeleteRenderbuffer(this.depthbufferId);
            }

            if (this.textureId > 0)
            {
                OpenTK.Graphics.OpenGL.GL.DeleteTexture(this.textureId);
            }

            this.textureId = OpenTK.Graphics.OpenGL.GL.GenTexture();

            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, this.textureId);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            OpenTK.Graphics.OpenGL.GL.TexParameter(
                OpenTK.Graphics.OpenGL.TextureTarget.Texture2D,
                OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);
            OpenTK.Graphics.OpenGL.GL.TexParameter(
                OpenTK.Graphics.OpenGL.TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);
            OpenTK.Graphics.OpenGL.GL.TexImage2D(
                OpenTK.Graphics.OpenGL.TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgb8,
                this.size.Width,
                this.size.Height,
                0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte,
                IntPtr.Zero);

            this.framebufferId = OpenTK.Graphics.OpenGL.GL.GenFramebuffer();
            OpenTK.Graphics.OpenGL.GL.BindFramebuffer(OpenTK.Graphics.OpenGL.FramebufferTarget.Framebuffer, this.framebufferId);
            OpenTK.Graphics.OpenGL.GL.FramebufferTexture2D(
                OpenTK.Graphics.OpenGL.FramebufferTarget.Framebuffer,
                OpenTK.Graphics.OpenGL.FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                this.textureId,
                0);

            this.depthbufferId = OpenTK.Graphics.OpenGL.GL.GenRenderbuffer();
            OpenTK.Graphics.OpenGL.GL.BindRenderbuffer(OpenTK.Graphics.OpenGL.RenderbufferTarget.Renderbuffer, this.depthbufferId);
            OpenTK.Graphics.OpenGL.GL.RenderbufferStorage(
                OpenTK.Graphics.OpenGL.RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.DepthComponent24,
                this.size.Width,
                this.size.Height);
            OpenTK.Graphics.OpenGL.GL.FramebufferRenderbuffer(
                OpenTK.Graphics.OpenGL.FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer,
                this.depthbufferId);

            OpenTK.Graphics.OpenGL.FramebufferErrorCode error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception();
            }

            this.loaded = true;
        }

        #endregion
    }

}
