netrefject is a small POC to show how to use Mono.Cecil to inject
.Net payloads into .Net assemblies if you have write access to those assemblies 

(And I assume they need to be unsigned but I haven't tested - though it seems obvious)


I have not seen this functionality in various C2/payload frameworks yet, just in-memory injection

This project is being developed on Linux with vscode, running dotnet core 2, just committed a fix to allow you to debug with vscode and be able to send stdinput during debugging. I realised the module import stuff do not work, so status of the live code is you're literally injecting a single string into whatever assembly you're injecting to.


PS. If you've been following, I've waxxed the small methodref issue I had. Next up - working payload hopefullY!
