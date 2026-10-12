using System.Collections.Concurrent;

namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{
    public readonly ConcurrentDictionary<string, int> Views = new();
    public readonly ConcurrentQueue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();

    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {
        Views.AddOrUpdate(
          product,
          1,
          (_, old) => old + 1
        );
    }

    public void PlaceOrder(int orderId)
    {
        EmailJobs.Enqueue($"Email: order {orderId}");
    }

    public void EmailWorker()
    {
        while (true)
        {
            try
            {
                string? job = null;

                if(EmailJobs.IsEmpty) // Early exit
                  return; 

                if (EmailJobs.TryDequeue(out string? order))
                  job = order;

                if (job is not null)
                {
                    lock (_sentSync)
                        SentEmails.Add(job);
                    continue;
                }

                if (ProducersDone && EmailJobs.Count == 0)
                    return;

                Thread.Sleep(1);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("InvalidOperationException");
                if (ProducersDone)
                    return;
            }
        }
    }
}
