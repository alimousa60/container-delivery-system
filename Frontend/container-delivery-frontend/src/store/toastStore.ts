import { create } from 'zustand';

interface Toast {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  message: string;
  duration?: number;
}

interface ToastState {
  toasts: Toast[];
  addToast: (toast: Omit<Toast, 'id'>) => void;
  removeToast: (id: string) => void;
  clearToasts: () => void;
}

export const useToastStore = create<ToastState>((set) => ({
  toasts: [],
  addToast: (toast) => set((state) => ({
    toasts: [...state.toasts, { ...toast, id: Math.random().toString(36).substr(2, 9) }],
  })),
  removeToast: (id) => set((state) => ({
    toasts: state.toasts.filter((t) => t.id !== id),
  })),
  clearToasts: () => set({ toasts: [] }),
}));

export const useToast = () => {
  const { addToast } = useToastStore();
  
  return {
    success: (message: string, duration?: number) => addToast({ type: 'success', message, duration }),
    error: (message: string, duration?: number) => addToast({ type: 'error', message, duration }),
    warning: (message: string, duration?: number) => addToast({ type: 'warning', message, duration }),
    info: (message: string, duration?: number) => addToast({ type: 'info', message, duration }),
  };
};