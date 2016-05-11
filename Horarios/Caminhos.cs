using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;
using QuickGraph;

/*
iterativo
+
callback (cplex namespace)
-> dizer que vertice nao pode ir para ele mesmo.
*/

namespace ProjetoPO
{
    using Plotter;
    using System.Diagnostics;
    using System.Drawing;
    using static Plotter.GraphPlotter;
    using Vertex = Int32;

    class MatrizAdjacenciaSimetrica<T>
    {
        T[] matrizLinear;

        public int N { get; private set; }

        public int LinearSize
        {

            get
            {
                return matrizLinear.Length;
            }
        }

        public MatrizAdjacenciaSimetrica(int dimensao)
        {
            N = dimensao;
            matrizLinear = new T[(N * (N + 1)) / 2];
        }

        public T this[int index]
        {
            get
            {
                return matrizLinear[index];
            }
        }

        public T this[int i, int j]
        {
            get
            {
                return Get(i, j);
            }
            set
            {
                Set(i, j, value);
            }
        }

        public T Get(int i, int j)
        {
            return matrizLinear[CalculaIndiceLinear(i, j)];
        }

        public T Set(int i, int j, T novoValor)
        {
            int indice = CalculaIndiceLinear(i, j);
            T valorAntigo = matrizLinear[indice];
            matrizLinear[indice] = novoValor;

            return valorAntigo;
        }

        private int CalculaIndiceLinear(int i, int j)
        {

            if (i > j)
            {
                var aux = i;
                i = j;
                j = aux;
            }

            return (N * i) + j - ((i * (i + 1)) / 2);
        }

        public string ToString(Func<T, string> transformer)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    var value = transformer(this[i, j]);
                    sb.Append(value).Append(" ");
                }
                sb.Append("\n");
            }


            return sb.ToString();
        }

        public override string ToString()
        {
            //Transformador trivial
            return ToString((t) => t.ToString());
        }

    }

    class Caminhos
    {
        public static void Main(string[] args)
        {

            //*******************************************************
            //  Obtem os dados de arquivo externo
            //
            //*******************************************************

            var files = Directory.GetFiles(@"./grafos/");

            foreach (var file in files)
            {
                if (Path.GetFileNameWithoutExtension(file) != "gr137")
                {
                    continue;
                }

                Solve(file);
            }

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            Console.ReadKey(true);

        }

        private static void Solve(string filePath)
        {
            // Transform points from file to actual PointF
            // instances that can be used to plot the chart.
            var points = ReadPoints(filePath);

            var matrix = AssembleMatrix(points);

            Console.WriteLine(matrix);

            Cplex model = new Cplex();

            // Each [i,j] is a bool var indicating whether
            // there is a connection between the points 'i' and 'j'.
            var X = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    X[i, j] = model.BoolVar();
                }

            }


            // Forces every vertex to connect to
            // another one.
            for (int i = 0; i < X.N; i++)
            {
                var exp = model.LinearNumExpr();

                for (int j = 0; j < X.N; j++)
                {
                    if (i != j)
                    {
                        exp.AddTerm(1.0, X[i, j]);
                    }
                }


                // Each vertex must connect to another one.
                model.AddEq(exp, 2.0);
            }            


            //Função objetivo
            var fo = model.LinearNumExpr();
            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    fo.AddTerm(matrix[i, j], X[i, j]);
                }
            }

            //Minimize
            model.AddMinimize(fo);

            Console.WriteLine("\n\n[Solving...]");
            bool solved = false;


            // Solve on a different thread.
            solved = model.Solve();

            Console.WriteLine("[Solved]\n\n");

            if (!solved)
            {
                Console.WriteLine("[No solution]");
                return;
            }

            Console.WriteLine("Solution status = " + model.GetStatus());
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Solution found:");
            Console.WriteLine(" Objective value = " + model.ObjValue);

            Console.WriteLine();
            Console.WriteLine("Grafo:");
            Console.WriteLine(matrix);
            Console.WriteLine("---------------\n");
            Console.WriteLine("Arestas escolhidas:");
            Console.Write(X.ToString((nv) => model.GetValue(nv).ToString()));

            Console.WriteLine("---------------\n");

            var Xdouble = new MatrizAdjacenciaSimetrica<double>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    Xdouble[i, j] = model.GetValue(X[i, j]);
                }
            }

            PlotPath(Xdouble, points);
        }

        static void FindTours(MatrizAdjacenciaSimetrica<double> matrix)
        {
            var vertex = new List<int>(matrix.N);

            // Traverse every vertex.
            for (int i = 0; i < matrix.N; i++)
            {
                //var dest = 
            }
        }

        /// <summary>
        /// Reads file and plots the vertexes.
        /// </summary>
        /// <param name="filePath">Path to graph file.</param>
        /// <param name="plotFileName">Name of the plot image that will be created.</param>
        /// <returns>List of points read from file.</returns>
        static List<PointF> ReadPoints(string filePath, bool plot = true, string plotFileName = "graphVertexes")
        {

            var lines = File.ReadAllLines(filePath);
            var chartPoints = new List<PointF>(lines.Length - 1);


            for (int i = 1; i < lines.Length; i++)
            {
                var xy = lines[i].Split(' ');
                chartPoints.Add(new PointF { X = float.Parse(xy[0]), Y = float.Parse(xy[1]) });
            }


            if (plot)
            {
                var bmp = Plot(chartPoints, new List<Edge>(), 1024 * 5, 768 * 3);
                bmp.Save(plotFileName + ".png");
                bmp.Dispose();
            }

            return chartPoints;

        }

        static void PlotPath(MatrizAdjacenciaSimetrica<double> matrix, List<PointF> points, string plotFileName = "TSP")
        {
            // Only N - 1 points have edges.
            // We need not compute the edge of the
            // lats vertex.
            var edges = new List<Edge>(points.Count - 1);

            // Assemble edges.
            for (int i = 0; i < edges.Capacity; i++)
            {
                var edge = new Edge() { VertexIndex = i };

                // We can only have 2 edges per vertex.
                edge.ConnectingVertexesIndexes = new List<int>(2);

                for (int j = i + 1; j < matrix.N; j++)
                {
                    // Scan only upper matrix.
                    if (matrix[i, j] != 0d)
                    {
                        edge.ConnectingVertexesIndexes.Add(j);
                    }

                }

                edges.Add(edge);
            }

            var bmp = GraphPlotter.Plot(points, edges, 1024 * 5, 768 * 3);
            bmp.Save(plotFileName + ".png");
            bmp.Dispose();
        }

        /// <summary>
        /// Creates an adjacency matrix where each [i,j]
        /// represents the distance between the point 'i'
        /// and point 'j'.
        /// </summary>
        /// <param name="points">List of points.</param>
        /// <returns>The adjacency matrix.</returns>
        static MatrizAdjacenciaSimetrica<double> AssembleMatrix(List<PointF> points)
        {
            var matrix = new MatrizAdjacenciaSimetrica<double>(points.Count);

            for (int i = 0; i < matrix.N - 1; i++)
            {
                //matrix.Set(i, i, 0);

                for (int j = i + 1; j < matrix.N; j++)
                {
                    matrix.Set(i, j, Distance(points[i], points[j]));
                }
            }

            // Last row, last column.
            //matrix.Set(matrix.N - 1, matrix.N - 1, 0);

            return matrix;
        }

        /// <summary>
        /// Calculates the euclidian distance between two points.
        /// </summary>
        /// <param name="p1">Point 1.</param>
        /// <param name="p2">Point 2.</param>
        /// <returns>The distance between p1 and p2.</returns>
        static double Distance(PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

    }
}
