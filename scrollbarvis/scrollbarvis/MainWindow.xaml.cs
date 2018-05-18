using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using EyeXFramework.Wpf;
using Tobii.EyeX.Framework;
using EyeXFramework;

namespace scrollbarvis
{
    public partial class MainWindow : Window
    {
        EyeXHost eyeXHost;
        Point track = new Point(0, 0);

        List<int>[] xCoords, yCoords;
        int[] numCoords;

        double smoothness = .8;
        Point[] prevpoints;

        WriteableBitmap wb;
        List<byte[,,]> pixels3d;

        Scrollbar scrollbar;
        Recorder recorder;

        Pointvis pointvis;
        Meanvis meanvis;

        String recordingpath = "gazerecordings/r";

        System.Windows.Threading.DispatcherTimer dispatcherTimer;

        String[] inputFile = { "gazerecordings/r0_0.csv",
                               "gazerecordings/r0_1.csv",
                               "gazerecordings/r0_2.csv",
                               "gazerecordings/r0_3.csv",
                               "gazerecordings/r0_4.csv",
                               "gazerecordings/r0_5.csv",
                               "gazerecordings/r0_6.csv",
                               "gazerecordings/r0_7.csv",
                               "gazerecordings/r0_8.csv",
                               "gazerecordings/r0_9.csv",
                               "gazerecordings/r0_10.csv",
                               "gazerecordings/r0_11.csv",
                               "gazerecordings/r0_12.csv",
                               "gazerecordings/r0_13.csv",
                               "gazerecordings/r0_14.csv",};

        /* playback */
        System.Windows.Threading.DispatcherTimer animateTimer;
        int coordNum;
        Ellipse[] ellipses;
        bool isAnimating = false;
        bool isAnimatingPaused = false;

        public MainWindow()
        {
            InitializeComponent();

            prevpoints = new Point[inputFile.Length];
            
            xCoords = new List<int>[inputFile.Length];
            yCoords = new List<int>[inputFile.Length];
            numCoords = new int[inputFile.Length];
            List<int>[] points;
            byte[,,] px;
            pixels3d = new List<byte[,,]>(inputFile.Length);
            for (int c = 0; c < inputFile.Length; c++)
           {
                points = makeHeatmap(inputFile[c], c);
                xCoords[c] = points[0];
                yCoords[c] = points[1];
            }
        }

        private void canvasloaded(object sender, RoutedEventArgs e)
        {
            double screenheight = this.ActualHeight - SystemParameters.WindowNonClientFrameThickness.Top - SystemParameters.WindowNonClientFrameThickness.Bottom;
            double screenwidth = this.ActualWidth - SystemParameters.WindowNonClientFrameThickness.Left - SystemParameters.WindowNonClientFrameThickness.Right;
            SolidColorBrush blankbg = new SolidColorBrush(Colors.LightGray);
            SolidColorBrush handle = new SolidColorBrush(Colors.Gray);

            #region 
            //List<byte[,]> colors = new List<byte[,]>(3);
            //colors.Add(new byte[,] { { 255, 0, 0 } });
            //colors.Add(new byte[,] { { 0, 255, 0 } });
            //colors.Add(new byte[,] { { 0, 0, 255 } });

            //List<byte[]> colors = new List<byte[]>(3);
            //colors.Add(new byte[] { 255, 0, 0 });
            //colors.Add(new byte[] { 0, 255, 0 });
            //colors.Add(new byte[] { 0, 0, 255 });

            //ImageBrush[] verticalheatmaps = new ImageBrush[inputFile.Length];
            //List<double> freqs = new List<double>(inputFile.Length);
            //for(int c = 0; c < inputFile.Length; c++) {
            //    Tuple<int, WriteableBitmap> vert = createVerticalHeatmap(200, 2 * (int)screenheight, yCoords[c], numCoords[c], 4330, 2 * 13, colors[c], 55);
            //    //Tuple<int, WriteableBitmap> vert = createMultiHeatmap(200, 2 * (int)screenheight, yCoords, numCoords, 4330, 2 * 13, colors, 55);
            //    verticalheatmaps[c] = new ImageBrush(vert.Item2);
            //    freqs.Add(vert.Item1);
            //}

            //double maxfreq = freqs.Max();
            //for (int c = 0; c < inputFile.Length; c++) {
            //    freqs[c] = freqs[c] / maxfreq;
            //}
            #endregion

            scrollbar = new Scrollbar(15, screenheight, screenwidth, bg, blankbg, handle, canv, 1);
            recorder = new Recorder(20, 5, 100, canv, recordingpath, cloudLecture);

            Color[] colors = { Colors.Red, Colors.DarkOrange, Colors.Gold, Colors.Green, Colors.Teal,
                               Colors.Blue, Colors.MediumAquamarine, Colors.Indigo, Colors.MediumPurple, Colors.Coral,
                               Colors.DeepPink, Colors.Chocolate, Colors.DarkOliveGreen, Colors.Magenta, Colors.YellowGreen};

            pointvis = new Pointvis(inputFile.Length, 35, colors, canv);
            meanvis = new Meanvis(inputFile.Length, 20, canv);

            eyeXHost = new EyeXHost();
            eyeXHost.Start();
            var gazeData = eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            gazeData.Next += newGazePoint;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Start();
        }

