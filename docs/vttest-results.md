# vttest Results

## Test conditions
- Windows 11 Version 24H2 (OS Build 26100.4351)
- WSL 2
- Fedora release 42 (Adams)
- vttest "VT100 test program, version 2.7 (20241204)"
- No defaults changed, except as noted in each test
- 80 columns, 24 rows
- Comparing to xterm "XTerm(397)" on same platform

## Test Results

### Pass
Items annotated with * are very likely due to being handled by ConPTY. Items
annotated with † pass with the broken mode workaround (see below).

- **3** Test of character sets ("Character set 2 (DEC Alternate character ROM
  special graphics)" contains the same as "Character set 0 (DEC Special
  graphics and line drawing)")
- **5.2** Keyboard Tests -&gt; Auto Repeat
- **5.3(.0)** Keyboard Tests -&gt; Keyboard Layout (Ctrl-H = BS, BS = DEL,
  Ctrl-J = LF)
- **5.9** Keyboard Tests -&gt; Control Keys (Ctrl-Space = NUL, Ctrl-\ = FS,
  Ctrl-` = RS, Ctrl-- = US)
- **6.2** Terminal Reports/Responses -&gt; Set/Reset Mode - LineFeed / Newline*
- **6.3** Terminal Reports/Responses -&gt; Device Status Report (DSR)*
- **6.4** Terminal Reports/Responses -&gt; Primary Device Attributes (DA)*
- **6.5** Terminal Reports/Responses -&gt; Secondary Device Attributes (DA)*
- **6.6** Terminal Reports/Responses -&gt; Tertiary Device Attributes (DA)*
- **6.7** Terminal Reports/Responses -&gt; Request Terminal Parameters
  (DECREQTPARM)*
- **10.1** Test of reset and self-test -&gt; Reset to Initial State (RIS)
- **10.2** Test of reset and self-test -&gt; Invoke Terminal Test (DECTST)*
- **10.3** Test of reset and self-test -&gt; Soft Terminal Reset (DECSTR)
- **11.1.1.1.1** VT220 Device Status Reports -&gt; Test Keyboard Status*
- **11.1.1.1.2** VT220 Device Status Reports -&gt; Test Operating Status*
- **11.1.1.1.3** VT220 Device Status Reports -&gt; Test Printer Status*
- **11.1.2.2** VT220 Screen-Display Tests -&gt; Test Visible/Invisible Cursor
  (DECTCEM)
- **11.1.2.3** VT220 Screen-Display Tests -&gt; Test Erase Char (ECH)
- **11.1.2.4** VT220 Screen-Display Tests -&gt; Test Protected-Areas (DECSCA)*
- **11.1.6** VT200 Tests -&gt; Test Soft Terminal Reset (DECSTR)
- **11.2.2.1** VT320 Cursor-Movement Tests -&gt; Test Pan down (SU)
- **11.2.2.2** VT320 Cursor-Movement Tests -&gt; Test Pan up (SD)
- **11.4.6.2** VT520 Screen-Display Tests -&gt; Test Set Cursor Style
  (DECSCUSR)
- **11.4.6.3** VT520 Screen-Display Tests -&gt; Test Alternate Text Color
  (DECATC)*
- **11.5.1** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test
  Character-Position-Absolute (HPA)
- **11.5.2** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test Cursor-Back-Tab
  (CBT)
- **11.5.3** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test
  Cursor-Character-Absolute (CHA)
- **11.5.4** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test
  Cursor-Horizontal-Index (CHT)
- **11.5.5** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test
  Horizontal-Position-Relative (HPR)
- **11.5.6** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test
  Line-Position-Absolute (VPA)
- **11.5.7** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test Next-Line (CNL)
- **11.5.8** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test Previous-Line (CPL)
- **11.5.9** ISO-6429 (ECMA-48) Cursor-Movement -&gt; Test
  Vertical-Position-Relative (VPR)
- **11.6.3** ISO 6429 colors -&gt; Test SGR-0 color reset
- **11.6.4** ISO 6429 colors -&gt; Test BCE-style clear line/display (ED, EL)
- **11.6.5** ISO 6429 colors -&gt; Test BCE-style clear line/display (ECH,
  Indexing)
- **11.6.7.2** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Repeat (REP)
- **11.6.7.3** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Scroll-Down
  (SD)
- **11.6.7.6** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Scroll-Up (SU)
- **11.7.2** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Repeat (REP)
- **11.7.3** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Scroll-Down (SD)
- **11.7.3** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Scroll-Up (SU)
- **11.8.2.5** XTERM miscellaneous reports -&gt; Title-Stack Position†
- **11.8.3** XTERM special features -&gt; Set window title
- **11.8.5.4** XTERM mouse features -&gt; Normal Mouse Tracking† (normal and
  SGR)
- **11.8.5.6** XTERM mouse features -&gt; Mouse Any-Event Tracking† (XFree86
  xterm) (normal and SGR)
- **11.8.5.7** XTERM mouse features -&gt; Mouse Button-Event Tracking† (XFree86
  xterm) (normal and SGR)
- **11.8.7.5** XTERM Alternate-Screen features -&gt; Better alternate screen
  (XFree86 xterm mode 1049)

### Partial Pass
Items annotated with * are very likely due to being handled by ConPTY.

- **1** Test of cursor movements (132-column mode not implemented)
- **2** Test of screen features (132-column mode not implemented)
- **5.4** Keyboard Tests -&gt; Cursor Keys (&lt;ANSI / Cursor key mode
  RESET&gt;, &lt;ANSI / Cursor key mode SET&gt; pass, &lt;VT52 Mode&gt; not
  implemented)
- **7** Test of VT52 mode (partially handled by ConPTY, not implemented)
- **8** Test of VT102 features (Insert/Delete Char/Line) (double-width mode not
  implemented, 132-column mode not implemented)
- **11.1.3** VT220 Tests -&gt; Test 8-bit controls (S7C1T/S8C1T) (8-bit control
  support not implemented, 7-bit control support pass)
- **11.2.3.1** Page Format Tests -&gt; Test set columns per page (DECSCPP)
  (132-column mode not implemented)*
- **11.2.3.2** Page Format Tests -&gt; Test set columns per page (DECSCPP)
  (non-24-row modes not implemented)*
- **11.3.2.7** VT420 Cursor-Movement Tests -&gt; Test Back Index (DECBI) (with
  and without 2 "Enable DECOM (origin mode)", 4 "Top/Bottom margins are reset",
  and 6 "Do not color test-regions (xterm)", left/right not implemented)*
- **11.3.2.8** VT420 Cursor-Movement Tests -&gt; Test Forward Index (DECFI)
  (with and without 2, 4, and 6, left/right not implemented)*
- **11.3.2.9** VT420 Cursor-Movement Tests -&gt; Test cursor movement within
  margins (with and without 2, 4, and 6, left/right not implemented)
- **11.3.2.10** VT420 Cursor-Movement Tests -&gt; Test other movement
  (CR/HT/LF/FF) within margins (with and without 2, 4, and 6, left/right not
  implemented)
- **11.3.3.9** VT420 Editing Sequence Tests -&gt; Test insert/delete column
  (DECIC, DECDC) (with and without 1 "Enable DECOM (origin mode)", 3
  "Top/Bottom margins are reset", and 5 "Do not show color test-regions
  (xterm)", left/right not implemented)*
- **11.3.3.10** VT420 Editing Sequence Tests -&gt; Test vertical scrolling
  (IND, RI) (with and without 1, 3, and 5, left/right not implemented)*
- **11.3.3.11** VT420 Editing Sequence Tests -&gt; Test insert/delete line (IL,
  DL) (with and without 1, 3, and 5, left/right not implemented)
- **11.3.3.12** VT420 Editing Sequence Tests -&gt; Test insert/delete char
  (ICH, DCH) (with and without 1, 3, and 5, left/right not implemented)
- **11.3.3.13** VT420 Editing Sequence Tests -&gt; Test ASCII formatting (BS,
  CR, TAB) (with and without 1, 3, and 5, left/right not implemented)
- **11.3.6.7** VT420 Rectangular Area Tests (should not work) -&gt; Test
  Change-Attributes in Rectangular Area (DECCARA) (with and without 1 "Enable
  DECOM (origin mode)", 3 "Top/Bottom margins are reset", 5 "Do not color
  test-regions (xterm)", and 6 "Do not use line-drawing characters", left/right
  not implemented)*
- **11.3.6.8** VT420 Rectangular Area Tests (should not work) -&gt; Test Copy
  Rectangular area (DECCRA) (with and without 1, 3, 5, and 6, left/right not
  implemented)*
- **11.3.6.9** VT420 Rectangular Area Tests (should not work) -&gt; Test Erase
  Rectangular area (DECERA) (with and without 1, 3, 5, and 6, left/right not
  implemented)*
- **11.3.6.10** VT420 Rectangular Area Tests (should not work) -&gt; Test Fill
  Rectangular area (DECFRA) (with and without 1, 3, 5, and 6, left/right not
  implemented)*
- **11.3.6.11** VT420 Rectangular Area Tests (should not work) -&gt; Test
  Reverse-Attributes in Rectangular Area (DECRARA) (with and without 1, 3, 5,
  and 6, left/right not implemented)*
- **11.3.6.12** VT420 Rectangular Area Tests (should not work) -&gt; Test
  Selective-Erase Rectangular area (DECSERA) (with and without 1, 3, 5, and 6,
  left/right not implemented)*
- **11.4.1.2.7** VT420 Cursor-Movement Tests -&gt; Test Back Index (DECBI)
  (with and without 2 "Enable DECOM (origin mode)", 4 "Top/Bottom margins are
  reset", and 6 "Do not color test-regions (xterm)", left/right not
  implemented)*
- **11.4.1.2.8** VT420 Cursor-Movement Tests -&gt; Test Forward Index (DECFI)
  (with and without 2, 3, 4, and 6, left/right not implemented)*
- **11.4.1.2.9** VT420 Cursor-Movement Tests -&gt; Test cursor movement within
  margins (with and without 2, 3, 4, and 6, left/right not implemented)
- **11.4.1.2.10** VT420 Cursor-Movement Tests -&gt; Test other movement
  (CR/HT/LF/FF) within margins (with and without 2, 3, 4, and 6, left/right not
  implemented)
- **11.4.1.3.9** VT420 Editing Sequence Tests -&gt; Test insert/delete column
  (DECIC, DECDC) (with and without 1 "Enable DECOM (origin mode)", 3
  "Top/Bottom margins are reset", and 5 "Do not color test-regions (xterm)",
  left/right not implemented)*
- **11.4.1.3.10** VT420 Editing Sequence Tests -&gt; Test vertical scrolling
  (IND, RI) (with and without 1, 3, and 5, left/right not implemented)*
- **11.4.1.3.11** VT420 Editing Sequence Tests -&gt; Test insert/delete line
  (IL, DL) (with and without 1, 3, and 5, left/right not implemented)
- **11.4.1.3.12** VT420 Editing Sequence Tests -&gt; Test insert/delete char
  (ICH, DCH) (with and without 1, 3, and 5, left/right not implemented)
- **11.4.1.3.13** VT420 Editing Sequence Tests -&gt; Test ASCII formatting (BS,
  CR, TAB) (with and without 1, 3, and 5, left/right not implemented)
- **11.4.2.7** VT520 Cursor-Movement -&gt; Test Character-Position-Absolute
  (HPA) (with and without 2 "Enable DECOM (origin mode)", 4 "Top/Bottom margins
  are reset", and 6 "Do not color test-regions (xterm)", left/right not
  implemented)
- **11.4.2.8** VT520 Cursor-Movement -&gt; Test Cursor-Back-Tab (CBT) (with and
  without 2, 4, and 6, left/right not implemented)
- **11.4.2.9** VT520 Cursor-Movement -&gt; Test Cursor-Character-Absolute (CHA)
  (with and without 2, 4, and 6, left/right not implemented)
- **11.4.2.10** VT520 Cursor-Movement -&gt; Test Cursor-Horizontal-Index (CHT)
  (with and without 2, 4, and 6, left/right not implemented)
- **11.4.2.11** VT520 Cursor-Movement -&gt; Test Horizontal-Position-Relative
  (HPR) (with and without 2, 4, and 6, left/right not implemented)
- **11.4.2.12** VT520 Cursor-Movement -&gt; Test Line-Position-Absolute (VPA)
  (with and without 2, 4, and 6, left/right not implemented)
- **11.4.2.13** VT520 Cursor-Movement -&gt; Test Next-Line (CNL) (with and
  without 2, 4, and 6, left/right not implemented)
- **11.4.2.14** VT520 Cursor-Movement -&gt; Test Previous-Line (CPL) (with and
  without 2, 4, and 6, left/right not implemented)
- **11.6.6.1** Test VT102-style features with BCE -&gt; Test of cursor
  movements (132-column mode not implemented)
- **11.6.6.2** Test VT102-style features with BCE -&gt; Test of screen features
  (132-column mode not implemented)
- **11.6.6.3** Test VT102-style features with BCE -&gt; Test of Insert/Delete
  Char/Line (132-column mode not implemented)
- **11.6.9** ISO 6429 colors -&gt; Test screen features with ISO 6429 SGR 22-27
  codes (conceal not implemented)
- **11.8.2.3** XTERM miscellaneous reports -&gt; Request Mode (DECRQM)/Report
  Mode (DECRPM)*
- **11.8.7.3** XTERM Alternate-Screen features -&gt; Switch to/from alternate
  screen (xterm) (with and without 1 and 2, broken cursor save/restore is a
  ConPTY-ism)
- **11.8.7.4** XTERM Alternate-Screen features -&gt; Improved alternate screen
  (XFree86 xterm mode 1047) (with and without 1 and 2, broken cursor
  save/restore is a ConPTY-ism)
- **11.8.9** XTERM special features -&gt; Window report-operations (dtterm)
  (only "Report size of window in chars (18)" implemented)

### Fail
- **4** Test of double-sized characters (not implemented)
- **5.1** Keyboard Tests -&gt; LED Lights (not implemented)
- **5.5** Keyboard Tests -&gt; Numeric Keypad (hangs the terminal)
- **5.8** Keyboard Tests -&gt; AnswerBack (not implemented)
- **6.1** Terminal Reports/Responses -&gt; &lt;ENQ&gt; (AnswerBack Message)
  (not implemented)
- **11.1.1.1.4** VT220 Device Status Reports -&gt; Test UDK Status (not
  implemented)
- **11.1.2.1** VT220 Screen-Display Tests -&gt; Test Send/Receive mode (SRM)
  (not implemented)
- **11.2.6.2** VT320 Screen-Display Tests -&gt; Test Status line
  (DECSASD/DECSSDT) (not implemented)
- **11.3.8.2** VT420 Screen-Display Tests -&gt; Test Select Number of Lines per
  Screen (DECSNLS) (not implemented)
- **11.4.6.1** VT520 Screen-Display Tests -&gt; Test No Clear on Column Change
  (DECNCSM) (not implemented)
- **11.6.7.1** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Protected-Area
  Tests (SPA not implemented)
- **11.6.7.3** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Scroll-Left
  (SL),
  **11.6.7.4** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Scroll-Right
  (SR) (this is a ConPTY-ism)
- **11.7.1.2** Protected-Area Tests -&gt; Test Protected-Areas (SPA) (not
  implemented)
- **11.7.4** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test Scroll-Left
  (SL), **11.7.5** Miscellaneous ISO-6429 (ECMA-48) Tests -&gt; Test
  Scroll-Right (SR) (this is a ConPTY-ism)
- **11.8.2.2** XTERM miscellaneous reports -&gt; Report version (XTVERSION)
  (not implemented)
- **11.8.5.5** XTERM mouse features -&gt; Mouse Highlight Tracking (not
  implemented)
- **11.8.5.8** XTERM mouse features -&gt; DEC Locator Events (DECterm) (not
  implemented)
- **11.8.6** XTERM special features -&gt; Tektronix 4014 features (not
  implemented)
- **11.8.8** XTERM special features -&gt; Window modify-operations (dtterm)
  (not implemented)

## † Broken mode
Something breaks down somewhere between the terminal emulator and vttest
running under the conditions described above, but I'm not sure what it is,
exactly. This occurs only in certain cases that are difficult to ascertain.
During mouse tests, for example, the broken mode seems to be required to get
mouse messages interpreted properly by vttest. In other cases, this does not
seem to be required. This is almost certainly a bug *somewhere*, though I'm not
sure where, nor am I sure how to easily determine which component is causing
the issue. I'm leaning toward something about the way ConPTY and WSL interact
with each other, though I have no evidence to support this. I have looked at
the vttest source code and see nothing wrong about it.

This code, which has been removed going forward, allows vttest to interpret
these sequences correctly:

```cs
/// <summary>
/// Sends <paramref name="escapeSequence"/> to the console input stream.
/// </summary>
/// <remarks>Regarding <paramref name="brokenMode"/>, I have <em>absolutely
/// no idea</em> what is going on. I'm assuming this must be a
/// ConPTY-ism.</remarks>
/// <param name="escapeSequence">The escape sequence (as 7-bit ASCII, with
/// no leading ESC) to send to the console input stream.</param>
/// <param name="brokenMode">Certain sequences seem to require a "broken"
/// sequence to be sent; this enables that functionality.</param>
public void SendEscapeSequence(byte[] escapeSequence, bool brokenMode = false) {
  byte esc = (byte) Ansi.C0.ESC;
  byte[] brokenEsc = [esc, (byte) Ansi.C0.NUL];
  byte[] toSend;

  if (!brokenMode) {
    toSend = new byte[1 + escapeSequence.Length];
    toSend[0] = esc;
    System.Buffer.BlockCopy(escapeSequence, 0, toSend, 1, escapeSequence.Length);
  } else {
    toSend = new byte[brokenEsc.Length + escapeSequence.Length];
    System.Buffer.BlockCopy(brokenEsc, 0, toSend, 0, brokenEsc.Length);
    System.Buffer.BlockCopy(escapeSequence, 0, toSend, brokenEsc.Length, escapeSequence.Length);
  }

  consoleInStream.Write(toSend, 0, toSend.Length);
  consoleInStream.Flush();
}
```
