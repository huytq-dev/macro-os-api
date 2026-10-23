# Estimator study (single round) — production vs prototypes, synthetic ground truth

Worlds: calibration 120 (S01, S02, S04, S06, S09, S10 × seeds 1–20); evaluation 360 (18 scenario variants × seeds 101–120; S18 has no data). Errors are measured from day 28.

## 1. Calibration (parameters chosen on calibration worlds only, objective = MAE)

| estimator | selected parameters | calibration MAE (kcal) | grid size |
|---|---|---|---|
| Production | β 0.1, W 21, trend N 20, 14-d freeze | 71.9 | – |
| ProdTuned (production structure, tuned β/W) | β 0.035, W 14 | 62.2 | 56 |
| Kalman (1 model) | σT 32, σV 1.2, σI 900, σmiss 400, σT₀ 300 | 70.4 | 80 |
| IMM (2 models) | σT 8/64, σV 2, stay 0.97, σI 900 (from Kalman) | 70.1 | 144 |
| RobustSlope (Theil–Sen on raw weights + EMA) | W 70, β 0.2 | 77.9 | 20 |

Calibration MAE of the best 5 settings per family:

| family | setting → MAE |
|---|---|
| Kalman | σT 32, σV 1.2, σI 900 → 70.4; σT 32, σV 1.2, σI 600 → 70.5; σT 32, σV 2, σI 400 → 70.5; σT 32, σV 2, σI 600 → 70.6; σT 32, σV 2, σI 250 → 70.6 |
| IMM | σT 8/64, σV 2, stay 0.97 → 70.1; σT 4/64, σV 2, stay 0.97 → 70.2; σT 2/64, σV 2, stay 0.97 → 70.2; σT 1/64, σV 2, stay 0.97 → 70.2; σT 1/128, σV 2, stay 0.99 → 70.4 |
| RobustSlope | W 70, β 0.2 → 77.9; W 56, β 0.2 → 78.6; W 56, β 0.1 → 79.6; W 42, β 0.05 → 80.4; W 70, β 0.1 → 81.0 |
| ProdTuned | β 0.035, W 7 → 62.1; β 0.035, W 10 → 62.2; β 0.035, W 14 → 62.2; β 0.05, W 14 → 63.1; β 0.05, W 10 → 63.6 |

## 2. Accuracy on evaluation worlds (MAE / RMSE of the TDEE estimate vs true TDEE, kcal; mean over worlds)

| group (scenarios) | Production | ProdTuned | Kalman | IMM | RobustSlope |
|---|---|---|---|---|---|
| steady truth (S01, S13–S17) | 61.7 / 75.0 | 39.1 / 48.2 | 43.2 / 54.9 | 40.6 / 51.4 | 47.4 / 61.8 |
| confounded scale (S06 water, S07 glycogen, S08 whoosh) | 131.4 / 158.4 | 75.7 / 90.9 | 85.7 / 103.9 | 80.1 / 95.7 | 85.0 / 103.0 |
| data gaps (S09, S10) | 65.5 / 79.2 | 44.6 / 52.5 | 50.6 / 62.4 | 48.5 / 59.3 | 52.1 / 65.3 |
| truth changes (S02–S05, S11) | 93.3 / 123.9 | 113.6 / 143.5 | 128.2 / 159.1 | 131.3 / 163.0 | 141.1 / 173.0 |
| start-up, TDEE₀ −12 % (S12a/b) | 58.5 / 72.1 | 61.6 / 79.9 | 46.3 / 58.6 | 45.0 / 58.7 | 39.4 / 51.6 |
| ALL 18 scenario variants | 82.2 / 102.6 | 69.0 / 85.8 | 75.1 / 93.3 | 73.7 / 91.5 | 79.3 / 98.8 |

### Paired comparison with production over all evaluation worlds (ΔMAE = estimator − production; negative = better; 95 % bootstrap CI over worlds)

| estimator | mean ΔMAE | 95 % CI | worlds better than production |
|---|---|---|---|
| ProdTuned | -13.2 | [-16.5, -9.9] | 68 % |
| Kalman | -7.1 | [-10.7, -3.5] | 67 % |
| IMM | -8.4 | [-12.3, -4.5] | 67 % |
| RobustSlope | -2.9 | [-7.0, 1.2] | 63 % |

## 3. Forecast error — persistence forecast: |estimate at day d − true TDEE at day d+h|, h = 7 / 14 / 28 days (kcal)

| set | h | Production | ProdTuned | Kalman | IMM | RobustSlope |
|---|---|---|---|---|---|---|
| all | 7 | 88.9 | 76.5 | 82.6 | 81.4 | 87.3 |
| all | 14 | 96.1 | 84.5 | 90.7 | 89.6 | 95.9 |
| all | 28 | 113.3 | 102.5 | 108.4 | 107.7 | 113.7 |
| truth changes | 7 | 115.9 | 138.7 | 153.4 | 156.9 | 166.7 |
| truth changes | 14 | 140.3 | 165.5 | 180.5 | 184.2 | 194.3 |
| truth changes | 28 | 195.3 | 225.2 | 239.1 | 243.7 | 253.8 |

## 4. Recovery after a true step (days after the event; "half" = covered half of the step, "±100" = within 100 kcal of the new truth for 7 consecutive days; mean over seeds reaching it)

