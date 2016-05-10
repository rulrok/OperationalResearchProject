using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;
using QuickGraph;

namespace ProjetoPO
{
    using Plotter;
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
                if (Path.GetFileNameWithoutExtension(file) != "test")
                {
                    continue;
                }
                encontrarSolucao(file);
            }

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            //Console.ReadKey(true);

        }

        private static void encontrarSolucao(string fileName)
        {

            MatrizAdjacenciaSimetrica<int> matriz;
            lerArquivo(fileName, out matriz);

            Cplex model = new Cplex();

            var X = new MatrizAdjacenciaSimetrica<INumVar>(matriz.N);
            for (int i = 0; i < matriz.N; i++)
            {
                for (int j = i; j < matriz.N; j++)
                {
                    X[i, j] = model.BoolVar();
                }
            }

            //Restrição de que todas as arestas que saem também entram
            //Fixa linha
            for (int i = 0; i < X.N; i++)
            {
                var exp = model.LinearNumExpr();

                //Varia coluna
                for (int j = 0; j < X.N; j++)
                {
                    if (i != j)
                        exp.AddTerm(1.0, X[i, j]);
                }

                model.AddEq(exp, 2.0);

            }

            //Restrição para que a diagonal sempre tenha valores 1
            {
                var exp = model.LinearNumExpr();
                for (int i = 0; i < matriz.N; i++)
                {
                    exp.AddTerm(1.0, X[i, i]);
                }
                model.AddEq(exp, matriz.N);
            }

            var resultadoNumExp = new MatrizAdjacenciaSimetrica<INumExpr>(matriz.N);

            //for (int potencia = 0; potencia < matriz.N; potencia++)
            {
                for (int i = 0; i < matriz.N; i++) //Calcula apenas a primeira linha da matriz resultante (for apenas para legibilidade)
                {
                    for (int j = 0; j < matriz.N; j++)
                    {
                        var sum = model.NumExpr();
                        for (int k = 0; k < matriz.N; k++)
                        {
                            sum = model.Sum(sum, model.Prod(X[i, k], X[i, j]));
                        }
                        resultadoNumExp[i, j] = sum;
                    }
                }
            }

            //Função objetivo
            var fo = model.LinearNumExpr();
            for (int i = 0; i < X.N; i++)
            {
                for (int j = 0; j < X.N; j++)
                {
                    fo.AddTerm(matriz[i, j], X[i, j]);
                }
            }

            //Minimize
            model.AddMinimize(fo);

            var solved = model.Solve();

            if (!solved)
            {
                return;
            }

            Console.WriteLine("Solution status = " + model.GetStatus());
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Solution found:");
            Console.WriteLine(" Objective value = " + model.ObjValue);

            Console.WriteLine();
            Console.WriteLine("Grafo:");
            Console.WriteLine(matriz);
            Console.WriteLine("---------------\n");
            Console.WriteLine("Arestas escolhidas:");
            Console.Write(X.ToString((nv) => model.GetValue(nv).ToString()));

            Console.WriteLine("---------------\n");
            Console.WriteLine("Produto numérico:");
            var Xdouble = new MatrizAdjacenciaSimetrica<double>(matriz.N);
            var resultadoDouble = new MatrizAdjacenciaSimetrica<double>(matriz.N);

            

            for (int i = 0; i < matriz.N; i++)
            {
                for (int j = i; j < matriz.N; j++)
                {
                    Xdouble[i, j] = model.GetValue(X[i, j]);
                }
            }

            Plot(Xdouble);

            //for (int count = 0; count < matriz.N; count++)
            {
                for (int i = 0; i < matriz.N; i++)
                {
                    for (int j = 0; j < matriz.N; j++)
                    {
                        double sum = 0;
                        for (int k = 0; k < matriz.N; k++)
                        {
                            sum = sum + Xdouble[i, k] * Xdouble[k, j];
                        }
                        resultadoDouble[i, j] = sum;
                    }
                }
            }
            Console.Write(resultadoDouble);

            Console.WriteLine("---------------\n");
            Console.WriteLine("Resultado num exp:");
            Console.Write(resultadoNumExp.ToString((t) => model.GetValue(t).ToString()));

            /*
            int numComponentes = calcComponentes(X);

            if (numComponentes == 1) solved
            else {

            foreach (component) {



            }

            }
            */

        }

        private static void lerArquivo(string fileName, out MatrizAdjacenciaSimetrica<int> matriz)
        {
            var lines = File.ReadAllLines(fileName);

            lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            var name = lines[0];
            var type = lines[1];
            //line 2 = comments
            var dimension = lines[3].Split(':');
            matriz = new MatrizAdjacenciaSimetrica<int>(int.Parse(dimension[1]));
            //line 4 = EDGE_WEIGHT_TYPE
            //line 5 = EDGE_WEIGHT_FORMAT
            //line 6 = EDGE_WEIGHT_SECTION

            for (int fileLineCount = 7, i = 0; i < lines.Length; fileLineCount++, i++)
            {
                var columns = lines[fileLineCount].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (columns[0].Equals("EOF"))
                {
                    break;
                }
                matriz.Set(i, i, 0);
                for (int fileColumnCount = 0, j = i + 1; fileColumnCount < columns.Length; fileColumnCount++, j++)
                {
                    matriz.Set(i, j, int.Parse(columns[fileColumnCount]));
                }
            }
        }

        private static void TestMatrixes()
        {
            var nCols = 4;
            var nRows = 4;
            var m = new int[nCols * nRows];
            var k = 0;

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    m[i * nCols + j] = k++;
                }
            }


            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    Console.Write(String.Format("[{0:D2}]", m[i * nCols + j]));
                }

                Console.WriteLine();
            }



            var upper = new int[(nCols * (nCols + 1)) / 2];

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    int x = ((i * i) + i) / 2 + j;
                    upper[x] = m[i * nCols + j];
                }
            }


            Console.WriteLine("Victor:");
            for (int i = 0; i < upper.Length; i++)
            {
                Console.Write(string.Format("[{0:D2}]", upper[i]));
            }


            Console.WriteLine("Mr");
            var mr = new MatrizAdjacenciaSimetrica<int>(nRows);
            k = 0;
            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    mr.Set(i, j, k++);
                }
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    Console.Write(String.Format("[{0:D2}]", mr.Get(i, j)));
                }

                Console.WriteLine();
            }

            Console.WriteLine();
            for (int i = 0; i < mr.LinearSize; i++)
            {
                Console.Write(string.Format("[{0:D2}]", mr[i]));
            }
        }

        static void Components()
        {
            // The graph.
            Vertex[,] G = new Vertex[,] {

            { 1, 1, 0, 0, 0 }, // each line being an edge.
            { 1, 0, 1, 0, 0 },
            { 0, 1, 1, 0, 0 },
            { 0, 0, 0, 1, 1 },
            { 0, 0, 0, 1, 1 }

            };

            var gVertexes = new Vertex[] { 1, 2, 3, 4, 5 };
            var vIndex = new int[] { 0, 0, 0, 0, 0 };

            // Each list contains a set of vertexes
            // that form a subtour in the given G.
            var components = new List<List<Vertex>>();

            // Contains the vertexes that compose
            // the current path being explored.
            var stack = new Stack<Vertex>();

            int index = 0;


            foreach (Vertex v in gVertexes)
            {

            }

        }

        static void Plot<T>(MatrizAdjacenciaSimetrica<T> matrix) where T : struct, IComparable
        {
            // We have N vertexes.
            var vertexes = new List<Point>(matrix.N);

            // And we have at most "number of vertexes" edges.
            // The last vertex does not have to analysed because
            // new edges can only happen if a vertex connects
            // to a vertex with an index greater than its own,
            // i. e., if vertex 5 connects to vertex 4, than when
            // analysing vertex 4 the edge will be included.
            var edges = new List<Edge>(vertexes.Capacity - 1);

            // Randomly distribute the N vertexes in a 10x10 cartesian plane.
            var rand = new Random();
            for (int i = 0; i < matrix.N; i++)
            {
                vertexes.Add(new Point { X = rand.Next(0, 21), Y = rand.Next(1, 21) });
            }

            // Assemble edges.
            for (int i = 0; i < matrix.N - 1; i++)
            {
                var edge = new Edge() { VertexIndex = i };
                // We can only have 2 edges per vertex.
                edge.ConnectingVertexesIndexes = new List<int>(2);


                for (int j = i + 1; j < matrix.N; j++)
                {
                    // Scan only upper matrix.
                    // Might blow up on CompareTo if T is Int.
                    if (matrix[i,j].CompareTo(0.0) != 0)
                    {
                        edge.ConnectingVertexesIndexes.Add(j);
                    }

                }

                edges.Add(edge);
            }


            var bmp = GraphPlotter.Plot(vertexes, edges, 1024, 768);
            bmp.Save("graph.png");
            bmp.Dispose();

        }
    }
}
