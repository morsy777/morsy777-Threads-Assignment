namespace OrderProcessing.Exceptions;

public class SmtpUnavailableException : Exception
{
  public SmtpUnavailableException(string message) : base(message)
  {
  }
}
