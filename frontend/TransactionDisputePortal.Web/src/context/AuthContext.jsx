import { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react';

const AuthContext = createContext(null);

const TOKEN_KEY       = 'tdp_token';
const USER_KEY        = 'tdp_user';
const INACTIVITY_MS   = 5 * 60 * 1000; // 5 minutes
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll'];

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => sessionStorage.getItem(TOKEN_KEY));
  const [user,  setUser]  = useState(() => {
    try { return JSON.parse(sessionStorage.getItem(USER_KEY) || 'null'); }
    catch { return null; }
  });
  const inactivityTimer = useRef(null);

  // Clear state if token is expired
  useEffect(() => {
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        if (Date.now() >= payload.exp * 1000) {
          clearAuth();
        }
      } catch {
        clearAuth();
      }
    }
  }, [token]);

  const clearAuth = useCallback(() => {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    setToken(null);
    setUser(null);
  }, []);

  // Inactivity logout — only active while a user is logged in
  useEffect(() => {
    if (!token) return;

    const resetTimer = () => {
      clearTimeout(inactivityTimer.current);
      inactivityTimer.current = setTimeout(clearAuth, INACTIVITY_MS);
    };

    ACTIVITY_EVENTS.forEach(e => window.addEventListener(e, resetTimer, { passive: true }));
    resetTimer(); // start the initial countdown

    return () => {
      clearTimeout(inactivityTimer.current);
      ACTIVITY_EVENTS.forEach(e => window.removeEventListener(e, resetTimer));
    };
  }, [token, clearAuth]);

  const login = useCallback(async (username, password) => {
    const res = await fetch('http://localhost:5115/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Invalid credentials');
    }

    const data = await res.json();
    const userData = {
      id:       data.userId,
      username: username.toLowerCase(),
      fullName: data.fullName,
      role:     data.role,
    };

    sessionStorage.setItem(TOKEN_KEY, data.accessToken);
    sessionStorage.setItem(USER_KEY,  JSON.stringify(userData));
    setToken(data.accessToken);
    setUser(userData);

    return userData;
  }, []);

  const logout = useCallback(() => {
    clearAuth();
  }, [clearAuth]);

  const value = {
    token,
    user,
    isAuthenticated: !!token && !!user,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
