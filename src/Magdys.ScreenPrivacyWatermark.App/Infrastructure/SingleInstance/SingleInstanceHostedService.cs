namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.SingleInstance;

internal class SingleInstanceHostedService(ILogger<SingleInstanceHostedService> logger, SingleInstanceOptions singleInstanceOptions) : IHostedService, IDisposable
{
    private Mutex? _mutex;

    private bool _isMutexCreated = false;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StartAsync));
        try
        {
            if (!singleInstanceOptions.Enabled)
            {
                logger.LogInformation("Skipping Single Instance Mode. The application will run multiple instances.");
                return Task.CompletedTask;
            }

            logger.LogTrace("Creating or getting the mutex.");
            _mutex = new Mutex(true, singleInstanceOptions.MutexId, out _isMutexCreated);
            if (!_isMutexCreated)
            {
                logger.LogWarning("Application is already running.");
                singleInstanceOptions.OnAlreadyRunning?.Invoke(logger);

                logger.LogTrace("Exiting application due to already running instance.");
                Environment.Exit(0);
            }

            logger.LogTrace("Mutex created successfully.");
            _isMutexCreated = true;
        }
        catch (AbandonedMutexException e)
        {
            // Another instance didn't cleanup correctly!
            // we can ignore the exception, it happened on the "WaitOne" but still the mutex belongs to us
            logger.LogWarning(e, "{appName} didn't cleanup correctly, but we got the mutex {mutexId}.", Metadata.ApplicationNameShort, singleInstanceOptions.MutexId);
        }
        catch (UnauthorizedAccessException e)
        {
            logger.LogError(e, "{appName} is most likely already running for a different user in the same session, can't create/get mutex {mutexId} due to error.", Metadata.ApplicationNameShort, singleInstanceOptions.MutexId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Problem obtaining the Mutex {mutexId} for {appName}, assuming it was already taken!", Metadata.ApplicationNameShort, singleInstanceOptions.MutexId);
        }

        logger.LogTrace("Executed {method}.", nameof(StartAsync));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StopAsync));
        if (_mutex != null && _isMutexCreated && _mutex.WaitOne(0))
        {
            _mutex.ReleaseMutex();
        }

        logger.LogTrace("Executed {method}.", nameof(StopAsync));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _mutex != null)
        {
            if (_mutex.WaitOne(TimeSpan.Zero))
            {
                _mutex.ReleaseMutex();
            }

            _mutex.Dispose();
        }

    }
}
