using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public sealed class Atmosphere : MonoBehaviour
    {
        public RectTransform[] motes;
        public Image[] lights;
        Vector2[] origins;
        void Awake() { origins = new Vector2[motes.Length]; for(int i=0;i<motes.Length;i++) origins[i]=motes[i].anchoredPosition; }
        void Update()
        {
            for(int i=0;i<motes.Length;i++)
            {
                float time=Time.unscaledTime;
                motes[i].anchoredPosition=origins[i]+new Vector2(Mathf.Sin(time*.45f+i*2)*12,Mathf.Repeat(time*(8+i%5)+i*13,150));
                var c=lights[i].color; c.a=(.3f+.7f*Mathf.Pow(Mathf.Sin(time*.8f+i),2))*.65f; lights[i].color=c;
            }
        }
    }
}
