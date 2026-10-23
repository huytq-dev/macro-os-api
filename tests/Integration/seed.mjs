// Seeds the isolated IT environment THROUGH THE API: test account (email confirmed directly in the throwaway LocalDB), 21 days of food logs
// (catalog entries, quick-add kcal-only, quick-add with macros > kcal, manual day, fasting, incomplete, gaps), weights and a body measurement.
// Idempotent: does nothing when the account already has logged days. To start over: powershell.exe -File BE\.it\api.ps1 reset  (drops MacroOS_IT).
// Usage: node BE/tests/Integration/seed.mjs
import { randomBytes } from 'node:crypto'
import { BASE, loadCreds, saveCreds, raw, client, login, localdb, addDays } from './lib.mjs'

const EMAIL = 'it-user@example.test'
let creds = loadCreds()
if (!creds) {
  const password = `Aa1!${randomBytes(9).toString('hex')}`
  const r = await raw('POST', '/api/auth/register', { body: { email: EMAIL, password, confirmPassword: password, firstName: 'IT', lastName: 'User', gender: 'Other' } })
  if (r.status < 200 || r.status >= 300) throw new Error(`register failed (${r.status}): ${JSON.stringify(r.body)?.slice(0, 300)}. If the account exists but credentials.local is gone: api.ps1 reset`)
  creds = { baseUrl: BASE, email: EMAIL, password, note: 'throwaway IT account on LocalDB MacroOS_IT; gitignored' }
  saveCreds(creds)
  console.log('registered', EMAIL)
}
localdb(`UPDATE Users SET EmailVerified = 1 WHERE Email = '${creds.email}'`) // throwaway DB only (guard in lib.localdb)
const token = await login(creds.email, creds.password)
const api = client(token)

const series = await api('GET', '/api/nutrients/macros/series')
const today = series.to
if (series.summary.loggedDays > 0) { console.log(`already seeded (loggedDays=${series.summary.loggedDays}, today=${today}); nothing to do`); process.exit(0) }

const foodCache = new Map()
async function food(name) {
  if (!foodCache.has(name)) {
    const res = await api('GET', `/api/foods?search=${encodeURIComponent(name)}&top=20`)
    const hit = res.items.find((f) => f.name.toLowerCase() === name.toLowerCase()) ?? res.items[0]
    if (!hit) throw new Error(`seed food not found: ${name}`)
    foodCache.set(name, hit.id)
  }
  return foodCache.get(name)
}

// [dayOffset, kind, payload, extra]  (offset 0 = today, -4/-8/-11/-14/-15/-17/-19 intentionally unlogged = gaps)
const plan = [
  [0, 'app', [['Chicken breast, cooked', 150], ['White rice, cooked', 200]]],
  [-1, 'quick', { name: 'IT quick kcal only', calories: 2100, protein: 0, carbs: 0, fat: 0 }],
  [-2, 'app', [['Broccoli, boiled', 200], ['Almonds', 40], ['Extra Virgin Olive Oil', 15]]],
  [-3, 'fast'],
  [-5, 'quick', { name: 'IT macros exceed kcal', calories: 500, protein: 60, carbs: 80, fat: 20 }],
  [-6, 'app', [['Banana', 120], ['Apple', 150], ['Oats, dry', 60]]],
  [-7, 'app', [['Beef, lean, cooked', 200], ['White rice, cooked', 250]], 'incomplete'],
  [-9, 'manual', { calories: 1800, proteinG: 120, fatG: 60, carbsG: 180 }],
  [-10, 'app', [['Egg, hard-boiled', 120], ['Brown rice, cooked', 200], ['Broccoli, boiled', 150]]],
  [-12, 'app', [['Chicken breast, cooked', 180], ['Sticky rice, cooked', 200]]],
  [-13, 'app', [['Egg, hard-boiled', 100], ['Oats, dry', 50], ['Banana', 100]]],
  [-16, 'app', [['Beef, lean, cooked', 150], ['White rice, cooked', 200]]],
  [-18, 'quick', { name: 'IT quick complete macros', calories: 1900, protein: 110, carbs: 210, fat: 70 }],
  [-20, 'fast'],
]
for (const [offset, kind, payload, extra] of plan) {
  const date = addDays(today, offset)
  if (kind === 'app')
    for (const [name, grams] of payload)
      await api('POST', '/api/food-logs/entries', { logDate: date, mealType: 'Lunch', foodItemId: await food(name), quantity: grams })
  else if (kind === 'quick') await api('POST', '/api/food-logs/quick-add', { logDate: date, mealType: 'Dinner', ...payload })
  else if (kind === 'fast') await api('PUT', `/api/nutrition-data/${date}`, { isFasting: true })
  else if (kind === 'manual') await api('PUT', `/api/nutrition-data/${date}`, payload)
  if (extra === 'incomplete') await api('PUT', `/api/nutrition-data/${date}`, { isIncomplete: true })
}

for (let i = 0; i < 14; i++) {
  const kg = Math.round((82.4 - 0.05 * (13 - i) + (((i * 7) % 5) - 2) * 0.15) * 10) / 10
  await api('POST', '/api/weights', { logDate: addDays(today, -i), weightKg: kg })
}
await api('POST', '/api/body-metrics/measurements/waist', { date: today, valueCm: 84.5 })

const after = await api('GET', '/api/nutrients/macros/series')
console.log(`seeded: today=${today} loggedDays=${after.summary.loggedDays} (expected 13 counted: 14 plan days - 1 incomplete); weights=14; waist=1`)
