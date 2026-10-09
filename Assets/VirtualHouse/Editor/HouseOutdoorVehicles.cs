using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // Editable, procedural vehicle models; no third-party model dependencies.
        private static GameObject VehicleMesh(Transform parent,string name,List<Vector3> vertices,List<int> triangles,Material material)
        {
            string path="Assets/VirtualHouse/GeneratedMeshes/Vehicle_"+parent.name+"_"+name+".asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            GameObject obj=new(name);obj.transform.SetParent(parent,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            return obj;
        }
        private static void VehiclePanel(Transform parent,string name,Material material,params Vector3[] points)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            // Separate vertices for reverse faces: shared opposite faces cancel their normals.
            for(int i=1;i<points.Length-1;i++)
            {
                int k=vertices.Count;
                vertices.AddRange(new[]{points[0],points[i],points[i+1],points[0],points[i+1],points[i]});
                triangles.AddRange(new[]{k,k+1,k+2,k+3,k+4,k+5});
            }
            VehicleMesh(parent,name,vertices,triangles,material);
        }
        private static void VehicleRing(Transform parent,string name,Vector3 center,float radius,float tube,float depth,Material material,float start=0,float sweep=360)
        {
            var vertices=new List<Vector3>();var indices=new List<int>();
            const int segments=64,sides=12;
            for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++)
            {
                float a=(start+sweep*i/segments)*Mathf.Deg2Rad,b=j*Mathf.PI*2/sides;
                float r=radius+Mathf.Cos(b)*tube;
                vertices.Add(center+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*depth));
            }
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {int k=i*(sides+1)+j;indices.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}
            VehicleMesh(parent,name,vertices,indices,material);
        }
        private static void CreateDetailedOutdoorVehicles(Transform root)
        {
            Material black=OutdoorMat("Black",.035f,.04f,.045f),rubber=OutdoorMat("TireRubber",.022f,.023f,.024f);
            Material grey=OutdoorMat("SientaGrey",.40f,.43f,.45f),glass=OutdoorMat("VehicleGlass",.075f,.15f,.18f);
            Material steel=OutdoorMat("Steel",.60f,.63f,.65f),chrome=OutdoorMat("Chrome",.77f,.80f,.83f);
            Material lens=OutdoorMat("HeadlampLens",.82f,.91f,.95f),red=OutdoorMat("TailRed",.7f,.04f,.025f);
            Material amber=OutdoorMat("IndicatorAmber",.95f,.38f,.03f),white=OutdoorMat("Ivory",.91f,.91f,.84f);
            foreach(Material m in new[]{steel,chrome,grey}){m.SetFloat("_Metallic",m==grey?.45f:.85f);m.SetFloat("_Smoothness",.7f);EditorUtility.SetDirty(m);}
            glass.SetFloat("_Smoothness",.92f);rubber.SetFloat("_Smoothness",.12f);
            Transform car=OutdoorGroup(root,"グレーのシエンタ_概形",791,292);
            // Longitudinal sections form a continuous rounded minivan body and sloped windscreen.
            float[] xs={-2.11f,-2.02f,-1.80f,.65f,1.19f,1.61f,2.04f,2.13f};
            float[] tops={1.34f,1.56f,1.69f,1.69f,1.52f,1.04f,1.00f,.87f};
            float[] widths={.69f,.79f,.83f,.83f,.83f,.81f,.76f,.66f};
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<xs.Length;i++)
            {
                float x=xs[i],h=tops[i],w=widths[i];
                vertices.AddRange(new[]{new Vector3(x,.64f,-w*.91f),new Vector3(x,.73f,-w),new Vector3(x,h-.13f,-w*.95f),new Vector3(x,h,-w*.79f),new Vector3(x,h,w*.79f),new Vector3(x,h-.13f,w*.95f),new Vector3(x,.73f,w),new Vector3(x,.64f,w*.91f)});
            }
            for(int i=0;i<xs.Length-1;i++)for(int j=0;j<8;j++)
            {int a=i*8+j,b=i*8+(j+1)%8,c=(i+1)*8+j,d=(i+1)*8+(j+1)%8;indices.AddRange(new[]{a,b,c,b,d,c});}
            for(int j=1;j<7;j++){indices.AddRange(new[]{0,j+1,j});int k=(xs.Length-1)*8;indices.AddRange(new[]{k,k+j,k+j+1});}
            GameObject body=VehicleMesh(car,"曲面ボディ",vertices,indices,grey);
            BoxCollider bodyCollider=body.AddComponent<BoxCollider>();bodyCollider.center=new Vector3(0,1.12f,0);bodyCollider.size=new Vector3(4.22f,.98f,1.64f);
            // Dark window surround and individual pillars remain flush with the shaped body.
            foreach(int side in new[]{-1,1})
            {
                float z=side*.842f;
                VehiclePanel(car,"窓周囲_"+side,black,new Vector3(-1.96f,.99f,z),new Vector3(1.55f,.99f,z),new Vector3(1.10f,1.48f,z*.972f),new Vector3(.62f,1.59f,z*.974f),new Vector3(-1.78f,1.59f,z*.974f));
                VehiclePanel(car,"後席ガラス_"+side,glass,new Vector3(-1.84f,1.05f,z*1.006f),new Vector3(-.99f,1.05f,z*1.006f),new Vector3(-.99f,1.53f,z*.979f),new Vector3(-1.73f,1.53f,z*.979f));
                VehiclePanel(car,"スライドドアガラス_"+side,glass,new Vector3(-.89f,1.05f,z*1.006f),new Vector3(.23f,1.05f,z*1.006f),new Vector3(.23f,1.53f,z*.979f),new Vector3(-.89f,1.53f,z*.979f));
                VehiclePanel(car,"運転席ガラス_"+side,glass,new Vector3(.34f,1.05f,z*1.006f),new Vector3(1.37f,1.05f,z*1.006f),new Vector3(1.04f,1.45f,z*.979f),new Vector3(.59f,1.53f,z*.979f),new Vector3(.34f,1.53f,z*.979f));
                foreach(float x in new[]{-1.34f,1.34f})
                {
                    string suffix=x+"_"+side;
                    Vector3 wheel=new Vector3(x,.32f,side*.76f);
                    VehicleRing(car,"タイヤ_"+suffix,wheel,.245f,.072f,.10f,rubber);
                    VehicleRing(car,"リム_"+suffix,wheel+new Vector3(0,0,side*.103f),.173f,.012f,.008f,steel);
                    OutdoorRound(car,"ホイール奥",wheel+new Vector3(0,0,side*.07f),new Vector3(.32f,.32f,.025f),black);
                    OutdoorRound(car,"ハブ",wheel+new Vector3(0,0,side*.12f),new Vector3(.10f,.10f,.03f),chrome);
                    for(int spoke=0;spoke<10;spoke++)
                    {
                        float a=spoke*Mathf.PI/5;Vector3 d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                        OutdoorRod(car,"アルミスポーク",wheel+d*.04f+new Vector3(0,0,side*.112f),wheel+d*.165f+new Vector3(0,0,side*.112f),.022f,steel);
                    }
                    VehicleRing(car,"ホイールアーチ_"+suffix,new Vector3(x,.32f,side*.824f),.345f,.028f,.027f,black,0,180);
                }
                foreach(float x in new[]{-1.95f,0,1.97f})OutdoorBox(car,"下部パネル",new Vector3(x,.53f,side*.76f),new Vector3(x==0?1.87f:.29f,.24f,.12f),grey);
                OutdoorBox(car,"サイドシル",new Vector3(0,.39f,side*.79f),new Vector3(1.87f,.09f,.10f),black);
                foreach(float x in new[]{-.94f,.28f,1.54f}) OutdoorRod(car,"ドアの継ぎ目",new Vector3(x,.71f,side*.833f),new Vector3(x,.99f,side*.833f),.009f,black);
                OutdoorRod(car,"スライドレール",new Vector3(-1.83f,.96f,side*.839f),new Vector3(-.20f,.96f,side*.839f),.012f,black);
                foreach(float x in new[]{-.30f,.48f})OutdoorRound(car,"ドアハンドル",new Vector3(x,.93f,side*.849f),new Vector3(.18f,.045f,.036f),chrome);
                OutdoorRod(car,"ミラー支柱",new Vector3(1.10f,1.04f,side*.78f),new Vector3(1.13f,1.08f,side*.96f),.035f,black);
                OutdoorRound(car,"ドアミラー",new Vector3(1.13f,1.10f,side*.98f),new Vector3(.24f,.14f,.16f),grey);
                OutdoorRound(car,"ミラー面",new Vector3(1.02f,1.10f,side*.98f),new Vector3(.014f,.095f,.12f),chrome);
                OutdoorRound(car,"ヘッドライト縁",new Vector3(2.071f,.89f,side*.53f),new Vector3(.14f,.23f,.35f),black);
                OutdoorRound(car,"LEDランプ",new Vector3(2.137f,.91f,side*.53f),new Vector3(.018f,.105f,.26f),lens);
                OutdoorBox(car,"縦型テールランプ",new Vector3(-2.084f,1.11f,side*.68f),new Vector3(.035f,.48f,.11f),red);
                OutdoorBox(car,"バンパー縦モール",new Vector3(2.105f,.65f,side*.62f),new Vector3(.025f,.30f,.10f),black);
            }
            VehiclePanel(car,"傾斜フロントガラス",glass,new Vector3(1.25f,1.48f,-.62f),new Vector3(1.25f,1.48f,.62f),new Vector3(1.61f,1.075f,.71f),new Vector3(1.61f,1.075f,-.71f));
            VehiclePanel(car,"リアガラス",glass,new Vector3(-2.047f,1.49f,-.58f),new Vector3(-2.047f,1.49f,.58f),new Vector3(-2.125f,1.09f,.64f),new Vector3(-2.125f,1.09f,-.64f));
            foreach(float z in new[]{-.36f,.25f})OutdoorRod(car,"ワイパー",new Vector3(1.60f,1.09f,z-.17f),new Vector3(1.46f,1.25f,z+.16f),.014f,black);
            OutdoorRound(car,"フロントバンパー",new Vector3(2.03f,.48f,0),new Vector3(.25f,.34f,1.52f),grey);
            OutdoorBox(car,"黒いグリル",new Vector3(2.158f,.57f,0),new Vector3(.024f,.24f,.86f),black);
            for(int i=0;i<5;i++)OutdoorRod(car,"グリル横桟",new Vector3(2.173f,.48f+i*.041f,-.40f),new Vector3(2.173f,.48f+i*.041f,.40f),.009f,steel);
            OutdoorBox(car,"ナンバープレート",new Vector3(2.19f,.55f,0),new Vector3(.012f,.16f,.32f),white);
            OutdoorBox(car,"後部ナンバー",new Vector3(-2.125f,.83f,0),new Vector3(.012f,.16f,.32f),white);

            Transform bike=OutdoorGroup(root,"黒のST250風バイク",755,214);
            foreach(float x in new[]{-.68f,.68f})
            {
                Vector3 hub=new Vector3(x,.32f,0);
                VehicleRing(bike,"タイヤ_"+x,hub,.275f,.044f,.063f,rubber);
                VehicleRing(bike,"クロームリム_"+x,hub,.227f,.013f,.027f,chrome);
                VehicleRing(bike,"フェンダー_"+x,hub,.343f,.015f,.065f,x<0?chrome:black,18,145);
                OutdoorRod(bike,"車軸ハブ",hub-new Vector3(0,0,.075f),hub+new Vector3(0,0,.075f),.10f,steel);
                for(int i=0;i<32;i++)foreach(float side in new[]{-1f,1f})
                {
                    float a=i*Mathf.PI/16,b=a+.34f;
                    OutdoorRod(bike,"交差スポーク",hub+new Vector3(Mathf.Cos(b)*.045f,Mathf.Sin(b)*.045f,side*.053f),hub+new Vector3(Mathf.Cos(a)*.221f,Mathf.Sin(a)*.221f,side*.012f),.006f,chrome);
                }
                VehicleRing(bike,"ブレーキディスク_"+x,hub+new Vector3(0,0,-.06f),.115f,.018f,.005f,steel);
            }
            foreach(float z in new[]{-.10f,.10f})
            {
                OutdoorRod(bike,"上フレーム",new Vector3(-.65f,.71f,z),new Vector3(.39f,.86f,z),.036f,black);
                OutdoorRod(bike,"ダウンチューブ",new Vector3(.39f,.86f,z),new Vector3(.25f,.24f,z),.032f,black);
                OutdoorRod(bike,"下フレーム",new Vector3(.25f,.24f,z),new Vector3(-.34f,.24f,z),.032f,black);
                OutdoorRod(bike,"後フレーム",new Vector3(-.34f,.24f,z),new Vector3(-.56f,.73f,z),.032f,black);
                OutdoorRod(bike,"スイングアーム",new Vector3(-.68f,.32f,z),new Vector3(-.19f,.36f,z),.035f,steel);
                OutdoorRod(bike,"フロントフォーク",new Vector3(.68f,.32f,z),new Vector3(.40f,1.02f,z),.035f,chrome);
                OutdoorRod(bike,"フォーク下部",new Vector3(.68f,.32f,z),new Vector3(.55f,.64f,z),.048f,steel);
                OutdoorRod(bike,"リアショック",new Vector3(-.65f,.33f,z*1.5f),new Vector3(-.45f,.72f,z*1.5f),.046f,chrome);
                for(int i=0;i<10;i++)OutdoorRound(bike,"サスペンションコイル",Vector3.Lerp(new Vector3(-.63f,.37f,z*1.5f),new Vector3(-.46f,.70f,z*1.5f),i/9f),new Vector3(.07f,.02f,.075f),black);
            }
            OutdoorRound(bike,"ティアドロップ燃料タンク",new Vector3(.09f,.81f,0),new Vector3(.64f,.35f,.38f),black);
            OutdoorRound(bike,"タンクキャップ",new Vector3(.13f,.986f,0),new Vector3(.073f,.018f,.073f),chrome);
            OutdoorRound(bike,"ダブルシート",new Vector3(-.45f,.81f,0),new Vector3(.80f,.13f,.34f),black);
            OutdoorRod(bike,"シートのベルト",new Vector3(-.47f,.867f,-.13f),new Vector3(-.47f,.867f,.13f),.021f,rubber);
            OutdoorRound(bike,"クランクケース",new Vector3(-.07f,.36f,0),new Vector3(.34f,.27f,.36f),steel);
            OutdoorBox(bike,"シリンダー",new Vector3(.11f,.55f,0),new Vector3(.19f,.28f,.23f),black);
            for(int i=0;i<9;i++)OutdoorBox(bike,"冷却フィン",new Vector3(.11f,.44f+i*.028f,0),new Vector3(.25f,.012f,.28f),steel);
            foreach(float z in new[]{-.18f,.18f})OutdoorRound(bike,"サイドカバー",new Vector3(-.32f,.61f,z),new Vector3(.32f,.26f,.055f),black);
            Vector3[] exhaust={new Vector3(.24f,.60f,-.1f),new Vector3(.38f,.53f,-.14f),new Vector3(.38f,.27f,-.18f),new Vector3(.17f,.22f,-.20f),new Vector3(-.26f,.23f,-.20f)};
            for(int i=1;i<exhaust.Length;i++)OutdoorRod(bike,"排気管",exhaust[i-1],exhaust[i],.037f,chrome);
            OutdoorRod(bike,"クロームサイレンサー",new Vector3(-.26f,.23f,-.20f),new Vector3(-.94f,.30f,-.20f),.087f,chrome);
            OutdoorRound(bike,"排気口",new Vector3(-.945f,.30f,-.20f),new Vector3(.014f,.061f,.061f),black);
            OutdoorRod(bike,"ハンドル中央",new Vector3(.37f,1.04f,-.18f),new Vector3(.37f,1.04f,.18f),.022f,chrome);
            foreach(int side in new[]{-1,1})
            {
                OutdoorRod(bike,"ハンドル",new Vector3(.37f,1.04f,side*.18f),new Vector3(.27f,1.07f,side*.35f),.022f,chrome);
                OutdoorRod(bike,"グリップ",new Vector3(.27f,1.07f,side*.26f),new Vector3(.24f,1.07f,side*.39f),.036f,rubber);
                OutdoorRod(bike,"ブレーキレバー",new Vector3(.32f,1.08f,side*.26f),new Vector3(.32f,1.08f,side*.38f),.011f,steel);
                OutdoorRod(bike,"ミラーステー",new Vector3(.31f,1.07f,side*.27f),new Vector3(.26f,1.27f,side*.34f),.012f,chrome);
                OutdoorRound(bike,"丸ミラー",new Vector3(.26f,1.27f,side*.34f),new Vector3(.035f,.10f,.10f),chrome);
                foreach(float x in new[]{.52f,-.91f})
                {
                    OutdoorRod(bike,"ウインカーステー",new Vector3(x,.73f,0),new Vector3(x,.73f,side*.22f),.017f,black);
                    OutdoorRound(bike,"ウインカー",new Vector3(x,.73f,side*.22f),new Vector3(.075f,.055f,.055f),amber);
                }
                OutdoorRod(bike,"ステップ",new Vector3(-.15f,.30f,side*.13f),new Vector3(-.15f,.30f,side*.28f),.028f,rubber);
            }
            OutdoorRound(bike,"クロームライトケース",new Vector3(.51f,.94f,0),new Vector3(.21f,.20f,.20f),chrome);
            OutdoorRound(bike,"丸型ヘッドライト",new Vector3(.618f,.94f,0),new Vector3(.015f,.17f,.17f),lens);
            OutdoorRound(bike,"メーターケース",new Vector3(.38f,1.10f,0),new Vector3(.12f,.08f,.12f),chrome);
            OutdoorRound(bike,"メーター文字盤",new Vector3(.38f,1.143f,0),new Vector3(.094f,.006f,.094f),black);
            OutdoorBox(bike,"尾灯",new Vector3(-.97f,.70f,0),new Vector3(.05f,.09f,.12f),red);
            OutdoorBox(bike,"後部ナンバー",new Vector3(-1.0f,.58f,0),new Vector3(.016f,.12f,.17f),white);
            OutdoorRod(bike,"サイドスタンド",new Vector3(-.20f,.31f,0),new Vector3(-.32f,.025f,-.27f),.022f,steel);
        }
    }
}
