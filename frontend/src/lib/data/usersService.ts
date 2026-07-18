import {
  mockPersonnel,
  type PersonnelRecord,
} from '@/lib/mock-data/personnelMock';

export type UserData = PersonnelRecord;

export const getAllUsers = (): UserData[] => mockPersonnel;

// Create a fast lookup map at module scope.
const userMap = new Map<string, UserData>(
  mockPersonnel.map((user) => [user.empId, user]),
);

export const getUserById = (empId: string): UserData | null => {
  const cleanId = empId.startsWith('@') ? empId.substring(1) : empId;

  return userMap.get(cleanId) ?? null;
};
