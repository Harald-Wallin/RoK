using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Dialogue Set"
)]
public sealed class FavourDialogueSet :
    ScriptableObject
{
    [Header("Dialogue")]

    [TextArea(2, 6)]
    [SerializeField]
    private string unavailable;

    [TextArea(2, 6)]
    [SerializeField]
    private string offer;

    [TextArea(2, 6)]
    [SerializeField]
    private string active;

    [TextArea(2, 6)]
    [SerializeField]
    private string readyToTurnIn;

    [TextArea(2, 6)]
    [SerializeField]
    private string completed;

    [TextArea(2, 6)]
    [SerializeField]
    private string failed;

    [TextArea(2, 6)]
    [SerializeField]
    private string cooldown;

    public string Unavailable =>
        unavailable;

    public string Offer =>
        offer;

    public string Active =>
        active;

    public string ReadyToTurnIn =>
        readyToTurnIn;

    public string Completed =>
        completed;

    public string Failed =>
        failed;

    public string Cooldown =>
        cooldown;

    public string GetDialogue(
        FavourState state)
    {
        return state switch
        {
            FavourState.Unavailable =>
                unavailable,

            FavourState.Available =>
                offer,

            FavourState.Active =>
                active,

            FavourState.ReadyToTurnIn =>
                readyToTurnIn,

            FavourState.Completed =>
                completed,

            FavourState.Failed =>
                failed,

            FavourState.Cooldown =>
                cooldown,

            _ => string.Empty
        };
    }
}