| scenario | measure | Production | ProdTuned | Kalman | IMM | RobustSlope |
|---|---|---|---|---|---|---|
| S04 | half | 17.1 (20/20) | 25.3 (20/20) | 31.9 (20/20) | 35.5 (20/20) | 42.6 (20/20) |
| S04 | ±100 | 26.7 (20/20) | 51.2 (20/20) | 55.3 (20/20) | 56.7 (20/20) | 59.4 (20/20) |
| S05 | half | 17.1 (20/20) | 29.2 (20/20) | 33.1 (20/20) | 35.6 (20/20) | 41.0 (20/20) |
| S05 | ±100 | 27.0 (20/20) | 46.2 (20/20) | 50.3 (20/20) | 51.0 (20/20) | 55.1 (20/20) |
| S11 | half | 18.1 (20/20) | 26.1 (20/20) | 33.7 (20/20) | 41.0 (20/20) | 39.2 (20/20) |
| S11 | ±100 | 18.1 (20/20) | 26.1 (20/20) | 39.0 (20/20) | 43.3 (20/20) | 39.2 (20/20) |

## 5. False change detection and stability (scenarios whose TRUE expenditure is constant: S01, S06–S10, S12–S17)

False change = a day where |estimate(d) − estimate(d−14)| exceeds δ while the truth did not move. Stability = mean |day-to-day change| of the estimate.

| measure | Production | ProdTuned | Kalman | IMM | RobustSlope |
|---|---|---|---|---|---|
| false change δ = 100 kcal (share of days) | 34.2 % | 12.7 % | 17.4 % | 12.8 % | 11.6 % |
| false change δ = 150 kcal | 17.1 % | 4.8 % | 7.3 % | 5.4 % | 3.8 % |
| stability: mean |Δ| per day (kcal) | 8.11 | 4.69 | 9.21 | 7.09 | 4.56 |

## 6. Initial vs learned TDEE, and Holding

| measure | Production | ProdTuned | Kalman | IMM | RobustSlope |
|---|---|---|---|---|---|---|
| S12a/b first 28 days: mean |error| (TDEE₀ is −12 % ≈ −300 kcal) | 237.3 | 265.8 | 226.2 | 244.7 | 237.0 |
| S12a/b days 28+: mean |error| | 58.5 | 61.6 | 46.3 | 45.0 | 39.4 |
| MAE on days production is Holding (S09, S10, S11) | 110.4 | 85.3 | 90.0 | 89.7 | 95.5 |

## 7. MAE per scenario (kcal, mean over seeds)

| scenario | Production | ProdTuned | Kalman | IMM | RobustSlope |
|---|---|---|---|---|---|
| S01 Stable 2500 | 56.1 | 36.1 | 41.5 | 38.2 | 44.4 |
| S02 Ramp 2400→2800 | 89.0 | 101.6 | 113.1 | 115.9 | 134.9 |
| S03 Ramp 2800→2300 | 91.1 | 147.5 | 162.6 | 166.1 | 165.3 |
| S04 Step up 2400→2800 | 103.7 | 122.2 | 137.3 | 142.1 | 154.6 |
| S05 Step down 2800→2400 | 103.0 | 124.7 | 139.0 | 141.4 | 152.4 |
| S06 Water retention | 80.1 | 50.5 | 58.5 | 55.4 | 57.9 |
| S07 Glycogen shifts | 147.9 | 77.9 | 89.8 | 80.0 | 66.3 |
| S08 Plateau + whoosh | 166.3 | 98.7 | 108.9 | 104.9 | 130.9 |
| S09 Missing logs | 62.5 | 43.7 | 55.9 | 54.0 | 50.4 |
| S10 Long weigh-in gap | 68.4 | 45.5 | 45.4 | 43.0 | 53.8 |
| S11 Holding → Updating | 79.9 | 71.9 | 89.0 | 90.9 | 98.2 |
| S12a Calibrating → Updating (frozen 14d) | 53.5 | 60.4 | 42.7 | 42.0 | 35.1 |
| S12b Calibrating → Updating (learn from day 1) | 63.5 | 62.7 | 49.8 | 48.1 | 43.7 |
| S13 Static mode (overlay không nối) | 56.1 | 36.1 | 41.5 | 38.2 | 44.4 |
| S14 Goal change | 70.1 | 44.8 | 45.7 | 43.5 | 51.2 |
| S15 Program change, no TDEE reset | 56.1 | 36.1 | 41.5 | 38.2 | 44.4 |
| S16 Backdated log | 59.8 | 36.8 | 44.8 | 40.4 | 45.4 |
| S17 Sparse weigh-ins (weekly) | 72.3 | 44.6 | 43.9 | 44.9 | 54.3 |

## 8. Evidence for the Flux re-assessment

**Identifiability of the Kalman noise parameters:** 9 of 80 grid settings are within 1 kcal of the best calibration MAE (70.4); calibration MAE ranges 70.4–118.9 over the whole grid. A flat objective means the data cannot pin σT / σV / σI down.

**Kalman posterior sd of the TDEE vs the TRUE change magnitude** (pooled over all evaluation worlds, days ≥ 28):

| truth window | Spearman ρ(sd, true change) |
|---|---|
| 14 d | 0.073 |
| 28 d | -0.012 |
| 42 d | -0.084 |

Mean posterior sd: 201.2 kcal on days where the truth is stable, 196.4 kcal where it is changing (a principled uncertainty that barely depends on whether the truth moved).
