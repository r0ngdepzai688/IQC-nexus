import { ApiImportJobRepository } from "./apiRepository";
import { FixtureImportJobRepository } from "./fixtureRepository";
export const importRepository = process.env.NEXT_PUBLIC_IMPORT_DATA_PROVIDER === "api" ? new ApiImportJobRepository() : new FixtureImportJobRepository();
export const importDataProvider = process.env.NEXT_PUBLIC_IMPORT_DATA_PROVIDER === "api" ? "api" : "fixture";