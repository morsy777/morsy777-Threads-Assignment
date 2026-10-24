namespace OrderProcessing.Exceptions;

public class InvalidCouponException : Exception
{
  public InvalidCouponException(string message) : base(message)
  {
  }
}
