netrefject is a small POC to show how to use Mono.Cecil to inject
.Net payloads into .Net assemblies if you have write access to those assemblies 

(And I assume they need to be unsigned but I haven't tested - though it seems obvious)


I have not seen this functionality in various C2/payload frameworks yet, just in-memory injection


This project is being developed on Linux with vscode, running dotnet core 2, just committed a fix to allow you to debug with vscode and be able to send stdinput during debugging. I realised the module import stuff do not work, so status of the live code is you're literally injecting a single string into whatever assembly you're injecting to.

The cool bit of this (IMO) is to be able to write a standard .Net method and to get that injected into the target assembly (IE. You don't have to write IL). 

It seems that I have a little hurdle to get over before I can pull that off in such a manner that it's useful to all of us. 

Either I'm going to have to get the current module reference code to work ('m1.Module.Import(typeof(Console).GetMethod("WriteLine",new[] {typeof(string)}));') OR I'm going to have to revert to using the ILProcessor to inject the calls to built-in methods like that. 

The last mentioned bit is what I'm trying to stay away from - it'll work like that, but it would be way less user friendly for all of us who want to write our own evil methods.

Anyway, if you can figure out why the module references are failing as I've done them, let me know please!

[!!] Hacky way has been added and is working - but i haven't given up on the cool way yet...
