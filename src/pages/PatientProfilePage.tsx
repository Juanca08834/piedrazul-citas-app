import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiRequest } from '../api/http';
import { useAuth } from '../auth/AuthContext';
import { PortalTabs } from '../components/PortalTabs';
import type { Gender, GenderOption, PatientProfile } from '../types';
import { clearRegisterDraft, readRegisterDraft } from '../utils/sessionStorage';
import { sanitizeNameInput, validatePatientForm } from '../utils/validators';

const tabs = [
  { to: '/portal/paciente', label: 'Mis citas' },
  { to: '/portal/paciente/perfil', label: 'Mi perfil' },
];

const initialProfile = {
  documentNumber: '',
  firstName: '',
  lastName: '',
  phone: '',
  gender: '' as GenderOption,
  birthDate: '',
  email: '',
};

export function PatientProfilePage() {
  const navigate = useNavigate();
  const { session } = useAuth();
  const [form, setForm] = useState(initialProfile);
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [showSuccessModal, setShowSuccessModal] = useState(false);

  useEffect(() => {
    const draft = readRegisterDraft();
    if (draft) {
      setForm((current) => ({
        ...current,
        documentNumber: current.documentNumber || draft.documentNumber || '',
        firstName: current.firstName || draft.firstName,
        lastName: current.lastName || draft.lastName,
        email: current.email || draft.email || '',
      }));
    }
  }, []);

  useEffect(() => {
    if (!session) return;

    apiRequest<PatientProfile>('/api/patient/profile', session)
      .then((profile) => {
        setForm((current) => ({
          ...current,
          documentNumber: profile.documentNumber,
          firstName: profile.firstName || current.firstName,
          lastName: profile.lastName || current.lastName,
          phone: profile.phone,
          gender: profile.gender,
          birthDate: profile.birthDate ?? '',
          email: profile.email ?? current.email,
        }));
      })
      .catch(() => undefined);
  }, [session]);

  useEffect(() => {
    if (!showSuccessModal) return undefined;

    const timer = window.setTimeout(() => {
      setShowSuccessModal(false);
      navigate('/portal/paciente', { replace: true });
    }, 3000);

    return () => window.clearTimeout(timer);
  }, [navigate, showSuccessModal]);

  const handleChange = (field: keyof typeof form, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setMessage(null);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.gender) {
      setMessage('Selecciona tu género para continuar.');
      return;
    }
    const errors = validatePatientForm(form);
    if (errors.length > 0) {
      setMessage(errors[0]);
      return;
    }

    try {
      setSubmitting(true);
      await apiRequest<PatientProfile>('/api/patient/profile', session, {
        method: 'PUT',
        body: {
          documentNumber: form.documentNumber,
          firstName: form.firstName,
          lastName: form.lastName,
          phone: form.phone,
          gender: form.gender as Gender,
          birthDate: form.birthDate || null,
          email: form.email || null,
        },
      });
      clearRegisterDraft();
      setMessage(null);
      setShowSuccessModal(true);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'No fue posible guardar el perfil.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="stack-lg">
      <section className="section-card">
        <h1>Mi perfil</h1>
        <p className="muted-text">Completa o actualiza tus datos para agilizar futuras reservas.</p>
      </section>

      <PortalTabs items={tabs} />

      <form className="section-card stack-md" onSubmit={handleSubmit}>
        <div className="form-grid">
          <label>
            Documento
            <input inputMode="numeric" maxLength={20} value={form.documentNumber} onChange={(event) => handleChange('documentNumber', event.target.value.replace(/\D/g, ''))} />
          </label>
          <label>
            Nombres
            <input maxLength={80} value={form.firstName} onChange={(event) => handleChange('firstName', sanitizeNameInput(event.target.value))} />
          </label>
          <label>
            Apellidos
            <input maxLength={80} value={form.lastName} onChange={(event) => handleChange('lastName', sanitizeNameInput(event.target.value))} />
          </label>
          <label>
            Celular
            <input inputMode="numeric" maxLength={15} value={form.phone} onChange={(event) => handleChange('phone', event.target.value.replace(/\D/g, ''))} />
          </label>
          <label>
            Género
            <select value={form.gender} onChange={(event) => handleChange('gender', event.target.value)}>
              <option value="">Seleccionar género</option>
              <option value="Female">Mujer</option>
              <option value="Male">Hombre</option>
              <option value="Other">Otro</option>
            </select>
          </label>
          <label>
            Fecha de nacimiento
            <input type="date" value={form.birthDate} onChange={(event) => handleChange('birthDate', event.target.value)} />
          </label>
          <label className="span-two">
            Correo electrónico
            <input maxLength={150} value={form.email} onChange={(event) => handleChange('email', event.target.value)} />
          </label>
        </div>

        {message && <div className="feedback-card error">{message}</div>}

        <div className="inline-actions end">
          <button type="submit" className="button" disabled={submitting}>
            {submitting ? 'Guardando...' : 'Guardar perfil'}
          </button>
        </div>
      </form>

      {showSuccessModal && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <section className="modal-card stack-md">
            <span className="eyebrow">Perfil actualizado</span>
            <h2>Perfil correctamente creado</h2>
            <p className="muted-text">Tus datos fueron guardados. En unos segundos te llevaremos a la sección principal del paciente.</p>
          </section>
        </div>
      )}
    </div>
  );
}
