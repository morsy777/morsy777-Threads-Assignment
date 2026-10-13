using System.Drawing;
using System.Runtime.CompilerServices;

namespace OrderProcessing;

public static class Logger
{
  private static readonly Lock _lock = new();
  public static void Success<T>(string message) 
    => Wrtie(typeof(T).Name, message, ConsoleColor.Green);

  public static void Info<T>(string message)
    => Wrtie(typeof(T).Name, message, ConsoleColor.Green);

  public static void Error<T>(string message)
    => Wrtie(typeof(T).Name, message, ConsoleColor.Red);

  // without finaly, if an exception occur that color doesn't change.
  private static void Wrtie(string source, string message, ConsoleColor color)
  {
    lock (_lock)
    {
      try
      {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Environment.CurrentManagedThreadId}] {source} -> {message}");
      }
      finally
      {
        Console.ResetColor();
      }
    }
  }
}