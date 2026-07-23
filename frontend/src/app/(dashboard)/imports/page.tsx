"use client";

import { PermissionGate } from "@/components/portal/PermissionGate";
import { EmptyState, ServiceErrorState, SlowNetworkState, TableSkeleton } from "@/components/portal/states";
import { UserBadge } from "@/components/ui/user-badge";
import { importDataProvider, importRepository } from "@/lib/imports";
import { ImportJob } from "@/lib/imports/contracts";
import { Search, SlidersHorizontal } from "lucide-react";
import React, { useCallback, useEffect, useState } from "react";

export default function ImportCenterPage() {
  return (
    <PermissionGate permission="import.view">
      <ImportJobs />
    </PermissionGate>
  );
}

function ImportJobs() {
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [sort, setSort] = useState<"newest" | "oldest" | "status">("newest");
  const [jobs, setJobs] = useState<ImportJob[]>([]);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [slow, setSlow] = useState(false);

  const load = useCallback(
    (signal?: AbortSignal) => {
      setState("loading");
      setSlow(false);
      const timer = setTimeout(() => setSlow(true), 1200);

      importRepository
        .list({ search: query, status, sort }, signal)
        .then((value) => {
          setJobs(value);
          setState("ready");
        })
        .catch((error) => {
          if (error?.name !== "AbortError") setState("error");
        })
        .finally(() => clearTimeout(timer));

      return () => clearTimeout(timer);
    },
    [query, status, sort]
  );

  useEffect(() => {
    const controller = new AbortController();
    let clear = () => {};
    Promise.resolve().then(() => {
      if (!controller.signal.aborted) clear = load(controller.signal);
    });
    return () => {
      clear();
      controller.abort();
    };
  }, [load]);

  return (
    <div className="page-stack">
      <section className="page-heading">
        <div>
          <p className="eyebrow">DATA PLATFORM</p>
          <h2>Import Center</h2>
          <p>Monitor provider-neutral import jobs and validation outcomes.</p>
        </div>
      </section>

      {importDataProvider === "fixture" && (
        <div className="fixture-banner" role="note">
          <strong>Fixture provider</strong>
          <span>Synthetic jobs are isolated from the production API adapter.</span>
        </div>
      )}

      <section className="panel table-panel">
        <div className="filter-bar">
          <label className="search-field">
            <Search aria-hidden="true" />
            <span className="sr-only">Search imports</span>
            <input
              placeholder="Search job, source, or creator"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </label>

          <label>
            <SlidersHorizontal aria-hidden="true" />
            Status
            <select value={status} onChange={(e) => setStatus(e.target.value)}>
              <option>All</option>
              <option>Pending review</option>
              <option>Failed</option>
              <option>Completed</option>
              <option>Validating</option>
            </select>
          </label>

          <label>
            Sort
            <select value={sort} onChange={(e) => setSort(e.target.value as typeof sort)}>
              <option value="newest">Newest</option>
              <option value="oldest">Oldest</option>
              <option value="status">Status</option>
            </select>
          </label>
        </div>

        {state === "loading" && (slow ? <SlowNetworkState /> : <TableSkeleton />)}
        {state === "error" && <ServiceErrorState retry={() => load()} />}
        {state === "ready" &&
          (jobs.length ? (
            <div className="data-table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Job</th>
                    <th>Status</th>
                    <th>Source</th>
                    <th>Creator</th>
                    <th>Created</th>
                    <th>Validation</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {jobs.map((job) => (
                    <tr key={job.id}>
                      <td>
                        <strong>{job.id}</strong>
                        <small>{job.fileName}</small>
                      </td>
                      <td>
                        <span
                          className={`status-badge ${
                            job.status === "Failed"
                              ? "danger"
                              : job.status === "Completed"
                              ? "success"
                              : job.status === "Pending review"
                              ? "warning"
                              : ""
                          }`}
                        >
                          {job.status}
                        </span>
                      </td>
                      <td>{job.source}</td>
                      <td>
                        <UserBadge name={job.creator} size="sm" />
                      </td>
                      <td>
                        <time dateTime={job.createdAt}>
                          {new Date(job.createdAt).toLocaleString()}
                        </time>
                      </td>
                      <td>
                        {job.errors} errors · {job.warnings} warnings
                      </td>
                      <td>
                        <a
                          href={`/imports/${job.id}`}
                          className="secondary-action"
                          style={{ textDecoration: "none", display: "inline-block" }}
                        >
                          View Detail
                        </a>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <EmptyState
              title="No import jobs found"
              detail="Adjust the search or filters. New import behavior is not enabled in this milestone."
            />
          ))}
      </section>
    </div>
  );
}