import { writable, derived } from 'svelte/store';
import { api } from '../api';

export interface UserState {
  id: number;
  name: string;
  role: 'admin' | 'linguist' | 'speaker' | 'viewer' | 'pending' | string;
  email: string;
  isAuthenticated: boolean;
  isLoading: boolean;
}

const initialUser: UserState = {
  id: 0,
  name: '',
  role: '',
  email: '',
  isAuthenticated: false,
  isLoading: true,
};

function createAuthStore() {
  const { subscribe, set, update } = writable<UserState>(initialUser);

  return {
    subscribe,

    checkAuth: async () => {
      try {
        const res = await api.get('/User/me');
        if (res && res.isAuthenticated && res.user) {
          set({
            id: res.user.id,
            name: res.user.name || '',
            role: res.user.role || 'speaker',
            email: res.user.email || '',
            isAuthenticated: true,
            isLoading: false,
          });
          return true;
        } else {
          set({ ...initialUser, isLoading: false });
          return false;
        }
      } catch {
        set({ ...initialUser, isLoading: false });
        return false;
      }
    },

    login: async (username: string, password: string) => {
      const res = await api.post('/User/login-api', { username, password });
      if (res && res.success && res.user) {
        set({
          id: res.user.id,
          name: res.user.name || '',
          role: res.user.role || 'speaker',
          email: res.user.email || '',
          isAuthenticated: true,
          isLoading: false,
        });
        return res;
      }
      throw new Error(res?.error || 'Login failed');
    },

    googleLogin: async (credential: string) => {
      const res = await api.post('/User/google-login-api', { credential });
      if (res && res.success && res.user) {
        set({
          id: res.user.id,
          name: res.user.name || '',
          role: res.user.role || 'speaker',
          email: res.user.email || '',
          isAuthenticated: true,
          isLoading: false,
        });
        return res;
      }
      throw new Error(res?.error || 'Google login failed');
    },

    logout: async () => {
      try {
        await api.post('/User/logout-api');
      } finally {
        set({ ...initialUser, isLoading: false });
      }
    },

    changeMyRole: async (newRole: number) => {
      const res = await api.post(`/User/changeMyRole?newRole=${newRole}`);
      // Refresh current user info
      const me = await api.get('/User/me');
      if (me && me.user) {
        update(u => ({ ...u, role: me.user.role }));
      }
      return res;
    }
  };
}

export const auth = createAuthStore();

export const isAdmin = derived(auth, $auth => $auth.role.toLowerCase() === 'admin');
export const isLinguist = derived(auth, $auth => $auth.role.toLowerCase() === 'linguist' || $auth.role.toLowerCase() === 'admin');
export const isSpeaker = derived(auth, $auth => ['speaker', 'linguist', 'admin'].includes($auth.role.toLowerCase()));
