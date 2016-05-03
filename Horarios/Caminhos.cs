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
        int N;

        public MatrizAdjacenciaSimetrica(int dimensao)
        {
            N = dimensao;

            int tamanhoLinear;

            if (dimensao == 1)
                tamanhoLinear = 1;
            else {
                //Assumindo dimensao par
                tamanhoLinear = (dimensao + 1) * (dimensao / 2);
                if (dimensao % 2 != 0)
                {
                    //Se impar
                    tamanhoLinear += (int)Math.Ceiling(dimensao / 2.0);
                }
            }
            matrizLinear = new T[tamanhoLinear];

        }

        public T get(int i, int j)
        {
            return matrizLinear[calculaIndiceLinear(i, j)];
        }

        public T set(int i, int j, T novoValor)
        {
            int indice = calculaIndiceLinear(i, j);
            T valorAntigo = matrizLinear[indice];
            matrizLinear[indice] = novoValor;

            return valorAntigo;
        }

        private int calculaIndiceLinear(int i, int j) {
            if (i > j)
                j = i;
            return (N * i) + j - ((i * (i + 1)) / 2);
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

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            Console.ReadKey(true);

        }

        private static void encontrarSolucao(string fileName)
        {
            throw new NotImplementedException();
        }

        private static void lerArquivo(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
