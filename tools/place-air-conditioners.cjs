// Offline scene update for environments where Unity cannot acquire a license.
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..');
const material=n=>fs.readFileSync(path.join(root,`Assets/VirtualHouse/Materials/${n}.mat.meta`),'utf8').match(/guid: (\w+)/)[1];
for(const scene of ['HouseRenovated','HouseRenovatedAlternative','HouseRenovatedPlanC']){
 const file=path.join(root,`Assets/Scenes/${scene}.unity`);
 let text=fs.readFileSync(file,'utf8').replace(/\r\n/g,'\n');
 if(text.includes('エアコン_天井近くの壁付け')) { console.log(`${scene}: already applied`); continue; }
 const entries=new Map(text.split(/(?=^--- !u!)/m).slice(1).map(b=>{const m=b.match(/^--- !u!(\d+) &(\d+)/);return [+m[2],{type:+m[1],b}]}));
 const name=b=>{const raw=b.match(/^  m_Name: (.*)$/m)?.[1];return raw?.startsWith('"')?JSON.parse(raw):raw};
 const floors={};for(const [id,{type,b}]of entries)if(type===1&&['1\u968e','2\u968e'].includes(name(b)))floors[name(b)]=+b.match(/component: \{fileID: (\d+)/)[1];
 if(Object.keys(floors).length!==2)throw Error('Unexpected floor state');
 const mesh=[...entries.values()].find(e=>e.type===33&&e.b.includes('m_Mesh: {fileID: 10202,'));
 const go=+mesh.b.match(/m_GameObject: \{fileID: (\d+)/)[1];
 const refs=[...entries.get(go).b.matchAll(/component: \{fileID: (\d+)/g)].map(m=>+m[1]);
 const templates={};for(const id of refs){const e=entries.get(id);templates[e.type]=e.b;}
 if(!templates[65]) templates[65]=[...entries.values()].find(e=>e.type===65).b;
 let next=2000000000;const added=[],children=new Map();
 const vec=a=>`{x: ${a[0]}, y: ${a[1]}, z: ${a[2]}}`;
 function node(n,parent,pos=[0,0,0],scale=[1,1,1],yaw=0,mat=null,collider=false){
  const [g,t,r,m,c]=[next++,next++,next++,next++,next++];
  if(!children.has(parent))children.set(parent,[]);children.get(parent).push(t);children.set(t,[]);
  const components=[t,...(mat?[r,m]:[]),...(collider?[c]:[])];
  added.push(`--- !u!1 &${g}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n${components.map(id=>`  - component: {fileID: ${id}}\n`).join('')}  m_Layer: 0\n  m_Name: ${JSON.stringify(n)}\n  m_TagString: Untagged\n  m_IsActive: 1\n`);
  const a=yaw*Math.PI/360;
  added.push(`--- !u!4 &${t}\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${g}}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: ${Math.sin(a)}, z: 0, w: ${Math.cos(a)}}\n  m_LocalPosition: ${vec(pos)}\n  m_LocalScale: ${vec(scale)}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {fileID: ${parent}}\n  m_LocalEulerAnglesHint: {x: 0, y: ${yaw}, z: 0}\n`);
  for(const [type,id]of [...(mat?[[23,r],[33,m]]:[]),...(collider?[[65,c]]:[])]){
   let b=templates[type].replace(/^--- !u!\d+ &\d+/,`--- !u!${type} &${id}`).replace(/m_GameObject: \{fileID: \d+}/,`m_GameObject: {fileID: ${g}}`);
   if(type===23)b=b.replace(/(m_Materials:\n  - \{fileID: 2100000, guid: )\w+/,`$1${mat}`);
   added.push(b);
  }return t;
 }
 const white=material('KitchenPowerIvory'),dark=material('OfficeCharcoal');
 const group='\u30a8\u30a2\u30b3\u30f3_\u5929\u4e95\u8fd1\u304f\u306e\u58c1\u4ed8\u3051';const lower=node(group,floors['1\u968e']),upper=node(group,floors['2\u968e']);
 for(const [n,x,v,yaw,up]of [['Western_North',227,291,180,false],['WestTatami_West',310,287,90,false],['EastStorage_East',915,308,-90,false],['Upper8_North',392,158,180,true],['Upper4.5_West',553,217,90,true]]){
  const a=yaw*Math.PI/180,p=[(x-(up?211:158))*1.82/75+Math.sin(a)*.11,(up?3:0)+2.31,((up?366:423)-v)*1.82/75+Math.cos(a)*.11];
  const unit=node(n,up?upper:lower,p,[1,1,1],yaw);
  for(const [label,pos,size,mat,col]of [['Body',[0,0,.11],[.8,.28,.22],white,true],['Front',[0,.025,.224],[.76,.19,.016],white,false],['Vent',[0,-.104,.224],[.68,.047,.012],dark,false],['Louver',[0,-.119,.238],[.67,.012,.045],white,false],['Indicator',[.32,-.062,.235],[.018,.008,.004],white,false]])node(label,unit,pos,size,0,mat,col);
 }
 for(const parent of Object.values(floors)){
  const old=entries.get(parent).b, lines=children.get(parent).map(id=>`  - {fileID: ${id}}\n`).join('');
  const b=old.includes('m_Children: []')?old.replace('  m_Children: []\n','  m_Children:\n'+lines):old.replace('  m_Children:\n','  m_Children:\n'+lines);text=text.replace(old,b);
 }
 text+=added.map(b=>{if(!b.startsWith('--- !u!4 '))return b;const id=+b.match(/^--- !u!4 &(\d+)/)[1],list=children.get(id);return list.length?b.replace('  m_Children: []\n','  m_Children:\n'+list.map(i=>`  - {fileID: ${i}}\n`).join('')):b}).join('');
 const ids=[...text.matchAll(/^--- !u!\d+ &(\d+)/gm)].map(m=>m[1]);
 if(new Set(ids).size!==ids.length||children.get(lower).length!==3||children.get(upper).length!==2)throw Error('Hierarchy validation failed');
 fs.writeFileSync(file,text);console.log(`${scene}: five AC units, unique IDs and floor hierarchy validated`);
}

