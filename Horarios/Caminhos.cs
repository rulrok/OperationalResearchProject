using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ILOG.Concert;
using ILOG.CPLEX;

/*
iterativo
+
callback (cplex namespace)
-> dizer que vertice nao pode ir para ele mesmo.
*/

namespace ProjetoPO
{
    using Plotter;
    using System.Drawing;
    using static Plotter.GraphPlotter;
    using System.Globalization;
    using System.Diagnostics;
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

                if (args.Length > 0
                        && args[0] == "-it")
                {
                    Solve(file, outputIterations: true);

                }
                else {
                    Solve(file, outputIterations: false);
                }
            }

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            Console.ReadKey(true);

        }

        private static void Solve(string filePath, bool outputIterations)
        {
            // Transform points from file to actual PointD
            // instances that can be used to plot the chart.
            var points = ReadPoints(filePath);

            var matrix = AssembleMatrix(points);

            if (matrix.N < 40)
            {
                Console.WriteLine("Grafo: ");
                Console.WriteLine(matrix);
            }

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

            Console.WriteLine("[Solving...]");

            model.SetOut(TextWriter.Null);
            var solved = model.Solve();

            Console.WriteLine("[Solved]");

            if (!solved)
            {
                Console.WriteLine("[No solution]\n");
                return;
            }

            Console.WriteLine("Solution status: " + model.GetStatus());
            Console.WriteLine("Objective value: " + model.ObjValue);
            Console.WriteLine("\nBinary Graph:");
            Console.WriteLine("---------------");

            if (X.N < 40)
            {
                Console.Write(X.ToString((nv) => model.GetValue(nv).ToString()));    
            }
            else
            {
                Console.WriteLine("Graph is too large to output.");
            }

            Console.WriteLine("---------------\n");

            var Xdouble = new MatrizAdjacenciaSimetrica<double>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    Xdouble[i, j] = model.GetValue(X[i, j]);
                }
            }


            int nameIdx = 1;
            var name = "TSP_";

            PlotPath(Xdouble, points, name + nameIdx);
            nameIdx++;

            var sw = new Stopwatch();
            sw.Start();

            while (!FindTours(Xdouble, model, X))
            {
                // Keep on solving.
                model.Solve();

                if (!solved)
                {
                    Console.WriteLine("[No solution]\n");
                    return;
                }

                // Update matrix of connections.
                for (int i = 0; i < matrix.N; i++)
                {
                    for (int j = i; j < matrix.N; j++)
                    {
                        Xdouble[i, j] = model.GetValue(X[i, j]);
                    }
                }

                if (outputIterations)
                {
                    PlotPath(Xdouble, points, name + nameIdx);
                    nameIdx++;
                }
            };

            sw.Stop();
            Console.WriteLine("It took: " + sw.Elapsed.TotalSeconds + " seconds to solve.");

            if (!outputIterations)
            {
                PlotPath(Xdouble, points, name + nameIdx);
            }
        }


        static bool FindTours(MatrizAdjacenciaSimetrica<double> matrix, Cplex model, MatrizAdjacenciaSimetrica<INumVar> X)
        {
            // Stores which vertexes in the graph
            // the algorithm visited in all iterations.
            var visited = new bool[matrix.N];

            // We must traverse the graph
            // at least once, so do-while
            // is better suited.
            do {

                // First we find the first vertex was not visited
                // so we know where to start from.
                // This way the algorithm won't revist nodes
                // and will potentially find new tours.
                var firstNotVisited = Array.IndexOf(visited, false);

                // This holds the vertex that were visited
                // in a specific iteration only.
                var visitedInCycle = new bool[matrix.N];

                // This holds the vertex present in a
                // cycle in order so that we know the
                // path, which we can't infer only from
                // the ones that were visited.
                var vertexesInCycle = new List<int>(matrix.N);

                dfs(matrix, firstNotVisited, firstNotVisited, visitedInCycle, vertexesInCycle);

                /////////////////////////////////
                // Add restriction based on    //
                // vertexes in vertexesInCycle //
                /////////////////////////////////

                if (vertexesInCycle.Count != matrix.N)
                {
                    var exp = model.LinearNumExpr();

                    int i = 0;
                    for (; i < vertexesInCycle.Count - 1; i++)
                    {
                        // Connects each vertex to the next in the list.
                        // Assuming vertexesInCyle equals
                        // [a, b, c, d], then the expression
                        // will be of the form
                        // X[a,b] + X[b,c] + X[c,d] + X[d,a] <= vertexsInCycle.Count.
                        exp.AddTerm(1.0, X[vertexesInCycle[i], vertexesInCycle[i + 1]]);
                    }

                    // Connects the last to the first,
                    // which adds the "... + X[d,a]" in the expression.
                    exp.AddTerm(1.0, X[vertexesInCycle[i], vertexesInCycle[0]]);

                    model.AddLe(exp, vertexesInCycle.Count - 1);
                }
                else
                {
                    // Then our cycle contains all the vertexes
                    // which means
                    // OPTIMAL SOLUTION WAS FOUND.

                    //return true;

                    // Logs the vertexes connections
                    // in the format 'x => y' in 
                    // ascending order.
                    var file = new StreamWriter(File.OpenWrite("saida.tsp"));

                    var sb = new StringBuilder(vertexesInCycle.Count * 5);

                    for (int i = 0; i < matrix.N; i++)
                    {
                        sb.Append(vertexesInCycle[i]);
                        sb.Append(" => ");

                        var idx = vertexesInCycle.IndexOf(i);

                        if (idx < matrix.N - 1)
                        {
                            file.WriteLine(vertexesInCycle[idx] + " => " + vertexesInCycle[idx + 1]);
                        }
                        else
                        {
                            file.WriteLine(vertexesInCycle[idx] + " => " + vertexesInCycle[0]);
                        }
                    }

                    sb.Append(vertexesInCycle[0]);
                    file.WriteLine(sb.ToString());

                    file.Close();

                    return true;

                }

                for (int i = 0; i < visitedInCycle.Length; i++)
                {
                    if (visitedInCycle[i])
                    {
                        visited[i] = true;
                    }
                }

                // Traverse until the algorithm visited
                // all vertexes in the graph.
            } while (visited.Where(v => v == true).Count() < visited.Length);


            // The algorithm visited all vertex no cycle
            // cycle containing all of them was found.
            return false;
        }

        /// <summary>
        /// Walks through the graph until a cycle is completed.
        /// </summary>
        /// <param name="matrix">The graph connectivity matrix.</param>
        /// <param name="src">The source vertex.</param>
        /// <param name="current">The current vertex.</param>
        /// <param name="visited">Vertexes that were visited.</param>
        /// <param name="vertexesInCycle">Vertexes present in the cycle, in order.</param>
        static void dfs(MatrizAdjacenciaSimetrica<double> matrix, int src, int current, bool[] visited, List<int> vertexesInCycle)
        {
            // Mark that the vertex was visited.
            visited[current] = true;

            // Add the node to the list of vertexes
            // in the cycle.
            vertexesInCycle.Add(current);

            for (int j = 0; j < matrix.N; j++)
            {
                if (j != src &&
                    matrix[current, j] > 0)
                {
                    if (!visited[j])
                    {
                        // Keep going.
                        dfs(matrix, current, j, visited, vertexesInCycle);
                    }

                    // Two ways to get here:
                    //
                    // 1 - 'j' was already visited.
                    // In this case, we are going back to a node already visited.
                    // This means 'current' is the last vertex of a cycle
                    // and connects to the origin.
                    // No need to further walk the graph because each vertex
                    // connect to only one other.
                    //
                    // 2 - the call to dfs returned.
                    // In this case, we visited the only 'current'
                    // could be connected to. Again, no need
                    // to further walk the graph.
                    return;
                }
            }
        }

        /// <summary>
        /// Reads file and plots the vertexes.
        /// </summary>
        /// <param name="filePath">Path to graph file.</param>
        /// <param name="plotFileName">Name of the plot image that will be created.</param>
        /// <returns>List of points read from file.</returns>
        static List<PointD> ReadPoints(string filePath, bool plot = true, string plotFileName = "graphVertexes")
        {

            var lines = File.ReadAllLines(filePath);
            var chartPoints = new List<PointD>(int.Parse(lines[0]));


            for (int i = 1; i < lines.Length; i++)
            {
                var xy = lines[i].Split(' ');
                var X = double.Parse(xy[0], CultureInfo.InvariantCulture.NumberFormat);
                var Y = double.Parse(xy[1], CultureInfo.InvariantCulture.NumberFormat);
                var point = new PointD { X = X, Y = Y };
                chartPoints.Add(point);
            }


            if (plot)
            {
                var bmp = Plot(chartPoints, new List<Edge>(), 1024 * 5, 768 * 3);
                bmp.Save(plotFileName + ".png");
                bmp.Dispose();
            }

            return chartPoints;

        }

        static void PlotPath(MatrizAdjacenciaSimetrica<double> matrix, List<PointD> points, string plotFileName = "TSP")
        {
            double weight = 0;

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
                        weight += Distance(points[i], points[j]);
                    }

                }

                edges.Add(edge);
            }

            var bmp = Plot(points, edges, 1024 * 5, 768 * 3);
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
        static MatrizAdjacenciaSimetrica<double> AssembleMatrix(List<PointD> points)
        {
            var matrix = new MatrizAdjacenciaSimetrica<double>(points.Count);

            for (int i = 0; i < matrix.N - 1; i++)
            {
                for (int j = i + 1; j < matrix.N; j++)
                {
                    matrix.Set(i, j, Distance(points[i], points[j]));
                }
            }

            return matrix;
        }

        /// <summary>
        /// Calculates the euclidian distance between two points.
        /// </summary>
        /// <param name="p1">Point 1.</param>
        /// <param name="p2">Point 2.</param>
        /// <returns>The distance between p1 and p2.</returns>
        static double Distance(PointD p1, PointD p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

    }
}
