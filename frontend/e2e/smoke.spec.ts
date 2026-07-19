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
