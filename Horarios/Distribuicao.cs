using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public static MatrizAdjacenciaSimetrica<double> matrix;
        public static List<Customer> customers;
        private static int callbackCount = 0;

        // Callback Main.
        public override void Main()
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("\tCorta sub-tours");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            var Xint = new MatrizAdjacenciaSimetrica<int>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    Xint[i, j] = (int)GetValue(X[i, j]);
                }
            }

            SCC scc = new BiconnectedComponents(Xint);
            var components = scc.FindComponents();
            PlotPath(Xint, customers.Select(p => p.Coord).ToList(), "VRP_" + callbackCount++, components);


            StringBuilder IntegerAndFeasible = new StringBuilder();
            int NoOfCustomers = 0, CAP = 0, NoOfEdges = 0, MaxNoOfCuts = 0;
            double EpsForIntegrality, MaxViolation = 0;
            int Demand = 0, EdgeTail = 0, EdgeHead = 0;
            double EdgeX = 0;
            NativeMethods.CnstrMgrRecord MyCutsCMP = new NativeMethods.CnstrMgrRecord();
            NativeMethods.CnstrMgrRecord MyOldCutsCMP = new NativeMethods.CnstrMgrRecord();

            NativeMethods.CMGR_CreateCMgr(ref MyCutsCMP, 100);
            NativeMethods.CMGR_CreateCMgr(ref MyOldCutsCMP, 100); /* Contains no cuts initially */

            /* Allocate memory for the three vectors EdgeTail, EdgeHead, and EdgeX */
            /* Solve the initial LP */

            EpsForIntegrality = 0.0001;

            do
            {
                /* Store the information on the current LP solution */
                /* in EdgeTail, EdgeHead, EdgeX. */
                /* Call separation. Pass the previously found cuts in MyOldCutsCMP: */

                NativeMethods.CAPSEP_SeparateCapCuts(NoOfCustomers,
                ref Demand,
                CAP,
                NoOfEdges,
                ref EdgeTail,
                ref EdgeHead,
                ref EdgeX,
                ref MyOldCutsCMP,
                MaxNoOfCuts,
                EpsForIntegrality,
                IntegerAndFeasible,
                ref MaxViolation,
                ref MyCutsCMP);

                if (IntegerAndFeasible.Equals('0')) break; /* Optimal solution found */

                if (MyCutsCMP.Size == 0) break; /* No cuts found */

                /* Read the cuts from MyCutsCMP, and add them to the LP */
                /* Resolve the LP */


                /* Move the new cuts to the list of old cuts: */
                for (int i = 0; i < MyCutsCMP.Size; i++)
                {
                    NativeMethods.CMGR_MoveCnstr(ref MyCutsCMP, ref MyOldCutsCMP, i, 0);
                }
                MyCutsCMP.Size = 0;
            } while (true);

            return;

            foreach (var component in components)
            {
                if (!component.Contains(0))
                {
                    var exp = model.LinearNumExpr();

                    Console.WriteLine("Eliminar tour: " + string.Join(" ", component.ToArray()));

                    for (int i = 0; i < component.Count - 1; i++)
                    {
                        // Connects each vertex to the next in the list.
                        // Assuming vertexesInCyle equals
                        // [a, b, c, d], then the expression
                        // will be of the form
                        // X[a,b] + X[b,c] + X[c,d] + X[d,a] <= vertexsInCycle.Count.
                        //Console.WriteLine("Eliminar: (" + vertexesInCycle[i] + "," + vertexesInCycle[i + 1] + ")");
                        exp.AddTerm(1.0, X[component[i], component[i + 1]]);
                    }

                    // Connects the last to the first,
                    // which adds the "... + X[d,a]" in the expression.
                    exp.AddTerm(1.0, X[component.Last(), component.First()]);
                    //Console.WriteLine("Eliminar: (" + vertexesInCycle[i] + "," + vertexesInCycle[0] + ")");

                    //Console.WriteLine("Coun - 1 = " + (vertexesInCycle.Count - 1));
                    //model.AddLe(exp, component.Count - 1);

                }
            }

            Console.WriteLine("\n\n");
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
            customers = ReadFile(filePath, out vehicleNumber, out capacity);
            matrix = AssembleMatrix(customers);

            vehicleNumber = 10;

            model = new Cplex();

            //
            // Create decision variable X
            #region Decision variable
            X = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    //If single-customer routes are not allowed, all used variables are binary; 
                    //otherwise, all customers variables are binary and all depot-leaving variables are in {0, 1, 2} set.
                    if (i == 0)
                        X[i, j] = model.IntVar(0, 2);
                    else
                        X[i, j] = model.BoolVar();
                }

            }
            #endregion

            //----------------------------------------------------------------------------
            // RESTRICTIONS
            //----------------------------------------------------------------------------


            // Forces that the depot to have 'vehicleNumber' edges departing/arriving.
            // Equivalent to restriction (1.17) or (1.23) in the book [p. 14]
            #region Restriction 1
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
            #endregion

            // Forces every vertex execept the zeroth to connect to another one.
            // See restrictions (1.16) and (1.17) or (1.22) and (1.23) in the book [p. 14]
            #region Restriction 2
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
            #endregion



            //----------------------------------------------------------------------------
            // OBJECTIVE FUNCTION
            //----------------------------------------------------------------------------

            #region Objective Function
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
            #endregion

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

            #region Print X matrix
            //Console.Write("    ");
            //for (int i = 0; i < X.N; i++) Console.Write(i + " ");
            //Console.WriteLine();
            //Console.Write("  +");
            //for (int i = 0; i < X.N; i++) Console.Write("--");
            //Console.WriteLine();

            //Console.WriteLine(X.ToString(v => model.GetValue(v).ToString()));
            #endregion

            #region Plot graph
            var Xdouble = new MatrizAdjacenciaSimetrica<int>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    Xdouble[i, j] = (int)model.GetValue(X[i, j]);
                }
            }

            PlotPath(Xdouble, customers.Select(p => p.Coord).ToList());
            #endregion
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

        static void PlotPath(MatrizAdjacenciaSimetrica<int> matrix, List<PointD> points, string plotFileName = "VRP", List<List<int>> components = null)
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

            var bmp = Plot(points, edges, 1024 * 5, 768 * 3, components);
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
