using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // Source pixels from the supplied 960 x 540 images. The 75px scale bar is 1.82m.
        // Filenames are reversed: use the floor number printed inside each image.
        private const float PlanScale = 1.82f / 75f;
        private static Vector3 PlanPoint(float x, float v, float y, bool upper = false)
            => new((x - (upper ? 211f : 158f)) * PlanScale, y,
                ((upper ? 366f : 423f) - v) * PlanScale);

        private static void PlanFloor(Transform parent, string name, float l, float t, float r, float b,
            string material, bool upper = false)
        {
            float y = upper ? SecondFloorY : 0f;
            CreateBox(name, PlanPoint((l+r)/2, (t+b)/2, y-FloorThickness/2, upper),
                new Vector3((r-l)*PlanScale, FloorThickness, (b-t)*PlanScale), GetMaterial(material), parent);
            Vector3 labelPosition=PlanPoint((l+r)/2,(t+b)/2,y+0.08f,upper);
            CreateLabel(parent,name,labelPosition.x/Grid,labelPosition.z/Grid,labelPosition.y);
            TextMesh label=parent.GetChild(parent.childCount-1).GetComponent<TextMesh>();
            label.characterSize=Mathf.Min(0.06f,(r-l)*PlanScale/(name.Length*10f));
        }

        private static void PlanWall(Transform parent, float x1, float v1, float x2, float v2, bool upper = false)
        {
            Vector3 a = PlanPoint(x1,v1,0,upper), b = PlanPoint(x2,v2,0,upper);
            CreateBox($"壁_{x1}_{v1}_{x2}_{v2}", (a+b)/2 + Vector3.up*((upper ? SecondFloorY : 0)+WallHeight/2),
                new Vector3(Mathf.Max(Mathf.Abs(a.x-b.x),WallThickness),WallHeight,
                    Mathf.Max(Mathf.Abs(a.z-b.z),WallThickness)),GetMaterial("Wall"),parent);
        }

        private static void PlanWalls(Transform parent, float[,] segments, bool upper = false)
        {
            for(int i=0;i<segments.GetLength(0);i++)
                PlanWall(parent,segments[i,0],segments[i,1],segments[i,2],segments[i,3],upper);
        }

        private static void CreateTracedHouse(Transform root)
        {
            Transform first = NewGroup("1階",root), second = NewGroup("2階",root);
            Transform f = NewGroup("床_部屋別",first), w = NewGroup("壁",first);
            PlanFloor(f,"8帖洋室",158,291,310,442,"FloorWood");
            PlanFloor(f,"西側張出し",140,311,158,423,"FloorWood");
            PlanFloor(f,"納戸",235,216,310,291,"Storage");
            PlanFloor(f,"8帖和室_西",310,216,462,367,"Tatami");
            PlanFloor(f,"8帖和室_東",462,216,613,367,"Tatami");
            PlanFloor(f,"北収納_西",310,179,386,216,"Storage");
            PlanFloor(f,"北収納_中央",386,179,462,216,"Storage");
            PlanFloor(f,"北収納_東",462,179,537,216,"Storage");
            PlanFloor(f,"北ホール",537,179,688,216,"Hall");
            PlanFloor(f,"階段北物入",613,216,651,255,"Storage");
            PlanFloor(f,"南廊下",310,367,651,423,"Hall");
            PlanFloor(f,"縦廊下",651,216,688,423,"Hall");
            PlanFloor(f,"玄関",613,423,688,480,"GenkanGreen");
            PlanFloor(f,"WC西",613,141,651,179,"Wet");
            PlanFloor(f,"WC東",651,141,688,179,"Wet");
            PlanFloor(f,"洗面",688,179,726,216,"Wet");
            PlanFloor(f,"水回り物入",688,216,726,254,"Storage");
            PlanFloor(f,"脱衣",688,254,764,291,"Wet");
            PlanFloor(f,"浴室",726,179,802,254,"Bath");
            PlanFloor(f,"給湯",802,179,840,254,"Utility");
            PlanFloor(f,"8帖DK",688,291,840,423,"FloorWood");
            PlanFloor(f,"DK北接続",764,254,840,291,"FloorWood");
            PlanFloor(f,"東物入",840,254,915,329,"Storage");
            // Every segment below is a thick stroke. Dotted runs have no wall or fitting collider.
            PlanWalls(w,new float[,] {
                {158,291,310,291},{158,291,158,311},{140,311,158,311},{140,311,140,423},
                {140,423,158,423},{158,423,158,442},{158,442,310,442},{310,405,310,442},
                {310,291,310,367},{310,367,348,367},{422,367,504,367},{578,367,613,367},
                {310,423,613,423},{613,405,613,480},{688,367,688,480},{688,423,840,423},
                {840,329,840,423},{840,329,915,329},{915,254,915,329},{840,254,915,254},
                {840,254,840,291},{764,254,802,254},{764,254,764,291},
                {688,291,764,291},{688,254,726,254},{688,216,726,216},{726,179,726,254},
                {802,179,802,254},{840,179,840,216},{688,179,840,179},
                {688,141,688,179},{613,141,688,141},{613,141,613,179},{651,141,651,179},
                {310,179,613,179},{310,179,310,216},{235,216,310,216},{235,216,235,291},
                {386,179,386,216},{462,179,462,250},{462,326,462,367},
                {537,179,537,216},{537,216,574,216},{613,216,651,216},{651,216,651,367},
                {613,255,651,255},{613,255,613,367}
            });

            f = NewGroup("床_部屋別",second); w = NewGroup("壁",second);
            PlanFloor(f,"8帖和室",365,158,515,310,"Tatami",true);
            PlanFloor(f,"北廊下_踊り場",515,158,704,194,"Hall",true);
            PlanFloor(f,"4.5帖和室",553,194,667,310,"Tatami",true);
            PlanFloor(f,"中央物入_北",515,194,553,275,"Storage",true);
            PlanFloor(f,"中央物入_南",515,275,553,310,"Storage",true);
            PlanFloor(f,"南廊下",402,310,667,346,"Hall",true);
            PlanFloor(f,"南西物入",365,310,402,346,"Storage",true);
            PlanFloor(f,"南東物入",667,310,704,346,"Storage",true);
            PlanWalls(w,new float[,] {
                {365,158,704,158},{365,158,365,346},{365,346,704,346},{704,158,704,346},
                {365,310,402,310},{515,310,591,310},{667,310,704,310},
                {667,194,667,310},{515,194,591,194},{553,199,553,275},
                {515,275,553,275},{515,275,515,310}
            },true);

            // Stair shaft aligned between the two images (53px horizontal, -57px vertical).
            Vector3 foot=PlanPoint(613,367,0);
            CreateStairs(first,foot.x/Grid,foot.z/Grid,38*PlanScale/Grid,112*PlanScale/Grid,0);
            CreateTracedExterior(root);
            Physics.SyncTransforms();
            ValidateTracedOpenings(false,new float[,] {
                {348,367,422,367},{504,367,578,367},{310,367,310,405},
                {310,216,310,291},{462,250,462,326},{574,216,613,216},
                {613,179,651,179},{651,179,688,179},{688,179,688,216},
                {688,216,688,254},{688,291,688,367},{726,254,764,254},
                {802,254,840,254},{840,216,840,254},{840,291,840,329},{613,423,688,423},{613,480,688,480}
            });
            ValidateTracedOpenings(true,new float[,] {
                {402,310,515,310},{591,310,667,310},{515,158,515,194},
                {515,194,515,275},{553,275,553,310},{591,194,667,194},{667,194,704,194},
                {402,310,402,346},{667,310,667,346}
            });
        }

        private static void ValidateTracedOpenings(bool upper,float[,] openings)
        {
            for(int i=0;i<openings.GetLength(0);i++)
            {
                Vector3 a=PlanPoint(openings[i,0],openings[i,1],0,upper);
                Vector3 b=PlanPoint(openings[i,2],openings[i,3],0,upper);
                Vector3 center=(a+b)/2+Vector3.up*((upper?SecondFloorY:0)+1.2f);
                Vector3 half=new(Mathf.Max(Mathf.Abs(a.x-b.x)/2-0.1f,0.025f),0.8f,
                    Mathf.Max(Mathf.Abs(a.z-b.z)/2-0.1f,0.025f));
                foreach(Collider hit in Physics.OverlapBox(center,half))
                    if(hit.name.StartsWith("壁_"))
                        throw new System.InvalidOperationException($"Opening {i} upper={upper} blocked by {hit.name}");
            }
            Debug.Log($"Traced opening validation passed: floor {(upper?2:1)}, {openings.GetLength(0)} openings");
        }

        public static void ValidateStairWalk()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(OutputScene);
            CharacterController player=GameObject.Find("Player").GetComponent<CharacterController>();
            Vector3 foot=PlanPoint(632,367,0);
            float run=112*PlanScale;
            foreach(bool descend in new[]{true,false})
            {
                player.enabled=false;
                player.transform.position=foot+new Vector3(0,descend?SecondFloorY+0.03f:0.03f,descend?run+0.45f:-0.55f);
                player.enabled=true;
                Physics.SyncTransforms();
                float target=foot.z+(descend?-0.55f:run+0.45f), vy=0;
                for(int i=0;i<360;i++)
                {
                    if(player.isGrounded && vy<0) vy=-2;
                    vy-=20f/60;
                    float dz=Mathf.Clamp(target-player.transform.position.z,-2.6f/60,2.6f/60);
                    player.Move(new Vector3(0,vy/60,dz));
                }
                Vector3 end=player.transform.position;
                Debug.Log($"Stair walk descend={descend}: end={end}, targetZ={target}");
                if(Mathf.Abs(end.z-target)>0.1f || Mathf.Abs(end.y-(descend?0:SecondFloorY))>0.15f)
                    throw new System.InvalidOperationException($"Stair walk failed descend={descend} at {end}");
            }
        }

        public static void RebuildAndValidateStairs()
        {
            GenerateAndRenderPreviews();
            ValidateStairWalk();
            ValidateServiceWalk();
        }

        public static void ValidateServiceWalk()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(OutputScene);
            CharacterController player=GameObject.Find("Player").GetComponent<CharacterController>();
            Vector3[] route={PlanPoint(821,310,0),PlanPoint(821,235,0),PlanPoint(875,235,0),PlanPoint(930,235,-0.09f)};
            foreach(bool returning in new[]{false,true})
            {
                player.enabled=false;
                player.transform.position=route[returning?route.Length-1:0]+Vector3.up*0.03f;
                player.enabled=true;
                Physics.SyncTransforms();
                float vy=0;
                for(int leg=1;leg<route.Length;leg++)
                {
                    Vector3 target=route[returning?route.Length-1-leg:leg];
                    for(int frame=0;frame<240;frame++)
                    {
                        if(player.isGrounded && vy<0) vy=-2;
                        vy-=20f/60;
                        Vector3 delta=target-player.transform.position;
                        delta.y=0;
                        delta=Vector3.ClampMagnitude(delta,2.6f/60);
                        player.Move(delta+Vector3.up*(vy/60));
                    }
                    Vector3 end=player.transform.position;
                    if(Vector2.Distance(new Vector2(end.x,end.z),new Vector2(target.x,target.z))>0.1f ||
                        Mathf.Abs(end.y-target.y)>0.18f)
                        throw new System.InvalidOperationException($"Service walk failed returning={returning}, leg={leg}, end={end}");
                }
                Debug.Log($"Service walk passed: returning={returning}, end={player.transform.position}");
            }
        }
    }
}
