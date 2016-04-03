using System;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;

namespace Horarios
{
    class Program
    {
        static void Main(string[] args)
        {
            double[,] a; //Matriz de aulas
            double[,,] i; //Matriz de indisponibilidades
            int P, T, D, H; //Indices para Professor, Turma, Dia e Horário

            //*****************************************************
            //Obtem os dados de arquivo externo
            //O método preenche as matrizes e variáveis necessárias
            //*****************************************************
            lerArquivo(out a, out i, out P, out T, out D, out H, "5_turmas.txt");

            Cplex model = new Cplex();

            // objeto que define uma variavel de decisao.
            var x = new INumVar[D, H, T, P];
            
            for (int d = 0; d < D; d++)
            {
                for (int h = 0; h < H; h++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        for (int p = 0; p < P; p++)
                        {
                            //Variável de decisão binária
                            //Para todo dhtp, X[dhtp] pertence {0, 1}
                            x[d, h, t, p] = model.BoolVar();

                        }
                    }
                }
            }

            // Add funcao objetivo.
            var fo = model.LinearNumExpr();

            for (int d = 0; d < D; d++)
            {
                for (int h = 0; h < H; h++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        for (int p = 0; p < P; p++)
                        {

                            // comeca gravacao aqui.
                            fo.AddTerm(1.0, x[d, h, t, p]);

                        }
                    }
                }
            }

            model.AddMaximize(fo);


            // Restrição 3: 
            // A quantidade de aulas de cada professor por turma.
            // e.g., Humberto, 7 periodo, 4 aulas de PO.
            // Adiciona TxP restricoes ao modelo.
            for (int t = 0; t < T; t++)
            {
                for (int p = 0; p < P; p++)
                {

                    var exp = model.LinearNumExpr();

                    // somatorio entre dia e horario.
                    for (int d = 0; d < D; d++)
                    {
                        for (int h = 0; h < H; h++)
                        {

                            exp.AddTerm(1.0, x[d, h, t, p]);

                        }
                    }

                    // O somatório tem que ser A[t][p],
                    // que é o número de aula q o prof tem que
                    // dar para uma turma.
                    model.AddEq(exp, a[t, p]);
                }
            }


            // Restrição 1:
            // Um professor não pode ser alocado ao mesmo tempo em turmas diferentes
            // Adiciona DxHxP restrições ao modelo
            for (int d = 0; d < D; d++)
            {
                for (int h = 0; h < H; h++)
                {
                    for (int p = 0; p < P; p++)
                    {

                        var exp = model.LinearNumExpr();

                        // Verifica para todas as turmas se o professor não está em mais de uma.
                        // Soma quantas vezes ele foi alocado no mesmo dia e mesmo horário.
                        for (int t = 0; t < T; t++)
                        {

                            exp.AddTerm(1.0, x[d, h, t, p]);

                        }

                        // Um professor não pode estar em mais de uma turma ao mesmo tempo.
                        // Xdhp <= 1, para todo T
                        model.AddLe(exp, 1);
                    }

                }
            }


            // R4
            // (volta gravacao aqui)
            for (int d = 0; d < D; d++)
            {
                for (int t = 0; t < T; t++)
                {
                    for (int p = 0; p < P; p++)
                    {

                        ILinearNumExpr exp = model.LinearNumExpr();

                        // verifica p tds horarios se o prof
                        // nao ta dando todas as aulas em uma turma so.
                        // Limita a 2 aulas.
                        for (int h = 0; h < H; h++)
                        {

                            exp.AddTerm(1.0, x[d, h, t, p]);

                        }

                        // limita a duas aulas no mesmo dia mesma
                        // turma e mesmo prof.
                        model.AddLe(exp, 2);
                    }

                }
            }


            // Aulas isoladas: terca feira.
            /*

            E(i = 1, H) Zi = 1
            (foto lousa)
            (a img eh pra um dia e um professor).

            (c5 - y)/5 = c5/5 - y/5
            addTerm(1/5, c5);
            addTerm(-1/5, y);
            

            INumVar y = model.IntVar(0, 5);
            INumVar z0 = model.BoolVar();
            INumVar z1 = model.BoolVar();
            INumVar z2 = model.BoolVar();
            INumVar z3 = model.BoolVar();
            INumVar z4 = model.BoolVar();
            INumVar z5 = model.BoolVar();

            INumVar C5 = model.IntVar(5, 5); model.AddEq(C5, 5);
            INumVar C4 = model.IntVar(4, 4); model.AddEq(C4, 4);
            INumVar C3 = model.IntVar(3, 3); model.AddEq(C3, 3);
            INumVar C2 = model.IntVar(2, 2); model.AddEq(C2, 2);

            ILinearNumExpr fo2 = model.LinearNumExpr();
            fo2.AddTerm(1, z1);
            model.AddMinimize(fo2);

            model.AddEq(y, 0); // num aulas.
            */


