using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;
using Plotter;
using static Plotter.GraphPlotter;

namespace ProjetoPO
{
    struct Customer
    {
        public int Id { get; set; }

        public PointD Coord { get; set; }

        public int Demand { get; set; }
    }


    class Distribuicao : Cplex.LazyConstraintCallback
    {
        public static Cplex model;
        public static MatrizAdjacenciaSimetrica<INumVar> X;

        // Callback Main.
        public override void Main()
        {
            // Cut subtours using 3rd party library.
        }

        public static void Main(string[] args)
        {

            //*******************************************************
            //  Obtem os dados de arquivo externo
            //
            //*******************************************************

            var files = Directory.GetFiles(@"./distribuicao/");

            foreach (var file in files)
            {
                if (Path.GetFileNameWithoutExtension(file) != "C101")
                    continue;

                Solve(file);
            }

            Console.WriteLine("Press any key to close the program...");
            Console.ReadKey(true);

        }

        private static void Solve(string filePath)
        {
            int vehicleNumber, capacity;
            var customers = ReadFile(filePath, out vehicleNumber, out capacity);
            var matrix = AssembleMatrix(customers);

            vehicleNumber = 10;

            model = new Cplex();

            //
            // Create decision variable X
            X = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    //If single-customer routes are not allowed, all used variables are binary; 
                    //otherwise, all customers variables are binary and all depot-leaving variables are in {0, 1, 2} set.
                    X[i, j] = model.BoolVar();
                }

            }

            //----------------------------------------------------------------------------
            // RESTRICTIONS
            //----------------------------------------------------------------------------


            // Forces that the depot to have 'vehicleNumber' edges departing/arriving.
            // Equivalent to restriction (1.17) or (1.23) in the book [p. 14]
            {
                var exp = model.LinearNumExpr();

                for (int j = 1; j < X.N; j++)
                {
                    exp.AddTerm(1.0, X[0, j]);
                }

                // Each vertex must connect to another one.
                model.AddEq(exp, 2 * vehicleNumber);
            }
            // This is not necessary using the undirected model from page 14
            //{
            //    var exp = model.LinearNumExpr();

            //    for (int i = 1; i < X.N; i++)
            //    {
            //        exp.AddTerm(1.0, X[i, 0]);
            //    }

            //    // Each vertex must connect to another one.
            //    model.AddEq(exp, 2 * vehicleNumber);
            //}

            // Forces every vertex execept the zeroth to connect to another one.
            // See restrictions (1.16) and (1.17) or (1.22) and (1.23) in the book [p. 14]
            for (int i = 1; i < X.N; i++)
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

            

            //----------------------------------------------------------------------------
            // OBJECTIVE FUNCTION
            //----------------------------------------------------------------------------

            // See item (1.15) or (1.21) on page 14 of the book
            var of = model.LinearNumExpr();

            for (int i = 0; i < matrix.LinearSize; i++)
            {
                //Accessing as linear vector rather than matrix
                of.AddTerm(matrix[i], X[i]);
            }
            //for (int i = 1; i < matrix.N; i++)
            //{
            //    for (int j = i; j < matrix.N; j++)
            //    {
            //        of.AddTerm(matrix[i, j], X[i, j]);
            //    }
            //}

            //Minimize
            model.AddMinimize(of);

            Console.WriteLine("[Solving...]");

            // Use callback to cut subtours.
            model.Use(new Distribuicao());

            var solved = model.Solve();

            Console.WriteLine("[Solved]");

            if (!solved)
            {
                Console.WriteLine("[No solution]\n");

                Console.WriteLine(model.GetStatus());

                return;
            }

            Console.WriteLine("Solution status: " + model.GetStatus());
            Console.WriteLine("Objective value: " + model.ObjValue);
            //Console.WriteLine("\nBinary Graph:");
            Console.WriteLine("---------------\n");

            Console.Write("    ");
            for (int i = 0; i < X.N; i++) Console.Write(i + " ");
            Console.WriteLine();
            Console.Write("  +");
            for (int i = 0; i < X.N; i++) Console.Write("--");
            Console.WriteLine();

            //Console.WriteLine(X.ToString(v => model.GetValue(v).ToString()));

