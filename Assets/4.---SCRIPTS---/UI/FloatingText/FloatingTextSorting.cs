using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public static class FloatingTextSorting
{
    public const string SortingLayerName =
        "FloatingText";

    public const int SortingOrder =
        0;

    public static void Apply(
        Transform root,
        TMP_Text text)
    {
        if (root == null)
            return;

        /*
         * World-space / Screen-space Camera Canvas.
         */
        Canvas[] canvases =
            root.GetComponentsInChildren<
                Canvas>(
                true
            );

        foreach (Canvas canvas
                 in canvases)
        {
            if (canvas == null)
                continue;

            canvas.overrideSorting =
                true;

            canvas.sortingLayerName =
                SortingLayerName;

            canvas.sortingOrder =
                SortingOrder;
        }

        /*
         * TextMeshPro (3D/world text) använder
         * Renderer i stället för Canvas sorting.
         */
        if (text != null)
        {
            Renderer renderer =
                text.GetComponent<
                    Renderer>();

            if (renderer != null)
            {
                renderer.sortingLayerName =
                    SortingLayerName;

                renderer.sortingOrder =
                    SortingOrder;
            }
        }

        /*
         * Om prefaben råkar innehålla en SortingGroup
         * ska även den flyttas till samma topplager.
         */
        SortingGroup[] sortingGroups =
            root.GetComponentsInChildren<
                SortingGroup>(
                true
            );

        foreach (SortingGroup group
                 in sortingGroups)
        {
            if (group == null)
                continue;

            group.sortingLayerName =
                SortingLayerName;

            group.sortingOrder =
                SortingOrder;
        }
    }
}
