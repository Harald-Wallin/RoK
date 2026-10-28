public readonly struct InteractionPresentation
{
    public InteractionCategory Category
    {
        get;
    }

    public string Text
    {
        get;
    }

    public InteractionPresentation(
        InteractionCategory category,
        string text)
    {
        Category =
            category;

        Text =
            text ?? string.Empty;
    }
}