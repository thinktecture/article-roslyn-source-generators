namespace DemoConsoleApplication;

class Program
{
    static void Main(string[] args)
    {
        foreach (var item in ProductCategory.Items)
        {
            Console.WriteLine(item.Name);
        }
    }
}
