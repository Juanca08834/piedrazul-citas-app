const DOCTOR_LINKS_STORAGE_KEY = 'piedrazul-doctor-links';
const REGISTER_DRAFT_STORAGE_KEY = 'piedrazul-register-draft';

export interface DoctorLinkMap {
  [email: string]: string;
}

export interface RegisterDraft {
  documentNumber?: string;
  firstName: string;
  lastName: string;
  email?: string;
}

export function readDoctorLinks(): DoctorLinkMap {
  try {
    const raw = localStorage.getItem(DOCTOR_LINKS_STORAGE_KEY);
    return raw ? JSON.parse(raw) as DoctorLinkMap : {};
  } catch {
    return {};
  }
}

export function linkDoctorToProvider(email: string, providerId: string) {
  const links = readDoctorLinks();
  links[email.trim().toLowerCase()] = providerId;
  localStorage.setItem(DOCTOR_LINKS_STORAGE_KEY, JSON.stringify(links));
}

export function getLinkedProviderId(email?: string) {
  if (!email) return null;
  const links = readDoctorLinks();
  return links[email.trim().toLowerCase()] ?? null;
}

export function saveRegisterDraft(draft: RegisterDraft) {
  localStorage.setItem(REGISTER_DRAFT_STORAGE_KEY, JSON.stringify(draft));
}

export function readRegisterDraft(): RegisterDraft | null {
  try {
    const raw = localStorage.getItem(REGISTER_DRAFT_STORAGE_KEY);
    return raw ? JSON.parse(raw) as RegisterDraft : null;
  } catch {
    return null;
  }
}

export function clearRegisterDraft() {
  localStorage.removeItem(REGISTER_DRAFT_STORAGE_KEY);
}
