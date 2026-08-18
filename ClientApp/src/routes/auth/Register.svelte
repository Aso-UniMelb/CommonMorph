<script lang="ts">
  import { api } from '../../lib/api';
  import { push } from '../../lib/stores/router';

  let username = '';
  let desiredRole = 2; // speaker by default
  let message = '';
  let error = '';
  let isSubmitting = false;

  async function handleRegister() {
    if (!username.trim()) {
      error = 'Please provide a valid email address.';
      return;
    }
    isSubmitting = true;
    error = '';
    message = '';
    try {
      const res = await api.post('/User/register-api', {
        username: username.trim(),
        desiredRole: desiredRole
      });
      if (res && res.success) {
        message = res.message || 'Registration email sent. Please check your inbox!';
        setTimeout(() => {
          push(`/user/activate?username=${encodeURIComponent(username.trim())}`);
        }, 2000);
      }
    } catch (err: any) {
      error = err.message || 'Registration failed. User may already exist.';
    } finally {
      isSubmitting = false;
    }
  }
</script>

<div class="auth-container">
  <div class="auth-card card">
    <div style="text-align: center; margin-bottom: 1.5rem;">
      <div class="logo-badge" style="margin: 0 auto 0.75rem;">CM</div>
      <h2>Create an Account</h2>
      <p style="color: var(--text-muted); font-size: 0.9rem;">
        Join the CommonMorph documentation community
      </p>
    </div>

    {#if error}
      <div class="alert alert-error">{error}</div>
    {/if}

    {#if message}
      <div class="alert alert-success">{message}</div>
    {/if}

    <form on:submit|preventDefault={handleRegister}>
      <div class="field small">
        <label for="reg-email">Valid Email Address</label>
        <input 
          id="reg-email"
          type="email" 
          bind:value={username} 
          placeholder="name@example.com" 
          required 
        />
      </div>

      <div class="field small">
        <label for="reg-role">Preferred Role</label>
        <select id="reg-role" bind:value={desiredRole}>
          <option value={2}>Speaker (submitting and validating forms)</option>
          <option value={1}>Linguist (managing paradigm structures)</option>
          <option value={3}>Viewer (browsing datasets)</option>
        </select>
      </div>

      <button type="submit" class="button primary" style="width: 100%; margin-top: 0.75rem;" disabled={isSubmitting}>
        {#if isSubmitting}
          <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 1.1rem;">sync</span>
          Sending registration code...
        {:else}
          Send Registration Code
        {/if}
      </button>
    </form>

    <div style="text-align: center; margin-top: 1.5rem; font-size: 0.9rem; color: var(--text-muted);">
      Already have an account? <a href="/user/login" style="font-weight: 600;">Log in</a>
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
    max-width: 440px;
    padding: 2.25rem;
    box-shadow: var(--shadow-lg);
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
