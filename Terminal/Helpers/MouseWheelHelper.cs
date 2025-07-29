using Microsoft.UI.Input;
using Spakov.AnsiProcessor.Ansi.EscapeSequences;
using Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions;
using System;
using System.Text;

namespace Spakov.Terminal.Helpers
{
    /// <summary>
    /// Methods for responding to the mouse wheel.
    /// </summary>
    internal static class MouseWheelHelper
    {
        /// <summary>
        /// <c>WHEEL_DELTA</c>.
        /// </summary>
        internal const int MouseWheelDelta = 120;

        private static int s_scrollRemainder = 0;

        /// <summary>
        /// Invoked when the user scrolls on the terminal.
        /// </summary>
        /// <param name="terminalControl">A <see
        /// cref="TerminalControl"/>.</param>
        /// <param name="pointerPoint">The <see cref="PointerPoint"/> from <see
        /// cref="TerminalControl.Canvas_PointerWheelChanged"/>.</param>
        internal static void HandleMouseWheel(TerminalControl terminalControl, PointerPoint pointerPoint)
        {
            if (pointerPoint.Properties.IsHorizontalMouseWheel)
            {
                return;
            }

            int delta = 0;
            s_scrollRemainder += pointerPoint.Properties.MouseWheelDelta;

            if (Math.Abs(s_scrollRemainder) >= 120)
            {
                delta = s_scrollRemainder / 120;
                s_scrollRemainder -= delta * 120;
            }

            if (delta > 0)
            {
                lock (terminalControl.TerminalEngine.ScreenBufferLock)
                {
                    terminalControl.TerminalEngine.VideoTerminal.ShiftFromScrollback((uint)(delta * terminalControl.LinesPerWheelScrollback));
                }
            }
            else
            {
                lock (terminalControl.TerminalEngine.ScreenBufferLock)
                {
                    terminalControl.TerminalEngine.VideoTerminal.ShiftToScrollback((uint)Math.Abs(delta * terminalControl.LinesPerWheelScrollback));
                }
            }

            if (delta == 0)
            {
                return;
            }

            (int row, int column) = terminalControl.TerminalEngine.VideoTerminal.PointToCellIndices(pointerPoint.Position);
            if (row < 0 || column < 0)
            {
                return;
            }

            // Handle mouse tracking
            if (
                terminalControl.TerminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
                || terminalControl.TerminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
                || terminalControl.TerminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
            )
            {
                if (
                    !terminalControl.TerminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)
                    && !terminalControl.TerminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)
                )
                {
                    // For mouse tracking
                    byte cb = 0x20;

                    if (delta > 0)
                    {
                        cb += 0x00;
                    }

                    if (delta < 0)
                    {
                        cb += 0x01;
                    }

                    // This is a scroll event
                    cb += 0x40;

                    if (row > 0xff - 0x20 || column > 0xff - 0x20)
                    {
                        cb = byte.MaxValue;
                    }

                    if (cb < byte.MaxValue)
                    {
                        terminalControl.TerminalEngine.AnsiWriter?.SendEscapeSequence(
                            [
                                (byte)Fe.CSI,
                                (byte)CSI_MouseTracking.MOUSE_TRACKING_LEADER,
                                cb,
                                (byte)(column + 1 + 0x20),
                                (byte)(row + 1 + 0x20)
                            ]
                        );
                    }
                }
                else
                {
                    // For mouse tracking
                    uint cb = 0x00;

                    if (delta > 0)
                    {
                        cb += 0x00;
                    }

                    if (delta < 0)
                    {
                        cb += 0x01;
                    }

                    // This is a scroll event
                    cb += 0x40;

                    StringBuilder mouseReport = new();

                    if (terminalControl.TerminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR))
                    {
                        mouseReport.Append(Fe.CSI);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                        mouseReport.Append(cb);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(column + 1);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(row + 1);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
                    }
                    else if (terminalControl.TerminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel))
                    {
                        mouseReport.Append(Fe.CSI);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                        mouseReport.Append(cb);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(pointerPoint.Position.X);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(pointerPoint.Position.Y);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
                    }

                    terminalControl.TerminalEngine.AnsiWriter?.SendEscapeSequence(
                        Encoding.ASCII.GetBytes(mouseReport.ToString())
                    );
                }
            }
        }
    }
}
