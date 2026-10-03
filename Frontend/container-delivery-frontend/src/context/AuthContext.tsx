import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { authService } from '@/services/api';
import type { User, AuthTokens, LoginRequest, MfaVerifyRequest } from '@/types/auth';

interface AuthContextType {
  user: User | null;
  tokens: AuthTokens | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  verifyMfa: (request: MfaVerifyRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
  updateUser: (user: Partial<User>) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { t } = useTranslation();
  const [user, setUser] = useState<User | null>(null);
  const [tokens, setTokens] = useState<AuthTokens | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    initializeAuth();
  }, []);

  const initializeAuth = async () => {
    try {
      const storedTokens = localStorage.getItem('auth_tokens');
      const storedUser = localStorage.getItem('auth_user');

      if (storedTokens && storedUser) {
        const parsedTokens = JSON.parse(storedTokens);
        const parsedUser = JSON.parse(storedUser);

        // Check if access token is expired
        if (parsedTokens.expiresAt > Date.now()) {
          setTokens(parsedTokens);
          setUser(parsedUser);
        } else {
          // Try to refresh token
          await refreshToken();
        }
      }
    } catch (error) {
      console.error('Auth initialization failed:', error);
      clearAuth();
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (credentials: LoginRequest) => {
    const response = await authService.login(credentials);
    
    if (response.requiresMfa) {
      // Store partial auth for MFA verification
      localStorage.setItem('pending_mfa_user', JSON.stringify(response.user));
      throw new Error('MFA_REQUIRED');
    }

    if (!response.success || !response.accessToken || !response.refreshToken || !response.user) {
      throw new Error(response.error || t('auth.loginFailed'));
    }

    const newTokens: AuthTokens = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: Date.now() + (response.expiresIn || 900) * 1000,
    };

    setTokens(newTokens);
    setUser(response.user);
    persistAuth(newTokens, response.user);
  };

  const verifyMfa = async (request: MfaVerifyRequest) => {
    const pendingUser = localStorage.getItem('pending_mfa_user');
    if (!pendingUser) {
      throw new Error('No pending MFA verification');
    }

    const { userId } = JSON.parse(pendingUser);
    const response = await authService.verifyMfa({ ...request, userId });

    if (!response.success || !response.accessToken || !response.refreshToken || !response.user) {
      throw new Error(response.error || t('auth.loginFailed'));
    }

    const newTokens: AuthTokens = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: Date.now() + (response.expiresIn || 900) * 1000,
    };

    setTokens(newTokens);
    setUser(response.user);
    persistAuth(newTokens, response.user);
    localStorage.removeItem('pending_mfa_user');
  };

  const logout = async () => {
    if (tokens?.refreshToken) {
      try {
        await authService.logout(tokens.refreshToken);
      } catch (error) {
        console.error('Logout error:', error);
      }
    }
    clearAuth();
  };

  const refreshToken = async () => {
    const storedTokens = localStorage.getItem('auth_tokens');
    if (!storedTokens) return;

    try {
      const { refreshToken } = JSON.parse(storedTokens);
      const response = await authService.refreshToken(refreshToken);

      if (response.success && response.accessToken && response.refreshToken && response.user) {
        const newTokens: AuthTokens = {
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
          expiresAt: Date.now() + (response.expiresIn || 900) * 1000,
        };

        setTokens(newTokens);
        setUser(response.user);
        persistAuth(newTokens, response.user);
      } else {
        clearAuth();
      }
    } catch (error) {
      clearAuth();
      throw error;
    }
  };

  const updateUser = (updates: Partial<User>) => {
    if (user) {
      const updatedUser = { ...user, ...updates };
      setUser(updatedUser);
      localStorage.setItem('auth_user', JSON.stringify(updatedUser));
    }
  };

  const persistAuth = (newTokens: AuthTokens, newUser: User) => {
    localStorage.setItem('auth_tokens', JSON.stringify(newTokens));
    localStorage.setItem('auth_user', JSON.stringify(newUser));
  };

  const clearAuth = () => {
    setTokens(null);
    setUser(null);
    localStorage.removeItem('auth_tokens');
    localStorage.removeItem('auth_user');
    localStorage.removeItem('pending_mfa_user');
  };

  return (
    <AuthContext.Provider value={{
      user,
      tokens,
      isAuthenticated: !!user && !!tokens,
      isLoading,
      login,
      verifyMfa,
      logout,
      refreshToken,
      updateUser,
    }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};