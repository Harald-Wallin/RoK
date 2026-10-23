
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class YSorter : MonoBehaviour
{
    [Header("Sorting")]

    [SerializeField]
    private int sortingOrderOffset = 0;

    [SerializeField]
    private float yOffset = 0f;

    [Header("Optimization")]

    [SerializeField]
    [Tooltip(
        "Static objects calculate sorting once. " +
        "Dynamic objects update continuously."
    )]
    private bool staticSorting = false;

    private SortingGroup sortingGroup;

    private int lastSortingOrder =
        int.MinValue;

    public bool IsStaticSorting =>
        staticSorting;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        sortingGroup =
            GetComponent<SortingGroup>();

        RefreshSortingOrder();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying ||
            !staticSorting)
        {
            return;
        }

        // Calculate once when entering play mode.
        RefreshSortingOrder();

        // A static object needs no LateUpdate calls.
        // The SortingGroup remains fully operational.
        enabled = false;
    }

    private void LateUpdate()
    {
        RefreshSortingOrder();
    }

    // =====================================================
    // CONFIGURATION
    // =====================================================

    public void ConfigureStaticSorting()
    {
        staticSorting = true;

        RefreshSortingOrder();

        if (Application.isPlaying)
        {
            enabled = false;
        }
    }

    // =====================================================
    // SORTING
    // =====================================================

    public void RefreshSortingOrder()
    {
        if (sortingGroup == null)
        {
            sortingGroup =
                GetComponent<SortingGroup>();
        }

        if (sortingGroup == null)
            return;

        int newSortingOrder =
            WorldSorting.CalculateOrder(
                transform.position.y,
                yOffset,
                sortingOrderOffset
            );

        if (newSortingOrder ==
                lastSortingOrder &&
            sortingGroup.sortingOrder ==
                newSortingOrder)
        {
            return;
        }

        sortingGroup.sortingOrder =
            newSortingOrder;

        lastSortingOrder =
            newSortingOrder;
    }

    // =====================================================
    // EDITOR
    // =====================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying ||
            !staticSorting)
        {
            return;
        }

        // Refresh a selected static object in Edit Mode.
        // The component stays active in the editor,
        // but disables itself during Play Mode.
        RefreshSortingOrder();
    }

#endif

    // =====================================================
    // LEGACY API
    // =====================================================

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
