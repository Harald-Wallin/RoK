using UnityEngine;

public sealed class FavourRewardChoiceOptionRuntime
{
    internal FavourRewardChoiceOptionRuntime(
        FavourRewardChoiceRuntime group,
        FavourRewardChoiceOption data,
        int optionIndex)
    {
        Group = group;
        Data = data;
        OptionIndex = optionIndex;
    }

    public FavourRewardChoiceRuntime Group
    {
        get;
    }

    public FavourRewardChoiceOption Data
    {
        get;
    }

    public int OptionIndex
    {
        get;
    }

    public FavourRewardChoiceType Type =>
        Data != null
            ? Data.Type
            : FavourRewardChoiceType.Item;

    public ItemData Item =>
        Data?.Item;

    public AbilityData Ability =>
        Data?.Ability;

    public int ItemAmount =>
        Data != null &&
        Data.Type ==
        FavourRewardChoiceType.Item
            ? Data.ItemAmount
            : 0;

    public string DisplayName =>
        Data != null
            ? Data.DisplayName
            : "Missing Reward";

    public string Description
    {
        get
        {
            if (Data == null)
                return string.Empty;

            switch (Data.Type)
            {
                case FavourRewardChoiceType.Item:
                    return Data.Item != null
                        ? Data.Item.description
                        : string.Empty;

                case FavourRewardChoiceType.Ability:
                    return Data.Ability != null
                        ? Data.Ability.description
                        : string.Empty;

                default:
                    return string.Empty;
            }
        }
    }

    public Sprite Icon
    {
        get
        {
            if (Data == null)
                return null;

            switch (Data.Type)
            {
                case FavourRewardChoiceType.Item:
                    return Data.Item != null
                        ? Data.Item.icon
                        : null;

                case FavourRewardChoiceType.Ability:
                    return Data.Ability != null
                        ? Data.Ability.icon
                        : null;

                default:
                    return null;
            }
        }
    }

    public bool IsValid =>
        Data != null &&
        Data.IsValid;

    public bool IsSelected =>
        Group?.Favour != null &&
        Group.Favour.IsRewardChoiceSelected(
            Group.GroupIndex,
            OptionIndex
        );

    public bool CanSelect
    {
        get
        {
            if (!IsValid ||
                Group == null ||
                !Group.CanChangeSelection)
            {
                return false;
            }

            if (IsSelected)
                return true;

            /*
             * I en Choose 1-grupp får ett nytt val ersätta det
             * befintliga valet.
             */
            if (Group.RequiredSelections == 1)
                return true;

            return Group.CanSelectMore;
        }
    }

    public bool CanDeselect =>
        IsValid &&
        IsSelected &&
        Group != null &&
        Group.CanChangeSelection;

    public bool Select()
    {
        if (IsSelected)
            return true;

        if (!CanSelect)
            return false;

        return Group.Favour
            .SetRewardChoiceSelected(
                Group.GroupIndex,
                OptionIndex,
                true
            );
    }

    public bool Deselect()
    {
        if (!IsSelected)
            return true;

        if (!CanDeselect)
            return false;

        return Group.Favour
            .SetRewardChoiceSelected(
                Group.GroupIndex,
                OptionIndex,
                false
            );
    }

    public bool Toggle()
    {
        return IsSelected
            ? Deselect()
            : Select();
    }
}