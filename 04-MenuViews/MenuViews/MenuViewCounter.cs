namespace MenuViews;

public sealed class MenuViewCounter
{
    public int Views;

    public void RegisterView()
    {
        // Opss, this opertaion is not automic so 
        // it read, update, wrtie and there is a lost
        // update problem.
        //Views++; 

        Interlocked.Increment(ref Views);
    }
}
