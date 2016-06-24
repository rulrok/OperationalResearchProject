using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoPO
{
    class NativeMethods
    {

        public struct CnstrRecord
        {
            int CType; /* Constraint Type. */
            int Key;
            int IntListSize;
            IntPtr IntList;
            int ExtListSize;
            IntPtr ExtList;
            int CListSize;
            IntPtr CList;
            IntPtr CoeffList;
            int A, B, L; /* For MSTARs: Lambda=L/B, Sigma=A/B. */
            double RHS;
            int BranchLevel;
            int GlobalNr;
        }


        public struct CnstrMgrRecord
        {
            public CnstrRecord CPL;
            public int Dim;
            public int Size;
        }

        [DllImport(@"./../Debug/CVRPSEP.dll")]
        public static extern void CAPSEP_SeparateCapCuts(int NoOfCustomers,
                    ref int Demand,
                    int CAP,
                    int NoOfEdges,
                    ref int EdgeTail,
                    ref int EdgeHead,
                    ref double EdgeX,
                    ref CnstrMgrRecord CMPExistingCuts,
                    int MaxNoOfCuts,
                    double EpsForIntegrality,
                    ref char IntegerAndFeasible,
                    ref double MaxViolation,
                    ref CnstrMgrRecord CutsCMP);
    }
}
