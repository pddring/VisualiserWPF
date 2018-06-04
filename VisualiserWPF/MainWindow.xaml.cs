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
    public class Annotation
    {
        public List<System.Windows.Point> points = new List<System.Windows.Point>();
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Thread tCamera;
        private bool liveCamera = false;
        int camID = 0;
        int frameCount = 0;
        int fps = 0;
        SolidColorBrush currentBrush = new SolidColorBrush(Colors.Yellow);
        System.Windows.Threading.DispatcherTimer frameTimer = new System.Windows.Threading.DispatcherTimer();
        Annotation currentAnnotation = new Annotation();

        public enum UIMode
        {
            Pan, Scribble
        };

        UIMode selectedTool = UIMode.Pan;

        public MainWindow()
        {
            InitializeComponent();
            frameTimer.Tick += FrameTimer_Tick;
            frameTimer.Interval = new TimeSpan(0, 0, 1);
            frameTimer.Start();
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            fps = frameCount;
            frameCount = 0;
        }

        private void Camera_Choose_Camera(object sender, RoutedEventArgs e)
        {
            if(liveStream)
            {
                stream.Stop();
                liveStream = false;
            }
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
            while (liveCamera)
            {
                capture.Read(frame);
                frameCount++;
                WriteableBitmap bmp = frame.ToWriteableBitmap(PixelFormats.Bgr24);
                bmp.Freeze();
                updateFrameStats(bmp, w, h);
            }
            frame.Release();
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
            if(liveStream)
            {
                stream.Stop();
                liveStream = false;
            }
        }

        TransformGroup transforms = new TransformGroup();
        TranslateTransform currentTranslateTransform;

        private void updateTransforms()
        {
            transforms.Children.Clear();
            transforms.Children.Add(new RotateTransform(rotation, imgPreview.ActualWidth / 2, imgPreview.ActualHeight / 2));
            transforms.Children.Add(new ScaleTransform((double)zoom / 100.0, (double)zoom / 100.0, imgPreview.ActualWidth / 2, imgPreview.ActualHeight / 2));
            transforms.Children.Add(currentTranslateTransform = new TranslateTransform(offsetX, offsetY));
            mainCanvas.RenderTransform = transforms;
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
        Line currentLine;

        private void imgPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            if(selectedTool == UIMode.Pan)
            {
                startDragPos = e.GetPosition(this);
                startDragPos.X -= offsetX;
                startDragPos.Y -= offsetY;
            }
            

            if(selectedTool == UIMode.Scribble)
            {
                startDragPos = e.GetPosition(mainCanvas);
                currentLine = new Line();
                currentLine.X1 = startDragPos.X;
                currentLine.Y1 = startDragPos.Y;
                currentLine.StrokeThickness = 5;
                currentLine.Stroke = currentBrush;
            }
        }

        private void imgPreview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
        }

        private void imgPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if(mouseDown)
            {
                System.Windows.Point newPos = e.GetPosition(mainCanvas);
                if(selectedTool == UIMode.Pan)
                {
                    newPos = e.GetPosition(this);
                    offsetX = newPos.X - startDragPos.X;
                    offsetY = newPos.Y - startDragPos.Y;
                    updateTransforms();
                }

                if(selectedTool == UIMode.Scribble)
                {
                    currentLine.X2 = newPos.X;
                    currentLine.Y2 = newPos.Y;
                    annotationCanvas.Children.Add(currentLine);
                    currentLine = new Line();
                    currentLine.X1 = newPos.X;
                    currentLine.Y1 = newPos.Y;
                    currentLine.Stroke = currentBrush;
                    currentLine.StrokeThickness = 5;
                }
            }
        }

        private void ResetView(object sender, ExecutedRoutedEventArgs e)
        {
            offsetX = offsetY = rotation = 0;
            zoom = 100;
            updateTransforms();
        }

        Accord.Video.MJPEGStream stream;
        bool liveStream = false;

        private void Camera_Choose_Stream(object sender, ExecutedRoutedEventArgs e)
        {
            RemoteCamChooser dlg = new RemoteCamChooser();
            dlg.ShowDialog();

            if ((bool)dlg.DialogResult)
            {
                if(liveStream)
                {
                    stream.Stop();
                }
                stream = new Accord.Video.MJPEGStream(dlg.txtStream.Text);
                stream.NewFrame += Stream_NewFrame;
                liveCamera = false;
                liveStream = true;
                stream.Start();
            }
        }

        private void updateFrameStats(WriteableBitmap bmp, int w, int h)
        {
            try
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    imgPreview.Source = bmp;
                    imgPreview.Stretch = Stretch.UniformToFill;
                    lblStatus.Content = fps + " fps " + w + "x" + h + " " + selectedTool.ToString();
                }));
            }
            catch
            {

            }
        }

        private void Stream_NewFrame(object sender, Accord.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                frameCount++;
                WriteableBitmap bmp = new WriteableBitmap(eventArgs.Frame.ToBitmapSource());
                bmp.Freeze();
                updateFrameStats(bmp, (int)bmp.Width, (int)bmp.Height);
            } catch
            {

            }
            
        }

        private void SetToolPan(object sender, RoutedEventArgs e)
        {
            selectedTool = UIMode.Pan;
            btnPan.FontWeight = FontWeights.Bold;
            btnAnnotate.FontWeight = FontWeights.Normal;
        }

        private void SetToolAnnotate(object sender, RoutedEventArgs e)
        {
            selectedTool = UIMode.Scribble;
            btnPan.FontWeight = FontWeights.Normal;
            btnAnnotate.FontWeight = FontWeights.Bold;
        }

        private void ColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentBrush = new SolidColorBrush();
            currentBrush.Color = (Color)ColorConverter.ConvertFromString((e.AddedItems[0] as ComboBoxItem).Content.ToString());
        }

        private void ClearAnnotations(object sender, RoutedEventArgs e)
        {
            annotationCanvas.Children.Clear();
        }
    }
}
