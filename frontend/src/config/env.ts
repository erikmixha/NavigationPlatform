/**
 * Environment configuration for the application.
 */
interface ImportMetaEnv {
  readonly VITE_KEYCLOAK_URL?: string;
  readonly VITE_KEYCLOAK_REALM?: string;
  readonly VITE_APP_URL?: string;
  readonly VITE_KEYCLOAK_CLIENT_ID?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

export const env = {
  keycloakUrl:
    (import.meta as unknown as ImportMeta).env?.VITE_KEYCLOAK_URL || 'http://localhost:8080',
  keycloakRealm: (import.meta as unknown as ImportMeta).env?.VITE_KEYCLOAK_REALM || 'navplat',
  appUrl: (import.meta as unknown as ImportMeta).env?.VITE_APP_URL || 'http://localhost:3000',
  keycloakClientId:
    (import.meta as unknown as ImportMeta).env?.VITE_KEYCLOAK_CLIENT_ID || 'navplat-gateway',
};
