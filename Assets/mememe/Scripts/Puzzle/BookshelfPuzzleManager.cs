using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Puzzle
{
    public class BookshelfPuzzleManager : MonoBehaviour
    {
        public static BookshelfPuzzleManager Instance { get; private set; }

        [Header("Scene References")]
        public Book[] books;
        public BookSlot[] slots;
        public FeedbackText feedbackText;

        [Header("Approach Trigger")]
        public BoxCollider approachTrigger;

        [Header("Snap Settings")]
        public float snapRadius = 0.12f;

        static readonly float[] HeightPool = { 0.7f, 0.7f, 0.8f, 0.8f, 0.9f, 0.9f, 1.0f, 1.0f, 1.1f, 1.1f, 1.2f, 1.2f };

        static readonly BookColor[] ColorPool =
        {
            BookColor.Red,    BookColor.Red,
            BookColor.Orange, BookColor.Orange,
            BookColor.Yellow, BookColor.Yellow,
            BookColor.Green,  BookColor.Green,
            BookColor.Blue,   BookColor.Blue,
            BookColor.Indigo,
            BookColor.Violet
        };

        enum Stage { WaitingForApproach, Stage1_AnyOrder, Stage2_SortHeight, Stage3_SortColor, Complete }
        Stage currentStage = Stage.WaitingForApproach;
        bool stageTransitioning = false;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].slotIndex = i;

            foreach (var book in books)
            {
                BookSlot nearest = FindNearestSlotUnlimited(book.transform.position);
                if (nearest != null)
                {
                    book.SnapToSlot(nearest);
                    nearest.Occupy(book);
                }
            }
            Debug.Log("[Puzzle] Initialized — 12 books on shelf, waiting for player to approach");
        }

        void OnTriggerEnter(Collider other)
        {
            if (currentStage != Stage.WaitingForApproach) return;
            if (other.GetComponent<CharacterController>() == null && !other.CompareTag("Player"))
                return;
            Debug.Log("[Puzzle] Stage 1 — player approached shelf, dropping 4 books");
            currentStage = Stage.Stage1_AnyOrder;
            DropRandomBooks(4);
        }

        void DropRandomBooks(int count)
        {
            var candidates = new List<int>();
            for (int i = 1; i < slots.Length - 1; i++)
                if (slots[i].IsOccupied) candidates.Add(i);

            Shuffle(candidates);
            int drops = Mathf.Min(count, candidates.Count);
            for (int i = 0; i < drops; i++)
            {
                var book = slots[candidates[i]].OccupiedBy;
                slots[candidates[i]].Vacate();
                book.DropToFloor(new Vector3(-2f, 0f, 0f));
            }
        }

        public void OnBookReleased(Book book)
        {
            if (stageTransitioning) return;

            BookSlot target = FindNearestEmptySlot(book.transform.position);
            if (target != null)
            {
                book.SnapToSlot(target);
                target.Occupy(book);
                int filled = 0;
                foreach (var s in slots) if (s.IsOccupied) filled++;
                Debug.Log($"[Puzzle] Book {book.bookIndex} → slot {target.slotIndex} ({filled}/12 filled)");
                CheckSolution();
            }
            else
            {
                Debug.Log($"[Puzzle] Book {book.bookIndex} released — no slot nearby, dropping");
                book.DropToFloor();
            }
        }

        public void OnBookRemoved()
        {
            // Pass
        }

        BookSlot FindNearestEmptySlot(Vector3 pos)
        {
            BookSlot best = null;
            float bestDist = snapRadius;
            foreach (var slot in slots)
            {
                if (slot.IsOccupied) continue;
                float d = Vector3.Distance(pos, slot.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = slot;
                }
            }
            return best;
        }

        BookSlot FindNearestSlotUnlimited(Vector3 pos)
        {
            BookSlot best = null;
            float bestDist = float.MaxValue;
            foreach (var slot in slots)
            {
                if (slot.IsOccupied) continue;
                float d = Vector3.Distance(pos, slot.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = slot;
                }
            }
            return best;
        }

        void CheckSolution()
        {
            if (stageTransitioning) return;

            foreach (var slot in slots)
                if (!slot.IsOccupied) return;

            if (currentStage == Stage.Stage1_AnyOrder)
            {
                StartCoroutine(Stage1Complete());
            }
            else if (currentStage == Stage.Stage2_SortHeight)
            {
                bool sorted = IsHeightSorted();
                Debug.Log($"[Puzzle] Stage 2 height check: {(sorted ? "SOLVED" : "not in order yet")}");
                if (sorted) StartCoroutine(Stage2Complete());
            }
            else if (currentStage == Stage.Stage3_SortColor)
            {
                bool sorted = IsColorSorted();
                Debug.Log($"[Puzzle] Stage 3 color check: {(sorted ? "SOLVED" : "not in rainbow order yet")}");
                if (sorted) StartCoroutine(Stage3Complete());
            }
        }

        bool IsHeightSorted()
        {
            float[] heights = new float[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                heights[i] = slots[i].OccupiedBy.heightMultiplier;
            return IsMonotonic(heights);
        }

        bool IsColorSorted()
        {
            int[] colors = new int[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                colors[i] = (int)slots[i].OccupiedBy.bookColor;
            return IsMonotonic(colors);
        }

        static bool IsMonotonic(float[] arr)
        {
            bool asc = true, desc = true;
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] < arr[i - 1]) asc = false;
                if (arr[i] > arr[i - 1]) desc = false;
            }
            return asc || desc;
        }

        static bool IsMonotonic(int[] arr)
        {
            bool asc = true, desc = true;
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] < arr[i - 1]) asc = false;
                if (arr[i] > arr[i - 1]) desc = false;
            }
            return asc || desc;
        }

        IEnumerator Stage1Complete()
        {
            stageTransitioning = true;
            Debug.Log("[Puzzle] Stage 1 complete — all books placed (any order)");
            feedbackText?.Show("It's all wrong...");
            yield return new WaitForSeconds(2f);

            float[] shuffledHeights = (float[])HeightPool.Clone();
            Shuffle(shuffledHeights);
            for (int i = 0; i < books.Length; i++)
                books[i].heightMultiplier = shuffledHeights[i];

            DropRandomBooks(5);

            foreach (var book in books)
                book.ApplyHeight();

            foreach (var s in slots)
                if (s.IsOccupied) s.OccupiedBy.SnapToSlot(s);

            Debug.Log("[Puzzle] Stage 2 — sort books by height (ascending or descending). 5 books dropped.");
            currentStage = Stage.Stage2_SortHeight;
            stageTransitioning = false;
        }

        IEnumerator Stage2Complete()
        {
            stageTransitioning = true;
            Debug.Log("[Puzzle] Stage 2 complete — books sorted by height");
            feedbackText?.Show("No. No-no-no. NO...");
            yield return new WaitForSeconds(2f);

            AssignColors();

            for (int i = 1; i < slots.Length - 1; i++)
            {
                if (slots[i].IsOccupied)
                {
                    var book = slots[i].OccupiedBy;
                    slots[i].Vacate();
                    book.DropToFloor(new Vector3(-2f, 0f, 0f));
                }
            }

            foreach (var book in books)
                book.ApplyColor();

            Debug.Log("[Puzzle] Stage 3 — sort books by rainbow order (Red→Violet or reverse). 10 books dropped.");
            currentStage = Stage.Stage3_SortColor;
            stageTransitioning = false;
        }

        IEnumerator Stage3Complete()
        {
            stageTransitioning = true;
            Debug.Log("[Puzzle] Stage 3 complete — rainbow order correct! Loading BasicScene...");
            feedbackText?.Show("Finally...");
            yield return new WaitForSeconds(2.8f);
            SceneManager.LoadScene("SpatialHome");
        }

        void AssignColors()
        {
            Book minBook = books[0], maxBook = books[0];
            foreach (var b in books)
            {
                if (b.heightMultiplier < minBook.heightMultiplier) minBook = b;
                if (b.heightMultiplier > maxBook.heightMultiplier) maxBook = b;
            }

            var remaining = new List<BookColor>
            {
                BookColor.Red,
                BookColor.Orange, BookColor.Orange,
                BookColor.Yellow, BookColor.Yellow,
                BookColor.Green,  BookColor.Green,
                BookColor.Blue,   BookColor.Blue,
                BookColor.Indigo,
                BookColor.Violet
            };
            Shuffle(remaining);

            foreach (var book in books)
            {
                if (book == minBook) { book.bookColor = BookColor.Red; continue; }
                if (book == maxBook) { book.bookColor = BookColor.Violet; continue; }
                book.bookColor = remaining[0];
                remaining.RemoveAt(0);
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static void Shuffle<T>(T[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
