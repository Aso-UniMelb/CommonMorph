<script lang="ts">
  import { onMount } from 'svelte';
  import { selectedLang, languages } from '../../lib/stores/language';
  import { currentMetaLang, dictionaries, t } from '../../lib/stores/i18n';
  import { UM_tags2DimFeat, UM_tag2word } from '../../lib/unimorph';
  import { api } from '../../lib/api';

  interface QItem {
    stitle: string;
    atitle?: string;
    tags: string;
    available: boolean;
  }

  let selectedMetaLang = $currentMetaLang || 'en';
  let activeTab: 'all' | 'unavailable' | 'available' = 'unavailable';
  let searchTerm = '';
  let isLoading = false;

  // Master lists
  let availableList: QItem[] = [];
  let unavailableList: QItem[] = [];
  let allQuestions: QItem[] = [];

  // Selected item state
  let selectedItem: QItem | null = null;
  let currentQuestionText = '';
  let isLoadingQuestion = false;
  let isSubmitting = false;

  // LLM Suggestions
  let isGeneratingLlm = false;
  let llmSuggestions: Array<{ model: string; text: string }> = [];

  onMount(async () => {
    if ($selectedLang) {
      await loadQuestionLists();
    }
  });

  $: if ($selectedLang || selectedMetaLang) {
    loadQuestionLists();
  }

  $: textDir = dictionaries[selectedMetaLang]?.dir || 'ltr';

  async function loadQuestionLists() {
    if (!$selectedLang) return;
    isLoading = true;
    selectedItem = null;
    currentQuestionText = '';
    llmSuggestions = [];

    try {
      const [availRes, unavailRes] = await Promise.all([
        api.get('/QTemplate/available', { metalang: selectedMetaLang, langid: $selectedLang.id }),
        api.get('/QTemplate/unavailable', { metalang: selectedMetaLang, langid: $selectedLang.id }),
      ]);

      const avail: QItem[] = (Array.isArray(availRes) ? availRes : []).map((q: any) => ({
        stitle: q.stitle || q.Stitle || '',
        atitle: q.atitle || q.Atitle || '',
        tags: q.tags || q.Tags || '',
        available: true,
      }));

      const unavail: QItem[] = (Array.isArray(unavailRes) ? unavailRes : []).map((q: any) => ({
        stitle: q.stitle || q.Stitle || '',
        atitle: q.atitle || q.Atitle || '',
        tags: q.tags || q.Tags || '',
        available: false,
      }));

      availableList = avail;
      unavailableList = unavail;
      allQuestions = [...avail, ...unavail].sort((a, b) => a.stitle.localeCompare(b.stitle));

      // Auto-select first item if available
      if (activeTab === 'unavailable' && unavail.length > 0) {
        selectItem(unavail[0]);
      } else if (activeTab === 'available' && avail.length > 0) {
        selectItem(avail[0]);
      } else if (allQuestions.length > 0) {
        selectItem(allQuestions[0]);
      }
    } catch (err) {
      console.error('Failed to fetch question templates list', err);
    } finally {
      isLoading = false;
    }
  }

  $: displayedList = (() => {
    let list = allQuestions;
    if (activeTab === 'available') list = availableList;
    if (activeTab === 'unavailable') list = unavailableList;

    if (searchTerm.trim()) {
      const term = searchTerm.toLowerCase();
      return list.filter(item =>
        item.tags.toLowerCase().includes(term) ||
        item.stitle.toLowerCase().includes(term) ||
        (item.atitle && item.atitle.toLowerCase().includes(term))
      );
    }
    return list;
  })();

  async function selectItem(item: QItem) {
    selectedItem = item;
    currentQuestionText = '';
    llmSuggestions = [];

    if (item.available) {
      isLoadingQuestion = true;
      try {
        const text = await api.get('/QTemplate/get', {
          metalang: selectedMetaLang,
          tags: item.tags,
        });
        currentQuestionText = typeof text === 'string' ? text : (text?.question || '');
      } catch (err) {
        console.error('Failed to get template question text', err);
      } finally {
        isLoadingQuestion = false;
      }
    }

    // Auto-fetch LLM inspirations
    fetchLlmInspirations(item.tags);
  }

  async function fetchLlmInspirations(tags: string) {
    if (!tags) return;
    isGeneratingLlm = true;
    llmSuggestions = [];

    try {
      const feats = UM_tags2DimFeat(tags, $selectedLang?.code);
      let promptTemplate = dictionaries[selectedMetaLang]?.prompt || 
        "Ask a speaker how they would say the word 'XXX' with these features: 'FFF'. Generate only the question for the speaker. 'XXX' should be in the question.";
      const prompt = promptTemplate.replace(/FFF/g, `<b>${feats.join(', ')}</b>`);

      const res = await api.post('/LLM/getQuestionFromLLM', { prompt });
      if (res && typeof res === 'object') {
        llmSuggestions = Object.entries(res).map(([model, text]) => ({
          model,
          text: String(text).replace(/\n/g, ' ').replace(/"/g, "'").trim()
        }));
      }
    } catch (err) {
      console.warn('Could not fetch LLM question inspirations', err);
    } finally {
      isGeneratingLlm = false;
    }
  }

  async function handleSubmit() {
    if (!selectedItem || !currentQuestionText.trim()) return;
    isSubmitting = true;

    try {
      const endpoint = selectedItem.available ? '/QTemplate/update' : '/QTemplate/insert';
      await api.post(endpoint, {
        questionlang: selectedMetaLang,
        metalang: selectedMetaLang,
        unimorphtags: selectedItem.tags,
        question: currentQuestionText.trim(),
      });

      // Update state locally
      selectedItem.available = true;
      const unavailIdx = unavailableList.findIndex(x => x.tags === selectedItem?.tags);
      if (unavailIdx >= 0) {
        unavailableList.splice(unavailIdx, 1);
        unavailableList = [...unavailableList];
      }
      if (!availableList.some(x => x.tags === selectedItem?.tags)) {
        availableList = [...availableList, selectedItem];
      }

      // Advance to next structure in current list
      nextStructure();
    } catch (err: any) {
      alert(err.message || 'Failed to save question template');
    } finally {
      isSubmitting = false;
    }
  }

  function nextStructure() {
    const currentIndex = displayedList.findIndex(x => x.tags === selectedItem?.tags);
    if (currentIndex >= 0 && currentIndex + 1 < displayedList.length) {
      selectItem(displayedList[currentIndex + 1]);
    } else if (displayedList.length > 0) {
      selectItem(displayedList[0]);
    }
  }
</script>

<div class="qtemplates-suite">
  <!-- Top Bar -->
  <div class="suite-header card">
    <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem;">
      <div>
        <h2 style="margin: 0;">Elicitation Question Templates</h2>
        <p style="color: var(--text-muted); margin: 0.25rem 0 0 0; font-size: 0.9rem;">
          Language Variety: <b>{$selectedLang?.title || 'None'} ({$selectedLang?.code || ''})</b>
        </p>
      </div>

      <div style="display: flex; align-items: center; gap: 0.75rem;">
        <label for="meta-lang-select" style="font-size: 0.9rem; margin: 0; font-weight: 600;">Elicitation Language:</label>
        <select id="meta-lang-select" bind:value={selectedMetaLang} style="width: auto; font-weight: 500;">
          {#each Object.entries(dictionaries) as [code, dict]}
            <option value={code}>{dict.name} ({code})</option>
          {/each}
        </select>
      </div>
    </div>
  </div>

  {#if isLoading}
    <div class="card" style="text-align: center; padding: 4rem;">
      <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 2.5rem; color: var(--primary);">sync</span>
      <p style="margin-top: 1rem;">Loading structure question coverage...</p>
    </div>
  {:else}
    <!-- Two Column Master-Detail Layout -->
    <div class="suite-layout">
      <!-- Left Panel: Lists & Filter Tabs -->
      <div class="suite-sidebar card">
        <!-- Tabs -->
        <div class="sidebar-tabs">
          <button 
            type="button" 
            class="tab-btn" 
            class:active={activeTab === 'unavailable'} 
            on:click={() => { activeTab = 'unavailable'; if (unavailableList.length > 0) selectItem(unavailableList[0]); }}
          >
            Missing ({unavailableList.length})
          </button>
          <button 
            type="button" 
            class="tab-btn" 
            class:active={activeTab === 'available'} 
            on:click={() => { activeTab = 'available'; if (availableList.length > 0) selectItem(availableList[0]); }}
          >
            Covered ({availableList.length})
          </button>
          <button 
            type="button" 
            class="tab-btn" 
            class:active={activeTab === 'all'} 
            on:click={() => activeTab = 'all'}
          >
            All ({allQuestions.length})
          </button>
        </div>

        <!-- Search box -->
        <div style="padding: 0.75rem 1rem; border-bottom: 1px solid var(--border);">
          <input 
            type="text" 
            placeholder="Search tags or titles..." 
            bind:value={searchTerm}
            style="padding: 0.4rem 0.65rem; font-size: 0.85rem;"
          />
        </div>

        <!-- List items -->
        <div class="sidebar-items-list">
          {#if displayedList.length === 0}
            <div style="padding: 2rem; text-align: center; color: var(--text-muted); font-size: 0.88rem;">
              No structures in this category.
            </div>
          {:else}
            {#each displayedList as item}
              {@const isSelected = selectedItem?.tags === item.tags}
              <button 
                type="button" 
                class="q-item" 
                class:active={isSelected}
                class:covered={item.available}
                on:click={() => selectItem(item)}
              >
                <span class="material-icons status-icon" class:success={item.available}>
                  {item.available ? 'check_circle' : 'radio_button_unchecked'}
                </span>
                <div class="q-item-info">
                  <strong class="q-item-title">
                    {item.stitle} {#if item.atitle}({item.atitle}){/if}
                  </strong>
                  <code class="q-item-tags">{item.tags}</code>
                </div>
              </button>
            {/each}
          {/if}
        </div>
      </div>

      <!-- Right Panel: Editor & AI Inspiration -->
      <div class="suite-main card" dir={textDir}>
        {#if !selectedItem}
          <div style="text-align: center; padding: 4rem; color: var(--text-muted);">
            <span class="material-icons" style="font-size: 3rem; opacity: 0.5;">touch_app</span>
            <p style="margin-top: 0.5rem;">Select a morphological structure from the left sidebar to edit or create its question template.</p>
          </div>
        {:else}
          <!-- Structure Header Info -->
          <div class="selected-header">
            <div style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem;">
              <div>
                <h3 style="margin: 0;">
                  {selectedItem.stitle} {#if selectedItem.atitle}({selectedItem.atitle}){/if}
                </h3>
                <span style="font-size: 0.85rem; color: var(--text-muted);">
                  {UM_tag2word(selectedItem.tags, $selectedLang?.code)}
                </span>
              </div>

              <div style="display: flex; align-items: center; gap: 0.5rem;">
                <code class="tag-badge" style="font-size: 0.9rem;">{selectedItem.tags}</code>
                {#if selectedItem.available}
                  <span class="badge primary">Covered</span>
                {:else}
                  <span class="badge accent">Needs Question</span>
                {/if}
              </div>
            </div>

            <!-- Linguistic Features Breakdown -->
            <div class="features-pill-row">
              {#each UM_tags2DimFeat(selectedItem.tags, $selectedLang?.code) as feat}
                <span class="feat-pill">{feat}</span>
              {/each}
            </div>
          </div>

          <!-- Editor Form -->
          <form on:submit|preventDefault={handleSubmit} style="margin-bottom: 0.5rem;">
            <div class="field">
              <label for="txtTemplateQuestion">
                {selectedItem.available ? 'Edit Existing Question Prompt:' : 'Write New Question Prompt:'}
              </label>
              {#if isLoadingQuestion}
                <div style="padding: 1rem; color: var(--text-muted);">Loading prompt...</div>
              {:else}
                <textarea 
                  id="txtTemplateQuestion"
                  bind:value={currentQuestionText} 
                  rows="3" 
                  placeholder=""
                  style="font-size: 1.05rem; line-height: 1.5; font-weight: 500;"
                  required
                ></textarea>
              {/if}

            <div style="display: flex; gap: 0.75rem;">
              <button 
                type="submit" 
                class="button primary" 
                disabled={isSubmitting || !currentQuestionText.trim()}
                style="padding: 0.2rem 0.9rem;"
              >
                <span class="material-icons">send</span> {selectedItem.available ? 'Update Prompt' : 'Submit Prompt'}
              </button>

              <button 
                type="button" 
                class="button secondary" 
                on:click={nextStructure}
                style="padding: 0.7rem 1.2rem;"
              >
                <span class="material-icons">skip_next</span> Skip / Next
              </button>
            </div>
            
            </div>
          </form>

          <!-- Instructions Card -->
          <div class="guideline-box">
            <strong>Question Writing Rules:</strong>
            <ul>
              <li>Must be written completely in the selected elicitation language ({dictionaries[selectedMetaLang]?.name || selectedMetaLang}).</li>
              <li>Use plain, natural language that <b>anyone can understand</b> (avoid jargon like "imperfective").</li>
              <li>Use <code>XXX</code> as the exact placeholder for the base verb/noun (e.g. <em>"Did you XXX yesterday?"</em>).</li>
            </ul>
          </div>

          <!-- AI Model Inspirations -->
          <div class="llm-inspirations-panel">
            <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.5rem;">
              <strong style="font-size: 0.9rem; color: var(--text-muted);">
                AI Prompt Inspirations (click any to use):
              </strong>
              {#if isGeneratingLlm}
                <span style="font-size: 0.8rem; color: var(--primary);">
                  <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 0.9rem; vertical-align: middle;">sync</span>
                  Thinking...
                </span>
              {/if}
            </div>

            {#if llmSuggestions.length === 0 && !isGeneratingLlm}
              <button 
                type="button" 
                class="secondary small" 
                on:click={() => selectedItem && fetchLlmInspirations(selectedItem.tags)}
              >
                <span class="material-icons">psychology</span> Generate AI Prompt Suggestions
              </button>
            {:else}
              <div class="llm-suggestions-grid">
                {#each llmSuggestions as sug}
                  <button 
                    type="button" 
                    class="llm-suggestion-card" 
                    on:click={() => currentQuestionText = sug.text}
                  >
                    <span class="llm-model-badge">{sug.model}</span>
                    <span class="llm-text">{sug.text}</span>
                  </button>
                {/each}
              </div>
            {/if}
          </div>
        {/if}
      </div>
    </div>
  {/if}
</div>

<style>
  .qtemplates-suite {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
  }

  .suite-header {
    padding: 1.25rem 1.5rem;
    margin: 0;
  }

  .suite-layout {
    display: grid;
    grid-template-columns: 340px 1fr;
    gap: 1.25rem;
    align-items: start;
  }

  .suite-sidebar {
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    height: calc(100vh - 220px);
    min-height: 520px;
    overflow: hidden;
  }

  .sidebar-tabs {
    display: flex;
    background: #f8fafc;
    border-bottom: 1px solid var(--border);
  }

  .tab-btn {
    flex: 1;
    background: transparent;
    border: none;
    border-bottom: 2px solid transparent;
    padding: 0.65rem 0.4rem;
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--text-muted);
    cursor: pointer;
    border-radius: 0;
    text-align: center;
    justify-content: center;
  }

  .tab-btn:hover {
    background: #f1f5f9;
  }

  .tab-btn.active {
    color: var(--primary);
    border-bottom-color: var(--primary);
    background: #ffffff;
  }

  .sidebar-items-list {
    flex: 1;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
  }

  .q-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem 0.7rem;
    background: transparent;
    border: none;
    border-bottom: 1px solid #f1f5f9;
    border-radius: 0;
    cursor: pointer;
    text-align: left;
    transition: background 0.15s ease;
    justify-content: flex-start;
  }

  .q-item:hover {
    background: #f8fafc;
  }

  .q-item.active {
    background: var(--primary-light);
    border-left: 3px solid var(--primary);
  }

  .status-icon {
    font-size: 1.2rem;
    color: #cbd5e1;
    flex-shrink: 0;
  }

  .status-icon.success {
    color: var(--success);
  }

  .q-item-info {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    overflow: hidden;
  }

  .q-item-title {
    font-size: 0.88rem;
    color: var(--text);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .q-item-tags {
    font-size: 0.75rem;
    color: var(--text-muted);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .suite-main {
    padding: 1rem;
    margin: 0;
    min-height: 520px;
  }

  .selected-header {
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.7rem;
    margin-bottom: 1rem;
  }

  .features-pill-row {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
    margin-top: 0.75rem;
  }

  .feat-pill {
    background: #ffffff;
    border: 1px solid var(--border);
    padding: 0.1rem 0.4rem;
    border-radius: 4px;
    font-size: 0.7rem;
  }

  .guideline-box {
    background: #eff6ff;
    border: 1px solid #bfdbfe;
    border-radius: 6px;
    padding: 0.85rem;
    font-size: 0.85rem;
    color: #1e3a8a;
  }

  .guideline-box ul {
    margin: 0.35rem 0 0 0;
    padding-left: 1.25rem;
  }

  .guideline-box li {
    margin-bottom: 0.2rem;
  }

  .llm-inspirations-panel {
    margin-top: 2rem;
    border-top: 1px solid var(--border);
    padding-top: 1.25rem;
  }

  .llm-suggestions-grid {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-top: 0.5rem;
  }

  .llm-suggestion-card {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    padding: 0.75rem 1rem;
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: 6px;
    cursor: pointer;
    text-align: left;
    transition: all 0.15s ease;
  }

  .llm-suggestion-card:hover {
    border-color: var(--primary);
    background: var(--primary-light);
  }

  .llm-model-badge {
    font-size: 0.72rem;
    font-weight: 700;
    color: var(--primary);
    margin-bottom: 0.2rem;
  }

  .llm-text {
    font-size: 0.95rem;
    color: var(--text);
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }

  @media (max-width: 900px) {
    .suite-layout {
      grid-template-columns: 1fr;
    }
    .suite-sidebar {
      height: 380px;
    }
  }
</style>
