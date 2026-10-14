import { screen } from '@testing-library/react';
import { Route } from 'react-router-dom';
import { renderAt, signIn } from '../test/utils';
import { RequireRole } from './RequireRole';

const adminRoute = (
  <Route
    path="/admin"
    element={
      <RequireRole roles={['ADMIN']}>
        <h1>Admin area</h1>
      </RequireRole>
    }
  />
);

describe('RequireRole', () => {
  it('sends anonymous visitors to the login page', () => {
    renderAt('/admin', adminRoute);
    expect(screen.getByTestId('location')).toHaveTextContent('/login');
    expect(screen.queryByText('Admin area')).not.toBeInTheDocument();
  });

  it.each(['USER', 'HOTEL_OWNER', 'TRAVEL_AGENT'] as const)('blocks %s from admin pages', (role) => {
    signIn(role);
    renderAt('/admin', adminRoute);
    expect(screen.getByTestId('location')).toHaveTextContent('/forbidden');
    expect(screen.queryByText('Admin area')).not.toBeInTheDocument();
  });

  it('lets an admin in', () => {
    signIn('ADMIN');
    renderAt('/admin', adminRoute);
    expect(screen.getByRole('heading', { name: 'Admin area' })).toBeInTheDocument();
  });
});
