<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '../lib/api';
  import Map from '../lib/components/Map.svelte';

  export let params: { langid: string } = { langid: '' };

  let lang: any = null;
  let submittedCount = 0;
  let isLoading = true;
  let errorMessage = '';

  $: langId = parseInt(params.langid, 10);

  onMount(async () => {
    try {
      const res = await api.get('/Home/api/dataset', { langid: langId });
      if (res && res.lang) {
        lang = res.lang;
        submittedCount = res.submitted || 0;
      } else {
        errorMessage = 'Language variety not found.';
      }
    } catch (err: any) {
      errorMessage = err.message || 'Failed to load dataset details';
    } finally {
      isLoading = false;
    }
  });

  let copied = false;

  const bibtex = `@inproceedings{mahmudi-etal-2026-commonmorph,
    title = "{C}ommon{M}orph: Participatory Morphological Documentation Platform",
    author = "Mahmudi, Aso  and
      Ahmadi, Sina  and
      Kurniawan, Kemal Maulana  and
      Sennrich, Rico  and
      Hovy, Eduard H.  and
      Vylomova, Ekaterina",
    booktitle = "Proceedings of the Fifteenth Language Resources and Evaluation Conference",
    month = may,
    year = "2026",
    address = "Palma de Mallorca, Spain",
    publisher = "ELRA Language Resource Association",
    url = "https://aclanthology.org/2026.lrec-1.919/",
    doi = "10.63317/5gqigwzjjv4b",
    pages = "11735--11746",
}`;

  async function copyBibtex() {
    try {
      await navigator.clipboard.writeText(bibtex);
      copied = true;
      setTimeout(() => copied = false, 2500);
    } catch (err) {
      console.warn('Clipboard write failed', err);
    }
  }
</script>

