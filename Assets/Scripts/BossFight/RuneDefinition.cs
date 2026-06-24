using UnityEngine;

[CreateAssetMenu(fileName = "NewRune", menuName = "SlimeAscent/RuneDefinition")]
public class RuneDefinition : ScriptableObject
{
    public string runeName = "Runa Sin Nombre";
    public Sprite displaySprite;
    public Vector2[] templatePoints;
}