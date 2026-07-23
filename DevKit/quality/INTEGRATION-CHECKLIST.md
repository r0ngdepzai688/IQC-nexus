# Integration Checklist

## Contracts

- [ ] Backend provider/lifecycle DTOs and frontend clients agree on names,
  enums, error codes, and nullability.
- [ ] Protocol version is explicit and validated.
- [ ] Permission names are shared constants where practical.
- [ ] Cancellation reaches stream parsing and database operations.

## Vertical slice

- [ ] Authenticate with synthetic development/test identity.
- [ ] Create an owner-bound import job.
- [ ] Normalize safe synthetic CSV and XLSX.
- [ ] Confirm mapping.
- [ ] Validate and review preview.
- [ ] Commit transactionally.
- [ ] Replay commit without duplication.
- [ ] Observe sanitized audit transition.
- [ ] Reject access from another authenticated user.

## Regression

- [ ] Existing Master Plan business-key and rollback tests remain green.
- [ ] Existing Data Hub behavior is adapted, not replaced by a competing stack.
- [ ] No production migration is created before entity/index review.
- [ ] CI commands match repository documentation.

