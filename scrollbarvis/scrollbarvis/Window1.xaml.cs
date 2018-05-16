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
    public partial class Window1 : Window
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
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        Color[] allColors = { Colors.Red, Colors.DarkOrange, Colors.Gold, Colors.Green, Colors.Teal,
                               Colors.Blue, Colors.MediumAquamarine, Colors.Indigo, Colors.MediumPurple, Colors.Coral,
                               Colors.DeepPink, Colors.Chocolate, Colors.DarkOliveGreen, Colors.Magenta, Colors.YellowGreen};

        String recordingpath = "gazerecordings/r";

        String[] inputFile = { "gazerecordings/r0_0.csv",
                               "gazerecordings/r0_1.csv",
                               "gazerecordings/r0_2.csv",
                               "gazerecordings/r0_3.csv",
                               "gazerecordings/r0_4.csv",
                               "gazerecordings/r0_5.csv",
                               "gazerecordings/r0_6.csv",
                               "gazerecordings/r0_7.csv",};

        public Window1()
        {
            InitializeComponent();

            if (inputFile.Length > 0)
            {
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
                    /* Create a heatmap for each file, save to List of byte arrays */
                    px = createScreenHeatmap(xCoords[c], yCoords[c], c);
                    pixels3d.Add(px);
                }
            }
        }

        private void canvasloaded(object sender, RoutedEventArgs e)
        {
            double screenheight = this.ActualHeight - SystemParameters.WindowNonClientFrameThickness.Top - SystemParameters.WindowNonClientFrameThickness.Bottom;
            double screenwidth = this.ActualWidth - SystemParameters.WindowNonClientFrameThickness.Left - SystemParameters.WindowNonClientFrameThickness.Right;
            SolidColorBrush blankbg = new SolidColorBrush(Colors.LightGray);
            SolidColorBrush handle = new SolidColorBrush(Colors.Gray);

            #region 
            List<byte[,]> colors = new List<byte[,]>(3);
            colors.Add(new byte[,] { { 0, 0, 255 } });
            colors.Add(new byte[,] { { 255, 0, 0 } });
            colors.Add(new byte[,] { { 0, 255, 0 } });

            ImageBrush[] verticalheatmaps = new ImageBrush[inputFile.Length];
            List<double> freqs = new List<double>(inputFile.Length);
            if (inputFile.Length > 0)
            {
                for (int c = 0; c < 3/*inputFile.Length*/; c++)
                {
                    Tuple<int, WriteableBitmap> vert = createVerticalHeatmap(200, 2 * (int)screenheight, yCoords[c], numCoords[c], 4330, 2 * 13, colors[c], 55);
                    //Tuple<int, WriteableBitmap> vert = createMultiHeatmap(200, 2 * (int)screenheight, yCoords, numCoords, 4330, 2 * 13, colors, 55);
                    verticalheatmaps[c] = new ImageBrush(vert.Item2);
                    freqs.Add(vert.Item1);
                }

                double maxfreq = freqs.Max();
                for (int c = 0; c < 3/*inputFile.Length*/; c++)
                {
                    freqs[c] = freqs[c] / maxfreq;
                }
                #endregion
            }
            scrollbar = new Scrollbar(15, 150, screenheight, screenwidth, 0.9, 100, bg, blankbg, handle, verticalheatmaps, freqs, canv, 1, wb, heatmap, pixels3d, allColors);

            recorder = new Recorder(20, 5, 100, canv, recordingpath);

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

        public class Recorder
        {
            private StringBuilder csv;
            private String filepath;
            private bool recording;
            private int sessionnumber;
            private int recordnumber;

            private Rectangle button;
            private SolidColorBrush recordcolor, pausecolor;


            public Recorder(int x, int y, int z, Canvas canv, String pathstart)
            {
                csv = new StringBuilder();
                recording = false;
                sessionnumber = 0;
                recordnumber = 0;

                filepath = pathstart + sessionnumber.ToString() + "_" + recordnumber.ToString() + ".csv";
                while (File.Exists(filepath))
                {
                    sessionnumber++;
                    filepath = pathstart + sessionnumber.ToString() + "_" + recordnumber.ToString() + ".csv";
                }

                recordcolor = new SolidColorBrush(Colors.Red);
                pausecolor = new SolidColorBrush(Colors.Black);

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
                    button.Fill = recordcolor;
                else
                {
                    button.Fill = pausecolor;
                    save();

                    recordnumber++;
                    filepath = filepath.Substring(0, filepath.IndexOf("_") + 1) + recordnumber.ToString() + ".csv";
                    csv = new StringBuilder();
                }
            }
        }

        #region
        public class Scrollbar
        {
            private Rectangle handle, blankbg, picturebg, hover, bg;
            private Rectangle[] heatmapbgs;
            private double inwidth, outwidth, currwidth, scrheight, scrwidth, smooth;
            private int z, topz;
            private int gazetimer;
            private int persistance;
            public bool needsupdate;

            /* Heatmap */
            WriteableBitmap wb;
            Image heatmap;
            List<byte[,,]> pixels;
            Button[] heatmapButtons;
            bool[] heatmapShown;
            double bgTopPosition = 0;
            
            public Scrollbar(double collapsedwidth, double expandedwidth, double screenheight, double screenwidth, double smoothness, int duration,
                             Rectangle background, SolidColorBrush blank, SolidColorBrush hand, ImageBrush[] vertheatmaps, List<double> vertscale, Canvas canv, int zindex,
                             WriteableBitmap writeableBitmap, Image heatmapImage, List<byte[,,]> heatmapPixels, Color[] allColors)
            {
                inwidth = collapsedwidth;
                outwidth = expandedwidth;
                currwidth = inwidth;
                scrheight = screenheight;
                scrwidth = screenwidth;
                smooth = smoothness;
                persistance = duration;
                bg = background;
                z = zindex;
                gazetimer = 0;
                needsupdate = false;
                heatmapbgs = new Rectangle[vertheatmaps.Length];

                int zind = z;

                hover = new Rectangle();
                hover.Width = 3000;
                hover.Height = 3000;
                Panel.SetZIndex(hover, zind);
                hover.Fill = blank;
                hover.Opacity = 0;
                hover.PreviewMouseMove += mousemove;
                hover.PreviewMouseUp += mouseup;
                hover.PreviewMouseWheel += mousescroll;
                canv.Children.Add(hover);

                zind++;

                picturebg = new Rectangle();
                picturebg.Width = outwidth;
                picturebg.Height = scrheight;
                Canvas.SetRight(picturebg, inwidth - outwidth);
                Canvas.SetTop(picturebg, 0);
                Panel.SetZIndex(picturebg, zind);
                picturebg.Fill = background.Fill;
                canv.Children.Add(picturebg);

                zind++;

                //for (int i = 0; i < heatmapbgs.Length; i++)
                //{
                //    heatmapbgs[i] = new Rectangle();
                //    heatmapbgs[i].Width = outwidth * vertscale[i];
                //    heatmapbgs[i].Height = scrheight;
                //    Canvas.SetRight(heatmapbgs[i], inwidth - outwidth);
                //    Canvas.SetTop(heatmapbgs[i], 0);
                //    Panel.SetZIndex(heatmapbgs[i], zind);
                //    heatmapbgs[i].Fill = vertheatmaps[i];
                //    heatmapbgs[i].Opacity = 2 / (double)heatmapbgs.Length;
                //    canv.Children.Add(heatmapbgs[i]);

                //    zind++;
                //}

                blankbg = new Rectangle();
                blankbg.Width = inwidth;
                blankbg.Height = scrheight;
                Canvas.SetRight(blankbg, 0);
                Canvas.SetTop(blankbg, 0);
                Panel.SetZIndex(blankbg, zind);
                blankbg.Fill = blank;
                blankbg.PreviewMouseDown += mousedown;
                blankbg.PreviewMouseWheel += mousescroll;
                canv.Children.Add(blankbg);

                zind++;

                handle = new Rectangle();
                handle.Width = inwidth;
                handle.Height = scrheight / bg.Height * scrheight;
                Canvas.SetRight(handle, 0);
                Canvas.SetTop(handle, 0);
                Panel.SetZIndex(handle, zind);
                handle.Fill = hand;
                handle.IsHitTestVisible = false;
                canv.Children.Add(handle);

                topz = zind + 1;

                /* Screen Heatmap */
                wb = writeableBitmap;
                heatmap = heatmapImage;
                pixels = heatmapPixels;

                if (pixels != null)
                {
                    heatmapButtons = new Button[pixels.Count];
                    heatmapShown = new bool[pixels.Count];

                    /* Buttons setup */
                    for (int i = 0; i < pixels.Count; i++)
                    {
                        heatmapButtons[i] = new Button();
                        heatmapButtons[i].Height = 25;
                        heatmapButtons[i].Width = 50;
                        heatmapButtons[i].Name = "H_" + i.ToString();
                        canv.Children.Add(heatmapButtons[i]);
                        Canvas.SetTop(heatmapButtons[i], 10 + 30 * i);
                        Canvas.SetRight(heatmapButtons[i], outwidth + 5);
                        Panel.SetZIndex(heatmapButtons[i], 100);
                        heatmapButtons[i].Click += new RoutedEventHandler(HeatmapButton_Click);
                        hideHeatmap(i);
                        heatmapShown[i] = false;
                        heatmapButtons[i].Background = new SolidColorBrush(allColors[i]);
                    }
                }
            }

            /*
             * Show/Hide heatmap buttons handler.
             */
            private void HeatmapButton_Click(object sender, EventArgs e)
            {
                Button thisButton = sender as Button;
                int x = Int32.Parse((thisButton.Name).Substring(2));
                heatmapButtonHelper(x);
            }
            /*
             * Helper function for heatmap buttons (show/hide).
             */
            private void heatmapButtonHelper(int index)
            {
                heatmapShown[index] = !heatmapShown[index];
                if (heatmapShown[index])
                {
                    showHeatmap(index);
                }
                else
                {
                    hideHeatmap(index);
                }
            }
            /*
             * Show the heatmap at specified index.
             */
            private void showHeatmap(int index)
            {
                heatmapShown[index] = true;
                heatmapButtons[index].Content = "Hide";

                double y = -1 * bgTopPosition;
                setScreenHeatmap((int)(y < 0 ? 0 : y));
                heatmap.Visibility = Visibility.Visible;
            }
            /*
             * Hide the heatmap at specified index.
             */
            private void hideHeatmap(int index)
            {
                heatmapShown[index] = false;
                heatmapButtons[index].Content = "Show";

                double y = -1 * bgTopPosition;
                setScreenHeatmap((int)(y < 0 ? 0 : y));
            }

            public void checkGaze(Point p)
            {
                gazetimer--;
                if (scrwidth - p.X < outwidth || gazetimer > 0 && scrwidth - p.X < outwidth * 2 ||
                    blankbg.IsMouseOver || Panel.GetZIndex(hover) == topz)
                    gazetimer = persistance;
                needsupdate = needsupdate || gazetimer > 0;
            }

            public void update()
            {
                if (gazetimer > 0)
                {
                    currwidth = currwidth * smooth + outwidth * (1 - smooth);
                }
                else if (currwidth - inwidth < .01)
                {
                    currwidth = inwidth;
                    needsupdate = false;
                }
                else
                {
                    currwidth = currwidth * smooth + inwidth * (1 - smooth);
                }
                handle.Width = currwidth;
                blankbg.Width = currwidth;
                for (int i = 0; i < heatmapbgs.Length; i++)
                    Canvas.SetRight(heatmapbgs[i], currwidth - outwidth);
                Canvas.SetRight(picturebg, currwidth - outwidth);

                double inpercentage = (outwidth - currwidth) / (outwidth - inwidth);
                blankbg.Opacity = inpercentage;
                handle.Opacity = inpercentage + .15;
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
                if (Panel.GetZIndex(hover) == topz)
                {
                    /* Set Heatmap */
                    double y = -1 * bgTopPosition;
                    setScreenHeatmap((int)(y < 0 ? 0 : y));
                }
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
                /* Set Heatmap*/
                double y = -1 * bgTopPosition;
                setScreenHeatmap((int)(y < 0 ? 0 : y));
            }

            /*
            * Set bitmap for the portion of screen starting at Y position screenPositionTop
            */
            private void setScreenHeatmap(int screenPositionTop)
            {
                if (pixels==null)
                {
                    return;
                }
                // Check if any heatmaps are shown
                int numHeatmaps = 0;
                for (int h = 0; h < heatmapShown.Length; h++)
                    if (heatmapShown[h]) numHeatmaps++;

                int height = (int)scrheight;
                int width = (int)scrwidth;
                // Copy the data into a one-dimensional array.
                byte[] pixels1d = new byte[height * width * 4];
                int index = 0;
                for (int row = screenPositionTop; row < screenPositionTop + height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        double totalOpacity = 0.0001;
                        double maxOpacity = 0;
                        double opacity;
                        double b = 0; double g = 0; double r = 0;
                        for (int h = 0; h < heatmapShown.Length; h++)
                        {
                            if (heatmapShown[h])
                            {
                                opacity = pixels[h][col, row, 3];
                                totalOpacity += pixels[h][col, row, 3];
                                if (opacity > maxOpacity) maxOpacity = opacity;
                                if (opacity > 0)
                                {
                                    b = (b * (totalOpacity - opacity) + pixels[h][col, row, 0] * opacity) / totalOpacity;
                                    g = (g * (totalOpacity - opacity) + pixels[h][col, row, 1] * opacity) / totalOpacity;
                                    r = (r * (totalOpacity - opacity) + pixels[h][col, row, 2] * opacity) / totalOpacity;
                                }
                            }
                        }
                        pixels1d[index++] = (byte)b;
                        pixels1d[index++] = (byte)g;
                        pixels1d[index++] = (byte)r;
                        pixels1d[index++] = (byte)maxOpacity;
                    }
                }
                // Update writeable bitmap
                Int32Rect rect = new Int32Rect(0, 0, width, height);
                int stride = 4 * width;
                wb.WritePixels(rect, pixels1d, stride, 0);

                heatmap.Stretch = Stretch.None;
                heatmap.Margin = new Thickness(0);
                heatmap.Source = wb;
            }
        }
        #endregion

        #region
        public Tuple<int, WriteableBitmap> createVerticalHeatmap(int width, int height, List<int> yCoords, int numCoords, double maxY, int spread, byte[,] colors, int minalpha)
        {
            int[] frequencies = new int[height];
            int maxfrequency = 0;
            for (int i = 0; i < numCoords; i++)
            {
                for (int s = -spread; s <= spread; s++)
                {
                    int y = (int)(height * yCoords[i] / maxY + s);
                    if (y > 0 && y < frequencies.Length)
                    {
                        frequencies[y] += spread - Math.Abs(s);
                        maxfrequency = frequencies[y] > maxfrequency ? frequencies[y] : maxfrequency;
                    }
                }
            }
            byte[,,] pixels = new byte[width, height, 4];

            for (int y = 0; y < frequencies.Length; y++)
            {
                byte alpha = (byte)((255 - minalpha) * frequencies[y] / (double)maxfrequency + minalpha);
                double color = (colors.GetLength(0) - 1) * frequencies[y] / (double)maxfrequency;
                byte b, g, r;
                int colorlow = (int)color;
                int colorhigh = (int)Math.Ceiling(color);
                color -= colorlow;
                b = (byte)(colors[colorlow, 0] * (1 - color) + colors[colorhigh, 0] * color);
                g = (byte)(colors[colorlow, 1] * (1 - color) + colors[colorhigh, 1] * color);
                r = (byte)(colors[colorlow, 2] * (1 - color) + colors[colorhigh, 2] * color);
                int start = width - (int)Math.Floor(Math.Pow(frequencies[y] / (double)maxfrequency, 1.0 / 2.5) * width);
                for (int x = start; x < width; x++)
                {
                    byte a = (byte)((alpha - minalpha) * (1 - (x - start) / (double)(width - start)) + minalpha);
                    pixels[x, y, 0] = b;
                    pixels[x, y, 1] = g;
                    pixels[x, y, 2] = r;
                    pixels[x, y, 3] = a;
                }
            }
            WriteableBitmap bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = 4 * width;
            bmp.WritePixels(rect, flattenArray(pixels), stride, 0);
            return Tuple.Create(maxfrequency, bmp);
        }

        public Tuple<int, WriteableBitmap> createMultiHeatmap(int width, int height, List<int>[] yCoords, int[] numCoords, double maxY, int spread, List<byte[]> colors, int minalpha)
        {
            int numusers = numCoords.Length;

            int[,] frequencies = new int[numusers, height];
            for (int u = 0; u < numusers; u++)
            {
                for (int i = 0; i < numCoords[u]; i++)
                {
                    for (int s = -spread; s <= spread; s++)
                    {
                        int y = (int)(height * yCoords[u][i] / maxY + s);
                        if (y > 0 && y < height)
                        {
                            frequencies[u, y] += spread - Math.Abs(s);
                        }
                    }
                }
            }

            int maxfrequency = 0;
            for (int u = 0; u < numusers; u++)
            {
                for (int i = 0; i < numCoords[u]; i++)
                {
                    int y = (int)(height * yCoords[u][i] / maxY);
                    if (y > 0 && y < height)
                        maxfrequency = frequencies[u, y] > maxfrequency ? frequencies[u, y] : maxfrequency;
                }
            }

            byte[,,] pixels = new byte[width, height, 4];
            //for (int y = 0; y < height; y++) {
            //    byte[] alpha = new byte[numusers];
            //    int[] start = new int[numusers];
            //    for (int u = 0; u < numusers; u++) {
            //        alpha[u] = (byte)((255 - minalpha) * frequencies[u, y] / (double)maxfrequency + minalpha);
            //        start[u] = width - (int)Math.Floor(Math.Pow(frequencies[u, y] / (double)maxfrequency, 1.0 / 2.5) * width);
            //    }
            //    for (int x = 0; x < width; x++) {
            //        double pixelalpha = 0;
            //        byte[] a = new byte[numusers];
            //        for (int u = 0; u < numusers; u++){
            //            a[u] = (byte)((alpha[u] - minalpha) * (1 - (x - start[u]) / (double)(width - start[u])) + minalpha);
            //            if(x > start[u])
            //                pixelalpha += a[u];
            //        }
            //        bool vis = false;
            //        for(int c = 0; c < 3; c++) {
            //            for (int u = 0; u < numusers; u++) {
            //                if(x >= start[u]) {
            //                    pixels[x, y, c] += (byte)(colors[u][c]*(a[u]/pixelalpha));
            //                    vis = true;
            //                }
            //            }
            //        }
            //        if (vis)
            //            pixels[x, y, 3] = pixelalpha > 255 ? (byte)255 : (byte)pixelalpha;
            //        else
            //            pixels[x, y, 3] = 0;
            //    }
            //}

            for (int u = 0; u < numusers; u++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = (byte)((255 - minalpha) * frequencies[u, y] / (double)maxfrequency + minalpha);
                    int start = width - (int)Math.Floor(Math.Pow(frequencies[u, y] / (double)maxfrequency, 1.0 / 2.5) * width);
                    for (int x = start; x < width; x++)
                    {
                        byte a = (byte)((alpha - minalpha) * (1 - (x - start) / (double)(width - start)) + minalpha);
                        pixels[x, y, 0] += colors[u][0];
                        pixels[x, y, 1] += colors[u][1];
                        pixels[x, y, 2] += colors[u][2];
                        pixels[x, y, 3] += a;
                    }
                }
            }

            WriteableBitmap bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = 4 * width;
            bmp.WritePixels(rect, flattenArray(pixels), stride, 0);
            return Tuple.Create(maxfrequency, bmp);
        }

        public byte[] flattenArray(byte[,,] shaped)
        {
            byte[] flat = new byte[shaped.GetLength(0) *
                                   shaped.GetLength(1) *
                                   shaped.GetLength(2)];
            int ind = 0;
            for (int a = 0; a < shaped.GetLength(1); a++)
            {
                for (int b = 0; b < shaped.GetLength(0); b++)
                {
                    for (int c = 0; c < shaped.GetLength(2); c++)
                    {
                        flat[ind++] = shaped[b, a, c];
                    }
                }
            }
            return flat;
        }
        #endregion

        #region screen heatmap setup
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
         * Create a bitmap of heatmap pixels. Color based on frequency of gaze coordinates at the pixel, plus color surrounding pixels.
         */
        private byte[,,] createScreenHeatmap(List<int> xCor, List<int> yCor, int index)
        {
            int totalWidth = (int)bg.Width;
            int totalHeight = (int)bg.Height;
            int x, y;
            double distanceFromCenter, currA, a;
            int maxDistance = 80;
            double maxOpacity = 220;

            wb = new WriteableBitmap(totalWidth, totalHeight, 96, 96, PixelFormats.Bgra32, null);
            byte[,,] pixels = new byte[totalWidth, totalHeight, 4];

            // Get gaze coordinates, change pixel colors
            for (int i = 0; i < numCoords[index]; i++)
            {
                x = xCor[i]; y = yCor[i];
                for (int j = (x - maxDistance) < 0 ? 0 : (x - maxDistance); j < ((x + maxDistance) > totalWidth ? totalWidth : (x + maxDistance)); j = j + 3)
                {
                    for (int k = (y - maxDistance) < 0 ? 0 : (y - maxDistance); k < ((y + maxDistance) > totalHeight ? totalHeight : (y + maxDistance)); k = k + 3)
                    {
                        distanceFromCenter = Math.Sqrt((double)(Math.Pow(x - j, 2) + Math.Pow(y - k, 2)));
                        if (distanceFromCenter <= (double)maxDistance)
                        {
                            currA = pixels[j, k, 3];
                            a = currA + (1 - currA / maxOpacity) * 90 * Math.Pow(0.965, distanceFromCenter); // EXPONENTIAL DECAY
                            a = (a > maxOpacity ? maxOpacity : a);

                            Color currColor = allColors[index];
                            pixels[j, k, 0] = currColor.B;
                            pixels[j, k, 1] = currColor.G;
                            pixels[j, k, 2] = currColor.R;
                            pixels[j, k, 3] = (byte)a;
                        }
                    }
                }
            }
            return pixels;
        }
        #endregion

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            String answers = Answer1.Text + "_" + Answer2.Text + "_" + Answer3.Text + "_" + Answer4.Text 
                + Answer5.Text + "_" + Answer6.Text + "_" + Answer7.Text + "_" + Answer8.Text + "_" 
                + Answer9.Text + "_" + Answer10.Text;
            /* Do something with answer */
        }
    }
}
