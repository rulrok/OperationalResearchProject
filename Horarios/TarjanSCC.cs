using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoPO
{
    public class TarjanSCC
    {
        private MatrizAdjacenciaSimetrica<int> matrix;
        private List<List<int>> components;
        private Stack<int> stack;
        private bool[] visited;
        private int[] index;
        private int[] lowlink;
        private int indexCount;

        public TarjanSCC(MatrizAdjacenciaSimetrica<int> matrix)
        {
            this.matrix = matrix;
            components = new List<List<int>>();
            visited = new bool[matrix.N];
            index = new int[matrix.N];
            lowlink = new int[matrix.N];
            stack = new Stack<int>();
            indexCount = 0;
        }

        public List<List<int>> run()
        {
            for(var i = 0; i < matrix.N; i++)
            {
                if (index[i] == 0)
                {
                    visit(i);
                }
            }

            return components;
        }

        private void visit(int v)
        {
            indexCount++;
            index[v] = indexCount;
            lowlink[v] = indexCount;

            stack.Push(v);


            for (int w = 0; w < matrix.N; w++)
            {
                if (w != v && (matrix[v, w] == 1))
                {
                    //Vizinho
                    if (index[w] == 0)
                    {
                        // Successor w has not yet been visited; recurse on it
                        visit(w);
                        lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                    } else if (stack.Contains(w))
                    {
                        // Successor w is in stack S and hence in the current SCC
                        lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                    }
                }
            }

            // If v is a root node, pop the stack and generate an SCC
            if (lowlink[v] == index[v])
            {
                List<int> component = new List<int>();

                int w = -1;
                do
                {
                    w = stack.Pop();
                    component.Add(w);
                } while (w != v);

                components.Add(component);
            }

        }
    }
}
