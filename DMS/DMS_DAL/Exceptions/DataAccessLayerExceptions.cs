namespace DMS_DAL.Exceptions;

public class DataAccessLayerException : Exception
{
    public DataAccessLayerException() { }

    public DataAccessLayerException(string message) : base(message) { }

    public DataAccessLayerException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException : DataAccessLayerException
{
    public EntityNotFoundException(string message) : base(message) { }
}

public class DbUpdateException : DataAccessLayerException
{
    public DbUpdateException(string message, Exception innerException) : base(message, innerException) { }
}

public class DbConnectionException : DataAccessLayerException
{
    public DbConnectionException(string message, Exception innerException) : base(message, innerException) { }
}