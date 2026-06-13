const accessTokenKey = 'lucid.admin.accessToken';
const refreshTokenKey = 'lucid.admin.refreshToken';
const accessTokenExpiresAtKey = 'lucid.admin.accessTokenExpiresAt';
const refreshTokenExpiresAtKey = 'lucid.admin.refreshTokenExpiresAt';

export interface AuthTokens {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export function getAccessToken() {
  return localStorage.getItem(accessTokenKey);
}

export function getRefreshToken() {
  return localStorage.getItem(refreshTokenKey);
}

export function getStoredTokens(): AuthTokens | null {
  const accessToken = localStorage.getItem(accessTokenKey);
  const expiresAt = localStorage.getItem(accessTokenExpiresAtKey);
  const refreshToken = localStorage.getItem(refreshTokenKey);
  const refreshTokenExpiresAt = localStorage.getItem(refreshTokenExpiresAtKey);

  if (!accessToken || !expiresAt || !refreshToken || !refreshTokenExpiresAt) {
    return null;
  }

  return {
    accessToken,
    expiresAt,
    refreshToken,
    refreshTokenExpiresAt,
  };
}

export function saveTokens(tokens: AuthTokens) {
  localStorage.setItem(accessTokenKey, tokens.accessToken);
  localStorage.setItem(accessTokenExpiresAtKey, tokens.expiresAt);
  localStorage.setItem(refreshTokenKey, tokens.refreshToken);
  localStorage.setItem(refreshTokenExpiresAtKey, tokens.refreshTokenExpiresAt);
}

export function clearTokens() {
  localStorage.removeItem(accessTokenKey);
  localStorage.removeItem(accessTokenExpiresAtKey);
  localStorage.removeItem(refreshTokenKey);
  localStorage.removeItem(refreshTokenExpiresAtKey);
}
