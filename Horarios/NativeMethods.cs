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

        [DllImport(@"./../Debug/VRPDll.dll")]
        public static extern void CAPSEP_SeparateCapCuts(
                    int NoOfCustomers,
                    IntPtr Demand,
                    int CAP,
                    int NoOfEdges,
                    ref int EdgeTail,
                    ref int EdgeHead,
                    ref double EdgeX,
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
