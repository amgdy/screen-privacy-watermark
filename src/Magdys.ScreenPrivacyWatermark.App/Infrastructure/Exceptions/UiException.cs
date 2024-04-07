namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Exceptions;

public class UiException : Exception
{
    public UiException()
    {
    }

    public UiException(string message) : base(message)
    {
    }

    public UiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
