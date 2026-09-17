using System.Net;

public class DomainException : Exception
{
    public HttpStatusCode StatusCode { get; private set; }
    public DomainException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}