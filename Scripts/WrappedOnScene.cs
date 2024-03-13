using FishNet;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

namespace WrappedNetwork.OnScene
{
    /// <summary>
    /// For using OnScene part you need to make GameObject with NetworkObject on it after you connect to the server
    /// </summary>
    public sealed partial class WrappedNetwork : NetworkBehaviour
    {
        public static WrappedNetwork Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(Instance.gameObject);
        }
        private void OnDestroy() => Instance = null;

        public static void SpawnObject(GameObject prefab, Vector3 pos = default, Vector3 rot = default)
        {
            if (Instance != null)
                Instance.InstantiateRPC(prefab, pos, rot);
            else
                Debug.LogError("Instance of the WrappedNetwork is not set - maybe you need to create GameObject with it");
        }
        [ServerRpc(RequireOwnership = false)]
        private void InstantiateRPC(GameObject obj, Vector3 pos, Vector3 rot, NetworkConnection owner = null)
        {
            GameObject go = Instantiate(obj, pos, Quaternion.Euler(rot));
            InstanceFinder.ServerManager.Spawn(go, owner);
        }
    }
}