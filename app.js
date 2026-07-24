const $=s=>document.querySelector(s),audio=$('#audio');let tracks=[],current=-1,shuffled=false,repeating=false,systemBands=Array(60).fill(0),displayBands=Array(60).fill(0),peakBands=Array(60).fill(0),systemLevel=0,isSystemMix=false,sensitivity=1,visualizerMode='spectrum';const key='wiinamp-settings',fallbackArtwork='https://wiinamp.local/default-artwork.png';
for(let i=0;i<60;i++){const b=document.createElement('i');b.className='bar';
$('#visualizer').append(b)}
const time=s=>!isFinite(s)?'0:00':`${Math.floor(s/60)}:${String(Math.floor(s%60)).padStart(2,'0')}`;
function draw(){document.querySelectorAll('.bar').forEach((b,i)=>{const local=audio.paused?0:(Math.sin(Date.now()/130+i*1.7)+1)*32+Math.random()*38;const raw=systemBands[i]||0;displayBands[i]=Math.max(raw,displayBands[i]*.84);const fft=Math.min(128,Math.max(2,displayBands[i]*128*sensitivity));const height=isSystemMix?fft:local;peakBands[i]=Math.max(height,peakBands[i]-0.75);b.style.height=`${height}px`;b.style.setProperty('--peak-offset',`${Math.max(0,peakBands[i]-height)}px`)});requestAnimationFrame(draw)}
function render(){const l=$('#playlistItems');
$('#count').textContent=`${tracks.length} TRACKS`;l.innerHTML=tracks.length?'':'<li class="empty">Drag and drop music files here, or use ADD MEDIA.</li>';tracks.forEach((t,i)=>{const e=document.createElement('li');e.className=i===current?'active':'';e.innerHTML=`<span class="name">${t.name}</span><span class="length">${t.duration?time(t.duration):'--:--'}</span>`;e.ondblclick=()=>load(i,true);l.append(e)})}
function addFiles(files){[...files].filter(f=>f.type.startsWith('audio/')).forEach(file=>{const t={name:file.name.replace(/\.[^/.]+$/,''),url:URL.createObjectURL(file),duration:0};const p=document.createElement('audio');p.src=t.url;p.onloadedmetadata=()=>{t.duration=p.duration;render()};tracks.push(t)});render()}
function load(i,play){if(!tracks[i])return;current=i;
audio.src=tracks[i].url;
$('#trackTitle').textContent=tracks[i].name.toUpperCase();
$('#trackInfo').textContent='LOCAL MEDIA';render();if(play)audio.play()}
function step(n){if(!tracks.length)return;let i=shuffled?Math.floor(Math.random()*tracks.length):current+n;if(i<0)i=tracks.length-1;if(i>=tracks.length)i=0;load(i,true)}
function toggleValue(button){return button.getAttribute('aria-checked')==='true'}
function setToggle(button,active){button.setAttribute('aria-checked',String(active));button.classList.toggle('active',active);button.querySelector('.toggle-text').textContent=active?'ON':'OFF'}
function applyMiniMode(mini){$('.app-shell').classList.toggle('mini-mode',mini);post(`mini-mode:${mini}`)}
function post(v){window.chrome?.webview?.postMessage(v)}function save(){localStorage.setItem(key,JSON.stringify({top:toggleValue($('#alwaysOnTopToggle')),sensitivity:$('#sensitivity').value,theme:$('#theme').value,visualizerMode,mini:toggleValue($('#miniModeToggle'))}))}function settings(){let s={};try{s=JSON.parse(localStorage.getItem(key)||'{}')}catch{}setToggle($('#alwaysOnTopToggle'),!!s.top);
$('#sensitivity').value=s.sensitivity||100;
$('#theme').value=s.theme||'luna';sensitivity=$('#sensitivity').value/100;
visualizerMode=s.visualizerMode||'spectrum';$('#visualizerMode').value=visualizerMode;$('#visualizer').dataset.mode=visualizerMode;
setToggle($('#miniModeToggle'),!!s.mini);applyMiniMode(!!s.mini);
$('#sensitivityValue').textContent=`${$('#sensitivity').value}%`;
$('.app-shell').dataset.theme=$('#theme').value;post(`always-on-top:${toggleValue($('#alwaysOnTopToggle'))}`)}
$('#openFiles').onclick=()=>$('#fileInput').click();
$('#fileInput').onchange=e=>addFiles(e.target.files);
$('#play').onclick=()=>{if(current<0&&tracks.length)load(0);
audio.paused?audio.play():audio.pause()};
$('#pause').onclick=()=>audio.pause();
$('#stop').onclick=()=>{audio.pause();
audio.currentTime=0};
$('#next').onclick=()=>step(1);
$('#previous').onclick=()=>step(-1);
$('#shuffle').onclick=e=>{shuffled=!shuffled;e.currentTarget.classList.toggle('active',shuffled)};
$('#repeat').onclick=e=>{repeating=!repeating;e.currentTarget.classList.toggle('active',repeating)};
$('#systemAudio').onclick=()=>post('system-audio-toggle');
$('#volume').oninput=e=>audio.volume=e.target.value/100;
$('#seek').oninput=e=>{if(audio.duration)audio.currentTime=audio.duration*e.target.value/100};
$('#clear').onclick=()=>{audio.pause();tracks=[];current=-1;
audio.removeAttribute('src');render()};

