import { writable } from 'svelte/store';
import { api } from '../api';

export interface Language {
  id: number;
  code: string;
  title: string;
  validchars?: string;
  description?: string;
  latitude?: number;
  longitude?: number;
}

export const languages = writable<Language[]>([]);
export const selectedLang = writable<Language | null>(null);

let initialLangId: number | null = null;
if (typeof localStorage !== 'undefined') {
  const saved = localStorage.getItem('cm_lang_id');
  if (saved) initialLangId = parseInt(saved, 10);
}

export async function loadLanguages(): Promise<Language[]> {
  try {
    const list: Language[] = await api.get('/Lang/list');
    languages.set(list);

    if (list && list.length > 0) {
      let chosen = list.find(l => l.id === initialLangId);
      if (!chosen) chosen = list[0];
      selectedLang.set(chosen);
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem('cm_lang_id', String(chosen.id));
      }
    }
    return list;
  } catch (err) {
    console.error('Failed to load languages', err);
    return [];
  }
}

export function selectLanguage(lang: Language) {
  selectedLang.set(lang);
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem('cm_lang_id', String(lang.id));
  }
}

export async function requestLanguage(data: Partial<Language>): Promise<any> {
  return await api.post('/Lang/request', data);
}
