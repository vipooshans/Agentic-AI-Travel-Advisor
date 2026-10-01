import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';

/** Default handlers for requests most pages make; tests add their own with server.use(). */
export const server = setupServer(
  http.get('*/api/settings/public', () =>
    HttpResponse.json({
      defaultCurrency: 'LKR',
      aiAssistantEnabled: true,
      maintenanceMessage: null,
      maxAdvanceBookingDays: 365,
      guestCancellationCutoffHours: 48,
    }),
  ),
);
