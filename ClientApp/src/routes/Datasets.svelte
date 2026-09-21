<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '../lib/api';
  import { dictionaries, currentMetaLang } from '../lib/stores/i18n';
  import { UM_tags2DimFeat } from '../lib/unimorph';

  let activeTab: 'morphology' | 'prompts' = 'morphology';
  let lexiconStats: any[] = [];
  let submittedByLang: Record<string | number, number> = {};
  let isLoading = true;

  // Prompts tab
  let selectedMetaLang = $currentMetaLang || 'en';
  let promptTemplates: any[] = [];
  let loadingPrompts = false;

  onMount(async () => {
    try {
      const res = await api.get('/Home/api/datasets');
      if (res) {
        lexiconStats = res.lexiconStats || [];
        submittedByLang = res.submittedByLang || {};
      }
    } catch (err) {
      console.error('Failed to load dataset stats', err);
    } finally {
      isLoading = false;
    }
  });

  async function loadPrompts(metalang: string) {
    loadingPrompts = true;
    try {
      promptTemplates = await api.get('/QTemplate/download', { metalang });
    } catch (err) {
      console.error('Failed to load prompt templates', err);
      promptTemplates = [];
    } finally {
      loadingPrompts = false;
    }
  }

  function handleMetaLangChange(e: Event) {
    const target = e.target as HTMLSelectElement;
    selectedMetaLang = target.value;
    loadPrompts(selectedMetaLang);
  }
</script>

<div class="container">
  <div style="margin-bottom: 2rem; text-align: center;">
    <h1>CommonMorph Open Datasets</h1>
    <p style="color: var(--text-muted); max-width: 650px; margin: 0 auto;">
      Freely accessible, community-verified multilingual morphological datasets formatted for modern NLP training and typological linguistics.
    </p>
  </div>

  <!-- Tabs Navigation -->
  <div class="tabs-nav">
    <button 
      type="button" 
      class="tab-btn" 
      class:active={activeTab === 'morphology'} 
      on:click={() => activeTab = 'morphology'}
    >
      <span class="material-icons">storage</span> Morphological Datasets
    </button>
    <button 
      type="button" 
      class="tab-btn" 
      class:active={activeTab === 'prompts'} 
      on:click={() => { activeTab = 'prompts'; if (promptTemplates.length === 0) loadPrompts(selectedMetaLang); }}
    >
      <span class="material-icons">question_answer</span> Elicitation Prompts
    </button>
  </div>

  {#if activeTab === 'morphology'}
    <div class="tab-pane">
      <div class="card" style="margin-bottom: 2rem;">
        <h3>UniMorph & CLDF Compatible</h3>
        <p>
          We provide tab-separated values (TSV) compatible with the Universal Morphology (UniMorph) standard. Each record consists of a lemma, surface inflected form, and structured feature bundle.
        </p>
      </div>

      {#if isLoading}
        <div style="text-align: center; padding: 3rem;">
          <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 2rem; color: var(--primary);">sync</span>
          <p>Loading dataset catalog...</p>
        </div>
      {:else if lexiconStats.length === 0}
        <div class="alert alert-info">No language varieties available yet.</div>
      {:else}
        <div class="dataset-grid">
          {#each lexiconStats as lang}
            {@const inflectedCount = submittedByLang[lang.langid] || 0}
            <a href="/dataset/{lang.langid}" class="dataset-card">
              <div class="card-header">
                <span class="lang-code">{lang.code}</span>
                <span class="lang-title">{lang.title}</span>
              </div>
              <div class="card-stats">
                <div class="stat-item">
                  <span class="stat-num">{inflectedCount}</span>
                  <span class="stat-label">Inflected Forms</span>
                </div>
                <div class="stat-item">
                  <span class="stat-num">{lang.cnt}</span>
                  <span class="stat-label">Lemmas</span>
                </div>
              </div>
              <div class="card-footer">
                <span>View & Download</span>
                <span class="material-icons" style="font-size: 1.1rem;">arrow_forward</span>
              </div>
            </a>
          {/each}
        </div>
      {/if}
    </div>
  {:else if activeTab === 'prompts'}
    <div class="tab-pane">
      <div class="card">
        <h3>Elicitation Template Prompts for Field Linguistics</h3>
        <p style="color: var(--text-muted);">
          Explore crowdsourcing prompt templates designed to elicit morphosyntactic features from native speakers without using linguistic jargon.
        </p>

        <div class="field" style="max-width: 320px; margin-top: 1.5rem;">
          <label>Select Elicitation Language:</label>
          <select value={selectedMetaLang} on:change={handleMetaLangChange}>
            {#each Object.entries(dictionaries) as [code, dict]}
              <option value={code}>{dict.name} ({code})</option>
            {/each}
          </select>
        </div>
      </div>

      {#if loadingPrompts}
        <div style="text-align: center; padding: 2rem;">
          <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 1.8rem; color: var(--primary);">sync</span>
          <p>Loading prompt templates...</p>
        </div>
      {:else if promptTemplates.length === 0}
        <div class="alert alert-info">No prompt templates available for this language yet.</div>
      {:else}
        <div class="prompts-list">
          {#each promptTemplates as pt}
            <div class="card" style="padding: 1.25rem;">
              <div style="display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 0.5rem;">
                <span class="badge primary">UniMorph: {pt.unimorphtags}</span>
              </div>
              <div style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 0.5rem;">
                <b>Features:</b> {UM_tags2DimFeat(pt.unimorphtags).join(', ')}
              </div>
              <div 
                dir={dictionaries[selectedMetaLang]?.dir || 'ltr'} 
                style="background: #f8fafc; padding: 0.75rem; border-radius: 6px; font-weight: 500;"
              >
                {pt.question}
              </div>
            </div>
          {/each}
        </div>
      {/if}
    </div>
  {/if}
</div>

<style>
  .tabs-nav {
    display: flex;
    gap: 0.5rem;
    border-bottom: 2px solid var(--border);
    margin-bottom: 1.5rem;
  }

  .tab-btn {
    background: transparent;
    border: none;
    border-bottom: 3px solid transparent;
    margin-bottom: -2px;
    border-radius: 0;
    padding: 0.75rem 1.25rem;
    font-size: 1rem;
    font-weight: 600;
    color: var(--text-muted);
    cursor: pointer;
  }

  .tab-btn.active {
    color: var(--primary);
    border-bottom-color: var(--primary);
  }

  .dataset-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 1.25rem;
  }

  .dataset-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1.25rem;
    box-shadow: var(--shadow-sm);
    display: flex;
    flex-direction: column;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .dataset-card:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow);
    border-color: var(--primary);
  }

  .card-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 1rem;
  }

  .lang-code {
    font-family: monospace;
    background: var(--primary-light);
    color: var(--primary);
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    font-weight: 700;
    font-size: 0.85rem;
  }

  .lang-title {
    font-weight: 600;
    font-size: 1.1rem;
    color: var(--text);
  }

  .card-stats {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem;
    background: #f8fafc;
    padding: 0.75rem;
    border-radius: 6px;
    margin-bottom: 1rem;
  }

  .stat-item {
    display: flex;
    flex-direction: column;
  }

  .stat-num {
    font-size: 1.2rem;
    font-weight: 700;
    color: var(--primary);
  }

  .stat-label {
    font-size: 0.75rem;
    color: var(--text-muted);
  }

  .card-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.88rem;
    font-weight: 600;
    color: var(--primary);
    margin-top: auto;
  }

  .prompts-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
