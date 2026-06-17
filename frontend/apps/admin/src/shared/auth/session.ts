import type { AuthTokens } from './token';

type RefreshSessionHandler = () => Promise<AuthTokens | null>;
type SessionExpiredHandler = () => void;
type TokensRefreshedHandler = (tokens: AuthTokens) => void;

let refreshSessionHandler: RefreshSessionHandler | null = null;
let refreshSessionPromise: Promise<AuthTokens | null> | null = null;
const sessionExpiredHandlers = new Set<SessionExpiredHandler>();
const tokensRefreshedHandlers = new Set<TokensRefreshedHandler>();

export function registerRefreshSessionHandler(handler: RefreshSessionHandler) {
  refreshSessionHandler = handler;

  return () => {
    if (refreshSessionHandler === handler) {
      refreshSessionHandler = null;
    }
  };
}

export async function refreshAuthSession() {
  if (!refreshSessionHandler) {
    return null;
  }

  refreshSessionPromise ??= refreshSessionHandler().finally(() => {
    refreshSessionPromise = null;
  });

  return refreshSessionPromise;
}

export function registerSessionExpiredHandler(handler: SessionExpiredHandler) {
  sessionExpiredHandlers.add(handler);

  return () => {
    sessionExpiredHandlers.delete(handler);
  };
}

export function registerTokensRefreshedHandler(handler: TokensRefreshedHandler) {
  tokensRefreshedHandlers.add(handler);

  return () => {
    tokensRefreshedHandlers.delete(handler);
  };
}

export function notifyTokensRefreshed(tokens: AuthTokens) {
  for (const handler of tokensRefreshedHandlers) {
    handler(tokens);
  }
}

export function notifySessionExpired() {
  for (const handler of sessionExpiredHandlers) {
    handler();
  }
}
