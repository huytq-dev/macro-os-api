// D8 audit reproduction (isolated IT environment ONLY): GoalSupport.EnsureActiveGoalAsync creates a Goal whenever the user has an active program but NO Active goal.
// There is no API that completes/abandons a goal without making a new one (CreateProgram supersedes AND creates), so "no Active goal" is simulated the way a future
// Complete would leave it: UPDATE Goals SET Status='Achieved' on the throwaway LocalDB. Each of the 4 callers is triggered from that state; everything is reverted at the end.
// Usage: node BE/tests/Integration/goal-d8-repro.mjs   (needs seed.mjs + a program: POST /api/programs once)
import { loadCreds, login, raw, localdb } from './lib.mjs'

const creds = loadCreds()
const token = await login(creds.email, creds.password)
const uid = localdb(`SET NOCOUNT ON; SELECT CONVERT(varchar(36), Id) FROM Users WHERE Email = '${creds.email}'`)
const today = (await raw('GET', '/api/nutrients/macros/series', { token })).body.data.to
const rows = () => localdb(`SET NOCOUNT ON; SELECT LEFT(CONVERT(varchar(36), Id), 8) + ' ' + Status + ' start=' + CONVERT(varchar(10), StartDate) + ' end=' + ISNULL(CONVERT(varchar(10), EndDate), '-') + ' startKg=' + ISNULL(CONVERT(varchar(10), StartWeightKg), '-') + ' target=' + ISNULL(CONVERT(varchar(10), TargetWeightKg), '-') FROM Goals WHERE UserId = '${uid}' ORDER BY CreatedAt`).split(/\r?\n/).filter(Boolean)
const ids = () => localdb(`SET NOCOUNT ON; SELECT CONVERT(varchar(36), Id) FROM Goals WHERE UserId = '${uid}'`).split(/\r?\n/).filter(Boolean)
const count = (status) => Number(localdb(`SET NOCOUNT ON; SELECT COUNT(*) FROM Goals WHERE UserId = '${uid}'${status ? ` AND Status = '${status}'` : ''}`))
const programGoal = () => localdb(`SET NOCOUNT ON; SELECT LEFT(CONVERT(varchar(36), GoalId), 8) FROM MacroPrograms WHERE UserId = '${uid}' AND IsActive = 1`)
const card = async () => (await raw('GET', '/api/programs/current', { token })).body.data.targetWeightKg
const complete = () => localdb(`UPDATE Goals SET Status = 'Achieved', EndDate = '${today}' WHERE UserId = '${uid}' AND Status = 'Active'`)

const original = ids()
const originalProgramGoal = localdb(`SET NOCOUNT ON; SELECT CONVERT(varchar(36), GoalId) FROM MacroPrograms WHERE UserId = '${uid}' AND IsActive = 1`)
console.log(`BASELINE goals=${original.length}, active=${count('Active')}, program.GoalId=${originalProgramGoal.slice(0, 8)}`)
rows().forEach((r) => console.log('  ', r))

const triggers = [
  ['1 GET  /api/goals/progress   GetGoalProgressQueryHandler.cs:10', () => raw('GET', '/api/goals/progress', { token })],
  ['2 GET  /api/goals/history    GetGoalHistoryQueryHandler.cs:10', () => raw('GET', '/api/goals/history', { token })],
  ['3 PUT  /api/goals/edit       EditGoalCommandHandler.cs:14', () => raw('PUT', '/api/goals/edit', { token, body: { targetWeightKg: 77, weeklyRatePercent: 0.5 } })],
  ['4 PUT  /api/goals/target     UpdateGoalTargetCommandHandler.cs:8', () => raw('PUT', '/api/goals/target', { token, body: { targetWeightKg: 77 } })],
]
for (const [name, call] of triggers) {
  complete()
  const before = { total: count(), active: count('Active'), pg: programGoal(), card: await card() }
  const r = await call()
  const after = { total: count(), active: count('Active'), pg: programGoal(), card: await card() }
  console.log(`\nTRIGGER ${name}\n  state before: goals=${before.total} active=${before.active} program.GoalId=${before.pg} strategy-card target=${before.card}\n  http ${r.status}\n  state after : goals=${after.total} active=${after.active} program.GoalId=${after.pg} strategy-card target=${after.card}\n  => ${after.total > before.total ? 'NEW GOAL ROW CREATED' : 'no new row'}${name.includes('GET') ? ' BY A GET REQUEST' : ''}`)
  console.log('  rows:'); rows().forEach((x) => console.log('    ', x))
}

// history order after a resurrection (what the FE Goal History list shows)
const hist = (await raw('GET', '/api/goals/history', { token })).body.data
console.log('\nGET /goals/history (order the FE renders):')
hist.forEach((h) => console.log(`   ${h.id.slice(0, 8)} ${h.status.padEnd(9)} start=${h.startDate} end=${h.endDate ?? '-'} target=${h.targetWeightKg}`))

// concurrency: no unique index on active goals => parallel reads create several Active goals
complete()
const beforeRace = count()
await Promise.all([1, 2, 3, 4, 5].map(() => raw('GET', '/api/goals/history', { token })))
console.log(`\nCONCURRENCY from "no Active goal": 5 parallel GET /goals/history => goals ${beforeRace} -> ${count()}, ACTIVE goals now = ${count('Active')} (schema allows >1: only a non-unique index (UserId, Status))`)

// revert everything this script did (throwaway DB)
localdb(`UPDATE MacroPrograms SET GoalId = '${originalProgramGoal}' WHERE UserId = '${uid}' AND IsActive = 1`)
for (const id of ids().filter((i) => !original.includes(i))) localdb(`DELETE FROM Goals WHERE Id = '${id}'`)
localdb(`UPDATE Goals SET Status = 'Active', EndDate = NULL, TargetWeightKg = 78 WHERE Id = '${original[0]}'`)
localdb(`UPDATE MacroProgramWeeks SET AdjustmentReason = 'Initial' WHERE AdjustmentReason = 'Manual' AND MacroProgramId IN (SELECT Id FROM MacroPrograms WHERE UserId = '${uid}' AND IsActive = 1)`) // PUT /goals/edit re-stamps the week as Manual
console.log(`\nREVERTED: goals=${count()}, active=${count('Active')}, program.GoalId=${programGoal()}`)
