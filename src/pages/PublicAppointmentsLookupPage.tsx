import { Link } from 'react-router-dom';

export function PublicAppointmentsLookupPage() {
  return (
    <div className="stack-lg">
      <section className="section-card stack-md">
        <span className="eyebrow">Acceso restringido</span>
        <h1>Consulta de citas</h1>
        <p className="muted-text">
          Para consultar tus citas debes iniciar sesión con tu cuenta. Si aún no tienes una, puedes crearla de forma gratuita.
        </p>
        <div className="inline-actions wrap">
          <Link className="button" to="/iniciar-sesion">Iniciar sesión</Link>
          <Link className="button button-secondary" to="/crear-cuenta">Crear cuenta</Link>
          <Link className="button button-secondary" to="/reservar">Reservar cita</Link>
        </div>
      </section>
    </div>
  );
}
