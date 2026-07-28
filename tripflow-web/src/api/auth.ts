import { apiFetch } from "./client";
import type { AccessTokenResponse, LoginResponse, UserDto } from "./types";

export function register(name: string, email: string, password: string) {
  return apiFetch<UserDto>("/api/auth/register", { method: "POST", body: { name, email, password }, skipAuth: true });
}

export function confirmEmail(email: string, code: string) {
  return apiFetch<void>("/api/auth/confirm-email", { method: "POST", body: { email, code }, skipAuth: true });
}

export function login(email: string, password: string) {
  return apiFetch<LoginResponse>("/api/auth/login", { method: "POST", body: { email, password }, skipAuth: true });
}

export function verifyTwoFactor(email: string, challengeToken: string, code?: string, recoveryCode?: string) {
  return apiFetch<AccessTokenResponse>("/api/auth/2fa/verify", {
    method: "POST",
    body: { email, challengeToken, code: code ?? null, recoveryCode: recoveryCode ?? null },
    skipAuth: true,
  });
}

export function logout() {
  return apiFetch<void>("/api/auth/logout", { method: "POST" });
}

export function setupTwoFactor() {
  return apiFetch<{ secret: string; otpAuthUri: string; qrCodePngBase64: string }>("/api/auth/2fa/setup", { method: "POST" });
}

export function enableTwoFactor(code: string) {
  return apiFetch<string[]>("/api/auth/2fa/enable", { method: "POST", body: { code } });
}

export function disableTwoFactor(password: string, code: string) {
  return apiFetch<void>("/api/auth/2fa/disable", { method: "POST", body: { password, code } });
}
