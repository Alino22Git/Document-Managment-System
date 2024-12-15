namespace DMS_REST_API.Exceptions;

public class MessagingException : Exception
{
    public MessagingException() { }
    public MessagingException(string message) : base(message) { }
    public MessagingException(string message, Exception innerException) : base(message, innerException) { }
}

public class MessagePublishException : MessagingException
{
    public MessagePublishException(string message, Exception innerException = null) : base(message, innerException) { }
}

public class MessageQueueConnectionException : MessagingException
{
    public MessageQueueConnectionException(string message, Exception innerException = null) : base(message, innerException) { }
}