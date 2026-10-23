// Real FE<->BE integration check of GET /api/nutrients/macros/series against the isolated IT API (seeded by seed.mjs).
// Asserts: continuous days; unlogged = null; kcal parts 4/9/4; Other signed; averages per logged day (fasting counts, incomplete excluded);
// bucket consistency for Day/Week/Month; seeded vectors; and that POST / PUT / DELETE of a food entry (and quick-add, fasting) change the series accordingly.
// Usage: node BE/tests/Integration/macros-e2e.mjs      Exit code 1 on any FAIL.
import { loadCreds, login, client, raw, addDays } from './lib.mjs'

const creds = loadCreds()
if (!creds) { console.error('no BE/.it/credentials.local: run seed.mjs first'); process.exit(2) }
const token = await login(creds.email, creds.password)
const api = client(token)

let pass = 0, fail = 0
const check = (ok, msg, detail = '') => { ok ? pass++ : fail++; console.log(`${ok ? 'PASS' : 'FAIL'} ${msg}${ok || !detail ? '' : `  -> ${detail}`}`) }
const near = (a, b, tol) => a !== null && b !== null && a !== undefined && b !== undefined && Math.abs(a - b) <= tol
const dayNum = (d) => Math.round(Date.parse(`${d}T00:00:00Z`) / 86400000)
const mean = (xs) => (xs.length ? xs.reduce((s, x) => s + x, 0) / xs.length : null)
const series = (q = '') => api('GET', `/api/nutrients/macros/series${q}`)
const VALUE_FIELDS = ['caloriesKcal', 'proteinG', 'fatG', 'carbsG', 'proteinKcal', 'fatKcal', 'carbsKcal', 'otherKcal']
const counted = (days) => days.filter((d) => d.isLogged && d.caloriesKcal !== null && !d.isIncomplete)

const base = await series()
const today = base.to
const q = {
  '1W': `?from=${addDays(today, -6)}&to=${today}`,
  '1M': `?from=${addDays(today, -29)}&to=${today}`,
  All: '?all=true',
}

function checkAverages(a, days, label) {
  if (days.length === 0) { check(VALUE_FIELDS.every((f) => a[f] === null) && a.proteinPercent === null, `${label}: no counted day => every average null`); return }
  const avg = (f) => mean(days.map((d) => d[f]))
  check(near(a.caloriesKcal, avg('caloriesKcal'), 0.1) && near(a.proteinG, avg('proteinG'), 0.1) && near(a.fatG, avg('fatG'), 0.1) && near(a.carbsG, avg('carbsG'), 0.1),
    `${label}: averages = mean per counted day (kcal ${a.caloriesKcal}, n=${days.length})`, JSON.stringify(a))
  check(near(a.proteinKcal, avg('proteinKcal'), 0.2) && near(a.fatKcal, avg('fatKcal'), 0.2) && near(a.carbsKcal, avg('carbsKcal'), 0.2) && near(a.otherKcal, avg('otherKcal'), 0.2),
    `${label}: kcal-part averages per counted day, Other averaged signed (${a.otherKcal})`)
  if (a.caloriesKcal > 0)
    check(['protein', 'fat', 'carbs', 'other'].every((p) => near(a[`${p}Percent`], (a[`${p}Kcal`] / a.caloriesKcal) * 100, 0.3)), `${label}: percent = part avg / total avg x 100 (Other may make the sum != 100)`, JSON.stringify(a))
}

