using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Serializing;
using UnityEngine;
using UnityEditor;
using WrappedFishNet;

#if UNITY_EDITOR

[CustomEditor(typeof(WrappedView))]
public class WrappedViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Scan MonoBehaviours"))
            ((WrappedView)target).ScanForBehaviours();
        DrawDefaultInspector();
    }
}
#endif

namespace WrappedFishNet
{
    [DefaultExecutionOrder(-64)]
    [RequireComponent(typeof(NetworkObject))]
    public class WrappedView : NetworkBehaviour
    {
        public Action OnStart;

        private Dictionary<string, Method> wrappedMethods = new();
        private struct Method
        {
            public MonoBehaviour source; public MethodInfo methodInfo; public bool wantSenderInfo;
            public Method(MonoBehaviour source, MethodInfo methodInfo, bool wantSenderInfo)
            {
                this.source = source;
                this.methodInfo = methodInfo;
                this.wantSenderInfo = wantSenderInfo;
            }
        }
        [SerializeField]
        private MonoBehaviour[] behavioursToBeScanned;

        private void Awake()
        {
            wrappedMethods = new();
            foreach (MonoBehaviour component in behavioursToBeScanned)
                foreach (var method in component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => Attribute.IsDefined(method, typeof(WrappedRPC)))
                .ToList())
                    wrappedMethods.Add(method.Name,
                        new Method(component
                        , method
                        , method.GetParameters().Length == 0 ? false : method.GetParameters().Last().ParameterType.Equals(typeof(NetworkConnection))));
        }
        public void ScanForBehaviours()
        {
            behavioursToBeScanned = GetComponentsInChildren<MonoBehaviour>(true)
                .Where(comp => comp.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(method => Attribute.IsDefined(method, typeof(WrappedRPC)))).ToArray();
        }
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().enabled)
                GetComponent<NetworkObject>().enabled = true;
            OnStart?.Invoke();
        }

        public void RPC(string methodName, RpcTarget target, params object[] args)
        {
            if (wrappedMethods[methodName].wantSenderInfo)
                args = args.Append(LocalConnection).ToArray();
            switch (target)
            {
                case RpcTarget.MasterClient:
                    ServerRPC(methodName, args);
                    break;
                case RpcTarget.Others:
                    OthersRPC(methodName, args, LocalConnection);
                    break;
                case RpcTarget.All:
                    AllRPC(methodName, args);
                    break;
            }
        }
        public void RPC(string methodName, NetworkConnection target, params object[] args)
        {
            TargetRPC(target, methodName, args);
        }


        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC(string methodName, object[] args)
        {
            FinalInvoke(methodName, args);
        }
        [ServerRpc(RequireOwnership = false)]
        private void OthersRPC(string methodName, object[] args, NetworkConnection sender)
        {
            FinalOthersRPC(methodName, args, sender);
        }
        [ObserversRpc]
        private void FinalOthersRPC(string methodName, object[] args, NetworkConnection sender)
        {
            if (LocalConnection.Equals(sender))
                return;
            FinalInvoke(methodName, args);
        }

        [ServerRpc(RequireOwnership = false)]
        private void AllRPC(string methodName, object[] args)
        {
            AllClientsRPC(methodName, args);
        }

        [ObserversRpc]
        private void AllClientsRPC(string methodName, object[] args)
        {
            FinalInvoke(methodName, args);
        }

        [TargetRpc]
        private void TargetRPC(NetworkConnection target, string methodName, object[] args)
        {
            FinalInvoke(methodName, args);
        }

        private void FinalInvoke(string methodName, object[] args)
        {
            wrappedMethods[methodName].methodInfo.Invoke(wrappedMethods[methodName].source, args);
        }

    }
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class WrappedRPC : Attribute
    { }
    public enum RpcTarget { MasterClient, Others, All }
    public static class ObjectSerializer
    {
        public static void WriteObject(this Writer writer, object value)
        {
            switch (value)
            {
                case int:
                    writer.WriteByte(0);
                    writer.WriteInt32((int)value);
                    break;
                case string:
                    writer.WriteByte(1);
                    writer.WriteString((string)value);
                    break;
                case bool:
                    writer.WriteByte(2);
                    writer.WriteBoolean((bool)value);
                    break;
                case NetworkConnection:
                    writer.WriteByte(3);
                    writer.WriteNetworkConnection((NetworkConnection)value);
                    break;
                case Vector3:
                    writer.WriteByte(4);
                    writer.WriteVector3((Vector3)value);
                    break;
                default:
                    throw new ArgumentException($"Unsupported type: {value.GetType().Name}");
            }
        }
        public static object ReadObject(this Reader reader)
        {
            byte dataType = reader.ReadByte();
            return dataType switch
            {
                0 => reader.ReadInt32(),
                1 => reader.ReadString(),
                2 => reader.ReadBoolean(),
                3 => reader.ReadNetworkConnection(),
                4 => reader.ReadVector3(),
                _ => throw new ArgumentException($"Unsupported type: {dataType}"),
            };
        }
    }
}