window.ArticleEditor = (function(){
  function init(cfg){
    const el = {
      content: byId(cfg.contentId),
      title: byId(cfg.titleId),
      slug: byId(cfg.slugId),
      headerImageUrl: byId(cfg.headerImageUrlId),
      metaDescription: byId(cfg.metaDescriptionId),
      metaKeywords: byId(cfg.metaKeywordsId),
      tags: cfg.tagsId ? byId(cfg.tagsId) : null,
      toolbar: cfg.toolbarId ? byId(cfg.toolbarId) : null,
      fmText: cfg.frontMatterTextareaId ? byId(cfg.frontMatterTextareaId) : null,
      fmInsertBtn: cfg.frontMatterInsertBtnId ? byId(cfg.frontMatterInsertBtnId) : null,
      fmLoadBtn: cfg.frontMatterLoadBtnId ? byId(cfg.frontMatterLoadBtnId) : null
    };

    // Keep tags and metaKeywords in sync
    if (el.tags && el.metaKeywords){
      el.tags.addEventListener('input', ()=> el.metaKeywords.value = el.tags.value);
      if (!el.tags.value && el.metaKeywords.value) el.tags.value = el.metaKeywords.value;
    }

    // Title -> Slug
    if (el.title && el.slug){
      el.title.addEventListener('input', ()=>{
        const s = toSlug(el.title.value);
        if (!cfg.lockSlug) el.slug.value = s;
      });
    }

    // Slash commands in content
    if (el.content){
      el.content.addEventListener('keydown', (e)=> handleShortcuts(e, el));
      el.content.addEventListener('input', (e)=> handleSlashCommands(e, el));
    }

    // Toolbar bindings
    if (el.toolbar){
      el.toolbar.addEventListener('click', function(e){
        const btn = e.target.closest('[data-cmd]');
        if (!btn) return;
        e.preventDefault();
        exec(btn.getAttribute('data-cmd'), el);
      });
    }

    // Front matter
    if (el.fmInsertBtn && el.fmText){
      el.fmInsertBtn.addEventListener('click', ()=>{
        const yaml = buildFrontMatter(el);
        el.fmText.value = yaml;
        insertOrUpdateFrontMatter(el.content, yaml);
        el.content.dispatchEvent(new Event('input'));
      });
    }

    if (el.fmLoadBtn && el.fmText){
      el.fmLoadBtn.addEventListener('click', ()=>{
        const yaml = extractFrontMatter(el.content.value);
        el.fmText.value = yaml || '---\n# Front matter not found. Click Insert to add.\n---';
        const data = parseFrontMatter(yaml);
        if (data.title && el.title) el.title.value = data.title;
        if (data.image && el.headerImageUrl) el.headerImageUrl.value = data.image;
        if (data.description && el.metaDescription) el.metaDescription.value = data.description;
        if (data.tags){
          const tags = Array.isArray(data.tags) ? data.tags.join(', ') : String(data.tags);
          if (el.tags) el.tags.value = tags;
          if (el.metaKeywords) el.metaKeywords.value = tags;
        }
      });
    }
  }

  function exec(cmd, el){
    switch(cmd){
      case 'h1': insertPrefix(el.content, '# '); break;
      case 'h2': insertPrefix(el.content, '## '); break;
      case 'h3': insertPrefix(el.content, '### '); break;
      case 'bold': wrap(el.content, '**'); break;
      case 'italic': wrap(el.content, '_'); break;
      case 'link': insertSnippet(el.content, '[text](https://)'); break;
      case 'image': insertSnippet(el.content, '![alt](https://)'); break;
      case 'ul': insertLinesPrefix(el.content, '- '); break;
      case 'ol': insertLinesPrefix(el.content, '1. '); break;
      case 'quote': insertLinesPrefix(el.content, '> '); break;
      case 'code': insertBlock(el.content, '```\ncode\n```'); break;
      case 'table': insertBlock(el.content, '| Col1 | Col2 |\n| --- | --- |\n| A | B |'); break;
      default: break;
    }
    el.content.dispatchEvent(new Event('input'));
  }

  function handleShortcuts(e, el){
    const mac = /(Mac|iPhone|iPod|iPad)/i.test(navigator.platform);
    const ctrl = mac ? e.metaKey : e.ctrlKey;
    if (!ctrl) return;
    const k = e.key.toLowerCase();
    if (k === 's'){ e.preventDefault(); closestForm(el.content)?.requestSubmit(); }
    if (k === 'b'){ e.preventDefault(); wrap(el.content, '**'); }
    if (k === 'i'){ e.preventDefault(); wrap(el.content, '_'); }
    if (k === 'k'){ e.preventDefault(); insertSnippet(el.content, '[text](https://)'); }
    if (k === '1'){ e.preventDefault(); insertPrefix(el.content, '# '); }
    if (k === '2'){ e.preventDefault(); insertPrefix(el.content, '## '); }
    if (k === '3'){ e.preventDefault(); insertPrefix(el.content, '### '); }
    if (k === '`'){ e.preventDefault(); insertBlock(el.content, '```\ncode\n```'); }
  }

  function handleSlashCommands(e, el){
    const ta = el.content;
    const pos = ta.selectionStart;
    const val = ta.value;
    const start = Math.max(0, val.lastIndexOf('\n', pos - 1) + 1);
    const token = val.substring(start, pos);
    const map = {
      '/h1': '# ', '/h2': '## ', '/h3': '### ', '/link': '[text](https://)', '/img': '![alt](https://)', '/quote': '> ', '/code': '```\ncode\n```'
    };
    const key = Object.keys(map).find(k=> token.endsWith(k));
    if (key){
      const before = val.substring(0, pos - key.length);
      const after = val.substring(pos);
      const insert = map[key];
      ta.value = before + insert + after;
      const newPos = (before + insert).length;
      ta.setSelectionRange(newPos, newPos);
    }
  }

  // Editing helpers
  function insertPrefix(ta, prefix){
    const {start, end, value} = ta;
    const sel = value.substring(ta.selectionStart, ta.selectionEnd) || '';
    const lineStart = value.lastIndexOf('\n', ta.selectionStart - 1) + 1;
    const head = value.substring(0, lineStart);
    const rest = value.substring(lineStart);
    ta.value = head + prefix + rest;
    const pos = (head + prefix).length;
    ta.setSelectionRange(pos, pos);
  }
  function insertLinesPrefix(ta, prefix){
    const s = ta.selectionStart; const e = ta.selectionEnd; const v = ta.value;
    const before = v.substring(0, s);
    const sel = v.substring(s, e);
    const after = v.substring(e);
    const lines = (sel || '').split('\n').map(l=> prefix + l);
    const out = before + lines.join('\n') + after;
    ta.value = out;
    const pos = before.length + (lines.join('\n')).length;
    ta.setSelectionRange(pos, pos);
  }
  function wrap(ta, sym){
    const s = ta.selectionStart, e = ta.selectionEnd; const v = ta.value;
    const sel = v.substring(s, e) || 'text';
    const out = v.substring(0, s) + sym + sel + sym + v.substring(e);
    ta.value = out;
    const pos = s + sym.length + sel.length + sym.length;
    ta.setSelectionRange(pos, pos);
  }
  function insertSnippet(ta, text){
    const s = ta.selectionStart, e = ta.selectionEnd; const v = ta.value;
    const out = v.substring(0, s) + text + v.substring(e);
    ta.value = out;
    const pos = s + text.length;
    ta.setSelectionRange(pos, pos);
  }
  function insertBlock(ta, block){
    insertSnippet(ta, (ta.value && !ta.value.endsWith('\n') ? '\n\n' : '') + block + '\n\n');
  }

  // Front matter helpers
  function buildFrontMatter(el){
    const title = (el.title?.value || '').trim();
    const image = (el.headerImageUrl?.value || '').trim();
    const desc = (el.metaDescription?.value || '').trim();
    const tags = (el.tags?.value || el.metaKeywords?.value || '').trim();
    const tagsArr = tags ? tags.split(',').map(t=> t.trim()).filter(Boolean) : [];
    let yaml = '---\n';
    if (title) yaml += `title: "${escapeYaml(title)}"\n`;
    if (tagsArr.length) yaml += `tags: [${tagsArr.map(t=> '"'+escapeYaml(t)+'"').join(', ')}]\n`;
    if (image) yaml += `image: "${escapeYaml(image)}"\n`;
    if (desc) yaml += `description: "${escapeYaml(desc)}"\n`;
    yaml += '---';
    return yaml;
  }

  function parseFrontMatter(yaml){
    const res = {}; if (!yaml) return res;
    const body = yaml.replace(/^---\s*|\s*---$/g, '');
    body.split(/\r?\n/).forEach(line=>{
      const i = line.indexOf(':'); if (i<0) return;
      const key = line.substring(0,i).trim();
      let val = line.substring(i+1).trim();
      if (/^\[.*\]$/.test(val)){
        // list
        val = val.replace(/^\[|\]$/g,'').split(',').map(x=> x.trim().replace(/^"|"$/g,''));
      } else {
        val = val.replace(/^"|"$/g,'');
      }
      res[key] = val;
    });
    return res;
  }

  function insertOrUpdateFrontMatter(ta, yaml){
    const current = extractFrontMatter(ta.value);
    if (current){
      ta.value = ta.value.replace(current, yaml);
    } else {
      ta.value = yaml + '\n\n' + ta.value.trimStart();
    }
  }
  function extractFrontMatter(text){
    const m = text.match(/^(---[\s\S]*?---)/);
    return m ? m[1] : null;
  }

  function toSlug(t){
    return t.toLowerCase().replace(/[^a-z0-9\s-]/g,'').replace(/\s+/g,'-').replace(/-+/g,'-').replace(/(^-|-$)/g,'');
  }
  function escapeYaml(s){
    return s.replace(/"/g,'\\"');
  }
  function byId(id){ return id ? document.getElementById(id) : null; }
  function closestForm(el){ return el?.closest('form'); }

  return { init };
})();