            var Xdouble = new MatrizAdjacenciaSimetrica<double>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    Xdouble[i, j] = model.GetValue(X[i, j]);
                }
            }

            PlotPath(Xdouble, customers.Select(p => p.Coord).ToList());
        }

        private static void SolveMinimumCars(string filePath)
        {
            int vehicleNumber, capacity;
            var customers = ReadFile(filePath, out vehicleNumber, out capacity);
            var matrix = AssembleMatrix(customers);

            model = new Cplex();

            //
            // Create decision variable X
            X = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    //If single-customer routes are not allowed, all used variables are binary; 
                    //otherwise, all customers variables are binary and all depot-leaving variables are in {0, 1, 2} set.
                    X[i, j] = model.BoolVar();
                }

            }

            //
            // Create decision variable Y
            var Y = new INumVar[vehicleNumber];

            {
                for (int i = 0; i < vehicleNumber; i++)
                {
                    Y[i] = model.BoolVar();
                }
            }

            //----------------------------------------------------------------------------
            // RESTRICTIONS TO FIND THE MINIMUM OF VEHICLES NEEDED
            //----------------------------------------------------------------------------

            for (int a = 0; a < vehicleNumber; a++)
            {
                var oQueVoceCarregaExp = model.LinearNumExpr();

                for (int i = 1; i < customers.Count; i++)
                {
                    oQueVoceCarregaExp.AddTerm(X[i, a], customers[i].Demand);
                }

                var capacidadeVeiculoExp = model.LinearNumExpr();
                capacidadeVeiculoExp.AddTerm(capacity, Y[a]);
                model.AddLe(oQueVoceCarregaExp, capacidadeVeiculoExp);
            }

            //Each customer is attended by one vehicle
            for (int i = 1; i < customers.Count; i++)
            {

                var exp = model.LinearNumExpr();
                for (int a = 0; a < vehicleNumber; a++)
                {
                    exp.AddTerm(X[i, a], 1);
                }
                model.AddEq(exp, 1);
            }

            //----------------------------------------------------------------------------
            // FIND THE MINIMUM OF VEHICLES (OBJETIVE FUNCTION)
            //----------------------------------------------------------------------------


            var of = model.LinearNumExpr();
            for (int a = 0; a < vehicleNumber; a++)
            {
                of.AddTerm(1.0, Y[a]);
            }

            model.AddMinimize(of);


            Console.WriteLine("[Solving...]");

            // Use callback to cut subtours.
            model.Use(new Distribuicao());

            var solved = model.Solve();

            Console.WriteLine("[Solved]");

            if (!solved)
            {
                Console.WriteLine("[No solution]\n");

                Console.WriteLine(model.GetStatus());

                return;
            }

            Console.WriteLine("Solution status: " + model.GetStatus());
            Console.WriteLine("Objective value: " + model.ObjValue);

            foreach (var item in Y)
            {
                Console.Write(model.GetValue(item));
            }


        }

        static void PlotPath(MatrizAdjacenciaSimetrica<double> matrix, List<PointD> points, string plotFileName = "VRP")
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

        private static List<Customer> ReadFile(string filePath, out int vehicleNumber, out int capacity)
        {
            var customers = new List<Customer>();

            var lines = File.ReadAllLines(filePath);

            var @params = lines[4].Split(new string[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

            vehicleNumber = int.Parse(@params[0]);
            capacity = int.Parse(@params[1]);

            // Cols:
            // Customer Id, X, Y, Demand
            for (int i = 9; i < lines.Length; i++)
            {
                if (lines[i][0] == '#') continue;

                var c = lines[i].Split(new string[] { " " }, 5, StringSplitOptions.RemoveEmptyEntries);

                customers.Add(new Customer
                {

                    Id = int.Parse(c[0]),
                    Coord = new PointD { X = double.Parse(c[1]), Y = double.Parse(c[2]) },
                    Demand = int.Parse(c[3])

                });
            }

            return customers;
        }

        static List<PointD> ReadPoints(string filePath)
        {

            var lines = File.ReadAllLines(filePath);
            var chartPoints = new List<PointD>(int.Parse(lines[0]));


            for (int i = 1; i < lines.Length; i++)
            {
                //TODO Implementar a leitura

                var X = 0;
                var Y = 0;
                var point = new PointD { X = X, Y = Y };
                chartPoints.Add(point);
            }

            return chartPoints;

        }

        private static MatrizAdjacenciaSimetrica<double> AssembleMatrix(List<Customer> customers)
        {
            var matrix = new MatrizAdjacenciaSimetrica<double>(customers.Count);

            for (int i = 0; i < matrix.N - 1; i++)
            {
                for (int j = i + 1; j < matrix.N; j++)
                {
                    matrix.Set(i, j, Distance(customers[i].Coord, customers[j].Coord));
                }
            }

            return matrix;
        }

        // Distancia euclidiana entre 2 pontos.
        private static double Distance(PointD p1, PointD p2)
        {
            if (p1.Equals(p2))
            {
                //Generally, the use of the loop arcs, (i, i),is not
                //allowed and this is imposed by defining en = +inf for all i e V [p. 6]
                return double.PositiveInfinity;
            }
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

    }
}
