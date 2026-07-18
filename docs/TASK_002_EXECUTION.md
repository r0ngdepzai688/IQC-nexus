# Task 002 / E01-T02 Execution Slices

## 1. Kết luận

Task 002 giữ nguyên một Task ID trong roadmap:

- **Task:** Task 002 / E01-T02.
- **Tên:** Phân loại và loại dữ liệu/binary khỏi repository.
- **Epic/Feature:** E01 — Baseline, Repository và Delivery Safety / E01-F01 — Repository integrity.
- **Trạng thái:** chưa bắt đầu; tài liệu này chỉ refinement cách thực hiện.

Task được triển khai qua ba execution slice tuần tự. Slice là đơn vị kiểm soát thay đổi, không phải Task ID mới và không thay đổi `EXECUTION_ORDER.md`:

```text
002A Contain tracked root data
  → 002B Replace embedded user datasets with synthetic fixtures
  → 002C Remove runtime Excel/identity coupling
  → E01-T02 overall sign-off
```

Không slice nào được đánh dấu là hoàn thành toàn bộ E01-T02. Task 002 chỉ hoàn thành sau 002C và khi Definition of Done tổng hợp được Data Owner/Security ký duyệt.

## 2. Quy tắc chung cho cả ba slice

- Mỗi slice chỉ có một objective kỹ thuật.
- Mỗi slice tối đa 10 file, tính cả bốn tài liệu bắt buộc của Session Closing Protocol.
- Không rewrite Git history, xóa file local ignored, thay auth architecture hoặc bắt đầu Task 003.
- Không dùng dữ liệu thật làm fixture, log, snapshot hoặc bằng chứng kiểm thử.
- Dữ liệu gốc chỉ được di chuyển/xóa sau khi Data Owner xác nhận secure backup, nơi lưu và retention.
- Chỉ stage bằng allowlist tường minh; không dùng `git add .`, `git add -A` hoặc wildcard rộng.
- Sau mỗi slice: build/test, scan staged set, cập nhật bốn tài liệu quản trị, commit riêng nếu được phê duyệt, rồi dừng.
- Nếu file conditional làm số file vượt giới hạn, dừng và xin refinement; không tự mở rộng slice.

## 3. Slice 002A — Contain tracked root data

### Mục tiêu

Loại các artifact dữ liệu nhân sự thật ở repository root khỏi HEAD, ngăn chúng bị track lại và tạo inventory có owner/disposition; không thay runtime, frontend fixture hoặc backend seeding.

### Phạm vi

- Xác nhận secure backup ngoài Git trước khi bỏ bất kỳ artifact nào khỏi HEAD.
- Ghi inventory cho dữ liệu tracked và dữ liệu local ignored liên quan, nhưng không ghi giá trị PII.
- Bổ sung ignore rule theo đúng tên/path cho root JSON generated.
- Loại bốn root artifacts đã xác định khỏi Git tracking.
- Ghi quyết định hoặc trạng thái chờ quyết định cho Git history cleanup.
- Không chạm `extract_users.py`, startup, seeder hoặc embedded user datasets trong slice này.

### File dự kiến thay đổi

Implementation và evidence — 6 file:

1. `.gitignore` — thêm rule path-specific cho root generated user JSON; giữ nguyên rule DB/Excel/ZIP/runtime hiện tại.
2. `User_DB.xlsx` — loại khỏi HEAD sau secure-backup sign-off.
3. `users.json` — loại khỏi HEAD.
4. `user_preview.json` — loại khỏi HEAD.
5. `user_preview_utf8.json` — loại khỏi HEAD.
6. `docs/security/data-inventory.md` — tạo inventory, classification, owner, storage, retention, disposition và history-decision status.

Session Closing Protocol — 4 file:

7. `docs/PROJECT_STATUS.md`
8. `docs/CURRENT_SPRINT.md`
9. `docs/CHANGELOG_INTERNAL.md`
10. `docs/NEXT_SESSION.md`

**Tổng tối đa: 10 file.** Không được thêm file thứ 11 trong 002A.

### Definition of Done

