<script lang="ts">
  import { onMount } from 'svelte';
  import { selectedLang } from '../../lib/stores/language';
  import { currentMetaLang, dictionaries, t } from '../../lib/stores/i18n';
  import { UM_tag2word, UM_Sort } from '../../lib/unimorph';
  import { api } from '../../lib/api';

  // 0 = Not confident (Conversational 1-by-1)
  // 1 = Basic (Batch table with Aliases)
  // 2 = Expert (Batch table with Linguistic terms)
  let grammarLevel: number = 0;
  let page = 1;
  let batchOrder: 'Lemma' | 'Structure' = 'Lemma';
  let isLoading = false;
  let isSubmitting = false;

  // Level 0 (Conversational 1-by-1) state
  let currentCell: any = null;
  let questionPrompt = '';
  let samples: any[] = [];
  let submittedForm = '';
  let suggestions: Array<{ source: 'formula' | 'llm' | 'nn'; text: string }> = [];
  let isCompleted = false;

  // Level 1 & 2 (Batch Table) state
  let batchTableData: any = null;
  let batchPool: any[] = [];
  let batchHeader: { title: string; subtitle?: string; gloss?: string } | null = null;
  let cellValues: Record<string, string> = {};
  let stats: { countAll: number; remaining: number; percent: number } | null = null;

  onMount(async () => {
    const savedLevel = localStorage.getItem('cm_grammar_level') ?? localStorage.getItem('myLevel');
    if (savedLevel !== null && savedLevel !== undefined) {
      grammarLevel = parseInt(savedLevel, 10) || 0;
    }
    if ($selectedLang) {
      await loadData();
    }
  });

  $: if ($selectedLang || $currentMetaLang) {
    page = 1;
    loadData();
  }

  function handleLevelChange(e: Event) {
    const target = e.target as HTMLSelectElement;
    grammarLevel = parseInt(target.value, 10);
    localStorage.setItem('cm_grammar_level', String(grammarLevel));
    localStorage.setItem('myLevel', String(grammarLevel));
    page = 1;
    loadData();
  }

  function handleOrderChange(e: Event) {
    const target = e.target as HTMLSelectElement;
    batchOrder = target.value as 'Lemma' | 'Structure';
    page = 1;
    loadData();
  }

  $: metaDict = dictionaries[$currentMetaLang] || dictionaries.en;
  $: textDir = metaDict.dir || 'ltr';

  async function loadData() {
    if (!$selectedLang) return;
    isLoading = true;

    try {
      if (grammarLevel === 0) {
        await loadConversationalCell();
      } else {
        await Promise.all([loadBatchTable(), loadStats()]);
      }
    } catch (err) {
      console.error('Failed to load elicitation data', err);
    } finally {
      isLoading = false;
    }
  }

  // --- Level 0: Conversational Single Item ---
  async function loadConversationalCell() {
    suggestions = [];
    submittedForm = '';

    const data = await api.get('/Elicit/listForEntry', {
      langid: $selectedLang.id,
      page: page,
      metalang: $currentMetaLang,
    });

    if (data && data.r && data.q && data.q.question) {
      currentCell = data.r;
      isCompleted = false;

      // 1. Calculate formula suggestion
      if (currentCell.formula) {
        const w = evaluateFormula(
          currentCell.formula,
          currentCell.lemma,
          currentCell.stem1,
          currentCell.stem2,
          currentCell.stem3,
          currentCell.stem4,
          currentCell.a
        );
        if (w) suggestions.push({ source: 'formula', text: w });
      }

      // 2. Question Prompt
      let q = data.q.question;
      const gloss = currentCell.eng ? currentCell.eng + ' / ' : '';
      q = q.replace(/XXX/g, `<b>${gloss}${currentCell.lemma}</b>`);
      questionPrompt = q;

      // 3. Samples Context
      if (data.s && Array.isArray(data.s)) {
        samples = data.s;
      } else {
        samples = [];
      }
      if (samples.length > 0) {
        fetchAiSuggestions(currentCell, samples);
      }
    } else {
      currentCell = null;
      isCompleted = true;
    }
  }

  async function handleSingleSubmit() {
    if (!currentCell || !submittedForm.trim()) return;
    isSubmitting = true;

    try {
      await api.post('/Cell/insert', {
        lemmaid: currentCell.lemmaid,
        structureid: currentCell.structureid,
        affixid: currentCell.affixid || 0,
        langid: $selectedLang.id,
        submitted: submittedForm.trim(),
      });

      page += 1;
      await loadConversationalCell();
    } catch (err: any) {
      alert(err.message || 'Failed to submit form');
    } finally {
      isSubmitting = false;
    }
  }

  // --- Level 1 & 2: Batch Table ---
  async function loadBatchTable() {
    cellValues = {};
    const endpoint = batchOrder === 'Structure' ? '/Elicit/EntryGetTableByStructure' : '/Elicit/EntryGetTableByLemma';
    const data = await api.get(endpoint, {
      langid: $selectedLang.id,
      page: page,
    });

    if (data && data.pool && data.pool.length > 0) {
      batchTableData = data;
      batchPool = data.pool;

      if (batchOrder === 'Structure' && data.structure) {
        batchHeader = {
          title: data.structure.title,
          subtitle: data.structure.unimorphtags,
        };
      } else if (data.lemma) {
        batchHeader = {
          title: data.lemma.entry,
          subtitle: data.lemma.unimorphtags,
          gloss: data.lemma.engmeaning,
        };
      }

      // Precompute formula defaults for each row
      for (const item of batchPool) {
        const formula = batchOrder === 'Structure' ? data.structure?.formula : item.formula;
        const lemma = batchOrder === 'Structure' ? item.lemma : data.lemma?.entry;
        const stem1 = batchOrder === 'Structure' ? item.stem1 : data.lemma?.stem1;
        const stem2 = batchOrder === 'Structure' ? item.stem2 : data.lemma?.stem2;
        const stem3 = batchOrder === 'Structure' ? item.stem3 : data.lemma?.stem3;
        const stem4 = batchOrder === 'Structure' ? item.stem4 : data.lemma?.stem4;
        const aff = item.a || '';

        const key = getRowKey(item);
        cellValues[key] = evaluateFormula(formula, lemma, stem1, stem2, stem3, stem4, aff);
      }

      isCompleted = false;
    } else {
      batchPool = [];
      batchHeader = null;
      isCompleted = true;
    }
  }

  async function loadStats() {
    if (!$selectedLang) return;
    try {
      const s = await api.get('/Elicit/GetStats', { langid: $selectedLang.id });
      if (s && s.countAll > 0) {
        const pct = Math.ceil(((s.countAll - s.remaining) * 100) / s.countAll);
        stats = { countAll: s.countAll, remaining: s.remaining, percent: pct };
      }
    } catch {}
  }

  function evaluateFormula(formula: string, lemma: string = '', stem1: string = '', stem2: string = '', stem3: string = '', stem4: string = '', agg: string = ''): string {
    if (!formula) return lemma || stem1 || '';
    let w = formula;
    w = w.replace(/L/g, lemma || '');
    w = w.replace(/S1/g, stem1 || '');
    w = w.replace(/S2/g, stem2 || '');
    w = w.replace(/S3/g, stem3 || '');
    w = w.replace(/S4/g, stem4 || '');
    if (/A.+A/.test(w)) {
      const parts = (agg || '').split('#');
      w = w.replace(/A\+(.+)\+A/g, (parts[0] || '') + '$1' + (parts[1] || ''));
    } else {
      w = w.replace(/(\+A|A\+)/g, agg || '');
    }
    w = w.replace(/\+/g, '').replace(/0/g, '');
    return w.trim();
  }

  function getRowKey(item: any): string {
    const lid = item.lemmaid || batchTableData?.lemma?.id || 0;
    const sid = item.structureid || batchTableData?.structure?.id || 0;
    const aid = item.affixid || 0;
    return `${lid}_${sid}_${aid}`;
  }

  async function submitSingleRow(item: any) {
    const key = getRowKey(item);
    const val = (cellValues[key] || '').trim();
    if (!val) return;

    const lid = item.lemmaid || batchTableData?.lemma?.id || 0;
    const sid = item.structureid || batchTableData?.structure?.id || 0;
    const aid = item.affixid || 0;

    try {
      await api.post('/Cell/insert', {
        lemmaid: lid,
        structureid: sid,
        affixid: aid,
        langid: $selectedLang.id,
        submitted: val,
      });

      batchPool = batchPool.filter(x => getRowKey(x) !== key);
      if (batchPool.length === 0) {
        page += 1;
        await loadBatchTable();
      }
    } catch (err: any) {
      alert(err.message || 'Failed to submit form');
    }
  }

  async function handleBatchSubmitAll() {
    if (batchPool.length === 0) return;
    isSubmitting = true;

    const cellsToSend = batchPool
      .map(item => {
        const key = getRowKey(item);
        const val = (cellValues[key] || '').trim();
        const lid = item.lemmaid || batchTableData?.lemma?.id || 0;
        const sid = item.structureid || batchTableData?.structure?.id || 0;
        const aid = item.affixid || 0;
        return {
          lemmaid: lid,
          structureid: sid,
          affixid: aid,
          langid: $selectedLang.id,
          submitted: val,
        };
      })
      .filter(c => c.submitted.length > 0);

    try {
      await api.post('/Cell/batchInsert', cellsToSend);
      page += 1;
      await loadData();
    } catch (err: any) {
      alert(err.message || 'Failed to submit batch forms');
    } finally {
      isSubmitting = false;
    }
  }

  async function fetchAiSuggestions(cell: any, sampleList: any[]) {
    try {
      const res = await api.post('/LLM/getSuggestionFromLLM', {
        curLemma: { lemma: cell.lemma, stem1: cell.stem1, stem2: cell.stem2, stem3: cell.stem3 },
        samples: sampleList,
        lang: $selectedLang?.title || '',
      });
      if (res && typeof res === 'object') {
        for (const [model, val] of Object.entries(res)) {
          const text = String(val).trim();
          if (text && !suggestions.some(s => s.text === text)) {
            suggestions = [...suggestions, { source: 'llm', text }];
          }
        }
      }
    } catch {}
  }
