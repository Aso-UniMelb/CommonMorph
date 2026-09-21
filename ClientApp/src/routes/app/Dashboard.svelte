<script lang="ts">
  import { onMount } from 'svelte';
  import { auth, isAdmin, isLinguist, isSpeaker } from '../../lib/stores/auth';
  import { currentMetaLang, dictionaries } from '../../lib/stores/i18n';
  import { languages, selectedLang, selectLanguage, requestLanguage } from '../../lib/stores/language';
  import { api } from '../../lib/api';
  import Modal from '../../lib/components/Modal.svelte';

  let showRoleModal = false;
  let showRequestModal = false;
  let newRoleValue = 2;

  // Language stats & Active learning
  let stats = { countAll: 0, remaining: 0 };
  let modelInfo = {
    isTrained: false,
    lastTrained: '',
    message: 'Checking status...',
    isOffline: false
  };
  let isTraining = false;
  let trainEpochs = 15;
  let trainSuccessMessage = '';
  let trainErrorMessage = '';
  let loadingStats = false;

  // Request new variety form
  let reqTitle = '';
  let reqCode = '';
  let reqValidChars = '';
  let reqDescription = '';
  let reqLatitude: number | undefined = undefined;
  let reqLongitude: number | undefined = undefined;
  let reqSuccessMessage = '';
  let reqErrorMessage = '';
  let isSubmittingReq = false;

  onMount(async () => {
    if ($selectedLang) {
      loadStats($selectedLang.id);
    }
  });

  $: if ($selectedLang) {
    loadStats($selectedLang.id);
  }

  async function loadStats(langId: number) {
    loadingStats = true;
    try {
      const data = await api.get('/Elicit/GetStats', { langid: langId });
      stats = data || { countAll: 0, remaining: 0 };
    } catch {
      stats = { countAll: 0, remaining: 0 };
    } finally {
      loadingStats = false;
    }

    try {
      const res = await api.get('/ActiveLearning/checkModelTrained', { langid: langId });
      if (res && typeof res === 'object') {
        modelInfo = {
          isTrained: !!res.isTrained,
          lastTrained: res.lastTrained || '',
          message: res.message || (res.isTrained ? 'Active' : 'Not trained yet'),
          isOffline: !!res.isOffline
        };
      }
    } catch {
      modelInfo = {
        isTrained: false,
        lastTrained: '',
        message: 'Active learning service offline',
        isOffline: true
      };
    }
  }

  async function handleRoleChange() {
    try {
      await auth.changeMyRole(newRoleValue);
      showRoleModal = false;
    } catch (err: any) {
      alert(err.message || 'Failed to change role');
    }
  }

  async function handleTrainModel() {
    if (!$selectedLang || isTraining) return;
    isTraining = true;
    trainSuccessMessage = '';
    trainErrorMessage = '';

    try {
      const res = await api.post(`/ActiveLearning/train?langid=${$selectedLang.id}&epochs=${trainEpochs}`, {
        langid: String($selectedLang.id),
        epochs: trainEpochs
      });
      if (res && res.success) {
        trainSuccessMessage = res.message || 'Neural model trained successfully.';
        if ($selectedLang) await loadStats($selectedLang.id);
      } else {
        trainErrorMessage = res?.error || 'Training failed';
      }
    } catch (err: any) {
      trainErrorMessage = err.message || 'Connection to active learning service failed.';
    } finally {
      isTraining = false;
    }
  }

  async function handleRequestVariety() {
    if (!reqTitle.trim() || !reqCode.trim()) {
      reqErrorMessage = 'Title and ISO 639-3 code are required.';
      return;
    }
    isSubmittingReq = true;
    reqErrorMessage = '';
    reqSuccessMessage = '';

    try {
      await requestLanguage({
        title: reqTitle.trim(),
        code: reqCode.trim(),
        validchars: reqValidChars.trim(),
        description: reqDescription.trim(),
        latitude: reqLatitude,
        longitude: reqLongitude
      });
      reqSuccessMessage = 'Language variety requested successfully! Administrators have been notified.';
      setTimeout(() => {
        showRequestModal = false;
        reqTitle = '';
        reqCode = '';
        reqValidChars = '';
        reqDescription = '';
        reqSuccessMessage = '';
      }, 2000);
    } catch (err: any) {
      reqErrorMessage = err.message || 'Failed to submit language request';
    } finally {
      isSubmittingReq = false;
    }
  }
