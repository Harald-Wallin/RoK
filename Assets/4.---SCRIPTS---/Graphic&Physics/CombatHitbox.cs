using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class CombatHitbox :
    MonoBehaviour
{
    [SerializeField]
    private CharacterStats owner;

    public CharacterStats Owner =>
        owner;

    public Collider2D Collider
    {
        get;
        private set;
    }

    private void Awake()
    {
        Collider =
            GetComponent<Collider2D>();

        if (owner == null)
        {
            owner =
                GetComponentInParent<
                    CharacterStats>();
        }
    }
}
