using System;
using System.Runtime.InteropServices;

namespace JustFOV
{
    public class Natives
    {
        #region ProcessAccessFlags

        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }

        #endregion

        #region Imports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer,
            int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer,
            uint nSize, out int lpNumberOfBytesWritten);

        #endregion

        #region Helpers

        public static byte[] ReadBytes(IntPtr handle, IntPtr address, uint numBytes)
        {
            var buf = new byte[numBytes];
            var numBytesRead = 0;
            ReadProcessMemory(handle, address, buf, buf.Length, out numBytesRead);

            return buf;
        }

        public static void WriteBytes(IntPtr handle, IntPtr address, byte[] bytes)
        {
            var numBytesWritten = 0;
            WriteProcessMemory(handle, address, bytes, (uint) bytes.Length, out numBytesWritten);
        }

        public static IntPtr ReadIntPtr(IntPtr handle, IntPtr address)
        {
            var buf = ReadBytes(handle, address, (uint) IntPtr.Size);

            return new IntPtr(BitConverter.ToInt64(buf, 0));
        }

        public static void WriteFloat(IntPtr handle, IntPtr address, float f)
        {
            var buf = BitConverter.GetBytes(f);

            WriteBytes(handle, address, buf);
        }

        public static float ReadFloat(IntPtr handle, IntPtr address)
        {
            var buf = ReadBytes(handle, address, 4);

            return BitConverter.ToSingle(buf, 0);
        }

        #endregion
    }
}