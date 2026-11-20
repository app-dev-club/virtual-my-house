const fs=require('fs');
for(const scene of ['HouseRenovated','HouseRenovatedAlternative','HouseRenovatedPlanC']){
 const s=fs.readFileSync(`Assets/Scenes/${scene}.unity`,'utf8');
 const blocks=s.split(/(?=^--- !u!)/m).slice(1),ids=new Set(blocks.map(b=>b.match(/^--- !u!\d+ &(\d+)/)[1]));
 if(ids.size!==blocks.length)throw Error('Duplicate IDs');
 const added=blocks.filter(b=>/^--- !u!\d+ &2000000\d+/m.test(b));
 const bodies=added.filter(b=>/m_Name: "Body"/.test(b));
 if(bodies.length!==5)throw Error('Expected five units');
 for(const b of added)for(const m of b.matchAll(/\{fileID: (\d+)}/g))if(m[1]!=='0'&&!ids.has(m[1]))throw Error(`Missing reference ${m[1]}`);
 const transforms=added.filter(b=>b.startsWith('--- !u!4 '));
 const units=transforms.filter(b=>/m_LocalPosition: \{x: [^,]+, y: (2\.31|5\.3100000000000005|5\.31),/.test(b));
 if(units.length!==5)throw Error('Invalid unit heights');
 console.log(`${scene}: 5 units, mounting heights, unique IDs and local references passed`);
}
