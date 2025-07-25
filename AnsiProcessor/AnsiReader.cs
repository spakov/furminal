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

namespace Spakov.AnsiProcessor
{
    /// <summary>
    /// An ANSI reader.
    /// </summary>
    /// <remarks>
    /// <para>Processes output from <see cref="_consoleOutStream"/> and converts
    /// to <see langword="string"/>s, <see cref="EscapeSequence"/>s, and <see
    /// langword="char"/>s. These are passed via <see cref="OnText"/>, <see
    /// cref="OnEscapeSequence"/>, and <see cref="OnControlCharacter"/>,
    /// respectively, to the consumer.</para>
    /// <para>The consumer must invoke <see cref="Resume"/> after handling each
    /// callback to resume processing ANSI.</para>
    /// </remarks>
    public class AnsiReader
    {
        /// <summary>
        /// The reader state.
        /// </summary>
        private enum State
        {
            Text,
            EscapeSequenceStart,
            EscapeSequence
        }

        /// <summary>
        /// The reader escape sequence state.
        /// </summary>
        internal enum EscapeSequenceState
        {
            Fe,
            Fp,
            Fs,
            nF
        }

        private readonly FileStream _consoleOutStream;
        private readonly Decoder _decoder;

        private readonly TerminalCapabilities _terminalCapabilities;

        private Palette _palette;

        private readonly byte[] _buffer = new byte[1024];
        private readonly char[] _charBuffer = new char[2048];

        private readonly StringBuilder _escapeSequenceBuilder;
        private readonly StringBuilder _textBuffer;

        private State _state;
        private EscapeSequenceState _escapeSequenceState;

        /// <summary>
        /// The <see cref="Palette"/> used for ANSI colors.
        /// </summary>
        public Palette Palette
        {
            get => _palette;
            set => _palette = value;
        }

        /// <summary>
        /// Callback for reading text.
        /// </summary>
        /// <param name="text">The text to read.</param>
        public delegate void TextCallback(string text);

        /// <summary>
        /// Callback for reading escape sequences.
        /// </summary>
        /// <param name="escapeSequence">An <see
        /// cref="EscapeSequence"/>.</param>
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

        private TaskCompletionSource<bool> _resumeSignal = new();

        /// <summary>
        /// Initializes an <see cref="AnsiReader"/>.
        /// </summary>
        /// <param name="consoleOutStream">The output stream from which to
        /// process ANSI.</param>
        /// <param name="terminalCapabilities">A <see
        /// cref="TerminalCapabilities"/> configuration.</param>
        /// <param name="palette">A <see cref="AnsiColors.Palette"/> to use for
        /// ANSI colors.</param>
        public AnsiReader(FileStream consoleOutStream, TerminalCapabilities terminalCapabilities, Palette? palette)
        {
            _consoleOutStream = consoleOutStream;
            _decoder = Encoding.UTF8.GetDecoder();
            _terminalCapabilities = terminalCapabilities;
            _palette = palette is null ? new() : palette;
            _state = State.Text;
            _escapeSequenceBuilder = new();
            _textBuffer = new();

            Task.Run(HandleAnsi);
        }

        /// <summary>
        /// Resume processing.
        /// </summary>
        public void Resume() => _resumeSignal.TrySetResult(true);

        /// <summary>
        /// Handles ANSI input from <see cref="_consoleOutStream"/>.
        /// </summary>
        private async Task HandleAnsi()
        {
            while (true)
            {
                int bytesRead = await _consoleOutStream.ReadAsync(_buffer);
                if (bytesRead == 0)
                {
                    return;
                }

                int charsDecoded = _decoder.GetChars(_buffer, 0, bytesRead, _charBuffer, 0, false);
                for (int i = 0; i < charsDecoded; i++)
                {
                    HandleCharacter(_charBuffer[i]);
                }

                FlushText();

                await WaitUntilResume();
            }
        }

        /// <summary>
        /// Handles a character of ANSI input.
        /// </summary>
        /// <param name="character">The character to handle.</param>
        private void HandleCharacter(char character)
        {
            switch (_state)
            {
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
        private void HandleText(char character)
        {
            if (_terminalCapabilities.ControlCharacters is not null)
            {
                if (_terminalCapabilities.ControlCharacters.C0.Contains(character))
                {
                    if (character == C0.ESC)
                    {
                        FlushText();
                        _escapeSequenceBuilder.Clear();
                        _state = State.EscapeSequenceStart;

                        return;
                    }
                    else if (char.IsControl(character))
                    {
                        FlushText();
                        OnControlCharacter?.Invoke(character);

                        return;
                    }
                }

                if (_terminalCapabilities.ControlCharacters.AllowBareC1)
                {
                    if (_terminalCapabilities.ControlCharacters.C1.Contains(character))
                    {
                        if (char.IsControl(character))
                        {
                            FlushText();
                            OnControlCharacter?.Invoke(character);

                            return;
                        }
                    }
                }
            }

            // Discard ignored control characters (but keep text)
            if (!char.IsControl(character))
            {
                _textBuffer.Append(character);
            }
        }

        /// <summary>
        /// Handles the start of an escape sequence in the ANSI input.
        /// </summary>
        /// <param name="character"><inheritdoc cref="HandleCharacter"
        /// path="/param[@name='character']"/></param>
        private void HandleEscapeSequenceStart(char character)
        {
            EscapeSequenceState? escapeSequenceState = EscapeSequence.DetermineEscapeSequenceType(_terminalCapabilities, character);

            if (escapeSequenceState is not null)
            {
                this._escapeSequenceState = (EscapeSequenceState)escapeSequenceState;

                _escapeSequenceBuilder.Append(character);
                _state = State.EscapeSequence;
            }
            else
            {
                _state = State.Text;
            }
        }

        /// <summary>
        /// Handles an escape sequence in the ANSI input.
        /// </summary>
        /// <param name="character"><inheritdoc cref="HandleCharacter"
        /// path="/param[@name='character']"/></param>
        private void HandleEscapeSequence(char character)
        {
            bool escapeSequenceComplete;

            // Err on the side of caution
            escapeSequenceComplete = true;

            _escapeSequenceBuilder.Append(character);

            switch (_escapeSequenceState)
            {
                case EscapeSequenceState.Fe:
                    escapeSequenceComplete = FeEscapeSequence.IsFeEscapeSequenceComplete(_escapeSequenceBuilder, character);

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

            if (escapeSequenceComplete)
            {
                OnEscapeSequence?.Invoke(
                    EscapeSequence.InitializeEscapeSequence(
                        _terminalCapabilities,
                        _palette,
                        _escapeSequenceBuilder.ToString(),
                        _escapeSequenceState
                    )
                );

                _state = State.Text;
            }
        }

        /// <summary>
        /// Flushes text.
        /// </summary>
        private void FlushText()
        {
            if (_textBuffer.Length > 0)
            {
                OnText?.Invoke(_textBuffer.ToString());
                _textBuffer.Clear();
            }
        }

        /// <summary>
        /// Waits until <see cref="Resume"/> is invoked.
        /// </summary>
        /// <returns>The next <see cref="Task"/>.</returns>
        private async Task WaitUntilResume()
        {
            await _resumeSignal.Task;
            _resumeSignal = new();
        }
    }
}
