public static class CharacterRoleExtensions
{
    public static string GetDisplayName(
        this CharacterRole role)
    {
        return role switch
        {
            CharacterRole.None =>
                string.Empty,

            CharacterRole.TrophyVendor =>
                "Trophy Vendor",

            CharacterRole.FoodVendor =>
                "Food Vendor",

            CharacterRole.BreadVendor =>
                "Bread Vendor",

            CharacterRole.Guard =>
                "Guard",

            CharacterRole.Captain =>
                "Captain",

            CharacterRole.Thane =>
                "Thane",

            CharacterRole.Innkeeper =>
                "Innkeeper",

            CharacterRole.Blacksmith =>
                "Blacksmith",

            CharacterRole.Trainer =>
                "Trainer",

            CharacterRole.StableMaster =>
                "Stable Master",

            CharacterRole.TributeTaker =>
                "Tribute Taker",

            CharacterRole.Healer =>
                "Healer",

            _ =>
                role.ToString()
        };
    }
}
