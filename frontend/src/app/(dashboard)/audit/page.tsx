"use client";

import { PermissionGate } from "@/components/portal/PermissionGate";
import { UserBadge } from "@/components/ui/user-badge";
import { Activity } from "lucide-react";
import React from "react";

const events = [
  {
    time: "2026-07-23 21:15:18",
    actor: "Synthetic Operator",
    action: "Mapping confirmed",
    resource: "IQC-SYN-1042",
    result: "Success",
  },
  {
    time: "2026-07-23 20:51:12",
    actor: "Synthetic Engineer",
    action: "Validation completed",
    resource: "IQC-SYN-1039",
    result: "Failed",
  },
];

export default function AuditPage() {
  return (
    <PermissionGate permission="audit.view">
      <div className="page-stack">
        <section className="page-heading">
          <div>
            <p className="eyebrow">TRACEABILITY</p>
            <h2>Audit Trail</h2>
            <p>Security and lifecycle events for authorized reviewers.</p>
          </div>
        </section>

        <div className="fixture-banner" role="note">
          <strong>Fixture provider</strong>
          <span>All activity shown is synthetic.</span>
        </div>

        <section className="panel table-panel">
          <div className="data-table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>Actor</th>
                  <th>Action</th>
                  <th>Resource</th>
                  <th>Outcome</th>
                </tr>
              </thead>
              <tbody>
                {events.map((event) => (
                  <tr key={event.time}>
                    <td>{event.time}</td>
                    <td>
                      <UserBadge name={event.actor} size="sm" />
                    </td>
                    <td>
                      <span className="artifact-name">
                        <Activity aria-hidden="true" />
                        {event.action}
                      </span>
                    </td>
                    <td>{event.resource}</td>
                    <td>
                      <span
                        className={`status-badge ${
                          event.result === "Success" ? "success" : "danger"
                        }`}
                      >
                        {event.result}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </PermissionGate>
  );
}