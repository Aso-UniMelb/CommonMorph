<script lang="ts">
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

<div class="container" style="padding-top: 1.5rem; padding-bottom: 3rem;">
  <div style="max-width: 850px; margin: 0 auto;">
    <div style="margin-bottom: 2rem;">
      <h1 style="margin-bottom: 0.5rem;">About CommonMorph</h1>
      <p style="color: var(--text-muted); font-size: 1.05rem;">
        An open collaborative ecosystem for documenting and expanding morphological resources.
      </p>
    </div>

    <div class="card about-section">
      <h2>Our Mission</h2>
      <p>
        CommonMorph aims to build the world's most diverse, standardized, and comprehensive repository of morphological paradigms by bridging the gap between field linguists, native language communities, and computational models. By enabling collaborative elicitation and validation, we create structured resources reflecting the rich morphological variation of the world's languages—especially under-documented, endangered, and indigenous varieties.
      </p>
    </div>

    <div class="card about-section">
      <h2>Background Study & Academic Collaboration</h2>
      <p>
        CommonMorph was designed and developed as part of an academic research initiative at <b>The University of Melbourne</b> in collaboration with the <b>University of Zurich</b>.
      </p>
      <p>
        The underlying methodologies, human-in-the-loop elicitation workflows, and system architecture are published and presented at the <b>15th Language Resources and Evaluation Conference (LREC 2026)</b>.
      </p>
      <div style="display: flex; gap: 0.75rem; flex-wrap: wrap; margin-top: 1rem;">
        <a 
          href="https://aclanthology.org/2026.lrec-1.919/" 
          target="_blank" 
          rel="noopener noreferrer" 
          class="button primary small"
        >
          <span class="material-icons">article</span> ACL Anthology
        </a>
        <a 
          href="https://lrec.elra.info/lrec2026-main-919" 
          target="_blank" 
          rel="noopener noreferrer" 
          class="button secondary small"
        >
          <span class="material-icons">open_in_new</span> LREC 2026
        </a>
      </div>
    </div>

    <!-- Citation & BibTeX Section -->
    <div class="card about-section citation-card">
      <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 0.75rem;">
        <h2 style="margin: 0;">Citation</h2>
        <button type="button" class="button secondary small" on:click={copyBibtex}>
          <span class="material-icons">{copied ? 'check' : 'content_copy'}</span>
          {copied ? 'Copied to Clipboard!' : 'Copy BibTeX'}
        </button>
      </div>

      <p style="font-size: 0.95rem; color: #334155; line-height: 1.6; margin-bottom: 1rem;">
        If you use CommonMorph in your research, field documentation, or NLP pipelines, please cite our paper:
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

    <div class="card about-section">
      <h2>How It Works</h2>
      <p>
        Participants contribute by structuring inflectional rules, inputting lexicon base stems, eliciting surface forms from speakers, and peer-reviewing community submissions. All verified records are automatically indexed and prepared for open-access export in UniMorph and CLDF formats.
      </p>
    </div>

    <div class="card about-section">
      <h2>Who Can Participate</h2>
      <p>
        CommonMorph is open to academic linguists, field researchers, native speakers, and language preservation groups. Whether you are documenting verbal prefixes in a regional dialect or contributing your native language's nominal declensions, your participation makes a lasting difference.
      </p>
    </div>

    <div class="card about-section">
      <h2>Ethics & Research Approval</h2>
      <p>
        The study design and participant involvement have been reviewed and approved by <b>the University of Melbourne Human Research Ethics Committee</b> (Reference No. 2025-32066-64105-3).
      </p>
    </div>
  </div>
</div>

<style>
  .about-section {
    margin-bottom: 1.5rem;
    padding: 1.75rem;
  }

  .about-section h2 {
    margin-top: 0;
    margin-bottom: 0.75rem;
    font-size: 1.35rem;
    color: var(--primary);
  }

  .about-section p {
    color: #475569;
    line-height: 1.7;
    margin-bottom: 0.75rem;
  }

  .about-section p:last-child {
    margin-bottom: 0;
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

  @media (max-width: 640px) {
    .about-section {
      padding: 1.25rem;
    }
  }
</style>
