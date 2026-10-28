using UnityEngine;

[DisallowMultipleComponent]
public sealed class BuildingModule :
    MonoBehaviour
{
    [Header("Position Snap")]

    [SerializeField]
    private bool enablePositionSnap =
        true;

    [SerializeField]
    [Min(1)]
    private int pixelsPerUnit =
        16;

    [SerializeField]
    [Min(1)]
    private int snapPixels =
        1;

    [SerializeField]
    private bool snapX =
        true;

    [SerializeField]
    private bool snapY =
        true;

    public bool EnablePositionSnap =>
        enablePositionSnap;

    public int PixelsPerUnit =>
        Mathf.Max(
            1,
            pixelsPerUnit
        );

    public int SnapPixels =>
        Mathf.Max(
            1,
            snapPixels
        );

    public bool SnapX =>
        snapX;

    public bool SnapY =>
        snapY;

    public float SnapStep =>
        SnapPixels /
        (float)PixelsPerUnit;

#if UNITY_EDITOR
    private void OnValidate()
    {
        pixelsPerUnit =
            Mathf.Max(
                1,
                pixelsPerUnit
            );

        snapPixels =
            Mathf.Max(
                1,
                snapPixels
            );
    }
#endif
}