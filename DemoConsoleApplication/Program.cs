using DemoConsoleApplication;
using Newtonsoft.Json;

foreach (var item in ProductCategory.Items)
{
   Console.WriteLine($"{item.Name} => {JsonConvert.SerializeObject(item)}");
}
