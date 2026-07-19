import canonicalPersonnel from '../../../../fixtures/personnel.synthetic.json';

export type PersonnelStatus = 'Active' | 'Inactive';

export interface PersonnelRecord {
  empId: string;
  name: string;
  department: string;
  organization: string;
  clName: string;
  email: string;
  position: string;
  scope: string;
  status: PersonnelStatus;
  preferredName?: string;
}

type CanonicalAccountStatus = 'Active' | 'Inactive' | 'Pending' | 'Locked';

interface CanonicalPersonnelRecord {
  employeeId: string;
  fullName: string;
  preferredName: string | null;
  department: string;
  organization: string;
  clName: string;
  knoxId: string | null;
  email: string | null;
  position: string;
  scope: string;
  systemRole: 'Administrator' | 'User';
  dashboardProfile: string;
  accountStatus: CanonicalAccountStatus;
  isActive: boolean;
  roleProfile: string | null;
  part: string | null;
  avatar: string | null;
  notes: string | null;
}

const SYNTHETIC_ID_PATTERN = /^SYN-[A-Z0-9-]+$/;
const ACCOUNT_STATUSES = new Set<CanonicalAccountStatus>([
  'Active',
  'Inactive',
  'Pending',
  'Locked',
]);

function requireTrimmedString(
  record: Record<string, unknown>,
  field: keyof CanonicalPersonnelRecord,
  index: number,
): string {
  const value = record[field];
  if (
    typeof value !== 'string' ||
    value.length === 0 ||
    value.trim() !== value ||
    value.normalize('NFC') !== value ||
    /[\u0000-\u001f\u007f]/u.test(value)
  ) {
    throw new Error(`Invalid synthetic personnel fixture record ${index + 1}: ${field}`);
  }
  return value;
}

function optionalString(
  record: Record<string, unknown>,
  field: keyof CanonicalPersonnelRecord,
  index: number,
): string | null {
  const value = record[field];
  if (value === null) return null;
  return requireTrimmedString(record, field, index);
}

function validateCanonicalPersonnel(
  fixture: unknown,
): ReadonlyArray<CanonicalPersonnelRecord> {
  if (!Array.isArray(fixture) || fixture.length === 0) {
    throw new Error('Synthetic personnel fixture must be a non-empty array');
  }

  const employeeIds = new Set<string>();
  const knoxIds = new Set<string>();

  return fixture.map((value, index) => {
    if (!value || typeof value !== 'object' || Array.isArray(value)) {
      throw new Error(`Invalid synthetic personnel fixture record ${index + 1}`);
    }
    const record = value as Record<string, unknown>;
    const employeeId = requireTrimmedString(record, 'employeeId', index);
    const fullName = requireTrimmedString(record, 'fullName', index);
    const department = requireTrimmedString(record, 'department', index);
    const organization = requireTrimmedString(record, 'organization', index);
    const clName = requireTrimmedString(record, 'clName', index);
    const position = requireTrimmedString(record, 'position', index);
    const scope = requireTrimmedString(record, 'scope', index);
    const systemRole = requireTrimmedString(record, 'systemRole', index);
    const dashboardProfile = requireTrimmedString(record, 'dashboardProfile', index);
    const accountStatus = requireTrimmedString(record, 'accountStatus', index);
    const preferredName = optionalString(record, 'preferredName', index);
    const knoxId = optionalString(record, 'knoxId', index);
    const email = optionalString(record, 'email', index);
    const roleProfile = optionalString(record, 'roleProfile', index);
    const part = optionalString(record, 'part', index);
    const avatar = optionalString(record, 'avatar', index);
    const notes = optionalString(record, 'notes', index);

    if (!SYNTHETIC_ID_PATTERN.test(employeeId) || employeeIds.has(employeeId)) {
      throw new Error(`Invalid or duplicate synthetic employeeId: ${employeeId}`);
    }
    employeeIds.add(employeeId);

    if (knoxId && knoxIds.has(knoxId)) {
      throw new Error(`Duplicate synthetic knoxId at record ${index + 1}`);
    }
    if (knoxId) knoxIds.add(knoxId);

    if (email && !/^[^\s@]+@example\.invalid$/u.test(email)) {
      throw new Error(`Invalid synthetic email at record ${index + 1}`);
    }
    if (systemRole !== 'Administrator' && systemRole !== 'User') {
      throw new Error(`Invalid synthetic systemRole at record ${index + 1}`);
    }
    if (!ACCOUNT_STATUSES.has(accountStatus as CanonicalAccountStatus)) {
      throw new Error(`Invalid synthetic accountStatus at record ${index + 1}`);
    }
    if (typeof record.isActive !== 'boolean' || record.isActive !== (accountStatus === 'Active')) {
      throw new Error(`Inconsistent synthetic account status at record ${index + 1}`);
    }

    return {
      employeeId,
      fullName,
      preferredName,
      department,
      organization,
      clName,
      knoxId,
      email,
      position,
      scope,
      systemRole,
      dashboardProfile,
      accountStatus: accountStatus as CanonicalAccountStatus,
      isActive: record.isActive,
      roleProfile,
      part,
      avatar,
      notes,
    } as CanonicalPersonnelRecord;
  });
}

const canonicalRecords = validateCanonicalPersonnel(canonicalPersonnel);

export const mockPersonnel: ReadonlyArray<PersonnelRecord> = canonicalRecords.map(
  (record) => ({
    empId: record.employeeId,
    name: record.fullName,
    department: record.department,
    organization: record.organization,
    clName: record.clName,
    email: record.email ?? '',
    position: record.position,
    scope: record.scope,
    status: record.isActive ? 'Active' : 'Inactive',
    ...(record.preferredName ? { preferredName: record.preferredName } : {}),
  }),
);
