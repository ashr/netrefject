using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

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
                Console.WriteLine(e.Message);
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

            m1.Module.Import(typeof(Console).GetMethod("WriteLine",new[] {typeof(string)}));

            for (int i = 0; i < originaInstructionCount; i++)
            {
                //Just before ret 
                //Yes offset stuff is fucked up, will fix later
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
                    //retCall.Offset = retCall.Offset + instructionCounter;
                }
            }

            //m1.Body.Variables.Add(new VariableDefinition(targetType.Module.ImportReference(typeof(System.Console))))
            

            /*ilp.Body.Variables.Add(targetType.Module.ImportReference(System.Console.GetT));
            foreach (var v in vars)
            {
                var nv = new VariableDefinition(targetType.Module.ImportReference(v.VariableType));
                m1.Body.Variables.Add(nv);
            }*/

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

        private void evilMethod()
        {
            string a = ("This is the beginning of the evil method code that's being injected into the assembly");

            Console.WriteLine("I am evilMethod");

            string b = ("This is the end of the evil method");
        }
    }
}
