using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.DataVisualization.Charting;

namespace Plotter
{
    public struct PointD
    {
        public double X { get; set; }
        public double Y { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is PointD))
            {
                return false;
            }

            var point = (PointD)obj;

            return (point.X == this.X && point.Y == this.Y);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }
    }

    public static class GraphPlotter
    {

        public struct Edge
        {
            public int VertexIndex { get; set; }
            public List<int> ConnectingVertexesIndexes { get; set; }
        }

        static double Distance(PointD p1, PointD p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        static PointD Middle(PointD p1, PointD p2)
        {
            return new PointD
            {
                X = (p1.X + p2.X) / 2,
                Y = (p1.Y + p2.Y) / 2
            };
        }

        private static Color GetColor(int number)
        {
            var colors = typeof(Color).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            var chosenColor = colors.ToList()
                .Where(x => x.Name != "Transparent")
                .ToList()[number % 100];

            return Color.FromName(chosenColor.Name);

        }

        public static Bitmap Plot(List<PointD> points, List<Edge> edges, int width = 500, int height = 500, List<List<int>> components = null)
        {

            var area = new ChartArea();
            area.Name = "area";

            area.AxisX.MajorGrid.LineColor = Color.Gray;
            area.AxisY.MajorGrid.LineColor = Color.Gray;
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisX.Interval = 5;
            area.AxisY.Interval = 5;

            var minx = points.Min(p => p.X);
            var maxx = points.Max(p => p.X);

            var miny = points.Min(p => p.Y);
            var maxy = points.Max(p => p.Y);

            area.AxisX.Minimum = minx;
            area.AxisX.Maximum = maxx;

            area.AxisY.Minimum = miny;
            area.AxisY.Maximum = maxy;

            var chart = new Chart();
            chart.ChartAreas.Add(area);
            chart.Titles.Add("title");
            chart.Titles[0].Text = "Graph";
            chart.Titles[0].Font = new Font("arial", 14);
            chart.Width = width;
            chart.Height = height;



            var backColor = Color.FromArgb(223, 229, 223);

            chart.BackColor =
                chart.ChartAreas[0].BackColor = backColor;

            var pointSeries = new Series("points");
            pointSeries.ChartType = SeriesChartType.Point;
            pointSeries.Color = Color.FromArgb(13, 95, 13);
            pointSeries.MarkerSize = 1;

            var font = new Font("arial", 14);
            double weight = 0;
            double annotationSizeModifier = 1;

            // -1 because the last point has no edges.
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];

                // Add point.
                pointSeries.Points.AddXY(p.X, p.Y);

                if (i > 99)
                {
                    annotationSizeModifier = 1.25;
                }
                else
                {
                    if (i > 999)
                    {
                        annotationSizeModifier = 1.5;
                    }
                }

                chart.Annotations.Add(
                    new CalloutAnnotation
                    {
                        AxisX = area.AxisX,
                        AxisY = area.AxisY,
                        X = p.X - 0.35,
                        Y = p.Y + 0.7,
                        Text = (i).ToString(),
                        Font = font,
                        CalloutStyle = CalloutStyle.Ellipse,
                        Height = 1 * annotationSizeModifier,
                        Width = 0.5 * annotationSizeModifier,
                        BackColor = Color.LimeGreen,
                        BackSecondaryColor = Color.ForestGreen,
                        ForeColor = Color.White,
                        LineColor = Color.Green,
                        BackGradientStyle = GradientStyle.Center
                    }
                );

                if (i < edges.Count)
                {
                    // Add lines.
                    int color = i * 10;
                    foreach (var vertex in edges[i].ConnectingVertexesIndexes)
                    {
                        var edgeName = "edge(" + i + "," + vertex + ")";
                        var line = chart.Series.Add(edgeName);
                        line.ChartType = SeriesChartType.Line;
                        line.BorderWidth = 7;
                        if (components != null)
                        {
                            var componentNumber = components.FindLastIndex(l => l.Contains(i));
                            if (componentNumber < 0)
                            {
                                componentNumber = 0;
                            }
                            line.Color = GetColor(componentNumber * 2);
                        }
                        line.Points.AddXY(p.X, p.Y);
                        line.Points.AddXY(points[vertex].X, points[vertex].Y);

                        var dist = Distance(p, points[vertex]);
                        var midPoint = Middle(p, points[vertex]);
                        weight += dist;

                        //chart.Annotations.Add(
                        //    new CalloutAnnotation
                        //    {
                        //        AxisX = area.AxisX,
                        //        AxisY = area.AxisY,
                        //        X = midPoint.X,
                        //        Y = midPoint.Y,
                        //        Text = dist.ToString()
                        //    }
                        //);
                    }
                }
            }


            chart.Series.Add(pointSeries);

            chart.Titles[0].Text += Environment.NewLine + "Total Weight: " + weight;

            var stream = new MemoryStream();

            chart.ImageType = ChartImageType.Png;
            chart.SaveImage(stream);
            //stream.Position = 0;

            return new Bitmap(stream);
        }
    }
}
