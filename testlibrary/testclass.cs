using System;
using System.Net;

namespace testlibrary
{
    public class Testclass
    {
        public Testclass()
        {
            Console.WriteLine("This is Constructor");
        }

        public void TestMethod()
        {
            Console.WriteLine("This is test method");
        }

        public void FlapMethod(){
            Console.WriteLine("Faan");
            WebClient wc = new WebClient();
            byte[] data = wc.DownloadData("Http://10.20.29.137:8000/fanie");
        }
    }
}
