// Shared helpers for the real FE<->BE integration scripts (run against the isolated IT API: see BE/.it/api.ps1).
// Node 18+ (global fetch). Talks ONLY to localhost; TLS verification is relaxed for the local dev certificate.
import { readFileSync, writeFileSync, existsSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0' // localhost dev certificate only; the base URL guard below refuses anything else

const here = dirname(fileURLToPath(import.meta.url))
export const CRED_FILE = join(here, '..', '..', '.it', 'credentials.local')
export const BASE = process.env.IT_BASE_URL ?? 'https://localhost:7000'
if (!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(BASE)) throw new Error(`refusing non-local base URL: ${BASE}`)

export const loadCreds = () => (existsSync(CRED_FILE) ? JSON.parse(readFileSync(CRED_FILE, 'utf8')) : null)
export const saveCreds = (c) => writeFileSync(CRED_FILE, JSON.stringify(c, null, 2))

/** Raw call; returns { status, body } where body is the ApiResponse envelope (or null). */
export async function raw(method, path, { token, body } = {}) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  const text = await res.text()
  let json = null
  try { json = text ? JSON.parse(text) : null } catch { /* non-JSON error body */ }
  return { status: res.status, body: json }
}

/** Authenticated call that throws unless 2xx; returns the envelope's `data`. */
export function client(token) {
  return async (method, path, body) => {
    const r = await raw(method, path, { token, body })
    if (r.status < 200 || r.status >= 300) throw new Error(`${method} ${path} -> ${r.status} ${JSON.stringify(r.body)?.slice(0, 400)}`)
    return r.body?.data
  }
}

export async function login(email, password) {
  const r = await raw('POST', '/api/auth/login', { body: { email, password } })
  if (r.status !== 200) throw new Error(`login failed: ${r.status} ${JSON.stringify(r.body)?.slice(0, 300)}`)
  return r.body.data.accessToken
}

/** Run a statement on the throwaway LocalDB database ONLY (guarded: server and database are fixed here). */
export function localdb(sql) {
  const out = spawnSync('sqlcmd', ['-S', '(localdb)\\MSSQLLocalDB', '-d', 'MacroOS_IT', '-E', '-I', '-b', '-h', '-1', '-W', '-Q', sql], { encoding: 'utf8' })
  if (out.status !== 0) throw new Error(`sqlcmd failed: ${out.stdout}${out.stderr}`)
  return out.stdout.trim()
}

export const iso = (d) => d.toISOString().slice(0, 10)
export const addDays = (isoDate, n) => {
  const d = new Date(`${isoDate}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + n)
  return iso(d)
}
export const sleep = (ms) => new Promise((r) => setTimeout(r, ms))