</script>

<div class="dashboard-page">
  <!-- Welcome Banner -->
  <div class="card welcome-card">
    <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem;">
      <div>
        <h2 style="margin: 0;">Welcome, {$auth.name || 'Contributor'}!</h2>
        <p style="color: var(--text-muted); margin-top: 0.25rem;">
          You are contributing in CommonMorph as a <span class="badge primary" style="font-size: 0.85rem; text-transform: capitalize;">{$auth.role}</span>
        </p>
      </div>

      <div style="display: flex; gap: 0.5rem;">
        <button type="button" class="secondary small" on:click={() => { newRoleValue = $auth.role === 'linguist' ? 2 : 1; showRoleModal = true; }}>
          <span class="material-icons">swap_horiz</span> Change Role
        </button>
      </div>
    </div>
  </div>

  <!-- Variety & Metalanguage Selector & Progress Card -->
  <div class="dashboard-grid">
    <div class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem; flex-wrap: wrap; gap: 0.5rem;">
        <h3 style="margin: 0;">Language & Elicitation Settings</h3>
        <button type="button" class="primary small" on:click={() => showRequestModal = true}>
          <span class="material-icons">add_circle</span> Request Variety
        </button>
      </div>

      <div style="margin-top: 0.75rem;">
        <div class="field">
          <label for="cmbDashboardVariety">Working on:</label>
          <select 
            id="cmbDashboardVariety"
            value={$selectedLang?.id} 
            on:change={(e) => {
              const id = parseInt(e.currentTarget.value, 10);
              const found = $languages.find(l => l.id === id);
              if (found) selectLanguage(found);
            }}
          >
            {#each $languages as lang}
              <option value={lang.id}>{lang.title} ({lang.code})</option>
            {/each}
          </select>
        </div>

        <div class="field">
          <label for="cmbDashboardMetaLang">Metalanguage (Prompt Questions):</label>
          <select 
            id="cmbDashboardMetaLang"
            value={$currentMetaLang} 
            on:change={(e) => currentMetaLang.set(e.currentTarget.value)}
          >
            {#each Object.entries(dictionaries) as [code, dict]}
              <option value={code}>{dict.name}</option>
            {/each}
          </select>
        </div>
      </div>

      <!-- Elicitation Progress -->
      <div style="margin-top: 1.25rem;">
        <div style="display: flex; justify-content: space-between; margin-bottom: 0.4rem; font-size: 0.9rem;">
          <span style="font-weight: 600;">Elicitation Completion:</span>
          <span>
            {#if stats.countAll > 0}
              {stats.countAll - stats.remaining} / {stats.countAll} ({Math.ceil(((stats.countAll - stats.remaining) * 100) / stats.countAll)}%)
            {:else}
              0 items
            {/if}
          </span>
        </div>

        <div class="progress-bar-bg">
          <div 
            class="progress-bar-fill" 
            style="width: {stats.countAll > 0 ? Math.ceil(((stats.countAll - stats.remaining) * 100) / stats.countAll) : 0}%;"
          ></div>
        </div>
      </div>
    </div>

    <!-- Active Learning AI Model Card -->
    <div class="card al-card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.6rem; flex-wrap: wrap; gap: 0.5rem;">
        <h3 style="margin: 0; display: flex; align-items: center; gap: 0.4rem;">
          <span class="material-icons" style="color: var(--primary); font-size: 1.25rem;">psychology</span>
          Active Learning Neural Model
        </h3>

        {#if isTraining}
          <span class="badge warning" style="display: inline-flex; align-items: center; gap: 0.3rem;">
            <span class="material-icons rotating" style="font-size: 0.85rem;">sync</span> Training...
          </span>
        {:else if modelInfo.isTrained}
          <span class="badge success" style="display: inline-flex; align-items: center; gap: 0.3rem;">
            <span class="material-icons" style="font-size: 0.85rem;">check_circle</span> Active
          </span>
        {:else if modelInfo.isOffline}
          <span class="badge secondary" style="font-size: 0.75rem;">Offline</span>
        {:else}
          <span class="badge secondary" style="font-size: 0.75rem;">Not Trained</span>
        {/if}
      </div>

      <p style="color: var(--text-muted); font-size: 0.85rem; line-height: 1.5; margin-bottom: 0.75rem;">
        Trains an internal sequence-to-sequence neural network on verified forms to generate real-time suggestions and rank uncertainty during elicitation.
      </p>

      <div class="al-status-box">
        <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.88rem;">
          <span style="color: var(--text-muted);">Model Status:</span>
          <strong style="color: var(--text);">{modelInfo.message}</strong>
        </div>
        {#if modelInfo.lastTrained}
          <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.82rem; margin-top: 0.35rem; color: var(--text-muted);">
            <span>Last Checkpoint:</span>
            <span>{modelInfo.lastTrained}</span>
          </div>
        {/if}
      </div>

      {#if trainSuccessMessage}
        <div class="alert alert-success" style="margin-top: 0.75rem; font-size: 0.85rem; padding: 0.6rem 0.8rem;">
          <span class="material-icons" style="font-size: 1rem;">check_circle</span>
          {trainSuccessMessage}
        </div>
      {/if}

      {#if trainErrorMessage}
        <div class="alert alert-error" style="margin-top: 0.75rem; font-size: 0.85rem; padding: 0.6rem 0.8rem;">
          <span class="material-icons" style="font-size: 1rem;">error_outline</span>
          {trainErrorMessage}
        </div>
      {/if}

      <div style="margin-top: 1rem; display: flex; flex-direction: column; gap: 0.75rem;">
        <div style="display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;">
          <label for="selTrainEpochs" style="font-size: 0.85rem; font-weight: 600; color: var(--text-muted); margin: 0; white-space: nowrap;">
            Training Epochs:
          </label>
          <select 
            id="selTrainEpochs" 
            bind:value={trainEpochs} 
            disabled={isTraining}
            style="padding: 0.4rem 0.6rem; font-size: 0.85rem; border-radius: var(--radius); flex: 1; max-width: 260px;"
          >
            <option value={5}>5 Epochs (Faster, lower accuracy)</option>
            <option value={10}>10 Epochs</option>
            <option value={15}>15 Epochs</option>
            <option value={20}>20 Epochs (Higher accuracy)</option>
          </select>
        </div>

        <button 
          type="button" 
          class="button secondary" 
          on:click={handleTrainModel} 
          disabled={isTraining || !$selectedLang}
          style="width: 100%; justify-content: center;"
        >
          {#if isTraining}
            <span class="material-icons rotating" style="font-size: 1rem;">sync</span>
            Training ({trainEpochs} Epochs)...
          {:else}
            <span class="material-icons" style="font-size: 1rem;">model_training</span>
            {modelInfo.isTrained ? 'Retrain Model' : 'Train Model'}
          {/if}
        </button>
      </div>
    </div>
  </div>

  <!-- Quick Action Tiles -->
  <div style="margin-top: 1rem;">
    <h3>Workspace Modules</h3>
    <div class="tiles-grid">
      {#if $isLinguist}
        <a href="/app/linguist" class="tile-card card">
          <div class="tile-icon" style="background: #e0f2fe; color: #0284c7;">
            <span class="material-icons">manage_accounts</span>
          </div>
          <div>
            <strong>Linguist Studio</strong>
            <p>Inflection classes, paradigm formulas, agreement layers & lexicon</p>
          </div>
        </a>

        <a href="/app/qtemplates" class="tile-card card">
          <div class="tile-icon" style="background: #fef3c7; color: #d97706;">
            <span class="material-icons">contact_support</span>
          </div>
          <div>
            <strong>Question Templates</strong>
            <p>Design elicitation prompt questions for speakers</p>
          </div>
        </a>
      {/if}

      {#if $isSpeaker}
        <a href="/app/elicit" class="tile-card card">
          <div class="tile-icon" style="background: #dcfce7; color: #15803d;">
            <span class="material-icons">question_answer</span>
          </div>
          <div>
            <strong>Elicitation Tool</strong>
            <p>Submit inflected forms with AI and formula assistance</p>
          </div>
        </a>

        <a href="/app/check" class="tile-card card">
          <div class="tile-icon" style="background: #f3e8ff; color: #7e22ce;">
            <span class="material-icons">how_to_reg</span>
          </div>
          <div>
            <strong>Verification & Check</strong>
            <p>Review and rate submissions from other community members</p>
          </div>
        </a>
      {/if}

      <a href="/datasets" class="tile-card card">
        <div class="tile-icon" style="background: #fee2e2; color: #b91c1c;">
          <span class="material-icons">download</span>
        </div>
        <div>
          <strong>Datasets Export</strong>
          <p>Download full UniMorph and extended TSV files</p>
        </div>
      </a>
    </div>
  </div>
</div>

<!-- Modal: Change Role -->
<Modal isOpen={showRoleModal} title="Change Role" onClose={() => showRoleModal = false}>
  <div class="field">
    <label>Select Your Contribution Role:</label>
    <select bind:value={newRoleValue}>
      <option value={1}>Linguist (Structure paradigms & lexicon)</option>
      <option value={2}>Speaker (Elicitation & Validation)</option>
      <option value={3}>Viewer (Read-only browsing)</option>
    </select>
  </div>
  <div class="field-end">
    <button type="button" class="secondary" on:click={() => showRoleModal = false}>Cancel</button>
    <button type="button" class="primary" on:click={handleRoleChange}>Confirm Change</button>
  </div>
</Modal>

<!-- Modal: Request New Language Variety -->
<Modal isOpen={showRequestModal} title="Request New Language Variety" onClose={() => showRequestModal = false} maxWidth="600px">
  {#if reqErrorMessage}
    <div class="alert alert-error">{reqErrorMessage}</div>
  {/if}
  {#if reqSuccessMessage}
    <div class="alert alert-success">{reqSuccessMessage}</div>
  {/if}

  <form on:submit|preventDefault={handleRequestVariety}>
    <div class="field small">
      <label>Language Variety Title *</label>
      <input type="text" bind:value={reqTitle} placeholder="e.g. Kurmanji Kurdish" required />
    </div>

    <div class="field small">
      <label>ISO 639-3 Code *</label>
      <input type="text" bind:value={reqCode} placeholder="e.g. kmr" required />
    </div>

    <div class="field small">
      <label>Valid Character Set (Alphabet)</label>
      <input type="text" bind:value={reqValidChars} placeholder="e.g. abcdeêfghiîjklmnoôpqrstuûvwxyz" />
    </div>

    <div class="field small">
      <label>Description & Regional Information</label>
      <textarea bind:value={reqDescription} rows="3" placeholder="Provide background on the variety or dialect..."></textarea>
    </div>

    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
      <div class="field small">
        <label>Latitude (approx.)</label>
        <input type="number" step="0.0001" bind:value={reqLatitude} placeholder="37.5" />
      </div>
      <div class="field small">
        <label>Longitude (approx.)</label>
        <input type="number" step="0.0001" bind:value={reqLongitude} placeholder="43.5" />
      </div>
    </div>

    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showRequestModal = false}>Cancel</button>
      <button type="submit" class="primary" disabled={isSubmittingReq}>
        {#if isSubmittingReq}Submitting...{:else}Submit Request{/if}
      </button>
    </div>
  </form>
</Modal>

<style>
  .dashboard-page {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
  }

  .dashboard-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 1.5rem;
  }

  .progress-bar-bg {
    width: 100%;
    height: 12px;
    background: #e2e8f0;
    border-radius: 9999px;
    overflow: hidden;
  }

  .progress-bar-fill {
    height: 100%;
    background: var(--primary);
    transition: width 0.3s ease;
  }

  .tiles-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1.25rem;
    margin-top: 0.75rem;
  }

  .tile-card {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1.25rem;
    margin: 0;
    transition: transform 0.15s ease, border-color 0.15s ease;
  }

  .tile-card:hover {
    transform: translateY(-2px);
    border-color: var(--primary);
  }

  .tile-card p {
    font-size: 0.82rem;
    color: var(--text-muted);
    margin: 0.2rem 0 0 0;
  }

  .tile-icon {
    width: 46px;
    height: 46px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .al-status-box {
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.75rem 1rem;
    margin: 0.5rem 0;
  }

  .rotating {
    animation: spin 1.2s infinite linear;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
