<script lang="ts">
  import { onMount } from 'svelte';
  import { auth } from '../../lib/stores/auth';
  import { push } from '../../lib/stores/router';

  let username = '';
  let password = '';
  let error = '';
  let isSubmitting = false;

  onMount(() => {
    // If already authenticated, redirect to dashboard
    if ($auth.isAuthenticated) {
      push('/app/dashboard');
      return;
    }

    // Google Sign-In init
    // @ts-ignore
    if (typeof window !== 'undefined' && window.google) {
      // @ts-ignore
      window.google.accounts.id.initialize({
        client_id: '502390886550-s6f1s457497l3vov30ep82r2vsq49a9o.apps.googleusercontent.com',
        callback: handleGoogleResponse
      });
      // @ts-ignore
      window.google.accounts.id.renderButton(
        document.getElementById('google-btn'),
        { theme: 'outline', size: 'large', width: '100%' }
      );
    }
  });

  async function handleGoogleResponse(response: any) {
    try {
      const res = await auth.googleLogin(response.credential);
      if (res && res.success) {
        push('/app/dashboard');
      }
    } catch (err: any) {
      error = err.message || 'Google sign-in failed.';
    }
  }

  async function handleSubmit() {
    error = '';
    isSubmitting = true;
    try {
      const res = await auth.login(username, password);
      if (res && res.success) {
        push('/app/dashboard');
      }
    } catch (err: any) {
      error = err.message || 'Invalid username or password.';
    } finally {
      isSubmitting = false;
    }
  }
</script>

<div class="auth-container">
  <div class="card auth-card">
    <div style="text-align: center; margin-bottom: 2rem;">
      <h2>Sign in to CommonMorph</h2>
      <p style="color: var(--text-muted); font-size: 0.9rem;">
        Contribute to morphological research, manage paradigms, or elicit forms.
      </p>
    </div>

    {#if error}
      <div class="alert alert-danger" style="margin-bottom: 1.5rem;">
        <span class="material-icons">error_outline</span>
        {error}
      </div>
    {/if}

    <!-- Google Sign-In button container -->
    <div id="google-btn" style="margin-bottom: 1.5rem; display: flex; justify-content: center;"></div>

    <div class="auth-divider">
      <span>or sign in with email</span>
    </div>

    <form on:submit|preventDefault={handleSubmit}>
      <div class="field small">
        <label for="login-email">Email Address</label>
        <input 
          id="login-email"
          type="email" 
          bind:value={username} 
          placeholder="name@example.com" 
          required 
        />
      </div>

      <div class="field small">
        <div style="display: flex; justify-content: space-between;">
          <label for="login-pass">Password</label>
          <a href="/user/forgot" style="font-size: 0.8rem;">Forgot password?</a>
        </div>
        <input 
          id="login-pass"
          type="password" 
          bind:value={password} 
          placeholder="••••••••" 
          required 
        />
      </div>

      <button type="submit" class="button primary" style="width: 100%; margin-top: 0.5rem;" disabled={isSubmitting}>
        {#if isSubmitting}
          <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 1.1rem;">sync</span>
          Logging in...
        {:else}
          Log In
        {/if}
      </button>
    </form>

    <div style="text-align: center; margin-top: 1.5rem; font-size: 0.9rem; color: var(--text-muted);">
      Don't have an account yet? <a href="/user/register" style="font-weight: 600;">Register here</a>
    </div>
  </div>
</div>

<style>
  .auth-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 80vh;
    padding: 2rem 1rem;
  }

  .auth-card {
    width: 100%;
    max-width: 420px;
    padding: 2.25rem;
    box-shadow: var(--shadow-lg);
  }

  .divider {
    display: flex;
    align-items: center;
    text-align: center;
    margin: 1.25rem 0;
    color: var(--text-muted);
    font-size: 0.85rem;
  }

  .divider::before,
  .divider::after {
    content: '';
    flex: 1;
    border-bottom: 1px solid var(--border);
  }

  .divider span {
    padding: 0 0.75rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
