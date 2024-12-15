namespace DMS_OCR.Exceptions;

public class OcrWorkerExceptions
{
    public class OcrWorkerException : Exception
    {
        public OcrWorkerException() { }

        public OcrWorkerException(string message) : base(message) { }

        public OcrWorkerException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class OcrProcessingException : OcrWorkerException
    {
        public OcrProcessingException(string message) : base(message) { }
        public OcrProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class OcrFileNotFoundException : OcrWorkerException
    {
        public OcrFileNotFoundException(string message) : base(message) { }
        public OcrFileNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class ElasticSearchException : OcrWorkerException
    {
        public ElasticSearchException(string message) : base(message) { }
        public ElasticSearchException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class MessageQueueConnectionException : OcrWorkerException
    {
        public MessageQueueConnectionException(string message) : base(message) { }
        public MessageQueueConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}