</script>

<div class="elicit-page" dir={textDir}>
  <!-- Top Bar -->
  <div class="card" style="margin-bottom: 0.25rem;">
    <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem;">
      <div>
        <h1 style="margin: 0;">Elicitation</h1>
        <p style="color: var(--text-muted); margin: 0.25rem 0 0 0;">
          Collecting forms for: <b>{$selectedLang?.title || 'Selected Variety'}</b>
        </p>
      </div>

      <!-- Controls: Level & Order -->
      <div style="display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
        <div style="display: flex; align-items: center; gap: 0.4rem;">
          <label for="cmbGrammarLevel" style="font-size: 0.85rem; font-weight: 600; margin: 0;">Grammar Knowledge:</label>
          <select id="cmbGrammarLevel" value={grammarLevel} on:change={handleLevelChange} style="width: auto; font-weight: 500;">
            <option value={0}>Not confident (Conversational 1-by-1)</option>
            <option value={1}>Basic (Batch Table with Aliases)</option>
            <option value={2}>Expert (Batch Table with Linguistic Terms)</option>
          </select>
        </div>

        {#if grammarLevel !== 0}
          <div style="display: flex; align-items: center; gap: 0.4rem;">
            <select value={batchOrder} on:change={handleOrderChange} style="width: auto;">
              <option value="Lemma">By Lemma</option>
              <option value="Structure">By Structure</option>
            </select>

            <div style="display: flex; gap: 0.25rem;">
              <button type="button" class="secondary small" on:click={() => { if (page > 1) { page -= 1; loadData(); } }} disabled={page <= 1}>
                <span class="material-icons">chevron_left</span>
              </button>
              <span style="font-size: 0.85rem; font-weight: 600; align-self: center; padding: 0 0.25rem;">Pg {page}</span>
              <button type="button" class="secondary small" on:click={() => { page += 1; loadData(); }}>
                <span class="material-icons">chevron_right</span>
              </button>
            </div>
          </div>
        {/if}
      </div>
    </div>

    <!-- Progress Bar (for Batch Modes) -->
    {#if grammarLevel !== 0 && stats}
      <div style="margin-top: 1rem; padding-top: 0.75rem; border-top: 1px solid var(--border);">
        <div style="display: flex; justify-content: space-between; font-size: 0.8rem; color: var(--text-muted); margin-bottom: 0.35rem;">
          <span>Progress</span>
          <span><b>{stats.countAll - stats.remaining}</b> of <b>{stats.countAll}</b> ({stats.percent}%)</span>
        </div>
        <div style="background: #e2e8f0; height: 8px; border-radius: 4px; overflow: hidden;">
          <div style="background: var(--primary); height: 100%; width: {stats.percent}%; transition: width 0.3s ease;"></div>
        </div>
      </div>
    {/if}
  </div>

  {#if isLoading}
    <div class="card" style="text-align: center; padding: 4rem;">
      <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 2.5rem; color: var(--primary);">sync</span>
      <p style="margin-top: 1rem;">Loading elicitation items...</p>
    </div>
  {:else if isCompleted}
    <div class="card" style="text-align: center; padding: 3rem 1.5rem;" dir={textDir}>
      <span class="material-icons" style="font-size: 3.5rem; color: var(--success);">check_circle</span>
      <h2>{metaDict.elicit_end || 'We are out of items for now. Please come back later.'}</h2>
      <div style="display: flex; justify-content: center; gap: 0.75rem; margin-top: 1.5rem;">
        <button type="button" class="secondary" on:click={() => { page = 1; loadData(); }}>
          <span class="material-icons">refresh</span> Start from Page 1
        </button>
        <a href="/app/dashboard" class="button primary">Back to Dashboard</a>
      </div>
    </div>
  {:else if grammarLevel === 0 && currentCell}
    <!-- LEVEL 0: Conversational Single-Item Card -->
    <div class="card elicit-card" dir={textDir}>
      <div class="prompt-box">
        <span class="material-icons" style="color: var(--primary); font-size: 1.6rem;">help_outline</span>
        <div style="flex: 1;">
          <h3 style="margin: 0 0 0.35rem 0; font-size: 0.95rem; color: var(--text-muted);">
            {metaDict.elicit_instructions || 'Please answer the prompt question:'}
          </h3>
          <div style="font-size: 1.15rem; line-height: 1.5; font-weight: 500;">
            {@html questionPrompt}
          </div>
        </div>
      </div>

      <!-- Quick AI / Formula Suggestions -->
      {#if suggestions.length > 0}
        <div class="suggestions-container">
          <span style="font-size: 0.85rem; font-weight: 600; color: var(--text-muted);">
            {metaDict.elicit_suggestions || 'Suggestions:'}
          </span>
          <div style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
            {#each suggestions as s}
              <button 
                type="button" 
                class="suggestion-chip" 
                on:click={() => submittedForm = s.text}
              >
              <div>
                <div>{s.text}</div>
                <div class="sug-tag">[{s.source}]</div>
              </div>
              </button>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Form Submission Input -->
      <form on:submit|preventDefault={handleSingleSubmit} style="margin-top: 1.5rem;">
        <div class="field">
          <label for="txtSubmittingForm" style="font-weight: 600; font-size: 0.95rem;">
            {metaDict.elicit_answer || 'Answer'}: *
          </label>
          <input 
            type="text" 
            id="txtSubmittingForm" 
            bind:value={submittedForm} 
            placeholder="Type here or click a suggestion..." 
            style="font-size: 1.2rem; font-weight: 600; padding: 0.75rem 1rem; unicode-bidi: plaintext;"
            required
          />
        </div>

        <div style="display: flex; gap: 0.75rem; margin-top: 1.25rem;">
          <button 
            type="submit" 
            class="button primary" 
            disabled={isSubmitting || !submittedForm.trim()}
            style="flex: 1; padding: 0.85rem; font-size: 1rem;"
          >
            <span class="material-icons">send</span> {metaDict.q_submit || 'Submit'}
          </button>
          <button 
            type="button" 
            class="button secondary" 
            on:click={() => { page += 1; loadConversationalCell(); }}
            style="padding: 0.85rem;"
          >
            <span class="material-icons">skip_next</span> {metaDict.elicit_skip || 'Skip'}
          </button>
        </div>
      </form>

      <!-- Samples Context -->
      {#if samples.length > 0}
        <div class="samples-box">
          <span style="font-size: 0.85rem; font-weight: 600; color: var(--text-muted);">
            {metaDict.elicit_samples || 'Context samples from other bases'}:
          </span>
          <div class="samples-list">
            {#each samples as s}
              <div class="sample-item">
                <strong style="color: var(--primary);">{s.form}</strong>
                <span style="color: var(--text-muted); font-size: 0.85rem;">({metaDict.elicit_from || 'from'} <em>{s.lemma}</em>)</span>
              </div>
            {/each}
          </div>
        </div>
      {/if}
    </div>
  {:else if (grammarLevel === 1 || grammarLevel === 2) && batchHeader}
    <!-- LEVEL 1 & 2: Batch Table View -->
    <div class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; flex-wrap: wrap; gap: 1rem;">
        <div>
          <h2 style="margin: 0; display: inline-flex; align-items: center; gap: 0.5rem;">
            {batchHeader.title}
            {#if batchHeader.gloss}<small style="color: var(--text-muted); font-size: 0.95rem; font-weight: normal;">({batchHeader.gloss})</small>{/if}
          </h2>
          {#if batchHeader.subtitle}
            <div style="font-size: 0.85rem; color: var(--text-muted); margin-top: 0.2rem;">
              {batchHeader.subtitle}
            </div>
          {/if}
        </div>

        <button type="button" class="primary" on:click={handleBatchSubmitAll} disabled={isSubmitting || batchPool.length === 0}>
          <span class="material-icons">done_all</span> Submit All on Page ({batchPool.length})
        </button>
      </div>

      <div style="overflow-x: auto;">
        <table class="data-table">
          <thead>
            <tr>
              {#if batchOrder === 'Structure'}
                <th>Base Lemma</th>
              {/if}

              <!-- Header label changes depending on Level 1 (Alias) vs Level 2 (Linguistic Terms) -->
              {#if grammarLevel === 1}
                <th>Structure & Affix Alias</th>
              {:else}
                <th>Linguistic Features (UniMorph)</th>
                <th>UniMorph Tags</th>
              {/if}

              <th>Inflected Form (Editable)</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {#each batchPool as item}
              {@const key = getRowKey(item)}
              {@const aliasTitle = item.stitle ? (item.atitle ? `${item.stitle} (${item.atitle})` : item.stitle) : (item.atitle || '')}
              {@const fullTags = item.tags || (item.ltags ? (item.tags ? `${item.ltags};${item.tags}` : item.ltags) : '')}
              <tr>
                {#if batchOrder === 'Structure'}
                  <td><strong>{item.lemma}</strong></td>
                {/if}

                {#if grammarLevel === 1}
                  <!-- LEVEL 1: Shown in Alias Titles -->
                  <td style="unicode-bidi: plaintext; color: var(--text);">
                    <strong>{aliasTitle}</strong>
                  </td>
                {:else}
                  <!-- LEVEL 2: Shown in Full Linguistic Terms -->
                  <td>
                    <span>{UM_tag2word(fullTags, $selectedLang?.code)}</span>
                  </td>
                  <td>
                    <code class="tag-badge" style="font-size: 0.8rem;">{fullTags}</code>
                  </td>
                {/if}

                <td style="min-width: 220px;">
                  <input 
                    type="text" 
                    bind:value={cellValues[key]} 
                    placeholder="Enter inflected form..." 
                    style="padding: 0.1rem 0.75rem; font-size: 1.1rem; unicode-bidi: plaintext; text-height: 2.5rem; line-height: 2.5rem"
                  />
                </td>
                <td>
                  <button 
                    type="button" 
                    class="secondary small" 
                    on:click={() => submitSingleRow(item)}
                    title="Submit this form"
                  >
                    <span class="material-icons">done</span>
                  </button>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {/if}
</div>

<style>
  .elicit-page {
    max-width: 900px;
    margin: 0 auto;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
  }

  .elicit-card {
    padding: 1rem;
  }

  .prompt-box {
    display: flex;
    gap: 1rem;
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1.25rem;
    margin-bottom: 1.25rem;
  }

  .suggestions-container {
    margin: 1rem 0;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .suggestion-chip {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    background: #eff6ff;
    border: 1px solid #bfdbfe;
    color: #1e3a8a;
    padding: 0.4rem 0.75rem;
    border-radius: 6px;
    font-size: 0.95rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .suggestion-chip:hover {
    background: #dbeafe;
    border-color: var(--primary);
  }

  .sug-tag {
    display: block;
    font-size: 0.5rem;
    color: #aaa;
    margin-top: 0.3rem;
  }

  .samples-box {
    margin-top: 1.75rem;
    padding-top: 1.25rem;
    border-top: 1px solid var(--border);
  }

  .samples-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.6rem;
    margin-top: 0.5rem;
  }

  .sample-item {
    background: #f8fafc;
    border: 1px solid var(--border);
    padding: 0.35rem 0.75rem;
    border-radius: 6px;
    font-size: 0.9rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
