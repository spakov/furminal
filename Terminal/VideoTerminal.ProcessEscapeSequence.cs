using Microsoft.Extensions.Logging;
using Spakov.AnsiProcessor.Ansi.EscapeSequences;
using Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions;
using Spakov.AnsiProcessor.Helpers;
using Spakov.AnsiProcessor.Output.EscapeSequences;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe.CSI;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe.CSI.SGR;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe.OSC;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fp;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fs;
using System;
using System.Text;

namespace Spakov.Terminal
{
    internal partial class VideoTerminal
    {
        /// <summary>
        /// Processes <paramref name="escapeSequence"/>.
        /// </summary>
        /// <param name="escapeSequence">The escape sequence.</param>
        internal void ProcessEscapeSequence(EscapeSequence escapeSequence)
        {
            _logger?.LogDebug("Handling escape sequence \"{escapeSequence}\"", escapeSequence.RawEscapeSequence);

            bool handled = false;

            // Select graphic rendition (SGR) escape sequence. These are (very
            // probably) most common.
            if (escapeSequence is SGREscapeSequence sgrEscapeSequence)
            {
                _graphicRendition.MergeFrom(sgrEscapeSequence);

                if (_terminalEngine.UseBackgroundColorErase)
                {
                    _backgroundColorErase = _graphicRendition.BackgroundColor;
                }

                handled = true;

                // Some other kind of control sequence introducer (CSI)
                // command, which are very likely the next most common
            }
            else if (escapeSequence is CSIEscapeSequence csiEscapeSequence)
            {
                // What kind of CSI escape sequence is this?
                switch (csiEscapeSequence.Type)
                {
                    // Insert character or scroll left (@)
                    case CSI.ICH:
                        if (csiEscapeSequence.Variant != CSI.SL[0])
                        {
                            if (_autoWrapMode)
                            {
                                WrapPending = false;
                            }

                            for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                            {
                                WriteGraphemeCluster(null);
                            }

                            handled = true;
                        }
                        else
                        {
                            for (int row = _scrollRegionTop; row <= _scrollRegionBottom; row++)
                            {
                                for (int col = 0; col < _screenBuffer[row].Length; col++)
                                {
                                    if (col + csiEscapeSequence.Ps![0] < _screenBuffer[row].Length)
                                    {
                                        _screenBuffer[row][col] = _screenBuffer[row][col + csiEscapeSequence.Ps![0]];
                                    }
                                    else
                                    {
                                        _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                                        _screenBuffer[row][col] = new()
                                        {
                                            GraphicRendition = _graphicRendition,
                                            TransparentEligible = _transparentEligible
                                        };

                                        if (_terminalEngine.UseBackgroundColorErase)
                                        {
                                            _screenBuffer[row][col].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                        }
                                    }
                                }
                            }

                            handled = true;
                        }

                        break;

                    // Cursor up or scroll right (A)
                    case CSI.CUU:
                        if (csiEscapeSequence.Variant != CSI.SL[0])
                        {
                            if (_autoWrapMode)
                            {
                                WrapPending = false;
                            }

                            for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                            {
                                CaretUp();
                            }

                            handled = true;
                        }
                        else
                        {
                            for (int row = _scrollRegionTop; row <= _scrollRegionBottom; row++)
                            {
                                for (int col = 0; col < _screenBuffer[row].Length - csiEscapeSequence.Ps![0]; col++)
                                {
                                    if (col - csiEscapeSequence.Ps![0] >= 0)
                                    {
                                        _screenBuffer[row][col + csiEscapeSequence.Ps![0]] = _screenBuffer[row][col];
                                    }
                                    else
                                    {
                                        _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                                        _screenBuffer[row][col] = new()
                                        {
                                            GraphicRendition = _graphicRendition,
                                            TransparentEligible = _transparentEligible
                                        };

                                        if (_terminalEngine.UseBackgroundColorErase)
                                        {
                                            _screenBuffer[row][col].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                        }
                                    }
                                }
                            }

                            handled = true;
                        }

                        break;

                    // Cursor down (B)
                    case CSI.CUD:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            CaretDown();
                        }

                        handled = true;

                        break;

                    // Cursor forward (C)
                    case CSI.CUF:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            CaretRight();
                        }

                        handled = true;

                        break;

                    // Cursor back (D)
                    case CSI.CUB:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            CaretLeft();
                        }

                        handled = true;

                        break;

                    // Cursor next line (E)
                    case CSI.CNL:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            CaretDown();
                            Column = 0;
                        }

                        handled = true;

                        break;

