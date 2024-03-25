using System.Collections.Concurrent;

namespace FilesDeduplicator.Extensions
{
    internal static class BlockingCollectionExtensions
    {
        public static List<T> GetConsumingEnumerableBatch<T>(
            this BlockingCollection<T> collection,
            int maxCount,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var cts = new CancellationTokenSource(timeout);

            // taken from: https://learn.microsoft.com/en-us/dotnet/standard/threading/how-to-listen-for-multiple-cancellation-requests
            var timeoutToken = cts.Token;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, cancellationToken);
            var items = new List<T>();
            try
            {
                foreach (var item in collection.GetConsumingEnumerable(linkedCts.Token))
                {
                    items.Add(item);
                    if (items.Count >= maxCount)
                        return items;
                }
            }
            catch (OperationCanceledException)
            {
                if (timeoutToken.IsCancellationRequested)
                    return items; // stop waiting for more items

                throw; // else cancel consuming completely
            }

            return items;
        }
    }
}
