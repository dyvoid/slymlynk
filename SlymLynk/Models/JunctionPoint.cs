using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SlymLynk.Models;

/// <summary>
/// Creates NTFS junction points via the DeviceIoControl reparse-point API.
/// No shell execution — pure P/Invoke.
/// </summary>
internal static class JunctionPoint
{
    private const int FSCTL_SET_REPARSE_POINT = 0x000900A4;
    private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
    private const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;

    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
    private const uint OPEN_EXISTING = 3;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct REPARSE_DATA_BUFFER
    {
        public uint ReparseTag;
        public ushort ReparseDataLength;
        public ushort Reserved;
        public ushort SubstituteNameOffset;
        public ushort SubstituteNameLength;
        public ushort PrintNameOffset;
        public ushort PrintNameLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXIMUM_REPARSE_DATA_BUFFER_SIZE)]
        public byte[] PathBuffer;
    }

    /// <summary>Creates a junction at <paramref name="junctionPath"/> pointing to <paramref name="targetPath"/>.</summary>
    public static void Create(string junctionPath, string targetPath, bool overwrite)
    {
        var target = @"\??\" + Path.GetFullPath(targetPath);

        using var handle = OpenReparsePoint(junctionPath,
            NativeMethods.FileAccess.GenericWrite);

        var targetBytes = Encoding.Unicode.GetBytes(target);
        var printBytes = Encoding.Unicode.GetBytes(targetPath);

        var rdb = new REPARSE_DATA_BUFFER
        {
            ReparseTag = IO_REPARSE_TAG_MOUNT_POINT,
            ReparseDataLength = (ushort)(targetBytes.Length + printBytes.Length + 12),
            SubstituteNameOffset = 0,
            SubstituteNameLength = (ushort)targetBytes.Length,
            PrintNameOffset = (ushort)(targetBytes.Length + 2),
            PrintNameLength = (ushort)printBytes.Length,
            PathBuffer = new byte[MAXIMUM_REPARSE_DATA_BUFFER_SIZE]
        };

        Array.Copy(targetBytes, rdb.PathBuffer, targetBytes.Length);
        Array.Copy(printBytes, 0, rdb.PathBuffer, targetBytes.Length + 2, printBytes.Length);

        var size = Marshal.SizeOf(rdb);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(rdb, buffer, false);
            var result = NativeMethods.DeviceIoControl(
                handle, FSCTL_SET_REPARSE_POINT, buffer, (uint)(rdb.ReparseDataLength + 8),
                IntPtr.Zero, 0, out _, IntPtr.Zero);

            if (!result)
                throw new IOException($"Failed to create junction (Win32 error {Marshal.GetLastWin32Error()}).");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static SafeFileHandle OpenReparsePoint(string path, NativeMethods.FileAccess access)
    {
        var handle = NativeMethods.CreateFile(
            path,
            access,
            NativeMethods.FileShare.ReadWrite | NativeMethods.FileShare.Delete,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
            IntPtr.Zero);

        if (handle.IsInvalid)
            throw new IOException($"Cannot open reparse point '{path}' (Win32 error {Marshal.GetLastWin32Error()}).");

        return handle;
    }
}
