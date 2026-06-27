using UnityEngine;

public enum AttackPattern
{
    SingleRandom,
    TwoRandom,
    AllThree
}

[CreateAssetMenu(fileName = "NewPattern", menuName = "SlimeAscent/AttackPattern")]
public class BossAttackPattern : ScriptableObject
{
    public AttackPattern type;
    [Range(0f, 1f)]
    public float weight = 1f;
}