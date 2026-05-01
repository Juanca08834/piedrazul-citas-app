import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <section className="section-card narrow-center">
      <h1>Página no encontrada</h1>
      <p className="muted-text">La ruta que intentaste abrir no existe o fue movida.</p>
      <Link className="button" to="/">Volver al inicio</Link>
    </section>
  );
}
