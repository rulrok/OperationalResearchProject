using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoPO
{
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
