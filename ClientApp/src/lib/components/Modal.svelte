<script lang="ts">
  export let isOpen = false;
  export let title = '';
  export let onClose: () => void = () => {};
  export let maxWidth = '550px';
</script>

{#if isOpen}
  <div 
    class="modal-backdrop" 
    on:click={onClose} 
    on:keydown={(e) => e.key === 'Escape' && onClose()} 
    role="button" 
    tabindex="0"
    aria-label="Close dialog overlay"
  >
    <div 
      class="modal-dialog" 
      style="max-width: {maxWidth};" 
      on:click|stopPropagation 
      on:keydown|stopPropagation
      role="dialog" 
      aria-modal="true"
      tabindex="-1"
    >
      <div class="modal-header">
        <h3>{title}</h3>
        <button type="button" class="close-btn" on:click={onClose} aria-label="Close modal">
          <span class="material-icons">close</span>
        </button>
      </div>
      <div class="modal-body">
        <slot />
      </div>
    </div>
  </div>
{/if}

<style>
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.45);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 100;
    padding: 1rem;
    backdrop-filter: blur(2px);
  }

  .modal-dialog {
    background-color: var(--surface);
    border-radius: var(--radius);
    width: 100%;
    max-height: 90vh;
    display: flex;
    flex-direction: column;
    box-shadow: var(--shadow-lg);
    border: 1px solid var(--border);
    animation: modalIn 0.18s ease-out;
  }

  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.25rem;
    border-bottom: 1px solid var(--border);
  }

  .modal-header h3 {
    margin: 0;
    font-size: 1.15rem;
  }

  .close-btn {
    background: transparent;
    border: none;
    padding: 0.25rem;
    cursor: pointer;
    color: var(--text-muted);
  }

  .close-btn:hover {
    color: var(--text);
  }

  .modal-body {
    padding: 1.25rem;
    overflow-y: auto;
  }

  @keyframes modalIn {
    from {
      transform: scale(0.96);
      opacity: 0;
    }
    to {
      transform: scale(1);
      opacity: 1;
    }
  }
</style>