        private void newGazePoint(object s, EyeXFramework.GazePointEventArgs e)
        {
            track.X = e.X;
            track.Y = e.Y;
        }

        private void update(object sender, EventArgs e)
        {
            Point currentgaze = PointFromScreen(track);
            currentgaze.Y -= Canvas.GetTop(bg);

            recorder.newpoint(currentgaze);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dispatcherTimer != null) dispatcherTimer.Stop();
            eyeXHost.Dispose();
            recorder.save();
        }

        public class Scrollbar
        {
            private Rectangle handle, blankbg, hover, bg;
            private double inwidth, currwidth, scrheight, scrwidth;
            private int z, topz;

            public double bgTopPosition = 0;

            public Scrollbar(double collapsedwidth, double screenheight, double screenwidth, Rectangle background,
                             SolidColorBrush blank, SolidColorBrush hand, Canvas canv, int zindex)
            {
                inwidth = collapsedwidth;
                currwidth = inwidth;
                scrheight = screenheight;
                scrwidth = screenwidth;
                bg = background;
                z = zindex;

                int zind = z;

                #region scroll setup
                //hover = new Rectangle();
                //hover.Width = 3000;
                //hover.Height = 3000;
                //Panel.SetZIndex(hover, zind);
                //hover.Fill = blank;
                //hover.Opacity = 0;
                //hover.PreviewMouseMove += mousemove;
                //hover.PreviewMouseUp += mouseup;
                //hover.PreviewMouseWheel += mousescroll;
                //canv.Children.Add(hover);

                //zind++;

                //blankbg = new Rectangle();
                //blankbg.Width = inwidth;
                //blankbg.Height = scrheight;
                //Canvas.SetRight(blankbg, 0);
                //Canvas.SetTop(blankbg, 0);
                //Panel.SetZIndex(blankbg, zind);
                //blankbg.Fill = blank;
                //blankbg.PreviewMouseDown += mousedown;
                //blankbg.PreviewMouseWheel += mousescroll;
                //canv.Children.Add(blankbg);

                //zind++;

                //handle = new Rectangle();
                //handle.Width = inwidth;
                //handle.Height = scrheight / bg.Height * scrheight;
                //Canvas.SetRight(handle, 0);
                //Canvas.SetTop(handle, 0);
                //Panel.SetZIndex(handle, zind);
                //handle.Fill = hand;
                //handle.IsHitTestVisible = false;
                //canv.Children.Add(handle);

                //topz = zind + 1;
                #endregion
            }

            private void mousedown(object sender, MouseButtonEventArgs e)
            {
                Panel.SetZIndex(hover, topz);
            }
            private void mousemove(object sender, MouseEventArgs e)
            {
                if (Panel.GetZIndex(hover) == topz)
                {
                    double handley = e.GetPosition(hover).Y - handle.Height / 2;
                    handley = handley > 0 ? handley : 0;
                    handley = handley + handle.Height < scrheight ? handley : scrheight - handle.Height;
                    Canvas.SetTop(handle, handley);
                    bgTopPosition = -handley * (bg.Height / scrheight);
                    Canvas.SetTop(bg, bgTopPosition);
                }
            }
            private void mouseup(object sender, MouseEventArgs e)
            {
                Panel.SetZIndex(hover, z);
            }
            private void mousescroll(object sender, MouseWheelEventArgs e)
            {
                double handley = Canvas.GetTop(handle) - e.Delta / (.001 * bg.Height);
                handley = handley > 0 ? handley : 0;
                handley = handley + handle.Height < scrheight ? handley : scrheight - handle.Height;
                Canvas.SetTop(handle, handley);
                bgTopPosition = -handley * (bg.Height / scrheight);
                Canvas.SetTop(bg, bgTopPosition);
            }
        }

        public class Recorder
        {
            private StringBuilder csv;
            private String filepath;
            private bool recording;
            private int sessionnumber;
            private int recordnumber;

