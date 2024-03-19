namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure;

internal static class Extensions
{
    /// <summary>
    /// Executes the provided action with a timeout and retry mechanism.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <param name="retryCondition">The condition that determines whether the action should be retried. If null or returns true, the action is retried.</param>
    /// <param name="timeout">The timeout for each execution of the action. If not provided, defaults to 1 second.</param>
    /// <param name="retries">The number of times to retry executing the action. Defaults to 1.</param>
    /// <param name="retryDelay">The delay between retries. If not provided, defaults to 1 second.</param>
    /// <param name="timeoutAction">The action to be executed if all retries time out.</param>
    /// <param name="logger">The logger used for logging retry and timeout information.</param>
    /// <param name="semaphore">The semaphore used to control access to the action execution.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public static async Task ExecuteWithTimeoutAsync(
        Func<int, Task> action,
        Func<Task<bool>>? retryCondition = null,
        TimeSpan timeout = default,
        int retries = 1,
        TimeSpan retryDelay = default,
        Func<int, Task>? timeoutAction = null,
        ILogger? logger = null,
        SemaphoreSlim? semaphore = null)
    {
        // Set default values for timeout and retryDelay if they are not provided
        timeout = timeout == default ? TimeSpan.FromSeconds(1) : timeout;
        retryDelay = retryDelay == default ? TimeSpan.FromSeconds(1) : retryDelay;

        // If a semaphore is provided, wait for it before proceeding
        if (semaphore != null)
        {
            await semaphore.WaitAsync();
        }

        try
        {
            // Retry the action the specified number of times
            for (int i = 0; i < retries; i++)
            {
                // Log the start of a retry
                logger?.LogTrace("Starting retry {RetryNumber} of {TotalRetries}.", i + 1, retries);

                try
                {
                    // Start the action
                    var task = action(i);

                    // If the action does not complete within the timeout, handle the timeout
                    if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                    {
                        // Log the timeout
                        logger?.LogTrace("Retry {RetryNumber} of {TotalRetries} timed out.", i + 1, retries);

                        // If this is the last retry or the retry condition is not met, execute the timeout action and break the loop
                        if (i == retries - 1 || (retryCondition != null && !await retryCondition()))
                        {
                            // Log the execution of the timeout action
                            logger?.LogTrace("All retries timed out. Executing timeout action.");
                            if (timeoutAction != null)
                            {
                                await timeoutAction(i);
                            }
                            break;
                        }
                        else
                        {
                            // Log the delay before the next retry
                            logger?.LogTrace("Waiting {DelaySeconds} seconds before next retry.", retryDelay.TotalSeconds);
                            await Task.Delay(retryDelay);
                        }
                    }
                    else
                    {
                        // Log the successful completion of a retry
                        logger?.LogTrace("Retry {RetryNumber} of {TotalRetries} completed successfully.", i + 1, retries);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Log any error that occurs during a retry
                    logger?.LogError(ex, "An error occurred during retry {RetryNumber} of {TotalRetries}.", i + 1, retries);
                    throw;
                }
            }
        }
        finally
        {
            // Release the semaphore if it is provided
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the Dictionary.
    /// </summary>
    /// <param name="source">The Dictionary to add elements to.</param>
    /// <param name="collection">The collection whose elements should be added to the end of the Dictionary.</param>
    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> source, IEnumerable<KeyValuePair<TKey, TValue>> collection)
    {
        ArgumentNullException.ThrowIfNull(source);

        ArgumentNullException.ThrowIfNull(collection);

        foreach (var item in collection)
        {
            source.Add(item);
        }
    }
    public static void TryAddRange<TKey, TValue>(this IDictionary<TKey, TValue> source, IEnumerable<KeyValuePair<TKey, TValue>> collection, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(collection);

        foreach (var item in collection)
        {
            if (!source.TryAdd(item.Key, item.Value))
            {
                logger?.LogWarning("Key {Key} already exists in the dictionary.", item.Key);
            }
        }
    }

    public static string[] SplitByNewLine(this string str)
    {
        if (str == null)
        {
            return [];
        }

        char[] separator = { '\r', '\n' };
        return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] SplitConfiguration(this string str)
    {
        if (str == null)
        {
            return [];
        }

        char[] separator = ['\r', '\n', ','];
        return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] SplitByComma(this string str)
    {
        if (str == null)
        {
            return [];
        }

        return str.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}