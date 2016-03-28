using System;
using ILOG.Concert;
using ILOG.CPLEX;

namespace Horarios
{
    class Program
    {
        static void Main(string[] args)
        {

            string[] lines = System.IO.File.ReadAllLines(@"./horarios/11_turmas.txt");

            int D = 5, H = 4, T = 2, P = 10;

            // mat Atp de necessidade de aulas. prrenche do arquivo.
            // turma X prof Y tem q dar Z aulas.
            var a = new double[T, P];

            // mat indisponibilidade do arquivo.
            var i = new double[D, H, P];


            // ate aqui, tudo arquivo. ^^^^

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

                            // p todo dhtp Xdhtp pertence {0, 1}
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

            // R3: Add restricao de quantidade de aulas
            // de cada professor por turma.
            // Humberto, 7 periodo, 4 aulas de PO.
            // add TxP restricoes ao modelo.


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

                    // o somatorio tem que ser A[t][p],
                    // que e o numero de aula q o prof tem q
                    // dar p uma turma.
                    model.AddEq(exp, a[t, p]);
                }
            }

            // R1
            for (int d = 0; d < D; d++)
            {
                for (int h = 0; h < H; h++)
                {
                    for (int p = 0; p < P; p++)
                    {

                        var exp = model.LinearNumExpr();

                        // verifica p tds turmas se prof n ta em mais de uma.
                        // soma qnts vezes foi alocado no msm dia msm hr.
                        for (int t = 0; t < T; t++)
                        {

                            exp.AddTerm(1.0, x[d, h, t, p]);

                        }

                        // nao pode ta em mais de uma ao msm tempo.
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
