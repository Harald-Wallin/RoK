
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class YSorter : MonoBehaviour
{
    [SerializeField]
    private int sortingOrderOffset = 0;

    [SerializeField]
    private float yOffset = 0f;

    private SortingGroup sortingGroup;

    private int lastSortingOrder =
        int.MinValue;

    private void Awake()
    {
        sortingGroup =
            GetComponent<SortingGroup>();

        RefreshSortingOrder();
    }

    private void LateUpdate()
    {
        RefreshSortingOrder();
    }

    private void RefreshSortingOrder()
    {
        int newSortingOrder =
            WorldSorting.CalculateOrder(
                transform.position.y,
                yOffset,
                sortingOrderOffset
            );

        if (newSortingOrder ==
            lastSortingOrder)
        {
            return;
        }

        sortingGroup.sortingOrder =
            newSortingOrder;

        lastSortingOrder =
            newSortingOrder;
    }

    /// <summary>
    /// Legacy-compatible API.
    ///
    /// Existing systems, including WorldScatter,
    /// can continue calling this method.
    /// </summary>
    public static int CalculateSortingOrder(
        float worldY,
        float yOffset = 0f,
        int sortingOrderOffset = 0)
    {
        return WorldSorting.CalculateOrder(
            worldY,
            yOffset,
            sortingOrderOffset
        );
    }
}
