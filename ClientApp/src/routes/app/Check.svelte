<script lang="ts">
  import { onMount } from 'svelte';
  import { selectedLang } from '../../lib/stores/language';
  import { currentMetaLang, dictionaries, t } from '../../lib/stores/i18n';
  import { UM_tag2word } from '../../lib/unimorph';
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
  let currentItem: any = null;
  let questionPrompt = '';
  let isCompleted = false;

  // Level 1 & 2 (Batch Table) state
  let batchTableData: any = null;
  let tablePool: any[] = [];
  let tableHeaderInfo: { title: string; subtitle?: string; wiktionaryUrl?: string } | null = null;

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
        await loadConversationalItem();
      } else if (batchOrder === 'Lemma') {
        await loadTableByLemma();
      } else {
        await loadTableByStructure();
      }
    } catch (err) {
      console.error('Failed to load check data', err);
    } finally {
      isLoading = false;
    }
  }

  // --- Level 0: Conversational Single Item ---
  async function loadConversationalItem() {
    const data = await api.get('/Elicit/listForCheck', {
      langid: $selectedLang.id,
      page: page,
      metalang: $currentMetaLang,
    });

    if (data && data.r && data.q && data.q.question) {
      currentItem = data.r;
      isCompleted = false;

      let q = data.q.question;
      const gloss = currentItem.eng ? currentItem.eng + ' / ' : '';
      q = q.replace(/XXX/g, `<b>${gloss}${currentItem.lemma}</b>`);
      questionPrompt = q;
    } else {
      currentItem = null;
      isCompleted = true;
    }
  }

  async function handleRateConversational(approve: boolean) {
    if (!currentItem) return;
    isSubmitting = true;
    const cid = currentItem.cellid || currentItem.id;

    try {
      const endpoint = approve ? '/Cell/approve' : '/Cell/disapprove';
      await api.post(endpoint, { cellid: cid });
      page += 1;
      await loadConversationalItem();
    } catch (err: any) {
      alert(err.message || 'Failed to submit rating');
    } finally {
      isSubmitting = false;
    }
  }

  // --- Level 1 & 2: Batch Table (By Lemma) ---
  async function loadTableByLemma() {
    const data = await api.get('/Elicit/CheckGetTableByLemma', {
      langid: $selectedLang.id,
      page: page,
    });

    if (data && data.lemma && data.pool && data.pool.length > 0) {
      batchTableData = data;
      tablePool = data.pool;
      tableHeaderInfo = {
        title: data.lemma.entry,
        subtitle: data.lemma.engmeaning,
        wiktionaryUrl: `https://en.wiktionary.org/wiki/${encodeURIComponent(data.lemma.entry)}`
      };
      isCompleted = false;
    } else {
      tablePool = [];
      tableHeaderInfo = null;
      isCompleted = true;
    }
  }

  // --- Level 1 & 2: Batch Table (By Structure) ---
  async function loadTableByStructure() {
    const data = await api.get('/Elicit/CheckGetTableByStructure', {
      langid: $selectedLang.id,
      page: page,
    });

    if (data && data.structure && data.pool && data.pool.length > 0) {
      batchTableData = data;
      tablePool = data.pool;
      tableHeaderInfo = {
        title: data.structure.title,
        subtitle: data.structure.unimorphtags,
      };
      isCompleted = false;
    } else {
      tablePool = [];
      tableHeaderInfo = null;
      isCompleted = true;
    }
  }

  async function rateSingleTableCell(cellid: number, approve: boolean) {
    try {
      const endpoint = approve ? '/Cell/approve' : '/Cell/disapprove';
      await api.post(endpoint, { cellid });
      tablePool = tablePool.filter(c => c.cellid !== cellid);
      if (tablePool.length === 0) {
        page += 1;
        await loadData();
      }
    } catch (err: any) {
      alert(err.message || 'Failed to rate cell');
    }
  }

  async function handleApproveAll() {
    if (tablePool.length === 0) return;
    isSubmitting = true;
    try {
      const cells = tablePool.map(c => ({ cellid: c.cellid }));
      await api.post('/Cell/batchApprove', cells);
      page += 1;
      await loadData();
    } catch (err: any) {
      alert(err.message || 'Failed to batch approve');
    } finally {
      isSubmitting = false;
    }
  }

  function prevPage() {
    if (page > 1) {
      page -= 1;
      loadData();
    }
  }

  function nextPage() {
    page += 1;
    loadData();
  }
</script>

