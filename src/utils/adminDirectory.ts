export interface InternalDirectoryAccount {
  email?: string;
  documentNumber?: string;
  password: string;
  displayName: string;
  subject: string;
  roles: string[];
}

const ACCOUNTS_STORAGE_KEY = 'piedrazul-accounts';

const seededAccounts: InternalDirectoryAccount[] = [
  {
    email: 'paciente@piedrazul.local',
    documentNumber: '1000000001',
    password: 'Paciente123*',
    displayName: 'Paciente Demo',
    subject: 'demo-patient',
    roles: ['Patient'],
  },
  {
    email: 'admin@piedrazul.local',
    documentNumber: '900000001',
    password: 'Admin123*',
    displayName: 'Administrador Piedrazul',
    subject: 'staff-admin@piedrazul.local',
    roles: ['Admin'],
  },
  {
    email: 'agenda@piedrazul.local',
    documentNumber: '900000002',
    password: 'Agenda123*',
    displayName: 'Agendador Piedrazul',
    subject: 'staff-agenda@piedrazul.local',
    roles: ['Scheduler'],
  },
  {
    email: 'medico@piedrazul.local',
    documentNumber: '900000003',
    password: 'Medico123*',
    displayName: 'Profesional Piedrazul',
    subject: 'staff-medico@piedrazul.local',
    roles: ['Doctor'],
  },
];

export function readInternalDirectory() {
  const stored = localStorage.getItem(ACCOUNTS_STORAGE_KEY);
  if (!stored) {
    localStorage.setItem(ACCOUNTS_STORAGE_KEY, JSON.stringify(seededAccounts));
    return seededAccounts;
  }

  try {
    return JSON.parse(stored) as InternalDirectoryAccount[];
  } catch {
    localStorage.setItem(ACCOUNTS_STORAGE_KEY, JSON.stringify(seededAccounts));
    return seededAccounts;
  }
}

export function saveInternalDirectory(accounts: InternalDirectoryAccount[]) {
  localStorage.setItem(ACCOUNTS_STORAGE_KEY, JSON.stringify(accounts));
}
