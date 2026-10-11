using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private List<BookDefinition> equippedBooks = new List<BookDefinition>();

    public float MovementSpeedBonus { get; private set; }
    public float ThrowRangeBonus { get; private set; }
    public float PickupRangeBonusCells { get; private set; }
    public IReadOnlyList<BookDefinition> EquippedBooks { get { return equippedBooks; } }

    private void Awake()
    {
        RecalculateBonuses();
    }

    private void OnValidate()
    {
        RecalculateBonuses();
    }

    public bool EquipBook(BookDefinition book)
    {
        if (book == null || equippedBooks.Contains(book))
        {
            return false;
        }

        equippedBooks.Add(book);
        RecalculateBonuses();
        return true;
    }

    public bool UnequipBook(BookDefinition book)
    {
        if (book == null || !equippedBooks.Remove(book))
        {
            return false;
        }

        RecalculateBonuses();
        return true;
    }

    private void RecalculateBonuses()
    {
        if (equippedBooks == null)
        {
            equippedBooks = new List<BookDefinition>();
        }

        float movementBonus = 0f;
        float throwBonus = 0f;
        float pickupRangeBonus = 0f;
        for (int i = 0; i < equippedBooks.Count; i++)
        {
            BookDefinition book = equippedBooks[i];
            if (book == null)
            {
                continue;
            }

            movementBonus += book.MovementSpeedBonus;
            throwBonus += book.ThrowRangeBonus;
            pickupRangeBonus += book.PickupRangeBonusCells;
        }

        MovementSpeedBonus = movementBonus;
        ThrowRangeBonus = throwBonus;
        PickupRangeBonusCells = pickupRangeBonus;
    }
}
