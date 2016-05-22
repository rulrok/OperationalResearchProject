using System;
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
            }

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            Console.ReadKey(true);

        }

        private static void Solve(string filePath)
        {
            var customers = ReadFile(filePath);

            var points = ReadPoints(filePath);
            var matrix = AssembleMatrix(points);

            var model = new Cplex();

            //
            // Cria variável de decisão X
            var X = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    X[i, j] = model.BoolVar();
                }

            }

            //
            // Cria variável de decisão Y
            var Y = new MatrizAdjacenciaSimetrica<INumVar>(matrix.N);

            for (int i = 0; i < matrix.N; i++)
            {
                for (int j = i; j < matrix.N; j++)
                {
                    X[i, j] = model.BoolVar();
                }

            }

            throw new NotImplementedException();
        }

        private static List<Customer> ReadFile(string filePath)
        {
            var customers = new List<Customer>();
            int vehicleNumber, capacity;

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

        private static MatrizAdjacenciaSimetrica<double> AssembleMatrix(List<PointD> points)
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

        // Distancia euclidiana entre 2 pontos.
        private static double Distance(PointD p1, PointD p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }
}
