(function(){
  const canvas = document.getElementById('hero-net');
  const gradient = document.querySelector('.hero-gradient');
  if(!canvas || !gradient) return;

  const ctx = canvas.getContext('2d');
  let width, height, dpr;

  const POINTS = 90;
  const MAX_LINK_DIST = 160;
  const SPEED = 0.15;
  let nodes = [];
  let t = 0; // time for parallax

  function rand(min, max){ return Math.random()*(max-min)+min; }

  function resize(){
    dpr = window.devicePixelRatio || 1;
    width = canvas.clientWidth = window.innerWidth;
    height = canvas.clientHeight = window.innerHeight;
    canvas.width = Math.floor(width * dpr);
    canvas.height = Math.floor(height * dpr);
    ctx.setTransform(dpr,0,0,dpr,0,0);
    ctx.globalCompositeOperation = 'lighter';
  }

  function createNodes(){
    nodes = Array.from({length: POINTS}, () => ({
      x: rand(0, width), y: rand(0, height), vx: rand(-SPEED, SPEED), vy: rand(-SPEED, SPEED)
    }));
  }

  function parallax(){
    t += 0.0005; // very slow
    const x = Math.sin(t) * 20; // dx in px
    const y = Math.cos(t*0.8) * 15; // dy in px
    gradient.style.transform = `translate(${x}px, ${y}px)`;
  }

  function step(){
    parallax();
    ctx.clearRect(0,0,width,height);

    // move
    for(const p of nodes){
      p.x += p.vx; p.y += p.vy;
      if(p.x < 0 || p.x > width) p.vx *= -1;
      if(p.y < 0 || p.y > height) p.vy *= -1;
    }

    // draw links
    for(let i=0;i<nodes.length;i++){
      for(let j=i+1;j<nodes.length;j++){
        const a = nodes[i], b = nodes[j];
        const dx = a.x-b.x, dy=a.y-b.y;
        const dist = Math.hypot(dx,dy);
        if(dist < MAX_LINK_DIST){
          const alpha = 1 - dist / MAX_LINK_DIST;
          ctx.strokeStyle = `rgba(255, 255, 255, ${Math.max(0.08, alpha*0.35)})`;
          ctx.lineWidth = 0.8;
          ctx.beginPath();
          ctx.moveTo(a.x, a.y);
          ctx.lineTo(b.x, b.y);
          ctx.stroke();
        }
      }
    }

    // draw points with subtle glow
    ctx.shadowColor = 'rgba(255,255,255,0.6)';
    ctx.shadowBlur = 6;
    for(const p of nodes){
      ctx.fillStyle = 'rgba(255, 255, 255, 0.9)';
      ctx.beginPath();
      ctx.arc(p.x, p.y, 1.8, 0, Math.PI*2);
      ctx.fill();
    }
    ctx.shadowBlur = 0;

    requestAnimationFrame(step);
  }

  window.addEventListener('resize', ()=>{ resize(); createNodes(); });
  resize();
  createNodes();
  step();
})();
