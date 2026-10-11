using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Book", fileName = "New Book")]
public sealed class BookDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "New Book";
    [SerializeField] private float movementSpeedBonus;
    [SerializeField, Min(0f)] private float throwRangeBonus;

    public string DisplayName { get { return displayName; } }
    public float MovementSpeedBonus { get { return movementSpeedBonus; } }
    public float ThrowRangeBonus { get { return throwRangeBonus; } }
}
