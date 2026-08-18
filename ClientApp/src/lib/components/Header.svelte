<script lang="ts">
  import { auth } from '../stores/auth';
  import { selectedLang, languages, selectLanguage } from '../stores/language';
  import { locationStore, push } from '../stores/router';

  export let onToggleDrawer: () => void = () => {};

  $: isInApp = $locationStore.startsWith('/app');

  async function handleLogout() {
    await auth.logout();
    push('/user/login');
  }

  function handleLanguageChange(e: Event) {
    const target = e.target as HTMLSelectElement;
    const langId = parseInt(target.value, 10);
    const found = $languages.find(l => l.id === langId);
    if (found) selectLanguage(found);
  }
</script>

<header class="header">
  <div class="nav">
    <div style="display: flex; align-items: center; gap: 1rem;">
      <button 
        type="button" 
        class="secondary small mobile-menu-btn" 
        on:click={onToggleDrawer}
        aria-label="Toggle navigation menu"
      >
        <span class="material-icons">menu</span>
      </button>

      <a href="/" class="nav-brand">
        <div class="logo-badge">CM</div>
        <div>
          <div style="line-height: 1.1;">CommonMorph</div>
          <div style="font-size: 0.72rem; color: var(--text-muted); font-weight: 400;">Morphological Documentation</div>
        </div>
      </a>
    </div>

    <!-- Center Language / Variety selector when in app -->
    {#if isInApp && $languages.length > 0}
      <div class="header-lang-picker">
        <span style="font-size: 0.7rem; color: var(--primary);">Working on:</span>
        <select 
          value={$selectedLang?.id} 
          on:change={handleLanguageChange}
          style="padding: 0.35rem 0.6rem; font-size: 0.88rem; width: auto; font-weight: 500;"
        >
          {#each $languages as lang}
            <option value={lang.id}>{lang.title} ({lang.code})</option>
          {/each}
        </select>
      </div>
    {/if}

    <div style="display: flex; align-items: center; gap: 1rem;">
      <nav class="nav-links">
        {#if !isInApp}
          <a href="/about" class:active={$locationStore === '/about'}>About</a>
          <a href="/datasets" class:active={$locationStore === '/datasets'}>Datasets</a>
          <a href="/search" class:active={$locationStore === '/search'}>Search</a>
        {/if}
        
        {#if $auth.isAuthenticated}
          {#if !isInApp}
          <a href="/app/dashboard" class="button primary small" class:active={isInApp}>
            <span class="material-icons" style="font-size: 1rem;">dashboard</span>
            Portal
          </a>
          {/if}
          <button type="button" class="secondary small" on:click={handleLogout} title="Log Out">
            <span class="material-icons" style="font-size: 1rem;">logout</span>
          </button>
        {:else}
          <a href="/user/login" class="button primary small">Sign In</a>
        {/if}
      </nav>
    </div>
  </div>
</header>

<style>
  .mobile-menu-btn {
    display: none;
    padding: 0.4rem;
  }

  .header-lang-picker {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    background: var(--primary-light);
    padding: 0.2rem 0.6rem;
    border-radius: 6px;
  }

  @media (max-width: 768px) {
    .mobile-menu-btn {
      display: inline-flex;
    }
    .header-lang-picker {
      display: none;
    }
  }
</style>
