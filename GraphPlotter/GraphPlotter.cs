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
    public static class GraphPlotter
    {

        public struct Edge
        {
            public int VertexIndex { get; set; }
            public List<int> ConnectingVertexesIndexes { get; set; }
        }

        static double Distance(PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        static PointF Middle(PointF p1, PointF p2)
        {
            return new PointF
            {
                X = (p1.X + p2.X) / 2,
                Y = (p1.Y + p2.Y) / 2
            };
        }

        public static Bitmap Plot(List<PointF> points, List<Edge> edges, int width = 500, int height = 500)
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
            pointSeries.MarkerSize = 22;

            var font = new Font("arial", 14);
            double weight = 0;

            // -1 because the last point has no edges.
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];

                // Add point.
                pointSeries.Points.AddXY(p.X, p.Y);

                //chart.Annotations.Add(
                //    new CalloutAnnotation
                //    {
                //        AxisX = area.AxisX,
                //        AxisY = area.AxisY,
                //        X = p.X /*- 0.27*/,
                //        Y = p.Y /*+ 0.32*/,
                //        Text = (i + 1).ToString(),
                //        Font = font,
                //        CalloutStyle = CalloutStyle.Ellipse,
                //        Height = 1,
                //        Width = 1,
                //        BackColor = Color.LimeGreen,
                //        BackSecondaryColor = Color.ForestGreen,
                //        ForeColor = Color.White,
                //        LineColor = Color.Green,
                //        BackGradientStyle = GradientStyle.Center
                //    }
                //);

                if (i < edges.Count)
                {
                    // Add lines.
                    foreach (var vertex in edges[i].ConnectingVertexesIndexes)
                    {
                        var edgeName = "edge(" + i + "," + vertex + ")";
                        var line = chart.Series.Add(edgeName);
                        line.ChartType = SeriesChartType.Line;
                        line.BorderWidth = 7;
                        //line.Color = Color.ForestGreen;
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
