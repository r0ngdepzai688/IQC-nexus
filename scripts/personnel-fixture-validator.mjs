import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ID_PATTERN = /^SYN-[A-Z0-9-]+$/;
const ACCOUNT_STATUSES = new Set(['Active', 'Inactive', 'Pending', 'Locked']);
const SYSTEM_ROLES = new Set(['Administrator', 'User']);
const REQUIRED_STRINGS = [
  'employeeId',
  'fullName',
  'department',
  'organization',
  'clName',
  'position',
  'scope',
  'systemRole',
  'dashboardProfile',
  'accountStatus',
];
const OPTIONAL_STRINGS = [
  'preferredName',
  'knoxId',
  'email',
  'roleProfile',
  'part',
  'avatar',
  'notes',
];

function fail(index, field, message) {
  throw new Error(`record ${index + 1}, field ${field}: ${message}`);
}

export function validatePersonnelFixture(value) {
  if (!Array.isArray(value) || value.length === 0) {
    throw new Error('fixture must be a non-empty array');
  }

  const employeeIds = new Set();
  const knoxIds = new Set();

  value.forEach((record, index) => {
    if (!record || typeof record !== 'object' || Array.isArray(record)) {
      fail(index, '$record', 'must be an object');
    }

    for (const field of REQUIRED_STRINGS) {
      const fieldValue = record[field];
      if (typeof fieldValue !== 'string' || fieldValue.trim() !== fieldValue || fieldValue.length === 0) {
        fail(index, field, 'must be a non-empty trimmed string');
      }
      if (fieldValue !== fieldValue.normalize('NFC') || /[\u0000-\u001f\u007f]/u.test(fieldValue)) {
        fail(index, field, 'must be NFC Unicode without control characters');
      }
    }

    for (const field of OPTIONAL_STRINGS) {
      const fieldValue = record[field];
      if (fieldValue !== null && (typeof fieldValue !== 'string' || fieldValue.trim() !== fieldValue || fieldValue.length === 0)) {
        fail(index, field, 'must be null or a non-empty trimmed string');
      }
      if (typeof fieldValue === 'string' && (fieldValue !== fieldValue.normalize('NFC') || /[\u0000-\u001f\u007f]/u.test(fieldValue))) {
        fail(index, field, 'must be NFC Unicode without control characters');
      }
    }

    if (!ID_PATTERN.test(record.employeeId)) {
      fail(index, 'employeeId', 'must match ^SYN-[A-Z0-9-]+$');
    }
    if (employeeIds.has(record.employeeId)) {
      fail(index, 'employeeId', 'must be unique (ordinal, case-sensitive)');
    }
    employeeIds.add(record.employeeId);

    if (record.knoxId !== null) {
      if (knoxIds.has(record.knoxId)) {
        fail(index, 'knoxId', 'must be unique when present');
      }
      knoxIds.add(record.knoxId);
    }

    if (record.email !== null && !/^[^\s@]+@example\.invalid$/u.test(record.email)) {
      fail(index, 'email', 'must use the example.invalid domain');
    }
    if (!SYSTEM_ROLES.has(record.systemRole)) {
      fail(index, 'systemRole', 'must be Administrator or User');
    }
    if (!ACCOUNT_STATUSES.has(record.accountStatus)) {
      fail(index, 'accountStatus', 'has an unsupported value');
    }
    if (typeof record.isActive !== 'boolean') {
      fail(index, 'isActive', 'must be boolean');
    }
    if (record.isActive !== (record.accountStatus === 'Active')) {
      fail(index, 'isActive', 'must be true only when accountStatus is Active');
    }
  });

  return value;
}

export function loadAndValidatePersonnelFixture(fixturePath) {
  return validatePersonnelFixture(JSON.parse(fs.readFileSync(fixturePath, 'utf8')));
}

const modulePath = fileURLToPath(import.meta.url);
const invokedPath = process.argv[1] ? path.resolve(process.argv[1]) : '';
if (modulePath === invokedPath) {
  const fixturePath = process.argv[2]
    ? path.resolve(process.argv[2])
    : path.resolve(path.dirname(modulePath), '..', 'fixtures', 'personnel.synthetic.json');
  const records = loadAndValidatePersonnelFixture(fixturePath);
  console.log(`Validated ${records.length} synthetic personnel records: ${fixturePath}`);
}
