using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public sealed class EquippedSkillHud : MonoBehaviour
    {
        MainScreen main;
        readonly Button[] slots=new Button[3];
        readonly Image[] icons=new Image[3], frames=new Image[3], cooldowns=new Image[3];
        readonly Text[] labels=new Text[3];
        readonly CollectionEntry[] shown=new CollectionEntry[3];

        public void Initialize(MainScreen owner,Sprite circle)
        {
            main=owner;
            var ring=Resources.Load<Sprite>("Moonlit/Skills/SkillRing-v1");
            for(int i=0;i<3;i++) {
                int slot=i;
                var button=Ui.ArtButton("Equipped battle skill "+i,transform,i*126,0,116,128);
                slots[i]=button;
                icons[i]=Ui.Image("Skill icon",button.transform,12,10,92,92,null);
                // The sprite is assigned before the first render; empty slots are hidden.
                icons[i].enabled=true;icons[i].preserveAspect=true;
                cooldowns[i]=Ui.Image("Turn cooldown shade",button.transform,8,6,100,100,circle,new Color(0,0,0,.68f));
                cooldowns[i].type=Image.Type.Filled;cooldowns[i].fillMethod=Image.FillMethod.Radial360;
                cooldowns[i].fillOrigin=2;cooldowns[i].fillClockwise=true;
                frames[i]=Ui.Image("Skill frame",button.transform,0,0,116,116,ring);
                frames[i].preserveAspect=true;
                labels[i]=Ui.Text("Turns until skill",button.transform,0,89,116,38,"",25,main.font,Color.white);
                labels[i].horizontalOverflow=HorizontalWrapMode.Overflow;
                button.onClick.AddListener(()=>{if(shown[slot]!=null)main.screens.Open("skill-details",shown[slot]);});
            }
            Refresh();
        }
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!main)return;
            var entries=CollectionProgression.EquippedSkills;
            var battle=main.GetComponent<BattleRuntime>();
            for(int i=0;i<3;i++) {
                var entry=i<entries.Count?entries[i]:null;shown[i]=entry;
                slots[i].gameObject.SetActive(entry!=null);
                if(entry==null)continue;
                icons[i].sprite=PrimitiveSkillEffects.SkillSprite(entry.grade,entry.variant);
                frames[i].color=EquipmentRules.TierColor(entry.grade);
                int remaining=entry.Cooldown;
                if(battle && battle.PlayerState!=null) {
                    int actual=battle.PlayerState.skills.FindIndex(skill=>skill.tier==entry.grade && skill.variant==entry.variant);
                    if(actual>=0)remaining=battle.PlayerSkillTurnsUntilReady(actual);
                }
                cooldowns[i].fillAmount=Mathf.Clamp01(remaining/(float)entry.Cooldown);
                labels[i].text=remaining==0?"발동":remaining+"턴";
            }
        }
    }
}
