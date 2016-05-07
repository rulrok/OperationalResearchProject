using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;

namespace ProjetoPO
{

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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    sb.AppendFormat("{0:D4}", Get(i, j)).Append(" ");
                }
                sb.Append("\n");
            }


            return sb.ToString();
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
                if (Path.GetFileNameWithoutExtension(file) != "brazil58")
                {
                    continue;
                }
                encontrarSolucao(file);
            }

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            Console.ReadKey(true);

        }

        private static void encontrarSolucao(string fileName)
        {

            MatrizAdjacenciaSimetrica<int> matriz;
            lerArquivo(fileName, out matriz);

            Cplex model = new Cplex();

            var X = new MatrizAdjacenciaSimetrica<INumVar>(matriz.N);
            for (int i = 0; i < matriz.N; i++)
            {
                for (int j = 0; j < matriz.N; j++)
                {
                    X[i, j] = model.BoolVar();
                }
            }

            //Fixa linha
            for (int i = 0; i < X.N; i++)
            {
                var exp = model.LinearNumExpr();

                //Varia coluna
                for (int j = 0; j < X.N; j++)
                {
                    exp.AddTerm(1.0, X[i, j]);
                }
                model.AddEq(exp, 2.0);

            }

            
            /*

            V a in A
            Xij e {0,1}

            E(i = 0 -> A)E(j = i + 1 -> V) cij.xij

            1 - read file
            2 - fill matrix
            3 - traverse lower triangular (i*col + j + i)

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
    }
}
