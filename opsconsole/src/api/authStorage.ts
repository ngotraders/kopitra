const ACCESS_TOKEN_KEY = 'opsconsole.accessToken';

type StorageProvider = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;

function getStorage(): StorageProvider | null {
  if (typeof window === 'undefined' || !window.localStorage) {
    return null;
  }

  return window.localStorage;
}

export function getStoredAccessToken(): string | null {
  const storage = getStorage();
  if (!storage) {
    return null;
  }

  try {
    return storage.getItem(ACCESS_TOKEN_KEY);
  } catch {
    return null;
  }
}

export function storeAccessToken(token: string | null): void {
  const storage = getStorage();
  if (!storage) {
    return;
  }

  try {
    if (!token) {
      storage.removeItem(ACCESS_TOKEN_KEY);
      return;
    }

    storage.setItem(ACCESS_TOKEN_KEY, token);
  } catch {
    // Ignore storage errors in development environments.
  }
}
