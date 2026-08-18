<script lang="ts">
  import { onMount } from 'svelte';
  import { selectedLang } from '../../lib/stores/language';
  import { t } from '../../lib/stores/i18n';
  import Modal from '../../lib/components/Modal.svelte';
  import UniMorphSelector from '../../lib/components/UniMorphSelector.svelte';
  import { api } from '../../lib/api';

  let activeAccordion: 'structures' | 'layers' | 'lexicon' | 'rules' = 'structures';
  let isLoading = false;

  // 1. Inflection Classes & Structures
  let inflectionClasses: any[] = [];
  let structuresByClass: Record<number, any[]> = {};
  let openedClasses: Record<number, boolean> = {};

  // Class modal
  let showClassModal = false;
  let currentClassId = 0;
  let classTitle = '';
  let classDescription = '';

  // Structure modal
  let showStructureModal = false;
  let currentStructureId = 0;
  let structureClassId = 0;
  let structureTitle = '';
  let structureTags = '';
  let structureFormula = '';
  let structureLayerId = 0;
  let structureOrder = 0;

  // Structures Import / Export
  let showImportStructuresModal = false;
  let structureImportText = '';

  // 2. Reusable Layers & Affixes
  let reusableLayers: any[] = [];
  let affixesByLayer: Record<number, any[]> = {};
  let openedLayers: Record<number, boolean> = {};

  // Layer modal
  let showLayerModal = false;
  let currentLayerId = 0;
  let layerTitle = '';

  // Affix modal
  let showAffixModal = false;
  let currentAffixId = 0;
  let affixLayerId = 0;
  let affixRealization = '';
  let affixTags = '';
  let affixTitle = '';
  let affixOrder = 1;

  // Layers Import / Export
  let showImportLayersModal = false;
  let layerImportText = '';

  // 3. Lexicon / Lemmas
  let lemmas: any[] = [];
  let lemmaSearch = '';
  let showLemmaModal = false;
  let currentLemmaId = 0;
  let lemmaEntry = '';
  let lemmaClassId = 0;
  let lemmaMeaning = '';
  let lemmaStem1 = '';
  let lemmaStem2 = '';
  let lemmaStem3 = '';
  let lemmaStem4 = '';
  let lemmaTags = '';
  let lemmaPriority = 1;

  // Import lemmas modal
  let showImportLemmasModal = false;
  let lemmaImportText = '';

  // 4. Morphophonology Rules
  let rules: any[] = [];
  let showRuleModal = false;
  let currentRuleId = 0;
  let ruleTitle = '';
  let ruleReplaceFrom = '';
  let ruleReplaceTo = '';

  onMount(async () => {
    if ($selectedLang) {
      loadAllData($selectedLang.id);
    }
  });

  $: if ($selectedLang) {
    loadAllData($selectedLang.id);
  }

  async function loadAllData(langId: number) {
    isLoading = true;
    try {
      await Promise.allSettled([
        loadInflectionClasses(langId),
        loadReusableLayers(langId),
        loadLemmas(langId),
        loadRules(langId),
      ]);
    } catch (err) {
      console.error('Failed to load linguist data', err);
    } finally {
      isLoading = false;
    }
  }

  // --- Inflection Classes & Structures ---
  async function loadInflectionClasses(langId: number) {
    try {
      inflectionClasses = await api.get('/InflectionClass/list', { LangId: langId }) || [];
    } catch (err) {
      console.error('Failed to load inflection classes', err);
      inflectionClasses = [];
    }
  }

  async function toggleClassStructures(classId: number) {
    openedClasses[classId] = !openedClasses[classId];
    if (openedClasses[classId] && !structuresByClass[classId]) {
      try {
        structuresByClass[classId] = await api.get('/Structure/list', { InflectionClassID: classId }) || [];
      } catch (err) {
        console.error('Failed to load structures for class', classId, err);
        structuresByClass[classId] = [];
      }
    }
  }

  function openAddClass() {
    currentClassId = 0;
    classTitle = '';
    classDescription = '';
    showClassModal = true;
  }

  function openEditClass(c: any) {
    currentClassId = c.id;
    classTitle = c.title;
    classDescription = c.description || '';
    showClassModal = true;
  }

  async function handleSaveClass() {
    if (!$selectedLang || !classTitle.trim()) return;
    try {
      if (currentClassId === 0) {
        await api.post('/InflectionClass/insert', {
          title: classTitle.trim(),
          description: classDescription.trim(),
          langid: $selectedLang.id
        });
      } else {
        await api.post('/InflectionClass/update', {
          id: currentClassId,
          title: classTitle.trim(),
          description: classDescription.trim(),
          langid: $selectedLang.id
        });
      }
      showClassModal = false;
      await loadInflectionClasses($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to save inflection class');
    }
  }

  async function handleDeleteClass(c: any) {
    if (!confirm(`Delete inflection class "${c.title}"?`)) return;
    try {
      await api.post(`/InflectionClass/delete?id=${c.id}`);
      await loadInflectionClasses($selectedLang!.id);
    } catch (err: any) {
      alert(err.message || 'Failed to delete class');
    }
  }

  function openAddStructure(classId: number) {
    currentStructureId = 0;
    structureClassId = classId;
    structureTitle = '';
    structureTags = '';
    structureFormula = '';
    structureLayerId = 0;
    structureOrder = (structuresByClass[classId]?.length || 0) + 1;
    showStructureModal = true;
  }

  function openEditStructure(s: any) {
    currentStructureId = s.id;
    structureClassId = s.inflectionclassid;
    structureTitle = s.title;
    structureTags = s.unimorphtags || '';
    structureFormula = s.formula || '';
    structureLayerId = s.reusablelayerid || 0;
    structureOrder = s.order || 0;
    showStructureModal = true;
  }

  async function handleSaveStructure() {
    if (!structureTitle.trim() || !structureClassId) return;
    try {
      if (currentStructureId === 0) {
        await api.post('/Structure/insert', {
          title: structureTitle.trim(),
          unimorphtags: structureTags.trim(),
          formula: structureFormula.trim(),
          reusablelayerid: structureLayerId || null,
          inflectionclassid: structureClassId,
          order: structureOrder
        });
      } else {
        await api.post('/Structure/update', {
          id: currentStructureId,
          title: structureTitle.trim(),
          unimorphtags: structureTags.trim(),
          formula: structureFormula.trim(),
          reusablelayerid: structureLayerId || null,
          inflectionclassid: structureClassId,
          order: structureOrder
        });
      }
      showStructureModal = false;
      structuresByClass[structureClassId] = await api.get('/Structure/list', { InflectionClassID: structureClassId }) || [];
    } catch (err: any) {
      alert(err.message || 'Failed to save structure');
    }
  }

  async function handleDeleteStructure(s: any) {
    if (!confirm(`Delete paradigm structure "${s.title}"?`)) return;
    try {
      await api.post(`/Structure/delete?id=${s.id}`);
      structuresByClass[s.inflectionclassid] = await api.get('/Structure/list', { InflectionClassID: s.inflectionclassid }) || [];
    } catch (err: any) {
      alert(err.message || 'Failed to delete structure');
    }
  }

  async function handleImportStructures() {
    if (!structureImportText.trim() || !$selectedLang) return;
    try {
      await api.post('/Structure/import', {
        file: structureImportText.trim(),
        langid: $selectedLang.id
      });
      showImportStructuresModal = false;
      structureImportText = '';
      structuresByClass = {};
      openedClasses = {};
      await loadInflectionClasses($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to import structures');
    }
  }

  async function handleExportStructures() {
    if (!$selectedLang || inflectionClasses.length === 0) {
      alert('No inflection classes to export.');
      return;
    }
    let file = '';
    for (const pc of inflectionClasses) {
      file += '#' + pc.title + '\n';
      try {
        const slots = await api.get('/Structure/list', { InflectionClassID: pc.id }) || [];
        for (const s of slots) {
          file += [s.unimorphtags, s.formula || '', s.title || ''].join('\t') + '\n';
        }
      } catch (err) {
        console.error('Error fetching structure list for export', err);
      }
    }
    const blob = new Blob([file], { type: 'text/tab-separated-values;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `${$selectedLang.title}_Structures.tsv`;
    link.click();
  }

  // --- Reusable Layers & Affixes ---
  async function loadReusableLayers(langId: number) {
    try {
      reusableLayers = await api.get('/Affix/listLayers', { LangId: langId }) || [];
    } catch (err) {
      console.error('Failed to load layers', err);
      reusableLayers = [];
    }
  }

  async function toggleLayerAffixes(layerId: number) {
    openedLayers[layerId] = !openedLayers[layerId];
    if (openedLayers[layerId] && !affixesByLayer[layerId]) {
      try {
        affixesByLayer[layerId] = await api.get('/Affix/listAffixes', { reusablelayerid: layerId }) || [];
      } catch (err) {
        console.error('Failed to load affixes for layer', layerId, err);
        affixesByLayer[layerId] = [];
      }
    }
  }

  function openAddLayer() {
    currentLayerId = 0;
    layerTitle = '';
    showLayerModal = true;
  }

  function openEditLayer(l: any) {
    currentLayerId = l.id;
    layerTitle = l.title;
    showLayerModal = true;
  }

  async function handleSaveLayer() {
    if (!$selectedLang || !layerTitle.trim()) return;
    try {
      if (currentLayerId === 0) {
        await api.post('/Affix/insertLayer', {
          title: layerTitle.trim(),
          langid: $selectedLang.id
        });
      } else {
        await api.post('/Affix/updateLayer', {
          id: currentLayerId,
          title: layerTitle.trim(),
          langid: $selectedLang.id
        });
      }
      showLayerModal = false;
      await loadReusableLayers($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to save layer');
    }
  }

  async function handleDeleteLayer(l: any) {
    if (!confirm(`Delete layer "${l.title}"?`)) return;
    try {
      await api.post(`/Affix/deleteLayer?id=${l.id}`);
      await loadReusableLayers($selectedLang!.id);
    } catch (err: any) {
      alert(err.message || 'Failed to delete layer');
    }
  }

  function openAddAffix(layerId: number) {
    currentAffixId = 0;
    affixLayerId = layerId;
    affixRealization = '';
    affixTags = '';
    affixTitle = '';
    affixOrder = (affixesByLayer[layerId]?.length || 0) + 1;
    showAffixModal = true;
  }

  function openEditAffix(a: any) {
    currentAffixId = a.id;
    affixLayerId = a.reusablelayerid;
    affixRealization = a.realization;
    affixTags = a.unimorphtags || '';
    affixTitle = a.title || '';
    affixOrder = a.order ?? 1;
    showAffixModal = true;
  }

  async function handleSaveAffix() {
    if (!affixRealization.trim() || !affixLayerId) return;
    try {
      if (currentAffixId === 0) {
        await api.post('/Affix/insertAffix', {
          reusablelayerid: affixLayerId,
          realization: affixRealization.trim(),
          unimorphtags: affixTags.trim(),
          title: affixTitle.trim(),
          order: affixOrder
        });
      } else {
        await api.post('/Affix/updateAffix', {
          id: currentAffixId,
          reusablelayerid: affixLayerId,
          realization: affixRealization.trim(),
          unimorphtags: affixTags.trim(),
          title: affixTitle.trim(),
          order: affixOrder
        });
      }
      showAffixModal = false;
      affixesByLayer[affixLayerId] = await api.get('/Affix/listAffixes', { reusablelayerid: affixLayerId }) || [];
    } catch (err: any) {
      alert(err.message || 'Failed to save affix');
    }
  }

  async function handleDeleteAffix(a: any) {
    if (!confirm(`Delete affix "${a.realization}"?`)) return;
    try {
      await api.post(`/Affix/deleteAffix?id=${a.id}`);
      affixesByLayer[a.reusablelayerid] = await api.get('/Affix/listAffixes', { reusablelayerid: a.reusablelayerid }) || [];
    } catch (err: any) {
      alert(err.message || 'Failed to delete affix');
    }
  }

  async function handleImportLayers() {
    if (!layerImportText.trim() || !$selectedLang) return;
    try {
      await api.post('/Affix/import', {
        file: layerImportText.trim(),
        langid: $selectedLang.id
      });
      showImportLayersModal = false;
      layerImportText = '';
      affixesByLayer = {};
      openedLayers = {};
      await loadReusableLayers($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to import reusable layers');
    }
  }

  async function handleExportLayers() {
    if (!$selectedLang || reusableLayers.length === 0) {
      alert('No reusable layers to export.');
      return;
    }
    let file = '';
    for (const ag of reusableLayers) {
      file += '#' + ag.title + '\n';
      try {
        const affixes = await api.get('/Affix/listAffixes', { reusablelayerid: ag.id }) || [];
        for (const a of affixes) {
          file += [a.order ?? 1, a.unimorphtags, a.realization, a.title || ''].join('\t') + '\n';
        }
      } catch (err) {
        console.error('Error fetching affixes for export', err);
      }
    }
    const blob = new Blob([file], { type: 'text/tab-separated-values;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `${$selectedLang.title}_ReusableLayers.tsv`;
    link.click();
  }

  // --- Lexicon (Lemmas) ---
  async function loadLemmas(langId: number) {
    try {
      lemmas = await api.get('/Lemma/list', { LangID: langId }) || [];
    } catch (err) {
      console.error('Failed to load lemmas', err);
      lemmas = [];
    }
  }

  $: filteredLemmas = Array.isArray(lemmas)
    ? (lemmaSearch.trim()
        ? lemmas.filter(l => 
            (l.entry && l.entry.toLowerCase().includes(lemmaSearch.toLowerCase())) ||
            (l.engmeaning && l.engmeaning.toLowerCase().includes(lemmaSearch.toLowerCase())))
        : lemmas)
    : [];

  function openAddLemma() {
    currentLemmaId = 0;
    lemmaEntry = '';
    lemmaClassId = inflectionClasses[0]?.id || 0;
    lemmaMeaning = '';
    lemmaStem1 = '';
    lemmaStem2 = '';
    lemmaStem3 = '';
    lemmaStem4 = '';
    lemmaTags = '';
    lemmaPriority = 1;
    showLemmaModal = true;
  }

  function openEditLemma(l: any) {
    currentLemmaId = l.id;
    lemmaEntry = l.entry;
    lemmaClassId = l.inflectionclassid;
    lemmaMeaning = l.engmeaning || '';
    lemmaStem1 = l.stem1 || '';
    lemmaStem2 = l.stem2 || '';
    lemmaStem3 = l.stem3 || '';
    lemmaStem4 = l.stem4 || '';
    lemmaTags = l.unimorphtags || '';
    lemmaPriority = l.priority !== undefined && l.priority !== null ? Number(l.priority) : 1;
    showLemmaModal = true;
  }

  async function handleSaveLemma() {
    if (!lemmaEntry.trim() || !lemmaClassId) return;
    try {
      if (currentLemmaId === 0) {
        await api.post('/Lemma/insert', {
          entry: lemmaEntry.trim(),
          inflectionclassid: lemmaClassId,
          engmeaning: lemmaMeaning.trim(),
          stem1: lemmaStem1.trim(),
          stem2: lemmaStem2.trim(),
          stem3: lemmaStem3.trim(),
          stem4: lemmaStem4.trim(),
          unimorphtags: lemmaTags.trim(),
          priority: lemmaPriority
        });
      } else {
        await api.post('/Lemma/update', {
          id: currentLemmaId,
          entry: lemmaEntry.trim(),
          inflectionclassid: lemmaClassId,
          engmeaning: lemmaMeaning.trim(),
          stem1: lemmaStem1.trim(),
          stem2: lemmaStem2.trim(),
          stem3: lemmaStem3.trim(),
          stem4: lemmaStem4.trim(),
          unimorphtags: lemmaTags.trim(),
          priority: lemmaPriority
        });
      }
      showLemmaModal = false;
      if ($selectedLang) await loadLemmas($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to save lemma');
    }
  }

  async function handleDeleteLemma(l: any) {
    if (!confirm(`Delete lemma "${l.entry}"?`)) return;
    try {
      await api.post(`/Lemma/delete?id=${l.id}`);
      if ($selectedLang) await loadLemmas($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to delete lemma');
    }
  }

  async function handleExportLemmas() {
    if (!$selectedLang || lemmas.length === 0) {
      alert('No lemmas to export.');
      return;
    }
    let file = ['Entry', 'InflectionClassID', 'EnglishMeaning', 'Stem1', 'Stem2', 'Stem3', 'Stem4', 'Priority'].join('\t') + '\n';
    for (const l of lemmas) {
      file += [l.entry, l.inflectionclassid, l.engmeaning || '', l.stem1 || '', l.stem2 || '', l.stem3 || '', l.stem4 || '', l.priority ?? 1].join('\t') + '\n';
    }
    const blob = new Blob([file], { type: 'text/tab-separated-values;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `${$selectedLang.title}_Lexicon.tsv`;
    link.click();
  }

  // --- Morphophonology Rules ---
  async function loadRules(langId: number) {
    try {
      rules = await api.get('/Morphophonology/list', { LangID: langId }) || [];
    } catch (err) {
      console.error('Failed to load morphophonology rules', err);
      rules = [];
    }
  }

  function openAddRule() {
    currentRuleId = 0;
    ruleTitle = '';
    ruleReplaceFrom = '';
    ruleReplaceTo = '';
    showRuleModal = true;
  }

  function openEditRule(r: any) {
    currentRuleId = r.id;
    ruleTitle = r.title || '';
    ruleReplaceFrom = r.replacefrom || '';
    ruleReplaceTo = r.replaceto || '';
    showRuleModal = true;
  }

  async function handleSaveRule() {
    if (!$selectedLang || !ruleTitle.trim() || !ruleReplaceFrom.trim()) return;
    try {
      if (currentRuleId === 0) {
        await api.post('/Morphophonology/insert', {
          langid: $selectedLang.id,
          title: ruleTitle.trim(),
          replacefrom: ruleReplaceFrom.trim(),
          replaceto: ruleReplaceTo.trim(),
        });
      } else {
        await api.post('/Morphophonology/update', {
          id: currentRuleId,
          langid: $selectedLang.id,
          title: ruleTitle.trim(),
          replacefrom: ruleReplaceFrom.trim(),
          replaceto: ruleReplaceTo.trim(),
        });
      }
      showRuleModal = false;
      await loadRules($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to save rule');
    }
  }

  async function handleDeleteRule(r: any) {
    if (!confirm(`Delete rule "${r.title}"?`)) return;
    try {
      await api.post(`/Morphophonology/delete?id=${r.id}`);
      if ($selectedLang) await loadRules($selectedLang.id);
    } catch (err: any) {
      alert(err.message || 'Failed to delete rule');
    }
  }
</script>

<div class="linguist-studio">
  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; gap: 0.75rem;">
    <div>
      <h1 style="margin: 0; font-size: 1.5rem;">Linguist Studio</h1>
      <p style="color: var(--text-muted); margin: 0.15rem 0 0 0; font-size: 0.88rem;">
        Language Variety: <b>{$selectedLang?.title || 'Selected Variety'}</b>
      </p>
    </div>
  </div>

  <!-- Accordion Tabs -->
  <div class="studio-tabs">
    <button type="button" class="s-tab" class:active={activeAccordion === 'structures'} on:click={() => activeAccordion = 'structures'}>
      <span class="material-icons" style="font-size: 1.1rem;">account_tree</span> Paradigm Structures ({inflectionClasses.length})
    </button>
    <button type="button" class="s-tab" class:active={activeAccordion === 'layers'} on:click={() => activeAccordion = 'layers'}>
      <span class="material-icons" style="font-size: 1.1rem;">layers</span> Reusable Layers ({reusableLayers.length})
    </button>
    <button type="button" class="s-tab" class:active={activeAccordion === 'lexicon'} on:click={() => activeAccordion = 'lexicon'}>
      <span class="material-icons" style="font-size: 1.1rem;">menu_book</span> Lexicon (Lemmas) ({lemmas.length})
    </button>
    <button type="button" class="s-tab" class:active={activeAccordion === 'rules'} on:click={() => activeAccordion = 'rules'}>
      <span class="material-icons" style="font-size: 1.1rem;">rule</span> Morphophonology ({rules.length})
    </button>
  </div>

  <!-- SECTION 1: Inflection Classes & Structures -->
  {#if activeAccordion === 'structures'}
    <div class="card compact-card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; gap: 0.5rem;">
        <h3 style="margin: 0; font-size: 1.05rem;">Paradigm Structures</h3>
        <div style="display: flex; gap: 0.4rem; align-items: center; flex-wrap: wrap;">
          <button type="button" class="secondary small" on:click={handleExportStructures} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">download</span> Export TSV
          </button>
          <button type="button" class="secondary small" on:click={() => showImportStructuresModal = true} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">upload_file</span> Import TSV
          </button>
          <button type="button" class="primary small" on:click={openAddClass} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">add</span> Add Class
          </button>
        </div>
      </div>

      {#if inflectionClasses.length === 0}
        <div class="alert alert-info">No inflection classes defined yet for this variety. Click "Add Class" above.</div>
      {:else}
        <div class="tree-list">
          {#each inflectionClasses as ic}
            <div class="tree-node">
              <div class="tree-node-header">
                <div style="display: flex; align-items: center; gap: 0.4rem;">
                  <button type="button" class="expand-btn" on:click={() => toggleClassStructures(ic.id)}>
                    <span class="material-icons" style="font-size: 1.2rem;">{openedClasses[ic.id] ? 'expand_more' : 'chevron_right'}</span>
                  </button>
                  <strong style="font-size: 0.92rem;">{ic.title}</strong>
                </div>

                <div style="display: flex; gap: 0.3rem; align-items: center;">
                  <button type="button" class="btn-compact-text" on:click={() => openAddStructure(ic.id)}>
                    <span class="material-icons" style="font-size: 0.95rem;">add</span> Slot
                  </button>
                  <button type="button" class="icon-btn edit" title="Edit Class" on:click={() => openEditClass(ic)}>
                    <span class="material-icons">edit</span>
                  </button>
                  <button type="button" class="icon-btn delete" title="Delete Class" on:click={() => handleDeleteClass(ic)}>
                    <span class="material-icons">delete_outline</span>
                  </button>
                </div>
              </div>

              {#if openedClasses[ic.id]}
                <div class="tree-children">
                  {#if !structuresByClass[ic.id] || structuresByClass[ic.id].length === 0}
                    <p style="color: var(--text-muted); font-size: 0.82rem; margin: 0.25rem 0;">No paradigm slots defined in this class yet.</p>
                  {:else}
                    <table class="data-table compact">
                      <thead>
                        <tr>
                          <th style="width: 50px;">Order</th>
                          <th>Slot Title</th>
                          <th>UniMorph Feature Tags</th>
                          <th>Formula</th>
                          <th style="width: 70px; text-align: center;">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {#each structuresByClass[ic.id] as s}
                          <tr>
                            <td>{s.order}</td>
                            <td><strong>{s.title}</strong></td>
                            <td><code style="font-size: 0.82rem;">{s.unimorphtags}</code></td>
                            <td><span class="badge" style="font-size: 0.75rem;">{s.formula || 'Default'}</span></td>
                            <td>
                              <div style="display: flex; gap: 0.25rem; justify-content: center;">
                                <button type="button" class="icon-btn edit" title="Edit" on:click={() => openEditStructure(s)}>
                                  <span class="material-icons">edit</span>
                                </button>
                                <button type="button" class="icon-btn delete" title="Delete" on:click={() => handleDeleteStructure(s)}>
                                  <span class="material-icons">delete_outline</span>
                                </button>
                              </div>
                            </td>
                          </tr>
                        {/each}
                      </tbody>
                    </table>
                  {/if}
                </div>
              {/if}
            </div>
          {/each}
        </div>
      {/if}
    </div>

  <!-- SECTION 2: Reusable Layers & Affixes -->
  {:else if activeAccordion === 'layers'}
    <div class="card compact-card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: gap: 0.5rem;">
        <h3 style="margin: 0; font-size: 1.05rem;">Reusable Layers ({reusableLayers.length})</h3>
        <div style="display: flex; gap: 0.4rem; align-items: center; flex-wrap: wrap;">
          <button type="button" class="secondary small" on:click={handleExportLayers} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">download</span> Export TSV
          </button>
          <button type="button" class="secondary small" on:click={() => showImportLayersModal = true} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">upload_file</span> Import TSV
          </button>
          <button type="button" class="primary small" on:click={openAddLayer} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">add</span> Add Layer
          </button>
        </div>
      </div>

      {#if reusableLayers.length === 0}
        <div class="alert alert-info">No reusable agreement layers defined yet.</div>
      {:else}
        <div class="tree-list">
          {#each reusableLayers as layer}
            <div class="tree-node">
              <div class="tree-node-header">
                <div style="display: flex; align-items: center; gap: 0.4rem;">
                  <button type="button" class="expand-btn" on:click={() => toggleLayerAffixes(layer.id)}>
                    <span class="material-icons" style="font-size: 1.2rem;">{openedLayers[layer.id] ? 'expand_more' : 'chevron_right'}</span>
                  </button>
                  <strong style="font-size: 0.92rem;">{layer.title}</strong>
                </div>

                <div style="display: flex; gap: 0.3rem; align-items: center;">
                  <button type="button" class="btn-compact-text" on:click={() => openAddAffix(layer.id)}>
                    <span class="material-icons" style="font-size: 0.95rem;">add</span> Affix
                  </button>
                  <button type="button" class="icon-btn edit" title="Edit Layer" on:click={() => openEditLayer(layer)}>
                    <span class="material-icons">edit</span>
                  </button>
                  <button type="button" class="icon-btn delete" title="Delete Layer" on:click={() => handleDeleteLayer(layer)}>
                    <span class="material-icons">delete_outline</span>
                  </button>
                </div>
              </div>

              {#if openedLayers[layer.id]}
                <div class="tree-children">
                  {#if !affixesByLayer[layer.id] || affixesByLayer[layer.id].length === 0}
                    <p style="color: var(--text-muted); font-size: 0.82rem; margin: 0.25rem 0;">No affixes in this layer yet.</p>
                  {:else}
                    <table class="data-table compact">
                      <thead>
                        <tr>
                          <th style="width: 55px;">Order</th>
                          <th>Realization</th>
                          <th>UniMorph Feature Tags</th>
                          <th>Title / Alias</th>
                          <th style="width: 70px; text-align: center;">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {#each affixesByLayer[layer.id] as a}
                          <tr>
                            <td>{a.order ?? 1}</td>
                            <td><strong style="color: var(--accent); font-size: 0.98rem;">{a.realization}</strong></td>
                            <td><code style="font-size: 0.82rem;">{a.unimorphtags}</code></td>
                            <td><span style="font-size: 0.85rem; color: var(--text-muted);">{a.title || '-'}</span></td>
                            <td>
                              <div style="display: flex; gap: 0.25rem; justify-content: center;">
                                <button type="button" class="icon-btn edit" title="Edit" on:click={() => openEditAffix(a)}>
                                  <span class="material-icons">edit</span>
                                </button>
                                <button type="button" class="icon-btn delete" title="Delete" on:click={() => handleDeleteAffix(a)}>
                                  <span class="material-icons">delete_outline</span>
                                </button>
                              </div>
                            </td>
                          </tr>
                        {/each}
                      </tbody>
                    </table>
                  {/if}
                </div>
              {/if}
            </div>
          {/each}
        </div>
      {/if}
    </div>

  <!-- SECTION 3: Lexicon (Lemmas) -->
  {:else if activeAccordion === 'lexicon'}
    <div class="card compact-card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; gap: 0.6rem;">
        <h3 style="margin: 0; font-size: 1.05rem;">Lexicon (Lemmas) ({lemmas.length})</h3>
        <div style="display: flex; gap: 0.4rem; flex-wrap: wrap; align-items: center;">
          <input 
            type="text" 
            placeholder="Search entries or gloss..." 
            bind:value={lemmaSearch} 
            style="width: 180px; padding: 0.3rem 0.5rem; font-size: 0.82rem;"
          />
          <button type="button" class="secondary small" on:click={handleExportLemmas} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">download</span> Export TSV
          </button>
          <button type="button" class="secondary small" on:click={() => showImportLemmasModal = true} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">upload_file</span> Import TSV
          </button>
          <button type="button" class="primary small" on:click={openAddLemma} style="padding: 0.35rem 0.6rem; font-size: 0.82rem;">
            <span class="material-icons" style="font-size: 0.95rem;">add</span> Add Lemma
          </button>
        </div>
      </div>

      <div style="overflow-x: auto;">
        <table class="data-table compact">
          <thead>
            <tr>
              <th>Lemma Entry</th>
              <th>Gloss / Meaning</th>
              <th>Inflection Class</th>
              <th style="width: 80px; text-align: center;">Priority</th>
              <th style="width: 70px; text-align: center;">Actions</th>
            </tr>
          </thead>
          <tbody>
            {#if filteredLemmas.length === 0}
              <tr>
                <td colspan="5" style="text-align: center; color: var(--text-muted); padding: 1.25rem;">
                  No lemmas found.
                </td>
              </tr>
            {:else}
              {#each filteredLemmas as l}
                {@const ic = inflectionClasses.find(c => c.id === l.inflectionclassid)}
                <tr>
                  <td><b>{l.entry}</b></td>
                  <td><span style="color: var(--text-muted);">{l.engmeaning || '-'}</span></td>
                  <td><span class="badge primary" style="font-size: 0.75rem; padding: 2px 6px;">{ic ? ic.title : (l.wClass || l.inflectionclassid)}</span></td>
                  <td style="text-align: center;">
                    {#if l.priority === 2}
                      <span class="badge danger" style="font-weight: 600; font-size: 0.72rem; padding: 1px 5px;">High</span>
                    {:else if l.priority === 1}
                      <span class="badge primary" style="font-weight: 600; font-size: 0.72rem; padding: 1px 5px;">Med</span>
                    {:else}
                      <span class="badge" style="color: var(--text-muted); font-size: 0.72rem; padding: 1px 5px;">Low</span>
                    {/if}
                  </td>
                  <td>
                    <div style="display: flex; gap: 0.25rem; justify-content: center;">
                      <button type="button" class="icon-btn edit" title="Edit" on:click={() => openEditLemma(l)}>
                        <span class="material-icons">edit</span>
                      </button>
                      <button type="button" class="icon-btn delete" title="Delete" on:click={() => handleDeleteLemma(l)}>
                        <span class="material-icons">delete_outline</span>
                      </button>
                    </div>
                  </td>
                </tr>
              {/each}
            {/if}
          </tbody>
        </table>
      </div>
    </div>

  <!-- SECTION 4: Morphophonology Rules -->
  {:else if activeAccordion === 'rules'}
    <div class="card compact-card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
        <h3 style="margin: 0; font-size: 1.05rem;">Morphophonology ({rules.length})</h3>
        <button type="button" class="primary small" on:click={openAddRule}>
          <span class="material-icons" style="font-size: 1rem;">add</span> Add Rule
        </button>
      </div>

      <div style="overflow-x: auto;">
        <table class="data-table compact">
          <thead>
            <tr>
              <th>Rule Title</th>
              <th>Replaces From (Source)</th>
              <th>Replaces To (Target)</th>
              <th style="width: 70px; text-align: center;">Actions</th>
            </tr>
          </thead>
          <tbody>
            {#if rules.length === 0}
              <tr>
                <td colspan="4" style="text-align: center; color: var(--text-muted); padding: 1.25rem;">
                  No morphophonological rules defined for this variety yet. Click "Add Rule" to create one.
                </td>
              </tr>
            {:else}
              {#each rules as r}
                <tr>
                  <td><strong>{r.title}</strong></td>
                  <td>
                    <code style="white-space: pre-wrap; font-size: 0.82rem;">{r.replacefrom}</code>
                  </td>
                  <td>
                    <strong style="color: var(--accent); white-space: pre-wrap; font-size: 0.88rem;">{r.replaceto}</strong>
                  </td>
                  <td>
                    <div style="display: flex; gap: 0.25rem; justify-content: center;">
                      <button type="button" class="icon-btn edit" title="Edit" on:click={() => openEditRule(r)}>
                        <span class="material-icons">edit</span>
                      </button>
                      <button type="button" class="icon-btn delete" title="Delete" on:click={() => handleDeleteRule(r)}>
                        <span class="material-icons">delete_outline</span>
                      </button>
                    </div>
                  </td>
                </tr>
              {/each}
            {/if}
          </tbody>
        </table>
      </div>
    </div>
  {/if}
</div>

<!-- Modal: Inflection Class -->
<Modal isOpen={showClassModal} title="{currentClassId === 0 ? 'Add' : 'Edit'} Inflection Class" onClose={() => showClassModal = false}>
  <form on:submit|preventDefault={handleSaveClass}>
    <div class="field small">
      <label for="txtClassTitle">Inflection Class Title *</label>
      <input id="txtClassTitle" type="text" bind:value={classTitle} required placeholder="e.g. Regular Verbs (Conjugation I)" />
    </div>
    <div class="field small">
      <label for="txtClassDesc">Description</label>
      <textarea id="txtClassDesc" bind:value={classDescription} rows="3"></textarea>
    </div>
    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showClassModal = false}>Cancel</button>
      <button type="submit" class="primary">Save Class</button>
    </div>
  </form>
</Modal>

<!-- Modal: Structure -->
<Modal isOpen={showStructureModal} title="{currentStructureId === 0 ? 'Add' : 'Edit'} Paradigm Structure" onClose={() => showStructureModal = false} maxWidth="650px">
  <form on:submit|preventDefault={handleSaveStructure}>
    <div class="field small">
      <label for="txtStructureTitle">Structure Title / Alias *</label>
      <input id="txtStructureTitle" type="text" bind:value={structureTitle} required placeholder="e.g. Present Indicative 1.SG" />
    </div>

    <UniMorphSelector bind:value={structureTags} label="UniMorph Morphosyntactic Features *" />

    <div class="field small">
      <label for="txtStructureFormula">Formula (Optional)</label>
      <input id="txtStructureFormula" type="text" bind:value={structureFormula} placeholder="e.g. S1+A or L" />
      <small style="color: var(--text-muted);">Available placeholders: S1 (stem1), S2 (stem2), S3 (stem3), A (reusable layer), L (lemma)</small>
    </div>

    <div class="field small">
      <label for="cmbStructureLayer">Reusable Agreement Layer (if formula contains A)</label>
      <select id="cmbStructureLayer" bind:value={structureLayerId}>
        <option value={0}>- None -</option>
        {#each reusableLayers as l}
          <option value={l.id}>{l.title}</option>
        {/each}
      </select>
    </div>

    <div class="field small">
      <label for="txtStructureOrder">Display Order</label>
      <input id="txtStructureOrder" type="number" bind:value={structureOrder} min="0" max="100" />
    </div>

    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showStructureModal = false}>Cancel</button>
      <button type="submit" class="primary">Save Structure</button>
    </div>
  </form>
</Modal>

<!-- Modal: Import Structures TSV -->
<Modal isOpen={showImportStructuresModal} title="Import Paradigm Structures (TSV)" onClose={() => showImportStructuresModal = false} maxWidth="700px">
  <div style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 0.75rem;">
    Paste tab-separated lines with class header prefixed by <code>#</code>:<br/>
    <code>#ClassName</code><br/>
    <code>UniMorphTags [tab] Formula [tab] StructureTitle</code>
  </div>
  <div class="field small">
    <textarea bind:value={structureImportText} rows="10"></textarea>
  </div>
  <div class="field-end">
    <button type="button" class="secondary" on:click={() => showImportStructuresModal = false}>Cancel</button>
    <button type="button" class="primary" on:click={handleImportStructures}>Import Structures</button>
  </div>
</Modal>

<!-- Modal: Reusable Layer -->
<Modal isOpen={showLayerModal} title="{currentLayerId === 0 ? 'Add' : 'Edit'} Reusable Layer" onClose={() => showLayerModal = false}>
  <form on:submit|preventDefault={handleSaveLayer}>
    <div class="field small">
      <label for="txtLayerTitle">Layer Title *</label>
      <input id="txtLayerTitle" type="text" bind:value={layerTitle} required placeholder="e.g. Present Agreement Endings" />
    </div>
    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showLayerModal = false}>Cancel</button>
      <button type="submit" class="primary">Save Layer</button>
    </div>
  </form>
</Modal>

<!-- Modal: Affix -->
<Modal isOpen={showAffixModal} title="{currentAffixId === 0 ? 'Add' : 'Edit'} Affix" onClose={() => showAffixModal = false} maxWidth="600px">
  <form on:submit|preventDefault={handleSaveAffix}>
    <div class="field small">
      <label for="txtAffixRealization">Surface Realization (Morpheme Ending/Prefix) *</label>
      <input id="txtAffixRealization" type="text" bind:value={affixRealization} required placeholder="e.g. -im or -imê" />
    </div>

    <UniMorphSelector bind:value={affixTags} label="Agreement UniMorph Feature Tags *" />

    <div class="field small">
      <label for="txtAffixTitle">Title / Alias (Optional)</label>
      <input id="txtAffixTitle" type="text" bind:value={affixTitle} placeholder="e.g. 1st Person Singular" />
    </div>

    <div class="field small">
      <label for="txtAffixOrder">Display Order</label>
      <input id="txtAffixOrder" type="number" bind:value={affixOrder} min="1" max="100" />
    </div>

    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showAffixModal = false}>Cancel</button>
      <button type="submit" class="primary">Save Affix</button>
    </div>
  </form>
</Modal>

<!-- Modal: Import Reusable Layers & Affixes TSV -->
<Modal isOpen={showImportLayersModal} title="Import Reusable Layers & Affixes (TSV)" onClose={() => showImportLayersModal = false} maxWidth="700px">
  <div style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 0.75rem;">
    Paste tab-separated lines with layer header prefixed by <code>#</code>:<br/>
    <code>#LayerName</code><br/>
    <code>Order [tab] UniMorphTags [tab] MorphemeRealization [tab] Title/Alias</code>
  </div>
  <div class="field small">
    <textarea bind:value={layerImportText} rows="10"></textarea>
  </div>
  <div class="field-end">
    <button type="button" class="secondary" on:click={() => showImportLayersModal = false}>Cancel</button>
    <button type="button" class="primary" on:click={handleImportLayers}>Import Layers</button>
  </div>
</Modal>

<!-- Modal: Lemma -->
<Modal isOpen={showLemmaModal} title="{currentLemmaId === 0 ? 'Add' : 'Edit'} Base Lemma" onClose={() => showLemmaModal = false} maxWidth="650px">
  <form on:submit|preventDefault={handleSaveLemma}>
    <div class="field small">
      <label for="txtLemmaEntry">Base Entry / Citation Form *</label>
      <input id="txtLemmaEntry" type="text" bind:value={lemmaEntry} required placeholder="e.g. nivîsîn" />
    </div>

    <div class="field small">
      <label for="cmbLemmaClass">Inflection Class *</label>
      <select id="cmbLemmaClass" bind:value={lemmaClassId} required>
        {#each inflectionClasses as ic}
          <option value={ic.id}>{ic.title}</option>
        {/each}
      </select>
    </div>

    <div class="field small">
      <label for="txtLemmaMeaning">English Meaning / Gloss</label>
      <input id="txtLemmaMeaning" type="text" bind:value={lemmaMeaning} placeholder="e.g. to write" />
    </div>

    <div class="field small">
      <label for="cmbLemmaPriority">Elicitation Priority</label>
      <select id="cmbLemmaPriority" bind:value={lemmaPriority}>
        <option value={2}>High (Top priority in elicitation)</option>
        <option value={1}>Med (Normal)</option>
        <option value={0}>Low (Lower priority)</option>
      </select>
    </div>

    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem;">
      <div class="field small">
        <label for="txtLemmaStem1">Stem 1 (e.g. Present root)</label>
        <input id="txtLemmaStem1" type="text" bind:value={lemmaStem1} placeholder="e.g. nivîs" />
      </div>
      <div class="field small">
        <label for="txtLemmaStem2">Stem 2 (e.g. Past root)</label>
        <input id="txtLemmaStem2" type="text" bind:value={lemmaStem2} placeholder="e.g. nivîsî" />
      </div>
      <div class="field small">
        <label for="txtLemmaStem3">Stem 3 (Optional)</label>
        <input id="txtLemmaStem3" type="text" bind:value={lemmaStem3} />
      </div>
      <div class="field small">
        <label for="txtLemmaStem4">Stem 4 (Optional)</label>
        <input id="txtLemmaStem4" type="text" bind:value={lemmaStem4} />
      </div>
    </div>

    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showLemmaModal = false}>Cancel</button>
      <button type="submit" class="primary">Save Lemma</button>
    </div>
  </form>
</Modal>

<!-- Modal: Morphophonological Rule -->
<Modal isOpen={showRuleModal} title="{currentRuleId === 0 ? 'Add' : 'Edit'} Morphophonological Rule" onClose={() => showRuleModal = false}>
  <form on:submit|preventDefault={handleSaveRule}>
    <div class="field small">
      <label for="txtRuleTitle">Rule Title *</label>
      <input id="txtRuleTitle" type="text" bind:value={ruleTitle} required placeholder="e.g. Glide Insertion or Vowel Harmony" />
    </div>
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
      <div class="field small">
        <label for="txtRuleReplaceFrom">Replaces From (Source, one per line) *</label>
        <textarea id="txtRuleReplaceFrom" bind:value={ruleReplaceFrom} rows="5" required placeholder="aa&#10;ee&#10;iy"></textarea>
      </div>
      <div class="field small">
        <label for="txtRuleReplaceTo">Replaces To (Target, one per line)</label>
        <textarea id="txtRuleReplaceTo" bind:value={ruleReplaceTo} rows="5" placeholder="a&#10;e&#10;î"></textarea>
      </div>
    </div>
    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showRuleModal = false}>Cancel</button>
      <button type="submit" class="primary">Save Rule</button>
    </div>
  </form>
</Modal>

<!-- Modal: Import Lemmas TSV -->
<Modal isOpen={showImportLemmasModal} title="Import Base Lemmas (TSV / CSV)" onClose={() => showImportLemmasModal = false} maxWidth="700px">
  <div style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 0.75rem;">
    Paste tab-separated lines with format: <br/>
    <code>Entry [tab] InflectionClassID [tab] EnglishMeaning [tab] Stem1 [tab] Stem2 [tab] Stem3 [tab] Stem4</code>
  </div>
  <div class="field small">
    <textarea bind:value={lemmaImportText} rows="8" placeholder="nivîsîn&#9;1&#9;to write&#9;nivîs&#9;nivîsî"></textarea>
  </div>
  <div class="field-end">
    <button type="button" class="secondary" on:click={() => showImportLemmasModal = false}>Cancel</button>
    <button type="button" class="primary" on:click={async () => {
      if (!lemmaImportText.trim() || !$selectedLang) return;
      const lines = lemmaImportText.trim().split('\n');
      for (const line of lines) {
        const parts = line.split('\t');
        if (parts.length >= 2) {
          try {
            await api.post('/Lemma/insert', {
              entry: parts[0].trim(),
              inflectionclassid: parseInt(parts[1].trim(), 10) || inflectionClasses[0]?.id || 1,
              engmeaning: parts[2]?.trim() || '',
              stem1: parts[3]?.trim() || '',
              stem2: parts[4]?.trim() || '',
              stem3: parts[5]?.trim() || '',
              stem4: parts[6]?.trim() || ''
            });
          } catch {}
        }
      }
      showImportLemmasModal = false;
      lemmaImportText = '';
      await loadLemmas($selectedLang.id);
    }}>Import Lemmas</button>
  </div>
</Modal>

<style>
  .linguist-studio {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .compact-card {
    padding: 1.25rem;
  }

  .studio-tabs {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;
  }

  .s-tab {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    background: #ffffff;
    border: 1px solid var(--border);
    padding: 0.45rem 0.85rem;
    border-radius: var(--radius);
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-muted);
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .s-tab:hover {
    background: #f8fafc;
    color: var(--text);
  }

  .s-tab.active {
    background: var(--primary);
    color: #ffffff;
    border-color: var(--primary);
  }

  .tree-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .tree-node {
    border: 1px solid var(--border);
    border-radius: var(--radius);
    overflow: hidden;
  }

  .tree-node-header {
    background: #f8fafc;
    padding: 0.45rem 0.75rem;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .expand-btn {
    background: transparent;
    border: none;
    padding: 0;
    cursor: pointer;
    display: flex;
    align-items: center;
    color: var(--text-muted);
  }

  .tree-children {
    padding: 0.65rem 0.75rem;
    background: #ffffff;
    border-top: 1px solid var(--border);
  }

  /* Compact Action Icons */
  .icon-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 26px;
    padding: 0;
    border-radius: 4px;
    border: 1px solid var(--border);
    background: #ffffff;
    color: var(--text-muted);
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .icon-btn .material-icons {
    font-size: 15px;
  }

  .icon-btn:hover {
    background: #f1f5f9;
    color: var(--text);
    border-color: #cbd5e1;
  }

  .icon-btn.edit {
    min-width: 26px;
  }

  .icon-btn.edit:hover {
    background: #eff6ff;
    color: var(--primary);
    border-color: #93c5fd;
  }

  .icon-btn.delete:hover {
    background: #fef2f2;
    color: #dc2626;
    border-color: #fca5a5;
  }

  .btn-compact-text {
    display: inline-flex;
    align-items: center;
    gap: 0.2rem;
    background: #ffffff;
    border: 1px solid var(--border);
    color: var(--text);
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    font-size: 0.78rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .btn-compact-text:hover {
    background: #f8fafc;
    border-color: var(--primary);
    color: var(--primary);
  }

  /* Compact Table Overrides */
  .data-table.compact th {
    padding: 0.4rem 0.6rem;
    font-size: 0.8rem;
  }

  .data-table.compact td {
    padding: 0.35rem 0.6rem;
    font-size: 0.875rem;
  }
</style>
