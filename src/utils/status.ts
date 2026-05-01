import type { AppointmentStatusValue } from '../types';

export function translateStatusLabel(value: string) {
  const normalized = value.trim().toLowerCase();
  switch (normalized) {
    case 'scheduled':
    case 'programada':
      return 'Programada';
    case 'cancelled':
    case 'cancelada':
      return 'Cancelada';
    case 'completed':
    case 'completada':
      return 'Completada';
    case 'no-show':
    case 'no show':
    case 'no asistio':
    case 'no asistió':
      return 'No asistió';
    default:
      return value;
  }
}

export function isTerminalStatus(value: string) {
  const translated = translateStatusLabel(value);
  return translated === 'Cancelada' || translated === 'Completada' || translated === 'No asistió';
}

export function hasAppointmentStarted(appointmentDate: string, startTime: string) {
  const start = new Date(`${appointmentDate}T${startTime}:00`);
  return Date.now() >= start.getTime();
}

export function canTransitionStatus(currentStatus: string, nextStatus: AppointmentStatusValue, appointmentDate: string, startTime: string) {
  const current = translateStatusLabel(currentStatus);
  if (current === nextStatus) return true;
  if (current === 'Cancelada' || current === 'Completada' || current === 'No asistió') return false;
  if (nextStatus === 'Cancelada') return true;
  if (nextStatus === 'Programada') return false;
  return hasAppointmentStarted(appointmentDate, startTime);
}
