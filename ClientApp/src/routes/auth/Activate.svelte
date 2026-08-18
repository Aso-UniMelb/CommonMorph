<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '../../lib/api';
  import { auth } from '../../lib/stores/auth';
  import { push } from '../../lib/stores/router';

  let username = '';
  let code = '';
  let name = '';
  let password = '';
  let confirmPassword = '';
  let acceptedTerms = false;
  let error = '';
  let isSubmitting = false;

  onMount(() => {
    // Parse query parameters from search or legacy hash
    const qs = window.location.search 
      ? window.location.search.substring(1) 
      : (window.location.hash.includes('?') ? window.location.hash.split('?')[1] : '');
    if (qs) {
      const params = new URLSearchParams(qs);
      username = params.get('username') || '';
      code = params.get('code') || '';
    }
  });

  async function handleActivate() {
    if (!username.trim() || !code.trim()) {
      error = 'Username and activation code are required.';
      return;
    }
    if (!name.trim()) {
      error = 'Please provide your full name.';
      return;
    }
    if (password.length < 6) {
      error = 'Password must be at least 6 characters.';
      return;
    }
    if (password !== confirmPassword) {
      error = 'Passwords do not match.';
      return;
    }
    if (!acceptedTerms) {
      error = 'You must accept the terms of use.';
      return;
    }

    isSubmitting = true;
    error = '';
    try {
      const res = await api.post('/User/activate-api', {
        username: username.trim(),
        code: code.trim(),
        password,
        name: name.trim()
      });
      if (res && res.success) {
        await auth.checkAuth();
        push('/app/dashboard');
      }
    } catch (err: any) {
      error = err.message || 'Activation failed. Invalid code or user.';
    } finally {
      isSubmitting = false;
    }
  }
</script>

<div class="auth-container">
  <div class="auth-card card">
    <div style="text-align: center; margin-bottom: 1.5rem;">
      <div class="logo-badge" style="margin: 0 auto 0.75rem;">CM</div>
      <h2>Activate & Set Password</h2>
      <p style="color: var(--text-muted); font-size: 0.9rem;">
        Complete your registration to start contributing
      </p>
    </div>

    {#if error}
      <div class="alert alert-error">{error}</div>
    {/if}

    <form on:submit|preventDefault={handleActivate}>
      <div class="field small">
        <label for="act-email">Email Address</label>
        <input 
          id="act-email"
          type="email" 
          bind:value={username} 
          placeholder="name@example.com" 
          required 
        />
      </div>

      <div class="field small">
        <label for="act-code">6-digit Activation Code</label>
        <input 
          id="act-code"
          type="text" 
          bind:value={code} 
          placeholder="e.g. 123456" 
          required 
        />
      </div>

      <div class="field small">
        <label for="act-name">Your Full Name</label>
        <input 
          id="act-name"
          type="text" 
          bind:value={name} 
          placeholder="Your name" 
          required 
        />
      </div>

      <div class="field small">
        <label for="act-pass">Create Password</label>
        <input 
          id="act-pass"
          type="password" 
          bind:value={password} 
          placeholder="••••••••" 
          required 
        />
      </div>

      <div class="field small">
        <label for="act-confpass">Confirm Password</label>
        <input 
          id="act-confpass"
          type="password" 
          bind:value={confirmPassword} 
          placeholder="••••••••" 
          required 
        />
      </div>

      <!-- Terms of Use -->
      <div class="terms-card">
        <strong>Terms of Participation:</strong>
        <ul>
          <li>Your contributions involve submitting or verifying linguistic data.</li>
          <li>Personal data is kept strictly confidential and not published.</li>
          <li>Contributed data is released openly for academic research & preservation.</li>
        </ul>
      </div>

      <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 1.25rem;">
        <input type="checkbox" id="chk-terms" bind:checked={acceptedTerms} />
        <label for="chk-terms" style="margin: 0; cursor: pointer; font-size: 0.85rem;">
          I accept the terms of participation
        </label>
      </div>

      <button type="submit" class="button primary" style="width: 100%;" disabled={isSubmitting || !acceptedTerms}>
        {#if isSubmitting}
          <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 1.1rem;">sync</span>
          Activating...
        {:else}
          Activate Account & Enter Portal
        {/if}
      </button>
    </form>
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
    max-width: 480px;
    padding: 2.25rem;
    box-shadow: var(--shadow-lg);
  }

  .terms-card {
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.75rem 1rem;
    font-size: 0.8rem;
    color: var(--text-muted);
    margin-bottom: 1rem;
  }

  .terms-card ul {
    margin: 0.35rem 0 0 0;
    padding-left: 1.2rem;
  }

  .terms-card li {
    margin-bottom: 0.25rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
