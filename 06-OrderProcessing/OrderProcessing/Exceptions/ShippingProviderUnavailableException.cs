namespace OrderProcessing.Exceptions;

public class ShippingProviderUnavailableException : Exception
{
  public ShippingProviderUnavailableException(string msg) : base(msg)
  {
    
  }
}