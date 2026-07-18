import { AuthState } from '../contexts/AuthContext';

export const hasAccessToProject = (user: AuthState, partId: string): boolean => {
  if (user.systemRole === 'Administrator') return true;
  if (user.position === 'Group Leader' && user.organization === 'IQC Group') return true;
  if (user.position === 'Part Leader') return user.part === partId || user.part === 'All';
  if (user.position === 'Cell Leader') return user.part === partId || user.part === 'All';
  return false; // Staff logic
};

export const hasAccessToScope = (user: AuthState, partId: string, scopeId: string): boolean => {
  if (user.systemRole === 'Administrator') return true;
  if (user.position === 'Group Leader') return true;
  if (user.position === 'Part Leader') return user.part === partId || user.part === 'All';
  if (user.position === 'Cell Leader') return (user.part === partId || user.part === 'All') && (user.scope === scopeId || user.scope === 'All');
  return false;
};

export const canApproveTask = (user: AuthState): boolean => {
  if (user.systemRole === 'Administrator') return true;
  return user.position === 'Group Leader' || user.position === 'Part Leader' || user.position === 'Cell Leader';
};
