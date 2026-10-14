using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using OrderProcessing.Exceptions;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Persistence;

/// <summary>
/// - The Class may lock complex but its not.
/// - First thing we create a concurrentDict to avoid dict problems with multiple threads 
///   so each file has its own async lock (semaphore slim).
/// - In ctor we build the Data dir path and then create the dir.
/// - ReadAsync Read, WriteAsync, Write: is a generic methods to be affordable to work with all domain models, 
///   those methods must protect the file state, in other word if there is a task or thread read/write the file the other
///   must wait it unitl it finish its work, that why I created a lock for each file.
/// - ReadCoreAsync, ReadCore, WriteCoreAsync, WriteCore: is just a helper method that make the actual read and write
///   operations, I perfer separate them as helpers to make the code easier to understand and readable.
/// - WriteCoreAsync, WriteCore: to achive R6, I make a temp file that conatinas the changes and if there is no exception 
///   occur, the I move the content of temp file into the original file.
/// - The normal flow for those methods: first thing get the file path and its lock, if the lock free u don't need to wait
///   and u can perform read/write on that file, after finishing ur work, release the lock to allow others to read/write.
/// - Important Note: every Async method must check if the lock is free before allow to the task to work on file.
/// </summary>
// where T : new() means the all objs has parameterless ctor
public sealed class JsonFileStore
{
    // each filePath has its own semaphoreSlim (async lock)
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _dataDirectory;

    // Creating the Data dir path in ctor and then create it.
    public JsonFileStore() 
    {
      _dataDirectory = Path.Combine(AppContext.BaseDirectory, DataFiles.DataDir);

      Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<T> ReadAsync<T>(string fileName, CancellationToken cancellationToken = default) where T : new()
    {
      string path = GetPath(fileName); // use a helper method instead of repeating the same code 
      SemaphoreSlim gate = GetLock(path); // each filePath has its own lock, if the lock free then we can read/write

      await gate.WaitAsync(cancellationToken);

      try
      {
        // Just to simplify the method insted of writing the read logic here
        return await ReadCoreAsync<T>(path, fileName, cancellationToken);
      }
      finally
      {
        gate.Release();
      }
    }

    public async Task WriteAsync<T>(string fileName, T data, CancellationToken cancellationToken = default)
      where T : new()
    {
      string path = GetPath(fileName);
      SemaphoreSlim gate = GetLock(path);

      await gate.WaitAsync(cancellationToken);

      try
      {
        await WriteCoreAsync(path, data, cancellationToken);
      }
      finally
      {
        gate.Release();
      }
    }

    public T Read<T>(string fileName) where T : new()
    {
        string path = GetPath(fileName);
        SemaphoreSlim gate = GetLock(path);

        gate.Wait();

        try
        {
          return ReadCore<T>(path, fileName);
        }
        finally
        {
          gate.Release();
        }
    }

    public void Write<T>(string fileName, T data) where T : new()
    {
      string path = GetPath(fileName);
      SemaphoreSlim gate = GetLock(path);

      gate.Wait();

      try
      {
        WriteCore(path, data);
      }
      finally
      {
        gate.Release();
      }
    }

    private static async Task<T> ReadCoreAsync<T>(string path, string fileName, CancellationToken cancellationToken)
      where T : new()
    {
      if (!File.Exists(path))
        return new T();

      try
      {
        string json = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

        // convert json into object
        return JsonSerializer.Deserialize<T>(json, JsonSerializerConfig.Options)
          ?? throw new JsonException("JSON content is null.");
      }
      catch (JsonException ex)
      {
        throw new DataFileCorruptedException($"File '{fileName}' contains invalid JSON.", ex);
      }
    }

    private static T ReadCore<T>(string path, string fileName)
      where T : new()
    {
      if (!File.Exists(path)) // if the file doesnt exist we return empty object 
        return new T();

      try
      {
        string json = File.ReadAllText(path, Encoding.UTF8);

        return JsonSerializer.Deserialize<T>(json, JsonSerializerConfig.Options)
          ?? throw new JsonException("JSON content is null.");
      }
      catch (JsonException ex)
      {
        throw new DataFileCorruptedException($"File '{fileName}' contains invalid JSON.", ex);
      }
    }

    private static async Task WriteCoreAsync<T>(string path, T data, CancellationToken cancellationToken)
    {
      // convert the objs int json
      string json = JsonSerializer.Serialize(data, JsonSerializerConfig.Options);

      // store it in a byte array to write it in stream
      byte[] bytes = Encoding.UTF8.GetBytes(json);
      string tempPath = GetTempPath(path);

      try
      {
        await using (var stream = new FileStream(
          tempPath,
          FileMode.CreateNew,
          FileAccess.Write,
          FileShare.None,
          81920,
          FileOptions.Asynchronous))
        {
          await stream.WriteAsync(bytes, cancellationToken);
          await stream.FlushAsync(cancellationToken);
          stream.Flush(flushToDisk: true);
        }

        cancellationToken.ThrowIfCancellationRequested();

        File.Move(tempPath, path, overwrite: true);
      }
      finally
      {
        if (File.Exists(tempPath))
          File.Delete(tempPath);
      }
    }

    // 1- convert the data into json string.
    // 2- the data stored as bytes so convert string into bytes.
    // 3- using() to dispose this section after using it.
    // 4- open stream in the tempPathe that contains the changes.
    // 5- if there is no excep, then move the content of temp file into the original file. to achicve (R6)
    // 6- at end delete the temp file, because we don't need it again.
    private static void WriteCore<T>(string path, T data)
    {
      string json = JsonSerializer.Serialize(data, JsonSerializerConfig.Options);

      byte[] bytes = Encoding.UTF8.GetBytes(json);
      string tempPath = GetTempPath(path);

      try
      {
        using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
          stream.Write(bytes);
          stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, path, overwrite: true);
      }
      finally
      {
        if (File.Exists(tempPath))
          File.Delete(tempPath);
      }
    }

    // This code is repeated so in every change ,
    // we must to visit all place and change it
    // but the fault is possible, so the better is to
    // make a helper method and the changes be in one place
    private string GetPath(string fileName)
    {
      if (Path.GetFileName(fileName) != fileName)
      {
        throw new ArgumentException("Only file names are allowed.",nameof(fileName));
      }

      return Path.Combine(_dataDirectory, fileName);
    }

    // Get the lock of specific file or Add the file with a new SemaphoreSlim(lock, permits)
    private static SemaphoreSlim GetLock(string path) =>
      FileLocks.GetOrAdd(Path.GetFullPath(path), _ => new SemaphoreSlim(1, 1)); 

    private static string GetTempPath(string path) =>
      $"{path}.{Guid.NewGuid():N}.tmp";
}