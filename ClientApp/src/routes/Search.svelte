<script lang="ts">
  import { api } from '../lib/api';
  import { UM_tag2word } from '../lib/unimorph';

  let form = '';
  let lemma = '';
  let meaning = '';
  let tags = '';
  let tagsFormat: 'unimorph' | 'universal' = 'unimorph';

  let results: any[] = [];
  let isSearching = false;
  let hasSearched = false;
  let errorMessage = '';

  async function handleSearch() {
    if (!form.trim() && !lemma.trim() && !meaning.trim() && !tags.trim()) {
      results = [];
      hasSearched = false;
      return;
    }

    isSearching = true;
    hasSearched = true;
    errorMessage = '';

    try {
      const res = await api.post('/Cell/getAnalyses', {
        form: form.trim(),
        lemma: lemma.trim(),
        meaning: meaning.trim(),
        tags: tags.trim()
      });
      results = res || [];
    } catch (err: any) {
      errorMessage = err.message || 'Search failed. Please try again.';
      results = [];
    } finally {
      isSearching = false;
    }
  }

  function handleReset() {
    form = '';
    lemma = '';
    meaning = '';
    tags = '';
    results = [];
    hasSearched = false;
    errorMessage = '';
  }
</script>

<div class="container">
  <div style="margin-bottom: 2rem; text-align: center;">
    <h1>Advanced Morphological Search</h1>
    <p style="color: var(--text-muted); max-width: 650px; margin: 0 auto;">
      Search across all documented languages and paradigms using surface forms, base lemmas, gloss meanings, or UniMorph tag patterns. Use <code>%</code> for wildcard matching.
    </p>
  </div>

  <div class="card" style="padding: 1.75rem;">
    <form on:submit|preventDefault={handleSearch}>
      <div class="search-fields-grid">
        <div class="field small">
          <label for="search-form">Inflected Surface Form</label>
          <input 
            id="search-form"
            type="text" 
            bind:value={form} 
            placeholder="e.g. amā% (wildcard %)" 
          />
        </div>

        <div class="field small">
          <label for="search-tags">UniMorph Tags</label>
          <input 
            id="search-tags"
            type="text" 
            bind:value={tags} 
            placeholder="e.g. V;3;SG;PRS" 
          />
        </div>

        <div class="field small">
          <label for="search-lemma">Lemma Entry</label>
          <input 
            id="search-lemma"
            type="text" 
            bind:value={lemma} 
            placeholder="e.g. amō" 
          />
        </div>

        <div class="field small">
          <label for="search-meaning">Lemma Gloss / Meaning</label>
          <input 
            id="search-meaning"
            type="text" 
            bind:value={meaning} 
            placeholder="e.g. love" 
          />
        </div>
      </div>

      <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; margin-top: 1rem;">
        <div style="display: flex; align-items: center; gap: 1rem; font-size: 0.9rem;">
          <span style="font-weight: 600;">Display Tag Format:</span>
          <label style="display: inline-flex; align-items: center; gap: 0.35rem; margin: 0; cursor: pointer;">
            <input type="radio" bind:group={tagsFormat} value="unimorph" />
            UniMorph
          </label>
          <label style="display: inline-flex; align-items: center; gap: 0.35rem; margin: 0; cursor: pointer;">
            <input type="radio" bind:group={tagsFormat} value="universal" />
            Linguistic Terms
          </label>
        </div>

        <div style="display: flex; gap: 0.5rem;">
          <button type="button" class="secondary" on:click={handleReset}>Clear</button>
          <button type="submit" class="primary" disabled={isSearching}>
            {#if isSearching}
              <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 1.1rem;">sync</span>
              Searching...
            {:else}
              <span class="material-icons">search</span> Search
            {/if}
          </button>
        </div>
      </div>
    </form>
  </div>

  {#if errorMessage}
    <div class="alert alert-error">{errorMessage}</div>
  {/if}

  {#if hasSearched && !isSearching}
    <div class="card" style="margin-top: 1.5rem;">
      <h3>Search Results ({results.length})</h3>
      {#if results.length === 0}
        <p style="color: var(--text-muted); margin: 0;">No matching morphological records found.</p>
      {:else}
        <div style="overflow-x: auto;">
          <table class="data-table">
            <thead>
              <tr>
                <th>Language Variety</th>
                <th>Lemma Gloss</th>
                <th>Lemma Entry</th>
                <th>Inflected Form</th>
                <th>Morphosyntactic Tags</th>
              </tr>
            </thead>
            <tbody>
              {#each results as row}
                <tr>
                  <td><b>{row.LanguageName || row.languageName}</b></td>
                  <td style="color: var(--text-muted);">{row.Meaning || row.meaning || '-'}</td>
                  <td><code>{row.Lemma || row.lemma}</code></td>
                  <td><strong style="color: var(--primary);">{row.Form || row.form}</strong></td>
                  <td>
                    {#if tagsFormat === 'universal'}
                      <span style="font-size: 0.85rem;">{UM_tag2word(row.Tags || row.tags)}</span>
                    {:else}
                      <code class="um-tag-badge">{row.Tags || row.tags}</code>
                    {/if}
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    </div>
  {/if}
</div>

<style>
  .search-fields-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: 1rem;
  }

  .um-tag-badge {
    background: var(--primary-light);
    color: var(--primary);
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    font-size: 0.85rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
