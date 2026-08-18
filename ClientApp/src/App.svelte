<script lang="ts">
  import { onMount } from 'svelte';
  import { auth, isAdmin, isLinguist, isSpeaker } from './lib/stores/auth';
  import { loadLanguages } from './lib/stores/language';
  import { t } from './lib/stores/i18n';
  import { locationStore, push } from './lib/stores/router';
  import Header from './lib/components/Header.svelte';
  import Drawer from './lib/components/Drawer.svelte';

  // Route Views
  import Home from './routes/Home.svelte';
  import About from './routes/About.svelte';
  import Datasets from './routes/Datasets.svelte';
  import DatasetDetail from './routes/DatasetDetail.svelte';
  import Search from './routes/Search.svelte';
  import Tutorials from './routes/Tutorials.svelte';
  import Privacy from './routes/Privacy.svelte';

  import Login from './routes/auth/Login.svelte';
  import Register from './routes/auth/Register.svelte';
  import Activate from './routes/auth/Activate.svelte';
  import Forgot from './routes/auth/Forgot.svelte';

  import Dashboard from './routes/app/Dashboard.svelte';
  import Admin from './routes/app/Admin.svelte';
  import Linguist from './routes/app/Linguist.svelte';
  import QTemplates from './routes/app/QTemplates.svelte';
  import Elicit from './routes/app/Elicit.svelte';
  import Check from './routes/app/Check.svelte';
  import Survey from './routes/app/Survey.svelte';

  let isDrawerOpen = false;

  const routes: Record<string, any> = {
    '/': Home,
    '/about': About,
    '/datasets': Datasets,
    '/dataset/:langid': DatasetDetail,
    '/search': Search,
    '/tutorials': Tutorials,
    '/privacy': Privacy,

    '/user/login': Login,
    '/user/register': Register,
    '/user/activate': Activate,
    '/user/forgot': Forgot,

    '/app/dashboard': Dashboard,
    '/app/admin': Admin,
    '/app/linguist': Linguist,
    '/app/qtemplates': QTemplates,
    '/app/elicit': Elicit,
    '/app/check': Check,
    '/app/survey': Survey,

    '*': Home,
  };

  function matchRoute(pathname: string) {
    if (routes[pathname]) return { component: routes[pathname], params: {} };

    for (const [pattern, comp] of Object.entries(routes)) {
      if (pattern.includes(':')) {
        const patternParts = pattern.split('/');
        const pathParts = pathname.split('/');
        if (patternParts.length === pathParts.length) {
          const params: Record<string, string> = {};
          let match = true;
          for (let i = 0; i < patternParts.length; i++) {
            if (patternParts[i].startsWith(':')) {
              params[patternParts[i].slice(1)] = pathParts[i];
            } else if (patternParts[i] !== pathParts[i]) {
              match = false;
              break;
            }
          }
          if (match) return { component: comp, params };
        }
      }
    }
    return { component: routes['*'] || Home, params: {} };
  }

  $: currentLocation = $locationStore;
  $: isInApp = $locationStore.startsWith('/app');
  $: currentMatch = matchRoute($locationStore);

  onMount(async () => {
    await Promise.all([
      auth.checkAuth(),
      loadLanguages(),
    ]);
  });
</script>

<div class="app-wrapper">
  <!-- Top Navigation Bar -->
  <Header onToggleDrawer={() => isDrawerOpen = !isDrawerOpen} />

  <!-- Mobile Drawer -->
  <Drawer isOpen={isDrawerOpen} onClose={() => isDrawerOpen = false} />

  {#if isInApp}
    <!-- Protected Application Portal Layout with Sidebar -->
    <div class="app-layout">
      <aside class="app-sidebar">
        <ul class="sidebar-nav">
          <li>
            <a href="/tutorials" class:active={currentLocation === '/tutorials'}>
              <span class="material-icons">assistant</span> Tutorials
            </a>
          </li>
          <li>
            <a href="/app/dashboard" class:active={currentLocation === '/app/dashboard'}>
              <span class="material-icons">dashboard</span> Dashboard
            </a>
          </li>

          {#if $isAdmin}
            <li>
              <a href="/app/admin" class:active={currentLocation === '/app/admin'}>
                <span class="material-icons">admin_panel_settings</span> Admin
              </a>
            </li>
          {/if}

          {#if $isLinguist}
            <li>
              <a href="/app/linguist" class:active={currentLocation === '/app/linguist'}>
                <span class="material-icons">manage_accounts</span> Linguist Studio
              </a>
            </li>
            <li>
              <a href="/app/qtemplates" class:active={currentLocation === '/app/qtemplates'}>
                <span class="material-icons">contact_support</span> Question Templates
              </a>
            </li>
          {/if}

          {#if $isSpeaker}
            <li>
              <a href="/app/elicit" class:active={currentLocation === '/app/elicit'}>
                <span class="material-icons">question_answer</span> Elicit Forms
              </a>
            </li>
            <li>
              <a href="/app/check" class:active={currentLocation === '/app/check'}>
                <span class="material-icons">how_to_reg</span> Check & Verify
              </a>
            </li>
            <li>
              <a href="/app/survey" class:active={currentLocation === '/app/survey'}>
                <span class="material-icons">rate_review</span> User Survey
              </a>
            </li>
          {/if}

          <li style="margin-top: 1.5rem; border-top: 1px solid var(--border); padding-top: 0.75rem;">
            <a href="/datasets">
              <span class="material-icons">download</span> Datasets
            </a>
          </li>
          <li>
            <button 
              type="button" 
              class="secondary small" 
              style="width: 100%; justify-content: flex-start; margin-top: 0.5rem;"
              on:click={async () => { await auth.logout(); push('/user/login'); }}
            >
              <span class="material-icons">logout</span> Log Out
            </button>
          </li>
        </ul>
      </aside>

      <main class="app-content">
        <svelte:component this={currentMatch.component} params={currentMatch.params} />
      </main>
    </div>
  {:else}
    <!-- Public & Auth Page Layout -->
    <main style="flex: 1;">
      <svelte:component this={currentMatch.component} params={currentMatch.params} />
    </main>

    <footer class="footer">
      <div class="footer-content">
        <div>© 2025–2026 CommonMorph Project</div>
        <div class="footer-links">
          <a href="/about">About</a>
          <a href="/datasets">Datasets</a>
          <a href="/search">Search</a>
          <a href="/tutorials">Tutorials</a>
          <a href="/privacy">Privacy</a>
        </div>
      </div>
    </footer>
  {/if}
</div>
