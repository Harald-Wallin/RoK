
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(GridLayoutGroup))]
public sealed class ResponsiveGridLayout :
    MonoBehaviour
{
    [SerializeField, Min(1)]
    private int columns = 2;

    [SerializeField, Min(1f)]
    private float cellHeight = 170f;

    private GridLayoutGroup grid;
    private RectTransform viewport;
    private float previousWidth = -1f;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();

        // Content is normally a child of Viewport.
        viewport = transform.parent as RectTransform;

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void LateUpdate()
    {
        if (viewport == null)
            return;

        if (!Mathf.Approximately(
                viewport.rect.width,
                previousWidth))
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (viewport == null)
            viewport = transform.parent as RectTransform;

        if (viewport == null)
            return;

        float width = viewport.rect.width;

        if (width <= 0f)
            return;

        grid.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;

        grid.constraintCount = columns;

        float padding =
            grid.padding.left +
            grid.padding.right;

        float totalSpacing =
            grid.spacing.x * (columns - 1);

        float availableWidth =
            width - padding - totalSpacing;

        float cellWidth =
            Mathf.Max(
                1f,
                availableWidth / columns
            );

        grid.cellSize = new Vector2(
            cellWidth,
            cellHeight
        );

        previousWidth = width;
    }
}