function checkSeries(r, name, granularity) {
  const { days } = r
  const n = dayNum(r.to) - dayNum(r.from) + 1
  check(days.length === n && days.every((d, i) => d.date === addDays(r.from, i)) && r.to <= today, `${name}: ${n} continuous ascending days from ${r.from} to ${r.to} (<= today)`)
  const unlogged = days.filter((d) => !d.isLogged)
  check(unlogged.every((d) => VALUE_FIELDS.every((f) => d[f] === null) && !d.isFasting && !d.isIncomplete), `${name}: ${unlogged.length} unlogged day(s) have every value null (not 0)`)
  const logged = days.filter((d) => d.isLogged)
  check(logged.every((d) => d.caloriesKcal !== null && near(d.proteinKcal, 4 * d.proteinG, 0.25) && near(d.fatKcal, 9 * d.fatG, 0.5) && near(d.carbsKcal, 4 * d.carbsG, 0.25)), `${name}: kcal parts are protein x4, fat x9, carbs x4 on ${logged.length} logged day(s)`)
  check(logged.every((d) => near(d.otherKcal, d.caloriesKcal - (d.proteinKcal + d.fatKcal + d.carbsKcal), 0.6)), `${name}: Other = reported kcal - (P+F+C kcal), signed, never clamped`)
  const fasting = days.filter((d) => d.isFasting)
  check(fasting.every((d) => d.isLogged && VALUE_FIELDS.every((f) => d[f] === 0)), `${name}: ${fasting.length} fasting day(s) are logged with zeros`)
  const cnt = counted(days)
  check(r.summary.loggedDays === cnt.length && r.summary.calendarDays === days.length, `${name}: summary.loggedDays=${r.summary.loggedDays} = counted days (calendarDays ${r.summary.calendarDays} is reference only)`)
  checkAverages(r.summary.averages, cnt, `${name} summary`)
  if (!granularity) { check(r.buckets.length === 0, `${name}: no buckets without granularity`); return }
  const b = r.buckets
  check(b.every((x, i) => i === 0 || b[i - 1].start < x.start) && b.every((x) => x.loggedDays > 0), `${name}/${granularity}: ${b.length} bucket(s) ascending, none empty`)
  check(b.reduce((s, x) => s + x.loggedDays, 0) === cnt.length, `${name}/${granularity}: bucket loggedDays add up to the summary (${cnt.length})`)
  for (const x of b) {
    const inside = cnt.filter((d) => d.date >= x.start && d.date <= x.end)
    check(x.loggedDays === inside.length, `${name}/${granularity}: bucket ${x.start}..${x.end} loggedDays=${x.loggedDays}`, `expected ${inside.length}`)
    checkAverages(x.averages, inside, `${name}/${granularity} ${x.start}`)
  }
  const span = (x) => dayNum(x.end) - dayNum(x.start)
  if (granularity === 'Day') check(b.every((x) => x.start === x.end), `${name}/Day: one day per bucket`)
  if (granularity === 'Week') check(b.every((x) => span(x) === 6), `${name}/Week: 7-day buckets`)
  if (granularity === 'Month') check(b.every((x) => x.start.endsWith('-01')), `${name}/Month: buckets start on the 1st`)
}

const fetched = {}
for (const [name, query] of Object.entries(q)) {
  fetched[name] = await series(query)
  checkSeries(fetched[name], name, null)
  for (const g of ['Day', 'Week', 'Month']) checkSeries(await series(`${query}${query.includes('?') ? '&' : '?'}granularity=${g}`), name, g)
}

