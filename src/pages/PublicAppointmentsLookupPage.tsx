import { useState } from 'react';
import { Link } from 'react-router-dom';
import { apiRequest } from '../api/http';
import type { AppointmentResponse } from '../types';
import { formatDateLabel } from '../utils/validators';

export function PublicAppointmentsLookupPage() {
  const [documentNumber, setDocumentNumber] = useState('');
  const [appointments, setAppointments] = useState<AppointmentResponse[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleLookup = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!/^\d{5,20}$/.test(documentNumber.trim())) {
      setMessage('Ingresa una cédula válida para consultar tus citas.');
      setAppointments([]);
      return;
    }

    try {
      setLoading(true);
      setMessage(null);
      const data = await apiRequest<AppointmentResponse[]>(`/api/public/appointments/by-document?document=${documentNumber.trim()}`, null);
      setAppointments(data);
      if (data.length === 0) {
        setMessage('No encontramos citas registradas para esa cédula.');
      }
    } catch (error) {
      setAppointments([]);
      setMessage(error instanceof Error ? error.message : 'No fue posible consultar las citas.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="stack-lg">
      <section className="section-card stack-md">
        <span className="eyebrow">Consulta pública</span>
        <h1>Consulta tus citas con número de cédula</h1>
        <p className="muted-text">Aquí puedes ver las reservas existentes asociadas a tu documento, incluso si aún no has creado usuario.</p>
        <form className="form-grid internal-filter-grid" onSubmit={handleLookup}>
          <label>
            Número de cédula
            <input inputMode="numeric" maxLength={20} value={documentNumber} onChange={(event) => setDocumentNumber(event.target.value.replace(/\D/g, ''))} />
          </label>
          <div className="inline-actions end" style={{ alignItems: 'end' }}>
            <button type="submit" className="button" disabled={loading}>{loading ? 'Consultando...' : 'Consultar citas'}</button>
          </div>
        </form>
        {message && <div className={`feedback-card ${appointments.length > 0 ? 'success' : 'error'}`}>{message}</div>}
      </section>

      <section className="section-card stack-md">
        <div className="between wrap">
          <h2>Resultados</h2>
          <div className="inline-actions wrap">
            <Link className="button button-secondary" to="/reservar">Reservar cita</Link>
            <Link className="button" to="/iniciar-sesion">Iniciar sesión</Link>
          </div>
        </div>

        {appointments.length === 0 && !loading && <div className="empty-state">No hay resultados para mostrar todavía.</div>}
        {appointments.length > 0 && (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Hora</th>
                  <th>Profesional</th>
                  <th>Especialidad</th>
                  <th>Estado</th>
                  <th>Canal</th>
                </tr>
              </thead>
              <tbody>
                {appointments.map((appointment) => (
                  <tr key={appointment.id}>
                    <td>{formatDateLabel(appointment.appointmentDate)}</td>
                    <td>{appointment.startTime} - {appointment.endTime}</td>
                    <td>{appointment.providerName}</td>
                    <td>{appointment.specialty}</td>
                    <td>{appointment.status}</td>
                    <td>{appointment.channel}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
