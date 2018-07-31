using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace AntiAfk
{
    public partial class Ferib : Form
    {
        private static long LastHardwareAction = 0x26F8028; //sig: F2 0F 10 15 ?? ?? ?? ?? BB 01 00 00 00 8B 2D ?? ?? ?? ??

        #region ImportDLL
        [DllImport("Kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr handle, long address, byte[] bytes, int nsize, ref int op);

        [DllImport("Kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hwind, long Address, byte[] bytes, int nsize, out int output);

        [DllImport("Kernel32.dll")]
        public static extern IntPtr OpenProcess(int Token, bool inheritH, int ProcID);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, long lpAddress,
        uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        enum MemoryProtection
        {
            NoAccess = 0x0001,
            ReadOnly = 0x0002,
            ReadWrite = 0x0004,
            WriteCopy = 0x0008,
            Execute = 0x0010,
            ExecuteRead = 0x0020,
            ExecuteReadWrite = 0x0040,
            ExecuteWriteCopy = 0x0080,
            GuardModifierflag = 0x0100,
            NoCacheModifierflag = 0x0200,
            WriteCombineModifierflag = 0x0400,
            Proc_All_Access = 2035711
        }
        #endregion ImportDLL

        public Ferib()
        {
            InitializeComponent();
        }

        private void InjectAFKCave(int id, IntPtr wHandle)
        {
            #region ASM
            byte[] AFKm = new byte[]
            {
                0x90,                                                                                           //nop
                0x55,                                                                                           //push rbp
                0x48, 0x8B, 0xEC,                                                                               //mov rbp, rsp
                0x48, 0xB9, 0xDE, 0xAD, 0xBE, 0xEF, 0xDE, 0xAD, 0xBE, 0xEF,                                     //rcx, LastHardwareAction   ;Make sure to remove this death beef with our LastHardwareAction offset!
                0x51,                                                                                           //push rcx
                0x48, 0xB9, 0x71, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                                     //rcx, 000000000000000271
                0xFF, 0x15, 0x02, 0x00, 0x00, 0x00, 0xEB, 0X08, 0xDE, 0xAD, 0xBE, 0xEF, 0xDE, 0xAD, 0xBE, 0xEF, //call KERNERL32.Sleep
                0x59,                                                                                           //pop rcx
                0x48, 0x8B, 0x19,                                                                               //mov rbx,[rcx]
                0x48, 0x81, 0xC3, 0x71, 0x02, 0x00, 0x00,                                                       //rbx,00000271
                0x48, 0x89, 0x19,                                                                               //[rcx],rbx
                0xEB, 0xD5,                                                                                     //jmp (-> push rcx)
                0x48, 0x8B, 0xE5,                                                                               //mov rsp, rbp
                0x5D,                                                                                           //pop rbp
                0xC3                                                                                            //ret
            };
            #endregion ASM
            int BytesWritten = 0;
            long hAlloc = (long)VirtualAllocEx(wHandle, 0, (uint)AFKm.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            Console.WriteLine("CodeCave[" + id + "] is @ " + hAlloc);

            WriteProcessMemory(wHandle, hAlloc, AFKm, AFKm.Length, out BytesWritten);
            long Base32_Sleep = (long)GetProcAddress(GetModuleHandle("kernel32.dll"),"Sleep");

            Console.WriteLine(Base32_Sleep);

            WriteProcessMemory(wHandle, hAlloc + 0x07, BitConverter.GetBytes((long)Process.GetProcessById(id).MainModule.BaseAddress + LastHardwareAction), 0x08, out BytesWritten);
            WriteProcessMemory(wHandle, hAlloc + 0x22, BitConverter.GetBytes(Base32_Sleep), 0x08, out BytesWritten);

            BypasAntiCheat01(id, true, wHandle);

            uint iThreadId = 0;
            IntPtr hThread = CreateRemoteThread(wHandle, IntPtr.Zero, 0, (IntPtr)hAlloc, IntPtr.Zero, 0, out iThreadId);

            System.Threading.Thread.Sleep(100);

            BypasAntiCheat01(id, false, wHandle);
            
            //CloseHandle
        }

        private void BypasAntiCheat01(int id, bool status, IntPtr wHandle)
        {
            byte[] Patch = { 0xFF, 0xE0, 0xCC, 0xCC, 0xCC };  //JMP RAX
            byte[] Patch2 = { 0x48, 0xFF, 0xC0, 0xFF, 0xE0 }; //INC RAX, JMP RAX

            //Blizzard will add 0xC3 (ret) at the begin of our code cave, So what we do is start our code cave with 0x90 (NOP) and then add the code cave under it.
            //We will patch a DLL function (Cuz i don't like touching Wow.exe) so it start executing our code cave from the second byte.

            long CreateRemoteThreadPatchOffset = (long)GetProcAddress(GetModuleHandle("kernel32.dll"), "BaseDumpAppcompatCacheWorker") + 0x1E0;

            Console.WriteLine(CreateRemoteThreadPatchOffset);

            if (status)
                Patch = Patch2;

            int BytesWritten = 0;
            WriteProcessMemory(wHandle, CreateRemoteThreadPatchOffset, Patch, Patch.Length, out BytesWritten);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ChangeOffsets.UpdateOffset();

            Process[] ProcList = Process.GetProcessesByName("Wow");

            foreach (Process Proc in ProcList){
                IntPtr wHandle = OpenProcess((int)MemoryProtection.Proc_All_Access, false, Proc.Id);
                InjectAFKCave(Proc.Id, wHandle);
            }

        }
    }
}
