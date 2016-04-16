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
                if (Path.GetFileNameWithoutExtension(file) != "nilda36")
                {
                    continue;
                }

                int[,] a; //Matriz de aulas
                int[,,] i; //Matriz de indisponibilidades
                int[,] l; //Matriz de limitações de aulas
                int[,] r; //Matriz de restrições de aulas entre professores

                int P, T, D, H; //Indices para Professor, Turma, Dia e Horário

                lerArquivo(out a, out i, out l, out r, out P, out T, out D, out H, file);

                Cplex model = new Cplex();

                //*******************************************************
                //  Define a variável de decisão
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

                // Primeiro tratar germinacao;
                // Segundo tratar aulas isoladas;
                // Terceiro resto das restricoes;

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
                            model.AddLe(exp, l[p, t] > 0 ? l[p, t] : 2);
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

                #region Restrição 7

                // w[d, p, t] = num aulas prof P dia D.
                var w = new INumVar[D, P, T];

                for (int d = 0; d < D; d++)
                {
                    for (int p = 0; p < P; p++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            w[d, p, t] = model.BoolVar();
                        }
                    }
                }


                for (int d = 0; d < D; d++)
                {
                    for (int p = 0; p < P; p++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            var exp = model.LinearNumExpr();

                            for (int h = 0; h < H; h++)
                            {
                                exp.AddTerm(1, x[d, h, t, p]);
                            }

                            model.Add(model.IfThen(model.Ge(exp, 1), model.Eq(w[d, p, t], 1)));
                            model.Add(model.IfThen(model.Eq(exp, 0), model.Eq(w[d, p, t], 0)));
                        }
                    }
                }

                for (int p1 = 0; p1 < P; p1++)
                {
                    for (int p2 = 0; p2 < P; p2++)
                    {
                        if (p2 == p1)
                        {
                            continue;
                        }

                        if (r[p1, p2] > -1)
                        {
                            //Se existir uma restrição para esses dois professores
                            //

                            
                            var turma = r[p1, p2];

                            for (int d = 0; d < D; d++)
                            {
                                var exp = model.LinearNumExpr();

                                exp.AddTerm(1.0, w[d, p1, turma]);
                                exp.AddTerm(1.0, w[d, p2, turma]);

                                //Adiciona a restrição para os professores
                                model.AddLe(exp, 1);
                            }
                            
                        }
                    }
                }

                #endregion

                #region Germinada

                // O professor nao da aulas esparsas.
                //
                // i   D      D
                // 0:  1      1
                // 1:  -  =>  1
                // 2:  1      -
                //    ...    ...
                //
                // Testar do par para o impar (0 -> 1, 2 -> 3, etc).
                // Nao testar 1 -> 2, 3 -> 4 etc.

                // H = 5 -> B = 3
                // H = 4 -> B = 2
                int B = (int)Math.Ceiling(H / 2.0);

                var g = new INumVar[D, B, T, P];

                for (int d = 0; d < D; d++)
                {
                    for (int b = 0; b < B; b++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            for (int p = 0; p < P; p++)
                            {                            
                                g[d, b, t, p] = model.BoolVar();
                            }
                        }
                    }
                }


                #endregion

                #region Funções objetivos
                //*******************************************************
                //  Função objetivo para garantir que todas as aulas
                //  sejam dadas
                //
                //*******************************************************
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
                                foTodasAsAulas.AddTerm(10.0, x[d, h, t, p]);

                            }
                        }
                    }
                }

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

                //*********************************************************
                //  Função objetivo para tentar maximizar aulas germinadas
                //
                //*********************************************************
                var foGerminada = model.LinearNumExpr();
                for (int d = 0; d < D; d++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        for (int p = 0; p < P; p++)
                        {
                            var b = 0;

                            for (int h = 0; h < H / 2; h += 2)
                            {
                                var exp = model.LinearNumExpr();

                                exp.AddTerm(1.0, x[d, h, t, p]);
                                exp.AddTerm(1.0, x[d, h + 1, t, p]);

                                model.Add(model.IfThen(model.Eq(exp, 2), model.Eq(g[d, b, t, p], 1)));
                                model.Add(model.IfThen(model.Le(exp, 1), model.Eq(g[d, b, t, p], 0)));

                                foGerminada.AddTerm(0.1, g[d, b, t, p]);

                                b++;
                            }

                        }

                    }
                }

                //*******************************************************
                //  Adiciona as funções objetivos ao modelo foTodasAsAulas
                //
                //*******************************************************
                model.AddMaximize(model.Sum(foMinAulasIsolada, foGerminada, foTodasAsAulas));

                #endregion

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
                Console.ReadKey();
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
        private static void lerArquivo(out int[,] a, out int[,,] i, out int[,] l, out int[,] r, out int P, out int T, out int D, out int H, string fileName)
        {
            var lines = System.IO.File.ReadAllLines(fileName);

            lines = lines.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            P = int.Parse(lines[0].Split('\t').First());
            T = int.Parse(lines[1].Split('\t').First());
            D = int.Parse(lines[2].Split('\t').First());
            H = int.Parse(lines[2].Split('\t')[1]);

            // matriz Aulas[t,p] de necessidade de aulas.
            // turma T prof P tem que dar A aulas.
            a = new int[T, P];

            // matriz de indisponibilidade.
            // No dia D, no horário H, o professor P está indisponível (1) ou não (0)
            i = new int[D, H, P];

            // matriz de limites de aulas
            l = new int[P, T];

            //Matriz de restrições de professores no mesmo dia
            r = new int[P, P];
            for (int p1 = 0; p1 < P; p1++)
            {
                for (int p2 = 0; p2 < P; p2++)
                {
                    r[p1, p2] = -1;
                }
            }

            //As três primeiras linhas do arquivo contém os cabeçalhos
            //portanto começamos o count a partir de 3
            for (int count = 3; count < lines.Length; count++)
            {
                var columns = lines[count].Split(new[] { ' ', '\t' });

                int firstNumber = int.Parse(columns[1]);
                int secondNumber = int.Parse(columns[2]);
                int thirdNumber = int.Parse(columns[3]);

                //Permite comentários nas linhas do arquivo
                if (columns[0].StartsWith("#"))
                {
                    continue;
                }

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
                else if (columns[0].Equals("l"))
                {
                    //firstNumber = professor
                    //secondNumber = Turma
                    //thirdNumber = limite
                    l[firstNumber, secondNumber] = thirdNumber;

                }
                else if (columns[0].Equals("r"))
                {
                    //firstNumber = professor 1
                    //secondNumber = professor 2
                    //thirdNumber = turma
                    r[firstNumber, secondNumber] = thirdNumber;
                }

            }
        }
        #endregion
    }

}