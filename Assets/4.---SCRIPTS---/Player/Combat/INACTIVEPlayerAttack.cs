/*using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    public Transform attackPoint;
    [SerializeField] public float attackRange = 0.5f;
    [SerializeField] public float attackDistance = 0.5f;
    [SerializeField] public Transform attackOrigin;
    [SerializeField] private float arcAngle = 90f; // grader

    [SerializeField] private BleedEffect testBleed;

    private float cooldownTimer = 0f;

    public float CooldownNormalized =>
        1f - (cooldownTimer / (1f / stats.GetStat(StatType.AttackSpeed)));

    public bool CanAttack => cooldownTimer <= 0f;

    //Använder vi ens denna?
    public LayerMask enemyLayers;
 

    private PlayerMovement playerMovement;
    private CharacterStats stats;
    private PlayerReputationManager reputationManager;


    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        stats = GetComponent<CharacterStats>();
        reputationManager = GetComponent<PlayerReputationManager>();
    }

    void Update()
    {
        UpdateAttackPointPosition();

        float attackSpeed = stats.GetStat(StatType.AttackSpeed);

        if (attackSpeed <= 0f)
            return;

        float cooldown = 1f / attackSpeed;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0) && cooldownTimer <= 0f)
        {
            //UI-blocking check
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Attack();
            cooldownTimer = cooldown; // 🔥 ENDA source of truth
        }
    }
    void UpdateAttackPointPosition()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 rawDirection = mouseWorld - attackOrigin.position;

        if (rawDirection.sqrMagnitude < 0.0001f)
            return;

        Vector2 direction = rawDirection.normalized;

        attackPoint.position = attackOrigin.position + (Vector3)(direction * attackDistance);
    }

    void Attack()
    {
        Collider2D[] hitColliders =
        Physics2D.OverlapCircleAll(
        attackOrigin.position,
        attackDistance,
        LayerMask.GetMask("Hitbox")
        );

        foreach (Collider2D col in hitColliders)
        {
            CombatHitbox hitbox =
            col.GetComponent<CombatHitbox>();

            if (hitbox == null)
                continue;

            CharacterStats target = hitbox.Owner;

            if (!IsValidTarget(target))
                continue;

            if (!CombatTargeting.CanAttack(stats, target))
                continue;

            if (!PassesConeCheck(col.transform.position))
                continue;

            ApplyDamage(target);
        }
    }

    bool IsValidTarget(CharacterStats target)
    {
        if (target == null)
            return false;

        if (target == stats) // oss själva
            return false;

        return true;
    }

    bool PassesConeCheck(Vector3 targetPosition)
    {
        Vector2 directionToTarget =
            (targetPosition - attackOrigin.position).normalized;

        Vector2 forward =
            (attackPoint.position - attackOrigin.position).normalized;

        float dot = Vector2.Dot(forward, directionToTarget);

        float angleThreshold =
            Mathf.Cos((arcAngle * 0.5f) * Mathf.Deg2Rad);

        return dot >= angleThreshold;
    }

    void ApplyDamage(CharacterStats target)
    {
        CombatResolver.DealDamage(
            stats,
            target,
            stats.GetAttackDamage()
        );
    }

    void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) return;

        Gizmos.color = Color.red;

        Vector3 forward = (attackPoint.position - attackOrigin.position).normalized;

        Quaternion leftRotation = Quaternion.Euler(0, 0, -arcAngle * 0.5f);
        Quaternion rightRotation = Quaternion.Euler(0, 0, arcAngle * 0.5f);

        Vector3 leftDir = leftRotation * forward;
        Vector3 rightDir = rightRotation * forward;

        Gizmos.DrawLine(attackOrigin.position,
            attackOrigin.position + leftDir * attackDistance);

        Gizmos.DrawLine(attackOrigin.position,
            attackOrigin.position + rightDir * attackDistance);

        Gizmos.DrawWireSphere(attackOrigin.position, attackDistance);
    }

}*/
