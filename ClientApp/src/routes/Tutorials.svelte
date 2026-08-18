<script lang="ts">
  let activeSection = 'welcome';

  const sections = [
    { id: 'welcome', title: 'Welcome & Video Overview' },
    { id: 'terminology', title: 'Linguistic Terminology' },
    { id: 'linguist', title: 'Linguist Paradigms & Layers' },
    { id: 'elicit', title: 'Elicitation & Validation' },
  ];
</script>

<div class="container">
  <div style="text-align: center; margin-bottom: 2rem;">
    <h1>User Guide & Tutorials</h1>
    <p style="color: var(--text-muted);">Step-by-step guides for contributors and field linguists.</p>
  </div>

  <div class="tutorials-layout">
    <div class="tutorials-sidebar">
      <ul class="sidebar-nav">
        {#each sections as sec}
          <li>
            <a 
              href="/tutorials" 
              class:active={activeSection === sec.id}
              on:click|preventDefault={() => activeSection = sec.id}
            >
              {sec.title}
            </a>
          </li>
        {/each}
      </ul>
    </div>

    <div class="tutorials-content">
      {#if activeSection === 'welcome'}
        <div class="card">
          <h2>Welcome to CommonMorph</h2>
          <p>Watch our video tutorial to understand how to contribute and structure morphological data:</p>
          <div class="video-container">
            <iframe 
              src="https://www.youtube.com/embed/KrbE2jhGVZ0" 
              title="CommonMorph Video Tutorial" 
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" 
              allowfullscreen
            ></iframe>
          </div>
        </div>
      {:else if activeSection === 'terminology'}
        <div class="card">
          <h2>Terminology (for Field Linguists)</h2>
          <p>CommonMorph organizes morphological schemas into several key concepts:</p>

          <dl class="term-dl">
            <dt>Inflection Class</dt>
            <dd>A classification of words within a part of speech sharing inflectional behaviors (e.g. 1st, 2nd, 3rd conjugation verbs).</dd>

            <dt>Lemma</dt>
            <dd>The dictionary or citation base form of a word. Belongs to a single Inflection Class.</dd>

            <dt>Stem (Stem 1, 2, 3...)</dt>
            <dd>A root or base variant derived from a lemma used in forming specific structures.</dd>

            <dt>Structure</dt>
            <dd>A grammatical category (e.g. Past Indicative) expressed with a formula such as <code>S1+A</code>.</dd>

            <dt>Reusable Layer</dt>
            <dd>A set of affixes/endings shared across multiple paradigm structures (e.g. Subject Person/Number agreement markers).</dd>
          </dl>
        </div>
      {:else if activeSection === 'linguist'}
        <div class="card">
          <h2>Linguist Suite Workflow</h2>
          <ol style="line-height: 1.8;">
            <li><strong>Define Inflection Classes:</strong> Group words into conjugation/declension classes.</li>
            <li><strong>Create Reusable Layers:</strong> Define shared morpheme slots (e.g. agreement affixes).</li>
            <li><strong>Define Paradigm Structures:</strong> Specify formulas combining stems and agreement layers with UniMorph tags.</li>
            <li><strong>Populate the Lexicon:</strong> Add base lemmas and stems.</li>
          </ol>
        </div>
      {:else if activeSection === 'elicit'}
        <div class="card">
          <h2>Elicitation & Validation Workflow</h2>
          <ol style="line-height: 1.8;">
            <li><strong>Elicit:</strong> Answer prompts in your language. Use AI/LLM and formula suggestions to speed up submissions.</li>
            <li><strong>Check & Rate:</strong> Review inflected forms submitted by community peers to verify accuracy and resolve spelling variations.</li>
          </ol>
        </div>
      {/if}
    </div>
  </div>
</div>

<style>
  .tutorials-layout {
    display: grid;
    grid-template-columns: 240px 1fr;
    gap: 1.5rem;
  }

  .video-container {
    position: relative;
    padding-bottom: 56.25%;
    height: 0;
    overflow: hidden;
    max-width: 100%;
    background: #000;
    border-radius: var(--radius);
    margin: 1.5rem 0;
  }

  .video-container iframe {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    border: 0;
  }

  .term-dl dt {
    font-weight: 700;
    color: var(--primary);
    margin-top: 1rem;
    font-size: 1.05rem;
  }

  .term-dl dd {
    margin-left: 0;
    margin-bottom: 0.5rem;
    color: var(--text);
  }

  @media (max-width: 768px) {
    .tutorials-layout {
      grid-template-columns: 1fr;
    }
  }
</style>
