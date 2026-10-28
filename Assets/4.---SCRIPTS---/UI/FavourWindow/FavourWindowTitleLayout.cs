using UnityEngine;

[ExecuteAlways]
public sealed class FavourWindowTitleLayout : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RectTransform windowPanel;

    [SerializeField]
    private RectTransform favourNameText;

    [SerializeField]
    private RectTransform giverNameText;

    [Header("Offsets From Top-Left Corner")]

    [SerializeField]
    private Vector2 favourNameOffset =
        new(-8f, 2f);

    [SerializeField]
    private Vector2 giverNameOffset =
        new(16f, -32f);

    private readonly Vector3[] corners =
        new Vector3[4];

    private void LateUpdate()
    {
        RefreshLayout();
    }

    private void OnValidate()
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (windowPanel == null)
            return;

        windowPanel.GetWorldCorners(
            corners
        );

        /*
         * GetWorldCorners:
         * 0 = bottom-left
         * 1 = top-left
         * 2 = top-right
         * 3 = bottom-right
         */
        Vector3 topLeftWorld =
            corners[1];

        PositionAtTopLeft(
            favourNameText,
            topLeftWorld,
            favourNameOffset
        );

        PositionAtTopLeft(
            giverNameText,
            topLeftWorld,
            giverNameOffset
        );
    }

    private static void PositionAtTopLeft(
        RectTransform target,
        Vector3 topLeftWorld,
        Vector2 offset)
    {
        if (target == null ||
            target.parent == null)
        {
            return;
        }

        RectTransform parent =
            target.parent as RectTransform;

        if (parent == null)
            return;

        Vector3 localPosition =
            parent.InverseTransformPoint(
                topLeftWorld
            );

        target.localPosition =
            localPosition +
            new Vector3(
                offset.x,
                offset.y,
                0f
            );
    }
}
