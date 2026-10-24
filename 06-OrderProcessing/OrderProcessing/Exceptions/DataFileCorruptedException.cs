namespace OrderProcessing.Exceptions;
public class DataFileCorruptedException : Exception
{
  public DataFileCorruptedException(string msg, Exception ex) : base(msg, ex)
  {
    
  }
}