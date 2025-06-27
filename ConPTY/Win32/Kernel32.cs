using System.Runtime.InteropServices;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace ConPTY.Win32 {
  internal unsafe static class Kernel32 {
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable IDE0079 // Remove unnecessary suppression
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = "Require different signature than CsWin32 pulls in")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    internal static extern bool CreateProcessW(
      string? lpApplicationName,
      string lpCommandLine,
      ref SECURITY_ATTRIBUTES lpProcessAttributes,
      ref SECURITY_ATTRIBUTES lpThreadAttributes,
      bool bInheritHandles,
      PROCESS_CREATION_FLAGS dwCreationFlags,
      void* lpEnvironment,
      string? lpCurrentDirectory,
      [In] ref STARTUPINFOEXW lpStartupInfo,
      out PROCESS_INFORMATION lpProcessInformation
    );
  }
}