- [ ] Data Owner xác nhận bốn root artifacts không phải bản duy nhất hoặc đã có secure backup với owner và nơi lưu rõ ràng.
- [ ] `git ls-files` không còn trả về bốn root artifacts.
- [ ] `git check-ignore -v` chứng minh bốn path sẽ tiếp tục bị ignore.
- [ ] Inventory ghi đủ: path, tracked/ignored, loại dữ liệu, sensitivity, owner, nơi lưu, retention, disposition, checksum metadata nếu được phép và approver/date.
- [ ] Hai ZIP, SQLite/WAL/SHM, `.env.local` và runtime Data Hub local vẫn ignored và không được stage/xóa.
- [ ] Không có source, runtime seeder hoặc embedded fixture bị thay đổi.
- [ ] Backend và frontend build không regression vì loại root artifacts.
- [ ] Git history có quyết định `rewrite`, `no rewrite` hoặc trạng thái blocked với approver/decision deadline; 002A không thực hiện rewrite.
- [ ] Staged set đúng 10 file hoặc ít hơn, không chứa binary/data ngoài bốn deletion đã phê duyệt.

### Build/Test

Từ repository root:

```powershell
git status --short
git ls-files -- User_DB.xlsx users.json user_preview.json user_preview_utf8.json
git check-ignore -v -- User_DB.xlsx users.json user_preview.json user_preview_utf8.json
dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
dotnet test backend/IqcQms.sln --no-build
```

Từ `frontend`:

```powershell
npx tsc --noEmit --incremental false
npm run lint
npm test --if-present
npm run build
```

Trước commit:

```powershell
git diff --cached --name-status
git diff --cached --check
git status --short
```

### Rollback

- Trước commit: unstage bằng đúng allowlist; khôi phục `.gitignore` và bốn file từ Git index/HEAD, không dùng reset diện rộng.
- Sau commit: tạo revert commit riêng khi được phê duyệt.
- Nếu phải rollback chức năng, không recommit PII. Dùng secure backup ngoài Git để vận hành tạm thời theo quyền Data Owner.
- Không xóa hoặc di chuyển ZIP/DB/.env local trong rollback.

### Điều kiện chuyển sang 002B

- 002A đạt toàn bộ DoD và có commit riêng được review.
- Working tree sạch.
- Secure backup, owner, storage và retention đã được ghi nhận.
- Data Owner/Security chấp thuận schema và persona tối thiểu cho synthetic fixtures.
- Không tự động bắt đầu 002B; cần yêu cầu rõ ràng của người dùng.

## 4. Slice 002B — Replace embedded datasets with synthetic fixtures

### Mục tiêu

Thay hai dataset người dùng thật đang nhúng trong frontend/backend bằng fixture synthetic có schema tương thích; không đổi runtime seeding, startup hoặc auth behavior.

### Phạm vi

- Tạo 4–6 persona synthetic, không phỏng theo người thật.
- Giữ nguyên schema hiện hành để tránh sửa consumer trong slice này.
- Bao phủ tối thiểu: administrator fixture, standard user, scoped user, inactive/denied user nếu schema hỗ trợ.
- Không đổi `usersService.ts`, component UI, `Program.cs`, `UserSeeder.cs` hoặc script extraction.
- Ghi inventory/version/checksum của fixture synthetic, không ghi credential hoạt động.

### File dự kiến thay đổi

Implementation và evidence — 3 file:

1. `frontend/src/data/users.json` — thay toàn bộ record thật bằng fixture synthetic giữ nguyên schema frontend.
2. `backend/src/IqcQms.Infrastructure/Data/Seeders/users_seed.json` — thay record thật bằng fixture synthetic giữ nguyên schema backend.
3. `docs/security/data-inventory.md` — cập nhật disposition, fixture owner, schema và scan evidence.

Session Closing Protocol — 4 file:

4. `docs/PROJECT_STATUS.md`
5. `docs/CURRENT_SPRINT.md`
6. `docs/CHANGELOG_INTERNAL.md`
7. `docs/NEXT_SESSION.md`

**Tổng tối đa: 7 file.** Nếu consumer cần đổi schema/code, dừng; không kéo consumer vào 002B.

### Definition of Done

- [ ] Hai JSON parse thành công và đúng schema hiện hành.
- [ ] Mọi record là synthetic; zero match theo danh sách/chỉ dấu PII do Security phê duyệt.
- [ ] Không dùng email domain, mã nhân viên, tên, tổ chức hoặc scope lấy từ dữ liệu thật.
- [ ] Fixture có 4–6 persona và bao phủ các lookup/state đã thống nhất.
- [ ] Frontend user lookup/badge/chat smoke hoạt động mà không sửa consumer.
- [ ] Backend fixture có schema hợp lệ, dù chưa được runtime consume trong slice này.
- [ ] Build artifacts và staged diff không chứa dữ liệu thật, secret hoặc binary.
- [ ] Không thay startup, seeder code, API, role/permission hay auth flow.

