using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace Spakov.ConPTY.Win32.Interop
{
    /// <summary>
    /// Represents a managed wrapper for a native process, providing access to
    /// its startup information and process details, and ensuring proper
    /// resource cleanup.
    /// </summary>
    /// <remarks>This class encapsulates the native process handles and startup
    /// information, ensuring that resources such as attribute lists and
    /// handles are properly released when the instance is disposed. Instances
    /// of this class are intended to be used with unmanaged process creation
    /// scenarios.</remarks>
    /// <param name="startupInfoEx">The process's <see
    /// cref="STARTUPINFOEXW"/>.</param>
    /// <param name="processInformation">The process's
    /// <see cref="PROCESS_INFORMATION"/>.</param>
    internal sealed class Process(STARTUPINFOEXW startupInfoEx, PROCESS_INFORMATION processInformation) : IDisposable
    {
        private bool _disposedValue;

        public STARTUPINFOEXW StartupInfoEx { get; } = startupInfoEx;
        public PROCESS_INFORMATION ProcessInformation { get; } = processInformation;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing){
                }

                // Free the attribute list
                if (StartupInfoEx.lpAttributeList != IntPtr.Zero)
                {
                    PInvoke.DeleteProcThreadAttributeList(StartupInfoEx.lpAttributeList);
                    Marshal.FreeHGlobal(StartupInfoEx.lpAttributeList);
                }

                // Close process and thread handles
                if (ProcessInformation.hProcess != IntPtr.Zero)
                {
                    PInvoke.CloseHandle(ProcessInformation.hProcess);
                }

                if (ProcessInformation.hThread != IntPtr.Zero)
                {
                    PInvoke.CloseHandle(ProcessInformation.hThread);
                }

                _disposedValue = true;
            }
        }

        ~Process()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool
            // disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool
            // disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
