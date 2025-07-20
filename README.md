## Architecture
There are several major components of w6t:
- **Terminal**, the terminal emulator control
  - **AnsiProcessor**, responsible for translating ANSI into events (and vice versa)
  - **ConPTY**, an interface with the Windows pseudoconsole API
  - **WideCharacter**, a Unicode character width calculation library
    - **utf8proc**, a Windows build of [utf8proc](https://github.com/JuliaStrings/utf8proc)
