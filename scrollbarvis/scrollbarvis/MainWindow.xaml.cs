﻿using System;
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
        List<int> xCoord, yCoord;
        int numCoords = 0;
        WriteableBitmap wb;

        Scrollbar scrollbar;

        String background = "TaskImage.jpg";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void canvasloaded(object sender, RoutedEventArgs e)
        {
            double screenheight = this.ActualHeight - SystemParameters.WindowNonClientFrameThickness.Top - SystemParameters.WindowNonClientFrameThickness.Bottom;
            double screenwidth = this.ActualWidth - SystemParameters.WindowNonClientFrameThickness.Left - SystemParameters.WindowNonClientFrameThickness.Right;
            SolidColorBrush blankbg = new SolidColorBrush(Colors.LightGray);
            SolidColorBrush handle = new SolidColorBrush(Colors.Gray);

            byte[,,] pixels = createBitmap();

            ImageBrush vertheatmap = new ImageBrush(createVerticalHeatmap(150, (int)screenheight, yCoord, numCoords, 4330, 5));

            scrollbar = new Scrollbar(15, 150, screenheight, screenwidth, 0.9, 100, bg, blankbg, handle, vertheatmap, canv, 1, wb, heatmap, pixels);

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

            scrollbar.checkGaze(currentGaze);
            if (scrollbar.needsupdate)
                scrollbar.update();
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
            byte[,,] pixels;
            bool heatmapEnabled = true; /* Enable or Disable Heatmap!*/
            double bgTopPosition = 0;

            public Scrollbar(double collapsedwidth, double expandedwidth, double screenheight, double screenwidth, double smoothness, int duration,
                             Rectangle background, SolidColorBrush blank, SolidColorBrush hand, ImageBrush vertheatmap, Canvas canv, int zindex,
                             WriteableBitmap writeableBitmap, Image heatmapImage, byte[,,] heatmapPixels) {
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

                /* Heatmap */
                wb = writeableBitmap;
                heatmap = heatmapImage;
                pixels = heatmapPixels;

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
                heatmap.Visibility = Visibility.Hidden;
            }
            private void mousemove(object sender, MouseEventArgs e) {
                if (Panel.GetZIndex(hover) == z + 5) {
                    double handley = e.GetPosition(hover).Y - handle.Height / 2;
                    handley = handley > 0 ? handley : 0;
                    handley = handley + handle.Height < scrheight ? handley : scrheight - handle.Height;
                    Canvas.SetTop(handle, handley);
                    bgTopPosition = -handley * (bg.Height / scrheight);
                    Canvas.SetTop(bg, bgTopPosition);
                } else
                {
                    heatmap.Visibility = Visibility.Hidden;
                }
            }
            private void mouseup(object sender, MouseEventArgs e) {
                Panel.SetZIndex(hover, z);
                /* Set Heatmap */
                double y = e.GetPosition(hover).Y - handle.Height / 2;
                if (heatmapEnabled)
                {
                    y = -1 * bgTopPosition;
                    setBitmap((int)(y < 0 ? 0 : y), pixels);
                }
            }

            private void mousescroll(object sender, MouseWheelEventArgs e) {
                double handley = Canvas.GetTop(handle) - e.Delta / (.001 * bg.Height);
                handley = handley > 0 ? handley : 0;
                handley = handley + handle.Height < scrheight ? handley : scrheight - handle.Height;
                Canvas.SetTop(handle, handley);
                bgTopPosition = -handley * (bg.Height / scrheight);
                Canvas.SetTop(bg, bgTopPosition);
            }

            /*
            * Set bitmap for the portion of screen starting at Y position screenPositionTop
            */
            private void setBitmap(int screenPositionTop, byte[,,] px)
            {
                int height = 3000;
                int width = (int)bg.Width;
                // Copy the data into a one-dimensional array.
                byte[] pixels1d = new byte[height * width * 4];
                int index = 0;
                for (int row = screenPositionTop; row < height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        for (int i = 0; i < 4; i++)
                            pixels1d[index++] = px[row, col, i];
                    }
                }
                // Update writeable bitmap
                Int32Rect rect = new Int32Rect(0, 0, width, height);
                int stride = 4 * width;
                wb.WritePixels(rect, pixels1d, stride, 0);

                heatmap.Stretch = Stretch.None;
                heatmap.Margin = new Thickness(0);
                heatmap.Source = wb;
                heatmap.Visibility = Visibility.Visible;
            }
        }

        public WriteableBitmap createVerticalHeatmap(int width, int height, List<int> yCoords, int numCoords, double maxY, int spread) {
            byte[,] colors = new byte[,] { { 255, 0, 0 },
                                           { 0, 0, 255 }};
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


        #region heatmap setup
        /*
         * Make a heatmap from existing gaze coordinate data from a previous session. Fill in arrays xCoord and yCoord.
         */
        private void makeHeatmap()
        {
            // Read in data
            using (var reader = new StreamReader(@"C:/Users/ResearchSquad/Documents/GitHub/scrollbarvis/scrollbarvis/scrollbarvis/sample-gaze-data.csv"))
            {
                reader.ReadLine(); // Read header line
                xCoord = new List<int>();
                yCoord = new List<int>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    xCoord.Add(int.Parse(values[0]));
                    yCoord.Add(int.Parse(values[1]));
                }
                numCoords = xCoord.Count <= yCoord.Count ? xCoord.Count : yCoord.Count;
            }
        }

        /*
         * Create a bitmap of heatmap pixels. Color based on frequency of gaze coordinates at the pixel, plus color surrounding pixels.
         */
        private byte[,,] createBitmap()
        {
            int totalWidth = (int)bg.Width;
            int totalHeight = (int)bg.Height;
            wb = new WriteableBitmap(totalWidth, totalHeight, 96, 96, PixelFormats.Bgra32, null);
            byte[,,] pixels = new byte[totalHeight, totalWidth, 4];

            // Clear to red and transparent
            for (int row = 0; row < totalHeight; row++)
            {
                for (int col = 0; col < totalWidth; col++)
                {
                    for (int i = 0; i < 4; i++)
                        if (i==2)
                        {
                            pixels[row, col, i] = 255;
                        } else
                        {
                            pixels[row, col, i] = 0;
                        }
                }
            }
            // Get gaze coordinates, change pixel colors
            makeHeatmap();
            for (int i = 0; i < numCoords; i++)
            {
                int x = xCoord[i];
                int y = yCoord[i];
                double distanceFromCenter, aDifferenceFromMax;
                int maxDistance = 100;
                int maxOpacity = 150;
                double gValue, aValue;
                for (int j = (x - maxDistance) < 0 ? 0 : (x - maxDistance); j < ((x + maxDistance) > totalWidth ? totalWidth : (x + maxDistance)); j++)
                {
                    for (int k = (y - maxDistance) < 0 ? 0 : (y - maxDistance); k < ((y + maxDistance) > totalHeight ? totalHeight : (y + maxDistance)); k++)
                    {
                        distanceFromCenter = Math.Sqrt((double)(Math.Pow(x - j,2) + Math.Pow(y - k,2)));
                        if (distanceFromCenter <= (double)maxDistance)
                        {
                            gValue = pixels[j, k, 1];
                            aDifferenceFromMax = pixels[j, k, 3] / maxOpacity; //closer to 1 = maxed
                            aValue = (pixels[j, k, 3] + 10 * (1 - (distanceFromCenter / maxDistance)));
                            aValue = (aValue > maxOpacity ? maxOpacity : aValue);
                            pixels[j, k, 1] = (byte) (255 * (1 - aValue / maxOpacity)); //Green value
                            pixels[j, k, 3] = (byte) aValue; //Opacity
                        }
                    }
                }
            }
            return pixels;
        }
        #endregion
    }
}
