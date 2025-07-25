namespace Spakov.AnsiProcessor.Output
{
    /// <summary>
    /// A list of underline styles as an extension to <see
    /// cref="Ansi.EscapeSequences.SGR.UNDERLINE"/>.
    /// </summary>
    public enum UnderlineStyle
    {
        None,
        Single = 1,
        Double,
        Undercurl
    }
}
