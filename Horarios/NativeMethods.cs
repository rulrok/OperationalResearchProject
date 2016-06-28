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
            public CnstrRecord CPL; //Maybe I could only have a IntPtr here and remove the above struct
            public int Dim;
            public int Size;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="NoOfCustomers">The number of customers in the problem. The n customers are assumed to be numbered 1, . . . , n.</param>
        /// <param name="Demand">A vector containing the customer demands, i.e., Demand[i] is the demand of customer i for i = 1, . . . , n.</param>
        /// <param name="CAP">The capacity of each vehicle.</param>
        /// <param name="NoOfEdges">The number of edges for which information is passed
        /// by the following three parameters.</param>
        /// <param name="EdgeTail">
        ///  These three vectors give information on the current LP solution.
        /// Only information on those edges e with x∗ e > 0
        /// should be passed in the three vectors.
        /// EdgeTail[e], EdgeHead[e] are the two end vertices of edge e.
        /// EdgeX[e] is x∗ e.
        /// The depot is assumed to be numbered n+1. 
        /// Note that edge numbers are 1 - based (e = 1, . . . , NoOfEdges).</param>
        /// <param name="EdgeHead">
        ///  These three vectors give information on the current LP solution.
        /// Only information on those edges e with x∗ e > 0
        /// should be passed in the three vectors.
        /// EdgeTail[e], EdgeHead[e] are the two end vertices of edge e.
        /// EdgeX[e] is x∗ e.
        /// The depot is assumed to be numbered n+1. 
        /// Note that edge numbers are 1 - based (e = 1, . . . , NoOfEdges).</param>
        /// <param name="EdgeX">
        ///  These three vectors give information on the current LP solution.
        /// Only information on those edges e with x∗ e > 0
        /// should be passed in the three vectors.
        /// EdgeTail[e], EdgeHead[e] are the two end vertices of edge e.
        /// EdgeX[e] is x∗ e.
        /// The depot is assumed to be numbered n+1. 
        /// Note that edge numbers are 1 - based (e = 1, . . . , NoOfEdges).</param>
        /// <param name="CMPExistingCuts">This is a pointer to a data structure containing previously
        /// generated rounded capacity inequalities.It is used as input
        /// to our fourth heuristic.It is explained in the text
        /// how to use this data structure.</param>
        /// <param name="MaxNoOfCuts">The maximum number of cuts to be returned. Note that this maximum
        /// applies only if the connected components heuristic fails.That is,
        /// the number of cuts found by the connected components heuristic
        /// cannot be controlled through this parameter.
        /// The heuristic based on max-flows finds at most half of the
        /// maximum number of cuts.The rest are allowed to be found by
        /// the greedy search and the add/drop(the fourth) heuristic.The
        /// actual number of cuts found is returned in CutsCMP->Size.</param>
        /// <param name="EpsForIntegrality">This is the tolerance used when checking whether the input solution is integer(use, e.g., EpsForIntegrality = 0.0001). If the
        /// connected components heuristic fails, and each x∗ e deviates
        /// no more than EpsForIntegrality from the nearest integer,
        /// the entire routine returns IntegerAndFeasible = 1,
        /// otherwise it returns IntegerAndFeasible = 0.</param>
        /// <param name="IntegerAndFeasible">See the comment to EpsForIntegrality</param>
        /// <param name="MaxViolation">This is the violation of the cut with the largest violation,
        /// in the form x(S : S) ≤ |S| − k(S).</param>
        /// <param name="CutsCMP">This is a pointer to the data structure containing the cuts
        /// found by the separation routine.It is explained in the text how to
        /// read the actual cuts from this structure.</param>
        [DllImport(@"./../Debug/VRPDll.dll")]
        public static extern void CAPSEP_SeparateCapCuts(
                    int NoOfCustomers,
                    IntPtr Demand,
                    int CAP,
                    int NoOfEdges,
                    IntPtr EdgeTail,
                    IntPtr EdgeHead,
                    IntPtr EdgeX,
                    ref CnstrMgrRecord CMPExistingCuts,
                    int MaxNoOfCuts,
                    double EpsForIntegrality,
                    StringBuilder IntegerAndFeasible, //char* originally (http://stackoverflow.com/questions/18495818/how-to-pass-and-receive-data-from-a-char-from-c-sharp-to-an-unmanaged-c-dll)
                    ref double MaxViolation,
                    ref CnstrMgrRecord CutsCMP
            );

        [DllImport(@"./../Debug/VRPDll.dll")]
        public static extern void CMGR_CreateCMgr(ref CnstrMgrRecord CMP, int Dim);

        [DllImport(@"./../Debug/VRPDll.dll")]
        public static extern void CMGR_MoveCnstr(ref CnstrMgrRecord SourcePtr,
                    ref CnstrMgrRecord SinkPtr,
                    int SourceIndex,
                    int SinkIndex);
    }
}
