namespace OrderProcessing.Exceptions;

public class DatabaseWriteException : Exception
{
  public DatabaseWriteException(string message) : base(message)
  {
  }
}
