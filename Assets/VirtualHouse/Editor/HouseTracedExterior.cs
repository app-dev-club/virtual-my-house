using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // Photo sequence 20260930_110940050 through 111155927: white plaster,
        // grey tiled gable, shallow lower roofs, red-brown fascia and silver sashes.
        // Dimensions not measurable in the photos remain estimates; plan coordinates
        // determine the footprint, including the open service passage.
        private static void CreateTracedExterior(Transform root)
        {
            Transform exterior=NewGroup("外観_写真ベース",root);
            Transform roofs=NewGroup("屋根と軒先",exterior);
            Transform trim=NewGroup("腰壁と窓",exterior);
            Material tile=GetMaterial("PhotoRoofTile");
            tile.color=new Color(0.31f,0.34f,0.35f);
            UnityEditor.EditorUtility.SetDirty(tile);

            // Main ridge follows the long axis of the traced second storey.
            Vector3 sw=PlanPoint(365,346,0,true), ne=PlanPoint(704,158,0,true);
            float cx=(sw.x+ne.x)/2, cz=(sw.z+ne.z)/2;
            float half=(ne.z-sw.z)/2+0.42f, length=ne.x-sw.x+0.8f;
            const float angle=23f, eave=5.58f;
            float rise=half*Mathf.Tan(angle*Mathf.Deg2Rad);
            foreach(int side in new[]{-1,1})
            {
                Vector3 center=new(cx,eave+rise/2,cz+side*half/2);
                Vector3 rotation=new(side*angle,0,0);
                CreateRotatedBox("主屋根_瓦面",center,new Vector3(length,0.12f,half/Mathf.Cos(angle*Mathf.Deg2Rad)),rotation,tile,roofs);
                CreateRotatedBox("主屋根_白い軒裏",center-Vector3.up*0.08f,
                    new Vector3(length-0.05f,0.055f,half/Mathf.Cos(angle*Mathf.Deg2Rad)),rotation,GetMaterial("Wall"),roofs);
                CreateBox("主屋根_軒樋",new Vector3(cx,eave-0.03f,cz+side*half),
                    new Vector3(length,0.11f,0.1f),GetMaterial("Gutter"),roofs);
                for(int row=1;row<10;row++)
                {
                    float distance=half*row/10;
                    CreateRotatedBox("瓦_横目地",new Vector3(cx,eave+rise-distance*Mathf.Tan(angle*Mathf.Deg2Rad)+0.075f,cz+side*distance),
                        new Vector3(length,0.025f,0.025f),rotation,GetMaterial("PhotoRoofTile"),roofs);
                }
                foreach(float x in new[]{sw.x-0.4f,ne.x+0.4f})
                    CreateRotatedBox("赤茶の破風",new Vector3(x,center.y-0.04f,center.z),
                        new Vector3(0.09f,0.14f,half/Mathf.Cos(angle*Mathf.Deg2Rad)),rotation,GetMaterial("Gutter"),roofs);
            }
            CreateBox("瓦棟",new Vector3(cx,eave+rise+0.09f,cz),new Vector3(length+0.06f,0.16f,0.20f),tile,roofs);
            foreach(float x in new[]{sw.x,ne.x})
                CreateGableWall(roofs,"白い妻壁",x,cz,ne.z-sw.z,5.7f,angle);
            foreach(float z in new[]{sw.z,ne.z})
                CreateBox("軒下白壁",new Vector3(cx,5.72f,z),new Vector3(ne.x-sw.x,0.20f,0.14f),GetMaterial("Wall"),roofs);

            // A ring of lower roofs, never a plane through the stairwell.
            PhotoShed(roofs,"西洋室下屋",128,280,312,454,3.22f,2.78f,true);
            PhotoShed(roofs,"西納戸下屋",223,205,312,292,3.22f,2.90f,true);
            PhotoShed(roofs,"北下屋",310,130,651,215,3.22f,2.83f,false,false);
            PhotoShed(roofs,"東下屋",651,167,852,435,3.22f,2.78f,true,false);
            PhotoShed(roofs,"東収納下屋",840,245,927,341,2.95f,2.76f,true,false);
            PhotoShed(roofs,"南廊下下屋",310,403,613,438,3.22f,2.83f,false);
            PhotoShed(roofs,"玄関下屋",613,403,700,493,3.22f,2.80f,false);

            // Only solid exterior strokes receive finishes. No strip crosses an opening.
            float[,] edges={
                {158,445,310,445},{310,427,613,427},{692,427,840,427},
                {136,311,136,423},{231,216,231,291},{310,175,613,175},
                {613,137,688,137},{688,175,840,175},{844,179,844,216},
                {844,329,844,423},{919,254,919,329},{840,333,915,333},
                {609,423,609,480},{692,423,692,480}
            };
            for(int i=0;i<edges.GetLength(0);i++)
            {
                Vector3 a=PlanPoint(edges[i,0],edges[i,1],0.6f),b=PlanPoint(edges[i,2],edges[i,3],0.6f);
                CreateBox("濃灰色の腰壁",(a+b)/2,new Vector3(Mathf.Max(0.05f,Mathf.Abs(a.x-b.x)),1.08f,
                    Mathf.Max(0.05f,Mathf.Abs(a.z-b.z))),GetMaterial("Cladding"),trim);
            }
            PhotoWindow(trim,"南西四枚サッシ",234,446,132,0.17f,2.02f,4);
            PhotoWindow(trim,"南廊下西サッシ",386,428,132,0.17f,2.02f,4);
            PhotoWindow(trim,"南廊下東サッシ",537,428,132,0.17f,2.02f,4);
            PhotoWindow(trim,"DK南腰窓",764,428,108,0.92f,1.05f,2);
            PhotoWindow(trim,"西洋室サッシ",135,367,94,0.17f,2.02f,4,false,true);
            PhotoWindow(trim,"東DK腰窓",845,376,65,0.9f,1.1f,2,false,true);
            PhotoWindow(trim,"東収納窓",920,291,49,1.15f,0.70f,2,false,true);
            PhotoWindow(trim,"北西腰窓",383,174,115,0.9f,1.10f,3);
            PhotoWindow(trim,"浴室小窓",764,174,48,1.28f,0.65f,2);
            PhotoWindow(trim,"二階南西窓",440,350,115,3.76f,1.36f,3,true);
            PhotoWindow(trim,"二階南東窓",610,350,115,3.76f,1.36f,3,true);
            PhotoWindow(trim,"二階北西窓",440,154,100,3.82f,1.25f,3,true);
            PhotoWindow(trim,"二階北東窓",610,154,100,3.82f,1.25f,3,true);
            PhotoWindow(trim,"二階西妻窓",361,252,70,3.8f,1.25f,2,true,true);
            PhotoWindow(trim,"二階東妻窓",708,252,70,3.8f,1.25f,2,true,true);

            // Level landing permits walking out and back without jumping the floor edge.
            PlanFloor(exterior,"屋外サービス踏み場",838,216,892,254,"Concrete");
            Vector3 porch=PlanPoint(650.5f,491,0);
            AddPorch(exterior,porch.x,porch.z);
        }

        private static void PhotoWindow(Transform parent,string name,float x,float v,float width,
            float bottom,float height,int columns,bool upper=false,bool side=false)
        {
            Vector3 p=PlanPoint(x,v,0,upper);
            if(side) WindowZ(parent,name,p.x/Grid,p.z/Grid,width*PlanScale/Grid,height,bottom,columns,false);
            else WindowX(parent,name,p.x/Grid,p.z/Grid,width*PlanScale/Grid,height,bottom,columns,false);
            // Shallow pale hoods observed on all photographed elevations.
            float outwards=side?(x>600?1:-1):(v<220?1:-1);
            Vector3 center=p+Vector3.up*(bottom+height+0.14f)+
                (side?Vector3.right:Vector3.forward)*outwards*0.17f;
            CreateBox("窓庇",center,side?new Vector3(0.40f,0.07f,width*PlanScale+0.26f):
                new Vector3(width*PlanScale+0.26f,0.07f,0.40f),GetMaterial("Awning"),parent);
        }

        private static void PhotoShed(Transform parent,string name,float l,float t,float r,float b,
            float high,float low,bool slopeX,bool highAtPositive=true)
        {
            Vector3 a=PlanPoint(l,b,0),c=PlanPoint(r,t,0);
            Transform group=NewGroup(name,parent);
            if(slopeX) ShedRoofZ(group,name,a.x/Grid,c.x/Grid,a.z/Grid,c.z/Grid,high,low,highAtPositive);
            else ShedRoofX(group,name,a.x/Grid,c.x/Grid,a.z/Grid,c.z/Grid,high,low,highAtPositive);
            Transform plane=group.GetChild(0);
            plane.GetComponent<Renderer>().sharedMaterial=GetMaterial("PhotoRoofTile");
            CreateRotatedBox("白い軒裏",plane.position-Vector3.up*0.085f,
                new Vector3(plane.localScale.x,0.06f,plane.localScale.z),plane.eulerAngles,GetMaterial("Wall"),group);
            float run=slopeX?c.x-a.x:c.z-a.z;
            float slope=(high-low)/run*(highAtPositive?1:-1);
            // Trim all four edges, matching each roof slope rather than floating gutters.
            for(int edge=0;edge<4;edge++)
            {
                Vector3 start=edge<2?new Vector3(a.x,0,edge==0?a.z:c.z):new Vector3(edge==2?a.x:c.x,0,a.z);
                Vector3 end=edge<2?new Vector3(c.x,0,start.z):new Vector3(start.x,0,c.z);
                start.y=(highAtPositive?low:high)+slope*((slopeX?start.x:start.z)-(slopeX?a.x:a.z))-0.02f;
                end.y=(highAtPositive?low:high)+slope*((slopeX?end.x:end.z)-(slopeX?a.x:a.z))-0.02f;
                GameObject beam=CreateBox("赤茶の軒先",(start+end)/2,new Vector3(0.085f,0.12f,Vector3.Distance(start,end)),GetMaterial("Gutter"),group);
                beam.transform.rotation=Quaternion.LookRotation(end-start);
            }
            CreateRoofInfill(group,name,a,c,high,low,slopeX,highAtPositive);
        }

        private static void CreateRoofInfill(Transform parent,string name,Vector3 a,Vector3 c,
            float high,float low,bool slopeX,bool highAtPositive)
        {
            // Close the space above the 2.7m walls with sloping white fascia walls.
            // These surfaces start above standing head height and do not add passage colliders.
            const string folder="Assets/VirtualHouse/GeneratedMeshes";
            if(!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets/VirtualHouse","GeneratedMeshes");
            string path=folder+"/"+name+".asset";
            Mesh mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null) { mesh=new Mesh(); UnityEditor.AssetDatabase.CreateAsset(mesh,path); }
            mesh.Clear();
            var vertices=new System.Collections.Generic.List<Vector3>();
            var triangles=new System.Collections.Generic.List<int>();
            float inset=0.25f;
            Vector3[] corners={new(a.x+inset,0,a.z+inset),new(c.x-inset,0,a.z+inset),
                new(c.x-inset,0,c.z-inset),new(a.x+inset,0,c.z-inset)};
            float run=slopeX?c.x-a.x:c.z-a.z;
            for(int i=0;i<4;i++)
            {
                Vector3 start=corners[i],end=corners[(i+1)%4];
                float u=((slopeX?start.x:start.z)-(slopeX?a.x:a.z))/run;
                float v=((slopeX?end.x:end.z)-(slopeX?a.x:a.z))/run;
                float y1=Mathf.Lerp(low,high,highAtPositive?u:1-u)-0.10f;
                float y2=Mathf.Lerp(low,high,highAtPositive?v:1-v)-0.10f;
                Vector3[] quad={start+Vector3.up*2.68f,end+Vector3.up*2.68f,
                    end+Vector3.up*y2,start+Vector3.up*y1};
                for(int side=0;side<2;side++)
                {
                    int k=vertices.Count;
                    vertices.AddRange(quad);
                    triangles.AddRange(side==0?new[]{k,k+1,k+2,k,k+2,k+3}:new[]{k+2,k+1,k,k+3,k+2,k});
                }
            }
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            UnityEditor.EditorUtility.SetDirty(mesh);
            GameObject infill=new("下屋の白い小壁");
            infill.transform.SetParent(parent,false);
            infill.AddComponent<MeshFilter>().sharedMesh=mesh;
            infill.AddComponent<MeshRenderer>().sharedMaterial=GetMaterial("Wall");
        }
    }
}
