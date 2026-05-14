export interface InternalDirectoryAccount {
  email?: string;
  documentNumber?: string;
  password: string;
  displayName: string;
  subject: string;
  roles: string[];
}

const ACCOUNTS_STORAGE_KEY = 'piedrazul-accounts';

export function readInternalDirectory(): InternalDirectoryAccount[] {
  const stored = localStorage.getItem(ACCOUNTS_STORAGE_KEY);
  if (!stored) return [];
  try {
    return JSON.parse(stored) as InternalDirectoryAccount[];
  } catch {
    return [];
  }
}

export function saveInternalDirectory(accounts: InternalDirectoryAccount[]) {
  localStorage.setItem(ACCOUNTS_STORAGE_KEY, JSON.stringify(accounts));
}
