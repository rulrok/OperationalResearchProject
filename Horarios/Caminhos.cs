using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoPO
{

    class MatrizAdjacenciaSimetrica<T> 
    {
        T[] matrizLinear;

        public int N { get; private set; }

        public int LinearSize {

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

        private int CalculaIndiceLinear(int i, int j) {

            if (i > j)
            {
                var aux = i;
                i = j;
                j = aux;
            }

            return (N * i) + j - ((i * (i + 1)) / 2);
            //return ((i * i) + i) / 2 + j;
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

            var files = Directory.GetFiles(@"./caminhos/");

            foreach (var file in files)
            {

                encontrarSolucao(file);
            }

            encontrarSolucao("");

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            Console.ReadKey(true);

        }

        private static void encontrarSolucao(string fileName)
        {
            /*
            
            V a in A
            Xij e {0,1}

            E(i = 0 -> A)E(j = i + 1 -> V) cij.xij

            1 - read file
            2 - fill matrix
            3 - traverse lower triangular (i*col + j + i)

            */

            throw new NotImplementedException();
        }

        private static void lerArquivo(string fileName)
        {
            throw new NotImplementedException();
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
