using System;
using System.IO;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Diagnostics;
using System.Collections.Generic;

namespace Horarios
{
    class Program
    {
        static void Main(string[] args)
        {

            //*******************************************************
            //  Obtem os dados de arquivo externo
            //
            //*******************************************************

            var files = Directory.GetFiles(@"./horarios/");

            foreach (var file in files)
            {
                //if (Path.GetFileNameWithoutExtension(file) != "nilda30")
                //{
                //    continue;
                //}

                encontrarSolucao(file);
            }

            Console.WriteLine("Pressione qualquer tecla para encerrar o programa");
            Console.ReadKey(true);
        }

        private static void encontrarSolucao(string file)
        {
            int[,] a; //Matriz de aulas
            int[,,] i; //Matriz de indisponibilidades
            int[,] l; //Matriz de limitações de aulas
            int[,] r; //Matriz de restrições de aulas entre professores

            int P, T, D, H; //Indices para Professor, Turma, Dia e Horário

            lerArquivo(out a, out i, out l, out r, out P, out T, out D, out H, file);

            Cplex model = new Cplex();

            #region Variáveis de decisão
            //*******************************************************
            //  Define a variável de decisão que define que todas
            //  as aulas devem ser dadas
            //
            //*******************************************************

            #region Variável X - Todas as aulas são dadas
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
            #endregion

            #region Variável Y - Aulas isoladas
            // y[d,p] = num aulas prof P dia D.
            var y = new INumVar[D, P];

            for (int d = 0; d < D; d++)
            {
                for (int p = 0; p < P; p++)
                {
                    y[d, p] = model.BoolVar();
                }
            }
            #endregion

            #region Variável W
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
            #endregion

            #region Variável G
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

            #region Variável J
            var j = new INumVar[P, D];

            for (int p = 0; p < P; p++)
            {
                for (int d = 0; d < D; d++)
                {

                    j[p, d] = model.BoolVar();

                }

            }
            #endregion

            #endregion

            // Primeiro tratar germinacao;
            // Segundo tratar aulas isoladas;
            // Terceiro resto das restricoes;

            //*******************************************************
            //  Adiciona as restrições do modelo
            //
            //*******************************************************

            #region Restrições 1 e 2 - Professor só da uma aula por vez; e Professor só da aula quando disponível
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

            #region Restrição 3 - Quantidade de aulas que um professor dá por turma
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

            #region Restrição 4 - Quantidade máxima de aulas por dia para um professor
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

            #region Restrição 5 - Dois professores não lecionam no mesmo dia e horário e turma
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

            #region Restrição 6 - Aulas isoladas
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

            #region Restrição 7 - Dois professores não lecionam no mesmo dia
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

            #region Janelas - Diminue blocos de aulas separados para professores
            //Fixa P e D
            for (int p = 0; p < P; p++)
            {
                for (int d = 0; d < D; d++)
                {
                    //Varia T e H
                    var blocos = model.LinearNumExpr();
                    for (int t = 0; t < T; t++)
                    {
                        var turmaBlocoExp = model.LinearNumExpr();
                        for (int h = 0; h < H; h++)
                        {

                            turmaBlocoExp.AddTerm(Math.Pow(10, h), x[d, h, t, p]);
                        }

                        blocos.Add(turmaBlocoExp);
                    }

                    //Se tiver qualquer bloco
                    model.Add(
                        model.IfThen(
                            model.Not(
                                model.Or(
                                    new[]
                                    {
                                        //5
                                        model.Eq(11111.0, blocos),
                                        //4
                                        model.Eq(01111.0, blocos),
                                        model.Eq(11110.0, blocos),
                                        //3
                                        model.Eq(00111.0, blocos),
                                        model.Eq(01110.0, blocos),
                                        model.Eq(11100.0, blocos),
                                        //2
                                        model.Eq(00011.0, blocos),
                                        model.Eq(00110.0, blocos),
                                        model.Eq(01100.0, blocos),
                                        model.Eq(11000.0, blocos),
                                        //1
                                        model.Eq(00001.0, blocos),
                                        model.Eq(00010.0, blocos),
                                        model.Eq(00100.0, blocos),
                                        model.Eq(01000.0, blocos),
                                        model.Eq(10000.0, blocos)
                                    }
                                  )
                                  , "blocos"
                                )
                            , model.Eq(j[p, d], 1)
                            )
                        );
                    //model.Add(model.IfThen(model.Ge(turmaBlocoExp, 1), model.Eq(j[p, d], 1)));
                }
            }
            #endregion

            #region Funções objetivos

            #region Pesos
            Dictionary<string, double> weights = new Dictionary<string, double>();
            weights.Add("maxTodasAulas", 1.0);
            weights.Add("maxGeminadas", 0.5);
            weights.Add("minIsoladas", -0.01);
            weights.Add("minJanelas", -0.1);
            #endregion

            //*******************************************************
            //  Função objetivo para garantir que todas as aulas
            //  sejam dadas
            //
            //*******************************************************
            #region FO Todas as aulas
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
                            foTodasAsAulas.AddTerm(weights["maxTodasAulas"], x[d, h, t, p]);

                        }
                    }
                }
            }
            #endregion

            //*******************************************************
            //  Função objetivo para minimizar o número de aulas
            //  isoladas
            //
            //*******************************************************
            #region FO Aulas isoladas
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
                    foMinAulasIsolada.AddTerm(weights["minIsoladas"], y[d, p]);

                }
            }
            #endregion

            //*********************************************************
            //  Função objetivo para tentar maximizar aulas geminadas
            //
            //*********************************************************
            #region FO Geminada
            var foGeminada = model.LinearNumExpr();
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

                            foGeminada.AddTerm(weights["maxGeminadas"], g[d, b, t, p]);

                            b++;
                        }

                    }

                }
            }
            #endregion

            //*********************************************************
            //  Função objetivo para tentar minimizar blocos separados
            //  de aulas
            //*********************************************************
            #region FO Janelas de aulas
            var foMinJanelas = model.LinearNumExpr();
            for (int p = 0; p < P; p++)
            {
                for (int d = 0; d < D; d++)
                {
                    foMinJanelas.AddTerm(weights["minJanelas"], j[p, d]);
                }
            }
            #endregion

            //*******************************************************
            //  Adiciona as funções objetivos ao modelo foTodasAsAulas
            //
            //*******************************************************
            model.AddMaximize(
                model.Sum(
                    foMinAulasIsolada
                    , foGeminada
                    , foTodasAsAulas
                    , foMinJanelas
                    )
                );

            #endregion

            #region Tenta resolver o problema
            var sw = new Stopwatch();

            sw.Start();
            Console.Beep(440, 500);

            //Define um tempo máximo em segundos para o cplex
            model.SetParam(Cplex.IntParam.TimeLimit, 30 * 60);

            //Pára o cplex ao encontrar a primeira solução 
            //model.SetParam(Cplex.IntParam.IntSolLim, 1);

            var solve = model.Solve();

            Console.Beep(880, 1 * 250);
            Console.Beep(880, 1 * 250);
            sw.Stop();
            #endregion

            #region Escreve resultados à saida

            var stdOutputStream = Console.Out;

            var fileName = Path.GetFileNameWithoutExtension(file);
            Directory.CreateDirectory("./saidas");
            var fileStream = new FileStream("./saidas/" + fileName + "_saida.txt", FileMode.OpenOrCreate, FileAccess.Write);
            var fileOutputStream = new StreamWriter(fileStream);

            //Write to the file instead of the standard output
            Console.SetOut(fileOutputStream);

            Console.WriteLine("Solving time: " + sw.Elapsed.Duration().ToString() + ".");

            //Solver the problem
            if (solve)
            {
                Console.WriteLine("Solution status = " + model.GetStatus());
                Console.WriteLine("--------------------------------------------");
                Console.WriteLine();
                Console.WriteLine("Solution found:");
                Console.WriteLine(" Objective value = " + model.ObjValue);
                Console.WriteLine(" DxHxT = {0}\t(Todas as aulas)", D * H * T);
                Console.WriteLine(" TxH   = {0} \t(Aulas isoladas)", T * H);
                Console.WriteLine();

                //Console.WriteLine();
                //Console.SetOut(stdOutputStream);
                //Console.WriteLine("Mostrar agenda dos professores? [y/n]");
                //var key = Console.ReadKey(true);
                //if (key.Key.Equals(ConsoleKey.Y))
                //{
                //Console.SetOut(fileOutputStream);
                exibirTabelasProfessores(P, T, D, H, model, x, y);
                //}

                //Console.WriteLine();
                //Console.SetOut(stdOutputStream);
                //Console.WriteLine("Mostrar agenda das turmas? [y/n]");
                //key = Console.ReadKey(true);
                //if (key.Key.Equals(ConsoleKey.Y))
                //{
                //    Console.SetOut(fileOutputStream);
                exibirTabelasTurmas(P, T, D, H, model, x);
                //}
            }
            else {
                Console.WriteLine("No solution found.");
                Console.WriteLine("Solution status: " + model.GetStatus());
            }

            //Close file
            Console.Out.Flush();
            fileOutputStream.Dispose();
            fileStream.Dispose();

            //Close console
            Console.SetOut(stdOutputStream);
            #endregion
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
                        }
                        if (turmasLecionadasNoMesmoMomento == 0)
                        {
                            Console.Write("  ---  |");
                        }
                        else if (turmasLecionadasNoMesmoMomento > 1)
                        {
                            Console.Write(" M.I. |");
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
                        }
                        if (turmasLecionadasNoMesmoMomento == 0)
                        {
                            Console.Write("  ---  |");
                        }
                        else if (turmasLecionadasNoMesmoMomento > 1)
                        {
                            Console.Write(" M.I. |");
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
            var lines = File.ReadAllLines(fileName);

            lines = lines.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

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