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

        static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        static PointF Middle(Point p1, Point p2)
        {
            return new PointF
            {
                X = (p1.X + p2.X) / 2,
                Y = (p1.Y + p2.Y) / 2
            };
        }

        public static Bitmap Plot(List<Point> points, List<Edge> edges, int width = 500, int height = 500)
        {

            var area = new ChartArea();
            area.Name = "area";
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.Enabled = false;
            //area.AxisX.Minimum = 0;
            //area.AxisY.Minimum = 0;
            //area.AxisX.Interval = 1;
            //area.AxisY.Interval = 1;
            //area.AxisX.IsLogarithmic = false;
            //area.AxisY.IsLogarithmic = false;
            area.AxisX.IsStartedFromZero = true;
            area.AxisY.IsStartedFromZero = true;

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
            pointSeries.MarkerSize = 7;

            double weight = 0;

            // -1 because the last point has no edges.
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p = points[i];

                // Add point.
                pointSeries.Points.AddXY(p.X, p.Y);

                chart.Annotations.Add(
                    new CalloutAnnotation
                    {
                        AxisX = area.AxisX,
                        AxisY = area.AxisY,
                        X = p.X - 0.27,
                        Y = p.Y + 0.32,
                        Text = (i + 1).ToString(),
                        CalloutStyle = CalloutStyle.Ellipse,
                        Height = 5,
                        Width = 4,
                        BackColor = Color.LimeGreen,
                        BackSecondaryColor = Color.ForestGreen,
                        ForeColor = Color.White,
                        LineColor = Color.Green,
                        BackGradientStyle = GradientStyle.Center
                    }
                );

                // Add lines.
                foreach (var vertex in edges[i].ConnectingVertexesIndexes)
                {
                    var edgeName = "edge(" + i + "," + vertex + ")";
                    var line = chart.Series.Add(edgeName);
                    line.ChartType = SeriesChartType.Line;
                    line.BorderWidth = 3;
                    //line.Color = Color.ForestGreen;
                    line.Points.AddXY(p.X, p.Y);
                    line.Points.AddXY(points[vertex].X, points[vertex].Y);

                    var dist = Distance(p, points[vertex]);
                    var midPoint = Middle(p, points[vertex]);
                    weight += dist;

                    chart.Annotations.Add(
                    new CalloutAnnotation
                    {
                        AxisX = area.AxisX,
                        AxisY = area.AxisY,
                        X = midPoint.X,
                        Y = midPoint.Y,
                        Text = dist.ToString()
                    }
                );
                }

            }


            // Plot last point.
            pointSeries.Points.AddXY(points.Last().X, points.Last().Y);

            chart.Annotations.Add(
                    new CalloutAnnotation
                    {
                        AxisX = area.AxisX,
                        AxisY = area.AxisY,
                        X = points.Last().X - 0.27,
                        Y = points.Last().Y + 0.32,
                        Text = points.Count.ToString(),
                        CalloutStyle = CalloutStyle.Ellipse,
                        Height = 5,
                        Width = 4,
                        BackColor = Color.LimeGreen,
                        BackSecondaryColor = Color.ForestGreen,
                        ForeColor = Color.White,
                        LineColor = Color.Green,
                        BackGradientStyle = GradientStyle.Center

                    }
                );

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