<div class="check-page" dir={textDir}>
  <!-- Top Bar -->
  <div class="card" style="margin-bottom: 1.5rem;">
    <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem;">
      <div>
        <h1 style="margin: 0;">Verification & Quality Check</h1>
        <p style="color: var(--text-muted); margin: 0.25rem 0 0 0;">
          Validating submissions for: <b>{$selectedLang?.title || 'Selected Variety'}</b>
        </p>
      </div>

      <!-- Controls: Level & Order -->
      <div style="display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
        <div style="display: flex; align-items: center; gap: 0.4rem;">
          <label for="cmbCheckGrammarLevel" style="font-size: 0.85rem; font-weight: 600; margin: 0;">Grammar Knowledge:</label>
          <select id="cmbCheckGrammarLevel" value={grammarLevel} on:change={handleLevelChange} style="width: auto; font-weight: 500;">
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
              <button type="button" class="secondary small" on:click={prevPage} disabled={page <= 1}>
                <span class="material-icons">chevron_left</span>
              </button>
              <span style="font-size: 0.85rem; font-weight: 600; align-self: center; padding: 0 0.25rem;">Pg {page}</span>
              <button type="button" class="secondary small" on:click={nextPage}>
                <span class="material-icons">chevron_right</span>
              </button>
            </div>
          </div>
        {/if}
      </div>
    </div>
  </div>

  {#if isLoading}
    <div class="card" style="text-align: center; padding: 4rem;">
      <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 2.5rem; color: var(--primary);">sync</span>
      <p style="margin-top: 1rem;">Loading items for verification...</p>
    </div>
  {:else if isCompleted}
    <div class="card" style="text-align: center; padding: 3rem 1.5rem;" dir={textDir}>
      <span class="material-icons" style="font-size: 3.5rem; color: var(--success);">done_all</span>
      <h2>{metaDict.elicit_end || 'We are out of items for now. Please come back later.'}</h2>
      <div style="display: flex; justify-content: center; gap: 0.75rem; margin-top: 1.5rem;">
        <button type="button" class="secondary" on:click={() => { page = 1; loadData(); }}>
          <span class="material-icons">refresh</span> Check from Page 1
        </button>
        <a href="/app/dashboard" class="button primary">Back to Dashboard</a>
      </div>
    </div>
  {:else if grammarLevel === 0 && currentItem}
    <div class="card check-card" dir={textDir}>
      <div class="prompt-box">
        <span class="material-icons" style="color: var(--primary); font-size: 1.6rem;">help_outline</span>
        <div style="flex: 1;">
          <div style="font-size: 1.15rem; line-height: 1.5; font-weight: 500;">
            {@html questionPrompt}
          </div>
          <div style="font-size: 1.5rem; line-height: 1.5; margin-top: 0.75rem; padding: 0.6rem 0.85rem; text-align: center;">
            {@html (metaDict.check_instructions || 'Is XXX the answer?').replace('XXX', `«<b style="color: var(--primary);">${currentItem.submitted}</b>»`)}
          </div>
        </div>
      </div>

      <!-- Rating Actions -->
      <div class="rating-actions" style="margin-bottom: 1.25rem;">
        <button 
          type="button" 
          class="button primary" 
          style="background-color: var(--success); flex: 1; padding: 0.5rem; font-size: 1rem;"
          on:click={() => handleRateConversational(true)}
          disabled={isSubmitting}
        >
          <span class="material-icons">check_circle</span> {metaDict.check_yes || 'Yes'}
        </button>

        <button 
          type="button" 
          class="button danger" 
          style="flex: 1; padding: 0.5rem; font-size: 1rem;"
          on:click={() => handleRateConversational(false)}
          disabled={isSubmitting}
        >
          <span class="material-icons">cancel</span> {metaDict.check_no || 'No'}
        </button>

        <button 
          type="button" 
          class="button secondary" 
          on:click={nextPage}
          style="padding: 0.5rem;"
        >
          <span class="material-icons">skip_next</span> {metaDict.elicit_skip || 'Skip'}
        </button>
      </div>
      
      <div class="linguistic-info-pill">
        <div>
          <span style="color: var(--text-muted); padding-inline-end: 0.4rem;">Lemma:</span> <strong>{currentItem.lemma}</strong>
          {#if currentItem.eng}<span style="color: var(--text-muted);"> ({currentItem.eng})</span>{/if}
        </div>
        <div style="display: flex; align-items: center; gap: 0.5rem;">
          <code class="tag-badge">{currentItem.tags}</code>
          <span style="font-size: 0.85rem; color: var(--text-muted);">({UM_tag2word(currentItem.tags, $selectedLang?.code)})</span>
        </div>
      </div>
    </div>
  {:else if (grammarLevel === 1 || grammarLevel === 2) && tableHeaderInfo}
    <!-- LEVEL 1 & 2: Batch Table View -->
    <div class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; flex-wrap: wrap; gap: 1rem;">
        <div>
          <h2 style="margin: 0; display: inline-flex; align-items: center; gap: 0.5rem;">
            {tableHeaderInfo.title}
            {#if tableHeaderInfo.subtitle}
              <small style="color: var(--text-muted); font-size: 0.95rem; font-weight: normal;">({tableHeaderInfo.subtitle})</small>
            {/if}
          </h2>
          {#if tableHeaderInfo.wiktionaryUrl}
            <div style="margin-top: 0.25rem;">
              <a href={tableHeaderInfo.wiktionaryUrl} target="_blank" rel="noopener noreferrer" style="font-size: 0.85rem; color: var(--primary);">
                View on Wiktionary &nearr;
              </a>
            </div>
          {/if}
        </div>

        <button type="button" class="primary small" on:click={handleApproveAll} disabled={isSubmitting || tablePool.length === 0}>
          <span class="material-icons">done_all</span> Approve All on Page ({tablePool.length})
        </button>
      </div>

      <div style="overflow-x: auto;">
        <table class="data-table">
          <thead>
            <tr>
              {#if batchOrder === 'Structure'}
                <th>Lemma</th>
              {/if}

              <!-- Header label changes depending on Level 1 (Alias) vs Level 2 (Linguistic Terms) -->
              {#if grammarLevel === 1}
                <th>Structure & Affix Alias</th>
              {:else}
                <th>Linguistic Features (UniMorph)</th>
                <th>UniMorph Tags</th>
              {/if}

              <th>Submitted Form</th>
              <th>Review Actions</th>
            </tr>
          </thead>
          <tbody>
            {#each tablePool as item}
              {@const aliasTitle = item.stitle ? (item.atitle ? `${item.stitle} (${item.atitle})` : item.stitle) : (item.atitle || '')}
              <tr>
                {#if batchOrder === 'Structure'}
                  <td><strong>{item.lemma}</strong></td>
                {/if}

                {#if grammarLevel === 1}
                  <!-- LEVEL 1: Shown in Alias Titles -->
                  <td>
                    <strong>{aliasTitle}</strong>
                    {#if batchOrder === 'Lemma' && item.lemma && item.lemma !== tableHeaderInfo.title}
                      <small style="color: var(--text-muted);"> ({item.lemma})</small>
                    {/if}
                  </td>
                {:else}
                  <!-- LEVEL 2: Shown in Full Linguistic Terms -->
                  <td>
                    <strong>{UM_tag2word(item.tags, $selectedLang?.code)}</strong>
                    {#if batchOrder === 'Lemma' && item.lemma && item.lemma !== tableHeaderInfo.title}
                      <small style="color: var(--text-muted);"> ({item.lemma})</small>
                    {/if}
                  </td>
                  <td>
                    <code class="tag-badge" style="font-size: 0.8rem;">{item.tags}</code>
                  </td>
                {/if}

                <td>
                  <span class="cell-form-chip">{item.submitted}</span>
                </td>
                <td>
                  <div style="display: flex; gap: 0.4rem;">
                    <button 
                      type="button" 
                      class="primary small" 
                      style="background-color: var(--success); padding: 0.35rem 0.65rem;"
                      on:click={() => rateSingleTableCell(item.cellid, true)}
                      title="Approve form"
                    >
                      <span class="material-icons" style="font-size: 1rem;">check</span>
                    </button>
                    <button 
                      type="button" 
                      class="danger small" 
                      style="padding: 0.35rem 0.65rem;"
                      on:click={() => rateSingleTableCell(item.cellid, false)}
                      title="Disapprove form"
                    >
                      <span class="material-icons" style="font-size: 1rem;">close</span>
                    </button>
                  </div>
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
  .check-page {
    max-width: 850px;
    margin: 0 auto;
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .check-card {
    padding: 2rem;
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

  .linguistic-info-pill {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.75rem;
    background: #f1f5f9;
    padding: 0.65rem 1rem;
    border-radius: 6px;
    font-size: 0.9rem;
    margin-bottom: 1.5rem;
  }

  .verification-box {
    text-align: center;
    background: #ffffff;
    border: 2px solid var(--primary-light);
    border-radius: var(--radius);
    padding: 2rem 1.5rem;
    margin-bottom: 1.5rem;
  }

  .surface-form-display {
    font-size: 2.25rem;
    font-weight: 700;
    color: var(--primary);
    letter-spacing: 0.02em;
  }

  .rating-actions {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .cell-form-chip {
    display: inline-block;
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: 4px;
    padding: 0.3rem 0.65rem;
    font-weight: 600;
    font-size: 0.95rem;
    color: var(--primary);
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
