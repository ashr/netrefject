using System;
using System.Net;

namespace testlibrary
{
    public class Testclass
    {
        public Testclass()
        {
            Console.WriteLine("This is Constructor");
            WebClient wc =new WebClient();
            byte[] data = wc.DownloadData("http://joupoes");
        }

        public void TestMethod()
        {
            Console.WriteLine("This is test method");
        }
    }
}
