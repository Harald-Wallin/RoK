using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrialRewardContainerPoint : MonoBehaviour
{
    public TrialZone Zone => GetComponentInParent<TrialZone>(true);
}
