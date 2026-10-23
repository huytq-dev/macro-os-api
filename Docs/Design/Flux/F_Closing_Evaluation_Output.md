# Closing evaluation — production vs β 0.035–0.07 with a 14-day window (fresh seeds 101–120)

Acceptance rule fixed before looking at the numbers: a candidate beats the baseline only if (1) overall MAE is lower with a bootstrap CI excluding 0, (2) false-change days drop, (3) half-step response ≤ 1.25 × production, (4) MAE when the true TDEE changes ≤ 1.15 × production.

## 1. MAE / RMSE (kcal)

| situation | split | Production (β 0.1, W 21) | β 0.07, W 14 | β 0.05, W 14 | β 0.035, W 14 |
|---|---|---|---|---|---|
| rapid rise (S11 step +200 through Holding) | HELD-OUT scenario | 79.9 / 104.5 | 76.1 / 99.6 | 72.7 / 95.7 | 71.9 / 92.7 |
| rapid fall (S05 step, S03 ramp) | HELD-OUT scenario | 97.0 / 129.5 | 97.0 / 129.2 | 109.3 / 143.9 | 136.1 / 170.2 |
| stable truth (S13–S17) | HELD-OUT scenario | 62.9 / 76.4 | 59.3 / 72.2 | 48.1 / 58.7 | 39.7 / 48.8 |
| water / glycogen / whoosh (S07, S08) | HELD-OUT scenario | 157.1 / 187.8 | 143.7 / 173.4 | 115.7 / 139.4 | 88.3 / 105.9 |
| start-up, TDEE₀ −12 % (S12a/b) | HELD-OUT scenario | 58.5 / 72.1 | 59.0 / 72.5 | 57.0 / 71.5 | 61.6 / 79.9 |
| ALL held-out scenarios | HELD-OUT scenario | 85.0 / 105.4 | 81.0 / 100.9 | 73.1 / 91.6 | 70.2 / 87.4 |
| rapid rise (S02 ramp, S04 step) — calibration scenarios, fresh seeds | fresh seeds | 96.3 / 128.1 | 93.6 / 124.0 | 99.6 / 130.8 | 111.9 / 142.1 |
| water retention (S06) — calibration scenario, fresh seeds | fresh seeds | 80.1 / 99.6 | 75.6 / 95.1 | 62.1 / 76.9 | 50.5 / 61.0 |
| missing logs / weigh-in gap (S09, S10) — calibration scenarios, fresh seeds | fresh seeds | 65.5 / 79.2 | 62.2 / 74.6 | 51.8 / 61.8 | 44.6 / 52.5 |

## 2. Responsiveness and recovery after a true step (days; mean over seeds)

| scenario | measure | Production (β 0.1, W 21) | β 0.07, W 14 | β 0.05, W 14 | β 0.035, W 14 |
|---|---|---|---|---|---|
| S04 (fresh seeds) | half-step | 17.1 (20/20) | 15.9 (20/20) | 19.3 (20/20) | 25.3 (20/20) |
| S04 | within ±100 kcal | 26.7 (20/20) | 27.3 (20/20) | 39.3 (20/20) | 51.2 (20/20) |
| S05 (held-out) | half-step | 17.1 (20/20) | 16.0 (20/20) | 20.4 (20/20) | 29.2 (20/20) |
| S05 | within ±100 kcal | 27.0 (20/20) | 29.8 (20/20) | 37.9 (20/20) | 46.2 (20/20) |
| S11 (held-out) | half-step | 18.1 (20/20) | 19.5 (20/20) | 21.8 (20/20) | 26.1 (20/20) |
| S11 | within ±100 kcal | 18.1 (20/20) | 19.5 (20/20) | 22.5 (20/20) | 26.1 (20/20) |

## 3. False change and stability (truth constant; held-out: S07, S08, S12a/b, S13–S17; plus fresh-seed S01, S06, S09, S10)

| measure | Production (β 0.1, W 21) | β 0.07, W 14 | β 0.05, W 14 | β 0.035, W 14 |
|---|---|---|---|---|
| false change ≥ 100 kcal / 14 d, held-out | 36.9 % | 36.6 % | 25.0 % | 15.1 % |
| false change ≥ 100 kcal / 14 d, fresh seeds | 28.0 % | 27.9 % | 16.3 % | 7.1 % |
| false change ≥ 150 kcal / 14 d, held-out | 19.6 % | 18.9 % | 11.7 % | 6.3 % |
| stability: mean |Δ|/day (kcal), held-out | 8.72 | 9.05 | 6.86 | 5.05 |

## 4. Acceptance rule applied (all 18 scenario variants for MAE; paired bootstrap vs production)

| candidate | (1) ΔMAE overall [95 % CI] | (2) false-change days (≥100) vs production | (3) half-step vs production (S04/S05/S11) | (4) MAE when truth changes vs production | verdict |
|---|---|---|---|---|---|
| β 0.07, W 14 | -3.7 [-4.4, -3.1] ✔ | 33.9 % vs 34.2 % ✔ | 17.1 d vs 17.4 d (×0.98) ✔ | 91.4 vs 93.3 (×0.98) ✔ | **beats baseline** |
| β 0.05, W 14 | -10.7 [-12.5, -9.0] ✔ | 22.3 % vs 34.2 % ✔ | 20.5 d vs 17.4 d (×1.18) ✔ | 98.1 vs 93.3 (×1.05) ✔ | **beats baseline** |
| β 0.035, W 14 | -13.2 [-16.6, -10.0] ✔ | 12.7 % vs 34.2 % ✔ | 26.9 d vs 17.4 d (×1.54) ✘ | 113.6 vs 93.3 (×1.22) ✘ | **does not beat baseline** |
