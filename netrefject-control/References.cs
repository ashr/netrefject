using Mono.Cecil;
namespace netrefject{
    public struct References
    {
        public TypeReference uint8; //byte
        public TypeReference Assembly;
        public TypeReference MethodInfo;
        public TypeReference var; //object
        public TypeReference boolean;
        public TypeReference var_array; //object[]
        public TypeReference int32;
        public TypeReference Exception;

        public MethodReference WebClientCtor;
        public MethodReference WebClient_DownloadData;

        public MethodReference Assembly_Load;
        public MethodReference Assembly_getEntryPoint;
        public MethodReference Assembly_CreateInstance;

        public MethodReference MemberInfo_getName;

        public MethodReference MethodBase_GetParameters;
        public MethodReference MethodBase_Invoke;
    }
}