using System.IO;
using System.Text;

namespace SlymLynk.Models;

/// <summary>
/// Determines the Explorer folder that received a drag-drop by reading the cursor
/// position immediately after DoDragDrop returns and querying Shell.Application.
/// No shell execution — uses COM automation and P/Invoke only.
/// </summary>
internal static class DragOutHelper
{
    /// <summary>
    /// Call immediately after DoDragDrop returns (before the user moves the mouse).
    /// Returns the absolute path of the Explorer folder under the cursor, or null
    /// if the drop target isn't a recognised Explorer window or the desktop.
    /// </summary>
    public static string? GetDropDestinationFolder()
    {
        NativeMethods.GetCursorPos(out var pt);
        var hwnd = NativeMethods.WindowFromPoint(pt);
        var rootHwnd = NativeMethods.GetAncestor(hwnd, 2 /* GA_ROOT */);

        if (IsDesktopWindow(rootHwnd))
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        return QueryShellApplicationForFolder(rootHwnd);
    }

    private static bool IsDesktopWindow(IntPtr hwnd)
    {
        var className = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, className, className.Capacity);
        return className.ToString() is "Progman" or "WorkerW";
    }

    // Hard bound on shell window enumeration: the collection is produced by an
    // out-of-process COM server, so never loop on an unbounded external count.
    private const int MaxShellWindows = 512;

    private static string? QueryShellApplicationForFolder(IntPtr targetHwnd)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null) return null;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic windows = shell.Windows();
            int count = Math.Min((int)windows.Count, MaxShellWindows);

            for (int i = 0; i < count; i++)
            {
                dynamic? w = windows.Item(i);
                if (w is null) continue;

                try
                {
                    // w.HWND is a COM int; compare against our target.
                    if (new IntPtr((int)w.HWND) != targetHwnd) continue;

                    string url = (string)w.LocationURL;
                    if (string.IsNullOrEmpty(url)) continue;

                    // LocationURL is a file URI: "file:///C:/path/to/folder"
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile)
                        return uri.LocalPath;
                }
                catch
                {
                    // Window may have closed mid-iteration. Swallowing is safe here
                    // (reviewed for M2): nothing user-controlled crosses this boundary,
                    // no state is mutated, and failure degrades to "not an Explorer
                    // window", which the caller handles by falling back to the picker.
                }
            }
        }
        catch
        {
            // COM unavailable (reviewed for M2): same reasoning as above — the null
            // return routes the user to the save dialog instead of failing silently.
        }

        return null;
    }
}
