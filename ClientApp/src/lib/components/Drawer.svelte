<script lang="ts">
  import { auth, isAdmin, isLinguist, isSpeaker } from '../stores/auth';
  import { t } from '../stores/i18n';
  import { locationStore, push } from '../stores/router';

  export let isOpen = false;
  export let onClose: () => void = () => {};

  function navigate(path: string) {
    onClose();
    push(path);
  }

  async function handleLogout() {
    onClose();
    await auth.logout();
    push('/user/login');
  }
</script>

{#if isOpen}
  <div class="drawer-overlay" on:click={onClose} on:keydown={(e) => e.key === 'Escape' && onClose()} role="button" tabindex="0">
    <div class="drawer-panel" on:click|stopPropagation role="region">
      <div class="drawer-header">
        <div class="nav-brand">
          <div class="logo-badge">CM</div>
          <span>CommonMorph</span>
        </div>
        <button type="button" class="secondary small" on:click={onClose}>
          <span class="material-icons">close</span>
        </button>
      </div>

      <div class="drawer-body">
        <ul class="sidebar-nav">
          <li>
            <a href="/tutorials" class:active={$locationStore === '/tutorials'} on:click={onClose}>
              <span class="material-icons">assistant</span> Tutorials
            </a>
          </li>

          {#if $auth.isAuthenticated}
            <li>
              <a href="/app/dashboard" class:active={$locationStore === '/app/dashboard'} on:click={onClose}>
                <span class="material-icons">dashboard</span> Dashboard
              </a>
            </li>

            {#if $isAdmin}
              <li>
                <a href="/app/admin" class:active={$locationStore === '/app/admin'} on:click={onClose}>
                  <span class="material-icons">admin_panel_settings</span> Admin
                </a>
              </li>
            {/if}

            {#if $isLinguist}
              <li>
                <a href="/app/linguist" class:active={$locationStore === '/app/linguist'} on:click={onClose}>
                  <span class="material-icons">manage_accounts</span> Linguist Studio
                </a>
              </li>
              <li>
                <a href="/app/qtemplates" class:active={$locationStore === '/app/qtemplates'} on:click={onClose}>
                  <span class="material-icons">contact_support</span> Question Templates
                </a>
              </li>
            {/if}

            {#if $isSpeaker}
              <li>
                <a href="/app/elicit" class:active={$locationStore === '/app/elicit'} on:click={onClose}>
                  <span class="material-icons">question_answer</span> Elicit Forms
                </a>
              </li>
              <li>
                <a href="/app/check" class:active={$locationStore === '/app/check'} on:click={onClose}>
                  <span class="material-icons">how_to_reg</span> Check & Verify
                </a>
              </li>
              <li>
                <a href="/app/survey" class:active={$locationStore === '/app/survey'} on:click={onClose}>
                  <span class="material-icons">rate_review</span> User Survey
                </a>
              </li>
            {/if}
          {/if}

          <li style="margin-top: 1rem; border-top: 1px solid var(--border); padding-top: 0.5rem;">
            <a href="/about" class:active={$locationStore === '/about'} on:click={onClose}>
              <span class="material-icons">info</span> About
            </a>
          </li>
          <li>
            <a href="/datasets" class:active={$locationStore === '/datasets'} on:click={onClose}>
              <span class="material-icons">download</span> Datasets
            </a>
          </li>
          <li>
            <a href="/search" class:active={$locationStore === '/search'} on:click={onClose}>
              <span class="material-icons">search</span> Search
            </a>
          </li>

          {#if $auth.isAuthenticated}
            <li style="margin-top: 1rem; border-top: 1px solid var(--border); padding-top: 0.5rem;">
              <button type="button" class="secondary" style="width: 100%; justify-content: flex-start;" on:click={handleLogout}>
                <span class="material-icons">logout</span> Log Out
              </button>
            </li>
          {:else}
            <li style="margin-top: 1rem; border-top: 1px solid var(--border); padding-top: 0.5rem;">
              <a href="/user/login" on:click={onClose}>
                <span class="material-icons">login</span> Sign In
              </a>
            </li>
          {/if}
        </ul>
      </div>
    </div>
  </div>
{/if}

<style>
  .drawer-panel {
    position: fixed;
    top: 0;
    left: 0;
    bottom: 0;
    width: 280px;
    background-color: var(--surface);
    z-index: 60;
    display: flex;
    flex-direction: column;
    box-shadow: var(--shadow-lg);
    animation: slideIn 0.2s ease-out;
  }

  .drawer-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem;
    border-bottom: 1px solid var(--border);
  }

  .drawer-body {
    padding: 1rem;
    overflow-y: auto;
    flex: 1;
  }

  @keyframes slideIn {
    from {
      transform: translateX(-100%);
    }
    to {
      transform: translateX(0);
    }
  }
</style>
