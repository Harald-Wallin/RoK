using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldSpaceCanvasSetup :
    MonoBehaviour
{
    [SerializeField]
    private Canvas targetCanvas;


    private void Awake()
    {
        ResolveCanvas();

        if (targetCanvas == null)
        {
            Debug.LogError(
                $"{nameof(WorldSpaceCanvasSetup)} på '{name}' " +
                "kunde inte hitta någon Canvas på objektet eller dess children.",
                this
            );

            return;
        }

        targetCanvas.renderMode =
            RenderMode.WorldSpace;

        if (Camera.main != null)
        {
            targetCanvas.worldCamera =
                Camera.main;
        }
    }


    private void ResolveCanvas()
    {
        if (targetCanvas != null)
            return;

        targetCanvas =
            GetComponent<Canvas>();

        if (targetCanvas != null)
            return;

        targetCanvas =
            GetComponentInChildren<Canvas>(
                true
            );
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetCanvas != null)
            return;

        targetCanvas =
            GetComponent<Canvas>();

        if (targetCanvas == null)
        {
            targetCanvas =
                GetComponentInChildren<Canvas>(
                    true
                );
        }
    }
#endif
}


