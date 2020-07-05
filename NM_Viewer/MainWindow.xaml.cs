using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using NM_Viewer.Helpers;
using NM_Viewer.Objects;

namespace NM_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public long NodeCount = 0;
        public long EdgeCount = 0;
        public long EdgeSkippedCount = 1;


        public Dictionary<long, Node> Nodes = new Dictionary<long, Node>();
        public Dictionary<long, Signal> Signals = new Dictionary<long, Signal>();
        HashSet<string> WayId = new HashSet<string>();

        List<string> tiplocs = new List<string>();
        Dictionary<string, string> elocs = new Dictionary<string, string>();

        bool drawing = true;

        bool showSignals = false;
        bool showElocs = false;
        bool showTiplocs = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {

            string file = "NM_2020_07_04.xml";

            if (!File.Exists(file))
            {                
                    MessageBox.Show($"Cannot find the following file:\n\n{file}", "Aborting");
                    Environment.Exit(1);                
            }

            wFilter wf = new wFilter();
            wf.Owner = this;
            wf.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wf.ShowDialog();

            showSignals = wf.Signals;
            showElocs = wf.Elocs;
            showTiplocs = wf.Tiplocs;

            Task.Factory.StartNew(() =>
            {
                UpdateStatus($"Loading {file}...");
                LoadModel(file);
                DrawModel();


                UpdateStatus($"Finished.");
            });

        }

        public void LoadModel(string filename)
        {
            Console.WriteLine($"Loading File [{filename}]...");
            XDocument xDoc = XDocument.Load(filename);

            Random r = new Random();

            Console.WriteLine($"Parsing {filename}...");
            XElement root = xDoc.Elements().FirstOrDefault(rr => rr.Name.LocalName == "tps_data");

            bool save = false;

            double lowestX = double.MaxValue;
            double lowestY = double.MaxValue;

            {
                var nodes = root.Elements().Where(nn => nn.Name.LocalName == "node");

                foreach (var node in nodes)
                {
                    //string validTo = node.GetValue<string>("validTo");
                    //DateTime validToDate = DateTime.ParseExact(validTo, "yyyy-MM-dd", CultureInfo.CurrentUICulture);

                    //if (validToDate < DateTime.Now)
                    //    continue;

                    Node n = new Node();
                    n.Id = node.Elements().First().GetValue<long>("nodeid");
                    n.X = node.GetValue<double>("netx");
                    n.Y = node.GetValue<double>("nety");

                    if (n.X < lowestX)
                        lowestX = n.X;

                    if (n.Y < lowestY)
                        lowestY = n.Y;

                    n.Name = node.GetValue<string>("name");
                    Nodes.Add(n.Id, n);
                    NodeCount++;
                }

                double offsetX = lowestX * -1;
                double offsetY = lowestY * -1;

                foreach (var node in Nodes)
                {
                    node.Value.X += offsetX;
                    node.Value.Y += offsetY;
                }
            }

            {
                var stations = root.Elements().Where(nn => nn.Name.LocalName == "station");

                foreach (var node in stations)
                {
                    //string validTo = node.GetValue<string>("validTo");
                    //DateTime validToDate = DateTime.ParseExact(validTo, "yyyy-MM-dd", CultureInfo.CurrentUICulture);

                    //if (validToDate < DateTime.Now)
                    //    continue;

                    byte R = (byte)r.Next(1, 255);
                    byte G = (byte)r.Next(1, 255);
                    byte B = (byte)r.Next(1, 255);

                    long stationId = node.GetValue<long>("stationid");
                    string stationName = node.GetValue<string>("longname");
                    string abbrev = node.GetValue<string>("abbrev");

                    var tracks = node.Elements().Where(nn => nn.Name.LocalName == "track");

                    long lastSeq = -1;
                    foreach (var track in tracks)
                    {
                        string validTo = track.GetValue<string>("validTo");
                        DateTime validToDate = DateTime.ParseExact(validTo, "yyyy-MM-dd", CultureInfo.CurrentUICulture);

                        if (validToDate < DateTime.Now)
                            continue;

                        long seq = track.GetValue<long>("seq");
                        if (seq <= lastSeq)
                            continue;

                        lastSeq = seq;

                        var way = track.Elements().FirstOrDefault(nn => nn.Name.LocalName == "way");
                        string trackName = track.GetValue<string>("name") + " - " +
                                           track.GetValue<string>("description");
                        if (way == null)
                            continue;

                        //foreach (var pnt in way.Elements().Where(nn => nn.Name.LocalName == "point"))
                        //{
                        //    long nodeId = pnt.GetValue<long>("nodeid");
                        //    Station n = new Station();
                        //    n.Name = $"{abbrev} - {stationName} ({trackName})";
                        //    n.Id = stationId;
                        //    n.TIPLOC = abbrev;

                        //    n.R = R;
                        //    n.G = G;
                        //    n.B = B;

                        //    Nodes[nodeId].Station = n;
                        //}

                        var points = way.Elements().Where(nn => nn.Name.LocalName == "point").ToArray();

                        var middle = points[points.Length / 2];
                        long nodeId = middle.GetValue<long>("nodeid");

                        Station n = new Station();
                        n.Name = $"{abbrev}, {stationName} ({trackName})";
                        n.Id = stationId;
                        n.TIPLOC = abbrev;

                        if (!elocs.ContainsKey(n.TIPLOC) && n.TIPLOC.StartsWith("ELOC"))
                            elocs.Add(n.TIPLOC, n.Name);

                        if (!tiplocs.Contains(n.TIPLOC) && !n.TIPLOC.StartsWith("ELOC"))
                            tiplocs.Add(n.TIPLOC);

                        if (!showElocs && n.TIPLOC.StartsWith("ELOC"))
                            continue;

                        if (!showTiplocs && !n.TIPLOC.StartsWith("ELOC"))
                            continue;

                        n.R = R;
                        n.G = G;
                        n.B = B;

                        Nodes[nodeId].Stations.Add(n);

                        if (Nodes[nodeId].Stations.Count > 1)
                        {
                            ;
                        }

                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("tiplocs.csv", false))
            {
                foreach (var tpl in tiplocs)
                {
                    sw.WriteLine($"{tpl.Trim()}");
                }
            }

            using (StreamWriter sw = new StreamWriter("elocs.csv", false))
            {
                foreach (var el in elocs)
                {
                    sw.WriteLine($"{el.Key.Trim()},\"{el.Value.Split(',')[1].Trim()}\"");
                }
            }

            {
                var edges = root.Elements().Where(nn => nn.Name.LocalName == "edge");
                foreach (var edge in edges)
                {
                    string validTo = edge.GetValue<string>("validTo");
                    DateTime validToDate = DateTime.ParseExact(validTo, "yyyy-MM-dd", CultureInfo.CurrentUICulture);

                    if (validToDate < DateTime.Now)
                        continue;

                    Edge e = new Edge();
                    e.Id = edge.GetValue<long>("id");
                    e.Length = edge.GetValue<long>("length");

                    long startId = 0;
                    long endId = 0;

                    var directed = edge.Elements().FirstOrDefault(ee => ee.Name.LocalName == "directed");
                    if (directed != null)
                    {
                        var start = directed.Elements().FirstOrDefault(ee => ee.Name.LocalName == "start");
                        startId = start.Elements().First().GetValue<long>("nodeid");

                        var end = directed.Elements().FirstOrDefault(ee => ee.Name.LocalName == "end");
                        endId = end.Elements().First().GetValue<long>("nodeid");
                    }

                    string wayId = $"{startId}-{endId}";
                    string revWayId = $"{endId}-{startId}";


                    if (WayId.Contains(revWayId))
                    {
                        EdgeSkippedCount++;
                        continue;
                    }

                    e.EndNode = Nodes[endId];
                    Nodes[startId].Edges.Add(e);
                    WayId.Add(wayId);
                    EdgeCount++;
                }

                WayId.Clear();
            }


            {
                if (showSignals)
                {
                    var signals = root.Elements().Where(nn => nn.Name.LocalName == "signal"); //SHOULD BE SIGNAL

                    foreach (var sgn in signals)
                    {
                        ////string validTo = node.GetValue<string>("validTo");
                        ////DateTime validToDate = DateTime.ParseExact(validTo, "yyyy-MM-dd", CultureInfo.CurrentUICulture);

                        ////if (validToDate < DateTime.Now)
                        ////    continue;

                        Signal n = new Signal();
                        n.Id = sgn.GetValue<long>("signalid");
                        n.BaseSignalType = sgn.GetValue<long>("interlockingsysid");
                        n.Name = sgn.GetValue<string>("name");

                        long startId = 0;
                        long endId = 0;

                        var directed = sgn.Elements().FirstOrDefault(ee => ee.Name.LocalName == "directed");
                        if (directed != null)
                        {
                            var start = directed.Elements().FirstOrDefault(ee => ee.Name.LocalName == "start");
                            startId = start.Elements().First().GetValue<long>("nodeid");

                            //var end = directed.Elements().FirstOrDefault(ee => ee.Name.LocalName == "end");
                            //endId = end.Elements().First().GetValue<long>("nodeid");
                        }

                        n.NodeId = startId;
                        Signals.Add(n.Id, n);

                    }
                }
            }

            //TODO: Doesn't work, needs improving...
            ////ReduceEdges
            //var iNodes = Nodes.Where(nn => nn.Value.Intersection).ToArray();
            // long NewWays = 0;
            //foreach (var node in iNodes)
            //{
            //    foreach (var edge in node.Value.Edges)
            //    {
            //        long length = edge.Length;
            //        List<long> nodeIds = new List<long>();
            //        nodeIds.Add(node.Key);

            //        var nextNode = edge.EndNode;

            //        if (nextNode.Intersection)
            //        {
            //            nodeIds.Add(nextNode.Id);
            //            NewWays++;
            //            continue;
            //        }

            //        while (nextNode.Intersection != true && !nodeIds.Contains(nextNode.Id))
            //        {
            //            nodeIds.Add(nextNode.Id);
            //            length += nextNode.Edges[0].Length;
            //            nextNode = nextNode.Edges[0].EndNode;
            //        }

            //        ;
            //    }

            //}

            //foreach (var track in tracks)
            //{
            //    string validTo = track.GetValue<string>("validTo");
            //    DateTime validToDate = DateTime.ParseExact(validTo, "yyyy-MM-dd", CultureInfo.CurrentUICulture);

            //    if (validToDate < DateTime.Now)
            //        continue;

            //    Track t = new Track();
            //    t.Id = track.GetValue<long>("trackID");
            //    t.Name = track.GetValue<string>("name");
            //    t.Description = track.GetValue<string>("description");
            //    t.Directed = track.GetValue<bool>("directed");
            //    t.TrackCategory = track.GetValue<int>("trackcategory");

            //    var way = track.Elements().FirstOrDefault(nn => nn.Name.LocalName == "way");

            //    if (way != null)
            //    {
            //        var points = way.Elements().Where(nn => nn.Name.LocalName == "point");
            //        foreach (var pnt in points)
            //        {
            //            ;
            //        }
            //    }

            //   // Nodes.Add(n.Id, n);
            //}

            UpdateStatus($"Loaded {filename}.");

        }


        public void DrawModel()
        {

            long count = Nodes.Count;
            UpdateStatus($"Drawing Model [{count:N0}]...");

            cMain.Dispatcher.Invoke(() =>
            {
                try
                {


                    long i = 0;
                    foreach (var n in Nodes)
                    {
                        if (i % 1000 == 0 || i == count - 1)
                        {
                            UpdateStatus($"Drawing Model [{i:N0} of {count:N0}]...");
                        }

                        foreach (Edge e in n.Value.Edges)
                        {
                            Line line = new Line();
                            //line.Stroke = Brushes.LightSteelBlue;
                            line.X1 = n.Value.X;
                            line.X2 = e.EndNode.X;
                            line.Y1 = n.Value.Y;
                            line.Y2 = e.EndNode.Y;
                            line.SnapsToDevicePixels = true;
                            line.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                            line.StrokeThickness = 2;
                            line.ToolTip = $"Id: {e.Id}, Length: {e.Length}";
                            line.Stroke = Brushes.Black;

                            cMain.Children.Add(line);

                            //{
                            //    Rectangle elip = new Rectangle();
                            //    elip.Height = 5;
                            //    elip.Width = 3;
                            //    elip.Fill = Brushes.Black;
                            //    elip.Stroke = Brushes.Black;
                            //    elip.StrokeThickness = 0.5;
                            //    Canvas.SetTop(elip, n.Value.Y - elip.Width / 2);
                            //    Canvas.SetLeft(elip, n.Value.X - elip.Height / 2);
                            //    cMain.Children.Add(elip);
                            //}
                            //{
                            //    Rectangle elip = new Rectangle();
                            //    elip.Height = 5;
                            //    elip.Width = 3;
                            //    elip.ToolTip = $" [{e.EndNode.Id}] {e.EndNode.X}, {e.EndNode.Y}";

                            //    elip.Fill = Brushes.Black;
                            //    elip.Stroke = Brushes.Black;
                            //    elip.StrokeThickness = 0.5;
                            //    Canvas.SetTop(elip, e.EndNode.Y - elip.Width / 2);
                            //    Canvas.SetLeft(elip, e.EndNode.X - elip.Height / 2);
                            //    cMain.Children.Add(elip);
                            //}
                        }

                        i++;
                    }

                    lblStatus.Content = "Drawing Tiplocs...";
                    foreach (var n in Nodes)
                    {
                        if (n.Value.Stations.Count > 0)
                        {

                            Rectangle elip = new Rectangle();
                            elip.Height = 6;
                            elip.Width = 6;
                            bool eloc = false;
                            foreach (var stn in n.Value.Stations)
                            {
                                elip.ToolTip += $"{stn.Name} [{n.Key}] {n.Value.X}, {n.Value.Y}" + Environment.NewLine;

                                if (stn.TIPLOC.StartsWith("ELOC"))
                                    elip.RenderTransform.Value.Rotate(45);

                            }

                            elip.ToolTip = elip.ToolTip.ToString().Trim();

                            Brush brush = new SolidColorBrush(Color.FromRgb(n.Value.Stations[0].R,
                                n.Value.Stations[0].G, n.Value.Stations[0].B));
                            elip.MouseDown += ElipOnMouseDown;
                            elip.Fill = brush;
                            elip.Stroke = Brushes.Black;
                            elip.StrokeThickness = 0.5;

                            Canvas.SetTop(elip, n.Value.Y - elip.Width / 2);
                            Canvas.SetLeft(elip, n.Value.X - elip.Height / 2);
                            cMain.Children.Add(elip);


                        }
                    }

                    //lblStatus.Content = "Drawing Intersections...";
                    //foreach (var n in Nodes)
                    //{
                    //    if (n.Value.Intersection)
                    //    {
                    //        Rectangle elip = new Rectangle();
                    //        elip.Height = 4;
                    //        elip.Width = 4;
                    //        elip.ToolTip = $"{n.Value.Edges.Count} [{n.Key}] {n.Value.X}, {n.Value.Y}";
                    //        elip.Fill = Brushes.Black;
                    //        elip.Stroke = Brushes.Black;
                    //        elip.StrokeThickness = 0.5;

                    //        Canvas.SetTop(elip, n.Value.Y - elip.Width / 2);
                    //        Canvas.SetLeft(elip, n.Value.X - elip.Height / 2);
                    //        cMain.Children.Add(elip);
                    //    }
                    //}

                    lblStatus.Content = "Drawing Signals...";
                    foreach (var n in Signals)
                    {
                        var node = Nodes[n.Value.NodeId];
                        Ellipse elip = new Ellipse();
                        elip.Height = 4;
                        elip.Width = 4;
                        elip.ToolTip = $"{n.Value.Name} [{n.Key}] {node.X}, {node.Y}";

                        if (n.Value.Name == "SPEED" ||
                            n.Value.Name.StartsWith("Boundary") ||
                            n.Value.Name == "Stop Board")
                            continue;

                        if (n.Value.Name.EndsWith("EXIT") || n.Value.Name.EndsWith("ENTRY"))
                            continue;

                        elip.Fill = Brushes.Yellow;
                        switch (n.Value.BaseSignalType)
                        {
                            case 1:
                                elip.Fill = Brushes.Red;
                                elip.ToolTip += Environment.NewLine + "Main Signal";
                                break;
                            case 2:
                                elip.Fill = Brushes.White;
                                elip.ToolTip += Environment.NewLine + "Speed Indicator";
                                continue;
                            case 3:
                                elip.ToolTip += Environment.NewLine + "Shunting Signal";
                                break;
                            case 4:
                                elip.ToolTip += Environment.NewLine + "Fictive Signal";
                                break;
                            case 5:
                                elip.ToolTip += Environment.NewLine + "Combined ATC/Main-Signal";
                                elip.Fill = Brushes.LightCoral;
                                break;
                            case 6:
                                elip.ToolTip += Environment.NewLine + "ATC-Signal";
                                elip.Fill = Brushes.Green;
                                break;
                            case 7:
                                elip.Fill = Brushes.Black;
                                elip.ToolTip += Environment.NewLine + "Stopping Point";
                                continue;
                            case 8:
                            case 9:
                                elip.Fill = Brushes.Red;
                                elip.ToolTip += Environment.NewLine + "Multi Aspect Signal";
                                break;
                            case 10:
                                elip.ToolTip += Environment.NewLine + "Ground Position Light";
                                break;
                            case 11:
                                elip.ToolTip += Environment.NewLine + "Semaphore Shunting Signal";
                                break;
                            case 12:
                                elip.ToolTip += Environment.NewLine + "Home/Starter Semaphore";
                                break;
                            case 13:
                                elip.ToolTip += Environment.NewLine + "Phone for Authorisation";
                                break;
                            case 14:
                                elip.ToolTip += Environment.NewLine + "Limit of Shunt";
                                break;
                            default:
                                elip.ToolTip += Environment.NewLine + $"SignalType: {n.Value.BaseSignalType}";
                                break;
                        }

                        elip.Stroke = Brushes.Black;
                        elip.StrokeThickness = 0.5;

                        Canvas.SetTop(elip, node.Y - elip.Width / 2);
                        Canvas.SetLeft(elip, node.X - elip.Height / 2);
                        cMain.Children.Add(elip);

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                svCanvas.ScrollToHorizontalOffset(17128);
                svCanvas.ScrollToVerticalOffset(31412);
                txtTiploc.IsEnabled = true;
                btnSearch.IsEnabled = true;

                drawing = false;
            });

            while (drawing)
            {
                Thread.Sleep(1000);
            }

            // UpdateStatus($"Drawing Model2 [{count:N0}]...");
            // cMain.RenderTransform = st;
        }

        private void ElipOnMouseDown(object sender, MouseButtonEventArgs e)
        {

            string tt = ((Rectangle)e.Source).ToolTip.ToString();

            wTooltip wt = new wTooltip(tt);
            wt.Owner = this;
            wt.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wt.ShowDialog();
        }

        public void UpdateStatus(string text)
        {
            Dispatcher.BeginInvoke(new Action(() => { lblStatus.Content = text; }), DispatcherPriority.Normal);
        }

        private void CCanvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (drawing)
                return;

            lblBar.Content =
                $"H[{svCanvas.HorizontalOffset}, V{svCanvas.VerticalOffset}] Mouse: {Mouse.GetPosition(cMain)}";
        }



        private void SvCanvas_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {

            e.Handled = true;
        }

        private void SvCanvas_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                st.ScaleX = 1;
                st.ScaleY = 1;
            }
        }

        private void BtnSearch_OnClick(object sender, RoutedEventArgs e)
        {
            bool found = false;
            txtTiploc.IsEnabled = false;
            foreach (var n in Nodes.Where(nn => nn.Value.Stations.Count > 0))
            {
                var tp = n.Value.Stations.FirstOrDefault(tt => tt.TIPLOC == txtTiploc.Text.ToUpper());

                if (tp != null)
                {
                    found = true;
                    svCanvas.ScrollToHorizontalOffset(n.Value.X - (this.Width / 2));
                    svCanvas.ScrollToVerticalOffset(n.Value.Y - (this.Height / 2));

                    Rectangle elip = new Rectangle();
                    elip.Height = 20;
                    elip.Width = 20;

                    Brush brush = new SolidColorBrush(Color.FromRgb(n.Value.Stations[0].R,
                        n.Value.Stations[0].G, n.Value.Stations[0].B));
                    elip.MouseDown += ElipOnMouseDown;
                    elip.Fill = brush;
                    elip.Stroke = Brushes.Black;
                    elip.StrokeThickness = 0.5;

                    Canvas.SetTop(elip, n.Value.Y - elip.Width / 2);
                    Canvas.SetLeft(elip, n.Value.X - elip.Height / 2);
                    cMain.Children.Add(elip);

                    Task.Factory.StartNew(() =>
                    {
                        bool visible = true;

                        for (int i = 0; i < 4; i++)
                        {
                            Thread.Sleep(500);


                            cMain.Dispatcher.Invoke(() =>
                            {
                                if (visible)
                                {
                                    cMain.Children.Remove(elip);
                                    visible = false;
                                }
                                else
                                {
                                    cMain.Children.Add(elip);
                                    visible = true;
                                }
                            });

                        }


                        cMain.Dispatcher.Invoke(() =>
                        {
                            if (visible)
                                cMain.Children.Remove(elip);

                            txtTiploc.IsEnabled = true;
                        });

                    });



                    break;
                }
            }

            if (!found)
            {
                MessageBox.Show("TIPLOC cannot be found and does not exist within the model.", "No TIPLOC");
                txtTiploc.IsEnabled = true;
            }

        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            var msg = MessageBox.Show("Are you sure you want to close this window?", "Confirmation?",
                MessageBoxButton.YesNo);

            if (msg != MessageBoxResult.Yes)
                e.Cancel = true;
        }
    }
}
