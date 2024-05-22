using System;
using System.Collections.Generic;

public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private List<(TElement Element, TPriority Priority)> _elements = new List<(TElement, TPriority)>();

    public int Count => _elements.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        _elements.Add((element, priority));
        HeapifyUp(_elements.Count - 1);
    }

    public TElement Dequeue()
    {
        if (Count == 0)
            throw new InvalidOperationException("The priority queue is empty");

        TElement element = _elements[0].Element;
        _elements[0] = _elements[_elements.Count - 1];
        _elements.RemoveAt(_elements.Count - 1);
        HeapifyDown(0);
        return element;
    }

    public bool Contains(TElement element)
    {
        return _elements.Exists(e => e.Element.Equals(element));
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (_elements[index].Priority.CompareTo(_elements[parentIndex].Priority) >= 0)
                break;

            (_elements[index], _elements[parentIndex]) = (_elements[parentIndex], _elements[index]);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = _elements.Count - 1;
        while (index < lastIndex)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            if (leftChildIndex > lastIndex)
                break;

            int smallestChildIndex = (rightChildIndex <= lastIndex && _elements[rightChildIndex].Priority.CompareTo(_elements[leftChildIndex].Priority) < 0) ? rightChildIndex : leftChildIndex;

            if (_elements[index].Priority.CompareTo(_elements[smallestChildIndex].Priority) <= 0)
                break;

            (_elements[index], _elements[smallestChildIndex]) = (_elements[smallestChildIndex], _elements[index]);
            index = smallestChildIndex;
        }
    }
}