                    // Cursor previous line (F)
                    case CSI.CPL:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            CaretUp();
                            Column = 0;
                        }

                        handled = true;

                        break;

                    // Cursor horizontal absolute (G)
                    case CSI.CHA:

                    // Character position absolute (`)
                    case CSI.HPA:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        Column = csiEscapeSequence.Ps![0] >= 1
                            ? csiEscapeSequence.Ps![0] <= _terminalEngine.Columns - 1
                                ? csiEscapeSequence.Ps![0] - 1
                                : _terminalEngine.Columns - 1
                            : 0;

                        handled = true;

                        break;

                    // Cursor position (H)
                    case CSI.CUP:

                    // Horizontal and vertical position (f)
                    case CSI.HVP:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        Row = csiEscapeSequence.Ps![0] >= 1
                            ? csiEscapeSequence.Ps![0] <= _terminalEngine.Rows - 1
                                ? csiEscapeSequence.Ps![0] - 1
                                : _terminalEngine.Rows - 1
                            : 0;

                        Column = csiEscapeSequence.Ps![1] >= 1
                            ? csiEscapeSequence.Ps![1] <= _terminalEngine.Columns - 1
                                ? csiEscapeSequence.Ps![1] - 1
                                : _terminalEngine.Columns - 1
                            : 0;

                        if (_originMode)
                        {
                            Row += _scrollRegionTop - 1;

                            if (Row > _scrollRegionBottom)
                            {
                                Row = _scrollRegionBottom - 1;
                            }
                        }

                        handled = true;

                        break;

                    // Cursor forward tabulation (I)
                    case CSI.CHT:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            NextTabStop();
                        }

                        handled = true;

                        break;

                    // Erase in display (J)
                    case CSI.ED:
                        if (csiEscapeSequence.Ps![0] is >= ((int)ScreenClearType.After) and <= ((int)ScreenClearType.EntireWithScrollback))
                        {
                            ClearScreen((ScreenClearType)csiEscapeSequence.Ps![0]);

                            handled = true;
                        }

                        break;

                    // Erase in line (K)
                    case CSI.EL:
                        if (csiEscapeSequence.Ps![0] is >= ((int)LineClearType.After) and <= ((int)LineClearType.Entire))
                        {
                            ClearLine((LineClearType)csiEscapeSequence.Ps![0]);

                            handled = true;
                        }

                        break;

                    // Insert line (L)
                    case CSI.IL:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            NextRow();
                            ClearLine(LineClearType.Entire);
                        }

                        handled = true;

                        break;

                    // Delete line (M)
                    case CSI.DL:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            ClearLine(LineClearType.Entire);
                            PreviousRow();
                        }

                        handled = true;

                        break;

                    // Delete character (P)
                    case CSI.DCH:
                        _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            _screenBuffer[Row][Column] = new()
                            {
                                GraphicRendition = _graphicRendition,
                                TransparentEligible = _transparentEligible
                            };

                            if (_terminalEngine.UseBackgroundColorErase)
                            {
                                _screenBuffer[Row][Column].GraphicRendition.BackgroundColor = _backgroundColorErase;
                            }

                            CaretLeft();
                        }

                        handled = true;

                        break;

                    // Scroll up or report position on title-stack (S)
                    case CSI.SU:
                        // Check for XTTITLEPOS
                        if (csiEscapeSequence.Variant == CSI.XTTITLEPOS[0])
                        {
                            StringBuilder response = new();

                            response.Append(Fe.CSI);
                            response.Append(_windowTitleStackLength);
                            response.Append(CSI.XTTITLEPOS_SEPARATOR);
                            response.Append(10);
                            response.Append(CSI.XTTITLEPOS);

                            _terminalEngine.SendEscapeSequence(
                                Encoding.ASCII.GetBytes(response.ToString())
                            );

                        } // This is an SU
                        else
                        {
                            if (_scrollbackBuffer is null || _scrollforwardBuffer is null)
                            {
                                for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                                {
                                    if (_screenBuffer.Count == _terminalEngine.Rows - 1)
                                    {
                                        _screenBuffer.RemoveAt(0);
                                    }

                                    _screenBuffer.Add(new Cell[_terminalEngine.Columns]);
                                    _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                                    for (int col = 0; col < _terminalEngine.Columns; col++)
                                    {
                                        _screenBuffer[_terminalEngine.Rows - 1][col] = new()
                                        {
                                            GraphicRendition = _graphicRendition,
                                            TransparentEligible = _transparentEligible
                                        };

                                        if (_terminalEngine.UseBackgroundColorErase)
                                        {
                                            _screenBuffer[_terminalEngine.Rows - 1][col].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ShiftToScrollback((uint)csiEscapeSequence.Ps![0], force: true);
                            }
                        }

                        handled = true;

                        break;

                    // Scroll down (T)
                    case CSI.SD:
                        if (_scrollbackBuffer is null || _scrollforwardBuffer is null)
                        {
                            for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                            {
                                if (_screenBuffer.Count == _terminalEngine.Rows - 1)
                                {
                                    _screenBuffer.RemoveAt(_terminalEngine.Rows - 1);
                                }

                                _screenBuffer.Insert(0, new Cell[_terminalEngine.Columns]);
                                _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                                for (int col = 0; col < _terminalEngine.Columns; col++)
                                {
                                    _screenBuffer[_terminalEngine.Rows - 1][col] = new()
                                    {
                                        GraphicRendition = _graphicRendition,
                                        TransparentEligible = _transparentEligible
                                    };

                                    if (_terminalEngine.UseBackgroundColorErase)
                                    {
                                        _screenBuffer[_terminalEngine.Rows - 1][col].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                    }
                                }
                            }
                        }
                        else
                        {
                            ShiftFromScrollback((uint)csiEscapeSequence.Ps![0], force: true);
                        }

                        handled = true;

                        break;

                    // Erase character (X)
                    case CSI.ECH:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                        if (csiEscapeSequence.Ps![0] < 1)
                        {
                            csiEscapeSequence.Ps![0] = 1;
                        }

                        for (int j = Column; j < Math.Min(Column + csiEscapeSequence.Ps![0], _terminalEngine.Columns); j++)
                        {
                            _screenBuffer[Row][j] = new()
                            {
                                GraphicRendition = _graphicRendition,
                                TransparentEligible = _transparentEligible
                            };

                            if (_terminalEngine.UseBackgroundColorErase)
                            {
                                _screenBuffer[Row][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                            }
                        }

                        handled = true;

                        break;

                    // Cursor backward tabulation (Z)
                    case CSI.CBT:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            PreviousTabStop();
                        }

                        handled = true;

                        break;

                    // Character position relative (a)
                    case CSI.HPR:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        if (csiEscapeSequence.Ps![0] < 0)
                        {
                            for (int i = csiEscapeSequence.Ps![0]; i < 0; i++)
                            {
                                CaretLeft();
                            }
                        }
                        else if (csiEscapeSequence.Ps![0] > 0)
                        {
                            for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                            {
                                CaretRight();
                            }
                        }

                        handled = true;

                        break;

                    // Repetition (b)
                    case CSI.REP:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        Cell source;

                        if (Column == 0)
                        {
                            if (Row == 0)
                            {
                                break;
                            }

                            source = _screenBuffer[Row - 1][0];
                        }
                        else
                        {
                            source = _screenBuffer[Row][Column - 1];
                        }

                        for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                        {
                            WriteGraphemeCluster(source.GraphemeCluster);
                        }

                        handled = true;

                        break;

                    // Line position absolute (d)
                    case CSI.VPA:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        Row = csiEscapeSequence.Ps![0] >= 1
                            ? csiEscapeSequence.Ps![0] <= _terminalEngine.Rows
                                ? csiEscapeSequence.Ps![0] - 1
                                : _terminalEngine.Rows - 1
                            : 0;

                        handled = true;

                        break;

                    // Line position relative (e)
                    case CSI.VPR:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        if (csiEscapeSequence.Ps![0] < 0)
                        {
                            for (int i = csiEscapeSequence.Ps![0]; i < 0; i++)
                            {
                                CaretUp();
                            }
                        }
                        else if (csiEscapeSequence.Ps![0] > 0)
                        {
                            for (int i = 0; i < csiEscapeSequence.Ps![0]; i++)
                            {
                                CaretDown();
                            }
                        }

                        handled = true;

                        break;

                    // Tab clear (g)
                    case CSI.TBC:
                        if (csiEscapeSequence.Ps![0] == CSI.TBC_CLEAR_CURRENT_COLUMN)
                        {
                            if (_tabStops.Contains(Column))
                            {
                                _tabStops.Remove(Column);
                            }
                        }
                        else if (csiEscapeSequence.Ps![0] == CSI.TBC_CLEAR_ALL)
                        {
                            _tabStops.Clear();
                        }

                        handled = true;

                        break;

                    // xterm key modifier operations (m)
                    case CSI.XTMODKEYS:
                        // XTMODKEYS
                        if (csiEscapeSequence.Variant == CSI_XTMODKEYS.XTMODKEYS)
                        {
                            // This mode is always enabled. Do nothing.
                            handled = true;

                        } // XTQMODKEYS
                        else if (csiEscapeSequence.Variant == CSI_XTMODKEYS.XTMODKEYS)
                        {
                            // This mode is always enabled. Do nothing.
                            handled = true;
                        }

                        break;

                    // Device status report (n)
                    case CSI.DSR:
                        // ConPTY handles DSR_STATUS_REPORT and DSR_RCP for us
                        if (
                            csiEscapeSequence.Ps![0] is not CSI_DSR.DSR_STATUS_REPORT
                            and not CSI_DSR.DSR_RCP
                        )
                        {
                            // Check for DSR_THEME_QUERY
                            if (csiEscapeSequence.Ps![0] == CSI_DSR.DSR_THEME_QUERY)
                            {
                                DSRThemeQueryResponse();
                            }
                        }

                        handled = true;

                        break;

                    // Soft terminal reset (p)
                    case CSI.DECSTR:
                        if (csiEscapeSequence.Variant == '!')
                        {
                            // This is quite a bit less than a RIS, but still,
                            // clean up a lot
                            _terminalEngine.CursorVisible = true;

                            _lastSelection.Row = -1;
                            _lastSelection.Column = -1;

                            _lazySelectionMode = false;
                            _selectionMode = false;
                            _wrapPending = false;
                            _originMode = false;

                            _graphicRendition.InitializeFromPalette(_palette);
                            _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                            if (_terminalEngine.UseBackgroundColorErase)
                            {
                                _backgroundColorErase = _graphicRendition.BackgroundColor;
                            }

                            _autoWrapMode = true;

                            handled = true;
                        }

                        break;

                    // DEC load LEDs or set cursor style (q)
                    case CSI.DECLL:
                        // Check for DECSCUSR
                        if (csiEscapeSequence.RawEscapeSequence.EndsWith(CSI.DECSCUSR))
                        {
                            switch (csiEscapeSequence.Ps![0])
                            {
                                case 0:
                                case 1:
                                    _terminalEngine.CursorStyle = CursorStyle.Block;
                                    _terminalEngine.CursorBlink = true;

                                    handled = true;

                                    break;

                                case 2:
                                    _terminalEngine.CursorStyle = CursorStyle.Block;
                                    _terminalEngine.CursorBlink = false;

                                    handled = true;

                                    break;

                                case 3:
                                    _terminalEngine.CursorStyle = CursorStyle.Underline;
                                    _terminalEngine.CursorBlink = true;

                                    handled = true;

                                    break;

                                case 4:
                                    _terminalEngine.CursorStyle = CursorStyle.Underline;
                                    _terminalEngine.CursorBlink = false;

                                    handled = true;

                                    break;

                                case 5:
                                    _terminalEngine.CursorStyle = CursorStyle.Bar;
                                    _terminalEngine.CursorBlink = true;

                                    handled = true;

                                    break;

                                case 6:
                                    _terminalEngine.CursorStyle = CursorStyle.Bar;
                                    _terminalEngine.CursorBlink = false;

                                    handled = true;

                                    break;
                            }
                        }
                        else
                        {
                            // This is a DECLL, which changes keyboard LED
                            // states, which, in my opinion, is not a thing a
                            // terminal emulator has any business doing. Do
                            // nothing.
                            handled = true;
                        }

                        break;

                    // DEC set scrolling region (r)
                    case CSI.DECSTBM:
                        _scrollRegionTop = csiEscapeSequence.Ps![0];
                        _scrollRegionBottom = csiEscapeSequence.Ps![1];

                        handled = true;

                        break;

                    // Save cursor (s)
                    case CSI.SAVE_CURSOR:
                        _savedCursorPosition = Caret;

                        handled = true;

                        break;

                    // xterm window manipulation (t)
                    case CSI.XTWINOPS:
                        switch (csiEscapeSequence.Ps![0])
                        {
                            // Report the size of the text area in characters
                            // (18)
                            case CSI_XTWINOPS.XTWINOPS_TEXT_AREA_SIZE:
                                StringBuilder textAreaSize = new();

                                textAreaSize.Append(Fe.CSI);
                                textAreaSize.Append(CSI_XTWINOPS.XTWINOPS_TEXT_AREA_SIZE_RESPONSE);
                                textAreaSize.Append(CSI_XTWINOPS.XTWINOPS_SEPARATOR);
                                textAreaSize.Append(_terminalEngine.Rows);
                                textAreaSize.Append(CSI_XTWINOPS.XTWINOPS_SEPARATOR);
                                textAreaSize.Append(_terminalEngine.Columns);
                                textAreaSize.Append(CSI.XTWINOPS);

                                _terminalEngine.AnsiWriter?.SendEscapeSequence(
                                    Encoding.ASCII.GetBytes(textAreaSize.ToString())
                                );

                                handled = true;

                                break;

                            // Save xterm [icon] [and] [window title] on stack
                            // (22)
                            case CSI_XTWINOPS.XTWINOPS_STACK_PUSH:

                            // Restore xterm [icon] [and] [window title] from
                            // stack (23)
                            case CSI_XTWINOPS.XTWINOPS_STACK_POP:
                                int? stackPosition = null;

                                if (csiEscapeSequence.Ps.Count > 2)
                                {
                                    stackPosition = csiEscapeSequence.Ps[2];
                                }

                                // Observation shows that a third parameter of
                                // 0 is used to mean, "just shove the thing on
                                // the stack, please," which is exactly the
                                // same as not specifying it at all
                                if (stackPosition is null or < 1 or > 10)
                                {
                                    if (csiEscapeSequence.Ps![0] == CSI_XTWINOPS.XTWINOPS_STACK_PUSH)
                                    {
                                        if (_windowTitleStackLength < 10)
                                        {
                                            _windowTitleStack[_windowTitleStackLength++] = _terminalEngine.WindowTitle;
                                        }
                                    }
                                    else if (csiEscapeSequence.Ps![0] == CSI_XTWINOPS.XTWINOPS_STACK_POP)
                                    {
                                        if (_windowTitleStackLength > 0)
                                        {
                                            _terminalEngine.WindowTitle = _windowTitleStack[--_windowTitleStackLength];
                                        }
                                    }
                                }
                                else
                                {
                                    if (csiEscapeSequence.Ps![0] == CSI_XTWINOPS.XTWINOPS_STACK_PUSH)
                                    {
                                        if (_windowTitleStackLength < 10)
                                        {
                                            _windowTitleStack[_windowTitleStackLength++] = _terminalEngine.WindowTitle;
                                        }
                                    }
                                    else if (csiEscapeSequence.Ps![0] == CSI_XTWINOPS.XTWINOPS_STACK_POP)
                                    {
                                        if (_windowTitleStackLength > stackPosition)
                                        {
                                            _terminalEngine.WindowTitle = _windowTitleStack[--_windowTitleStackLength];
                                        }
                                    }
                                }

                                handled = true;

                                break;
                        }

                        break;

                    // Restore cursor (u)
                    case CSI.RESTORE_CURSOR:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        if (_savedCursorPosition is not null)
                        {
                            Row = ((Caret)_savedCursorPosition).Row;
                            Column = ((Caret)_savedCursorPosition).Column;
                        }

                        handled = true;

                        break;

                    // DECSET (h)
                    case CSI.DECSET_HIGH:
                        if (csiEscapeSequence.Ps is null)
                        {
                            break;
                        }

                        foreach (int ps in csiEscapeSequence.Ps)
                        {
                            switch (ps)
                            {
                                // Application Cursor Keys (DECCKM), VT100 (1)
                                case CSI_DECSET.DECSET_DECCKM:
                                    _terminalEngine.ApplicationCursorKeys = true;

                                    handled = true;

                                    break;

                                // Designate USASCII for character sets G0-G3
                                // (DECANM), VT100, and set VT100 mode (2)
                                case CSI_DECSET.DECSET_DECANM:
                                    // Not sure this is useful today. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // 132 Column Mode (DECCOLM), VT100 (3)
                                case CSI_DECSET.DECSET_DECCOLM:
                                    // Not sure this is useful today. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Smooth (Slow) Scroll (DECSCLM), VT100 (4)
                                case CSI_DECSET.DECSET_DECSCLM:
                                    // Not sure this is useful today. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Reverse Video (DECSCNM), VT100 (5)
                                case CSI_DECSET.DECSET_DECSCNM:
                                    _graphicRendition.Inverse = true;

                                    handled = true;

                                    break;

                                // Origin Mode (DECOM), VT100 (6)
                                case CSI_DECSET.DECSET_DECOM:
                                    _originMode = true;

                                    handled = true;

                                    break;

                                // Auto-Wrap Mode (DECAWM), VT100 (7)
                                case CSI_DECSET.DECSET_DECAWM:
                                    _autoWrapMode = true;

                                    handled = true;

                                    break;

                                // Auto-Repeat Keys (DECARM), VT100 (8)
                                case CSI_DECSET.DECSET_DECARM:
                                    _terminalEngine.AutoRepeatKeys = true;

                                    handled = true;

                                    break;

                                // Send Mouse X & Y on button press (X10) (9)
                                case CSI_DECSET.DECSET_XTERM_X10_MOUSE:
                                    _terminalEngine.MouseTrackingMode |= MouseTrackingModes.X10;

                                    handled = true;

                                    break;

                                // Show toolbar (rxvt) (10)
                                case CSI_DECSET.DECSET_RXVT_SHOW_TOOLBAR:
                                    // rxvt-specific, plus toolbars have fallen
                                    // out of favor. Do nothing.
                                    handled = true;

                                    break;

                                // Start blinking cursor (AT&T 610) (12)
                                case CSI_DECSET.DECSET_ATT160:

                                // Start blinking cursor (set only via resource
                                // or menu) (13)
                                case CSI_DECSET.DECSET_XTERM_START_BLINKING_CURSOR:
                                    _terminalEngine.CursorBlink = true;

                                    handled = true;

                                    break;

                                // Enable XOR of blinking cursor control
                                // sequence and menu (14)
                                case CSI_DECSET.DECSET_XTERM_XOR_BLINKING_CURSOR:

                                // Print Form Feed (DECPFF), VT220 (18)
                                case CSI_DECSET.DECSET_DECPFF:

                                // Set print extent to full screen (DECPEX),
                                // VT220 (19)
                                case CSI_DECSET.DECSET_DECPEX:
                                    // Specialized, not sufficiently modern. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Show cursor (DECTCEM), VT220 (25)
                                case CSI_DECSET.DECSET_DECTCEM:
                                    _terminalEngine.CursorVisible = true;

                                    handled = true;

                                    break;

                                // Show scrollbar (rxvt) (30)
                                case CSI_DECSET.DECSET_RXVT_SHOW_SCROLLBAR:

                                // Enable font-shifting functions (rxvt) (35)
                                case CSI_DECSET.DECSET_RXVT_ENABLE_FONT_SHIFTING:
                                    // rxvt-specific. Do nothing.
                                    handled = true;

                                    break;

                                // Enter Tektronix mode (DECTEK), VT240, xterm
                                // (38)
                                case CSI_DECSET.DECSET_DECTEK:

                                // Allow 80 ⇒ 132 mode, xterm (40)
                                case CSI_DECSET.DECSET_XTERM_80_132:

                                // more(1) fix (41)
                                case CSI_DECSET.DECSET_XTERM_MORE_FIX:

                                // Enable National Replacement Character sets
                                // (DECNRCM), VT220 (42)
                                case CSI_DECSET.DECSET_DECNRCM:

                                // Enable Graphic Expanded Print Mode
                                // (DECGEPM), VT340 (43)
                                case CSI_DECSET.DECSET_DECGEPM:

                                // Enable Graphic Print Color Mode (DECGPCM),
                                // VT340 (44), or Turn on margin bell, xterm
                                // (44)
                                case CSI_DECSET.DECSET_DECGPCM:

                                // Enable Graphic Print Color Syntax (DECGPCS),
                                // VT340 (45), or Reverse-wraparound mode
                                // (XTREVWRAP), xterm (45)
                                case CSI_DECSET.DECSET_DECGPCS:

                                // Graphic Print Background Mode, VT340 (46),
                                // or Start logging (XTLOGGING), xterm (46)
                                case CSI_DECSET.DECSET_GRAPHIC_PRINT_BACKGROUND_MODE:
                                    // Very specialized. Do nothing.
                                    handled = true;

                                    break;

                                // Use Alternate Screen Buffer, xterm (47), or
                                // Enable Graphic Rotated Print Mode (DECGRPM),
                                // VT340 (47)
                                case CSI_DECSET.DECSET_ALTERNATE_SCREEN_BUFFER:
                                    UseAlternateScreenBuffer = true;

                                    handled = true;

                                    break;

                                // Application keypad mode (DECNKM), VT320 (66)
                                case CSI_DECSET.DECSET_DECNKM:
                                    // Pretty much unused these days. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Backarrow key sends backspace (DECBKM),
                                // VT340, VT420 (67)
                                case CSI_DECSET.DECSET_DECBKM:
                                    // Subverts user expectations. Do nothing.
                                    handled = true;

                                    break;

                                // Enable left and right margin mode (DECLRMM),
                                // VT420 and up (69)
                                case CSI_DECSET.DECSET_DECLRMM:

                                // Enable Sixel Display Mode (DECSDM), VT330,
                                // VT340, VT382 (80)
                                case CSI_DECSET.DECSET_DECSDM:

                                // Do not clear screen when DECCOLM is
                                // set/reset (DECNCSM), VT510 and up (95)
                                case CSI_DECSET.DECSET_DECNCSM:
                                    // Highly specialized. Do nothing.
                                    handled = true;

                                    break;

                                // Send Mouse X & Y on button press and release
                                // (X11) (1000)
                                case CSI_DECSET.DECSET_XTERM_X11_MOUSE:
                                    _terminalEngine.MouseTrackingMode |= MouseTrackingModes.X11;

                                    handled = true;

                                    break;

                                // Use Hilite Mouse Tracking, xterm (1001)
                                case CSI_DECSET.DECSET_XTERM_HILITE_MOUSE_TRACKING:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Use Cell Motion Mouse Tracking, xterm (1002)
                                case CSI_DECSET.DECSET_XTERM_CELL_MOTION_MOUSE_TRACKING:
                                    _terminalEngine.MouseTrackingMode |= MouseTrackingModes.CellMotion;

                                    handled = true;

                                    break;

                                // Use All Motion Mouse Tracking, xterm (1003)
                                case CSI_DECSET.DECSET_XTERM_ALL_MOTION_MOUSE_TRACKING:
                                    _terminalEngine.MouseTrackingMode |= MouseTrackingModes.AllMotion;

                                    handled = true;

                                    break;

                                // Send FocusIn/FocusOut events, xterm (1004)
                                case CSI_DECSET.DECSET_XTERM_FOCUSIN_FOCUSOUT:
                                    // Do nothing. This is always enabled
                                    // because that's what ConPTY expects. See
                                    // TerminalControl.HasFocus for more on
                                    // this.
                                    handled = true;

                                    break;

                                // Enable UTF-8 Mouse Mode, xterm (1005)
                                case CSI_DECSET.DECSET_XTERM_UTF8_MOUSE_MODE:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Enable SGR Mouse Mode, xterm (1006)
                                case CSI_DECSET.DECSET_XTERM_SGR_MOUSE_MODE:
                                    _terminalEngine.MouseTrackingMode |= MouseTrackingModes.SGR;

                                    handled = true;

                                    break;

                                // Enable Alternate Scroll Mode, xterm (1007)
                                case CSI_DECSET.DECSET_XTERM_ALTERNATE_SCROLL_MODE:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Scroll to bottom on tty output (rxvt) (1010)
                                case CSI_DECSET.DECSET_RXVT_SCROLL_TO_BOTTOM_ON_OUTPUT:

                                // Scroll to bottom on key press (rxvt) (1011)
                                case CSI_DECSET.DECSET_RXVT_SCROLL_TO_BOTTOM_ON_KEY_PRESS:
                                    // These are always active. Do nothing.
                                    handled = true;

                                    break;

                                // Enable fastScroll resource, xterm (1014)
                                case CSI_DECSET.DECSET_XTERM_FAST_SCROLL:
                                    // Not really needed on modern computers.
                                    // Do nothing.
                                    handled = true;

                                    break;

                                // Enable urxvt Mouse Mode (1015)
                                case CSI_DECSET.DECSET_URXVT_MOUSE_MODE:
                                    // From Mr. Dickey: "The 1015 control is
                                    // not recommended; it is not an
                                    // improvement over 1006." Do nothing.
                                    handled = true;

                                    break;

                                // Enable SGR Mouse PixelMode, xterm (1016)
                                case CSI_DECSET.DECSET_XTERM_SGR_MOUSE_PIXEL_MODE:
                                    _terminalEngine.MouseTrackingMode |= MouseTrackingModes.Pixel;

                                    handled = true;

                                    break;

                                // Interpret "meta" key, xterm (1034)
                                case CSI_DECSET.DECSET_XTERM_INTERPRET_META_KEY:
                                    // For all practical intents and purposes,
                                    // Alt and Meta are the same thing these
                                    // days. Do nothing.
                                    handled = true;

                                    break;

                                // Enable special modifiers for Alt and NumLock
                                // keys, xterm (1035)
                                case CSI_DECSET.DECSET_XTERM_SPECIAL_MODIFIERS:
                                    // Sounds fairly Sun-specific, and I do not
                                    // believe this is widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Send ESC when Meta modifies a key, xterm
                                // (1036)
                                case CSI_DECSET.DECSET_XTERM_SEND_ESC_ON_META:
                                    // This is always enabled. Do nothing.
                                    handled = true;

                                    break;

                                // Send DEL from the editing-keypad Delete key,
                                // xterm (1037)
                                case CSI_DECSET.DECSET_XTERM_SEND_DEL:
                                    // Modern systems do not have an
                                    // editing-keypad Delete key. Do nothing.
                                    handled = true;

                                    break;

                                // Send ESC when Alt modifies a key, xterm
                                // (1039)
                                case CSI_DECSET.DECSET_XTERM_SEND_ESC_ON_ALT:
                                    // This is always enabled. Do nothing.
                                    handled = true;

                                    break;

                                // Keep selection even if not highlighted,
                                // xterm (1040)
                                case CSI_DECSET.DECSET_XTERM_KEEP_SELECTION:

                                // Use the CLIPBOARD selection, xterm (1041)
                                case CSI_DECSET.DECSET_XTERM_SELECT_TO_CLIPBOARD:
                                    // Subverts user expectations and handled
                                    // by TerminalControl. Do nothing.
                                    handled = true;

                                    break;

                                // Enable Urgency window manager hint when
                                // Control - G is received, xterm (1042)
                                case CSI_DECSET.DECSET_XTERM_BELL_IS_URGENT:

                                // Enable raising of the window when Control-G
                                // is received, xterm (1043)
                                case CSI_DECSET.DECSET_XTERM_POP_ON_BELL:
                                    // X-specific and/or have the potential for
                                    // abuse (or at least annoyance). Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Reuse the most recent data copied to
                                // CLIPBOARD, xterm (1044)
                                case CSI_DECSET.DECSET_XTERM_KEEP_CLIPBOARD:
                                    // Subverts user expectations and handled
                                    // by TerminalControl. Do nothing.
                                    handled = true;

                                    break;

                                // Extended Reverse-wraparound mode
                                // (XTREVWRAP2), xterm (1045)
                                case CSI_DECSET.DECSET_XTREVWRAP2:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Enable switching to/from Alternate Screen
                                // Buffer, xterm (1046)
                                case CSI_DECSET.DECSET_XTERM_ALTERNATE_SCREEN_BUFFER:
                                    // This is always enabled. Do nothing.
                                    handled = true;

                                    break;

                                // Use Alternate Screen Buffer, xterm (1047)
                                case CSI_DECSET.DECSET_XTERM_USE_ALTERNATE_SCREEN_BUFFER:
                                    UseAlternateScreenBuffer = true;
                                    ClearScreen(ScreenClearType.Entire);

                                    handled = true;

                                    break;

                                // Save cursor as in DECSC, xterm (1048)
                                case CSI_DECSET.DECSET_XTERM_SAVE_CURSOR:
                                    _savedCursorState = new(this);

                                    handled = true;

                                    break;

                                // Save cursor as in DECSC, xterm. After saving
                                // he cursor, switch to the Alternate Screen
                                // Buffer, clearing it first. This control
                                // combines the effects of the 1 0 4 7 and
                                // 1 0 4 8 modes. (1049)
                                case CSI_DECSET.DECSET_XTERM_SAVE_CURSOR_AND_USE_ASB:
                                    _savedCursorState = new(this);
                                    UseAlternateScreenBuffer = true;
                                    ClearScreen(ScreenClearType.Entire);

                                    handled = true;

                                    break;

                                // Set terminfo/termcap function-key mode,
                                // xterm (1050)
                                case CSI_DECSET.DECSET_XTERM_SET_TERMINFO_TERMCAP_F_KEY:

                                // Set Sun function-key mode, xterm (1051)
                                case CSI_DECSET.DECSET_XTERM_SET_SUN_F_KEY:

                                // Set HP function-key mode, xterm (1052)
                                case CSI_DECSET.DECSET_XTERM_SET_HP_F_KEY:

                                // Set SCO function-key mode, xterm (1053)
                                case CSI_DECSET.DECSET_XTERM_SET_SCO_F_KEY:

                                // Set legacy keyboard emulation, i.e, X11R6,
                                // xterm (1060)
                                case CSI_DECSET.DECSET_XTERM_LEGACY_KEYBOARD_EMULATION:

                                // Set VT220 keyboard emulation, xterm (1061)
                                case CSI_DECSET.DECSET_XTERM_VT220_KEYBOARD_EMULATION:
                                    // These are highly specialized and not
                                    // relevant to modern/Windows systems. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Enable readline mouse button-1, xterm (2001)
                                case CSI_DECSET.DECSET_XTERM_READLINE_MOUSE_BUTTON_1:

                                // Enable readline mouse button-2, xterm (2002)
                                case CSI_DECSET.DECSET_XTERM_READLINE_MOUSE_BUTTON_2:

                                // Enable readline mouse button-3, xterm (2003)
                                case CSI_DECSET.DECSET_XTERM_READLINE_MOUSE_BUTTON_3:
                                    // Not really seeing the utility in this.
                                    // See
                                    // https://github.com/termux/termux-app/issues/4302#issuecomment-2563385400
                                    // for an explanation of what these
                                    // actually do. Do nothing.
                                    handled = true;

                                    break;

                                // Set bracketed paste mode, xterm (2004)
                                case CSI_DECSET.DECSET_XTERM_BRACKETED_PASTE_MODE:
                                    _terminalEngine.BracketedPasteMode = true;

                                    handled = true;

                                    break;

                                // Enable readline character-quoting, xterm
                                // (2005)
                                case CSI_DECSET.DECSET_XTERM_READLINE_CHARACTER_QUOTING:

                                // Enable readline newline pasting, xterm
                                // (2006)
                                case CSI_DECSET.DECSET_XTERM_READLINE_NEWLINE_PASTING:
                                    // Not really seeing the utility in this.
                                    // Presumably works with the same use case
                                    // as 2001-2003. Do nothing.
                                    handled = true;

                                    break;

                                // Theme change notification (2031)
                                case CSI_DECSET.DECSET_THEME_CHANGE:
                                    _reportPaletteUpdate = true;

                                    handled = true;

                                    break;

                                // In-band window resize notification (2048)
                                case CSI_DECSET.DECSET_WINDOW_RESIZE:
                                    _terminalEngine.ReportResize = true;

                                    handled = true;

                                    break;

                                // ConPTY win32-input-mode (9001)
                                case CSI_DECSET.DECSET_WIN32_INPUT_MODE:
                                    // Do nothing. This is a Windows
                                    // Terminal-ism and is safe to disregard.
                                    // See CSI.DECSET_WIN32_INPUT_MODE for more
                                    // on this.
                                    handled = true;

                                    break;
                            }
                        }

                        break;

                    // DECRST (l)
                    case CSI.DECSET_LOW:
                        if (csiEscapeSequence.Ps is null)
                        {
                            break;
                        }

                        foreach (int ps in csiEscapeSequence.Ps)
                        {
                            switch (ps)
                            {
                                // Normal Cursor Keys (DECCKM), VT100 (1)
                                case CSI_DECSET.DECSET_DECCKM:
                                    _terminalEngine.ApplicationCursorKeys = false;

                                    handled = true;

                                    break;

                                // Designate VT52 mode (DECANM), VT100 (2)
                                case CSI_DECSET.DECSET_DECANM:
                                    // Not sure this is useful today. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // 80 Column Mode (DECCOLM), VT100 (3)
                                case CSI_DECSET.DECSET_DECCOLM:
                                    // Not sure this is useful today. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Jump (Fast) Scroll (DECSCLM) (4)
                                case CSI_DECSET.DECSET_DECSCLM:
                                    // Not sure this is useful today. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Normal Video (DECSCNM), VT100 (5)
                                case CSI_DECSET.DECSET_DECSCNM:
                                    _graphicRendition.Inverse = false;

                                    handled = true;

                                    break;

                                // Normal Cursor Mode (DECOM), VT100 (6)
                                case CSI_DECSET.DECSET_DECOM:
                                    _originMode = false;

                                    handled = true;

                                    break;

                                // No Auto-Wrap Mode (DECAWM), VT100 (7)
                                case CSI_DECSET.DECSET_DECAWM:
                                    _autoWrapMode = false;

                                    handled = true;

                                    break;

                                // No Auto-Repeat Keys (DECARM), VT100 (8)
                                case CSI_DECSET.DECSET_DECARM:
                                    _terminalEngine.AutoRepeatKeys = false;

                                    handled = true;

                                    break;

                                // Don't send Mouse X & Y on button press,
                                // xterm (9)
                                case CSI_DECSET.DECSET_XTERM_X10_MOUSE:
                                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X10))
                                    {
                                        _terminalEngine.MouseTrackingMode -= MouseTrackingModes.X10;
                                    }

                                    handled = true;

                                    break;

                                // Hide toolbar (rxvt) (10)
                                case CSI_DECSET.DECSET_RXVT_SHOW_TOOLBAR:
                                    // rxvt-specific, plus toolbars have fallen
                                    // out of favor. Do nothing.
                                    handled = true;

                                    break;

                                // Stop blinking cursor (AT&T 610) (12)
                                case CSI_DECSET.DECSET_ATT160:

                                // Disable blinking cursor (set only via
                                // resource or menu) (13)
                                case CSI_DECSET.DECSET_XTERM_START_BLINKING_CURSOR:
                                    _terminalEngine.CursorBlink = false;

                                    handled = true;

                                    break;

                                // Disable XOR of blinking cursor control
                                // sequence and menu (14)
                                case CSI_DECSET.DECSET_XTERM_XOR_BLINKING_CURSOR:

                                // Don't Print Form Feed (DECPFF), VT220 (18)
                                case CSI_DECSET.DECSET_DECPFF:

                                // Limit print to scrolling region (DECPEX),
                                // VT220 (19)
                                case CSI_DECSET.DECSET_DECPEX:
                                    // Specialized, not sufficiently modern. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Hide cursor (DECTCEM), VT220 (25)
                                case CSI_DECSET.DECSET_DECTCEM:
                                    _terminalEngine.CursorVisible = false;

                                    handled = true;

                                    break;

                                // Don't show scrollbar (rxvt) (30)
                                case CSI_DECSET.DECSET_RXVT_SHOW_SCROLLBAR:

                                // Disable font-shifting functions (rxvt) (35)
                                case CSI_DECSET.DECSET_RXVT_ENABLE_FONT_SHIFTING:
                                    // rxvt-specific. Do nothing.
                                    handled = true;

                                    break;

                                // Disallow 80 ⇒ 132 mode, xterm (40)
                                case CSI_DECSET.DECSET_XTERM_80_132:

                                // No more(1) fix (41)
                                case CSI_DECSET.DECSET_XTERM_MORE_FIX:

                                // Disable National Replacement Character sets
                                // (DECNRCM), VT220 (42)
                                case CSI_DECSET.DECSET_DECNRCM:

                                // Disable Graphic Expanded Print Mode
                                // (DECGEPM), VT340 (43)
                                case CSI_DECSET.DECSET_DECGEPM:

                                // Disable Graphic Print Color Mode (DECGPCM),
                                // VT340 (44), or Turn off margin bell, xterm
                                // (44)
                                case CSI_DECSET.DECSET_DECGPCM:

                                // Disable Graphic Print Color Syntax
                                // (DECGPCS), VT340 (45), or No
                                // Reverse-wraparound mode (XTREVWRAP), xterm
                                // (45)
                                case CSI_DECSET.DECSET_DECGPCS:

                                // Stop logging (XTLOGGING), xterm (46)
                                case CSI_DECSET.DECSET_XTLOGGING:
                                    // Very specialized. Do nothing.
                                    handled = true;

                                    break;

                                // Use Normal Screen Buffer, xterm (47), or
                                // Disable Graphic Rotated Print Mode
                                // (DECGRPM), VT340 (47)
                                case CSI_DECSET.DECSET_ALTERNATE_SCREEN_BUFFER:
                                    UseAlternateScreenBuffer = false;

                                    handled = true;

                                    break;

                                // Numeric keypad mode (DECNKM), VT320 (66)
                                case CSI_DECSET.DECSET_DECNKM:
                                    // Pretty much unused these days. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Backarrow key sends delete (DECBKM), VT340,
                                // VT420 (67)
                                case CSI_DECSET.DECSET_DECBKM:
                                    // Subverts user expectations. Do nothing.
                                    handled = true;

                                    break;

                                // Disable left and right margin mode
                                // (DECLRMM), VT420 and up (69)
                                case CSI_DECSET.DECSET_DECLRMM:

                                // Disable Sixel Display Mode (DECSDM), VT330,
                                // VT340, VT382 (80)
                                case CSI_DECSET.DECSET_DECSDM:

                                // Clear screen when DECCOLM is set/reset
                                // (DECNCSM), VT510 and up (95)
                                case CSI_DECSET.DECSET_DECNCSM:
                                    // Highly specialized. Do nothing.
                                    handled = true;

                                    break;

                                // Don't send Mouse X & Y on button press and
                                // release (X11) (1000)
                                case CSI_DECSET.DECSET_XTERM_X11_MOUSE:
                                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11))
                                    {
                                        _terminalEngine.MouseTrackingMode -= MouseTrackingModes.X11;
                                    }

                                    handled = true;

                                    break;

                                // Don't use Hilite Mouse Tracking, xterm
                                // (1001)
                                case CSI_DECSET.DECSET_XTERM_HILITE_MOUSE_TRACKING:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Don't use Cell Motion Mouse Tracking, xterm
                                // (1002)
                                case CSI_DECSET.DECSET_XTERM_CELL_MOTION_MOUSE_TRACKING:
                                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion))
                                    {
                                        _terminalEngine.MouseTrackingMode -= MouseTrackingModes.CellMotion;
                                    }

                                    handled = true;

                                    break;

                                // Don't use All Motion Mouse Tracking, xterm
                                // (1003)
                                case CSI_DECSET.DECSET_XTERM_ALL_MOTION_MOUSE_TRACKING:
                                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion))
                                    {
                                        _terminalEngine.MouseTrackingMode -= MouseTrackingModes.AllMotion;
                                    }

                                    handled = true;

                                    break;

                                // Don't send FocusIn/FocusOut events, xterm
                                // (1004)
                                case CSI_DECSET.DECSET_XTERM_FOCUSIN_FOCUSOUT:
                                    // Do nothing. This is always enabled
                                    // because that's what ConPTY expects. See
                                    // TerminalControl.HasFocus for more on
                                    // this.
                                    handled = true;

                                    break;

                                // Disable UTF-8 Mouse Mode, xterm (1005)
                                case CSI_DECSET.DECSET_XTERM_UTF8_MOUSE_MODE:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Disable SGR Mouse Mode, xterm (1006)
                                case CSI_DECSET.DECSET_XTERM_SGR_MOUSE_MODE:
                                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR))
                                    {
                                        _terminalEngine.MouseTrackingMode -= MouseTrackingModes.SGR;
                                    }

                                    handled = true;

                                    break;

                                // Disable Alternate Scroll Mode, xterm (1007)
                                case CSI_DECSET.DECSET_XTERM_ALTERNATE_SCROLL_MODE:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Don't scroll to bottom on tty output (rxvt)
                                // (1010)
                                case CSI_DECSET.DECSET_RXVT_SCROLL_TO_BOTTOM_ON_OUTPUT:

                                // Don't scroll to bottom on key press (rxvt)
                                // (1011)
                                case CSI_DECSET.DECSET_RXVT_SCROLL_TO_BOTTOM_ON_KEY_PRESS:
                                    // These are always active. Do nothing.
                                    handled = true;

                                    break;

                                // Disable fastScroll resource, xterm (1014)
                                case CSI_DECSET.DECSET_XTERM_FAST_SCROLL:
                                    // Not really needed on modern computers.
                                    // Do nothing.
                                    handled = true;

                                    break;

                                // Disable urxvt Mouse Mode (1015)
                                case CSI_DECSET.DECSET_URXVT_MOUSE_MODE:
                                    // From Mr. Dickey: "The 1015 control is
                                    // not recommended; it is not an
                                    // improvement over 1006." Do nothing.
                                    handled = true;

                                    break;

                                // Disable SGR Mouse Pixel-Mode, xterm (1016)
                                case CSI_DECSET.DECSET_XTERM_SGR_MOUSE_PIXEL_MODE:
                                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel))
                                    {
                                        _terminalEngine.MouseTrackingMode -= MouseTrackingModes.Pixel;
                                    }

                                    handled = true;

                                    break;

                                // Don't interpret "meta" key, xterm (1034)
                                case CSI_DECSET.DECSET_XTERM_INTERPRET_META_KEY:
                                    // For all practical intents and purposes,
                                    // Alt and Meta are the same thing these
                                    // days. Do nothing.
                                    handled = true;

                                    break;

                                // Disable special modifiers for Alt and
                                // NumLock keys, xterm (1035)
                                case CSI_DECSET.DECSET_XTERM_SPECIAL_MODIFIERS:
                                    // Sounds fairly Sun-specific, and I do not believe this is
                                    // widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Don't send ESC when Meta modifies a key,
                                // xterm (1036)
                                case CSI_DECSET.DECSET_XTERM_SEND_ESC_ON_META:
                                    // This is always enabled. Do nothing.
                                    handled = true;

                                    break;

                                // Send VT220 Remove from the editing-keypad
                                // Delete key, xterm (1037)
                                case CSI_DECSET.DECSET_XTERM_SEND_DEL:
                                    // Modern systems do not have an
                                    // editing-keypad Delete key. Do nothing.
                                    handled = true;

                                    break;

                                // Don't send ESC when Alt modifies a key,
                                // xterm (1039)
                                case CSI_DECSET.DECSET_XTERM_SEND_ESC_ON_ALT:
                                    // This is always enabled. Do nothing.
                                    handled = true;

                                    break;

                                // Do not keep selection when not highlighted,
                                // xterm (1040)
                                case CSI_DECSET.DECSET_XTERM_KEEP_SELECTION:

                                // Use the PRIMARY selection, xterm (1041)
                                case CSI_DECSET.DECSET_XTERM_SELECT_TO_CLIPBOARD:
                                    // Subverts user expectations and handled
                                    // by TerminalControl. Do nothing.
                                    handled = true;

                                    break;

                                // Disable Urgency window manager hint when
                                // Control - G is received, xterm (1042)
                                case CSI_DECSET.DECSET_XTERM_BELL_IS_URGENT:

                                // Disable raising of the window when Control-G
                                // is received, xterm (1043)
                                case CSI_DECSET.DECSET_XTERM_POP_ON_BELL:
                                    // X-specific and/or have the potential for
                                    // abuse (or at least annoyance). Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // No Extended Reverse-wraparound mode
                                // (XTREVWRAP2), xterm (1045)
                                case CSI_DECSET.DECSET_XTREVWRAP2:
                                    // Not widely used. Do nothing.
                                    handled = true;

                                    break;

                                // Disable switching to/from Alternate Screen
                                // Buffer, xterm (1046)
                                case CSI_DECSET.DECSET_XTERM_ALTERNATE_SCREEN_BUFFER:
                                    // This is always enabled. Do nothing.
                                    handled = true;

                                    break;

                                // Use Normal Screen Buffer, xterm. Clear the
                                // screen first if in the Alternate Screen
                                // Buffer. (1047)
                                case CSI_DECSET.DECSET_XTERM_USE_ALTERNATE_SCREEN_BUFFER:
                                    if (UseAlternateScreenBuffer)
                                    {
                                        ClearScreen(ScreenClearType.Entire);
                                        UseAlternateScreenBuffer = false;
                                    }

                                    handled = true;

                                    break;

                                // Restore cursor as in DECRC, xterm (1048)
                                case CSI_DECSET.DECSET_XTERM_SAVE_CURSOR:
                                    if (_savedCursorState is not null)
                                    {
                                        ((CursorState)_savedCursorState).Restore(this);
                                        _savedCursorState = null;
                                    }
                                    else
                                    {
                                        Row = 0;
                                        Column = 0;
                                        _originMode = false;
                                        _graphicRendition.InitializeFromPalette(_palette);
                                    }

                                    handled = true;

                                    break;

                                // Use Normal Screen Buffer and restore cursor
                                // as in DECRC, xterm. This combines the
                                // effects of the 1 0 4 7 and 1 0 4 8 modes.
                                // (1049)
                                case CSI_DECSET.DECSET_XTERM_SAVE_CURSOR_AND_USE_ASB:
                                    if (UseAlternateScreenBuffer)
                                    {
                                        ClearScreen(ScreenClearType.Entire);
                                        UseAlternateScreenBuffer = false;
                                    }

                                    if (_savedCursorState is not null)
                                    {
                                        ((CursorState)_savedCursorState).Restore(this);
                                        _savedCursorState = null;
                                    }
                                    else
                                    {
                                        Row = 0;
                                        Column = 0;
                                        _originMode = false;
                                        _graphicRendition.InitializeFromPalette(_palette);
                                    }

                                    handled = true;

                                    break;

                                // Reset terminfo/termcap function-key mode,
                                // xterm (1050)
                                case CSI_DECSET.DECSET_XTERM_SET_TERMINFO_TERMCAP_F_KEY:

                                // Reset Sun function-key mode, xterm (1051)
                                case CSI_DECSET.DECSET_XTERM_SET_SUN_F_KEY:

                                // Reset HP function-key mode, xterm (1052)
                                case CSI_DECSET.DECSET_XTERM_SET_HP_F_KEY:

                                // Reset SCO function-key mode, xterm (1053)
                                case CSI_DECSET.DECSET_XTERM_SET_SCO_F_KEY:

                                // Reset legacy keyboard emulation, i.e, X11R6,
                                // xterm (1060)
                                case CSI_DECSET.DECSET_XTERM_LEGACY_KEYBOARD_EMULATION:

                                // Reset keyboard emulation to Sun/PC style,
                                // xterm (1061)
                                case CSI_DECSET.DECSET_XTERM_VT220_KEYBOARD_EMULATION:
                                    // These are highly specialized and not
                                    // relevant to modern/Windows systems. Do
                                    // nothing.
                                    handled = true;

                                    break;

                                // Disable readline mouse button-1, xterm
                                // (2001)
                                case CSI_DECSET.DECSET_XTERM_READLINE_MOUSE_BUTTON_1:

                                // Disable readline mouse button-2, xterm
                                // (2002)
                                case CSI_DECSET.DECSET_XTERM_READLINE_MOUSE_BUTTON_2:

                                // Disable readline mouse button-3, xterm
                                // (2003)
                                case CSI_DECSET.DECSET_XTERM_READLINE_MOUSE_BUTTON_3:
                                    // Not really seeing the utility in this.
                                    // See
                                    // https://github.com/termux/termux-app/issues/4302#issuecomment-2563385400
                                    // for an explanation of what these
                                    // actually do. Do nothing.
                                    handled = true;

                                    break;

                                // Reset bracketed paste mode, xterm (2004)
                                case CSI_DECSET.DECSET_XTERM_BRACKETED_PASTE_MODE:
                                    _terminalEngine.BracketedPasteMode = false;

                                    handled = true;

                                    break;

                                // Disable readline character-quoting, xterm
                                // (2005)
                                case CSI_DECSET.DECSET_XTERM_READLINE_CHARACTER_QUOTING:

                                // Disable readline newline pasting, xterm
                                // (2006)
                                case CSI_DECSET.DECSET_XTERM_READLINE_NEWLINE_PASTING:
                                    // Not really seeing the utility in this.
                                    // Presumably works with the same use case
                                    // as 2001-2003. Do nothing.
                                    handled = true;

                                    break;

                                // Disable theme change notification (2031)
                                case CSI_DECSET.DECSET_THEME_CHANGE:
                                    _reportPaletteUpdate = false;

                                    handled = true;

                                    break;

                                // Disable in-band window resize notification
                                // (2048)
                                case CSI_DECSET.DECSET_WINDOW_RESIZE:
                                    _terminalEngine.ReportResize = false;

                                    handled = true;

                                    break;

                                // Disable ConPTY win32-input-mode (9001)
                                case CSI_DECSET.DECSET_WIN32_INPUT_MODE:
                                    // Do nothing. This is a Windows
                                    // Terminal-ism and is safe to disregard.
                                    // See CSI.DECSET_WIN32_INPUT_MODE for more
                                    // on this.
                                    handled = true;

                                    break;
                            }
                        }

                        break;
                }

                // Sequences that aren't captured above that we'd like to
                // explicitly ignore since we don't support them
                if (
                  csiEscapeSequence.RawEscapeSequence is "[5m" // SGR slow blink
                  or "[6m" // SGR fast blink
                  or "[8m" // SGR conceal
                  or "[25m" // SGR blink off
                  or "[28m" // SGR reveal
                )
                {
                    handled = true;
                }

                // Fs escape sequence. These are not super common, but they are
                // very important.
            }
            else if (escapeSequence is FsEscapeSequence fsEscapeSequence)
            {
                // Full reset (c)
                if (fsEscapeSequence.RawEscapeSequence[0] == Fs.RIS)
                {
                    // Do our best to get things back to the way they were when
                    // we started
                    _screenBuffer.Clear();
                    _alternateScreenBuffer.Clear();
                    _tabStops.Clear();

                    Row = 1;
                    Column = 1;
                    _lastSelection.Row = -1;
                    _lastSelection.Column = -1;

                    _useAlternateScreenBuffer = false;
                    _lazySelectionMode = false;
                    _selectionMode = false;
                    _wrapPending = false;
                    _originMode = false;

                    _savedCursorState = null;
                    _savedCursorPosition = null;

                    _graphicRendition.InitializeFromPalette(_palette);
                    _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                    if (_terminalEngine.UseBackgroundColorErase)
                    {
                        _backgroundColorErase = _graphicRendition.BackgroundColor;
                    }

                    _terminalEngine.CursorVisible = true;
                    _terminalEngine.AutoRepeatKeys = true;
                    _terminalEngine.ApplicationCursorKeys = false;
                    _terminalEngine.BracketedPasteMode = false;
                    _autoWrapMode = true;

                    _windowTitleStackLength = 0;
                    _terminalEngine.WindowTitle = _terminalEngine.DefaultWindowTitle;

                    // Initialize screen buffers
                    Resize();

                    InitializeTabStops();

                    handled = true;
                }

                // Fp escape sequence. These are not super common, but they are
                // very important.
            }
            else if (escapeSequence is FpEscapeSequence fpEscapeSequence)
            {
                switch (fpEscapeSequence.RawEscapeSequence[0])
                {
                    // DEC save cursor (7)
                    case Fp.DECSC:
                        _savedCursorState = new(this);

                        handled = true;

                        break;

                    // DEC restore cursor (8)
                    case Fp.DECRC:
                        if (_autoWrapMode)
                        {
                            WrapPending = false;
                        }

                        if (_savedCursorState is not null)
                        {
                            ((CursorState)_savedCursorState).Restore(this);
                            _savedCursorState = null;
                        }
                        else
                        {
                            Row = 0;
                            Column = 0;
                            _originMode = false;
                            _graphicRendition.InitializeFromPalette(_palette);
                        }

                        handled = true;

                        break;
                }

                // Operating system command (OSC) escape sequence. These are
                // relatively rare.
            }
            else if (escapeSequence is OSCEscapeSequence oscEscapeSequence)
            {
                // Ps >= 3 are pretty specialized for xterm/X. We'll use 0, 1,
                // and 2 to set the window title and ignore everything else.
                if (oscEscapeSequence.Ps is >= 0 and <= 2)
                {
                    _terminalEngine.WindowTitle = oscEscapeSequence.Pt![0];

                    handled = true;
                }

                // Another less-specific Fe escape sequence, which can really
                // only mean HTS
            }
            else if (escapeSequence is FeEscapeSequence feEscapeSequence)
            {
                // Horizontal tab stop (H)
                if (feEscapeSequence.RawEscapeSequence[0] == Fe.HTS)
                {
                    if (!_tabStops.Contains(Column))
                    {
                        _tabStops.Add(Column);
                    }

                    handled = true;
                }
            }

            if (!handled)
            {
                _logger?.LogWarning("Unhandled {escapeSequenceType}: {escapeSequence}", escapeSequence.GetType(), escapeSequence.RawEscapeSequence);
            }
        }
    }
}
