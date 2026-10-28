using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class PlayerMovement : MonoBehaviour

{
    private HumanoidVisualController visual;
    private HumanoidEquipment equipment;
    private SpriteRenderer spriteRenderer;

    [SerializeField] float skinWidth = 0.02f;

    private CharacterStats stats;
    private Vector3 lastMousePosition;
    private bool mouseMoved;
    private Vector2 lastMovement;

    private CharacterStateController stateController;
    private CharacterActionController actionController;

    private Rigidbody2D rb;
    private Vector2 movement;

    private RoadPath currentRoad;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    public bool IsOnRoad =>
        currentRoad != null;

    public RoadPath CurrentRoad =>
        currentRoad;

    public RoadProfile CurrentRoadProfile =>
        currentRoad != null
            ? currentRoad.Profile
            : null;

    public float RoadMovementSpeedMultiplier
    {
        get;
        private set;
    } = 1f;

    public event Action
    MovementInputStarted;

    public Vector2 MovementInput =>
        movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        visual = GetComponentInChildren<HumanoidVisualController>();
        equipment = GetComponent<HumanoidEquipment>();
        stats = GetComponent<CharacterStats>();
        stateController = GetComponent<CharacterStateController>();
        actionController = GetComponent<CharacterActionController>();
    }

    void Start()
    {
        if (visual != null)
        {
            visual.SetFacing(FacingDirection);
            visual.SetMoving(false);
        }
    }

    void Update()
    {
        if (IsTypingInInputField())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 currentMovement;
        currentMovement.x = Input.GetAxisRaw("Horizontal");
        currentMovement.y = Input.GetAxisRaw("Vertical");
        currentMovement = currentMovement.normalized;

        bool movementStarted = currentMovement !=
        Vector2.zero && movement == Vector2.zero;

        movement = currentMovement;

        if (movementStarted)
        {
            MovementInputStarted?.Invoke();
        }

        // Kolla om movement ändrades
        if (currentMovement != lastMovement)
        {
            if (stateController == null || stateController.CanRotate)
            {
                if (currentMovement != Vector2.zero)
                {
                    UpdateFacingDirectionFromMovement();
                }
            }

            lastMovement = currentMovement;
        }

        // --- Kolla mus ---
        if (stateController == null || stateController.CanRotate)
        {
            if (Input.mousePosition != lastMousePosition)
            {
                UpdateFacingDirectionFromMouse();
                lastMousePosition = Input.mousePosition;
            }
        }

        bool stateAllowsMovement =
            stateController == null ||
            stateController.CanMove;

        bool actionAllowsMovement =
            actionController == null ||
            !actionController.BlocksMovement;

        bool moving =
            stateAllowsMovement &&
            actionAllowsMovement &&
            movement != Vector2.zero;

        if (visual != null)
        {
            visual.SetFacing(FacingDirection);
            visual.SetMoving(moving);
        }

        if (equipment != null)
        {
            equipment.UpdateVisualDirection(FacingDirection);
        }
    }

    bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected =
            EventSystem.current.currentSelectedGameObject;

        if (selected == null)
            return false;

        return selected.GetComponent<TMP_InputField>() != null;
    }

    void UpdateFacingDirectionFromMouse()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 direction = (mouseWorld - transform.position).normalized;

        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            FacingDirection = direction.y > 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            FacingDirection = direction.x > 0 ? Vector2.right : Vector2.left;
        }
    }

    void UpdateFacingDirectionFromMovement()
    {
        if (Mathf.Abs(movement.y) > Mathf.Abs(movement.x))
        {
            FacingDirection = movement.y > 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            FacingDirection = movement.x > 0 ? Vector2.right : Vector2.left;
        }
    }



    // =========================================================
    // ROAD SURFACE STATE
    // =========================================================

    private void RefreshRoadState()
    {
        if (rb == null)
        {
            currentRoad =
                null;

            RoadMovementSpeedMultiplier =
                1f;

            return;
        }


        if (RoadPath.TryGetRoadAtWorldPoint(
                rb.position,
                out RoadPath road,
                out float movementSpeedMultiplier))
        {
            currentRoad =
                road;

            RoadMovementSpeedMultiplier =
                Mathf.Max(
                    0f,
                    movementSpeedMultiplier
                );

            return;
        }


        currentRoad =
            null;

        RoadMovementSpeedMultiplier =
            1f;
    }


    void FixedUpdate()
    {
        rb.linearVelocity =
    Vector2.zero;

        /*
         * Vägens redan genererade Surface-mesh används som
         * gameplay-yta.
         *
         * Inga extra road-colliders, triggers, layers eller
         * osynliga overlay-objekt behövs.
         */
        RefreshRoadState();

        if (stateController != null &&
            !stateController.CanMove)
        {
            return;
        }

        float actionMovementMultiplier =
            actionController != null
                ? actionController
                    .CurrentMovementMultiplier
                : 1f;

        if (actionMovementMultiplier <=
            0.0001f)
        {
            return;
        }

        if (movement ==
            Vector2.zero)
        {
            return;
        }

        float moveSpeedStat =
            stats.GetStat(
                StatType.MovementSpeed
            );

        float finalMovementSpeed =
            moveSpeedStat *
            actionMovementMultiplier *
            RoadMovementSpeedMultiplier;

        Vector2 desiredMove =
            movement *
            finalMovementSpeed *
            Time.fixedDeltaTime;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = LayerMask.GetMask("Blocking", "NPC");
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[1];

        int hitCount = rb.Cast(
            desiredMove.normalized,
            filter,
            hits,
            desiredMove.magnitude + skinWidth
        );


        if (hitCount == 0)
        {
            rb.MovePosition(rb.position + desiredMove * (1f - skinWidth));

        }
        else
        {
            Vector2 hitNormal = hits[0].normal;

            float dot = Vector2.Dot(desiredMove, hitNormal);

            if (dot < 0f)
            {
                // 1️⃣ Rör oss BORT från hindret → tillåt FULL rörelse
                rb.MovePosition(rb.position + desiredMove);
            }
            else
            {
                // 2️⃣ Rör oss IN i eller längs hindret → glid
                Vector2 slideMove = desiredMove - hitNormal * dot;

                if (slideMove.sqrMagnitude > 0.0001f)
                {
                    rb.MovePosition(rb.position + slideMove);
                }
            }
        }

    }

}




