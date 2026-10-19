using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderProcessing.Exceptions;
public class ShippingProviderUnavailableException : Exception
{
  public ShippingProviderUnavailableException(string msg) : base(msg)
  {
    
  }
}