// --- seeded vectors (seed.mjs); only meaningful on the seeded IT account
const byDate = (r, off) => r.days.find((d) => d.date === addDays(today, off))
const m1 = fetched['1M']
if (counted(m1.days).length === 13) {
  check(counted(fetched['1W'].days).length === 6 && counted(m1.days).length === 13 && counted(fetched.All.days).length === 13, 'seeded: counted days 1W=6, 1M=13, All=13 (13 logged - incomplete day excluded; fasting days count)')
  const d0 = byDate(m1, 0), dQ = byDate(m1, -1), dN = byDate(m1, -2), dF = byDate(m1, -3), dG = byDate(m1, -4), dX = byDate(m1, -5), dI = byDate(m1, -7), dM = byDate(m1, -9)
  check(near(d0.caloriesKcal, 507.5, 0.2) && d0.otherKcal > 0 && d0.otherKcal < 40, `seeded today (chicken+rice): ${d0.caloriesKcal} kcal, Other ${d0.otherKcal} > 0 (reported above 4/9/4 derived)`)
  check(dQ.caloriesKcal === 2100 && dQ.otherKcal === 2100 && dQ.proteinKcal === 0, 'seeded quick-add kcal-only: Other = the whole 2100 kcal')
  check(dN.otherKcal < 0, `seeded broccoli+almond+olive oil: Other ${dN.otherKcal} < 0 (signed, not clamped)`)
  check(dF.isFasting && dF.isLogged && dF.caloriesKcal === 0, 'seeded fasting day: logged, 0 kcal')
  check(!dG.isLogged && dG.caloriesKcal === null && dG.otherKcal === null, 'seeded gap day: unlogged, null (not 0)')
  check(dX.caloriesKcal === 500 && dX.proteinKcal === 240 && dX.fatKcal === 180 && dX.carbsKcal === 320 && dX.otherKcal === -240, 'seeded quick-add macros > kcal: 500 kcal vs 740 from macros => Other = -240, P/F/C unchanged')
  check(dI.isLogged && dI.isIncomplete && dI.caloriesKcal > 0 && !counted(m1.days).includes(dI), 'seeded incomplete day: has numbers but is excluded from every statistic')
  check(dM.caloriesKcal === 1800 && dM.proteinKcal === 480 && dM.fatKcal === 540 && dM.carbsKcal === 720 && dM.otherKcal === 60, 'seeded manual day 1800/120P/60F/180C: Other = +60')
  const negDays = counted(m1.days).filter((d) => d.otherKcal < 0), posDays = counted(m1.days).filter((d) => d.otherKcal > 0)
  console.log(`INFO seeded 1M negative-Other days: ${negDays.length}/${counted(m1.days).length} (min ${Math.min(...negDays.map((d) => d.otherKcal))}); positive: ${posDays.length}; ` +
    `Other as % of day kcal (neg): ${negDays.map((d) => `${Math.round((d.otherKcal / d.caloriesKcal) * 100)}%`).join(', ')}`)
} else console.log('SKIP seeded vectors (account not seeded as expected)')

// --- mutation round trip on an unlogged day: POST -> PUT -> DELETE entry, quick-add, fasting; series must follow and finally return to baseline
const D = addDays(today, -4)
const base1 = await series(`${q['1M']}&granularity=Week`)
const baseN = counted(base1.days).length, baseAvg = base1.summary.averages.caloriesKcal
const chickenSearch = await api('GET', '/api/foods?search=Chicken%20breast%2C%20cooked&top=5')
const chicken = chickenSearch.items.find((f) => f.name === 'Chicken breast, cooked') ?? chickenSearch.items[0]
const on = async () => byDate(await series(`${q['1M']}&granularity=Week`), -4)
check(!(await on()).isLogged, `mutation: ${D} starts unlogged`)

const entryId = await api('POST', '/api/food-logs/entries', { logDate: D, mealType: 'AfternoonSnack', foodItemId: chicken.id, quantity: 100 })
let r = await series(`${q['1M']}&granularity=Week`); let d = byDate(r, -4)
check(d.isLogged && near(d.caloriesKcal, 165, 0.2) && near(d.proteinG, 31, 0.1) && near(d.fatG, 3.6, 0.1) && d.carbsG === 0 && near(d.otherKcal, 165 - (124 + 32.4), 0.3), `POST entry: day becomes logged ${d.caloriesKcal} kcal, Other ${d.otherKcal}`)
check(r.summary.loggedDays === baseN + 1 && near(r.summary.averages.caloriesKcal, (baseAvg * baseN + 165) / (baseN + 1), 0.2), `POST entry: loggedDays ${baseN}->${r.summary.loggedDays}, average ${baseAvg} -> ${r.summary.averages.caloriesKcal} (per logged day)`)

await api('PUT', `/api/food-logs/entries/${entryId}`, { quantity: 200 })
r = await series(`${q['1M']}&granularity=Week`); d = byDate(r, -4)
check(near(d.caloriesKcal, 330, 0.3) && near(d.proteinG, 62, 0.2) && r.summary.loggedDays === baseN + 1, `PUT entry (100 g -> 200 g): day ${d.caloriesKcal} kcal, protein ${d.proteinG} g, loggedDays unchanged`)

