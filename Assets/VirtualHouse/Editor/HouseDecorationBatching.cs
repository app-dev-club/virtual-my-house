using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        private static void CombineRepeatedDecorations(Transform root)
        {
            // Only fixed, non-colliding repetition. Doors, glass, furniture and structural
            // walls remain independent, preserving passage tests and transparent sorting.
            string[] prefixes={"青灰タイル","床タイル_","カーテンひだ_","水切り溝","排水溝",
                "シャワーホース","縦框_","横框_","南欄間縦框_","南欄間横框_","縦格子_",
                "窓縦框_","窓横框_","天井竿縁_","縁側板_"};
            var groups=root.GetComponentsInChildren<MeshFilter>()
                .Where(f=>f.sharedMesh!=null && f.transform.childCount==0 && f.GetComponent<Collider>()==null
                    && prefixes.Any(p=>f.name.StartsWith(p)) && f.GetComponent<MeshRenderer>()!=null)
                .GroupBy(f=>new {Parent=f.transform.parent,Material=f.GetComponent<MeshRenderer>().sharedMaterial,
                    Prefix=prefixes.First(p=>f.name.StartsWith(p))}).ToArray();
            const string folder="Assets/VirtualHouse/GeneratedMeshes/Repeated";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/VirtualHouse/GeneratedMeshes","Repeated");
            int before=0,after=0,batchIndex=0;
            foreach(var group in groups)
            {
                var parts=group.ToArray();
                if(parts.Length<4 || group.Key.Material==null || group.Key.Material.renderQueue>=3000)continue;
                string hierarchy=group.Key.Parent.name;
                for(Transform p=group.Key.Parent.parent;p!=null;p=p.parent)hierarchy=p.name+"/"+hierarchy;
                // Same-named curtain groups may coexist in a room; never share their baked transforms.
                string path=folder+"/"+Hash128.Compute(hierarchy+"/"+group.Key.Prefix+"/"+group.Key.Material.name+"/"+batchIndex++)+".asset";
                Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.indexFormat=IndexFormat.UInt32;
                mesh.CombineMeshes(parts.Select(f=>new CombineInstance{mesh=f.sharedMesh,
                    transform=group.Key.Parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                mesh.name=group.Key.Prefix+"統合";mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                GameObject combined=new(mesh.name);combined.transform.SetParent(group.Key.Parent,false);
                combined.AddComponent<MeshFilter>().sharedMesh=mesh;
                MeshRenderer renderer=combined.AddComponent<MeshRenderer>();renderer.sharedMaterial=group.Key.Material;
                renderer.shadowCastingMode=parts[0].GetComponent<MeshRenderer>().shadowCastingMode;
                renderer.receiveShadows=parts[0].GetComponent<MeshRenderer>().receiveShadows;
                foreach(var part in parts)Object.DestroyImmediate(part.gameObject);
                before+=parts.Length;after++;
            }
            Debug.Log($"Repeated decoration batching: {before} objects -> {after} meshes; colliders unchanged.");
        }
    }
}
