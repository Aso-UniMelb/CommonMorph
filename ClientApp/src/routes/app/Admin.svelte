<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '../../lib/api';
  import Modal from '../../lib/components/Modal.svelte';

  let users: any[] = [];
  let languagesList: any[] = [];
  let isLoading = true;

  // Collapsible accordion state
  let isUsersOpen = false;
  let isLangsOpen = false;

  // User search/filter
  let userSearch = '';
  let langSearch = '';

  // Change user role modal
  let showRoleModal = false;
  let selectedUser: any = null;
  let newRole = 2;

  // Invite user modal
  let showInviteModal = false;
  let inviteEmail = '';
  let inviteRole = 2;
  let inviteError = '';
  let inviteSuccess = '';
  let isInviting = false;

  // Add/Edit language modal
  let showLangModal = false;
  let langId = 0;
  let langTitle = '';
  let langCode = '';
  let langValidChars = '';
  let langDescription = '';
  let langLatitude: number | undefined = undefined;
  let langLongitude: number | undefined = undefined;
  let langError = '';
  let isSavingLang = false;

  const roleLabels: Record<number, string> = {
    0: 'Admin',
    1: 'Linguist',
    2: 'Speaker',
    3: 'Viewer',
    4: 'Pending',
  };

  function getRoleLabel(role: any): string {
    if (role === null || role === undefined) return '-';
    if (typeof role === 'number') return roleLabels[role] || `Role ${role}`;
    if (typeof role === 'string') {
      const num = parseInt(role, 10);
      if (!isNaN(num) && roleLabels[num]) return roleLabels[num];
      return role.charAt(0).toUpperCase() + role.slice(1);
    }
    return String(role);
  }

  function getRoleBadgeClass(role: any): string {
    const r = typeof role === 'number' ? role : parseInt(role, 10);
    if (r === 0 || role === 'admin') return 'badge accent';
    if (r === 1 || role === 'linguist') return 'badge primary';
    if (r === 2 || role === 'speaker') return 'badge success';
    return 'badge';
  }

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    isLoading = true;
    try {
      const [u, l] = await Promise.all([
        api.get('/User/list'),
        api.get('/Lang/list'),
      ]);
      users = Array.isArray(u) ? u : [];
      languagesList = Array.isArray(l) ? l : [];
    } catch (err) {
      console.error('Failed to load admin data', err);
    } finally {
      isLoading = false;
    }
  }

  // --- Users management ---
  function openChangeRole(user: any) {
    selectedUser = user;
    if (typeof user.role === 'number') {
      newRole = user.role;
    } else if (typeof user.role === 'string') {
      const parsed = parseInt(user.role, 10);
      if (!isNaN(parsed)) newRole = parsed;
      else if (user.role.toLowerCase() === 'admin') newRole = 0;
      else if (user.role.toLowerCase() === 'linguist') newRole = 1;
      else if (user.role.toLowerCase() === 'speaker') newRole = 2;
      else if (user.role.toLowerCase() === 'viewer') newRole = 3;
    }
    showRoleModal = true;
  }

  async function handleChangeRole() {
    if (!selectedUser) return;
    try {
      await api.post(`/User/changeRole?usrId=${selectedUser.id}&newRole=${newRole}`);
      showRoleModal = false;
      await loadData();
    } catch (err: any) {
      alert(err.message || 'Failed to change role');
    }
  }

  async function handleInvite() {
    if (!inviteEmail.trim()) {
      inviteError = 'Email is required';
      return;
    }
    isInviting = true;
    inviteError = '';
    inviteSuccess = '';

    try {
      await api.post('/User/invite', {
        username: inviteEmail.trim(),
        role: inviteRole
      });
      inviteSuccess = 'Invitation sent successfully!';
      setTimeout(async () => {
        showInviteModal = false;
        inviteEmail = '';
        inviteSuccess = '';
        await loadData();
      }, 1200);
    } catch (err: any) {
      inviteError = err.message || 'Failed to invite user';
    } finally {
      isInviting = false;
    }
  }

  // --- Languages management ---
  function openAddLang() {
    langId = 0;
    langTitle = '';
    langCode = '';
    langValidChars = '';
    langDescription = '';
    langLatitude = undefined;
    langLongitude = undefined;
    langError = '';
    showLangModal = true;
  }

  function openEditLang(l: any) {
    langId = l.id;
    langTitle = l.title || '';
    langCode = l.code || '';
    langValidChars = l.validchars || '';
    langDescription = l.description || '';
    langLatitude = l.latitude;
    langLongitude = l.longitude;
    langError = '';
    showLangModal = true;
  }

  async function handleSaveLang() {
    if (!langTitle.trim() || !langCode.trim()) {
      langError = 'Title and ISO 639-3 code are required.';
      return;
    }
    isSavingLang = true;
    langError = '';

    try {
      if (langId === 0) {
        await api.post('/Lang/insert', {
          title: langTitle.trim(),
          code: langCode.trim(),
          validchars: langValidChars.trim(),
          description: langDescription.trim(),
          latitude: langLatitude,
          longitude: langLongitude
        });
      } else {
        await api.post('/Lang/update', {
          id: langId,
          title: langTitle.trim(),
          code: langCode.trim(),
          validchars: langValidChars.trim(),
          description: langDescription.trim(),
          latitude: langLatitude,
          longitude: langLongitude
        });
      }
      showLangModal = false;
      await loadData();
    } catch (err: any) {
      langError = err.message || 'Failed to save language variety';
    } finally {
      isSavingLang = false;
    }
  }

  $: filteredUsers = userSearch.trim()
    ? users.filter(u => 
        (u.username && u.username.toLowerCase().includes(userSearch.toLowerCase())) ||
        (u.name && u.name.toLowerCase().includes(userSearch.toLowerCase()))
      )
    : users;

  $: filteredLangs = langSearch.trim()
    ? languagesList.filter(l =>
        l.title.toLowerCase().includes(langSearch.toLowerCase()) ||
        l.code.toLowerCase().includes(langSearch.toLowerCase())
      )
    : languagesList;
