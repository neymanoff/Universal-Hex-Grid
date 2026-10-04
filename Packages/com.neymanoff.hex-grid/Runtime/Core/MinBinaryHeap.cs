using System;

namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Lightweight, zero-allocation binary min-heap for priority queue operations in A* and Dijkstra.
    /// Pure C# / .NET Standard 2.1 compatible.
    /// </summary>
    /// <typeparam name="TElement">The payload item type.</typeparam>
    /// <typeparam name="TPriority">The priority type (must implement IComparable).</typeparam>
    public sealed class MinBinaryHeap<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private struct Node
        {
            public TElement Element;
            public TPriority Priority;

            public Node(TElement element, TPriority priority)
            {
                Element = element;
                Priority = priority;
            }
        }

        private Node[] nodes;
        private int count;

        public int Count => count;

        public MinBinaryHeap(int initialCapacity = 64)
        {
            nodes = new Node[Math.Max(4, initialCapacity)];
            count = 0;
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            if (count == nodes.Length)
            {
                Array.Resize(ref nodes, nodes.Length * 2);
            }

            nodes[count] = new Node(element, priority);
            SiftUp(count);
            count++;
        }

        public TElement Dequeue()
        {
            if (count == 0)
                throw new InvalidOperationException("The heap is empty.");

            var result = nodes[0].Element;
            count--;

            if (count > 0)
            {
                nodes[0] = nodes[count];
                nodes[count] = default;
                SiftDown(0);
            }
            else
            {
                nodes[0] = default;
            }

            return result;
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (count == 0)
            {
                element = default;
                priority = default;
                return false;
            }

            element = nodes[0].Element;
            priority = nodes[0].Priority;
            count--;

            if (count > 0)
            {
                nodes[0] = nodes[count];
                nodes[count] = default;
                SiftDown(0);
            }
            else
            {
                nodes[0] = default;
            }

            return true;
        }

        public void Clear()
        {
            Array.Clear(nodes, 0, count);
            count = 0;
        }

        private void SiftUp(int index)
        {
            var node = nodes[index];
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (node.Priority.CompareTo(nodes[parent].Priority) >= 0)
                    break;

                nodes[index] = nodes[parent];
                index = parent;
            }
            nodes[index] = node;
        }

        private void SiftDown(int index)
        {
            var node = nodes[index];
            int half = count / 2;

            while (index < half)
            {
                int child = 2 * index + 1;
                int right = child + 1;

                if (right < count && nodes[right].Priority.CompareTo(nodes[child].Priority) < 0)
                {
                    child = right;
                }

                if (node.Priority.CompareTo(nodes[child].Priority) <= 0)
                    break;

                nodes[index] = nodes[child];
                index = child;
            }
            nodes[index] = node;
        }
    }
}
