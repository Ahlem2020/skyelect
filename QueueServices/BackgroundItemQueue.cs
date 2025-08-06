using System.Collections.Concurrent;

namespace ElectionApi.QueueServices
{
    public interface IBackgroundItemQueue<T>
    {
        /// <summary>
        /// Schedules a task which needs to be processed.
        /// </summary>
        /// <param name="item">Item to be executed.</param>
        void Enqueue(T item);

        /// <summary>
        /// Tries to remove and return the object at the beginning of the queue.
        /// </summary>
        /// <returns>If found, an item, otherwise null.</returns>
        T? Dequeue();
    }

    public class BackgroundItemQueue<T> : IBackgroundItemQueue<T> where T : class
    {
        private readonly ConcurrentQueue<T> _items = new ConcurrentQueue<T>();

        public void Enqueue(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Enqueue(item);
        }

        public T? Dequeue()
        {
            var success = _items.TryDequeue(out var workItem);

            return success
                ? workItem
                : null;
        }
    }
}
