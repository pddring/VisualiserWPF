using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VisualiserWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Thread tCamera;
        private bool liveCamera = false;
        int camID = 1;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Camera_Choose_Camera(object sender, RoutedEventArgs e)
        {
            if (liveCamera)
            {
                liveCamera = false;
            } else
            {
                CaptureCamera();
            }
        }


        private void CaptureCamera()
        {
            if(!liveCamera)
            {
                tCamera = new Thread(new ThreadStart(CaptureCameraCallback));
                tCamera.Start();
                liveCamera = true;
            }
            
        }

        private void CaptureCameraCallback()
        {
            Mat frame = new Mat();
            int frameCount = 0;
            VideoCapture capture = new VideoCapture();
            capture.Open(camID);
            if (!capture.IsOpened())
            {
                return;
            }

            // discover max resolution
            capture.Set(CaptureProperty.FrameWidth, 10000);
            capture.Set(CaptureProperty.FrameHeight, 10000);
            int w = (int)capture.Get(CaptureProperty.FrameWidth);
            int h = (int)capture.Get(CaptureProperty.FrameHeight);

            DateTime lastFrame = DateTime.Now;
            float fps = 0;
            while (liveCamera)
            {
                capture.Read(frame);
                TimeSpan tDiff = DateTime.Now - lastFrame;
                if(frameCount % 10 == 0 && tDiff.Milliseconds > 0)
                {
                    fps = 1000 / tDiff.Milliseconds;
                    frameCount = 0;
                }
                frameCount++;
                lastFrame = DateTime.Now;
                var bmp = frame.ToWriteableBitmap(PixelFormats.Bgr24);
                bmp.Freeze();
                Dispatcher.Invoke(new Action(() => {
                        imgPreview.Source = bmp;
                        lblStatus.Content = fps + " fps " + w + "x" + h;
                    }));
                
                
            }
            capture.Release();
            try
            {
                Dispatcher.Invoke(new Action(() => {
                    lblStatus.Content = "";
                }));
            } catch
            {

            }
            
        }

        int rotation = 0;
        int zoom = 100;
        double offsetX = 0;
        double offsetY = 0;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            liveCamera = false;
        }

        TransformGroup transforms = new TransformGroup();
        TranslateTransform currentTranslateTransform;

        private void updateTransforms()
        {
            transforms.Children.Clear();
            transforms.Children.Add(new RotateTransform(rotation, imgPreview.ActualWidth / 2, imgPreview.ActualHeight / 2));
            transforms.Children.Add(new ScaleTransform((double)zoom / 100.0, (double)zoom / 100.0, imgPreview.ActualWidth / 2, imgPreview.ActualHeight / 2));
            transforms.Children.Add(currentTranslateTransform = new TranslateTransform(offsetX, offsetY));
            imgPreview.RenderTransform = transforms;
        }

        private void imgPreview_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int mul = 1;
            if(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                mul = 5;
            }
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                rotation += mul * (e.Delta > 0 ? 1 : -1);
            } else
            {
                zoom += mul * (e.Delta > 0 ? 1 : -1);
            }

            

            updateTransforms();
        }

        private void RotateLeft(object sender, ExecutedRoutedEventArgs e)
        {
            rotation = 90;
            updateTransforms();
        }

        private void RotateRight(object sender, ExecutedRoutedEventArgs e)
        {
            rotation = 270;
            updateTransforms();
        }

        private void RotateUp(object sender, ExecutedRoutedEventArgs e)
        {
            rotation = 0;
            updateTransforms();
        }

        private void RotateDown(object sender, ExecutedRoutedEventArgs e)
        {
            rotation = 180;
            updateTransforms();
        }

        private void Quit(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        bool mouseDown = false;
        System.Windows.Point startDragPos;

        private void imgPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            startDragPos = e.GetPosition(this);
            startDragPos.X -= offsetX;
            startDragPos.Y -= offsetY;
        }

        private void imgPreview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
        }

        private void imgPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if(mouseDown)
            {
                System.Windows.Point newPos = e.GetPosition(this);
                offsetX = newPos.X - startDragPos.X;
                offsetY = newPos.Y - startDragPos.Y;
                updateTransforms();
            }
        }

        private void ResetView(object sender, ExecutedRoutedEventArgs e)
        {
            offsetX = offsetY = rotation = 0;
            zoom = 100;
            updateTransforms();
        }
    }
}
