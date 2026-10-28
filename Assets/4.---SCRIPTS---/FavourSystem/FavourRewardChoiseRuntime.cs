using System.Collections.Generic;

public sealed class FavourRewardChoiceRuntime
{
    private readonly List<
        FavourRewardChoiceOptionRuntime>
        options =
            new();

    internal FavourRewardChoiceRuntime(
        FavourRuntime favour,
        FavourRewardChoiceGroup data,
        int groupIndex)
    {
        Favour = favour;
        Data = data;
        GroupIndex = groupIndex;

        BuildOptions();
    }

    public FavourRuntime Favour
    {
        get;
    }

    public FavourRewardChoiceGroup Data
    {
        get;
    }

    public int GroupIndex
    {
        get;
    }

    public string DisplayName =>
        Data != null
            ? Data.DisplayName
            : "Choose a Reward";

    public int RequiredSelections =>
        Data != null
            ? Data.ChoicesAllowed
            : 0;

    public int SelectedCount =>
        Favour != null
            ? Favour.GetSelectedRewardCount(
                GroupIndex
            )
            : 0;

    public bool IsComplete =>
        RequiredSelections > 0 &&
        SelectedCount ==
        RequiredSelections;

    public bool CanChangeSelection =>
        Favour != null &&
        Favour.CanChangeRewardSelections;

    public bool CanSelectMore =>
        CanChangeSelection &&
        SelectedCount <
        RequiredSelections;

    public IReadOnlyList<
        FavourRewardChoiceOptionRuntime>
        Options =>
            options;

    private void BuildOptions()
    {
        options.Clear();

        if (Data?.Options == null)
            return;

        for (int optionIndex = 0;
             optionIndex < Data.Options.Count;
             optionIndex++)
        {
            FavourRewardChoiceOption option =
                Data.Options[optionIndex];

            if (option == null)
                continue;

            options.Add(
                new FavourRewardChoiceOptionRuntime(
                    this,
                    option,
                    optionIndex
                )
            );
        }
    }

    public void ClearSelection()
    {
        if (!CanChangeSelection)
            return;

        foreach (
            FavourRewardChoiceOptionRuntime option
            in options)
        {
            if (option.IsSelected)
            {
                option.Deselect();
            }
        }
    }
}
