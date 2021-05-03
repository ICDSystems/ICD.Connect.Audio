namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
    public static class RpcUtils
    {
	    // RPCID's are used to seperate responses from the QSys based on the command sent
	    internal const string RPCID_NO_OP = "NoOp";
	    internal const string RPCID_STATUS_GET = "Status";
	    internal const string RPCID_NAMED_CONTROL_GET = "NamedControlGet";
	    internal const string RPCID_NAMED_CONTROL_SET = "NamedControlSet";
	    internal const string RPCID_NAMED_COMPONENT_GET = "NamedComponentGet";
	    internal const string RPCID_CHANGEGROUP_RESPONSE = "ChangeGroupResponse";
	    internal const string RPCID_NAMED_COMPONENTS_GET_ALL = "NamedComponentsGet";
    }
}