            private Rectangle button;
            private SolidColorBrush recordcolor, pausecolor;

            private MediaElement myVideo;


            public Recorder(int x, int y, int z, Canvas canv, String pathstart, MediaElement video)
            {
                csv = new StringBuilder();
                recording = false;
                sessionnumber = 0;
                recordnumber = 0;
                myVideo = video;

                filepath = pathstart + sessionnumber.ToString() + "_" + recordnumber.ToString() + ".csv";
                while (File.Exists(filepath))
                {
                    sessionnumber++;
                    filepath = pathstart + sessionnumber.ToString() + "_" + recordnumber.ToString() + ".csv";
                }

                recordcolor = new SolidColorBrush(Colors.Red);
                pausecolor = new SolidColorBrush(Colors.Gray);

                button = new Rectangle();
                button.Width = 30;
                button.Height = 30;
                Canvas.SetRight(button, x);
                Canvas.SetTop(button, y);
                Panel.SetZIndex(button, z);
                button.RadiusX = 7;
                button.RadiusY = 7;
                button.Fill = pausecolor;
                button.PreviewMouseUp += buttonclick;
                canv.Children.Add(button);
            }

            public void newpoint(Point p)
            {
                if (recording)
                {
                    String line = string.Format("{0},{1}", (int)p.X, (int)p.Y);
                    csv.AppendLine(line);
                }
            }

            public void save()
            {
                if (csv.Length != 0)
                    File.WriteAllText(filepath, csv.ToString());
            }

            private void buttonclick(object sender, MouseButtonEventArgs e)
            {
                recording = !recording;
                if (recording)
                {
                    myVideo.Play();
                    button.Fill = recordcolor;
                }
                else
                {
                    myVideo.Pause();
                    button.Fill = pausecolor;
                    save();

                    recordnumber++;
                    filepath = filepath.Substring(0, filepath.IndexOf("_") + 1) + recordnumber.ToString() + ".csv";
                    csv = new StringBuilder();
                }
            }
        }

        public class Meanvis {
            private Ellipse meandot;
            private double radius;
            private int numpoints;

            private Point currmean;
            private double currpoints;

            public Meanvis(int np,  double r, Canvas canv) {
                meandot = new Ellipse();
                meandot.Width = 2 * r;
                meandot.Height = 2 * r;
                meandot.Visibility = Visibility.Hidden;
                meandot.Fill = new SolidColorBrush(Colors.Black);
                canv.Children.Add(meandot);

                numpoints = np;
                radius = r;

                currmean = new Point(0, 0);
                currpoints = 0;
            }

            public void addpoint(double x, double y) {
                currmean.X += x;
                currmean.Y += y;
                currpoints++;
            }

            public void update() {
                currmean.X /= currpoints;
                currmean.Y /= currpoints;

                Canvas.SetLeft(meandot, currmean.X - radius);
                Canvas.SetTop(meandot, currmean.Y - radius);
                currmean = new Point(0, 0);
                currpoints = 0;
            }

            public void show() {
                meandot.Visibility = Visibility.Visible;
            }

            public void hide() {
                meandot.Visibility = Visibility.Hidden;
            }
        }

        public class Pointvis {
            private Ellipse[] ellipses;
            private double radius;
            private int numpoints;

            private Point[] currset;

            public Pointvis(int np, double r, Color[] colors, Canvas canv){
                ellipses = new Ellipse[np];

                for (int i = 0; i < np; i++){
                    ellipses[i] = new Ellipse();
                    ellipses[i].Width = 2 * r;
                    ellipses[i].Height = 2 * r;
                    ellipses[i].Visibility = Visibility.Hidden;
                    ellipses[i].StrokeThickness = 2.0;
                    ellipses[i].Stroke = new SolidColorBrush(colors[i]);
                    canv.Children.Add(ellipses[i]);
                }

                numpoints = np;
                radius = r;

                currset = new Point[np];
                for (int i = 0; i < np; i++)
                    currset[i] = new Point(-2*radius, -2*radius);
            }

            public void addpoint(int ind, double x, double y) {
                currset[ind] = new Point(x, y);
            }

            public void update() {
                for (int i = 0; i < currset.Length; i++) {
                    Canvas.SetLeft(ellipses[i], currset[i].X);
                    Canvas.SetTop(ellipses[i], currset[i].Y);
                    currset[i] = new Point(-2*radius, -2*radius);
                }
            }

            public void show() {
                for (int i = 0; i < numpoints; i++) {
                    ellipses[i].Visibility = Visibility.Visible;
                }
            }

