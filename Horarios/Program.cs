using System;
using System.IO;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Diagnostics;

namespace Horarios
{
    class Program
    {
        static void Main(string[] args)
        {

            //*******************************************************
            //  Obtem os dados de arquivo externo
            //  O método preenche as matrizes e variáveis necessárias
            //
            //*******************************************************

            var files = Directory.GetFiles(@"./horarios/");
            var stdout = Console.Out;

            foreach (var file in files)
            {
                double[,] a; //Matriz de aulas
                double[,,] i; //Matriz de indisponibilidades
                double[,] l;
                int P, T, D, H; //Indices para Professor, Turma, Dia e Horário

                lerArquivo(out a, out i, out l, out P, out T, out D, out H, file);

                Cplex model = new Cplex();

                //*******************************************************
                //  Define a variável de decisão e função objetivo
                //
                //*******************************************************
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

                // Add funcao objetivo para garantir que todas as aulas sejam dadas
                var foTodasAsAulas = model.LinearNumExpr();

                for (int d = 0; d < D; d++)
                {
                    for (int h = 0; h < H; h++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            for (int p = 0; p < P; p++)
                            {

                                // comeca gravacao aqui.
                                foTodasAsAulas.AddTerm(1.0, x[d, h, t, p]);

                            }
                        }
                    }
                }



                //*******************************************************
                //  Adiciona as restrições do modelo
                //
                //*******************************************************

                #region Restrições 1 e 2
                // Restrições 1 e 2:
                //
                // Restrição 1:
                // Um professor não pode ser alocado ao mesmo tempo em turmas diferentes
                // Adiciona DxHxP restrições ao modelo
                //
                // Restricao 2:
                // O professor nao pode ser alocado para dar aula
                // quando ele esta indisponivel.
                // E(t = 1 -> T) 1*Xdhpt = 0, para todo d, h, p, se i[d, h, p] = 1.





                for (int d = 0; d < D; d++)
                {
                    for (int h = 0; h < H; h++)
                    {
                        for (int p = 0; p < P; p++)
                        {
                            ILinearNumExpr exp = model.LinearNumExpr();

                            for (int t = 0; t < T; t++)
                            {
                                exp.AddTerm(1.0, x[d, h, t, p]);
                            }

                            if (i[d, h, p] == 1)
                            {
                                //Restrição 2
                                model.AddEq(exp, 0);
                            }
                            else
                            {
                                //Restrição 1
                                model.AddLe(exp, 1);
                            }


                        }

                    }
                }
                #endregion

                #region Restrição 3
                // Restrição 3: 
                // A quantidade de aulas de cada professor por turma.
                // e.g., Humberto, 7 periodo, 4 aulas de PO.
                // Adiciona TxP restricoes ao modelo.
                for (int t = 0; t < T; t++)
                {
                    for (int p = 0; p < P; p++)
                    {

                        ILinearNumExpr exp = model.LinearNumExpr();

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
                #endregion

                #region Restrição 4
                // Restrição 4:
                // O professor leciona no máximo duas vezes em cada turma para cada dia
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
                #endregion

                #region Restrição 5
                // Restrição 5:
                // Uma turma não pode ter mais de um professor no mesmo dia e horário
                // Adiciona DxHxT restrições ao modelo
                for (int d = 0; d < D; d++)
                {
                    for (int h = 0; h < H; h++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            ILinearNumExpr exp = model.LinearNumExpr();

                            for (int p = 0; p < P; p++)
                            {

                                exp.AddTerm(1.0, x[d, h, t, p]);

                            }

                            // Um turma não pode ter no mesmo dia e horário mais de um professor alocado
                            model.AddLe(exp, 1);
                        }

                    }
                }
                #endregion

                #region Restrição 6

                // y[d,p] = num aulas prof P dia D.
                var y = new INumVar[D, P];

                for (int d = 0; d < D; d++)
                {
                    for (int p = 0; p < P; p++)
                    {
                        y[d, p] = model.BoolVar();
                    }
                }


                for (int d = 0; d < D; d++)
                {
                    for (int p = 0; p < P; p++)
                    {
                        var exp = model.LinearNumExpr();

                        for (int h = 0; h < H; h++)
                        {
                            for (int t = 0; t < T; t++)
                            {
                                exp.AddTerm(1, x[d, h, t, p]);
                            }
                        }

                        model.Add(model.IfThen(model.Eq(exp, 1), model.Eq(y[d, p], 1)));
                        model.Add(model.IfThen(model.Eq(exp, 0), model.Eq(y[d, p], 0)));
                        model.Add(model.IfThen(model.Ge(exp, 2), model.Eq(y[d, p], 0)));

                    }
                }
                #endregion

                //*******************************************************
                //  Função objetivo para minimizar o número de aulas
                //  isoladas
                //
                //*******************************************************
                var foMinAulasIsolada = model.LinearNumExpr();
                for (int p = 0; p < P; p++)
                {
                    for (int d = 0; d < D; d++)
                    {
                        ILinearNumExpr exp = model.LinearNumExpr();

                        for (int t = 0; t < T; t++)
                        {
                            for (int h = 0; h < H; h++)
                            {
                                exp.AddTerm(1.0, x[d, h, t, p]);
                            }
                        }

                        model.Add(model.IfThen(model.Eq(exp, 1), model.Eq(y[d, p], 1)));

                        //Multiplica por -1 para 'puxar' para baixo as aulas isoladas
                        //de forma que elas não apareçam, quando possível, na solução encontrada
                        foMinAulasIsolada.AddTerm(-1, y[d, p]);

                    }
                }

                //*******************************************************
                //  Adiciona as funções objetivos ao modelo
                //
                //*******************************************************
                model.AddMaximize(model.Sum(foMinAulasIsolada, foTodasAsAulas));


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

                var fileName = Path.GetFileNameWithoutExtension(file);
                var outputStream = new FileStream("./saidas/" + fileName + "_saida.txt", FileMode.OpenOrCreate, FileAccess.Write);
                var writer = new StreamWriter(outputStream);


                var sw = new Stopwatch();

                sw.Start();

                var solve = model.Solve();

                sw.Stop();

                Console.SetOut(writer);

                Console.WriteLine("Solving time: " + sw.Elapsed.TotalSeconds + ".");

                //Solver the problem
                if (solve)
                {
                    Console.WriteLine("Solution status = " + model.GetStatus());
                    Console.WriteLine("--------------------------------------------");
                    Console.WriteLine();
                    Console.WriteLine("Solution found:");
                    Console.WriteLine(" Objective value = " + model.ObjValue);
                    Console.WriteLine(" DxHxT = {0}", D * H * T);
                    Console.WriteLine();

                    Console.WriteLine();
                    Console.WriteLine("Mostrar agenda dos professores? [y/n]");
                    var key = Console.ReadKey(true);
                    if (key.Key.Equals(ConsoleKey.Y))
                    {
                        exibirTabelasProfessores(P, T, D, H, model, x, y);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Mostrar agenda das turmas? [y/n]");
                    key = Console.ReadKey(true);
                    if (key.Key.Equals(ConsoleKey.Y))
                    {
                        exibirTabelasTurmas(P, T, D, H, model, x);
                    }
                }
                else {
                    Console.WriteLine("No solution found.");
                    Console.WriteLine("Solution status: " + model.GetStatus());
                }

                //Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
                //Console.ReadKey();
                Console.Out.Flush();
                writer.Dispose();
                outputStream.Dispose();
                Console.SetOut(stdout);
            }
        }

        #region Métodos para exibir as tabelas no console

        private static void exibirTabelasProfessores(int P, int T, int D, int H, Cplex model, INumVar[,,,] x, INumVar[,] y)
        {
            for (int p = 0; p < P; p++)
            {
                Console.WriteLine();
                Console.WriteLine("Agenda do professor {0}", p);
                //Para cada professor em cada turma, imprimimos a sua tabela de horários
                Console.WriteLine("+---------------------------------------+");
                Console.WriteLine("|   S   |   T   |   Q   |   Q   |   S   |");
                Console.WriteLine("+---------------------------------------+");
                for (int h = 0; h < H; h++)
                {
                    Console.Write("|");
                    for (int d = 0; d < D; d++)
                    {
                        int turmasLecionadasNoMesmoMomento = 0;
                        for (int t = 0; t < T; t++)
                        {
                            var turma = x[d, h, t, p];

                            double professorLecionaNessaTurmaNesseDiaEHorario = model.GetValue(turma);
                            if (professorLecionaNessaTurmaNesseDiaEHorario == 1)
                            {
                                Console.Write("  T{0:D2}  |", t);
                                turmasLecionadasNoMesmoMomento++;
                            }
                            else if (professorLecionaNessaTurmaNesseDiaEHorario == 0)
                            {
                                //Para esse dia e horário, a turma t não está alocada para o professor p
                                //Não fazemos nada por enquanto

                            }
                            else {
                                Console.WriteLine("Modelo inconsistente");
                            }
                        }
                        if (turmasLecionadasNoMesmoMomento == 0)
                        {
                            Console.Write("  ---  |");
                        }
                        else if (turmasLecionadasNoMesmoMomento > 1)
                        {
                            Console.WriteLine("Modelo inconsistente");
                        }

                    } //For D
                    Console.WriteLine("");
                } //For H
                Console.WriteLine("+---------------------------------------+");

            } //For P

            for (int d = 0; d < D; d++)
            {
                for (int p = 0; p < P; p++)
                {
                    if (model.GetValue(y[d, p]) == 1)
                    {
                        Console.WriteLine("Professor {0} no dia {1} tem aula isolada!", p, d);
                    }
                }
            }
        }

        private static void exibirTabelasTurmas(int P, int T, int D, int H, Cplex model, INumVar[,,,] x)
        {
            for (int t = 0; t < T; t++)
            {
                Console.WriteLine();
                Console.WriteLine("Agenda da turma {0}", t);
                //Para cada professor em cada turma, imprimimos a sua tabela de horários
                Console.WriteLine("+---------------------------------------+");
                Console.WriteLine("|   S   |   T   |   Q   |   Q   |   S   |");
                Console.WriteLine("+---------------------------------------+");
                for (int h = 0; h < H; h++)
                {
                    Console.Write("|");
                    for (int d = 0; d < D; d++)
                    {
                        int turmasLecionadasNoMesmoMomento = 0;
                        for (int p = 0; p < P; p++)
                        {
                            var professor = x[d, h, t, p];

                            double turmaContemProfessorNesseDiaEHorario = model.GetValue(professor);
                            if (turmaContemProfessorNesseDiaEHorario == 1)
                            {
                                Console.Write("  P{0:D2}  |", p);
                                turmasLecionadasNoMesmoMomento++;
                            }
                            else if (turmaContemProfessorNesseDiaEHorario == 0)
                            {
                                //Para esse dia e horário, a turma t não está alocada para o professor p
                                //Não fazemos nada por enquanto

                            }
                            else {
                                Console.WriteLine("Modelo inconsistente");
                            }
                        }
                        if (turmasLecionadasNoMesmoMomento == 0)
                        {
                            Console.Write("  ---  |");
                        }
                        else if (turmasLecionadasNoMesmoMomento > 1)
                        {
                            Console.WriteLine("Modelo inconsistente");
                        }

                    } //For D
                    Console.WriteLine("");
                } //For H
                Console.WriteLine("+---------------------------------------+");

            } //For P
        }
        #endregion

        #region Leitura de arquivo
        private static void lerArquivo(out double[,] a, out double[,,] i, out double[,] l, out int P, out int T, out int D, out int H, string fileName)
        {
            var lines = System.IO.File.ReadAllLines(fileName);

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

            // matriz de limites de aulas
            l = new double[P, T];

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
                } else if (columns[0].Equals("l"))
                {
                    //firstNumber = professor
                    //secondNumber = Turma
                    //thirdNumber = limite
                    l[firstNumber, secondNumber ] = thirdNumber;

                }

            }
        }
        #endregion
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
