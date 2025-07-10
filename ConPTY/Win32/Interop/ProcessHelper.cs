using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace Spakov.ConPTY.Win32.Interop {
  /// <summary>
  /// Process helper methods.
  /// </summary>
  internal static class ProcessHelper {
    /// <summary>
    /// Starts a new process with the specified command and attributes,
    /// associating it with a pseudoconsole.
    /// </summary>
    /// <remarks>This method initializes the necessary thread attributes to
    /// associate the process with a pseudoconsole, then starts the process
    /// using the specified command. The caller must ensure that the
    /// pseudoconsole handle (<paramref name="hPC"/>) remains valid for the
    /// duration of the process's lifetime.</remarks>
    /// <param name="command">The command to execute.</param>
    /// <param name="attribute"><see
    /// cref="PseudoConsole.PseudoConsoleThreadAttribute"/>.</param>
    /// <param name="hPC">The pseudoconsole handle.</param>
    internal static Process Start(string command, nuint attribute, ClosePseudoConsoleSafeHandle hPC) {
      STARTUPINFOEXW startupInfoEx = ConfigureProcessThread(hPC, attribute);
      PROCESS_INFORMATION processInformation = RunProcess(ref startupInfoEx, command);

      return new Process(startupInfoEx, processInformation);
    }

    /// <summary>
    /// Configures a process thread with the specified pseudo console handle
    /// and attribute.
    /// </summary>
    /// <remarks>This method initializes a process thread attribute list,
    /// associates the specified pseudoconsole handle with the thread, and
    /// returns the configured <see cref="STARTUPINFOEXW"/> structure. The
    /// caller is responsible for ensuring that the returned structure and its
    /// associated resources are properly disposed of to avoid memory
    /// leaks.</remarks>
    /// <param name="hPC">A safe handle to the pseudo console that will be
    /// associated with the process thread.</param>
    /// <param name="attribute">The attribute to be applied to the process
    /// thread. This is typically a constant value representing the type of
    /// attribute to set.</param>
    /// <returns>A <see cref="STARTUPINFOEXW"/> structure containing the
    /// initialized attribute list and other startup information for the
    /// process thread.</returns>
    /// <exception cref="Win32Exception">Thrown if any of the underlying
    /// Windows API calls fail, such as initializing or updating the process
    /// thread attribute list.</exception>
    private static STARTUPINFOEXW ConfigureProcessThread(ClosePseudoConsoleSafeHandle hPC, nuint attribute) {
      bool success;

      nuint lpSize = nuint.Zero;

      // Get the number of bytes needed for lpAttributeList
      success = PInvoke.InitializeProcThreadAttributeList(
        LPPROC_THREAD_ATTRIBUTE_LIST.Null,
        1,
        ref lpSize
      );

      if (success || lpSize == nuint.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());

      STARTUPINFOEXW startupInfoExW = new() {
        lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST) Marshal.AllocHGlobal((int) lpSize)
      };

      startupInfoExW.StartupInfo.cb = (uint) Marshal.SizeOf<STARTUPINFOEXW>();

      // Initialize the attribute list
      success = PInvoke.InitializeProcThreadAttributeList(
        startupInfoExW.lpAttributeList,
        1,
        ref lpSize
      );

      if (!success) throw new Win32Exception(Marshal.GetLastWin32Error());

      // Set the pseudoconsole thread attribute
      unsafe {
        success = PInvoke.UpdateProcThreadAttribute(
          startupInfoExW.lpAttributeList,
          0,
          attribute,
          hPC.DangerousGetHandle().ToPointer(),
          (nuint) IntPtr.Size,
          null,
          (nuint?) null
        );
      }

      return success
        ? startupInfoExW
        : throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    /// <summary>
    /// Creates and starts a new process using the specified startup
    /// information and command line.
    /// </summary>
    /// <remarks>This method wraps the Windows API function
    /// <c>CreateProcessW</c> to create a new process. The caller is
    /// responsible for ensuring that the provided <paramref
    /// name="startupInfoExW"/> and <paramref name="commandLine"/> are valid
    /// and meet the requirements of the underlying API. If the process
    /// creation fails, the method throws a <see cref="Win32Exception"/> with
    /// the relevant error code.</remarks>
    /// <param name="startupInfoExW">A reference to a <see
    /// cref="STARTUPINFOEXW"/> structure that specifies the extended startup
    /// information for the process.</param>
    /// <param name="commandLine">The command line to execute, including the
    /// application name and any arguments. This cannot be <see
    /// langword="null"/> or empty.</param>
    /// <returns>A <see cref="PROCESS_INFORMATION"/> structure containing
    /// information about the newly created process and its primary
    /// thread.</returns>
    /// <exception cref="Win32Exception">Thrown if the process creation fails.
    /// The exception's error code will contain the result of the last Win32
    /// error.</exception>
    private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEXW startupInfoExW, string commandLine) {
      SECURITY_ATTRIBUTES lpProcessAttributes = new() { nLength = (uint) Marshal.SizeOf<SECURITY_ATTRIBUTES>() };
      SECURITY_ATTRIBUTES lpThreadAttributes = new() { nLength = (uint) Marshal.SizeOf<SECURITY_ATTRIBUTES>() };

      PROCESS_INFORMATION processInformation;

      bool success;

      unsafe {
        success = Kernel32.CreateProcessW(
          null,
          commandLine,
          ref lpProcessAttributes,
          ref lpThreadAttributes,
          false,
          PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
          null,
          null,
          ref startupInfoExW,
          out processInformation
        );
      }

      return success
        ? processInformation
        : throw new Win32Exception(Marshal.GetLastWin32Error());
    }
  }
}
