using System.Collections.Generic;

namespace NlFuzzy;

/// <summary>
/// Collects the best <c>capacity</c> matches without materializing or sorting every candidate.
/// With a positive capacity the collector keeps a bounded binary heap, so memory stays O(capacity)
/// and the total cost is O(n log capacity) instead of O(n) memory plus an O(n log n) sort.
/// A capacity of zero or less keeps every match, which still needs a full sort.
/// </summary>
/// <typeparam name="T">The matched item type.</typeparam>
internal sealed class TopMatchCollector<T>
{
    private readonly List<FuzzyMatch<T>> _heap;
    private readonly int _capacity;

    internal TopMatchCollector(int capacity)
    {
        _capacity = capacity;
        _heap = capacity > 0 ? new List<FuzzyMatch<T>>(capacity) : new List<FuzzyMatch<T>>();
    }

    internal int Count => _heap.Count;

    /// <summary>Offers a match. Ignored when it cannot make the current top-k.</summary>
    internal void Add(FuzzyMatch<T> match)
    {
        if (_capacity <= 0 || _heap.Count < _capacity)
        {
            _heap.Add(match);
            SiftUp(_heap.Count - 1);

            return;
        }

        // The heap root is the weakest match kept so far; replace it only when the candidate wins.
        if (Compare(match, _heap[0]) >= 0)
        {
            return;
        }

        _heap[0] = match;
        SiftDown(0);
    }

    /// <summary>Returns the collected matches, best first.</summary>
    internal List<FuzzyMatch<T>> ToSortedList()
    {
        _heap.Sort(DutchEnumerableExtensions.CompareByScoreDescending);

        return _heap;
    }

    private static int Compare(FuzzyMatch<T> left, FuzzyMatch<T> right) =>
        DutchEnumerableExtensions.CompareByScoreDescending(left, right);

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            var parent = (index - 1) / 2;

            if (Compare(_heap[index], _heap[parent]) <= 0)
            {
                return;
            }

            Swap(index, parent);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            var left = (2 * index) + 1;

            if (left >= _heap.Count)
            {
                return;
            }

            var weakest = left;
            var right = left + 1;

            if (right < _heap.Count && Compare(_heap[right], _heap[left]) > 0)
            {
                weakest = right;
            }

            if (Compare(_heap[weakest], _heap[index]) <= 0)
            {
                return;
            }

            Swap(index, weakest);
            index = weakest;
        }
    }

    private void Swap(int left, int right)
    {
        var temp = _heap[left];
        _heap[left] = _heap[right];
        _heap[right] = temp;
    }
}
