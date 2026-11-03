import { createBrowserRouter, Link } from 'react-router-dom';
import { AdminLayout } from '../layouts/AdminLayout';
import { Login } from '../pages/Login';
import { Dashboard } from '../pages/Dashboard';
import { MapPage } from '../pages/MapPage';
import { Occurrences } from '../pages/Occurrences';
import { OccurrenceDetail } from '../pages/OccurrenceDetail';
import { OccurrenceForm } from '../pages/OccurrenceForm';
import { CommunicationPage } from '../pages/CommunicationPage';
import { Employees } from '../pages/Employees';
import { Settings } from '../pages/Settings';
import { TrafficPage } from '../pages/TrafficPage';
import { AttendanceQueue } from '../pages/AttendanceQueue';
import { CitizenPortal } from '../pages/CitizenPortal';
export const router = createBrowserRouter([
  { path: '/login', element: <Login /> },
  { path: '/cidadao', element: <CitizenPortal /> },
  {
    element: <AdminLayout />,
    errorElement: (
      <div className="empty">
        <h1>Não foi possível carregar esta tela</h1>
        <p>Recarregue a página para tentar novamente.</p>
        <a className="button" href="/">
          Voltar ao início
        </a>
      </div>
    ),
    children: [
      { path: '/', element: <Dashboard /> },
      { path: '/transito', element: <TrafficPage /> },
      { path: '/fila', element: <AttendanceQueue /> },
      { path: '/mapa', element: <MapPage /> },
      { path: '/ocorrencias', element: <Occurrences /> },
      { path: '/ocorrencias/nova', element: <OccurrenceForm /> },
      { path: '/ocorrencias/:id/editar', element: <OccurrenceForm /> },
      { path: '/ocorrencias/:id', element: <OccurrenceDetail /> },
      { path: '/comunicacao', element: <CommunicationPage /> },
      { path: '/assistente', element: <CommunicationPage assistant /> },
      { path: '/funcionarios', element: <Employees /> },
      { path: '/configuracoes', element: <Settings /> },
      {
        path: '*',
        element: (
          <div className="empty">
            <h1>Página não encontrada</h1>
            <Link to="/">Voltar ao painel</Link>
          </div>
        ),
      },
    ],
  },
]);
