using FishNet.Managing.Scened;
using FishNet;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WrappedNetwork.SceneManagement
{
    public sealed partial class WrappedNetwork : NetworkBehaviour
    {
        public static void LoadScene(string sceneName)
        {
            if (!InstanceFinder.NetworkManager.IsServer)
                return;
            Scene scn = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            LoadScene(scn);
        }
        public static void LoadScene(int index)
        {
            if (!InstanceFinder.NetworkManager.IsServer)
                return;
            Scene scn = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(index);
            LoadScene(scn);
        }
        public static void LoadScene(Scene scene)
        {
            if (!InstanceFinder.NetworkManager.IsServer)
                return;
            if (scene == null)
            {
                Debug.LogError("Could not find scene");
                return;
            }
            SceneLoadData sld = new(scene) { ReplaceScenes = ReplaceOption.All };
            InstanceFinder.SceneManager.LoadGlobalScenes(sld);
        }
    }
}