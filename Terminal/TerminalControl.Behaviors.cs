using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Spakov.AnsiProcessor.Ansi.EscapeSequences;
using Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions;
using System;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace Spakov.Terminal
{
    public sealed partial class TerminalControl : UserControl
    {
        /// <summary>
        /// Reports window focus state.
        /// </summary>
        /// <remarks>See <see
        /// href="https://github.com/microsoft/terminal/commit/a496af361458dcf6c185c1d7923b78f7a21017ec"
        /// />. ConPTY expects these regardless of <c>ESC CSI ? 1004</c>
        /// state.</remarks>
        internal void ReportFocus() => _terminalEngine.SendEscapeSequence([
            (byte)'[',
            (byte)(_hasFocus ? 'I' : 'O')
        ]);

        /// <summary>
        /// Pastes text from the clipboard.
        /// </summary>
        internal async void PasteFromClipboard()
        {
            DataPackageView dataPackageView = Clipboard.GetContent();

            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                StringBuilder toPaste = new(await dataPackageView.GetTextAsync());

                if (_terminalEngine.BracketedPasteMode)
                {
                    toPaste.Insert(0, CSI_BracketedPasteMode.BRACKETED_PASTE_MODE_START);
                    toPaste.Append(CSI_BracketedPasteMode.BRACKETED_PASTE_MODE_END);
                }

                _terminalEngine.SendText(toPaste.ToString());
            }
        }

        /// <summary>
        /// Reports the actual window size (DECSET 2048).
        /// </summary>
        internal void ReportWindowSize()
        {
            if (_terminalEngine.ReportResize)
            {
                StringBuilder sizeReport = new();

                sizeReport.Append(Fe.CSI);
                sizeReport.Append(Rows);
                sizeReport.Append(CSI_DECSET.DECSET_WINDOW_RESIZE_SEPARATOR);
                sizeReport.Append(Columns);
                sizeReport.Append(CSI_DECSET.DECSET_WINDOW_RESIZE_SEPARATOR);
                sizeReport.Append(Canvas.ActualHeight);
                sizeReport.Append(CSI_DECSET.DECSET_WINDOW_RESIZE_SEPARATOR);
                sizeReport.Append(Canvas.ActualWidth);
                sizeReport.Append(CSI_DECSET.DECSET_WINDOW_RESIZE_TERMINATOR);

                _terminalEngine.SendEscapeSequence(Encoding.ASCII.GetBytes(sizeReport.ToString()));

                logger?.LogDebug("Reported window size ({rows};{columns};{height};{width})", Rows, Columns, Canvas.ActualHeight, Canvas.ActualWidth);
            }
        }
    }
}
