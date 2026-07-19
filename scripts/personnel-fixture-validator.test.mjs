import assert from 'node:assert/strict';
import test from 'node:test';

import { validatePersonnelFixture } from './personnel-fixture-validator.mjs';

const validRecord = Object.freeze({
  employeeId: 'SYN-TEST-001',
  fullName: 'Nguyễn 하늘',
  preferredName: null,
  department: 'Quality',
  organization: 'IQC',
  clName: 'Synthetic Team',
  knoxId: 'syn.test.001',
  email: 'test.001@example.invalid',
  position: 'Quality Engineer',
  scope: 'Testing',
  systemRole: 'User',
  dashboardProfile: 'Auto',
  accountStatus: 'Active',
  isActive: true,
  roleProfile: null,
  part: null,
  avatar: null,
  notes: null,
});

test('accepts a valid Unicode synthetic record', () => {
  assert.equal(validatePersonnelFixture([{ ...validRecord }]).length, 1);
});

test('rejects duplicate identifiers before use', () => {
  assert.throws(
    () => validatePersonnelFixture([{ ...validRecord }, { ...validRecord }]),
    /employeeId: must be unique/,
  );
});

test('rejects non-canonical identifier case', () => {
  assert.throws(
    () => validatePersonnelFixture([{ ...validRecord, employeeId: 'syn-test-001' }]),
    /must match/,
  );
});

test('rejects status and active-flag contradictions', () => {
  assert.throws(
    () => validatePersonnelFixture([{ ...validRecord, accountStatus: 'Locked' }]),
    /must be true only when accountStatus is Active/,
  );
});

test('rejects deliverable email domains', () => {
  assert.throws(
    () => validatePersonnelFixture([{ ...validRecord, email: 'test@example.com' }]),
    /example.invalid/,
  );
});