<div class="container">
  <div style="margin-bottom: 1.5rem;">
    <a href="/datasets" class="button secondary small">
      <span class="material-icons">arrow_back</span> Back to Datasets
    </a>
  </div>

  {#if isLoading}
    <div style="text-align: center; padding: 4rem;">
      <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 2.5rem; color: var(--primary);">sync</span>
      <p>Loading dataset information...</p>
    </div>
  {:else if errorMessage}
    <div class="alert alert-error">{errorMessage}</div>
  {:else if lang}
    <div class="card" style="padding: 2rem;">
      <div style="display: flex; align-items: flex-start; justify-content: space-between; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
        <div>
          <div style="display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem;">
            <h1 style="margin: 0;">{lang.title}</h1>
            <span class="badge primary" style="font-size: 0.9rem; padding: 0.3rem 0.6rem;">{lang.code}</span>
          </div>
          {#if lang.description}
            <p style="color: var(--text-muted); max-width: 700px; margin: 0;">
              {lang.description}
            </p>
          {/if}
        </div>

        <div class="dataset-meta-box">
          <div><span class="material-icons">how_to_reg</span> <b>{submittedCount}</b> Verified Forms</div>
          <div><span class="material-icons">copyright</span> <b>License:</b> CC BY 4.0</div>
          <div><span class="material-icons">code</span> <b>ISO 639-3:</b> {lang.code}</div>
        </div>
      </div>

      <!-- Download options -->
      <div class="download-section">
        <h3>Export & Download Formats</h3>
        <div class="download-buttons-grid">
          <a href="/download/unimorph/{lang.id}" class="download-card" download>
            <div class="d-icon"><span class="material-icons">storage</span></div>
            <div>
              <strong>UniMorph Format (.tsv)</strong>
              <p>Standard 3-column TSV (Lemma, Form, UniMorph Tag Bundle)</p>
            </div>
            <span class="material-icons d-arrow">download</span>
          </a>

          <a href="/download/extended/{lang.id}" class="download-card" download>
            <div class="d-icon"><span class="material-icons">view_column</span></div>
            <div>
              <strong>Extended Format (.tsv)</strong>
              <p>Detailed analysis including stems, formulas, realizations, and ratings</p>
            </div>
            <span class="material-icons d-arrow">download</span>
          </a>

          <a href="/download/lexicon/{lang.id}" class="download-card" download>
            <div class="d-icon"><span class="material-icons">menu_book</span></div>
            <div>
              <strong>Lexicon / Stems (.tsv)</strong>
              <p>Dictionary base entries, glosses, and inflection classes</p>
            </div>
            <span class="material-icons d-arrow">download</span>
          </a>
        </div>
      </div>

      <!-- Geographical Location Map -->
      {#if lang.latitude !== undefined && lang.longitude !== undefined && lang.latitude !== null && lang.longitude !== null}
        <div style="margin-top: 2rem;">
          <h3>Geographical Region</h3>
          <Map 
            mapData={[{ id: lang.id, title: lang.title, code: lang.code, latitude: lang.latitude, longitude: lang.longitude }]} 
            height="320px" 
          />
        </div>
      {/if}

      <!-- Citation Section -->
      <div class="card citation-card" style="margin-top: 2rem;">
        <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 0.75rem;">
          <h3 style="margin: 0;">Citation</h3>
          <button type="button" class="button secondary small" on:click={copyBibtex}>
            <span class="material-icons">{copied ? 'check' : 'content_copy'}</span>
            {copied ? 'Copied to Clipboard!' : 'Copy BibTeX'}
          </button>
        </div>

        <p style="font-size: 0.9rem; color: #475569; line-height: 1.6; margin-bottom: 1rem;">
          If you use this dataset or CommonMorph resources in your research, documentation, or NLP pipelines, please cite:
        </p>

        <div class="citation-formatted">
          Aso Mahmudi, Sina Ahmadi, Kemal Maulana Kurniawan, Rico Sennrich, Eduard H. Hovy, and Ekaterina Vylomova. 2026. 
          <b>CommonMorph: Participatory Morphological Documentation Platform</b>. 
          In <i>Proceedings of the Fifteenth Language Resources and Evaluation Conference</i>, pages 11735–11746, Palma de Mallorca, Spain. ELRA Language Resource Association. 
          <a href="https://doi.org/10.63317/5gqigwzjjv4b" target="_blank" rel="noopener noreferrer" style="word-break: break-all;">doi:10.63317/5gqigwzjjv4b</a>.
        </div>

        <div class="bibtex-container">
          <pre><code>{bibtex}</code></pre>
        </div>
      </div>
    </div>
  {/if}
</div>

<style>
  .dataset-meta-box {
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    font-size: 0.9rem;
  }

  .dataset-meta-box div {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .dataset-meta-box .material-icons {
    font-size: 1.1rem;
    color: var(--primary);
  }

  .download-section {
    margin-top: 2rem;
    border-top: 1px solid var(--border);
    padding-top: 1.5rem;
  }

  .download-buttons-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1rem;
    margin-top: 1rem;
  }

  .download-card {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1rem;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    background: #fff;
    color: var(--text);
    transition: all 0.15s ease;
  }

  .download-card:hover {
    border-color: var(--primary);
    background: var(--primary-light);
    transform: translateY(-2px);
    box-shadow: var(--shadow-sm);
  }

  .download-card p {
    margin: 0;
    font-size: 0.8rem;
    color: var(--text-muted);
  }

  .d-icon {
    width: 42px;
    height: 42px;
    border-radius: 8px;
    background: var(--primary-light);
    color: var(--primary);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .d-arrow {
    margin-left: auto;
    color: var(--primary);
  }

  .citation-card {
    border-left: 4px solid var(--primary);
  }

  .citation-formatted {
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem 1.25rem;
    font-size: 0.92rem;
    line-height: 1.6;
    color: #1e293b;
    margin-bottom: 1rem;
  }

  .bibtex-container {
    background: #0f172a;
    border-radius: var(--radius);
    padding: 1rem 1.25rem;
    overflow-x: auto;
  }

  .bibtex-container pre {
    margin: 0;
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
    font-size: 0.82rem;
    color: #f1f5f9;
    line-height: 1.5;
    white-space: pre-wrap;
    word-break: break-word;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
