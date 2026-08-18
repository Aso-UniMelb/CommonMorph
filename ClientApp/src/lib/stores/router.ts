import { writable } from 'svelte/store';

export function getPath(): string {
  if (typeof window === 'undefined') return '/';
  // Handle any legacy hash format gracefully
  if (window.location.hash && window.location.hash.startsWith('#/')) {
    return window.location.hash.substring(1).split('?')[0] || '/';
  }
  return window.location.pathname || '/';
}

export const locationStore = writable<string>(getPath());

export function push(url: string) {
  if (typeof window === 'undefined') return;
  const path = url.startsWith('#') ? url.substring(1) : url;
  window.history.pushState({}, '', path);
  locationStore.set(getPath());
  window.scrollTo({ top: 0, behavior: 'instant' });
}

export function replace(url: string) {
  if (typeof window === 'undefined') return;
  const path = url.startsWith('#') ? url.substring(1) : url;
  window.history.replaceState({}, '', path);
  locationStore.set(getPath());
}

if (typeof window !== 'undefined') {
  // If user opens a legacy hash bookmark like /#/app/dashboard, normalize to /app/dashboard
  if (window.location.hash && window.location.hash.startsWith('#/')) {
    const cleanPath = window.location.hash.substring(1);
    window.history.replaceState({}, '', cleanPath);
    locationStore.set(getPath());
  }

  window.addEventListener('popstate', () => {
    locationStore.set(getPath());
  });

  // Global click listener for client-side routing on internal <a> links
  document.addEventListener('click', (e) => {
    const target = (e.target as HTMLElement).closest('a');
    if (!target) return;
    const href = target.getAttribute('href');
    if (!href) return;

    // Ignore external URLs, javascript:, mailto:, tel:, file downloads, or target="_blank"
    if (
      target.target === '_blank' ||
      href.startsWith('http://') ||
      href.startsWith('https://') ||
      href.startsWith('mailto:') ||
      href.startsWith('tel:') ||
      href.startsWith('javascript:') ||
      href.startsWith('/download/') ||
      target.hasAttribute('download')
    ) {
      return;
    }

    e.preventDefault();
    push(href);
  });
}
