using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoPO
{
    /// <summary>
    /// This class implements the follow algorithm:
    /// http://www.cs.umd.edu/class/fall2005/cmsc451/biconcomps.pdf
    /// </summary>
    public class BiconnectedComponents : SCC
    {

        private MatrizAdjacenciaSimetrica<int> matrix;
        private List<List<int>> components;

        private int count;
        private Stack<int> stack;
        private bool[] visited;
        private int[] parent;

        private int[] d;
        private int[] low;


        public BiconnectedComponents(MatrizAdjacenciaSimetrica<int> matrix)
        {
            this.matrix = matrix;
            components = new List<List<int>>();

            count = 0;
            stack = new Stack<int>();

            visited = new bool[matrix.N];
            parent = new int[matrix.N];
            d = new int[matrix.N];
            low = new int[matrix.N];

        }

        public List<List<int>> FindComponents()
        {

            for (var i = 0; i < matrix.N; i++)
            {
                if (!visited[i])
                {
                    visit(i);
                }
            }

            return components;
        }

        private void visit(int u)
        {
            visited[u] = true;
            count++;

            d[u] = count;
            low[u] = d[u];

            for (int v = 0; v < matrix.N; v++)
            {
                if (u != v && (matrix[u, v] == 1))
                {
                    if (!visited[v])
                    {
                        stack.Push(v);
                        parent[v] = u;
                        visit(v);

                        if (low[v] >= d[u])
                        {
                            OutputComp(u, v);
                        }
                        low[u] = Math.Min(low[u], low[v]);
                    }
                    else if ((parent[u] != v) && (d[v] < d[u]))
                    {
                        //(u,v) is a back edge from u to its ancestor v
                        stack.Push(v);
                        low[u] = Math.Min(low[u], d[v]);
                    }
                }
            }

        }

        private void OutputComp(int u, int v)
        {
            List<int> component = new List<int>();
            int e;
            do
            {
                e = stack.Pop();
                component.Add(e);

            } while (e != v);
            components.Add(component);
        }
    }
}
