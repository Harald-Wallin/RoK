/*using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyAttack : MonoBehaviour
{
    private float lastAttackTime;
    private Enemy enemy;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public void TryAttack(CharacterStats target)
    {
        //OBS Attackspeed 1.0 = 1s, 2.0 = 0.5s, 0.5 = 2s, etc.
        float attackSpeed = enemy.GetStat(StatType.AttackSpeed);

        if (attackSpeed <= 0f)
            return;

        float cooldown = 1f / attackSpeed;

        if (Time.time < lastAttackTime + cooldown)
            return;

        lastAttackTime = Time.time;

        CombatResolver.DealDamage(
        enemy,
        target,
        enemy.GetAttackDamage()
        );
    }

}*/


