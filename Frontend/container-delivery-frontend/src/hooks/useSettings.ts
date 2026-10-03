import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { settingsService } from '@/services/api';
import type { CompanySettingsDto, UpdateSettingsRequest } from '@/types';

export const useSettings = () => {
  const queryClient = useQueryClient();

  const { data: settings, isLoading, error, refetch } = useQuery({
    queryKey: ['settings'],
    queryFn: () => settingsService.getAll(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  });

  const updateSettings = useMutation({
    mutationFn: (data: UpdateSettingsRequest) => settingsService.update(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] });
    },
  });

  const uploadLogo = useMutation({
    mutationFn: (file: File) => settingsService.uploadLogo(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] });
    },
  });

  return {
    settings,
    isLoading,
    error,
    refetch,
    updateSettings: updateSettings.mutateAsync,
    uploadLogo: uploadLogo.mutateAsync,
  };
};

export const useCompanyName = () => {
  const queryClient = useQueryClient();

  return useQuery({
    queryKey: ['settings', 'companyName'],
    queryFn: () => settingsService.getCompanyName(),
    staleTime: 1000 * 60 * 10, // 10 minutes
  });
};

export const useLogoUrl = () => {
  const queryClient = useQueryClient();

  return useQuery({
    queryKey: ['settings', 'logoUrl'],
    queryFn: () => settingsService.getLogoUrl(),
    staleTime: 1000 * 60 * 10, // 10 minutes
  });
};

export const useReportFooter = () => {
  const queryClient = useQueryClient();

  return useQuery({
    queryKey: ['settings', 'reportFooter'],
    queryFn: () => settingsService.getReportFooter(),
    staleTime: 1000 * 60 * 10, // 10 minutes
  });
};