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
       WriteableBitmap wb;

        Scrollbar scrollbar;

        StringBuilder csv = new StringBuilder();
        String filePath;
        String pathStart = "gazerecordings/recording";
        bool recorded = false;

        String[] inputFile = { "gazerecordings/recording0.csv", "gazerecordings/recording1-test.csv" };

        public MainWindow()
        {
            InitializeComponent();

            int offset = 0;
            filePath = pathStart + offset.ToString() + ".csv";
            while (File.Exists(filePath)) {
                offset++;
                filePath = pathStart + offset.ToString() + ".csv";
            }
        }

        private void canvasloaded(object sender, RoutedEventArgs e)
        {
            double screenheight = this.ActualHeight - SystemParameters.WindowNonClientFrameThickness.Top - SystemParameters.WindowNonClientFrameThickness.Bottom;
            double screenwidth = this.ActualWidth - SystemParameters.WindowNonClientFrameThickness.Left - SystemParameters.WindowNonClientFrameThickness.Right;
            SolidColorBrush blankbg = new SolidColorBrush(Colors.LightGray);
            SolidColorBrush handle = new SolidColorBrush(Colors.Gray);

            xCoords = new List<int>[inputFile.Length];
            yCoords = new List<int>[inputFile.Length];
            numCoords = new int[inputFile.Length];
            List<int>[] points;
            byte[,,] px;
            List<byte[,,]> pixels3d = new List<byte[,,]>(inputFile.Length);
            for (int c = 0; c < inputFile.Length; c++) {
                points = makeHeatmap(inputFile[c],c);
                xCoords[c] = points[0];
                yCoords[c] = points[1];
                /* Create a heatmap for each file, save to List of byte arrays */
                px = createScreenHeatmap(xCoords[c], yCoords[c], c);
                pixels3d.Add(px);
            }

            byte[,] colors = new byte[,] { { 255, 0, 0 } };

            ImageBrush vertheatmap = new ImageBrush(createVerticalHeatmap(150, (int)screenheight, yCoords[0], numCoords[0], 4330, 5, colors));

            scrollbar = new Scrollbar(15, 150, screenheight, screenwidth, 0.9, 100, bg, blankbg, handle, vertheatmap, canv, 1, wb, heatmap, pixels3d);

            eyeXHost = new EyeXHost();
            eyeXHost.Start();
            var gazeData = eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            gazeData.Next += newGazePoint;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
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
            Point currentGaze = PointFromScreen(track);
            currentGaze.Y -= Canvas.GetTop(bg);

            scrollbar.checkGaze(currentGaze);
            if (scrollbar.needsupdate)
                scrollbar.update();

            recordGazePoint(currentGaze);
        }

        public class Scrollbar {
            private Rectangle handle, blankbg, heatmapbg, picturebg, hover, bg;
            private double inwidth, outwidth, currwidth, scrheight, scrwidth, smooth;
            private int z;
            private int gazetimer;
            private int persistance;
            public bool needsupdate;

            /* Heatmap */
            WriteableBitmap wb;
            Image heatmap;
            List<byte[,,]> pixels;
            //bool heatmapShown = false; /* Show or Hide Heatmap!*/
            Button[] heatmapButtons;
            bool[] heatmapShown;
            double bgTopPosition = 0;


            public Scrollbar(double collapsedwidth, double expandedwidth, double screenheight, double screenwidth, double smoothness, int duration,
                             Rectangle background, SolidColorBrush blank, SolidColorBrush hand, ImageBrush vertheatmap, Canvas canv, int zindex,
                             WriteableBitmap writeableBitmap, Image heatmapImage, List<byte[,,]> heatmapPixels) {
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

                handle = new Rectangle();
                handle.Width = inwidth;
                handle.Height = scrheight / bg.Height * scrheight;
                Canvas.SetRight(handle, 0);
                Canvas.SetTop(handle, 0);
                Panel.SetZIndex(handle, z + 4);
                handle.Fill = hand;
                handle.IsHitTestVisible = false;
                canv.Children.Add(handle);

                blankbg = new Rectangle();
                blankbg.Width = inwidth;
                blankbg.Height = scrheight;
                Canvas.SetRight(blankbg, 0);
                Canvas.SetTop(blankbg, 0);
                Panel.SetZIndex(blankbg, z + 3);
                blankbg.Fill = blank;
                blankbg.PreviewMouseDown += mousedown;
                blankbg.PreviewMouseWheel += mousescroll;
                canv.Children.Add(blankbg);

                heatmapbg = new Rectangle();
                heatmapbg.Width = inwidth;
                heatmapbg.Height = scrheight;
                Canvas.SetRight(heatmapbg, 0);
                Canvas.SetTop(heatmapbg, 0);
                Panel.SetZIndex(heatmapbg, z + 2);
                heatmapbg.Fill = vertheatmap;
                canv.Children.Add(heatmapbg);

                picturebg = new Rectangle();
                picturebg.Width = inwidth;
                picturebg.Height = scrheight;
                Canvas.SetRight(picturebg, 0);
                Canvas.SetTop(picturebg, 0);
                Panel.SetZIndex(picturebg, z + 1);
                picturebg.Fill = background.Fill;
                canv.Children.Add(picturebg);

                hover = new Rectangle();
                hover.Width = 3000;
                hover.Height = 3000;
                Panel.SetZIndex(hover, z);
                hover.Fill = blank;
                hover.Opacity = 0;
                hover.PreviewMouseMove += mousemove;
                hover.PreviewMouseUp += mouseup;
                hover.PreviewMouseWheel += mousescroll;
                canv.Children.Add(hover);

                /* Screen Heatmap */
                wb = writeableBitmap;
                heatmap = heatmapImage;
                pixels = heatmapPixels;
                heatmapButtons = new Button[pixels.Count];
                heatmapShown = new bool[pixels.Count];

                /* Buttons setup */
                for (int i = 0; i < pixels.Count; i++)
                {
                    heatmapButtons[i] = new Button();
                    heatmapButtons[i].Height = 40;
                    heatmapButtons[i].Width = 100;
                    heatmapButtons[i].Name = "HeatmapButton" + i.ToString();
                    canv.Children.Add(heatmapButtons[i]);
                    Canvas.SetTop(heatmapButtons[i], 10 + 50*i);
                    Canvas.SetRight(heatmapButtons[i], outwidth + 10);
                    Panel.SetZIndex(heatmapButtons[i], 100);
                    string handlerName = heatmapButtons[i].Name + "_Click";
                    heatmapButtons[i].Click += new RoutedEventHandler(HeatmapButton_Click);
                    hideHeatmap(i);
                    heatmapShown[i] = false;
                    switch (i)
                    {
                        case 0:
                            heatmapButtons[i].Background = new SolidColorBrush(Colors.Blue);
                            break;
                        case 1:
                            heatmapButtons[i].Background = new SolidColorBrush(Colors.Green);
                            break;
                        case 2:
                            heatmapButtons[i].Background = new SolidColorBrush(Colors.Red);
                            break;
                    }
                }
            }

            /*
             * Show/Hide heatmap buttons handler.
             */
            private void HeatmapButton_Click(object sender, EventArgs e) {
                Button thisButton = sender as Button;
                switch (thisButton.Name)
                {
                    case "HeatmapButton0":
                        heatmapButtonHelper(0);
                        break;
                    case "HeatmapButton1":
                        heatmapButtonHelper(1);
                        break;
                    case "HeatmapButton2":
                        heatmapButtonHelper(2);
                        break;
                }
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
                heatmapButtons[index].Content = "Hide heatmap";

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
                heatmapButtons[index].Content = "Show heatmap";

                double y = -1 * bgTopPosition;
                setScreenHeatmap((int)(y < 0 ? 0 : y));
            }

            public void checkGaze(Point p) {
                gazetimer--;
                if (scrwidth - p.X < inwidth * 2 ||
                    gazetimer > 0 && scrwidth - p.X < outwidth * 2 ||
                    blankbg.IsMouseOver ||
                    Panel.GetZIndex(hover) == z + 5)
                    gazetimer = persistance;
                needsupdate = needsupdate || gazetimer > 0;
            }

            public void update() {
                if (gazetimer > 0) {
                    currwidth = currwidth * smooth + outwidth * (1 - smooth);
                }
                else if (currwidth - inwidth < .01) {
                    currwidth = inwidth;
                    needsupdate = false;
                }
                else {
                    currwidth = currwidth * smooth + inwidth * (1 - smooth);
                }
                handle.Width = currwidth;
                blankbg.Width = currwidth;
                heatmapbg.Width = currwidth;
                picturebg.Width = currwidth;

                double inpercentage = (outwidth - currwidth) / (outwidth - inwidth);
                blankbg.Opacity = inpercentage;
                handle.Opacity = inpercentage + .25;
            }

            private void mousedown(object sender, MouseButtonEventArgs e) {
                Panel.SetZIndex(hover, z + 5);
            }
            private void mousemove(object sender, MouseEventArgs e) {
                if (Panel.GetZIndex(hover) == z + 5) {
                    double handley = e.GetPosition(hover).Y - handle.Height / 2;
                    handley = handley > 0 ? handley : 0;
                    handley = handley + handle.Height < scrheight ? handley : scrheight - handle.Height;
                    Canvas.SetTop(handle, handley);
                    bgTopPosition = -handley * (bg.Height / scrheight);
                    Canvas.SetTop(bg, bgTopPosition);
                }
            }
            private void mouseup(object sender, MouseEventArgs e) {
                if (Panel.GetZIndex(hover) == z + 5)
                {
                    /* Set Heatmap */
                    double y = -1 * bgTopPosition;
                    setScreenHeatmap((int)(y < 0 ? 0 : y));
                }
                Panel.SetZIndex(hover, z);
            }

            private void mousescroll(object sender, MouseWheelEventArgs e) {
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
                // Check if any heatmaps are shown
                int numHeatmaps = 0;
                for (int h=0; h<heatmapShown.Length; h++)
                    if (heatmapShown[h]) numHeatmaps++;

                int height = (int)scrheight;
                int width = (int)scrwidth;
                // Copy the data into a one-dimensional array.
                byte[] pixels1d = new byte[height * width * 4];
                int index = 0;
                int currVal, newVal;
                for (int row = screenPositionTop; row < screenPositionTop+height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        double totalOpacity = 0.0001;
                        double maxOpacity = 0;
                        double opacity;
                        for (int h = 0; h < heatmapShown.Length; h++)
                        {
                            if (heatmapShown[h])
                            {
                                opacity = pixels[h][col, row, 3];
                                totalOpacity += pixels[h][col, row, 3];
                                if (opacity > maxOpacity) maxOpacity = opacity;
                            }
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            if (i==3)
                            {
                                pixels1d[index] = (byte)maxOpacity;
                            }
                            else if (i < heatmapShown.Length && heatmapShown[i])
                            {
                                pixels1d[index] = (byte)(255 * pixels[i][col, row, 3] / totalOpacity);
                            } else
                            {
                                pixels1d[index] = 0;
                            }
                            index++;
                        }
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

        public WriteableBitmap createVerticalHeatmap(int width, int height, List<int> yCoords, int numCoords, double maxY, int spread, byte[,] colors) {
            int[] frequencies = new int[height];
            int maxfrequency = 0;
            for (int i = 0; i < numCoords; i++) {
                for (int s = -spread; s <= spread; s++) {
                    int y = (int)(height * yCoords[i]/maxY + s);
                    if(y > 0 && y < frequencies.Length) { 
                        frequencies[y] += spread - Math.Abs(s);
                        maxfrequency = frequencies[y] > maxfrequency ? frequencies[y] : maxfrequency;
                    }
                }
            }
            byte[,,] pixels = new byte[width, height, 4];
            
            for (int y = 0; y < frequencies.Length; y++) {
                byte alpha = (byte)(255 * frequencies[y] / (double)maxfrequency);
                double color = (colors.GetLength(0) - 1) * frequencies[y] / (double)maxfrequency;
                byte b, g, r;
                int colorlow = (int)color;
                int colorhigh = (int)Math.Ceiling(color);
                color -= colorlow;
                b = (byte)(colors[colorlow, 0] * (1 - color) + colors[colorhigh, 0] * color);
                g = (byte)(colors[colorlow, 1] * (1 - color) + colors[colorhigh, 1] * color);
                r = (byte)(colors[colorlow, 2] * (1 - color) + colors[colorhigh, 2] * color);
                for (int x = 0; x < width; x++) {
                    pixels[x, y, 0] = b;
                    pixels[x, y, 1] = g;
                    pixels[x, y, 2] = r;
                    pixels[x, y, 3] = alpha;
                }
            }

            WriteableBitmap bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = 4 * width;
            bmp.WritePixels(rect, flattenArray(pixels), stride, 0);
            return bmp;
        }

        public byte[] flattenArray(byte[,,] shaped) {
            byte[] flat = new byte[shaped.GetLength(0) *
                                   shaped.GetLength(1) *
                                   shaped.GetLength(2)];
            int ind = 0;
            for (int a = 0; a < shaped.GetLength(1); a++){
                for (int b = 0; b < shaped.GetLength(0); b++){
                    for (int c = 0; c < shaped.GetLength(2); c++){
                        flat[ind++] = shaped[b, a, c];
                    }
                }
            }
            return flat;
        }

        public void recordGazePoint(Point p) {
            String line = string.Format("{0},{1}", (int)p.X, (int)p.Y);
            csv.AppendLine(line);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e){
            if(recorded)
                File.WriteAllText(filePath, csv.ToString());
        }

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
            wb = new WriteableBitmap(totalWidth, totalHeight, 96, 96, PixelFormats.Bgra32, null);
            byte[,,] pixels = new byte[totalWidth, totalHeight, 4];

            // Clear to black and transparent
            for (int row = 0; row < totalHeight; row++)
            {
                for (int col = 0; col < totalWidth; col++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        pixels[col, row, i] = 0;
                    }
                }
            }
            // Get gaze coordinates, change pixel colors
            for (int i = 0; i < numCoords[index]; i++)
            {
                int x = xCor[i];
                int y = yCor[i];
                double distanceFromCenter, distanceRatio, currA, b, g, r, a, mainColor, secondColor, maxSecondColor;
                int maxDistance = 100;
                int maxOpacity = 200;
                for (int j = (x - maxDistance) < 0 ? 0 : (x - maxDistance); j < ((x + maxDistance) > totalWidth ? totalWidth : (x + maxDistance)); j++)
                {
                    for (int k = (y - maxDistance) < 0 ? 0 : (y - maxDistance); k < ((y + maxDistance) > totalHeight ? totalHeight : (y + maxDistance)); k++)
                    {
                        distanceFromCenter = Math.Sqrt((double)(Math.Pow(x - j,2) + Math.Pow(y - k,2)));
                        if (distanceFromCenter <= (double)maxDistance)
                        {
                            currA = pixels[j, k, 3];
                            distanceRatio = distanceFromCenter / maxDistance;
                            a = currA + 10 * (1 - currA/255) * (1 - distanceRatio); // Add less opacity to current value if farther from gaze coordinate
                            a = (a > maxOpacity ? maxOpacity : a);

                            maxSecondColor = 60; b = 0; g = 0; r = 0;
                            mainColor = 255; //(255 * (a / maxOpacity));
                            secondColor = maxSecondColor * Math.Pow((1 - a/maxOpacity),2);
                            secondColor = (secondColor > maxSecondColor) ? maxSecondColor : secondColor;
                            switch (index)
                            {
                                case 0: // Blue
                                    b = mainColor;
                                    g = secondColor;
                                    r = secondColor;
                                    break;
                               case 1:  // Green
                                    g = mainColor;
                                    r = secondColor;
                                    b = secondColor;
                                    break;
                                default: // Red
                                    r = mainColor;
                                    b = secondColor;
                                    g = secondColor;
                                    break;
                            }
                            pixels[j, k, 0] = (byte)b;
                            pixels[j, k, 1] = (byte)g;
                            pixels[j, k, 2] = (byte)r;
                            pixels[j, k, 3] = (byte)a;
                        }
                    }
                }
            }
            return pixels;
        }
        #endregion
    }
}
