using System;

namespace testconsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[!!] Loading Test Library");
            testlibrary.Testclass testClass = new testlibrary.Testclass();
            Console.WriteLine("[!!] Executing Test Method");
            testClass.TestMethod();
            Console.WriteLine("[!!] Unloading Test Library");
        }
    }
}
