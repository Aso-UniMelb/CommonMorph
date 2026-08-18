export async function apiRequest<T = any>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const defaultHeaders: Record<string, string> = {};

  if (options.body && typeof options.body === 'string') {
    defaultHeaders['Content-Type'] = 'application/json';
  }

  const response = await fetch(endpoint, {
    ...options,
    credentials: 'include',
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
  });

  if (!response.ok) {
    let errorMessage = `HTTP Error ${response.status}: ${response.statusText}`;
    try {
      const errorJson = await response.json();
      if (typeof errorJson === 'string') {
        errorMessage = errorJson;
      } else if (errorJson?.error) {
        errorMessage = errorJson.error;
      } else if (errorJson?.message) {
        errorMessage = errorJson.message;
      }
    } catch {
      try {
        const errorText = await response.text();
        if (errorText) errorMessage = errorText;
      } catch {}
    }
    throw new Error(errorMessage);
  }

  // Handle empty or text responses
  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    return await response.json();
  }
  
  return (await response.text()) as unknown as T;
}

export const api = {
  get: <T = any>(endpoint: string, params?: Record<string, any>) => {
    let url = endpoint;
    if (params) {
      const search = new URLSearchParams();
      for (const [key, value] of Object.entries(params)) {
        if (value !== undefined && value !== null) {
          search.append(key, String(value));
        }
      }
      const qs = search.toString();
      if (qs) url += (url.includes('?') ? '&' : '?') + qs;
    }
    return apiRequest<T>(url, { method: 'GET' });
  },

  post: <T = any>(endpoint: string, body?: any) => {
    return apiRequest<T>(endpoint, {
      method: 'POST',
      body: body !== undefined ? (typeof body === 'string' ? body : JSON.stringify(body)) : undefined,
    });
  },

  postForm: <T = any>(endpoint: string, formData: FormData) => {
    return apiRequest<T>(endpoint, {
      method: 'POST',
      body: formData,
    });
  }
};