            public void hide() {
                for (int i = 0; i < numpoints; i++) {
                    ellipses[i].Visibility = Visibility.Hidden;
                }
            }
        }

        #region playback
        private void PlaybackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isAnimating)
            {
                isAnimatingPaused = true;
                coordNum = (int)(PlaybackSlider.Value / PlaybackSlider.Maximum * numCoords.Max());
                isAnimatingPaused = false; // start playing again
                AnimatePlay.Visibility = Visibility.Hidden;
                AnimatePause.Visibility = Visibility.Visible;
            }
        }

        private void startAnimate()
        {
            coordNum = 0;
            animateTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            animateTimer.Tick += animate_tick;
            animateTimer.Interval = new TimeSpan(0, 0, 0, 0, 30); //days,hrs,min,sec,ms
            animateTimer.Start();
        }

        private void stopAnimate()
        {
            pointvis.hide();
            meanvis.hide();
            isAnimating = false;
            PlaybackSlider.Value = 0;
            if (animateTimer!=null) animateTimer.Stop();
            AnimatePlay.Visibility = Visibility.Visible;
            AnimatePause.Visibility = Visibility.Hidden;
        }
        private void animate_tick(object sender, EventArgs e)
        {
            if (coordNum == numCoords[0])
            {
                isAnimating = false;
                animateTimer.Stop();
            }
            if (!isAnimatingPaused)
            {
                animate(coordNum, 0);
                coordNum++;
                PlaybackSlider.Value = (int)((double)coordNum / numCoords.Max() * PlaybackSlider.Maximum); // update slider
            }
        }

        private void animate(int coordNum, int type)
        {
            // For all students to display
            switch (type)
            {
                default:
                    drawNextCoordinate(coordNum);
                    break;
            }
        }

        private void Animate_Click(object sender, RoutedEventArgs e)
        {
            pointvis.show();
            meanvis.show();
            if (!isAnimating)
            {
                // start
                isAnimating = true;
                isAnimatingPaused = false;
                startAnimate();
                AnimatePlay.Visibility = Visibility.Hidden;
                AnimatePause.Visibility = Visibility.Visible;
            }
            else
            {
                // pause/resume
                isAnimatingPaused = !isAnimatingPaused;
                if (AnimatePlay.Visibility == Visibility.Visible)
                {
                    AnimatePlay.Visibility = Visibility.Hidden;
                    AnimatePause.Visibility = Visibility.Visible;
                }
                else
                {
                    AnimatePlay.Visibility = Visibility.Visible;
                    AnimatePause.Visibility = Visibility.Hidden;
                }
            }
        }

        private void Clear_Animate_Click(object sender, RoutedEventArgs e)
        {
            stopAnimate();
        }

        public void drawNextCoordinate(int coordNum)
        {
            double y = -1 * this.scrollbar.bgTopPosition;
            int screenPositionTop = (int)(y < 0 ? 0 : y);
            for (int i = 0; i < inputFile.Length; i++)
            {
                if (coordNum < xCoords[i].Count && coordNum < yCoords[i].Count)
                {
                    prevpoints[i].X = prevpoints[i].X * smoothness + xCoords[i][coordNum] * (1 - smoothness);
                    prevpoints[i].Y = prevpoints[i].Y * smoothness + (yCoords[i][coordNum] - screenPositionTop) * (1 - smoothness);

                    pointvis.addpoint(i, prevpoints[i].X, prevpoints[i].Y);
                    meanvis.addpoint(prevpoints[i].X, prevpoints[i].Y);
                }
            }
            pointvis.update();
            meanvis.update();
        }
        #endregion

        /*
         * Make a heatmap from existing gaze coordinate data from a previous session. Fill in arrays xCoord and yCoord.
         */
        private List<int>[] makeHeatmap(String input, int index)
        {
            List<int>[] coords = new List<int>[2];
            // Read in data
            using (var reader = new StreamReader(input))
            {
                reader.ReadLine(); // Read header line
                coords[0] = new List<int>();
                coords[1] = new List<int>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    coords[0].Add(int.Parse(values[0]));
                    coords[1].Add(int.Parse(values[1]));
                }
                numCoords[index] = coords[0].Count <= coords[1].Count ? coords[0].Count : coords[1].Count;
            }
            return coords;
        }

        /*
         * Open other window for heatmap viz (static)
         */
        private void HeatmapViz_Button_Click(object sender, RoutedEventArgs e)
        {
            Window1 window1 = new Window1();
            stopAnimate();
            window1.Show();
            //this.Close();
        }
    }
}
