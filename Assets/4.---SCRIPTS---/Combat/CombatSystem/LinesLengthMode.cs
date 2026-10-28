public enum LineLengthMode
{
    /// <summary>
    /// Linjen slutar vid muspekaren, men begränsas av
    /// abilityns maximala range.
    /// </summary>
    ToCursor,

    /// <summary>
    /// Muspekaren bestämmer endast riktningen.
    /// Linjen använder alltid abilityns fulla range.
    /// </summary>
    FullRange
}
