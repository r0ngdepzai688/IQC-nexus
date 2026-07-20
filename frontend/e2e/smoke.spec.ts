import { expect, test, type Page } from "@playwright/test";

const syntheticUserId = "SYN-0001";

async function loginAsSyntheticUser(page: Page) {
  const password = process.env.IQC_E2E_SEED_PASSWORD;
  if (!password) {
    throw new Error("The Playwright process did not receive its generated synthetic seed password.");
  }

  await page.goto("/login");
  await page.getByLabel(/Mã nhân viên/i).fill(syntheticUserId);
  await page.getByLabel(/Mật khẩu/i).fill(password);
  await page.getByRole("button", { name: /^Đăng nhập$/i }).click();
  await expect(page).toHaveURL(/\/overview$/);
}

test("login page exposes the supported authentication controls", async ({ page }) => {
  await page.goto("/login");

  await expect(page).toHaveTitle("IQC Quality Management Cloud");
  await expect(page.getByRole("heading", { name: "Đăng nhập" })).toBeVisible();
  await expect(page.getByLabel(/Mã nhân viên/i)).toBeVisible();
  await expect(page.getByLabel(/Mật khẩu/i)).toBeVisible();
});

test("unauthenticated visitors are redirected from a protected route", async ({ page }) => {
  await page.goto("/overview");

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole("heading", { name: "Đăng nhập" })).toBeVisible();
});

test("a seeded synthetic user can log in and reach the protected overview", async ({ page }) => {
  await loginAsSyntheticUser(page);

  await expect(page.getByRole("heading", { name: "Enterprise Command Center" })).toBeVisible();
});

test("an authenticated synthetic user can load the empty Data Hub history", async ({ page }) => {
  await loginAsSyntheticUser(page);
  await page.goto("/support/data-hub");

  await expect(page.getByRole("heading", { name: "Data Hub Operations" })).toBeVisible();
  await expect(page.getByText("No import history found.")).toBeVisible();
});

test("an authenticated user can inspect, review, and commit a new Master Plan workbook", async ({ page }) => {
  await loginAsSyntheticUser(page);
  const basic = `SYN-E2E-${Date.now()}`;
  await page.goto("/new-model/import");
  await page.getByLabel("Master Plan workbook").setInputFiles({
    name: "synthetic-master-plan.xlsx",
    mimeType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    buffer: createWorkbook(basic),
  });

  await expect(page.getByRole("heading", { name: "Header mapping" })).toBeVisible();
  await page.getByRole("button", { name: "Upload and stage" }).click();
  await expect(page.getByRole("heading", { name: "Validation and review" })).toBeVisible();
  for (let attempt = 0; attempt < 10; attempt++) {
    const override = page.getByRole("button", { name: "Override" });
    if (await override.count()) {
      await override.first().click();
      await expect.poll(async () => {
        const next = page.getByRole("button", { name: "Override" });
        return await next.count() === 0 || await next.first().isEnabled();
      }).toBe(true);
      continue;
    }
    const accept = page.getByRole("button", { name: "Accept" });
    if (await accept.count()) {
      await accept.first().click();
      await expect.poll(async () => {
        const next = page.getByRole("button", { name: "Accept" });
        return await next.count() === 0 || await next.first().isEnabled();
      }).toBe(true);
      continue;
    }
    break;
  }
  const commit = page.getByRole("button", { name: "Confirm atomic upsert" });
  await expect(commit).toBeEnabled();
  await commit.click();
  await expect(page.getByRole("heading", { name: "Commit successful" })).toBeVisible();
  await expect(page.getByText("1 record(s) inserted, 0 updated. 0 rows were skipped with no change.")).toBeVisible();

  const token = await page.evaluate(() => window.localStorage.getItem("token"));
  const response = await page.request.get("http://localhost:5000/api/masterplan/records", {
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(response.ok()).toBe(true);
  expect(await response.json()).toEqual(expect.arrayContaining([expect.objectContaining({ basic, cat: "LPR" })]));
});

function createWorkbook(basic: string): Buffer {
  const escape = (value: string) => value.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");
  const cells = (values: string[], row: number) => values.map((value, index) => `<c r="${String.fromCharCode(65 + index)}${row}" t="inlineStr"><is><t>${escape(value)}</t></is></c>`).join("");
  const entries: Array<[string, string]> = [
    ["[Content_Types].xml", '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>'],
    ["_rels/.rels", '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>'],
    ["xl/workbook.xml", '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="MasterPlan" sheetId="1" r:id="rId1"/></sheets></workbook>'],
    ["xl/_rels/workbook.xml.rels", '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>'],
    ["xl/worksheets/sheet1.xml", `<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1">${cells(["Project Name", "Basic", "Grade", "Cat", "PVR Target", "Area", "PIC"], 1)}</row><row r="2">${cells(["Synthetic E2E Model", basic, "B", "LPR", "2026-08-01", "Synthetic Area", "Synthetic PIC"], 2)}</row></sheetData></worksheet>`],
  ];
  return createStoredZip(entries);
}

function createStoredZip(entries: Array<[string, string]>): Buffer {
  const localParts: Buffer[] = [];
  const centralParts: Buffer[] = [];
  let offset = 0;
  for (const [name, text] of entries) {
    const nameBuffer = Buffer.from(name);
    const data = Buffer.from(text);
    const checksum = crc32(data);
    const local = Buffer.alloc(30);
    local.writeUInt32LE(0x04034b50, 0); local.writeUInt16LE(20, 4); local.writeUInt16LE(0x0800, 6);
    local.writeUInt32LE(checksum, 14); local.writeUInt32LE(data.length, 18); local.writeUInt32LE(data.length, 22); local.writeUInt16LE(nameBuffer.length, 26);
    localParts.push(local, nameBuffer, data);
    const central = Buffer.alloc(46);
    central.writeUInt32LE(0x02014b50, 0); central.writeUInt16LE(20, 4); central.writeUInt16LE(20, 6); central.writeUInt16LE(0x0800, 8);
    central.writeUInt32LE(checksum, 16); central.writeUInt32LE(data.length, 20); central.writeUInt32LE(data.length, 24); central.writeUInt16LE(nameBuffer.length, 28); central.writeUInt32LE(offset, 42);
    centralParts.push(central, nameBuffer);
    offset += local.length + nameBuffer.length + data.length;
  }
  const centralDirectory = Buffer.concat(centralParts);
  const end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0); end.writeUInt16LE(entries.length, 8); end.writeUInt16LE(entries.length, 10);
  end.writeUInt32LE(centralDirectory.length, 12); end.writeUInt32LE(offset, 16);
  return Buffer.concat([...localParts, centralDirectory, end]);
}

function crc32(data: Buffer): number {
  let crc = 0xffffffff;
  for (const byte of data) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1));
  }
  return (crc ^ 0xffffffff) >>> 0;
}
