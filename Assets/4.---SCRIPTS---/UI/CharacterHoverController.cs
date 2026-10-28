using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Global hover-resolution för Characters och vanliga
/// InteractionTargets i världen.
///
/// Characters har prioritet när både en Character och ett
/// world interactable ligger under muspekaren.
///
/// Hoverpresentationen behålls en kort period efter att musen
/// lämnat objektet.
/// </summary>
public sealed class CharacterHoverController :
    MonoBehaviour
{
    [Header("Physics")]

    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private LayerMask characterLayers =
        ~0;

    [Header("UI Blocking")]

    [SerializeField]
    private bool ignoreWorldHoverWhenPointerIsOverUI =
        true;

    [Header("Hover Retention")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur länge nameplate och tooltip ligger kvar efter " +
        "att musen lämnat world-targetet."
    )]
    private float hoverRetentionDuration =
        2f;

    private CharacterStats
        hoveredCharacter;

    private InteractionTarget
        hoveredInteractionTarget;

    private NameplateUI
        hoveredNameplate;

    private float
        hoverRetentionTimer;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }
    }

    private void Update()
    {
        if (worldCamera == null)
            return;

        bool pointerBlockedByUI =
            ignoreWorldHoverWhenPointerIsOverUI &&
            EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject();

        if (pointerBlockedByUI)
        {
            UpdateNoHover();
            return;
        }

        Vector2 pointer =
            GetPointerWorldPosition();

        /*
         * Characters har högsta prioritet.
         */
        CharacterStats character =
            ResolveHoveredCharacter(
                pointer
            );

        if (character != null)
        {
            UpdateCharacterHover(
                character
            );

            return;
        }

        InteractionTarget interactionTarget =
            ResolveHoveredInteractionTarget(
                pointer
            );

        if (interactionTarget != null)
        {
            UpdateInteractionHover(
                interactionTarget
            );

            return;
        }

        UpdateNoHover();
    }

    private Vector2 GetPointerWorldPosition()
    {
        Vector3 mouseWorld =
            worldCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        return new Vector2(
            mouseWorld.x,
            mouseWorld.y
        );
    }

    private CharacterStats
        ResolveHoveredCharacter(
            Vector2 point)
    {
        Collider2D collider =
            Physics2D.OverlapPoint(
                point,
                characterLayers
            );

        if (collider == null)
            return null;

        return
            TargetUtility.GetCharacterStats(
                collider
            );
    }

    private InteractionTarget
        ResolveHoveredInteractionTarget(
            Vector2 point)
    {
        Collider2D[] colliders =
            Physics2D.OverlapPointAll(
                point
            );

        if (colliders == null ||
            colliders.Length == 0)
        {
            return null;
        }

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider2D collider =
                colliders[i];

            if (collider == null)
                continue;

            InteractionTarget target =
                collider.GetComponent<
                    InteractionTarget>();

            if (target == null)
                continue;

            /*
             * NPC InteractionTargets behöver inte visas som
             * world objects. Character-tooltipen äger NPC:n.
             */
            CharacterStats character =
                TargetUtility.GetCharacterStats(
                    collider
                );

            if (character != null)
                continue;

            return target;
        }

        return null;
    }

    private void UpdateCharacterHover(
        CharacterStats character)
    {
        hoverRetentionTimer =
            hoverRetentionDuration;

        if (character ==
                hoveredCharacter &&
            hoveredInteractionTarget ==
                null)
        {
            return;
        }

        ClearCurrentPresentation();

        hoveredInteractionTarget =
            null;

        hoveredCharacter =
            character;

        NameplateUI.TryGet(
            character,
            out hoveredNameplate
        );

        hoveredNameplate?.SetHovered(
            true
        );

        if (ItemTooltip.Instance != null)
        {
            ItemTooltip.Instance
                .ShowFixedBottomRight(
                    new CharacterTooltipProvider(
                        character
                    ),
                    PlayerReference.Player
                );
        }
    }

    private void UpdateInteractionHover(
        InteractionTarget target)
    {
        hoverRetentionTimer =
            hoverRetentionDuration;

        if (target ==
                hoveredInteractionTarget &&
            hoveredCharacter ==
                null)
        {
            return;
        }

        ClearCurrentPresentation();

        hoveredCharacter =
            null;

        hoveredNameplate =
            null;

        hoveredInteractionTarget =
            target;

        if (ItemTooltip.Instance != null)
        {
            ItemTooltip.Instance
                .ShowFixedBottomRight(
                    new WorldObjectTooltipProvider(
                        target
                    ),
                    PlayerReference.Player
                );
        }
    }

    private void UpdateNoHover()
    {
        if (hoveredCharacter == null &&
            hoveredInteractionTarget == null)
        {
            return;
        }

        hoverRetentionTimer -=
            Time.unscaledDeltaTime;

        if (hoverRetentionTimer <= 0f)
        {
            ClearHover();
        }
    }

    private void ClearHover()
    {
        ClearCurrentPresentation();

        hoveredCharacter =
            null;

        hoveredInteractionTarget =
            null;

        hoveredNameplate =
            null;

        hoverRetentionTimer =
            0f;
    }

    private void ClearCurrentPresentation()
    {
        hoveredNameplate?.SetHovered(
            false
        );

        ItemTooltip.Instance?.Hide();
    }

    private void OnDisable()
    {
        ClearHover();
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        hoverRetentionDuration =
            Mathf.Max(
                0f,
                hoverRetentionDuration
            );
    }

#endif
}