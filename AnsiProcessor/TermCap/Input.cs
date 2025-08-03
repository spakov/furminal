namespace Spakov.AnsiProcessor.TermCap
{
    /// <summary>
    /// Allows defining input behavior.
    /// </summary>
    public class Input
    {
        /// <summary>
        /// Whether the Backspace key sends a <c>DEL</c> (<see
        /// langword="true"/>) or a <c>BS</c> (<see langword="false"/>).
        /// </summary>
        public bool BackspaceIsDel { get; set; } = true;
    }
}
