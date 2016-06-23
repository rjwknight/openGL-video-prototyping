using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using VidGrabNoForm;
using System.Windows.Media.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;
using EnableCap = OpenTK.Graphics.OpenGL.EnableCap;
using FramebufferAttachment = OpenTK.Graphics.OpenGL.FramebufferAttachment;
using FramebufferErrorCode = OpenTK.Graphics.OpenGL.FramebufferErrorCode;
using FramebufferTarget = OpenTK.Graphics.OpenGL.FramebufferTarget;
using GL = OpenTK.Graphics.OpenGL.GL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
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
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string AUDIO_CLIPS = "*.mp3;*.wav;*.wma;*.mid";

        public const string VIDEO_CLIPS =
            "*.mp*;*.vro;*.avi;*.asf;*.wmv;*.vob;*.avs;*.mov;*.divx;*.mp4;*.mts;*.3gp;*.swf;*.m2v;*.mkv;*.flv;*.webm;*.ts;*.m4v;*.mp4v;*.ogg;*.amv;*.rm;*.m2t*";

        public const string IMAGE_FILES = "*.jpg;*.jpeg;*.jpe;*.bmp;*.gif;*.png";

        public const string OPEN_MEDIA_FILES = "All media files|" + VIDEO_CLIPS + AUDIO_CLIPS + IMAGE_FILES
                                               + "|Video clips|" + VIDEO_CLIPS
                                               + "|Audio clips|" + AUDIO_CLIPS;

        public const string OPEN_MEDIA_FILES_EXTENDED = OPEN_MEDIA_FILES
                                                        + "|Image files|" + IMAGE_FILES;

        public const string OPEN_PICTURE_FILES =
            "Image files|*.bmp;*.jpg;*.gif;*.png;*.tif;*.tiff;*.wmf;*.emf;*.exf;*.jpe;*.jpeg";

        private readonly VideoGrabberWPF Vg;
        private bool IsPlaying;
        private bool maintainAspectRatio = true;

        private WriteableBitmap backbuffer;

        private FrameBufferHandler framebufferHandler;

        private int frames;

        private DateTime lastMeasureTime;

        private Renderer renderer;

        public MainWindow()
        {
            InitializeComponent();

            this.renderer = new Renderer(new Size(400, 400));
            this.framebufferHandler = new FrameBufferHandler();

            GL.Enable(EnableCap.Texture2D);
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += this.TimerOnTick;
            timer.Start();
            // Top panel is video feed
            // Get BMP from video feed, write into framebuffer
            // Bottom panel is the video image that is in the frame buffer, plus the OpenGL guff



            Vg = new VideoGrabberWPF();
            Vg.OnFirstFrameReceived += Vg_OnFirstFrameReceived;
            Vg.OnFrameSourceAvailable += Vg_OnFrameSourceAvailable;
            Vg.OnFrameBitmap += VideoGrabber1_OnFrameBitmap;
        }

        private void Vg_OnFrameSourceAvailable(object sender, EventArgs e)
        {
            image1.Dispatcher.Invoke((Action) (() => image1.Source = Vg.FrameSource));
        }

        private void Vg_OnFirstFrameReceived(object sender, EventArgs e)
        {
            double AspectRatio = Vg.VideoWidth/Vg.VideoHeight;
            image1.Height = image1.Width*AspectRatio;
        }

        private void OpenClip_Click(object sender, RoutedEventArgs e)
        {
            var fd = new OpenFileDialog();

            fd.Filter = OPEN_MEDIA_FILES_EXTENDED;
            if (fd.ShowDialog() == true)
            {
                Vg.PlayerFileName = fd.FileName;
                Vg.OpenPlayer();
                IsPlaying = true;
                PlayClip.Content = "Pause";
                PlayClip.IsEnabled = true;
                image1.Visibility = Visibility.Visible;
            }
        }

        private void CloseClip_Click(object sender, RoutedEventArgs e)
        {
            Vg.ClosePlayer();
            PlayClip.IsEnabled = false;
            image1.Visibility = Visibility.Hidden;
        }

        private void PlayClip_Click(object sender, RoutedEventArgs e)
        {
            if (Vg.PlayerState == TPlayerState.ps_Closed)
                return;

            if (IsPlaying)
            {
                Vg.PausePlayer();
                IsPlaying = false;
                PlayClip.Content = "Play";
            }
            else
            {
                Vg.RunPlayer();
                IsPlaying = true;
                PlayClip.Content = "Pause";
            }
        }

        private void VideoGrabber1_OnFrameBitmap(object sender, TOnFrameBitmapEventArgs e)
        {
            var VideoGrabberSender = (VideoGrabberWPF) sender;
            var FrameInfo = (TFrameInfo) Marshal.PtrToStructure(e.frameInfo, typeof (TFrameInfo));
            var FrameBitmapInfo = (TFrameBitmapInfo) Marshal.PtrToStructure(e.bitmapInfo, typeof (TFrameBitmapInfo));

            // FrameBitmapInfo.bitmapDataPtr gives a direct access to the bitmap bits
            // FrameBitmapInfo.bitmapSize returns the size of the bitmap bits

            //Access the raw pixels here!!!
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Vg.ClosePlayer();
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            var chkbox = sender as CheckBox;
            if (chkbox == null)
                return;
            if (chkbox.IsChecked == true)
            {
                maintainAspectRatio = true;
                image1.Stretch = Stretch.Uniform;
            }
            else
            {
                maintainAspectRatio = false;
                image1.Stretch = Stretch.Fill;
            }
        }

        // Penguin
        private void Render()
        {
            if (this.image.ActualWidth <= 0 || this.image.ActualHeight <= 0)
            {
                return;
            }

            this.framebufferHandler.Prepare(new Size((int)this.ActualWidth, (int)this.ActualHeight));

            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Projection);
            GL.LoadIdentity();
            float halfWidth = (float)(this.ActualWidth / 2);
            float halfHeight = (float)(this.ActualHeight / 2);
            GL.Ortho(-halfWidth, halfWidth, halfHeight, -halfHeight, 1000, -1000);
            GL.Viewport(0, 0, (int)this.ActualWidth, (int)this.ActualHeight);

            string filename2 = "C:\\Users\\hp\\image.bmp";
            int loadTexture = LoadTexture(filename2);
            //DrawImage(loadTexture);

            this.renderer.Render();

            GL.Finish();

            this.framebufferHandler.Cleanup(ref this.backbuffer);
           
            if (this.backbuffer != null)
            {
                this.image.Source = this.backbuffer;
            }

            this.frames++;
        }

        // Penguin
        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (DateTime.Now.Subtract(this.lastMeasureTime) > TimeSpan.FromSeconds(1))
            {
                this.Title = this.frames + "fps";
                this.frames = 0;
                this.lastMeasureTime = DateTime.Now;
            }

            this.Render();
        }

        static int LoadTexture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(filename);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            // We will not upload mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // We can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(filename);
            BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            bmp.UnlockBits(bmp_data);

            return id;
        }

        void DrawImage(int textureID)
        {
            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            //glOrtho(0.0, glutGet(GLUT_WINDOW_WIDTH), 0.0, glutGet(GLUT_WINDOW_HEIGHT), -1.0, 1.0);
            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
            //GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Lighting);

            GL.Color3(1, 1, 1);
            //GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            //GL.BindTexture(OpenTK.Graphics.OpenGL.EnableCap.Texture2D, textureID);

            
            // Draw a textured quad
            GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);
            //GL.TexCoord2(0, 0);
            GL.Vertex3(0, 0, 1000);

            //GL.TexCoord2(0, 1);
            GL.Vertex3(0, 100, 1000);

            //GL.TexCoord2(1, 1);
            GL.Vertex3(100, 100, 1000);

            //GL.TexCoord2(1, 0);
            GL.Vertex3(100, 0, 1000);
            GL.End();

            //GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            GL.PopMatrix();
            
            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Projection);
            GL.PopMatrix();

            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Modelview);
            
        }

    }


}