### Build/Test

Kiểm tra JSON và PII bằng scanner/policy đã được Security phê duyệt, sau đó:

```powershell
dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
dotnet test backend/IqcQms.sln --no-build
```

Từ `frontend`:

```powershell
npx tsc --noEmit --incremental false
npm run lint
npm test --if-present
npm run build
```

Kiểm tra bắt buộc bổ sung:

- Parse JSON và xác nhận record count trong khoảng 4–6.
- So sánh property names với schema trước thay đổi.
- Smoke `getAllUsers()` và `getUserById()` cho found/not-found.
- Scan staged blobs và build output cho PII/secret.
- `git diff --cached --name-status`, `git diff --cached --check`, `git status --short`.

### Rollback

- Không rollback bằng dữ liệu thật.
- Nếu fixture làm consumer lỗi, revert schema/content synthetic về phiên bản synthetic last-known-good hoặc sửa fixture trong cùng scope trước commit.
- Sau commit, revert commit chỉ khi kết quả không đưa PII trở lại; nếu parent commit chứa PII, dùng forward-fix synthetic thay vì revert mù.

### Điều kiện chuyển sang 002C

- 002B đạt DoD, commit riêng được review và working tree sạch.
- Frontend chạy với fixture synthetic.
- Backend synthetic JSON được Security/Data Owner chấp thuận làm nguồn DEV/TEST.
- Có quyết định runtime seeding: chỉ DEV/TEST, không production; credential/role không hard-code theo danh tính.
- Không tự động bắt đầu 002C; cần yêu cầu rõ ràng của người dùng.

## 5. Slice 002C — Remove runtime Excel and identity coupling

### Mục tiêu

Loại phụ thuộc runtime vào Excel/danh tính thật và chuyển startup seeding DEV/TEST sang fixture synthetic đã tạo ở 002B; không thay auth architecture hoặc permission catalogue.

### Phạm vi

- Bỏ logic dò `User_DB.xlsx` trong startup.
- Chuyển seeder sang đọc fixture synthetic đã có, chỉ trong environment được phê duyệt.
- Bỏ hard-code định danh administrator và default password dùng chung.
- Loại one-off extraction script có đường dẫn máy cá nhân hoặc thay bằng quyết định remove; không xây ETL tool mới.
- Cấu hình fixture thành resource/copy-to-output theo cơ chế project hiện có nếu cần.
- Không triển khai OIDC, secret rotation, role redesign hoặc database migration.

### File dự kiến thay đổi

Implementation và evidence — tối đa 5 file:

1. `backend/src/IqcQms.Api/Program.cs` — bỏ Excel path probing; gọi seeder synthetic theo environment guard.
2. `backend/src/IqcQms.Infrastructure/Data/Seeders/UserSeeder.cs` — đọc fixture synthetic; không gán quyền/password theo danh tính thật.
3. `extract_users.py` — xóa one-off script; không thay bằng pipeline mới.
4. `backend/src/IqcQms.Infrastructure/IqcQms.Infrastructure.csproj` — chỉ sửa nếu cần khai báo fixture resource/copy behavior.
5. `docs/security/data-inventory.md` — cập nhật runtime disposition, validation evidence và remaining risks.

Session Closing Protocol — 4 file:

6. `docs/PROJECT_STATUS.md`
7. `docs/CURRENT_SPRINT.md`
8. `docs/CHANGELOG_INTERNAL.md`
9. `docs/NEXT_SESSION.md`

**Tổng tối đa: 9 file.** File `.csproj` là slot conditional đã dự phòng; nếu không cần thì tổng là 8.

### Definition of Done

