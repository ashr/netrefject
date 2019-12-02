using System;
using netrefject;

public static class Startup
{
    public static void Main(string[] args)
    {
        /*
        if (args.Length != 1)
        {
            Worker.syntax();
            return;
        }

        string filename = args[0];

        if (!filename.EndsWith("dll"))
        {
            Worker.syntax();
            return;
        }*/
        //string filename = @"C:\Users\ashr\source\repos\netrefject\testlibrary\bin\Debug\netstandard2.0\testlibrary.dll";
        string filename = "/home/ashr/dev/customShit/Code/netrefject/testlibrary/bin/Debug/netstandard2.0/testlibrary.dll";
        Worker w = new Worker();
        w.HandleInjectionFlow(filename);
    }
}