            //foto 2: eh o somatrio Ydp = EE... da foto da lousa.
            // o Z vale 1 se for aula isolada, caso contrario 0.
            // relatorio da minimizacao dos 2 jeitos (lousa e foto do projetor).
            // instancia X tantos segundos, tantas aulas.


            // terca ^



            //Solver the problem
            if (model.Solve())
            {
                Console.WriteLine("Model Feasible");
                Console.WriteLine("Solution status=" + model.GetStatus());
                Console.WriteLine("Solution value = " + model.ObjValue);
                //TODO Print the values
            }
            else {
                Console.WriteLine("Solution status=" + model.GetStatus());
            }

            Console.ReadKey();
        }

        private static void lerArquivo(out double[,] a, out double[,,] i, out int P, out int T, out int D, out int H, string fileName)
        {
            var lines = System.IO.File.ReadAllLines(@"./horarios/" + fileName);

            lines = lines.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            P = int.Parse(lines[0].Split('\t').First());
            T = int.Parse(lines[1].Split('\t').First());
            D = int.Parse(lines[2].Split('\t').First());
            H = int.Parse(lines[2].Split('\t')[1]);

            // matriz Aulas[t,p] de necessidade de aulas.
            // turma T prof P tem que dar A aulas.
            a = new double[T, P];

            // matriz de indisponibilidade.
            // No dia D, no horário H, o professor P está indisponível (1) ou não (0)
            i = new double[D, H, P];

            //As três primeiras linhas do arquivo contém os cabeçalhos
            //portanto começamos o count a partir de 3
            for (int count = 3; count < lines.Length; count++)
            {
                var columns = lines[count].Split(new[] { ' ', '\t' });

                int firstNumber = int.Parse(columns[1]);
                int secondNumber = int.Parse(columns[2]);
                int thirdNumber = int.Parse(columns[3]);

                if (columns[0].Equals("i"))
                {
                    //firstNumber = dia
                    //secondNumber = hora
                    //thirdNumber = está indisponível?
                    i[firstNumber, secondNumber, thirdNumber] = 1;
                }
                else if (columns[0].Equals("a"))
                {
                    //firstNumber = professor
                    //secondNumber = Turma
                    //thirdNumber = horas
                    a[secondNumber, firstNumber] = thirdNumber;
                }

            }
        }
    }

}


/*
p todo dhtp Xdhtp pertence {0, 1}

==============================================


R1 n pode estar em 2  : E(t = 1 -> T) Xdhtp <= 1.
lugares ao mesmo tempo

<= 1 pq o prof so pode dar 1 aula em um dia em uma hora.
> 1 = dando duas aulas ao mesmo tempo.

==============================================

R2 Indisponibilidade: E(t = 1 -> T) Xdhtp = 1 - Idhp  V dhp.
Somatorio de todas as aulas do prof no dia e na hora tem que
ser 0 (1 - Idhp).

===========================================

Matriz Atp (quantas aulas cada prof tem q dar por turma).
Atp pertence aos Z.

A(t1, p1) = 4 aulas.

Fixa t = 1, p = 1.

R3: E(d = 1 -> D) E(h = 1 -> H) Xdhtp = Atp.
(garantia de aulas p/ cada turma e professor).

===============================================

Como garantir que nao esta na mesma turma dando
total de aulas da semana.

R4 maximo de aulas: E(h = 1 -> H) Xdhtp <= 2.
Garantindo que o prof de no maximo duas aulas pra uma turma.
(evita aula tripla)

=========================================================

R5 nao pode ocorrer mais de uma aula no
mesmo dia e horario: 

fixa dia, turma e horario, e varia os profs.

E(p = 1 -> P) Xdhtp = 1 prof lecionando naquela hora.
E(p = 1 -> P)E(t = 1 -> T) Xdhtp = T, p todo d e h.

(diz que as duas sao equivalentes)
(segue falando sobre essas duas restricoes)


F.O. max E(d)E(h)E(t)E(p) Xdhtp = DxHxT


Nilda36 = 450.

if(model.getvalue(X[d][h][t][p] > 0.99)


*/
