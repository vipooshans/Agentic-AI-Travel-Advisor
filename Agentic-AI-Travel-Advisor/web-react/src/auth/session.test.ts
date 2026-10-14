import { clearSession, getToken, loadSession, saveSession } from './session';
import { makeUser } from '../test/utils';

describe('session storage', () => {
  it('round-trips a valid session', () => {
    const user = makeUser('USER');
    saveSession({ token: 'abc', expiresAt: new Date(Date.now() + 60_000).toISOString(), user });
    expect(loadSession()?.user.email).toBe(user.email);
    expect(getToken()).toBe('abc');
  });

  it('drops an expired session instead of sending a stale token', () => {
    saveSession({ token: 'old', expiresAt: new Date(Date.now() - 1_000).toISOString(), user: makeUser('USER') });
    expect(loadSession()).toBeNull();
    expect(getToken()).toBeNull();
    expect(localStorage.getItem('ta.session')).toBeNull();
  });

  it('drops corrupt JSON', () => {
    localStorage.setItem('ta.session', '{not json');
    expect(loadSession()).toBeNull();
  });

  it('clearSession removes the token', () => {
    saveSession({ token: 'abc', expiresAt: new Date(Date.now() + 60_000).toISOString(), user: makeUser('ADMIN') });
    clearSession();
    expect(getToken()).toBeNull();
  });
});
