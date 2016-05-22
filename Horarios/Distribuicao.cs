﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;
using Plotter;

namespace ProjetoPO
{
    struct Customer
    {
        public int Id { get; set; }

        public PointD Coord { get; set; }
        
        public int Demand { get; set; }
    }

    class Distribuicao
    {
        public static void Main(string[] args)
        {

            //*******************************************************
            //  Obtem os dados de arquivo externo
            //
            //*******************************************************

            var files = Directory.GetFiles(@"./distribuicao/");

            foreach (var file in files)
            {
                if (Path.GetFileNameWithoutExtension(file) != "RC101")
                    continue;

                Solve(file);
                //test
            }

            Console.WriteLine("Press any key to close the program...");
            Console.ReadKey(true);

        }

        private static void Solve(string filePath)
        {
            int vehicleNumber, capacity;
            var customers = ReadFile(filePath, out vehicleNumber, out capacity);
            var matrix = AssembleMatrix(customers);

            var model = new Cplex();

            //
            // Create decision variable X
            var X = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    X[i, j] = model.BoolVar();
                }

            }

            //
            // Create decision variable Y
            var Y = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    Y[i, j] = model.BoolVar();
                }

            }

            //----------------------------------------------------------------------------
            // RESTRICTIONS
            //----------------------------------------------------------------------------


            // Forces every vertex execept the zeroth to connect to another one.    
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

            // Forces that the depot to have 'vehicleNumber' edges departing/arriving.
            {
                var exp = model.LinearNumExpr();

                for (int j = 1; j < X.N; j++)
                {
                    exp.AddTerm(1.0, X[0, j]);
                }

                // Each vertex must connect to another one.
                model.AddEq(exp, vehicleNumber);
            }
            {
                var exp = model.LinearNumExpr();

                for (int i = 1; i < X.N; i++)
                {
                    exp.AddTerm(1.0, X[i, 0]);
                }

                // Each vertex must connect to another one.
                model.AddEq(exp, vehicleNumber);
            }

            //----------------------------------------------------------------------------
            // OBJECTIVE FUNCTION
            //----------------------------------------------------------------------------

            var of = model.LinearNumExpr();
            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    of.AddTerm(matrix[i, j], X[i, j]);
                }
            }

            //Minimize
            model.AddMinimize(of);

            Console.WriteLine("[Solving...]");

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
            Console.WriteLine("\nBinary Graph:");
            Console.WriteLine("---------------");
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
                var c = lines[i].Split(new string[] { " " }, 5, StringSplitOptions.RemoveEmptyEntries);

                customers.Add(new Customer {

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
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }
}