</script>

<div class="admin-page">
  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; gap: 1rem;">
    <div>
      <h1 style="margin: 0;">Administration Panel</h1>
      <p style="color: var(--text-muted); margin: 0.25rem 0 0 0;">
        Manage platform permissions, users, and language varieties
      </p>
    </div>
    <div style="display: flex; gap: 0.5rem;">
      <button type="button" class="primary small" on:click={() => showInviteModal = true}>
        <span class="material-icons">person_add</span> Invite User
      </button>
      <button type="button" class="secondary small" on:click={openAddLang}>
        <span class="material-icons">add</span> Add Variety
      </button>
    </div>
  </div>

  {#if isLoading}
    <div style="text-align: center; padding: 4rem;">
      <span class="material-icons" style="animation: spin 1s infinite linear; font-size: 2.5rem; color: var(--primary);">sync</span>
      <p style="margin-top: 1rem;">Loading administration records...</p>
    </div>
  {:else}
    <!-- SECTION 1: Users Management (Collapsible Accordion) -->
    <div class="card accordion-card">
      <div class="accordion-header" on:click={() => isUsersOpen = !isUsersOpen}>
        <div style="display: flex; align-items: center; gap: 0.75rem;">
          <span class="material-icons chevron-icon" class:rotated={isUsersOpen}>
            expand_more
          </span>
          <h3 style="margin: 0; display: inline-flex; align-items: center; gap: 0.5rem;">
            Users Management
            <span class="badge" style="font-size: 0.8rem; font-weight: 500;">{users.length}</span>
          </h3>
        </div>

        <div style="display: flex; align-items: center; gap: 0.75rem;" on:click|stopPropagation>
          {#if isUsersOpen}
            <input 
              type="text" 
              placeholder="Search users..." 
              bind:value={userSearch} 
              style="width: 200px; padding: 0.35rem 0.65rem; font-size: 0.85rem;"
            />
          {/if}
          <button type="button" class="primary small" on:click={() => showInviteModal = true}>
            <span class="material-icons">person_add</span> Invite
          </button>
        </div>
      </div>

      {#if isUsersOpen}
        <div class="accordion-content">
          <div style="overflow-x: auto;">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Full Name</th>
                  <th>Email</th>
                  <th>Assigned Role</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {#if filteredUsers.length === 0}
                  <tr>
                    <td colspan="6" style="text-align: center; color: var(--text-muted); padding: 1.5rem;">
                      No users found.
                    </td>
                  </tr>
                {:else}
                  {#each filteredUsers as u}
                    <tr>
                      <td><b>{u.name || '-'}</b></td>
                      <td><code>{u.username}</code></td>
                      <td>
                        <span class={getRoleBadgeClass(u.role)}>
                          {getRoleLabel(u.role)}
                        </span>
                        {#if u.desiredrole !== null && u.desiredrole !== undefined}
                          <span class="badge" style="font-size: 0.8rem;">
                            {getRoleLabel(u.desiredrole)}
                          </span>
                        {:else}
                          -
                        {/if}
                      </td>
                      <td>
                        <button type="button" class="secondary small" on:click={() => openChangeRole(u)}>
                          <span class="material-icons">swap_horiz</span> Change Role
                        </button>
                      </td>
                    </tr>
                  {/each}
                {/if}
              </tbody>
            </table>
          </div>
        </div>
      {/if}
    </div>

    <!-- SECTION 2: Language Varieties (Collapsible Accordion) -->
    <div class="card accordion-card">
      <div class="accordion-header" on:click={() => isLangsOpen = !isLangsOpen}>
        <div style="display: flex; align-items: center; gap: 0.75rem;">
          <span class="material-icons chevron-icon" class:rotated={isLangsOpen}>
            expand_more
          </span>
          <h3 style="margin: 0; display: inline-flex; align-items: center; gap: 0.5rem;">
            Language Varieties
            <span class="badge" style="font-size: 0.8rem; font-weight: 500;">{languagesList.length}</span>
          </h3>
        </div>

        <div style="display: flex; align-items: center; gap: 0.75rem;" on:click|stopPropagation>
          {#if isLangsOpen}
            <input 
              type="text" 
              placeholder="Search varieties..." 
              bind:value={langSearch} 
              style="width: 200px; padding: 0.35rem 0.65rem; font-size: 0.85rem;"
            />
          {/if}
          <button type="button" class="secondary small" on:click={openAddLang}>
            <span class="material-icons">add</span> Add Variety
          </button>
        </div>
      </div>

      {#if isLangsOpen}
        <div class="accordion-content">
          <div style="overflow-x: auto;">
            <table class="data-table">
              <thead>
                <tr>
                  <th>ISO 639-3</th>
                  <th>Variety Title</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {#if filteredLangs.length === 0}
                  <tr>
                    <td colspan="6" style="text-align: center; color: var(--text-muted); padding: 1.5rem;">
                      No varieties found.
                    </td>
                  </tr>
                {:else}
                  {#each filteredLangs as l}
                    <tr>
                      <td><code>{l.code}</code></td>
                      <td><strong>{l.title}</strong></td>
                      <td>
                        <button type="button" class="secondary small" on:click={() => openEditLang(l)}>
                          <span class="material-icons">edit</span> Edit
                        </button>
                      </td>
                    </tr>
                  {/each}
                {/if}
              </tbody>
            </table>
          </div>
        </div>
      {/if}
    </div>
  {/if}
</div>

<!-- Modal: Change Role -->
<Modal isOpen={showRoleModal} title="Change User Role" onClose={() => showRoleModal = false}>
  {#if selectedUser}
    <p style="margin-top: 0;">User: <b>{selectedUser.username}</b> ({selectedUser.name || 'No name set'})</p>
    <div class="field small">
      <label for="cmbUserRoleToChange">Select New Role:</label>
      <select id="cmbUserRoleToChange" bind:value={newRole}>
        <option value={0}>Admin (Full management access)</option>
        <option value={1}>Linguist (Manage paradigms & structures)</option>
        <option value={2}>Speaker (Elicitation & Validation)</option>
        <option value={3}>Viewer (Read-only browsing)</option>
      </select>
    </div>
    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showRoleModal = false}>Cancel</button>
      <button type="button" class="primary" on:click={handleChangeRole}>Update Role</button>
    </div>
  {/if}
</Modal>

<!-- Modal: Invite User -->
<Modal isOpen={showInviteModal} title="Invite New User" onClose={() => showInviteModal = false}>
  {#if inviteError}<div class="alert alert-error">{inviteError}</div>{/if}
  {#if inviteSuccess}<div class="alert alert-success">{inviteSuccess}</div>{/if}

  <form on:submit|preventDefault={handleInvite}>
    <div class="field small">
      <label for="txtInviteEmail">Email Address *</label>
      <input id="txtInviteEmail" type="email" bind:value={inviteEmail} placeholder="colleague@university.edu" required />
    </div>

    <div class="field small">
      <label for="cmbInviteRole">Assigned Role *</label>
      <select id="cmbInviteRole" bind:value={inviteRole}>
        <option value={2}>Speaker (Elicitation & Validation)</option>
        <option value={1}>Linguist (Manage paradigms & structures)</option>
        <option value={0}>Admin (Full management access)</option>
      </select>
    </div>

    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showInviteModal = false}>Cancel</button>
      <button type="submit" class="primary" disabled={isInviting}>
        {#if isInviting}Sending invitation...{:else}Send Invitation{/if}
      </button>
    </div>
  </form>
</Modal>

<!-- Modal: Add/Edit Variety -->
<Modal isOpen={showLangModal} title="{langId === 0 ? 'Add' : 'Edit'} Language Variety" onClose={() => showLangModal = false} maxWidth="600px">
  {#if langError}<div class="alert alert-error">{langError}</div>{/if}

  <form on:submit|preventDefault={handleSaveLang}>
    <div class="field small">
      <label for="txtLangTitle">Title *</label>
      <input id="txtLangTitle" type="text" bind:value={langTitle} required placeholder="e.g. Kurmanji Kurdish" />
    </div>
    <div class="field small">
      <label for="txtLangCode">ISO 639-3 Code *</label>
      <input id="txtLangCode" type="text" bind:value={langCode} required placeholder="e.g. kmr" />
    </div>
    <div class="field small">
      <label for="txtLangValidChars">Valid Characters (Alphabet)</label>
      <input id="txtLangValidChars" type="text" bind:value={langValidChars} placeholder="e.g. abcdeêfghiî..." />
    </div>
    <div class="field small">
      <label for="txtLangDesc">Description & Regional Information</label>
      <textarea id="txtLangDesc" bind:value={langDescription} rows="3"></textarea>
    </div>
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
      <div class="field small">
        <label for="txtLangLat">Latitude</label>
        <input id="txtLangLat" type="number" step="0.0001" bind:value={langLatitude} placeholder="37.5" />
      </div>
      <div class="field small">
        <label for="txtLangLng">Longitude</label>
        <input id="txtLangLng" type="number" step="0.0001" bind:value={langLongitude} placeholder="43.5" />
      </div>
    </div>
    <div class="field-end">
      <button type="button" class="secondary" on:click={() => showLangModal = false}>Cancel</button>
      <button type="submit" class="primary" disabled={isSavingLang}>
        {#if isSavingLang}Saving...{:else}Save Variety{/if}
      </button>
    </div>
  </form>
</Modal>

<style>
  .admin-page {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
  }

  .accordion-card {
    padding: 0;
    overflow: hidden;
    margin: 0;
  }

  .accordion-header {
    background: #f8fafc;
    padding: 1rem 1.5rem;
    display: flex;
    align-items: center;
    justify-content: space-between;
    cursor: pointer;
    user-select: none;
    border-bottom: 1px solid var(--border);
    transition: background 0.15s ease;
  }

  .accordion-header:hover {
    background: #f1f5f9;
  }

  .chevron-icon {
    font-size: 1.5rem;
    color: var(--primary);
    transition: transform 0.2s ease;
  }

  .chevron-icon.rotated {
    transform: rotate(0deg);
  }

  .chevron-icon:not(.rotated) {
    transform: rotate(-90deg);
  }

  .accordion-content {
    padding: 1.25rem 1.5rem;
    background: #ffffff;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }
</style>
