using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using VidGrabNoForm;

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

        public MainWindow()
        {
            InitializeComponent();

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
    }
}