- [ ] Không còn reference runtime hoặc script tới `User_DB.xlsx` và đường dẫn máy cá nhân.
- [ ] `extract_users.py` không còn trong HEAD.
- [ ] Seeder chỉ chạy trong DEV/TEST hoặc environment allowlist được phê duyệt; production không tự seed synthetic users.
- [ ] Seeder đọc đúng fixture 002B bằng cơ chế tái lập từ clean build output.
- [ ] Không còn hard-code danh tính thật, administrator assignment theo employee ID hoặc default password dùng chung trong seeding path.
- [ ] Không tạo role/permission mới và không sửa auth API/contract.
- [ ] Startup với fixture hợp lệ thành công; fixture thiếu/invalid fail theo policy đã duyệt và không lộ path/PII.
- [ ] Backend build/test xanh; frontend regression suite vẫn xanh.
- [ ] Data Owner/Security ký inventory; nơi lưu/retention/history-cleanup decision đầy đủ.
- [ ] E01-T02 tổng hợp chỉ được đánh dấu Completed sau khi 002A, 002B và 002C đều đạt DoD.

### Build/Test

```powershell
git status --short
rg -n "User_DB\.xlsx|SyncUsersFromExcel|Welcome@123|extract_users|[A-Z]:\\" backend extract_users.py
dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
dotnet test backend/IqcQms.sln --no-build
```

Integration/smoke bắt buộc:

- Startup DEV/TEST với fixture synthetic hợp lệ.
- Startup production không tạo synthetic users.
- Fixture thiếu và JSON invalid có outcome/log an toàn theo policy.
- Seed chạy lại không tạo duplicate và không tự nâng quyền.
- Scan source, staged blobs và build output cho PII/secret/path máy cá nhân.

Frontend regression từ `frontend`:

```powershell
npx tsc --noEmit --incremental false
npm run lint
npm test --if-present
npm run build
```

Trước commit:

```powershell
git diff --cached --name-status
git diff --cached --check
git status --short
```

### Rollback

- Không khôi phục Excel thật, hard-coded identity hoặc shared default password.
- Nếu seeder mới lỗi, tắt DEV/TEST seeding bằng environment guard/config last-known-good và forward-fix; production giữ seeding disabled.
- Trước commit, khôi phục code bằng targeted restore theo allowlist nhưng giữ dữ liệu thật ngoài Git.
- Sau commit, revert chỉ khi kiểm tra chứng minh không đưa PII/credential cũ trở lại; nếu không, dùng compensation commit.

### Điều kiện hoàn thành Task 002 và chuyển tiếp

- 002A, 002B, 002C đều có evidence và commit riêng được review.
- Data Owner/Security ký inventory và fixture disposition.
- Zero tracked real PII fixture trong HEAD và build artifacts theo scanner/policy được phê duyệt.
- Nơi lưu, owner, retention và history-cleanup decision đã được ghi.
- Backend/frontend quality gates đạt; known baseline failure có bằng chứng và owner nếu được chấp thuận.
- `PROJECT_STATUS.md`, `CURRENT_SPRINT.md`, `CHANGELOG_INTERNAL.md`, `NEXT_SESSION.md` nhất quán.
- Working tree sạch.
- Agent dừng và chỉ đề xuất Task tiếp theo theo `EXECUTION_ORDER.md`; không tự động bắt đầu.

## 6. Ranh giới với các task khác

- Secret rotation, vault và credential lifecycle thuộc Task 007 / E02-T01. Trong Task 002 chỉ loại hard-code khỏi seeding path đang được dọn, không thiết kế secret platform.
- Auth/OIDC và mock-login removal thuộc Task 014–015 trong execution order; không kéo vào 002C.
- Test pyramid/CI scanner framework thuộc Task 005–006; Task 002 dùng kiểm tra hiện có và evidence cục bộ được Security chấp thuận.
- Encoding/mojibake baseline thuộc Task 004; synthetic fixtures phải là UTF-8 hợp lệ nhưng không sửa encoding toàn repository.
- Git history rewrite là thao tác phá hủy độc lập, chỉ thực hiện khi có kế hoạch/phê duyệt riêng; ghi quyết định không đồng nghĩa được quyền rewrite.

## 7. Phê duyệt cần có trước implementation

Trước 002A:

- Data Owner, Security approver, secure storage, retention và xác nhận backup.
- Disposition của bốn root artifacts và quyết định cấp quản trị về Git history.

Trước 002B:

- Persona/schema synthetic, PII scanner/pattern policy và fixture owner.

Trước 002C:

- Environment allowlist cho seeding, behavior khi fixture thiếu/invalid và cách cấp quyền/credential synthetic không hard-code.
- Xác nhận cho phép xóa `extract_users.py` và sửa `.csproj` nếu cần.

Mọi slice vẫn cần yêu cầu triển khai rõ ràng của người dùng. Tài liệu này không tự cấp quyền sửa code.
