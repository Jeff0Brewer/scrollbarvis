using System;
using System.Collections.Generic;
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

        Scrollbar scrollbar;

        String background = "TaskImage.jpg";

        public MainWindow()
        {
            InitializeComponent();

            eyeXHost = new EyeXHost();
            eyeXHost.Start();
            var gazeData = eyeXHost.CreateGazePointDataStream(GazePointDataMode.Unfiltered);
            gazeData.Next += newGazePoint;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Start();
        }

        private void canvasloaded(object sender, RoutedEventArgs e)
        {
            double screenheight = this.ActualHeight - SystemParameters.WindowNonClientFrameThickness.Top - SystemParameters.WindowNonClientFrameThickness.Bottom;
            SolidColorBrush blankbg = new SolidColorBrush(Colors.LightGray);
            SolidColorBrush handle = new SolidColorBrush(Colors.Gray);

            scrollbar = new Scrollbar(15,50,screenheight,bg,blankbg,handle,canv,1);
        }

        private void newGazePoint(object s, EyeXFramework.GazePointEventArgs e)
        {
            track.X = e.X;
            track.Y = e.Y;
        }

        private void update(object sender, EventArgs e)
        {
            Point currentGaze = PointFromScreen(track);
        }

        public class Scrollbar {
            private Rectangle handle, blankbg, picturebg, hover, bg;
            private double inwidth, outwidth, scrheight;
            private int z;

            public Scrollbar(double collapsedwidth, double expandedwidth, double screenheight, 
                             Rectangle background, SolidColorBrush blank, SolidColorBrush hand, Canvas canv, int zindex) {
                inwidth = collapsedwidth;
                outwidth = expandedwidth;
                scrheight = screenheight;
                bg = background;
                z = zindex;

                handle = new Rectangle();
                handle.Width = inwidth;
                handle.Height = scrheight/bg.Height*scrheight;
                Canvas.SetRight(handle, 0);
                Canvas.SetTop(handle, 0);
                Panel.SetZIndex(handle, z + 3);
                handle.Fill = hand;
                handle.IsHitTestVisible = false;
                canv.Children.Add(handle);

                blankbg = new Rectangle();
                blankbg.Width = inwidth;
                blankbg.Height = scrheight;
                Canvas.SetRight(blankbg, 0);
                Canvas.SetTop(blankbg, 0);
                Panel.SetZIndex(blankbg, z + 2);
                blankbg.Fill = blank;
                blankbg.PreviewMouseDown += mousedown;
                canv.Children.Add(blankbg);

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
            }

            private void mousedown(object sender, MouseButtonEventArgs e) {
                Panel.SetZIndex(hover, z + 4);
            }

            private void mousemove(object sender, MouseEventArgs e) {
                if (Panel.GetZIndex(hover) == z + 4) {
                    double handley = e.GetPosition(hover).Y - handle.Height/2;
                    handley = handley > 0 ? handley : 0;
                    handley = handley + handle.Height < scrheight ? handley : scrheight - handle.Height;
                    Canvas.SetTop(handle, handley);
                    Canvas.SetTop(bg, -handley * (bg.Height / scrheight));
                }
            }

            private void mouseup(object sender, MouseEventArgs e) {
                Panel.SetZIndex(hover, z);
            }

            private void mousescroll(object sender, MouseWheelEventArgs e) {
                double handley = Canvas.GetTop(handle) - e.Delta / (.001*bg.Height);
                handley = handley > 0 ? handley : 0;
                handley = handley + handle.Height < scrheight ? handley : scrheight - handle.Height;
                Canvas.SetTop(handle, handley);
                Canvas.SetTop(bg, -handley * (bg.Height / scrheight));
            }
        }
    }
}
