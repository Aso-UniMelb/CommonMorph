<script lang="ts">
  import { UM, Dimensions, AggrementDimensions, UM_Sort, type UMTag } from '../unimorph';

  export let value: string = '';
  export let filterAggreementOnly = false;
  export let label = 'Morphosyntactic Features (UniMorph)';

  let selectedDimension = filterAggreementOnly ? 'Person' : 'POS';
  let searchTerm = '';

  $: availableDimensions = filterAggreementOnly ? AggrementDimensions : Dimensions;

  $: activeTags = (value || '')
    .split(/[;+]/)
    .map(t => t.trim())
    .filter(Boolean);

  $: dimensionFeatures = UM.filter(tag => tag.d === selectedDimension);

  $: filteredFeatures = searchTerm.trim() 
    ? UM.filter(tag => 
        (availableDimensions.includes(tag.d)) &&
        (tag.f.toLowerCase().includes(searchTerm.toLowerCase()) || 
         tag.l.toLowerCase().includes(searchTerm.toLowerCase())))
    : dimensionFeatures;

  function toggleTag(tagLabel: string) {
    let current = [...activeTags];
    const index = current.indexOf(tagLabel);
    if (index >= 0) {
      current.splice(index, 1);
    } else {
      current.push(tagLabel);
    }
    value = UM_Sort(current.join(';'));
  }

  function removeTag(tagLabel: string) {
    let current = activeTags.filter(t => t !== tagLabel);
    value = UM_Sort(current.join(';'));
  }

  function clearAll() {
    value = '';
  }
</script>

<div class="unimorph-selector">
  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.4rem;">
    <span style="font-size: 0.875rem; font-weight: 500; color: var(--text);">{label}</span>
    {#if activeTags.length > 0}
      <button type="button" class="secondary small" on:click={clearAll} style="padding: 0.15rem 0.45rem; font-size: 0.75rem;">
        Clear All
      </button>
    {/if}
  </div>

  <!-- Selected Tag Badges -->
  <div class="selected-tags-box">
    {#if activeTags.length === 0}
      <span style="color: var(--text-muted); font-size: 0.85rem; font-style: italic;">No features selected yet. Pick from below.</span>
    {:else}
      {#each activeTags as tag}
        {@const found = UM.find(u => u.l === tag)}
        <span class="tag-badge">
          <b>{tag}</b>
          <span style="font-size: 0.75rem; opacity: 0.85;">({found ? found.f : tag})</span>
          <button 
            type="button" 
            class="remove-tag-btn" 
            on:click={() => removeTag(tag)}
            aria-label="Remove {tag}"
          >
            <span class="material-icons" style="font-size: 0.95rem;">close</span>
          </button>
        </span>
      {/each}
    {/if}
  </div>

  <!-- UniMorph output text string -->
  <div class="tag-output-row">
    <label for="txtUniMorphOutput" style="font-size: 0.82rem; font-weight: 600; color: var(--text-muted); margin: 0; min-width: 120px;">
      UniMorph String:
    </label>
    <input 
      id="txtUniMorphOutput"
      type="text" 
      bind:value={value} 
      readonly 
      style="background: #f8fafc; font-family: monospace; font-size: 0.85rem; padding: 0.4rem 0.6rem;" 
    />
  </div>

  <!-- Dimension tabs and tag chooser -->
  <div class="picker-container">
    <div class="dimension-tabs">
      {#each availableDimensions as dim}
        <button
          type="button"
          class="dim-tab"
          class:active={selectedDimension === dim && !searchTerm}
          on:click={() => { selectedDimension = dim; searchTerm = ''; }}
        >
          {dim}
        </button>
      {/each}
    </div>

    <div class="features-picker">
      <div style="margin-bottom: 0.5rem;">
        <input 
          type="text" 
          placeholder="Filter features by name or tag code..." 
          bind:value={searchTerm}
          style="padding: 0.35rem 0.65rem; font-size: 0.85rem;"
          aria-label="Search UniMorph features"
        />
      </div>

      <div class="features-grid">
        {#each filteredFeatures as item}
          {@const isSelected = activeTags.includes(item.l)}
          <button
            type="button"
            class="feature-btn"
            class:selected={isSelected}
            on:click={() => toggleTag(item.l)}
          >
            <span class="tag-code">{item.l}</span>
            <span class="tag-name">{item.f}</span>
            {#if isSelected}
              <span class="material-icons" style="font-size: 1rem; color: var(--primary);">check</span>
            {/if}
          </button>
        {/each}
      </div>
    </div>
  </div>
</div>

<style>
  .unimorph-selector {
    background-color: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem;
    margin-bottom: 1rem;
  }

  .selected-tags-box {
    min-height: 42px;
    padding: 0.4rem;
    background: #f8fafc;
    border: 1px dashed var(--border);
    border-radius: 6px;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.3rem;
    margin-bottom: 0.75rem;
  }

  .remove-tag-btn {
    background: transparent;
    border: none;
    padding: 0;
    margin-left: 0.2rem;
    display: inline-flex;
    align-items: center;
    cursor: pointer;
    color: inherit;
    opacity: 0.7;
  }

  .remove-tag-btn:hover {
    opacity: 1;
  }

  .tag-output-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.75rem;
  }

  .picker-container {
    display: grid;
    grid-template-columns: 160px 1fr;
    border: 1px solid var(--border);
    border-radius: 6px;
    overflow: hidden;
    height: 220px;
  }

  .dimension-tabs {
    background: #f8fafc;
    border-right: 1px solid var(--border);
    overflow-y: auto;
    display: flex;
    flex-direction: column;
  }

  .dim-tab {
    text-align: left;
    padding: 0.4rem 0.65rem;
    font-size: 0.8rem;
    background: transparent;
    border: none;
    border-bottom: 1px solid #f1f5f9;
    color: var(--text);
    border-radius: 0;
    cursor: pointer;
    justify-content: flex-start;
  }

  .dim-tab:hover {
    background: #eef2f6;
  }

  .dim-tab.active {
    background: var(--primary-light);
    color: var(--primary);
    font-weight: 600;
  }

  .features-picker {
    padding: 0.6rem;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .features-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(170px, 1fr));
    gap: 0.4rem;
    overflow-y: auto;
    padding-right: 0.3rem;
  }

  .feature-btn {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.35rem 0.55rem;
    background: #fff;
    border: 1px solid var(--border);
    border-radius: 5px;
    font-size: 0.82rem;
    cursor: pointer;
    text-align: left;
  }

  .feature-btn:hover {
    border-color: var(--primary);
    background: #fdfdfd;
  }

  .feature-btn.selected {
    background: var(--primary-light);
    border-color: var(--primary);
    font-weight: 600;
  }

  .tag-code {
    font-weight: 700;
    color: var(--primary);
    margin-right: 0.4rem;
    font-family: monospace;
  }

  .tag-name {
    flex: 1;
    color: var(--text);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  @media (max-width: 640px) {
    .picker-container {
      grid-template-columns: 1fr;
      height: 300px;
    }
    .dimension-tabs {
      flex-direction: row;
      border-right: none;
      border-bottom: 1px solid var(--border);
      max-height: 42px;
      overflow-x: auto;
      overflow-y: hidden;
    }
  }
</style>
