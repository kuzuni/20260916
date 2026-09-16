using UnityEngine;

namespace Moonlit.UI
{
    /// <summary>The only authored scene object. All visible UI is created when the scene runs.</summary>
    public sealed class MainScreenBootstrap : MonoBehaviour
    {
        public MainScreenAssets assets;
        public MainScreen Screen { get; private set; }
        void Awake() { Build(); }
        public MainScreen Build()
        {
            if(Screen) return Screen;
            if(!Application.isPlaying) return null;
            if(!assets) { Debug.LogError("MainScreenBootstrap requires a MainScreenAssets asset.",this); return null; }
            var root=new GameObject("Moonlit UI — runtime only");
            root.transform.SetParent(transform,false); root.SetActive(false);
            Screen=new RuntimeMainScreenFactory(assets).Create(root.transform);
            root.SetActive(true);
            return Screen;
        }
    }
}
