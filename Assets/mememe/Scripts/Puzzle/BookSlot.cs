using UnityEngine;

namespace Puzzle
{
    public class BookSlot : MonoBehaviour
    {
        public int slotIndex;

        public Book OccupiedBy { get; private set; }
        public bool IsOccupied => OccupiedBy != null;

        public void Occupy(Book book)
        {
            OccupiedBy = book;
        }

        public void Vacate()
        {
            OccupiedBy = null;
        }
    }
}
