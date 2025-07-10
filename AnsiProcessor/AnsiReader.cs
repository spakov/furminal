using Spakov.AnsiProcessor.Ansi;
using Spakov.AnsiProcessor.AnsiColors;
using Spakov.AnsiProcessor.Output.EscapeSequences;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fp;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fs;
using Spakov.AnsiProcessor.Output.EscapeSequences.NF;
using Spakov.AnsiProcessor.TermCap;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Spakov.AnsiProcessor {
  /// <summary>
  /// An ANSI reader.
  /// </summary>
  public class AnsiReader {
    /// <summary>
    /// The reader state.
    /// </summary>
    private enum State {
      Text,
      EscapeSequenceStart,
      EscapeSequence
    }

    /// <summary>
    /// The reader escape sequence state.
    /// </summary>
    internal enum EscapeSequenceState {
      Fe,
      Fp,
      Fs,
      nF
    }

    private readonly FileStream consoleOutStream;
    private readonly Decoder decoder;

    private readonly TerminalCapabilities terminalCapabilities;

    private Palette palette;

    private readonly byte[] buffer = new byte[1024];
    private readonly char[] charBuffer = new char[2048];

    private readonly StringBuilder escapeSequenceBuilder;
    private readonly StringBuilder textBuffer;

    private State state;
    private EscapeSequenceState escapeSequenceState;

    /// <summary>
    /// The <see cref="Palette"/> used for ANSI colors.
    /// </summary>
    public Palette Palette {
      get => palette;
      set => palette = value;
    }

    /// <summary>
    /// Callback for reading text.
    /// </summary>
    /// <param name="text">The text to read.</param>
    public delegate void TextCallback(string text);

    /// <summary>
    /// Callback for reading escape sequences.
    /// </summary>
    /// <param name="escapeSequence">An <see cref="EscapeSequence"/>.</param>
    public delegate void EscapeSequenceCallback(EscapeSequence escapeSequence);

    /// <summary>
    /// Callback for reading control characters.
    /// </summary>
    /// <param name="controlCharacter">The control character.</param>
    public delegate void ControlCharacterCallback(char controlCharacter);

    /// <summary>
    /// Raised when text is ready to be read.
    /// </summary>
    /// <remarks>Invoke <see cref="Resume"/> to continue processing after
    /// handling the text.</remarks>
    public event TextCallback? OnText;

    /// <summary>
    /// Raised when an escape squence is ready to be read.
    /// </summary>
    /// <remarks>Invoke <see cref="Resume"/> to continue processing after
    /// handling the escape sequence.</remarks>
    public event EscapeSequenceCallback? OnEscapeSequence;

    /// <summary>
    /// Raised when a control character is ready to be read.
    /// </summary>
    /// <remarks>Invoke <see cref="Resume"/> to continue processing after
    /// handling the control character.</remarks>
    public event ControlCharacterCallback? OnControlCharacter;

    private TaskCompletionSource<bool> resumeSignal = new();

    /// <summary>
    /// Initializes an <see cref="AnsiReader"/>.
    /// </summary>
    /// <param name="consoleOutStream">The output stream from which to process
    /// ANSI.</param>
    /// <param name="terminalCapabilities">A <see cref="TerminalCapabilities"/>
    /// configuration.</param>
    /// <param name="palette">A <see cref="AnsiColors.Palette"/> to use for
    /// ANSI colors.</param>
    public AnsiReader(FileStream consoleOutStream, TerminalCapabilities terminalCapabilities, Palette? palette) {
      this.consoleOutStream = consoleOutStream;
      decoder = Encoding.UTF8.GetDecoder();

      this.terminalCapabilities = terminalCapabilities;
      this.palette = palette is null ? new() : palette;

      state = State.Text;
      escapeSequenceBuilder = new();
      textBuffer = new();

      Task.Run(HandleAnsi);
    }

    /// <summary>
    /// Resume processing.
    /// </summary>
    public void Resume() => resumeSignal.TrySetResult(true);

    /// <summary>
    /// Handles ANSI input from <see cref="consoleOutStream"/>.
    /// </summary>
    private async Task HandleAnsi() {
      while (true) {
        int bytesRead = await consoleOutStream.ReadAsync(buffer);
        if (bytesRead == 0) return;

        int charsDecoded = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0, false);
        for (int i = 0; i < charsDecoded; i++) {
          HandleCharacter(charBuffer[i]);
        }

        FlushText();

        await WaitUntilResume();
      }
    }

    /// <summary>
    /// Handles a character of ANSI input.
    /// </summary>
    /// <param name="character">The character to handle.</param>
    private void HandleCharacter(char character) {
      switch (state) {
        case State.Text:
          HandleText(character);

          break;
        case State.EscapeSequenceStart:
          HandleEscapeSequenceStart(character);

          break;
        case State.EscapeSequence:
          HandleEscapeSequence(character);

          break;
      }
    }

    /// <summary>
    /// Handles text in the ANSI input.
    /// </summary>
    /// <param name="character"><inheritdoc cref="HandleCharacter"
    /// path="/param[@name='character']"/></param>
    private void HandleText(char character) {
      if (terminalCapabilities.ControlCharacters is not null) {
        if (terminalCapabilities.ControlCharacters.C0.Contains(character)) {
          if (character == C0.ESC) {
            FlushText();
            escapeSequenceBuilder.Clear();
            state = State.EscapeSequenceStart;

            return;
          } else if (char.IsControl(character)) {
            FlushText();
            OnControlCharacter?.Invoke(character);

            return;
          }
        }

        if (terminalCapabilities.ControlCharacters.AllowBareC1) {
          if (terminalCapabilities.ControlCharacters.C1.Contains(character)) {
            if (char.IsControl(character)) {
              FlushText();
              OnControlCharacter?.Invoke(character);

              return;
            }
          }
        }
      }

      // Discard ignored control characters (but keep text)
      if (!char.IsControl(character)) {
        textBuffer.Append(character);
      }
    }

    /// <summary>
    /// Handles the start of an escape sequence in the ANSI input.
    /// </summary>
    /// <param name="character"><inheritdoc cref="HandleCharacter"
    /// path="/param[@name='character']"/></param>
    private void HandleEscapeSequenceStart(char character) {
      EscapeSequenceState? escapeSequenceState = EscapeSequence.DetermineEscapeSequenceType(terminalCapabilities, character);

      if (escapeSequenceState is not null) {
        this.escapeSequenceState = (EscapeSequenceState) escapeSequenceState;

        escapeSequenceBuilder.Append(character);
        state = State.EscapeSequence;
      } else {
        state = State.Text;
      }
    }

    /// <summary>
    /// Handles an escape sequence in the ANSI input.
    /// </summary>
    /// <param name="character"><inheritdoc cref="HandleCharacter"
    /// path="/param[@name='character']"/></param>
    private void HandleEscapeSequence(char character) {
      bool escapeSequenceComplete;

      // Err on the side of caution
      escapeSequenceComplete = true;

      escapeSequenceBuilder.Append(character);

      switch (escapeSequenceState) {
        case EscapeSequenceState.Fe:
          escapeSequenceComplete = FeEscapeSequence.IsFeEscapeSequenceComplete(escapeSequenceBuilder, character);

          break;
        case EscapeSequenceState.Fp:
          escapeSequenceComplete = FpEscapeSequence.IsFpEscapeSequenceComplete();

          break;
        case EscapeSequenceState.Fs:
          escapeSequenceComplete = FsEscapeSequence.IsFsEscapeSequenceComplete();

          break;
        case EscapeSequenceState.nF:
          escapeSequenceComplete = NFEscapeSequence.IsNFEscapeSequenceComplete(character);

          break;
      }

      if (escapeSequenceComplete) {
        OnEscapeSequence?.Invoke(
          EscapeSequence.InitializeEscapeSequence(
            terminalCapabilities,
            palette,
            escapeSequenceBuilder.ToString(),
            escapeSequenceState
          )
        );

        state = State.Text;
      }
    }

    /// <summary>
    /// Flushes text.
    /// </summary>
    private void FlushText() {
      if (textBuffer.Length > 0) {
        OnText?.Invoke(textBuffer.ToString());
        textBuffer.Clear();
      }
    }

    /// <summary>
    /// Waits until <see cref="Resume"/> is invoked.
    /// </summary>
    /// <returns>The next <see cref="Task"/>.</returns>
    private async Task WaitUntilResume() {
      await resumeSignal.Task;
      resumeSignal = new();
    }
  }
}
