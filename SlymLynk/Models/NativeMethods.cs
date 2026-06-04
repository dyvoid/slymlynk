using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SlymLynk.Models;

/// <summary>P/Invoke declarations for Win32 filesystem APIs.</summary>
internal static class NativeMethods
{
    [Flags]
    internal enum FileAccess : uint
    {
        GenericRead = 0x80000000,
        GenericWrite = 0x40000000
    }

    [Flags]
    internal enum FileShare : uint
    {
        None = 0,
        Read = 1,
        Write = 2,
        Delete = 4,
        ReadWrite = Read | Write
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern SafeFileHandle CreateFile(
        string lpFileName,
        FileAccess dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateSymbolicLink(
        string lpSymlinkFileName,
        string lpTargetFileName,
        int dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);
}