await api('DELETE', `/api/food-logs/entries/${entryId}`)
r = await series(`${q['1M']}&granularity=Week`); d = byDate(r, -4)
check(!d.isLogged && d.caloriesKcal === null && r.summary.loggedDays === baseN && near(r.summary.averages.caloriesKcal, baseAvg, 0.05), 'DELETE entry: day back to unlogged/null and the statistics return to baseline')

const quickId = await api('POST', '/api/food-logs/quick-add', { logDate: D, mealType: 'Dinner', name: 'IT temp', calories: 400, protein: 0, carbs: 0, fat: 0 })
d = byDate(await series(`${q['1M']}&granularity=Week`), -4)
check(d.caloriesKcal === 400 && d.otherKcal === 400 && d.proteinKcal === 0, 'POST quick-add (kcal only): Other = 400')
await api('DELETE', `/api/food-logs/entries/${quickId}`)
check(!(await on()).isLogged, 'DELETE quick-add entry: unlogged again')

await api('PUT', `/api/nutrition-data/${D}`, { isFasting: true })
r = await series(`${q['1M']}&granularity=Week`); d = byDate(r, -4)
check(d.isFasting && d.isLogged && d.caloriesKcal === 0 && r.summary.loggedDays === baseN + 1 && r.summary.averages.caloriesKcal < baseAvg, `PUT fasting: logged with 0 kcal, loggedDays +1, average drops (${baseAvg} -> ${r.summary.averages.caloriesKcal})`)
await api('DELETE', `/api/nutrition-data/${D}`)
r = await series(`${q['1M']}&granularity=Week`)
check(!byDate(r, -4).isLogged && r.summary.loggedDays === baseN && near(r.summary.averages.caloriesKcal, baseAvg, 0.05), 'DELETE fasting day: back to baseline')

// --- data-integrity cases: the BE never changes calories or macros to make the chart look nicer
const snapshot = await series('?all=true')
{
  const nd = await api('GET', `/api/nutrition-data?from=${snapshot.from}&to=${snapshot.to}`)
  const byD = new Map(snapshot.days.map((x) => [x.date, x]))
  check(nd.items.length === snapshot.days.filter((x) => x.isLogged).length, `integrity: Nutrition Data Manager lists exactly the ${nd.items.length} logged days of the series (gaps absent)`)
  check(nd.items.every((i) => { const s = byD.get(i.date); return s?.isLogged && near(s.caloriesKcal, i.calories, 0.011) && near(s.proteinG, i.proteinG, 0.011) && near(s.fatG, i.fatG, 0.011) && near(s.carbsG, i.carbsG, 0.011) && s.isFasting === i.isFasting && s.isIncomplete === i.isIncomplete }),
    'integrity: series kcal / P / F / C / fasting / incomplete equal the stored day values read from another endpoint (no normalising, no clamping, no derived kcal)')
  const x = nd.items.find((i) => i.date === addDays(today, -5))
  check(x && x.calories === 500 && x.proteinG === 60 && x.fatG === 20 && x.carbsG === 80, 'integrity: the macros-above-kcal quick-add is stored and served as entered (500 kcal, 60P/20F/80C), Other carries the difference')
}

// missing data: a window with nothing logged, and a window reaching into the future
{
  const gap = await series(`?from=${addDays(today, -100)}&to=${addDays(today, -90)}&granularity=Week`)
  check(gap.days.length === 11 && gap.days.every((x) => !x.isLogged && VALUE_FIELDS.every((f) => x[f] === null)) && gap.summary.loggedDays === 0 && Object.values(gap.summary.averages).every((v) => v === null) && gap.buckets.length === 0,
    'missing data: 11 empty days => every day null, loggedDays 0, every average null (not 0), no buckets')
  const fut = await series(`?from=${addDays(today, -2)}&to=${addDays(today, 5)}`)
  check(fut.to === today && fut.days.length === 3, 'missing data: a "to" in the future is clipped to today (no invented future days)')
}

