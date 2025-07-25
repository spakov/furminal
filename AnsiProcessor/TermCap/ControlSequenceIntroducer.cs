namespace Spakov.AnsiProcessor.TermCap
{
    /// <summary>
    /// Allows defining supported CSI escape sequences to handle via callbacks.
    /// </summary>
    public class ControlSequenceIntroducer
    {
        /// <summary>
        /// Select graphic rendition (SGR) escape sequences configuration.
        /// </summary>
        public SelectGraphicRendition? SGR { get; set; } = new();
    }
}
