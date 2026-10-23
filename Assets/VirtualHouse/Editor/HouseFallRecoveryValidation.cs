using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static class HouseFallRecoveryValidation
    {
        public static void Run()
        {
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
            Type type=typeof(HouseFirstPersonController);
            foreach(string name in new[]{"HouseBlockout","HouseRenovated","HouseRenovatedAlternative","HouseRenovatedPlanC"})
            {
                EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var player=UnityEngine.Object.FindFirstObjectByType<HouseFirstPersonController>();
                type.GetMethod("Awake",flags).Invoke(player,null);
                var controller=player.GetComponent<CharacterController>();
                var entry=GameObject.Find("住宅概形_図面ベース/1階/床_部屋別/玄関").GetComponent<Collider>().bounds;
                Vector3 expected=new Vector3(entry.center.x,entry.max.y+.05f,entry.center.z);
                MethodInfo recover=type.GetMethod("RecoverIfFallen",flags);
                void MoveTo(Vector3 position)
                {
                    controller.enabled=false;player.transform.position=position;controller.enabled=true;Physics.SyncTransforms();
                }
                foreach(float height in new[]{3f,0f,-1f})
                {
                    Vector3 position=expected+Vector3.up*height;
                    MoveTo(position);
                    if((bool)recover.Invoke(player,null) || Vector3.Distance(player.transform.position,position)>.001f)
                        throw new InvalidOperationException("Recovery triggered above fall threshold: "+name);
                }
                for(int attempt=0;attempt<2;attempt++)
                {
                    MoveTo(new Vector3(100,-20-attempt*20,100));
                    type.GetField("verticalVelocity",flags).SetValue(player,-60f);
                    type.GetField("pitch",flags).SetValue(player,35f);
                    type.GetField("touchMovement",flags).SetValue(player,Vector2.one);
                    if(!(bool)recover.Invoke(player,null) || Vector3.Distance(player.transform.position,expected)>.001f || !controller.enabled
                        || (float)type.GetField("verticalVelocity",flags).GetValue(player)!=0 || player.ViewPitch!=0
                        || (Vector2)type.GetField("touchMovement",flags).GetValue(player)!=Vector2.zero)
                        throw new InvalidOperationException("Fall recovery did not reset player state: "+name);
                    for(int frame=0;frame<60;frame++)controller.Move(Vector3.down*(2f/60));
                    if(Mathf.Abs(player.transform.position.y-entry.max.y)>.08f || !controller.isGrounded)
                        throw new InvalidOperationException("Recovered player is not supported by entrance floor: "+name);
                    Vector3 start=player.transform.position;
                    for(int frame=0;frame<12;frame++)controller.Move(new Vector3(0,-2f/60,.026f));
                    if(player.transform.position.z-start.z<.20f)
                        throw new InvalidOperationException("Recovered player cannot move: "+name);
                }
                Debug.Log("FALL RECOVERY VALIDATED: "+name+"; normal heights, repeated falls, entry support, velocity / look / touch reset and movement.");
            }
        }
    }
}
