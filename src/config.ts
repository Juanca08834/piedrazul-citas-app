export type AuthMode = 'demo' | 'keycloak';

export const appConfig = {
  apiBaseUrl: import.meta.env.VITE_API_URL ?? 'http://localhost:5184',
  authMode: (import.meta.env.VITE_AUTH_MODE ?? 'demo') as AuthMode,
  keycloakUrl: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  keycloakRealm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'piedrazul',
  keycloakClientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'piedrazul-web',
};
