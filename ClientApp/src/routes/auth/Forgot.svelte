<script lang="ts">
  import { api } from '../../lib/api';
  import { push } from '../../lib/stores/router';

  let username = '';
  let message = '';
  let error = '';
  let isSubmitting = false;

  async function handleForgot() {
    if (!username.trim()) {
      error = 'Please enter your email address.';
      return;
    }

    isSubmitting = true;
    error = '';
    message = '';

    try {
      const res = await api.post('/User/forgot-api', { username: username.trim() });
      if (res && res.success) {
        message = res.message || 'Password reset link sent to your email.';
        setTimeout(() => {
          push(`/user/activate?username=${encodeURIComponent(username.trim())}`);
        }, 2000);
      }
    } catch (err: any) {
      error = err.message || 'Failed to send reset code.';
    } finally {
      isSubmitting = false;
    }
  }
</script>

<div class="auth-container">
  <div class="auth-card card">
    <div style="text-align: center; margin-bottom: 1.5rem;">
      <div class="logo-badge" style="margin: 0 auto 0.75rem;">CM</div>
      <h2>Reset Password</h2>
      <p style="color: var(--text-muted); font-size: 0.9rem;">
        We will send a reset code to your registered email
      </p>
    </div>

    {#if error}
      <div class="alert alert-error">{error}</div>
    {/if}

    {#if message}
      <div class="alert alert-success">{message}</div>
    {/if}

    <form on:submit|preventDefault={handleForgot}>
      <div class="field small">
        <label for="forgot-email">Your Registered Email</label>
        <input 
          id="forgot-email"
          type="email" 
          bind:value={username} 
          placeholder="name@example.com" 
          required 
        />
      </div>

      <button type="submit" class="button primary" style="width: 100%; margin-top: 0.5rem;" disabled={isSubmitting}>
        {#if isSubmitting}
          <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 1.1rem;">sync</span>
          Sending reset code...
        {:else}
          Send Password Reset Code
        {/if}
      </button>
    </form>

    <div style="text-align: center; margin-top: 1.5rem; font-size: 0.9rem;">
      <a href="/user/login" style="font-weight: 600;">Back to Login</a>
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

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
