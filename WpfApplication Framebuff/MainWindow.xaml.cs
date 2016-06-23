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

        private int frames;
        private GLControl glcontrol;
        private DateTime lastMeasureTime;
        private Renderer renderer;

        public MainWindow()
        {
            InitializeComponent();

            renderer = new Renderer(new Size(400, 400));

            lastMeasureTime = DateTime.Now;
            frames = 0;

            glcontrol = new GLControl();
            glcontrol.Paint += this.GlcontrolOnPaint;
            glcontrol.Dock = System.Windows.Forms.DockStyle.Fill;
            Host.Child = this.glcontrol;

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
        private void GlcontrolOnPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            this.glcontrol.MakeCurrent();

            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Projection);
            GL.LoadIdentity();
            float halfWidth = (float)(this.glcontrol.Width / 2);
            float halfHeight = (float)(this.glcontrol.Height / 2);
            GL.Ortho(-halfWidth, halfWidth, halfHeight, -halfHeight, 1000, -1000);
            GL.Viewport(this.glcontrol.Size);

            this.renderer.Render();

            GL.Finish();

            this.glcontrol.SwapBuffers();

            this.frames++;
        }

        // Penguin
        private void TimerOnTick(object sender, EventArgs e)
        {
            if (DateTime.Now.Subtract(this.lastMeasureTime) > TimeSpan.FromSeconds(1))
            {
                this.Title = this.frames + "fps";
                this.frames = 0;
                this.lastMeasureTime = DateTime.Now;
            }

            this.glcontrol.Invalidate();
        }


    }


}