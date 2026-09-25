(function(){
const OL='#2A1B30';
let SW=6,UID=0;
const uid=()=>'aa'+(++UID).toString(36);
const f=n=>Math.round(n*10)/10;
function rr(x,y,w,h,r){r=Math.max(0,Math.min(r,w/2,h/2));return `M${f(x+r)} ${f(y)}H${f(x+w-r)}A${f(r)} ${f(r)} 0 0 1 ${f(x+w)} ${f(y+r)}V${f(y+h-r)}A${f(r)} ${f(r)} 0 0 1 ${f(x+w-r)} ${f(y+h)}H${f(x+r)}A${f(r)} ${f(r)} 0 0 1 ${f(x)} ${f(y+h-r)}V${f(y+r)}A${f(r)} ${f(r)} 0 0 1 ${f(x+r)} ${f(y)}Z`}
function el(cx,cy,rx,ry){ry=ry??rx;return `M${f(cx-rx)} ${f(cy)}A${f(rx)} ${f(ry)} 0 1 0 ${f(cx+rx)} ${f(cy)}A${f(rx)} ${f(ry)} 0 1 0 ${f(cx-rx)} ${f(cy)}Z`}
const rect=(x,y,w,h)=>`M${f(x)} ${f(y)}h${f(w)}v${f(h)}h${f(-w)}Z`;
function P(d,fill,o={}){
  let s=`<path d="${d}" fill="${fill}"/>`;
  if(o.inner||o.sh||o.hi){const id=uid();s+=`<clipPath id="${id}"><path d="${d}"/></clipPath><g clip-path="url(#${id})">${o.inner||''}${o.sh?`<path d="${o.sh}" fill="${OL}" fill-opacity="0.22" fill-rule="evenodd"/>`:''}${o.hi?`<path d="${o.hi}" fill="#FFFFFF" fill-opacity="0.42"/>`:''}</g>`}
  return s+`<path d="${d}" fill="none" stroke="${OL}" stroke-width="${f(SW*(o.w||1))}" stroke-linejoin="round"/>`;
}
const Pi=(d,fill)=>`<path d="${d}" fill="${fill}" stroke="${OL}" stroke-width="${f(SW*.55)}" stroke-linejoin="round"/>`;
const ln=(d,w,c=OL,op)=>`<path d="${d}" fill="none" stroke="${c}" stroke-width="${f(w)}" stroke-linecap="round" stroke-linejoin="round"${op?` stroke-opacity="${op}"`:''}/>`;
const tube=(d,w,c)=>ln(d,w+2*SW,OL)+ln(d,w,c);
const G=(x,y,a,s,id)=>`<g${id?` id="${id}"`:''} transform="translate(${f(x)} ${f(y)})${a?` rotate(${f(a)})`:''}">${s}</g>`;
const rot=([x,y],a)=>{const r=a*Math.PI/180,c=Math.cos(r),s=Math.sin(r);return[x*c-y*s,x*s+y*c]};
const add=(a,b)=>[a[0]+b[0],a[1]+b[1]];
const sub=(a,b)=>[a[0]-b[0],a[1]-b[1]];
const dirA=v=>Math.atan2(-v[0],v[1])*180/Math.PI;
function ik(S,T,L1,L2,pick){const v=sub(T,S);let d=Math.hypot(v[0],v[1]);d=Math.max(Math.abs(L1-L2)+1,Math.min(d,L1+L2-.5));const base=dirA(v);const al=Math.acos(Math.max(-1,Math.min(1,(L1*L1+d*d-L2*L2)/(2*L1*d))))*180/Math.PI;const c=[base+al,base-al].map(a1=>{const E=add(S,rot([0,L1],a1));return{a1,E,a2:dirA(sub(T,E))}});c.sort((p,q)=>pick==='down'?q.E[1]-p.E[1]:p.E[0]-q.E[0]);return c[0]}
const leaf=(x,y,a,L,c)=>G(x,y,a,P(`M0 0Q${f(L*.5)} ${f(-L*.42)} ${f(L)} 0Q${f(L*.5)} ${f(L*.42)} 0 0Z`,c,{w:.75})+ln(`M3 0H${f(L*.75)}`,SW*.3,OL,.55));
const flame=(x,y,a,k)=>G(x,y,a,P(`M0 0C${-9*k} ${-6*k} ${-9*k} ${-18*k} ${-2*k} ${-30*k}C0 ${-20*k} ${6*k} ${-18*k} ${8*k} ${-26*k}C${14*k} ${-14*k} ${10*k} ${-4*k} 0 0Z`,'#FF8A1F',{w:.8,inner:`<path d="M0 ${-3*k}C${-4*k} ${-7*k} ${-4*k} ${-13*k} 0 ${-19*k}C${2*k} ${-13*k} ${6*k} ${-9*k} 0 ${-3*k}Z" fill="#FFE14A"/>`}));

/* ---------- parts ---------- */
function dims(ch){const b=ch.b||{},bw=b.bw||1,lw=b.lw||1,lh=b.lh||1,bs=ch.bow.scale||1;
  return{bw,lw,lh,k:lw,TW:66*bw,TH:96,UA:40*lh,FA:36*lh,UL:34*lh,LL:30*lh,wUA:24*lw,wFA:22*lw,wUL:30*lw,wLL:27*lw,hr:15*Math.sqrt(lw),bH:90*bs,bD:34*bs}}
function limb(w,L,c,o={}){
  let inner='';
  if(o.stripe)inner+=`<path d="${rect(w*.06,-w,w*.22,L+2*w)}" fill="${o.stripe}"/>`;
  if(o.band)inner+=Pi(rect(-w,L-(o.bandLen||12),2*w,2*w),o.band);
  if(o.bandTop)inner+=Pi(rect(-w,-w,2*w,w/2+(o.bandTopLen||12)),o.bandTop);
  if(o.grain)inner+=ln(`M${f(-w*.1)} 2Q${f(w*.2)} ${f(L*.5)} ${f(-w*.12)} ${f(L)}`,SW*.4,o.grain);
  return{svg:P(rr(-w/2,-w/2,w,L+w,w/2),c,{inner,sh:rect(-w,-w,w*.8,L+3*w),hi:rr(w*.1,w*.1,w*.17,L*.55,w*.085)}),box:[-w/2-4,-w/2-4,w+8,L+w+8]}}
function hand(r,c){const cy=r*.5;return{svg:P(el(0,cy,r),c,{sh:el(0,cy,r+12)+' '+el(r*.3,cy-r*.3,r),hi:el(r*.35,cy-r*.45,r*.28,r*.18)})+ln(`M${f(r*.15)} ${f(cy+r*.15)}Q${f(r*.55)} ${f(cy+r*.25)} ${f(r*.62)} ${f(cy-r*.2)}`,SW*.5),box:[-r-4,cy-r-4,2*r+8,2*r+8]}}
function foot(k,c,sole,root){
  const d=`M${f(-13*k)} ${f(-8*k)}V${f(12*k)}Q${f(-13*k)} ${f(20*k)} ${f(-5*k)} ${f(20*k)}H${f(30*k)}Q${f(42*k)} ${f(20*k)} ${f(42*k)} ${f(9*k)}Q${f(42*k)} ${f(-1*k)} ${f(26*k)} ${f(-2*k)}L${f(13*k)} ${f(-4*k)}V${f(-8*k)}Z`;
  let pre='';
  if(root)pre=P(`M${f(30*k)} ${f(8*k)}Q${f(50*k)} ${f(6*k)} ${f(56*k)} ${f(20*k)}H${f(30*k)}Z`,c,{w:.8})+P(`M${f(-8*k)} ${f(10*k)}Q${f(-24*k)} ${f(10*k)} ${f(-28*k)} ${f(20*k)}H${f(-6*k)}Z`,c,{w:.8});
  return{svg:pre+P(d,c,{inner:root?ln(`M${f(-4*k)} ${f(-4*k)}Q${f(8*k)} ${f(6*k)} ${f(24*k)} ${f(4*k)}`,SW*.4,'#4E3220'):Pi(rect(-20*k,13*k,70*k,12*k),sole),hi:rr(20*k,2*k,12*k,5*k,2.5*k)}),box:[(root?-32:-17)*k,-12*k,(root?92:63)*k,36*k]}}
function torso(ch,D){const{TW,TH}=D,t=ch.torso;
  const svg=P(rr(-TW/2,-TH,TW,TH+10,Math.min(28,TW*.42)),t.c,{inner:t.detail?t.detail(D):'',sh:rect(-TW/2-10,-TH-10,TW*.3+10,TH+30),hi:rr(TW/2-15,-TH+14,6,28,3)})+(t.over?t.over(D):'');
  const bx=t.box?t.box(D):[-TW/2-6,-TH-6,TW+12,TH+22];return{svg,box:bx}}
function capeP(ch,D){const c=ch.cape;if(!c)return null;const W=D.TW,H=D.TH;let d;
  const top=`M18 6C-8 -4 ${f(-W*.6)} 4 ${f(-W*.72)} 32C${f(-W*.84)} 70 ${f(-W*.92)} 108 ${f(-W*1.02)} 136`;
  if(c.shape==='coat')d=`M20 ${H-34}C-10 ${H-40} ${f(-W*.56)} ${H-30} ${f(-W*.6)} ${H}L${f(-W*.72)} ${H+58}Q${f(-W*.1)} ${H+72} 26 ${H+56}Z`;
  else if(c.shape==='thorn')d=top+`L${f(-W*.86)} 128L${f(-W*.74)} 144L${f(-W*.58)} 130L${f(-W*.44)} 146L${f(-W*.3)} 132L${f(-W*.18)} 140C-16 92 -8 44 18 6Z`;
  else if(c.shape==='moss')d=top+`Q${f(-W*.9)} 150 ${f(-W*.74)} 138Q${f(-W*.6)} 152 ${f(-W*.46)} 138Q${f(-W*.32)} 150 ${f(-W*.2)} 136C-16 92 -8 44 18 6Z`;
  else d=top+`Q${f(-W*.66)} 148 ${f(-W*.2)} 138C-16 92 -8 44 18 6Z`;
  let inner=c.patch?Pi(rr(-W*.7,70,18,16,3),c.patch):'';
  if(c.shape==='coat')inner+=ln(`M${f(-W*.3)} ${H-20}L${f(-W*.34)} ${H+58}`,SW*.5,OL,.5);
  const svg=P(d,c.c,{inner,sh:rect(-W*.34,-20,120,260),hi:rr(-W*.86,40,5,50,2.5)});
  return{svg,box:c.shape==='coat'?[-W*.72-8,H-46,W*.72+40,124]:[-W*1.04-8,-8,W*1.04+30,162]}}
function quiverP(ch){const q=ch.quiver,fs=q.f;let s='';
  [-8,0,8].forEach((x,i)=>{s+=P(`M${x-5} 6V-18Q${x} -30 ${x+5} -18V6Z`,fs[i%fs.length],{w:.8})});
  s+=P(rr(-15,0,30,86,13),q.c,{sh:rect(-22,-5,14,100),hi:rr(6,10,5,40,2.5),inner:Pi(rect(-20,14,40,8),q.band)+Pi(rect(-20,62,40,8),q.band)});
  return{svg:s,box:[-24,-36,48,128]}}
function bowP(ch,D){const B=ch.bow,h=D.bH,d=D.bD,st=B.style,w=st==='heavy'?15:st==='branch'?13:10;
  let path=`M${f(-d)} ${f(-h)}Q${f(d)} 0 ${f(-d)} ${f(h)}`;
  if(st==='recurve'||st==='curved')path=`M${f(-d+16)} ${f(-h-16)}Q${f(-d-4)} ${f(-h-8)} ${f(-d)} ${f(-h)}Q${f(d)} 0 ${f(-d)} ${f(h)}Q${f(-d-4)} ${f(h+8)} ${f(-d+16)} ${f(h+16)}`;
  let pre='',post='';
  const nodes=[[-d*.25,-h*.5],[-d*.25,h*.5]];
  if(st==='curved'){pre+=flame(-d+16,-h-16,40,1)+flame(-d+16,h+16,140,1)}
  if(st==='metal'){nodes.forEach(([x,y])=>{post+=P(el(x,y,6),'#3FE0F0',{w:.7})});pre+=tube(`M${f(-d-8)} ${f(-h+4)}l-10 -8l10 -4l-8 -12`,3.5,'#FFE14A')+tube(`M${f(-d-8)} ${f(h-4)}l-10 8l10 4l-8 12`,3.5,'#FFE14A')}
  if(st==='heavy'){nodes.forEach(([x,y])=>{post+=P(rr(x-11,y-5,22,10,4),B.band,{w:.7})})}
  if(st==='branch'){post+=leaf(-d,-h,-120,30,'#5E9A32')+leaf(-d,-h,-60,26,'#8CC04A')+leaf(-d,h,120,30,'#5E9A32')+leaf(-d,h,60,26,'#8CC04A')+P(el(-d*.25,-h*.5,5),'#4E3220',{w:.6})}
  if(st==='long'&&B.leaf){post+=leaf(-d,-h,-110,22,'#5E8E34')+leaf(-d,h,110,22,'#5E8E34')}
  if(st==='recurve'&&B.tipCap){post+=P(el(-d+16,-h-16,5),B.tipCap,{w:.6})+P(el(-d+16,h+16,5),B.tipCap,{w:.6})}
  const grip=P(rr(-w/2-3,-17,w+6,34,5),B.grip,{inner:ln(`M${-w/2-3} -6L${w/2+3} -10M${-w/2-3} 6L${w/2+3} 2`,SW*.35,OL,.5)});
  return{svg:pre+tube(path,w,B.wood)+ln(path,w*.28,'#FFFFFF',.35)+post+grip,box:[-d-30,-h-44,d+56,2*h+88]}}
function stringSeg(d,c,glow){return(glow?ln(d,11,c,.3):'')+ln(d,6.5*SW/6,OL)+ln(d,2.8*SW/6,c)}
function stringP(ch,D){const d=`M0 ${f(-D.bH)}V${f(D.bH)}`;return{svg:stringSeg(d,ch.bow.string,ch.bow.glow),box:[-8,-D.bH-8,16,2*D.bH+16]}}
function arrowP(ch){const L=104,t=ch.arrowTip||'steel',fl=ch.quiver.f;
  let s=tube(`M0 0H${L}`,5,'#D9B27A');
  s+=P(`M-4 0L8 -12H26L16 0Z`,fl[0],{w:.7})+P(`M-4 0L8 12H26L16 0Z`,fl[1]||fl[0],{w:.7});
  if(t==='bomb')s+=P(el(L+10,0,10),'#2B2730',{hi:el(L+13,-4,3,2)})+P(rr(L-1,-5,6,10,2),'#D9A441',{w:.6})+ln(`M${L+10} -10q3 -6 8 -6`,SW*.4,'#D9A441')+P(el(L+19,-16,3.5),'#FFD83A',{w:.5});
  else{const c=t==='fire'?'#FF8A1F':t==='electric'?'#3FE0F0':'#C9D2DC';
    if(t!=='steel')s+=`<path d="${el(L+6,0,16)}" fill="${c}" fill-opacity="0.35"/>`;
    s+=P(`M${L-4} -9L${L+18} 0L${L-4} 9Z`,c,{w:.8,hi:`M${L} -5L${L+10} -1L${L} -1Z`})}
  return{svg:s,box:[-8,-20,L+34,40]}}

/* ---------- head ---------- */
function eye(ch,ex){const F=ch.face||{},st=F.eye||'dot',sk=ch.skin;
  if(ex==='aim')return st==='glow'?P(el(36,-60,10,4),'#C6FF4A',{w:.7}):ln('M26 -60Q36 -66 47 -60',SW*.95);
  if(ex==='hurt')return ln('M28 -69L42 -60L28 -51',SW*.9);
  if(ex==='happy')return ln('M26 -57Q36 -70 47 -57',SW*.95);
  if(st==='glow')return `<path d="${el(36,-60,16,18)}" fill="#C6FF4A" fill-opacity="0.35"/>`+P(el(36,-60,9,12),'#C6FF4A',{w:.7})+`<path d="${el(37,-62,4,6)}" fill="#FFFFFF"/>`;
  if(st==='nervous')return P(el(36,-61,12,13.5),'#FFFFFF',{w:.7})+`<path d="${el(41,-59,5,6)}" fill="${OL}"/><circle cx="42.5" cy="-61.5" r="1.8" fill="#FFFFFF"/>`;
  let s=`<path d="${el(36,-60,7,10.5)}" fill="${OL}"/><circle cx="38.6" cy="-64" r="3" fill="#FFFFFF"/>`;
  if(st==='lidded')s+=`<path d="M26 -60A10 12 0 0 1 46 -60Z" fill="${sk}"/>`+ln('M26 -60L46 -61',SW*.7);
  return s}
function brow(ch,ex){const F=ch.face||{},c=F.brow||ch.hair;const m={normal:F.browN||'M24 -80Q35 -87 48 -81',aim:'M24 -87L48 -76',hurt:'M24 -80L48 -89',happy:'M24 -84Q35 -92 48 -85'};
  return `<g transform="translate(0 ${F.browDy||0})">${ln(m[ex]||m.normal,SW*(F.browW||1.05),c)}</g>`}
function mouth(ch,ex){const st=(ch.face||{}).mouth||'smile';
  if(ex==='aim')return ln('M40 -29L56 -31',SW*.85);
  if(ex==='hurt')return P(el(47,-27,6.5,8),OL,{w:.5})+`<path d="${el(47,-23,4,3)}" fill="#FF7C8A"/>`;
  if(ex==='happy')return P('M33 -35Q46 -8 59 -37Z',OL,{w:.6})+`<path d="${el(47,-22,6,4)}" fill="#FF7C8A"/>`;
  if(st==='proud')return P('M33 -35Q46 -12 59 -37Z','#FFFFFF',{w:.8})+ln('M36 -32Q46 -26 57 -34',SW*.35);
  if(st==='boss')return P('M22 -32Q44 -10 62 -36Q44 -24 22 -32Z','#3A2414',{w:.8});
  if(st==='grin')return ln('M34 -29Q47 -24 58 -37',SW*.85)+ln('M55 -38L60 -34',SW*.6);
  if(st==='nervous')return ln('M38 -27Q42 -31 46 -27Q50 -23 54 -27',SW*.75);
  if(st==='calm')return ln('M40 -28Q48 -25 55 -29',SW*.8);
  return ln('M35 -31Q46 -21 57 -33',SW*.85)}
function headP(ch,ex){const F=ch.face||{},sk=ch.skin;let s='';
  s+=F.nose==='stub'?P(rr(50,-58,28,13,6.5),sk,{hi:rr(58,-56,14,3,1.5)}):P(el(60,-50,10),sk);
  s+=P(el(6,-58,56),sk,{sh:el(6,-58,78)+' '+el(20,-72,56),inner:(F.blush===false?'':`<path d="${el(40,-34,10,6)}" fill="#FF6F7A" fill-opacity="0.45"/>`)+(F.headInner||'')});
  if(F.ear!==false)s+=P(el(-16,-54,12,13),sk)+ln('M-12 -60Q-20 -56 -14 -48',SW*.45);
  s+=(F.under||'')+eye(ch,ex)+brow(ch,ex)+(F.pre||'')+mouth(ch,ex)+(F.over||'');
  if(ex==='hurt')s+=P('M72 -106Q64 -94 66 -88Q70 -82 76 -86Q80 -92 72 -106Z','#8FD8FF',{w:.6});
  return{svg:s,box:[-60,-122,142,132]}}

/* ---------- gear (head-local) ---------- */
function unionCircles(cs,c,hi,glow){const d=cs.map(a=>el(a[0],a[1],a[2])).join(' ');const id=uid();
  return `<path d="${d}" fill="none" stroke="${OL}" stroke-width="${f(SW*2)}" stroke-linejoin="round"/><path d="${d}" fill="${c}"/><clipPath id="${id}"><path d="${d}"/></clipPath><g clip-path="url(#${id})"><path d="${rect(-200,-60,400,200)} ${el(-20,-60,60,30)}" fill="${OL}" fill-opacity="0.22"/>${hi.map(a=>`<path d="${el(a[0],a[1],a[2])}" fill="#FFFFFF" fill-opacity="0.42"/>`).join('')}${glow||''}</g>`}
const GEAR={
 ranger:()=>({svg:P('M-30 -118C-42 -148 -64 -166 -84 -170C-76 -148 -58 -126 -38 -110Z','#F3E6C4',{inner:ln('M-36 -114C-50 -136 -64 -152 -80 -166',SW*.4,'#C9B48A')})+
   P('M54 -94C44 -126 -10 -138 -42 -118C-60 -106 -84 -86 -94 -70C-78 -64 -68 -52 -64 -38C-60 -20 -54 -4 -40 6L-16 6C-26 -16 -22 -44 -6 -66C10 -86 34 -92 54 -94Z','#4FB548',{sh:rect(-110,-150,70,170),hi:'M44 -96C22 -94 2 -84 -8 -62C-18 -42 -20 -18 -14 4L-8 4C-12 -20 -10 -44 0 -62C10 -80 26 -88 48 -92Z'}),box:[-98,-174,156,184]}),
 fire:()=>({svg:P('M-26 -108C-50 -128 -44 -168 -14 -184C-20 -164 -6 -156 2 -170C14 -150 10 -126 -2 -110Z','#D8321F',{hi:'M-20 -124C-30 -140 -26 -160 -14 -172C-18 -154 -16 -140 -12 -126Z'})+
   P('M62 -76C60 -112 28 -128 -6 -126C-40 -122 -62 -98 -60 -66C-60 -48 -54 -34 -44 -24C-44 -44 -38 -58 -26 -66L-18 -56L-12 -74C2 -80 16 -80 28 -76L34 -88L44 -74C50 -74 56 -74 62 -76Z','#D8321F',{sh:rect(-80,-140,44,130),hi:el(10,-114,16,5)})+
   P(rr(-30,-122,32,12,5),'#F2C14E'),box:[-64,-190,130,168]}),
 electric:()=>({svg:P('M62 -82L74 -102L48 -104L56 -134L28 -118L20 -150L-2 -124L-22 -150L-30 -118L-60 -130L-54 -98L-74 -86L-54 -72C-58 -54 -52 -38 -44 -28C-42 -54 -32 -72 -10 -82C14 -90 40 -86 62 -82Z','#DDF1FF',{sh:rect(-90,-160,50,140),hi:'M-2 -118L10 -136L14 -112Z'})+
   P('M-56 -86L60 -106L62 -93L-54 -72Z','#15224A')+P(el(34,-100,15),'#9AA8BC')+P(el(34,-100,9),'#3FE0F0',{w:.7,hi:el(37,-104,3.5)}),box:[-78,-154,154,130]}),
 bomb:()=>({svg:P('M-50 -86C-58 -66 -54 -46 -44 -36C-40 -54 -36 -70 -30 -86Z','#3E2A1C')+
   P('M64 -92C66 -146 -56 -152 -58 -92Z','#7A8A3A',{sh:el(-50,-80,50,70),hi:el(22,-128,16,6)})+P(el(-20,-108,3.5),'#D9A441',{w:.5})+P(el(0,-130,3.5),'#D9A441',{w:.5})+
   P(el(32,-112,10),'#F08A24',{w:.8})+P(rr(-68,-97,144,13,6.5),'#5E6B2C'),box:[-72,-150,150,70]}),
 pip:()=>({svg:P('M58 -88C48 -126 -24 -136 -52 -102C-68 -80 -64 -40 -52 -10L-30 -8C-36 -40 -26 -72 -2 -84C18 -94 40 -92 58 -88Z','#D9A62E',{sh:rect(-80,-140,40,140),hi:el(10,-118,14,5)})+
   Pi(rr(-44,-98,20,18,3),'#7A5230')+ln('M-40 -94l4 4M-32 -84l4 4',SW*.35,'#F3E6C4')+P('M44 -92L60 -102L56 -88L68 -90L54 -78Z','#7A5230',{w:.8}),box:[-68,-132,140,128]}),
 moss:()=>({svg:tube('M-20 -106C-30 -140 -44 -156 -64 -166M-36 -140C-50 -144 -60 -138 -70 -130M-28 -126C-24 -150 -14 -160 -4 -168',11,'#8A5A36')+
   tube('M16 -108C24 -140 38 -154 54 -164M30 -136C42 -138 52 -132 62 -124',11,'#8A5A36')+
   P('M62 -80C64 -126 -54 -132 -56 -80C-40 -88 40 -90 62 -80Z','#5E8E34',{sh:rect(-70,-140,40,70),hi:el(16,-116,16,5)})+leaf(-30,-94,-160,22,'#8CC04A')+leaf(34,-96,-20,20,'#8CC04A'),box:[-80,-176,150,110]}),
 twig:()=>({svg:tube('M-10 -106L-22 -150M-20 -136L-38 -146M10 -110L18 -156M16 -140L30 -150M30 -100L52 -132',8,'#7A5230')+leaf(52,-132,-50,16,'#8CC04A')+
   P('M58 -84C50 -120 -30 -130 -52 -94C-60 -78 -56 -54 -46 -38C-38 -60 -24 -76 -2 -84C20 -90 40 -88 58 -84Z','#5A3A22',{sh:rect(-70,-140,36,120)}),box:[-58,-162,118,132]}),
 bramble:()=>{const th=[[-44,-118,-40,-146,-30,-122],[-64,-104,-86,-118,-72,-94],[-84,-80,-106,-82,-88,-68],[-68,-52,-88,-40,-66,-40],[-60,-24,-78,-8,-56,-12]];
   return{svg:th.map(t=>P(`M${t[0]} ${t[1]}L${t[2]} ${t[3]}L${t[4]} ${t[5]}Z`,'#2F5A3A',{w:.8})).join('')+
   P('M54 -94C44 -126 -10 -138 -42 -118C-60 -106 -84 -86 -94 -70C-78 -64 -68 -52 -64 -38C-60 -20 -54 -4 -40 6L-16 6C-26 -16 -22 -44 -6 -66C10 -86 34 -92 54 -94Z','#7B3F8C',{sh:rect(-110,-150,70,170),hi:'M44 -96C22 -94 2 -84 -8 -62C-18 -42 -20 -18 -14 4L-8 4C-12 -20 -10 -44 0 -62C10 -80 26 -88 48 -92Z'})+
   P(el(-16,2,7),'#C9CED6',{w:.7}),box:[-110,-150,170,164]}},
 thorn:()=>{let sp='';[[-44,-104],[-22,-114],[2,-118],[26,-116],[48,-106]].forEach(([x,y],i)=>{sp+=P(`M${x-6} ${y+4}L${x+(i%2?4:-4)} ${y-20}L${x+6} ${y+2}Z`,'#5A3420',{w:.8})});
   return{svg:P('M60 -82C56 -118 20 -130 -12 -126C-46 -120 -62 -94 -58 -62C-56 -46 -50 -36 -42 -30C-42 -52 -34 -68 -18 -76C6 -84 34 -86 60 -82Z','#3A2014',{sh:rect(-80,-140,40,120)})+sp+
   tube('M-50 -98C-20 -114 30 -114 60 -98',11,'#5A3420')+P(el(4,-110,7),'#F2C14E',{w:.7,hi:el(6,-113,2.5)})+P(el(38,-106,5),'#F2C14E',{w:.7})+P(el(-28,-106,5),'#F2C14E',{w:.7}),box:[-64,-146,134,100]}},
 warden:()=>{const cs=[[-36,-108,30],[0,-132,34],[38,-118,28],[-62,-86,22],[62,-100,18],[-70,-116,18],[18,-150,18]];
   return{svg:tube('M-40 -140L-66 -164M-56 -154L-72 -150',7,'#6B4226')+unionCircles(cs,'#5E9A32',[[6,-144,9],[40,-128,7],[-30,-122,7]],`<path d="${el(-6,-110,5)} ${el(30,-100,4)} ${el(-50,-96,4)}" fill="#C6FF4A"/>`),box:[-96,-176,184,120]}}
};

/* ---------- characters ---------- */
const RANGER={key:'ranger',name:'Ranger',role:'Hero · Starter',hero:1,s:1,skin:'#F4C49A',hair:'#6B4226',gearName:'hood',
 arm:{ua:'#F3E6C4',fa:'#F3E6C4',faO:{band:'#8B5A2E',bandLen:22},hand:'#F4C49A'},leg:{ul:'#6B4A2B',ll:'#5A3A22',foot:'#5A3A22',sole:'#3A2618'},
 torso:{c:'#F3E6C4',detail:D=>{const a=D.TW/2,t=-D.TH;return Pi(`M${-a-6} ${t-6}H${a-16}L${a-8} -30L${a-14} 16H${-a-6}Z`,'#8B5A2E')+Pi(rect(-a-6,-28,D.TW+12,14),'#5A3A22')+Pi(rr(a-24,-31,15,20,4),'#E0B04A')+Pi(`M${-a-6} ${t-6}H${a+6}V${t+14}Q${a*.5} ${t+28} 0 ${t+20}Q${-a*.5} ${t+30} ${-a-6} ${t+18}Z`,'#4FB548')}},
 cape:{c:'#3F9A3A'},quiver:{c:'#8B5A2E',band:'#5A3A22',f:['#F3E6C4','#4FB548','#E0B04A']},
 bow:{wood:'#B7793F',grip:'#5A3A22',string:'#F7F1E1',style:'long'},face:{mouth:'smile'},
 sw:['#4FB548','#8B5A2E','#F3E6C4','#5A3A22','#F4C49A']};
const FIRE={key:'fire',name:'Fire Archer',role:'Hero',hero:1,s:1,skin:'#E3A073',hair:'#D8321F',gearName:'hair',
 arm:{ua:'#A12A2E',uaO:{bandTop:'#F2782B',bandTopLen:14},fa:'#38313A',faO:{band:'#F2C14E',bandLen:8},hand:'#E3A073'},leg:{ul:'#38313A',ll:'#A12A2E',llO:{bandTop:'#F2782B',bandTopLen:6},foot:'#38313A',sole:'#1E1A20'},
 torso:{c:'#A12A2E',detail:D=>{const a=D.TW/2,t=-D.TH;return Pi(`M${-a+8} ${t-6}H${a+6}V-44Q${a*.2} -32 ${-a+8} -46Z`,'#F2782B')+ln(`M${-a*.4} ${t+18}L${-a*.05} ${t+30}L${a*.3} ${t+22}L${a*.6} ${t+34}`,SW*.55,'#FFD34A')+ln(`M${-a+6} -36L${-a*.3} -31L0 -38L${a*.4} -31L${a+4} -36`,SW*.5,'#FFB23F')+Pi(rect(-a-6,-26,D.TW+12,14),'#38313A')+Pi(rr(a-24,-29,14,20,4),'#F2C14E')}},
 cape:null,quiver:{c:'#38313A',band:'#F2C14E',f:['#F2782B','#D8321F','#F2C14E']},
 bow:{wood:'#38313A',grip:'#F2C14E',string:'#FFE3B0',style:'curved'},arrowTip:'fire',face:{mouth:'grin',brow:'#A8231A'},
 sw:['#D8321F','#F2782B','#38313A','#F2C14E','#A12A2E']};
const ELEC={key:'electric',name:'Electric Archer',role:'Hero',hero:1,s:1,skin:'#F8D5B4',hair:'#DDF1FF',gearName:'hair',
 arm:{ua:'#23386E',uaO:{stripe:'#FFD83A'},fa:'#23386E',faO:{band:'#3FE0F0',bandLen:9},hand:'#EAF4FF'},leg:{ul:'#23386E',ulO:{stripe:'#FFD83A'},ll:'#23386E',llO:{stripe:'#FFD83A'},foot:'#FFD83A',sole:'#15224A'},
 torso:{c:'#23386E',detail:D=>{const a=D.TW/2,t=-D.TH;return Pi(`M${a+8} ${t+4}L${-a*.1} -58L${a*.3} -56L${-a-8} -2L${-a-8} -22L${-a*.35} -48L${-a*.75} -50L${a+8} ${t-12}Z`,'#FFD83A')+Pi(rect(-a-6,-24,D.TW+12,12),'#15224A')+Pi(rr(a-22,-27,13,18,4),'#3FE0F0')+Pi(rr(-a*.4,t-6,a*1.3,14,6),'#3FE0F0')}},
 cape:null,quiver:{c:'#9AA8BC',band:'#23386E',f:['#3FE0F0','#FFD83A','#DDF1FF']},
 bow:{wood:'#9AA8BC',grip:'#23386E',string:'#3FE0F0',glow:1,style:'metal'},arrowTip:'electric',face:{mouth:'grin',brow:'#8CC9F0'},
 sw:['#23386E','#3FE0F0','#FFD83A','#9AA8BC','#DDF1FF']};
const bombs=D=>{const a=D.TW/2;return[-a+10,0,a-6].map(x=>P(el(x,-24,9),'#2B2730',{hi:el(x+3,-28,3,2)})+P(rr(x-4,-37,8,5,1.5),'#D9A441',{w:.6})+ln(`M${x} -37q2 -5 6 -6`,SW*.4,'#D9A441')).join('')};
const BOMB={key:'bomb',name:'Bomb Archer',role:'Hero',hero:1,s:1,skin:'#B97A50',hair:'#3E2A1C',gearName:'helmet',b:{bw:1.25,lw:1.12,lh:.92},
 arm:{ua:'#6E7F36',fa:'#6E7F36',faO:{band:'#7A4E2A',bandLen:10},hand:'#7A4E2A'},leg:{ul:'#F08A24',ll:'#F08A24',llO:{band:'#2E2A28',bandLen:10},foot:'#2E2A28',sole:'#151315'},
 torso:{c:'#6E7F36',detail:D=>{const a=D.TW/2,t=-D.TH;return Pi(`M${-a-6} -50H${a+6}V20H${-a-6}Z`,'#F08A24')+Pi(rect(a-22,t-6,11,60),'#F08A24')+Pi(el(a-16.5,-50,4),'#D9A441')+Pi(rect(-a-6,-36,D.TW+12,14),'#4A3A22')},over:bombs,box:D=>[-D.TW/2-14,-D.TH-6,D.TW+28,D.TH+22]},
 cape:null,quiver:{c:'#8A6A3A',band:'#D9A441',f:['#F08A24','#6E7F36','#D9A441']},
 bow:{wood:'#6B4A2B',grip:'#2E2A28',band:'#D9A441',string:'#F7F1E1',style:'heavy'},arrowTip:'bomb',
 face:{mouth:'smile',over:P('M38 -42C30 -46 18 -42 14 -30C22 -34 30 -32 36 -30C42 -26 50 -26 56 -32C62 -28 70 -30 76 -40C68 -38 62 -46 54 -46C48 -46 42 -46 38 -42Z','#3E2A1C',{hi:'M50 -42C56 -42 62 -40 68 -38L64 -36C60 -38 56 -40 50 -40Z'}),browW:1.4},
 sw:['#6E7F36','#F08A24','#2B2730','#D9A441','#B97A50']};
const PIP={key:'pip',name:'Scout Pip',role:'World 1 · Enemy',s:.86,skin:'#F1C39A',hair:'#6B4226',gearName:'hood',b:{bw:.92,lw:.9,lh:.9},
 arm:{ua:'#7A5230',fa:'#E0B23A',hand:'#F1C39A'},leg:{ul:'#7A5230',ll:'#7A5230',foot:'#4A3222',sole:'#2A1C14'},
 torso:{c:'#E0B23A',detail:D=>{const a=D.TW/2;return Pi(rr(-4,-64,16,15,3),'#7B4BB7')+ln('M-1 -61l4 4M5 -55l4 4',SW*.35,'#F3E6C4')+Pi(rr(-a+4,-86,14,13,3),'#A0773F')+Pi(rect(-a-6,-26,D.TW+12,10),'#A0773F')+Pi(el(a-12,-21,6),'#A0773F')}},
 cape:{c:'#7A5230',patch:'#A0773F'},quiver:{c:'#7A5230',band:'#4A3222',f:['#E0B23A','#7B4BB7','#E0B23A']},
 bow:{wood:'#A0773F',grip:'#4A3222',string:'#F7F1E1',style:'long',scale:.72},
 face:{eye:'nervous',mouth:'nervous',under:P(rr(-4,-80,78,32,15),'#7B4BB7',{hi:rr(4,-76,50,5,2.5)})},
 sw:['#E0B23A','#7A5230','#7B4BB7','#4A3222','#F1C39A']};
const MOSS={key:'moss',name:'Hunter Moss',role:'World 1 · Enemy',s:1,skin:'#9C6A45',hair:'#3E5A22',gearName:'antler_hat',b:{bw:1,lw:1,lh:1.18},
 arm:{ua:'#5E8E34',fa:'#8A5A36',hand:'#9C6A45'},leg:{ul:'#8A5A36',ll:'#5E8E34',foot:'#5A3A22',sole:'#3A2618'},
 torso:{c:'#6E9A3A',detail:D=>{const a=D.TW/2;return Pi(`M${-a-4} -70Q${-a*.4} -86 ${-a*.1} -68Q${-a*.3} -52 ${-a-4} -56Z`,'#4E7A2A')+Pi(`M${a*.2} -46Q${a*.7} -60 ${a+6} -42Q${a*.6} -30 ${a*.1} -34Z`,'#8A5A36')+Pi(el(-a*.2,-16,10,6),'#4E7A2A')+Pi(rect(-a-6,-28,D.TW+12,10),'#5A3A22')},over:D=>leaf(-D.TW*.2,-D.TH+4,-150,22,'#8CC04A')+leaf(D.TW*.2,-D.TH+2,-40,20,'#5E8E34'),box:D=>[-D.TW/2-24,-D.TH-18,D.TW+36,D.TH+28]},
 cape:{c:'#4E7A2A',shape:'moss'},quiver:{c:'#8A5A36',band:'#5A3A22',f:['#8CC04A','#5E8E34','#8CC04A']},
 bow:{wood:'#8A5A36',grip:'#5A3A22',string:'#F7F1E1',style:'long',leaf:1},
 face:{eye:'lidded',mouth:'calm',pre:P('M4 -30Q2 -6 18 0Q24 10 34 2Q44 10 50 -2Q62 -4 60 -22Q50 -18 40 -20Q24 -18 14 -34Z','#5E8E34',{hi:el(44,-10,5,3)})},
 sw:['#6E9A3A','#4E7A2A','#8A5A36','#5A3A22','#9C6A45']};
const twig=(sc,key)=>({key,name:'Twig Twins',role:'World 1 · Enemy',s:.95,skin:'#D8A56E',hair:'#4A2F1A',gearName:'twigs',b:{bw:.62,lw:.7,lh:1.12},
 arm:{ua:'#7A5230',uaO:{grain:'#5A3A22'},fa:'#7A5230',faO:{grain:'#5A3A22'},hand:'#D8A56E'},leg:{ul:'#7A5230',ulO:{grain:'#5A3A22'},ll:'#5A3A22',foot:'#4A2F1A',sole:'#2A1C14'},
 torso:{c:'#7A5230',detail:D=>{const a=D.TW/2,t=-D.TH;return Pi(el(a*.2,-52,6,9),'#5A3A22')+ln(`M${-a*.3} ${t+20}Q${-a*.1} -50 ${-a*.35} -10`,SW*.4,'#5A3A22')+Pi(rect(-a-6,-24,D.TW+12,10),'#4A2F1A')},
  over:D=>{const a=D.TW/2,t=-D.TH;return P(`M${-a+2} ${t+2}C${-a-14} ${t+6} ${-a-24} ${t+20} ${-a-30} ${t+34}L${-a-16} ${t+38}C${-a-12} ${t+26} ${-a-4} ${t+16} ${-a+8} ${t+10}Z`,sc)+P(rr(-a-5,t-8,D.TW+10,18,9),sc,{hi:rr(-a,t-5,D.TW*.6,4,2)})},box:D=>[-D.TW/2-36,-D.TH-12,D.TW+44,D.TH+28]},
 cape:null,quiver:{c:'#5A3A22',band:'#4A2F1A',f:[sc,'#8CC04A',sc]},
 bow:{wood:'#8A6A3A',grip:'#4A2F1A',string:'#F7F1E1',style:'long',scale:.85},face:{mouth:'grin'},
 sw:['#7A5230','#4A2F1A','#E0413A','#3A7BE0','#D8A56E']});
const BRAMBLE={key:'bramble',name:'Ranger Bramble',role:'World 1 · Rival',s:1,skin:'#E6B28E',hair:'#3B1F45',gearName:'hood',
 arm:{ua:'#7B3F8C',fa:'#2F5A3A',faO:{band:'#C9CED6',bandLen:8},hand:'#E6B28E'},leg:{ul:'#2F5A3A',ll:'#3B2440',foot:'#3B2440',sole:'#1E1224'},
 torso:{c:'#2F5A3A',detail:D=>{const a=D.TW/2,t=-D.TH;return Pi(`M${-a-6} ${t-6}H${a+6}V${t+18}L${a*.5} ${t+28}L${a*.1} ${t+20}L${-a*.3} ${t+32}L${-a*.7} ${t+22}L${-a-6} ${t+30}Z`,'#7B3F8C')+Pi(rect(-a-6,-26,D.TW+12,13),'#3B2440')+Pi(rr(a-22,-29,13,19,4),'#C9CED6')}},
 cape:{c:'#6A3279',shape:'thorn'},quiver:{c:'#3B2440',band:'#C9CED6',f:['#C9CED6','#7B3F8C','#2F5A3A']},
 bow:{wood:'#4A2A22',grip:'#C9CED6',tipCap:'#C9CED6',string:'#EDE6F2',style:'recurve'},
 face:{eye:'lidded',mouth:'grin',browN:'M24 -86L48 -79'},
 sw:['#7B3F8C','#2F5A3A','#C9CED6','#3B2440','#4A2A22']};
const THORN={key:'thorn',name:'Captain Thorn',role:'World 1 · Mini-boss',s:1.12,skin:'#EDB38A',hair:'#3A2014',gearName:'crown',b:{bw:1.35,lw:1.2,lh:1},
 arm:{ua:'#B3262E',fa:'#B3262E',faO:{band:'#F2C14E',bandLen:9},hand:'#4A2A1A'},leg:{ul:'#4A2A1A',ll:'#2E1C14',llO:{bandTop:'#F2C14E',bandTopLen:6},foot:'#2E1C14',sole:'#150C08'},
 torso:{c:'#B3262E',detail:D=>{const a=D.TW/2,t=-D.TH;return Pi(`M${a-20} ${t-6}H${a+6}V20H${a-8}Z`,'#4A2A1A')+[t+26,t+46,t+66].map(y=>Pi(el(a-27,y,4.5),'#F2C14E')).join('')+Pi(rect(-a-6,-26,D.TW+12,15),'#4A2A1A')+Pi(rr(a-32,-30,20,22,4),'#F2C14E')}},
 cape:{c:'#8E1C24',shape:'coat'},quiver:{c:'#4A2A1A',band:'#F2C14E',f:['#B3262E','#F2C14E','#B3262E']},
 bow:{wood:'#4A2A1A',grip:'#F2C14E',tipCap:'#F2C14E',string:'#F7F1E1',style:'recurve',scale:1.25},
 face:{mouth:'proud',pre:P('M-2 -34C0 -6 22 10 44 6C60 2 68 -14 66 -30C58 -22 46 -22 40 -18C30 -14 16 -22 8 -38Z','#4A2A1A',{hi:el(40,-2,6,3)}),
  over:ln('M-44 -96L64 -78',SW*.8)+P('M56 -76Q72 -74 70 -58Q64 -50 54 -56Z',OL,{w:.6})},
 sw:['#B3262E','#4A2A1A','#F2C14E','#8E1C24','#EDB38A']};
const WARDEN={key:'warden',name:'The Forest Warden',role:'World 1 · Boss',boss:1,s:2,skin:'#8B5B37',hair:'#4E3220',gearName:'canopy',b:{bw:1.5,lw:1.45,lh:1},
 arm:{ua:'#8B5B37',uaO:{band:'#5E9A32',bandLen:10,grain:'#5E3A20'},fa:'#8B5B37',faO:{grain:'#5E3A20'},hand:'#7A4E2E'},leg:{ul:'#8B5B37',ulO:{grain:'#5E3A20'},ll:'#7A4E2E',llO:{grain:'#5E3A20'},foot:'#6B4226',root:1},
 torso:{c:'#8B5B37',detail:D=>{const a=D.TW/2,t=-D.TH;return ln(`M${-a*.6} ${t+6}Q${-a*.4} -50 ${-a*.7} 4`,SW*.5,'#5E3A20')+ln(`M${a*.62} ${t+10}Q${a*.8} -60 ${a*.5} -4`,SW*.5,'#5E3A20')+Pi(`M${-a-6} ${t-6}H${a+6}V${t+10}Q${a*.4} ${t+22} 0 ${t+12}Q${-a*.5} ${t+26} ${-a-6} ${t+14}Z`,'#5E9A32')+tube(`M${-a} -16C${-a*.3} -26 ${a*.2} -6 ${a} -26`,5,'#4E8A2A')},
  over:D=>{const x=-D.TW*.2;return `<g id="warden_weakspot"><path d="${el(x,-50,28)}" fill="#C6FF4A" fill-opacity="0.5"/>`+P(el(x,-50,19),'#FFC83D',{hi:el(x+6,-57,5,3.5),inner:ln(`M${x} -50m-8 0a8 8 0 1 1 8 8`,SW*.45,'#C98A1A')})+`</g>`}},
 cape:{c:'#4E8A2A',shape:'moss'},quiver:{c:'#6B4226',band:'#4E3220',f:['#C6FF4A','#5E9A32','#8CC04A']},
 bow:{wood:'#6B4226',grip:'#5E9A32',string:'#C6FF4A',glow:1,style:'branch'},
 face:{eye:'glow',mouth:'boss',nose:'stub',ear:false,blush:false,brow:'#4E3220',browW:1.6,browN:'M22 -82L50 -80',headInner:ln('M-30 -30Q-24 -10 -10 -4M-36 -60Q-40 -44 -34 -36',SW*.5,'#5E3A20')},
 sw:['#8B5B37','#5E9A32','#C6FF4A','#FFC83D','#4E3220']};

/* ---------- assembly ---------- */
function partsOf(ch,D,ex){const A=ch.arm,L=ch.leg;return{
  head:()=>headP(ch,ex),gear:()=>GEAR[ch.key.startsWith('twig')?'twig':ch.key](),torso:()=>torso(ch,D),cape:ch.cape?()=>capeP(ch,D):null,quiver:()=>quiverP(ch),
  ua:()=>limb(D.wUA,D.UA,A.ua,A.uaO),fa:()=>limb(D.wFA,D.FA,A.fa,A.faO),hand:()=>hand(D.hr,A.hand),
  ul:()=>limb(D.wUL,D.UL,L.ul,L.ulO),ll:()=>limb(D.wLL,D.LL,L.ll,L.llO),foot:()=>foot(D.k,L.foot,L.sole,L.root),
  bow:()=>bowP(ch,D),string:()=>stringP(ch,D),arrow:()=>arrowP(ch)}}
function build(ch,pose,o={}){
  const D=dims(ch),K=o.prefix||ch.key,nm=n=>o.ids?`${K}_${n}`:null,pt=partsOf(ch,D,pose.expr||'normal');
  const tr=pose.tr||0,T=p=>rot(p,tr);
  const neck=T([2,-D.TH+2]),shF=T([D.TW*.14,-D.TH+16]),shB=T([-D.TW*.08,-D.TH+14]),hpF=T([D.TW*.16,-8]),hpB=T([-D.TW*.14,-8]);
  const headA=tr+(pose.hr||0);
  const leg=(H,a)=>{const Kn=add(H,rot([0,D.UL],a[0]));const An=add(Kn,rot([0,D.LL],a[0]+a[1]));return{H,Kn,An,a1:a[0],a2:a[0]+a[1],fa:a[2]||0}};
  const LF=leg(hpF,pose.fl||[-6,4]),LB=leg(hpB,pose.bl||[10,-4]);
  const lift=Math.max(LF.An[1],LB.An[1])+20*D.k;
  const cheek=add(neck,rot([34,-2],headA));
  const arm=(S,sp)=>{let a1,a2;if(sp.t){const r=ik(S,sp.t,D.UA,D.FA,sp.pick);a1=r.a1;a2=r.a2}else[a1,a2]=sp;const E=add(S,rot([0,D.UA],a1)),W=add(E,rot([0,D.FA],a2));return{S,E,W,a1,a2,C:add(W,rot([0,D.hr*.5],a2))}};
  const AF=arm(shF,pose.fa||[-4,-30]);
  const br=pose.br??60,grip=AF.C;
  const tipT=add(grip,rot([-D.bD,-D.bH],br)),tipB=add(grip,rot([-D.bD,D.bH],br)),rest=add(grip,rot([-D.bD,0],br));
  let nock=null;if(pose.pull)nock=pose.pull==='full'?cheek:[(rest[0]+cheek[0])/2,(rest[1]+cheek[1])/2];
  let baSp=pose.ba||[14,4];
  if(nock)baSp={t:nock,pick:'back'};else if(pose.baT)baSp={t:pose.baT({neck,S:shB,D,T}),pick:pose.baPick||'back'};
  const AB=arm(shB,baSp);
  const armL=(A,s)=>G(A.S[0],A.S[1],A.a1,pt.ua().svg,nm('upper_arm_'+s))+G(A.E[0],A.E[1],A.a2,pt.fa().svg,nm('forearm_'+s))+G(A.W[0],A.W[1],A.a2,pt.hand().svg,nm('hand_'+s));
  const legL=(l,s)=>G(l.H[0],l.H[1],l.a1,pt.ul().svg,nm('upper_leg_'+s))+G(l.Kn[0],l.Kn[1],l.a2,pt.ll().svg,nm('lower_leg_'+s))+G(l.An[0],l.An[1],l.fa,pt.foot().svg,nm('foot_'+s));
  const out=[];const front=!!(nock||pose.baFront);
  if(!front)out.push(armL(AB,'back'));
  out.push(legL(LB,'back'));
  if(pt.cape)out.push(G(neck[0],neck[1],tr,pt.cape().svg,nm('cape')));
  const qp=T([-D.TW*.4,-D.TH+6]);out.push(G(qp[0],qp[1],tr-14,pt.quiver().svg,nm('quiver')));
  out.push(G(0,0,tr,pt.torso().svg,nm('torso')));
  out.push(legL(LF,'front'));
  if(front)out.push(armL(AB,'back'));
  out.push(G(neck[0],neck[1],headA,pt.head().svg,nm('head')));
  out.push(G(neck[0],neck[1],headA,pt.gear().svg,nm(ch.gearName)));
  out.push(G(grip[0],grip[1],br,pt.bow().svg,nm('bow_body')));
  if(nock){const d=`M${f(tipT[0])} ${f(tipT[1])}L${f(nock[0])} ${f(nock[1])}L${f(tipB[0])} ${f(tipB[1])}`;out.push(`<g${o.ids?` id="${K}_bow_string"`:''}>${stringSeg(d,ch.bow.string,ch.bow.glow)}</g>`);
    if(pose.arrow!==false){const v=sub(grip,nock);out.push(G(nock[0],nock[1],Math.atan2(v[1],v[0])*180/Math.PI,pt.arrow().svg,nm('arrow')))}}
  else out.push(G(rest[0],rest[1],br,pt.string().svg,nm('bow_string')));
  out.push(armL(AF,'front'));
  return `<g transform="translate(0 ${f(-lift)})">${out.join('')}</g>`}
function place(ch,pose,o){const s=(ch.s||1)*(o.fs||1.15),flip=ch.hero?1:-1,prev=SW;SW=6/s;const inner=build(ch,pose,o);SW=prev;
  return `<g${o.ids?` id="${o.prefix||ch.key}"`:''} transform="translate(${o.cx} ${o.gy}) scale(${f(flip*s)} ${f(s)})">${inner}</g>`}
const wrap=(inner,w,h,disp)=>`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${w} ${h}"${disp?' width="100%" style="display:block"':` width="${w}" height="${h}"`}>${inner}</svg>`;
function headThumb(ch,ex){const flip=ch.hero?1:-1,D=dims(ch),pt=partsOf(ch,D,ex);SW=6;const h=pt.head(),g=pt.gear();
  const x0=Math.min(h.box[0],g.box[0]),y0=Math.min(h.box[1],g.box[1]),x1=Math.max(h.box[0]+h.box[2],g.box[0]+g.box[2]),y1=Math.max(h.box[1]+h.box[3],g.box[1]+g.box[3]);
  const S=Math.max(x1-x0,y1-y0)+22,cx=(x0+x1)/2*flip,cy=(y0+y1)/2;
  return wrap(`<g transform="scale(${flip} 1)">${h.svg}${g.svg}</g>`.replace('<svg',''),0,0,1).replace('viewBox="0 0 0 0"',`viewBox="${f(cx-S/2)} ${f(cy-S/2)} ${f(S)} ${f(S)}"`)}
function sheet(ch){const s=ch.s||1,flip=ch.hero?1:-1,prev=SW;SW=6/s;const D=dims(ch),pt=partsOf(ch,D,'normal'),K=ch.key;
  const list=[[ch.gearName,pt.gear],['head',pt.head],['torso',pt.torso],['cape',pt.cape],['quiver',pt.quiver],
   ['upper_arm_front',pt.ua],['forearm_front',pt.fa],['hand_front',pt.hand],['upper_arm_back',pt.ua],['forearm_back',pt.fa],['hand_back',pt.hand],
   ['upper_leg_front',pt.ul],['lower_leg_front',pt.ll],['foot_front',pt.foot],['upper_leg_back',pt.ul],['lower_leg_back',pt.ll],['foot_back',pt.foot],
   ['bow_body',pt.bow],['bow_string',pt.string],['arrow',pt.arrow]].filter(p=>p[1]);
  const W=ch.boss?1800:1100,pad=36,lab=30;let x=pad,y=pad,rowH=0,groups='',labels='';
  for(const [n,fn] of list){const p=fn();let [bx,by,bw,bh]=p.box;if(flip<0)bx=-bx-bw;const w=bw*s,h=bh*s;
    if(x+Math.max(w,n.length*9)>W-pad){x=pad;y+=rowH+lab+18;rowH=0}
    const cw=Math.max(w,n.length*9);const px=x+(cw-w)/2-bx*s,py=y-by*s;
    groups+=`<g id="${K}_${n}" transform="translate(${f(px)} ${f(py)})"><g transform="scale(${f(flip*s)} ${f(s)})">${p.svg}</g><g id="${K}_${n}_pivot"><path d="M-11 0H11M0 -11V11" stroke="#FF2D87" stroke-width="2"/><circle r="5" fill="#FF2D87" stroke="#FFFFFF" stroke-width="2"/></g></g>`;
    labels+=`<text x="${f(x+cw/2)}" y="${f(y+h+22)}" text-anchor="middle" font-family="Nunito, sans-serif" font-weight="800" font-size="15" fill="#8A8290">${n}</text>`;
    x+=cw+40;rowH=Math.max(rowH,h)}
  SW=prev;const H=y+rowH+lab+pad;return{inner:groups+`<g id="labels">${labels}</g>`,w:W,h:Math.round(H)}}

const POSES=[
 ['Idle',{expr:'normal'}],
 ['Drawing',{expr:'aim',fa:[-76,-84],br:-6,pull:'half',tr:1,fl:[-10,5],bl:[14,-4]}],
 ['Full draw',{expr:'aim',fa:[-90,-90],br:0,pull:'full',tr:3,fl:[-14,6],bl:[16,-4]}],
 ['Release',{expr:'normal',fa:[-100,-96],br:-6,baT:c=>add(c.S,[-58,-2]),baPick:'down',tr:-3,fl:[-14,6],bl:[16,-4]}],
 ['Hit',{expr:'hurt',tr:-14,hr:-10,fa:[-150,-120],br:36,ba:[150,120],fl:[-44,56],bl:[6,-2]}],
 ['Victory',{expr:'happy',fa:[-6,-40],br:66,ba:[-100,-130],baFront:1,fl:[-10,6],bl:[14,-4]}]];

function card(ch,o){const size=ch.boss?1024:512;const inner=o.inner||place(ch,POSES[0][1],{ids:1,cx:ch.boss?540:(ch.hero?236:276),gy:ch.boss?960:456});
  const disp=o.inner?o.disp:place(ch,POSES[0][1],{cx:ch.boss?540:(ch.hero?236:276),gy:ch.boss?960:456});
  return{key:ch.key,name:ch.name,role:ch.role,file:'archer-arcade_'+ch.key,svg:wrap(inner,size,size),display:wrap(disp,size,size,1),
   sw:ch.sw.map(h=>({hex:h})),expr:o.expr?['normal','aim','hurt'].map(e=>({label:e==='aim'?'aiming':e,display:headThumb(ch,e)})):[]}}

window.AAChars={board(){
  const heroes=[RANGER,FIRE,ELEC,BOMB].map(c=>card(c,{expr:1}));
  const tr=twig('#E0413A','twig_red'),tb=twig('#3A7BE0','twig_blue');
  const twinIn=ids=>place(tb,POSES[0][1],{ids,cx:360,gy:456})+place(tr,POSES[0][1],{ids,cx:160,gy:456});
  const twins=card(tr,{inner:`<g id="twig_twins">${twinIn(1)}</g>`,disp:twinIn(0)});twins.key='twins';twins.file='archer-arcade_twig_twins';
  const enemies=[card(PIP,{expr:1}),card(MOSS,{expr:1}),twins,card(BRAMBLE,{expr:1}),card(THORN,{expr:1})];
  const boss=[card(WARDEN,{})];
  const sheets=[RANGER,FIRE,ELEC,BOMB,WARDEN].map(c=>{const s=sheet(c);return{key:c.key,name:c.name+' — parts',file:'archer-arcade_'+c.key+'_parts',svg:wrap(s.inner,s.w,s.h),display:wrap(s.inner.replace(/id="[^"]*"/g,m=>m.includes('aa')?m:''),s.w,s.h,1),boss:!!c.boss}});
  const poses=POSES.map(([label,p],i)=>({label:`${i+1} · ${label}`,display:wrap(place(RANGER,p,{cx:236,gy:456}),512,512,1),svg:wrap(place(RANGER,p,{ids:1,cx:236,gy:456}),512,512),file:'archer-arcade_ranger_pose_'+label.toLowerCase().replace(/ /g,'_')}));
  return{heroes,enemies,boss,sheets,poses}}};
})();