$('#settings').onclick=()=>$('#settingsPanel').hidden=false;
$('#miniSettings').onclick=()=>$('#settingsPanel').hidden=false;
$('#closeSettings').onclick=()=>$('#settingsPanel').hidden=true;
$('#togglePlaylist').onclick=()=>{const c=$('#playlist').classList.toggle('collapsed');
$('.app-shell').classList.toggle('playlist-hidden',c);
$('#togglePlaylist').textContent=c?'+':'−';post(`playlist-collapsed:${c}`)};
$('#alwaysOnTopToggle').onclick=e=>{const active=!toggleValue(e.currentTarget);setToggle(e.currentTarget,active);save();post(`always-on-top:${active}`)};
$('#sensitivity').oninput=()=>{sensitivity=$('#sensitivity').value/100;
$('#sensitivityValue').textContent=`${$('#sensitivity').value}%`;save()};
$('#theme').onchange=()=>{$('.app-shell').dataset.theme=$('#theme').value;save()};
$('#visualizerMode').onchange=e=>{visualizerMode=e.target.value;$('#visualizer').dataset.mode=visualizerMode;save()};
$('#miniModeToggle').onclick=e=>{const active=!toggleValue(e.currentTarget);setToggle(e.currentTarget,active);applyMiniMode(active);save()};

audio.ontimeupdate=()=>{$('#elapsed').textContent=time(audio.currentTime);
$('#seek').value=audio.duration?audio.currentTime/audio.duration*100:0};
audio.onloadedmetadata=()=>$('#duration').textContent=time(audio.duration);
audio.onended=()=>repeating?(audio.currentTime=0,audio.play()):step(1);

function updateArtwork(source){const artwork=$('#artwork'),next=source||fallbackArtwork;artwork.onerror=()=>{if(artwork.dataset.fallback!=='true'){artwork.dataset.fallback='true';artwork.src=fallbackArtwork}else{artwork.removeAttribute('src');artwork.hidden=true}};if(artwork.getAttribute('src')!==next){artwork.dataset.fallback=next===fallbackArtwork?'true':'';artwork.src=next}artwork.hidden=false}
window.chrome?.webview?.addEventListener('message',e=>{const d=e.data;if(d.type==='frequencyBands')systemBands=d.bands;else if(d.type==='systemAudio')systemLevel=d.level;else if(d.type==='systemAudioState'){isSystemMix=d.enabled;
$('#systemAudio').classList.toggle('active',d.enabled);
$('#status').textContent=d.message?(d.enabled?'SYSTEM MIX LIMITED':'SYSTEM MIX UNAVAILABLE'):(d.enabled?'SYSTEM MIX ACTIVE':'SYSTEM MIX OFF');
$('#status').title=d.message||''}else if(d.type==='systemAudioNotice'){$('#status').textContent='SYSTEM MIX WARNING';
$('#status').title=d.message||''}else if(d.type==='appleMusic'&&isSystemMix){$('#trackTitle').textContent=d.title||'APPLE MUSIC';updateArtwork(d.artwork);
$('#trackInfo').textContent=[d.artist,d.album].filter(Boolean).join(' — ')||'APPLE MUSIC';if(d.duration>0){$('#elapsed').textContent=time(d.elapsed);
$('#duration').textContent=time(d.duration);
$('#seek').value=d.elapsed/d.duration*100}}});
document.querySelectorAll('[data-host-action]').forEach(b=>b.onclick=()=>post(b.dataset.hostAction));
$('#appTitlebar').onmousedown=e=>{if(!e.target.closest('button'))post('drag')};
document.addEventListener('dragover',e=>e.preventDefault());
document.addEventListener('drop',e=>{e.preventDefault();addFiles(e.dataTransfer.files)});draw();render();settings();

// SYSTEM MIX is the default source.  Its transport buttons use Windows SMTC
// to control the active Apple Music media session.
function mediaCommand(command, localAction){
  if(isSystemMix) post(`system-media:${command}`); else localAction();
}
$('#play').onclick=()=>mediaCommand('toggle',()=>{if(current<0&&tracks.length)load(0);audio.paused?audio.play():audio.pause()});
$('#pause').onclick=()=>mediaCommand('pause',()=>audio.pause());
$('#next').onclick=()=>mediaCommand('next',()=>step(1));
$('#previous').onclick=()=>mediaCommand('previous',()=>step(-1));
$('#stop').onclick=()=>mediaCommand('stop',()=>{audio.pause();audio.currentTime=0});
post('system-audio-enable');