// quick-add / fasting / incomplete behaviours on gap days (temporary, removed again)
{
  const G = addDays(today, -8), H = addDays(today, -11)
  const one = async (date) => (await series('?all=true')).days.find((x) => x.date === date)
  const qa = await raw('POST', '/api/food-logs/quick-add', { token, body: { logDate: G, mealType: 'Dinner', name: 'IT macros only', calories: 0, protein: 25, carbs: 0, fat: 0 } })
  if (qa.status === 200) {
    const x = await one(G)
    check(x.caloriesKcal === 0 && x.proteinKcal === 100 && x.otherKcal === -100, `quick-add with 0 kcal and 25 g protein is accepted and kept as entered: Other = -100 (not "fixed" to 100 kcal)`)
    await api('DELETE', `/api/food-logs/entries/${qa.body.data}`)
  } else check(qa.status === 400, `quick-add with 0 kcal but macros is rejected by validation (${qa.status}), nothing stored`)
  check(!(await one(G)).isLogged, 'quick-add case cleaned up: day is unlogged/null again')

  // incomplete: numbers stay, statistics drop the day; flipping back restores
  const before = (await series('?all=true')).summary
  const qid = await api('POST', '/api/food-logs/quick-add', { logDate: G, mealType: 'Dinner', name: 'IT incomplete', calories: 600, protein: 0, carbs: 0, fat: 0 })
  const counted1 = (await series('?all=true')).summary
  await api('PUT', `/api/nutrition-data/${G}`, { isIncomplete: true })
  const inc = await series('?all=true'); const xi = inc.days.find((x) => x.date === G)
  check(counted1.loggedDays === before.loggedDays + 1 && xi.isIncomplete && xi.caloriesKcal === 600 && inc.summary.loggedDays === before.loggedDays && near(inc.summary.averages.caloriesKcal, before.averages.caloriesKcal, 0.01),
    'incomplete: day keeps 600 kcal in the series but is excluded from loggedDays and every average; flag off restores it')
  await api('PUT', `/api/nutrition-data/${G}`, { isIncomplete: false })
  check((await series('?all=true')).summary.loggedDays === before.loggedDays + 1, 'incomplete flag off: the day counts again')
  await api('DELETE', `/api/food-logs/entries/${qid}`)

  // fasting rules
  const inc400 = await raw('PUT', `/api/nutrition-data/${H}`, { token, body: { isIncomplete: true } })
  check(inc400.status === 400, `incomplete on a day with no data is rejected (${inc400.status}), no phantom day`)
  const fast400 = await raw('PUT', `/api/nutrition-data/${today}`, { token, body: { isFasting: true } })
  check(fast400.status === 400 && byDate(await series(`${q['1M']}`), 0).caloriesKcal === 507.5, `fasting on a day that has food is rejected (${fast400.status}) and the day's kcal stay 507.5`)
  await api('PUT', `/api/nutrition-data/${G}`, { isFasting: true })
  const food = chicken.id
  const eid = await api('POST', '/api/food-logs/entries', { logDate: G, mealType: 'Lunch', foodItemId: food, quantity: 100 })
  const xf = (await series('?all=true')).days.find((x) => x.date === G)
  check(!xf.isFasting && xf.isLogged && near(xf.caloriesKcal, 165, 0.2), 'fasting day + a food entry: the day stops being a fasting day and shows the entry (165 kcal)')
  await api('DELETE', `/api/food-logs/entries/${eid}`)
  const xe = (await series('?all=true')).days.find((x) => x.date === G)
  check(!xe.isLogged && !xe.isFasting && xe.caloriesKcal === null, 'removing that entry leaves the day unlogged (null), not fasting')
}

// nothing the checks did changed any stored value: the whole series equals the snapshot taken before the extra cases
{
  const again = await series('?all=true')
  check(JSON.stringify(again.days) === JSON.stringify(snapshot.days) && JSON.stringify(again.summary) === JSON.stringify(snapshot.summary), 'integrity: the full series + summary after all mutations are byte-identical to the snapshot taken before them')
}

console.log(`\n${fail === 0 ? 'ALL PASS' : 'FAILURES'}: ${pass} PASS, ${fail} FAIL (base ${new URL(creds.baseUrl).origin}, today ${today})`)
process.exit(fail === 0 ? 0 : 1)
