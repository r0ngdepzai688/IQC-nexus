"use client";

import { AuthApiError } from "@/lib/auth/client";
import { useAuth } from "@/lib/contexts/AuthContext";
import { Eye, EyeOff, LockKeyhole } from "lucide-react";
import { useRouter } from "next/navigation";
import React, { useState } from "react";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!username.trim() || !password) return;

    setLoading(true);
    setError("");

    try {
      await login(username.trim(), password);
      router.replace("/overview");
    } catch (reason: unknown) {
      if (reason instanceof AuthApiError) {
        if (reason.status === 429) {
          setError("Too many sign-in attempts. Please wait one minute and try again.");
        } else if (reason.status === 401) {
          setError(reason.message || "Invalid username or password, or account disabled.");
        } else {
          setError(reason.message || "Authentication failed. Please verify credentials.");
        }
      } else {
        setError("Authentication service is unavailable. Please check your network connection.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="login-grid">
      <section className="login-identity" aria-labelledby="product-name">
        <div className="brand-mark" aria-hidden="true">
          IQ
        </div>
        <div>
          <p className="eyebrow">QUALITY OPERATIONS PLATFORM</p>
          <h1 id="product-name">IQC Nexus</h1>
          <p className="login-purpose">
            Controlled access to incoming-quality workflows, evidence, and audit activity.
          </p>
        </div>
        <dl className="login-facts">
          <div>
            <dt>Environment</dt>
            <dd>{process.env.NEXT_PUBLIC_ENVIRONMENT || "Development"}</dd>
          </div>
          <div>
            <dt>Access</dt>
            <dd>Role-based control</dd>
          </div>
        </dl>
      </section>

      <section className="login-panel">
        <form className="login-form" onSubmit={handleSubmit} noValidate>
          <LockKeyhole className="login-icon" aria-hidden="true" />
          <p className="eyebrow">SECURE ACCESS</p>
          <h2>Access your workspace</h2>
          <p className="muted">Use your assigned platform credentials.</p>

          <label htmlFor="username">Username or Email</label>
          <input
            id="username"
            type="text"
            autoComplete="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
            autoFocus
            placeholder="e.g. SYN-0001"
          />

          <label htmlFor="password">Password</label>
          <div className="password-field">
            <input
              id="password"
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="Enter password"
            />
            <button
              type="button"
              className="icon-button"
              onClick={() => setShowPassword((val) => !val)}
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? <EyeOff aria-hidden="true" /> : <Eye aria-hidden="true" />}
            </button>
          </div>

          {error && (
            <p className="state-inline state-danger" role="alert">
              {error}
            </p>
          )}

          <button type="submit" className="primary-action" disabled={loading}>
            {loading ? "Signing in…" : "Sign in"}
          </button>

          <p className="security-note">
            Access attempts are audited. Contact your system administrator if your account is locked or disabled.
          </p>
        </form>
      </section>
    </main>
  );
}