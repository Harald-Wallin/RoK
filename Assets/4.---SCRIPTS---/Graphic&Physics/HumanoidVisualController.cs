using UnityEngine;
public class HumanoidVisualController : MonoBehaviour
{
    [Header("=== SKIN RENDERERS ===")]
    [SerializeField] private SpriteRenderer skinHead;
    [SerializeField] private SpriteRenderer skinTorso;
    [SerializeField] private SpriteRenderer skinArms;
    [SerializeField] private SpriteRenderer skinLegs;
    [SerializeField] private SpriteRenderer skinFeet;

    [Header("=== HAIR / BEARD ===")]
    [SerializeField] private SpriteRenderer hair;
    [SerializeField] private SpriteRenderer beard;

    [Header("ANIMATIONS")]
    private float animationTimer;
    private int animationFrame;
    [SerializeField] private float frameRate = 8f;

    private HumanoidAnimationState currentState = HumanoidAnimationState.Idle;
    private CharacterStateController characterState;
    private bool inCombat;

    private HumanoidEquipment equipment;
    public Vector2 CurrentFacing => currentFacing;

    [Header("Initial Facing")]
    [SerializeField] private Vector2 initialFacing = Vector2.down;

    public HumanoidAnimationSet headAnimations;
    public HumanoidAnimationSet torsoAnimations;
    public HumanoidAnimationSet armsAnimations;
    public HumanoidAnimationSet legsAnimations;
    public HumanoidAnimationSet feetAnimations;

    private Vector2 currentFacing = Vector2.down;
    private bool isMoving = false;

    private void Awake()
    {
        characterState =
            GetComponentInParent<CharacterStateController>(
                true
            );

        equipment =
            GetComponentInParent<HumanoidEquipment>(
                true
            );

        if (characterState == null)
        {
            /*
             * Om VisualRoot faktiskt tillhör en gameplay-karaktär
             * med CharacterStats är detta ett riktigt setup-fel.
             *
             * Ett fristående visual/preview-objekt får däremot
             * fungera utan CharacterStateController och använder
             * då vanliga non-combat-animationer.
             */
            CharacterStats character =
                GetComponentInParent<CharacterStats>(
                    true
                );

            if (character != null)
            {
                Debug.LogError(
                    $"HumanoidVisualController on '{name}' belongs to " +
                    $"character '{character.name}', but no " +
                    $"{nameof(CharacterStateController)} exists in its " +
                    $"parent hierarchy.",
                    this
                );
            }

            inCombat =
                false;

            return;
        }

        inCombat =
            characterState.InCombat;

        characterState.OnCombatStateChanged +=
            HandleCombatStateChanged;
    }

    private void Start()
    {
        ApplyInitialFacing();
    }

    void Update()
    {
        animationTimer += Time.deltaTime;

        if (animationTimer > 1f / frameRate)
        {
            animationTimer = 0f;
            animationFrame++;

            RefreshAnimation();
        }
    }

    public void SetFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        if (currentFacing == direction)
            return;

        currentFacing = direction;

        RefreshAnimation();
    }

    public void SetMoving(bool moving)
    {
        if (isMoving == moving)
            return;

        isMoving = moving;

        RefreshAnimation();
    }

    public void SetCombatState(
    bool state)
    {
        if (inCombat == state)
            return;

        inCombat = state;

        animationFrame = 0;

        RefreshAnimation();
    }

    private void UpdateSprites(Vector2 dir)
    {
        if (dir == Vector2.zero)
            dir = Vector2.down;

        currentFacing = dir;

        DirectionalSpriteSet headSet = GetSet(headAnimations);
        DirectionalSpriteSet torsoSet = GetSet(torsoAnimations);
        DirectionalSpriteSet armsSet = GetSet(armsAnimations);
        DirectionalSpriteSet legsSet = GetSet(legsAnimations);
        DirectionalSpriteSet feetSet = GetSet(feetAnimations);

        if (headSet != null)
            skinHead.sprite = headSet.GetSprite(dir, animationFrame);

        if (torsoSet != null)
            skinTorso.sprite = torsoSet.GetSprite(dir, animationFrame);

        if (armsSet != null)
            skinArms.sprite = armsSet.GetSprite(dir, animationFrame);

        if (legsSet != null)
            skinLegs.sprite = legsSet.GetSprite(dir, animationFrame);

        if (feetSet != null)
            skinFeet.sprite = feetSet.GetSprite(dir, animationFrame);
    }



    DirectionalSpriteSet GetSet(HumanoidAnimationSet set)
    {
        bool moving = currentState == HumanoidAnimationState.Walk;

        if (inCombat)
        {
            if (moving)
            {
                if (HasSprites(set.combatWalk))
                    return set.combatWalk;

                return set.walk;
            }

            if (HasSprites(set.combatIdle))
                return set.combatIdle;

            return set.idle;
        }

        return moving ? set.walk : set.idle;
    }

    private bool HasSprites(DirectionalSpriteSet set)
    {
        if (set == null)
            return false;

        return
            (set.down != null && set.down.Length > 0) ||
            (set.up != null && set.up.Length > 0) ||
            (set.left != null && set.left.Length > 0) ||
            (set.right != null && set.right.Length > 0);
    }

    public void SetAnimationState(HumanoidAnimationState state)
    {
        if (currentState == state)
            return;

        currentState = state;
        animationFrame = 0;
    }

    public void SetHairVisible(bool state)
    {
        if (hair != null)
            hair.enabled = state;
    }

    public void SetBeardVisible(bool state)
    {
        if (beard != null)
            beard.enabled = state;
    }

    public void SetTorsoVisible(bool state)
    {
        if (skinTorso != null)
            skinTorso.enabled = state;
    }

    public void SetArmsVisible(bool state)
    {
        if (skinArms != null)
            skinArms.enabled = state;
    }

    public void SetHeadVisible(bool state)
    {
        if (skinHead != null)
            skinHead.enabled = state;
    }

    public void SetLegsVisible(bool state)
    {
        if (skinLegs != null)
            skinLegs.enabled = state;
    }

    public void SetFeetVisible(bool state)
    {
        if (skinFeet != null)
            skinFeet.enabled = state;
    }

    private void HandleCombatStateChanged(
    bool combat)
    {
        SetCombatState(
            combat
        );
    }

    void ApplyInitialFacing()
    {
        SetFacing(initialFacing);
    }

    private void RefreshAnimation()
    {
        SetAnimationState(
            isMoving
                ? HumanoidAnimationState.Walk
                : HumanoidAnimationState.Idle
        );

        UpdateSprites(currentFacing);

        equipment?.UpdateVisualDirection(currentFacing);
    }

    private void OnDestroy()
    {
        if (characterState != null)
        {
            characterState.OnCombatStateChanged -= HandleCombatStateChanged;
        }
    }
}