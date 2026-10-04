namespace IdentityProvider.Web.Infrastructure;

internal static class AsyncEnumerableExtensions
{
    public static Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ExecuteAsync(source, cancellationToken);

        static async Task<List<T>> ExecuteAsync(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
        {
            var list = new List<T>();

            await foreach (var element in source.WithCancellation(cancellationToken))
            {
                list.Add(element);
            }

            return list;
        }
    }
}
