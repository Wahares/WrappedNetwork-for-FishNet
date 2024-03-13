using FishNet.Connection;
using FishNet;
using FishNet.Object;

namespace WrappedNetwork
{
    public sealed partial class WrappedNetwork : NetworkBehaviour
    {
        public static bool IsMasterClient => InstanceFinder.NetworkManager.IsServer;
        public static new NetworkConnection LocalConnection => InstanceFinder.ClientManager.Connection;
        public static int LocalID => InstanceFinder.ClientManager.Connection.ClientId;
    }
}

