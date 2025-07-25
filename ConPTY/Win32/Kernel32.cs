using System.Runtime.InteropServices;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace Spakov.ConPTY.Win32
{
    internal unsafe static class Kernel32
    {
        /// <summary>
        /// <para>Creates a new process and its primary thread. The new process
        /// runs in the security context of the calling process.</para>
        /// <para>If the calling process is impersonating another user, the new
        /// process uses the token for the calling process, not the
        /// impersonation token.To run the new process in the security context
        /// of the user represented by the impersonation token, use the
        /// <c>CreateProcessAsUser</c> or <c>CreateProcessWithLogonW</c>
        /// function.</para>
        /// </summary>
        /// <param name="lpApplicationName">The name of the module to be
        /// executed. This module can be a Windows-based application. It can be
        /// some other type of module (for example, MS-DOS or OS/2) if the
        /// appropriate subsystem is available on the local computer.</param>
        /// <param name="lpCommandLine">The command line to be
        /// executed.</param>
        /// <param name="lpProcessAttributes">A pointer to a
        /// <see cref="SECURITY_ATTRIBUTES"/> structure that determines whether
        /// the returned handle to the new process object can be inherited by
        /// child processes. If <paramref name="lpProcessAttributes"/> is <see
        /// langword="null"/>, the handle cannot be inherited.</param>
        /// <param name="lpThreadAttributes">A pointer to a <see
        /// cref="SECURITY_ATTRIBUTES"/> structure that specifies a security
        /// descriptor for the new thread and determines whether child
        /// processes can inherit the returned handle. If <paramref
        /// name="lpThreadAttributes"/> is <see langword="null"/>, the thread
        /// gets a default security descriptor and the handle cannot be
        /// inherited. The access control lists (ACL) in the default security
        /// descriptor for a thread come from the primary token of the
        /// creator.</param>
        /// <param name="bInheritHandles">If this parameter is <see
        /// langword="true"/>, each inheritable handle in the calling process
        /// is inherited by the new process. If the parameter is <see
        /// langword="false"/>, the handles are not inherited. Note that
        /// inherited handles have the same value and access rights as the
        /// original handles. For additional discussion of inheritable handles,
        /// see Remarks.</param>
        /// <param name="dwCreationFlags">The flags that control the priority
        /// class and the creation of the process. For a list of values, see
        /// <see
        /// href="https://learn.microsoft.com/en-us/windows/desktop/ProcThread/process-creation-flags"
        /// >Process Creation Flags</see>.</param>
        /// <param name="lpEnvironment">A pointer to the <see
        /// href="https://learn.microsoft.com/en-us/windows/win32/procthread/environment-variables"
        /// >environment block</see> for the new process. If this parameter is
        /// <see langword="null"/>, the new process uses the environment of the
        /// calling process.</param>
        /// <param name="lpCurrentDirectory">
        /// <para>The full path to the current directory for the process. The
        /// string can also specify a UNC path.</para>
        /// <para>If this parameter is <see langword="null"/>, the new process
        /// will have the same current drive and directory as the calling
        /// process. (This feature is provided primarily for shells that need
        /// to start an application and specify its initial drive and working
        /// directory.)</para>
        /// </param>
        /// <param name="lpStartupInfo">
        /// <para>A pointer to a <c>STARTUPINFO</c> or <see
        /// cref="STARTUPINFOEXW"/> structure.</para>
        /// <para>To set extended attributes, use a <see
        /// cref="STARTUPINFOEXW"/> structure and specify
        /// <c>EXTENDED_STARTUPINFO_PRESENT</c> in the <paramref
        /// name="dwCreationFlags"/> parameter.</para>
        /// <para>Handles in <c>STARTUPINFO</c> or <see cref="STARTUPINFOEXW"/>
        /// must be closed with <c>CloseHandle</c> when they are no longer
        /// needed.</para>
        /// </param>
        /// <param name="lpProcessInformation">
        /// <para>A pointer to a <see cref="PROCESS_INFORMATION"/> structure
        /// that receives identification information about the new
        /// process.</para>
        /// <para>Handles in <see cref="PROCESS_INFORMATION"/> must be closed
        /// with <c>CloseHandle</c> when they are no longer needed.</para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is nonzero.</para>
        /// <para>If the function fails, the return value is zero. To get
        /// extended error information, call <c>GetLastError</c>.</para>
        /// <para>Note that the function returns before the process has
        /// finished initialization. If a required DLL cannot be located or
        /// fails to initialize, the process is terminated. To get the
        /// termination status of a process, call
        /// <c>GetExitCodeProcess</c>.</para>
        /// </returns>
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
