namespace OrderProcessing.Exceptions;
public class DataFileCorruptedException : Exception
{
  public DataFileCorruptedException(string msg) : base(msg)
  {
    
  }
}