# Flux research run (MACROOS_FLUX_V1 candidate) — synthetic ground truth

- production estimator replicated with the production functions: window W=21, EMA β=0.1, trend EMA N=20, kcal/kg=7700, Calibrating freeze 14 d (default).
- calibration scenarios: S01, S02, S04, S06, S09, S10 × seeds 1–20; held-out: S03, S05, S07, S08, S11, S12a, S12b, S13, S14, S15, S16, S17, S18; validation seeds 101–120 (never used for calibration).

## 1. Plausibility screening (calibration scenarios only)

A candidate trajectory is *plausible* when an estimator with those settings would itself be acceptable: MAE vs ground truth ≤ κ × MAE of production (κ = 1.5, MACROOS POLICY, needs approval). Production (β 0.1, W 21) MAE = 71.9 kcal.

| axis | value | MAE (kcal) | MAE / production | plausible |
|---|---|---|---|---|
| β (W=21) | 0.025 | 68.9 | 0.96 | yes |
| β (W=21) | 0.035 | 64.2 | 0.89 | yes |
| β (W=21) | 0.05 | 64.5 | 0.90 | yes |
| β (W=21) | 0.07 | 67.4 | 0.94 | yes |
| β (W=21) | 0.1 | 71.9 | 1.00 | yes |
| β (W=21) | 0.14 | 76.6 | 1.06 | yes |
| β (W=21) | 0.2 | 81.2 | 1.13 | yes |
| β (W=21) | 0.3 | 85.1 | 1.18 | yes |
| W (β=0.1) | 7 | 82.9 | 1.15 | yes |
| W (β=0.1) | 10 | 79.5 | 1.10 | yes |
| W (β=0.1) | 14 | 75.6 | 1.05 | yes |
| W (β=0.1) | 21 | 71.9 | 1.00 | yes |
| W (β=0.1) | 28 | 72.0 | 1.00 | yes |
| W (β=0.1) | 35 | 72.7 | 1.01 | yes |
| W (β=0.1) | 42 | 75.4 | 1.05 | yes |
| W (β=0.1) | 56 | 82.1 | 1.14 | yes |

Plausible slow β: 0.025, 0.035, 0.05, 0.07; fast β: 0.14, 0.2, 0.3; short W: 7, 10, 14; long W: 28, 35, 42, 56.

## 2. Calibration (objective: the band width must rank-track the TRUE change magnitude; scale-free, so it does not tune the band to look like any screenshot)

### C1 responsiveness envelope (slow/fast β): top 5 of 12 by Spearman(width, true change magnitude), calibration only

| combination | Spearman ρ | mean width | width in stable truth | width when truth changes |
|---|---|---|---|---|
| β 0.05/0.14 | 0.103 | 47.0 | 46.4 | 52.2 |
| β 0.07/0.14 | 0.102 | 31.6 | 31.1 | 39.0 |
| β 0.05/0.2 | 0.101 | 59.6 | 58.8 | 71.7 |
| β 0.05/0.3 | 0.094 | 71.2 | 70.2 | 92.1 |
| β 0.07/0.2 | 0.091 | 44.8 | 44.1 | 58.9 |

**Selected C1 responsiveness envelope (slow/fast β): β 0.07/0.14** (ρ 0.102; ties within 0.005 resolved to the narrower band).

### C2 short/long window divergence: top 5 of 12 by Spearman(width, true change magnitude), calibration only

| combination | Spearman ρ | mean width | width in stable truth | width when truth changes |
|---|---|---|---|---|
| W 7/56 | 0.158 | 113.9 | 110.5 | 174.8 |
| W 10/56 | 0.157 | 103.1 | 100.1 | 145.2 |
| W 14/56 | 0.155 | 89.2 | 86.5 | 114.2 |
| W 7/42 | 0.098 | 102.9 | 101.0 | 153.8 |
| W 10/42 | 0.097 | 91.6 | 90.0 | 124.3 |

**Selected C2 short/long window divergence: W 14/56** (ρ 0.155; ties within 0.005 resolved to the narrower band).

### C3 combined envelope: top 5 of 144 by Spearman(width, true change magnitude), calibration only

| combination | Spearman ρ | mean width | width in stable truth | width when truth changes |
|---|---|---|---|---|
| β 0.07/0.14 + W 7/56 | 0.155 | 119.6 | 116.4 | 177.6 |
| β 0.07/0.14 + W 10/56 | 0.154 | 108.7 | 105.8 | 148.3 |
| β 0.07/0.14 + W 14/56 | 0.152 | 94.8 | 92.2 | 117.8 |
| β 0.05/0.14 + W 7/56 | 0.152 | 122.2 | 119.2 | 178.0 |
| β 0.07/0.2 + W 7/56 | 0.152 | 123.5 | 120.3 | 179.2 |

**Selected C3 combined envelope: β 0.07/0.14 + W 14/56** (ρ 0.152; ties within 0.005 resolved to the narrower band).

### C4 motion envelope (range of the learned TDEE over the last H days): calibration only

| H (days) | Spearman ρ | mean width | width in stable truth | width when truth changes |
|---|---|---|---|---|
| 7 | 0.042 | 47.0 | 46.6 | 61.5 |
| 10 | 0.044 | 64.6 | 64.1 | 78.2 |
| 14 | 0.045 | 87.6 | 87.0 | 98.4 |
| 21 | 0.060 | 123.4 | 122.9 | 131.9 |
| 28 | 0.074 | 151.4 | 150.5 | 157.8 |
| 42 | 0.055 | 197.0 | 196.3 | 186.4 |

**Selected C4: H = 28 days** (ρ 0.074).

### C5 net-displacement band (|learned TDEE(t) − learned TDEE(t−H)|): calibration only

| H (days) | Spearman ρ | mean width | width in stable truth | width when truth changes |
|---|---|---|---|---|
| 7 | 0.046 | 44.9 | 44.4 | 59.7 |
| 14 | 0.053 | 78.2 | 77.4 | 88.7 |
| 21 | 0.075 | 104.0 | 103.2 | 109.7 |
| 28 | 0.101 | 118.7 | 117.5 | 127.0 |
| 42 | 0.050 | 143.1 | 142.9 | 107.4 |

**Selected C5: H = 28 days** (ρ 0.101).

### Sensitivity of the plausibility bound κ (calibration only)

| κ | plausible β | plausible W | C3 selection |
|---|---|---|---|
| 1.25 | 0.025,0.035,0.05,0.07,0.1,0.14,0.2,0.3 | 7,10,14,21,28,35,42,56 | β 0.07/0.14 + W 7/56 (ρ 0.155, mean width 119.6) |
| 1.5 | 0.025,0.035,0.05,0.07,0.1,0.14,0.2,0.3 | 7,10,14,21,28,35,42,56 | β 0.07/0.14 + W 7/56 (ρ 0.155, mean width 119.6) |
| 2 | 0.025,0.035,0.05,0.07,0.1,0.14,0.2,0.3 | 7,10,14,21,28,35,42,56 | β 0.07/0.14 + W 7/56 (ρ 0.155, mean width 119.6) |

"Expanded" threshold τ = 1.5 × median stable-truth width on S01 (calibration seeds): L0 legacy ±37 111.0 kcal; C1 β-envelope 31.5 kcal; C2 W-divergence 91.5 kcal; C3 combined 97.5 kcal; C4 motion 174.0 kcal; C5 net displacement 115.5 kcal.

## 3. Validation on HELD-OUT scenarios (fresh seeds, parameters frozen)

### L0 legacy ±37 (τ 111.0 kcal) — mean over 20 seeds

| scenario | split | learned MAE | width mean | width stable | width change | false expansion | onset lag (d) | recovery (d) | flicker/day | truth in band % |
|---|---|---|---|---|---|---|---|---|---|---|
| S01 Stable 2500 | calibration(fresh seeds) | 56.1 | 74.0 | 74.0 | - | 0% | - | - | 13.2 | 40 |
| S02 Ramp 2400→2800 | calibration(fresh seeds) | 89.0 | 74.0 | 74.0 | - | 0% | - | - | 13.6 | 22 |
| S03 Ramp 2800→2300 | HELD-OUT | 91.1 | 74.0 | 74.0 | - | 0% | - | - | 13.5 | 30 |
| S04 Step up 2400→2800 | calibration(fresh seeds) | 103.7 | 74.0 | 74.0 | 74.0 | 0% | - | - | 14.9 | 31 |
| S05 Step down 2800→2400 | HELD-OUT | 103.0 | 74.0 | 74.0 | 74.0 | 0% | - | - | 14.7 | 30 |
| S06 Water retention | calibration(fresh seeds) | 80.1 | 74.0 | 74.0 | - | 0% | - | - | 17.1 | 32 |
| S07 Glycogen shifts | HELD-OUT | 147.9 | 74.0 | 74.0 | - | 0% | - | - | 31.9 | 14 |
| S08 Plateau + whoosh | HELD-OUT | 166.3 | 74.0 | 74.0 | - | 0% | - | - | 26.0 | 14 |
| S09 Missing logs | calibration(fresh seeds) | 62.5 | 74.0 | 74.0 | - | 0% | - | - | 12.0 | 39 |
| S10 Long weigh-in gap | calibration(fresh seeds) | 68.4 | 74.0 | 74.0 | - | 0% | - | - | 11.6 | 32 |
| S11 Holding → Updating | HELD-OUT | 79.9 | 74.0 | 74.0 | 74.0 | 0% | - | - | 13.4 | 33 |
| S12a Calibrating → Updating (frozen 14d) | HELD-OUT | 53.5 | 74.0 | 74.0 | - | 0% | - | - | 12.6 | 45 |
| S12b Calibrating → Updating (learn from day 1) | HELD-OUT | 63.5 | 74.0 | 74.0 | - | 0% | - | - | 12.9 | 35 |
| S13 Static mode (overlay không nối) | HELD-OUT | 56.1 | 74.0 | 74.0 | - | 0% | - | - | 13.2 | 40 |
| S14 Goal change | HELD-OUT | 70.1 | 74.0 | 74.0 | - | 0% | - | - | 14.2 | 37 |
| S15 Program change, no TDEE reset | HELD-OUT | 56.1 | 74.0 | 74.0 | - | 0% | - | - | 13.2 | 40 |
| S16 Backdated log | HELD-OUT | 59.8 | 74.0 | 74.0 | - | 0% | - | - | 13.4 | 37 |
| S17 Sparse weigh-ins (weekly) | HELD-OUT | 72.3 | 74.0 | 74.0 | - | 0% | - | - | 19.5 | 32 |
| S18 No valid observations | HELD-OUT | - | 74.0 | 74.0 | - | 0% | - | - | 0.0 | 0 |

### C1 β-envelope (τ 31.5 kcal) — mean over 20 seeds

| scenario | split | learned MAE | width mean | width stable | width change | false expansion | onset lag (d) | recovery (d) | flicker/day | truth in band % |
|---|---|---|---|---|---|---|---|---|---|---|
| S01 Stable 2500 | calibration(fresh seeds) | 56.1 | 28.0 | 28.0 | - | 37% | - | - | 13.2 | 13 |
| S02 Ramp 2400→2800 | calibration(fresh seeds) | 89.0 | 35.1 | 33.1 | - | 46% | 8.4 (20/20) | 32.9 (20/20) | 13.7 | 10 |
| S03 Ramp 2800→2300 | HELD-OUT | 91.1 | 39.5 | 35.6 | - | 46% | 2.8 (20/20) | 23.2 (20/20) | 13.7 | 13 |
| S04 Step up 2400→2800 | calibration(fresh seeds) | 103.7 | 39.0 | 39.3 | 35.8 | 50% | 6.5 (20/20) | 38.3 (20/20) | 15.0 | 15 |
| S05 Step down 2800→2400 | HELD-OUT | 103.0 | 37.9 | 38.7 | 30.8 | 49% | 7.5 (20/20) | 37.3 (18/20) | 14.8 | 12 |
| S06 Water retention | calibration(fresh seeds) | 80.1 | 36.8 | 36.8 | - | 49% | - | - | 17.2 | 13 |
| S07 Glycogen shifts | HELD-OUT | 147.9 | 70.5 | 70.5 | - | 76% | - | - | 31.8 | 10 |
| S08 Plateau + whoosh | HELD-OUT | 166.3 | 63.1 | 63.1 | - | 65% | - | - | 26.0 | 10 |
| S09 Missing logs | calibration(fresh seeds) | 62.5 | 31.5 | 31.5 | - | 40% | - | - | 12.1 | 13 |
| S10 Long weigh-in gap | calibration(fresh seeds) | 68.4 | 29.7 | 29.7 | - | 40% | - | - | 11.7 | 10 |
| S11 Holding → Updating | HELD-OUT | 79.9 | 31.7 | 31.6 | 32.5 | 43% | 5.7 (20/20) | 35.3 (20/20) | 13.4 | 13 |
| S12a Calibrating → Updating (frozen 14d) | HELD-OUT | 53.5 | 29.8 | 29.8 | - | 40% | - | - | 12.8 | 17 |
| S12b Calibrating → Updating (learn from day 1) | HELD-OUT | 63.5 | 30.5 | 30.5 | - | 42% | - | - | 13.0 | 12 |
| S13 Static mode (overlay không nối) | HELD-OUT | 56.1 | 28.0 | 28.0 | - | 37% | - | - | 13.2 | 13 |
| S14 Goal change | HELD-OUT | 70.1 | 31.5 | 31.5 | - | 44% | - | - | 14.2 | 14 |
| S15 Program change, no TDEE reset | HELD-OUT | 56.1 | 28.0 | 28.0 | - | 37% | - | - | 13.2 | 13 |
| S16 Backdated log | HELD-OUT | 59.8 | 28.6 | 28.6 | - | 38% | - | - | 13.5 | 12 |
| S17 Sparse weigh-ins (weekly) | HELD-OUT | 72.3 | 35.9 | 35.9 | - | 46% | - | - | 19.6 | 11 |
| S18 No valid observations | HELD-OUT | - | - | - | - | -% | - | - | - | - |

### C2 W-divergence (τ 91.5 kcal) — mean over 20 seeds

| scenario | split | learned MAE | width mean | width stable | width change | false expansion | onset lag (d) | recovery (d) | flicker/day | truth in band % |
|---|---|---|---|---|---|---|---|---|---|---|
| S01 Stable 2500 | calibration(fresh seeds) | 56.1 | 76.4 | 76.4 | - | 34% | - | - | 11.1 | 48 |
| S02 Ramp 2400→2800 | calibration(fresh seeds) | 89.0 | 106.6 | 94.8 | - | 46% | 13.7 (20/20) | 52.2 (20/20) | 13.0 | 27 |
| S03 Ramp 2800→2300 | HELD-OUT | 91.1 | 109.6 | 102.3 | - | 47% | 13.7 (20/20) | 41.4 (20/20) | 13.6 | 29 |
| S04 Step up 2400→2800 | calibration(fresh seeds) | 103.7 | 114.0 | 115.7 | 100.4 | 54% | 6.3 (20/20) | 46.9 (20/20) | 13.5 | 32 |
| S05 Step down 2800→2400 | HELD-OUT | 103.0 | 114.2 | 117.0 | 90.8 | 58% | 4.7 (20/20) | 40.5 (20/20) | 13.4 | 34 |
| S06 Water retention | calibration(fresh seeds) | 80.1 | 96.2 | 96.2 | - | 46% | - | - | 14.7 | 44 |
| S07 Glycogen shifts | HELD-OUT | 147.9 | 173.2 | 173.2 | - | 82% | - | - | 22.7 | 43 |
| S08 Plateau + whoosh | HELD-OUT | 166.3 | 172.5 | 172.5 | - | 70% | - | - | 21.4 | 34 |
| S09 Missing logs | calibration(fresh seeds) | 62.5 | 82.5 | 82.5 | - | 35% | - | - | 10.4 | 44 |
| S10 Long weigh-in gap | calibration(fresh seeds) | 68.4 | 88.3 | 88.3 | - | 38% | - | - | 10.3 | 42 |
| S11 Holding → Updating | HELD-OUT | 79.9 | 90.6 | 89.9 | 96.6 | 43% | 8.0 (20/20) | 40.7 (20/20) | 12.0 | 33 |
| S12a Calibrating → Updating (frozen 14d) | HELD-OUT | 53.5 | 70.6 | 70.6 | - | 24% | - | - | 11.2 | 44 |
| S12b Calibrating → Updating (learn from day 1) | HELD-OUT | 63.5 | 76.7 | 76.7 | - | 34% | - | - | 11.6 | 37 |
| S13 Static mode (overlay không nối) | HELD-OUT | 56.1 | 76.4 | 76.4 | - | 34% | - | - | 11.1 | 48 |
| S14 Goal change | HELD-OUT | 70.1 | 87.3 | 87.3 | - | 38% | - | - | 12.4 | 46 |
| S15 Program change, no TDEE reset | HELD-OUT | 56.1 | 76.4 | 76.4 | - | 34% | - | - | 11.1 | 48 |
| S16 Backdated log | HELD-OUT | 59.8 | 80.9 | 80.9 | - | 36% | - | - | 11.8 | 46 |
| S17 Sparse weigh-ins (weekly) | HELD-OUT | 72.3 | 91.3 | 91.3 | - | 42% | - | - | 16.2 | 43 |
| S18 No valid observations | HELD-OUT | - | - | - | - | -% | - | - | - | - |

### C3 combined (τ 97.5 kcal) — mean over 20 seeds

| scenario | split | learned MAE | width mean | width stable | width change | false expansion | onset lag (d) | recovery (d) | flicker/day | truth in band % |
|---|---|---|---|---|---|---|---|---|---|---|
| S01 Stable 2500 | calibration(fresh seeds) | 56.1 | 81.2 | 81.2 | - | 33% | - | - | 11.1 | 49 |
| S02 Ramp 2400→2800 | calibration(fresh seeds) | 89.0 | 110.7 | 99.1 | - | 45% | 13.7 (20/20) | 46.6 (20/20) | 13.2 | 28 |
| S03 Ramp 2800→2300 | HELD-OUT | 91.1 | 115.5 | 108.1 | - | 45% | 12.8 (20/20) | 44.5 (20/20) | 13.7 | 31 |
| S04 Step up 2400→2800 | calibration(fresh seeds) | 103.7 | 119.1 | 120.9 | 103.5 | 52% | 6.5 (20/20) | 46.2 (20/20) | 13.4 | 35 |
| S05 Step down 2800→2400 | HELD-OUT | 103.0 | 118.9 | 121.9 | 93.9 | 56% | 5.5 (20/20) | 41.0 (19/20) | 13.4 | 37 |
| S06 Water retention | calibration(fresh seeds) | 80.1 | 102.5 | 102.5 | - | 45% | - | - | 14.8 | 47 |
| S07 Glycogen shifts | HELD-OUT | 147.9 | 183.8 | 183.8 | - | 82% | - | - | 23.2 | 45 |
| S08 Plateau + whoosh | HELD-OUT | 166.3 | 181.4 | 181.4 | - | 70% | - | - | 21.9 | 35 |
| S09 Missing logs | calibration(fresh seeds) | 62.5 | 89.1 | 89.1 | - | 35% | - | - | 10.5 | 47 |
| S10 Long weigh-in gap | calibration(fresh seeds) | 68.4 | 94.6 | 94.6 | - | 38% | - | - | 10.4 | 43 |
| S11 Holding → Updating | HELD-OUT | 79.9 | 96.5 | 95.9 | 102.0 | 41% | 8.2 (20/20) | 40.5 (20/20) | 12.1 | 35 |
| S12a Calibrating → Updating (frozen 14d) | HELD-OUT | 53.5 | 75.9 | 75.9 | - | 23% | - | - | 11.3 | 46 |
| S12b Calibrating → Updating (learn from day 1) | HELD-OUT | 63.5 | 81.3 | 81.3 | - | 33% | - | - | 11.7 | 39 |
| S13 Static mode (overlay không nối) | HELD-OUT | 56.1 | 81.2 | 81.2 | - | 33% | - | - | 11.1 | 49 |
| S14 Goal change | HELD-OUT | 70.1 | 92.9 | 92.9 | - | 38% | - | - | 12.4 | 48 |
| S15 Program change, no TDEE reset | HELD-OUT | 56.1 | 81.2 | 81.2 | - | 33% | - | - | 11.1 | 49 |
| S16 Backdated log | HELD-OUT | 59.8 | 85.4 | 85.4 | - | 34% | - | - | 11.8 | 48 |
| S17 Sparse weigh-ins (weekly) | HELD-OUT | 72.3 | 97.6 | 97.6 | - | 43% | - | - | 16.7 | 45 |
| S18 No valid observations | HELD-OUT | - | - | - | - | -% | - | - | - | - |

### C4 motion (τ 174.0 kcal) — mean over 20 seeds

| scenario | split | learned MAE | width mean | width stable | width change | false expansion | onset lag (d) | recovery (d) | flicker/day | truth in band % |
|---|---|---|---|---|---|---|---|---|---|---|
| S01 Stable 2500 | calibration(fresh seeds) | 56.1 | 142.9 | 142.9 | - | 29% | - | - | 5.9 | 81 |
| S02 Ramp 2400→2800 | calibration(fresh seeds) | 89.0 | 164.9 | 155.9 | - | 37% | 25.5 (20/20) | 58.8 (19/20) | 7.7 | 34 |
| S03 Ramp 2800→2300 | HELD-OUT | 91.1 | 181.4 | 168.2 | - | 42% | 6.5 (20/20) | 31.8 (20/20) | 8.8 | 36 |
| S04 Step up 2400→2800 | calibration(fresh seeds) | 103.7 | 182.1 | 187.5 | 136.5 | 45% | 10.7 (20/20) | 40.2 (19/20) | 9.1 | 48 |
| S05 Step down 2800→2400 | HELD-OUT | 103.0 | 178.8 | 185.6 | 122.5 | 48% | 11.9 (20/20) | 39.3 (18/20) | 9.5 | 47 |
| S06 Water retention | calibration(fresh seeds) | 80.1 | 184.7 | 184.7 | - | 47% | - | - | 7.6 | 72 |
| S07 Glycogen shifts | HELD-OUT | 147.9 | 348.1 | 348.1 | - | 87% | - | - | 13.8 | 83 |
| S08 Plateau + whoosh | HELD-OUT | 166.3 | 311.7 | 311.7 | - | 77% | - | - | 16.2 | 53 |
| S09 Missing logs | calibration(fresh seeds) | 62.5 | 139.7 | 139.7 | - | 27% | - | - | 5.8 | 70 |
| S10 Long weigh-in gap | calibration(fresh seeds) | 68.4 | 139.8 | 139.8 | - | 29% | - | - | 6.0 | 66 |
| S11 Holding → Updating | HELD-OUT | 79.9 | 151.0 | 153.6 | 128.8 | 36% | 17.5 (20/20) | 36.9 (19/20) | 7.2 | 54 |
| S12a Calibrating → Updating (frozen 14d) | HELD-OUT | 53.5 | 143.3 | 143.3 | - | 29% | - | - | 6.7 | 73 |
| S12b Calibrating → Updating (learn from day 1) | HELD-OUT | 63.5 | 146.5 | 146.5 | - | 30% | - | - | 7.4 | 62 |
| S13 Static mode (overlay không nối) | HELD-OUT | 56.1 | 142.9 | 142.9 | - | 29% | - | - | 5.9 | 81 |
| S14 Goal change | HELD-OUT | 70.1 | 160.3 | 160.3 | - | 36% | - | - | 7.0 | 77 |
| S15 Program change, no TDEE reset | HELD-OUT | 56.1 | 142.9 | 142.9 | - | 29% | - | - | 5.9 | 81 |
| S16 Backdated log | HELD-OUT | 59.8 | 146.4 | 146.4 | - | 31% | - | - | 6.0 | 81 |
| S17 Sparse weigh-ins (weekly) | HELD-OUT | 72.3 | 183.6 | 183.6 | - | 47% | - | - | 7.2 | 80 |
| S18 No valid observations | HELD-OUT | - | - | - | - | -% | - | - | - | - |

### C5 net displacement (τ 115.5 kcal) — mean over 20 seeds

| scenario | split | learned MAE | width mean | width stable | width change | false expansion | onset lag (d) | recovery (d) | flicker/day | truth in band % |
|---|---|---|---|---|---|---|---|---|---|---|
| S01 Stable 2500 | calibration(fresh seeds) | 56.1 | 103.6 | 103.6 | - | 38% | - | - | 13.0 | 64 |
| S02 Ramp 2400→2800 | calibration(fresh seeds) | 89.0 | 136.1 | 130.8 | - | 50% | 9.9 (20/20) | 36.6 (20/20) | 12.6 | 31 |
| S03 Ramp 2800→2300 | HELD-OUT | 91.1 | 156.3 | 147.7 | - | 55% | 3.0 (20/20) | 29.1 (20/20) | 13.3 | 26 |
| S04 Step up 2400→2800 | calibration(fresh seeds) | 103.7 | 148.3 | 154.7 | 95.0 | 54% | 8.4 (20/20) | 37.2 (19/20) | 15.4 | 38 |
| S05 Step down 2800→2400 | HELD-OUT | 103.0 | 150.2 | 157.8 | 87.1 | 56% | 9.1 (20/20) | 41.2 (20/20) | 15.3 | 36 |
| S06 Water retention | calibration(fresh seeds) | 80.1 | 133.8 | 133.8 | - | 52% | - | - | 17.1 | 56 |
| S07 Glycogen shifts | HELD-OUT | 147.9 | 260.7 | 260.7 | - | 79% | - | - | 29.6 | 74 |
| S08 Plateau + whoosh | HELD-OUT | 166.3 | 257.1 | 257.1 | - | 76% | - | - | 26.3 | 46 |
| S09 Missing logs | calibration(fresh seeds) | 62.5 | 109.2 | 109.2 | - | 38% | - | - | 11.6 | 56 |
| S10 Long weigh-in gap | calibration(fresh seeds) | 68.4 | 107.4 | 107.4 | - | 39% | - | - | 11.2 | 52 |
| S11 Holding → Updating | HELD-OUT | 79.9 | 117.9 | 122.8 | 77.2 | 47% | 12.9 (20/20) | 38.4 (20/20) | 13.4 | 42 |
| S12a Calibrating → Updating (frozen 14d) | HELD-OUT | 53.5 | 111.9 | 111.9 | - | 39% | - | - | 12.5 | 54 |
| S12b Calibrating → Updating (learn from day 1) | HELD-OUT | 63.5 | 112.9 | 112.9 | - | 43% | - | - | 13.4 | 52 |
| S13 Static mode (overlay không nối) | HELD-OUT | 56.1 | 103.6 | 103.6 | - | 38% | - | - | 13.0 | 64 |
| S14 Goal change | HELD-OUT | 70.1 | 122.8 | 122.8 | - | 45% | - | - | 14.5 | 58 |
| S15 Program change, no TDEE reset | HELD-OUT | 56.1 | 103.6 | 103.6 | - | 38% | - | - | 13.0 | 64 |
| S16 Backdated log | HELD-OUT | 59.8 | 108.1 | 108.1 | - | 39% | - | - | 13.4 | 65 |
| S17 Sparse weigh-ins (weekly) | HELD-OUT | 72.3 | 116.1 | 116.1 | - | 42% | - | - | 18.5 | 55 |
| S18 No valid observations | HELD-OUT | - | - | - | - | -% | - | - | - | - |

### Learned estimator (production) responsiveness on step scenarios — days until the learned TDEE has covered half of the true step

| scenario | days to half-step (mean, seeds reaching it) |
|---|---|
| S02 Ramp 2400→2800 | 46.3 (20/20) |
| S03 Ramp 2800→2300 | 49.0 (20/20) |
| S04 Step up 2400→2800 | 17.1 (20/20) |
| S05 Step down 2800→2400 | 17.1 (20/20) |
| S11 Holding → Updating | 18.1 (20/20) |

### Pooled Spearman(width, true change magnitude) on held-out data (parameters were NOT fitted on it)

| set | held-out scenarios ρ | calibration scenarios, fresh seeds ρ | held-out mean width | held-out width in stable truth | held-out width when truth changes |
|---|---|---|---|---|---|
| L0 legacy ±37 | - | - | 74.0 | 74.0 | 74.0 |
| C1 β-envelope | 0.056 | 0.060 | 37.9 | 37.8 | 31.6 |
| C2 W-divergence | 0.062 | 0.121 | 101.6 | 101.1 | 93.7 |
| C3 combined | 0.066 | 0.113 | 107.6 | 107.1 | 97.9 |
| C4 motion | 0.010 | 0.058 | 186.4 | 187.2 | 125.7 |
| C5 net displacement | 0.030 | 0.070 | 143.4 | 143.6 | 82.1 |

### Sensitivity to how "true change magnitude" is windowed (held-out pooled Spearman; primary definition = 14 days, fixed before looking at results)

| set | truth window 14 d | 28 d | 42 d |
|---|---|---|---|
| L0 legacy ±37 | - | - | - |
| C1 β-envelope | 0.056 | 0.135 | 0.139 |
| C2 W-divergence | 0.062 | 0.150 | 0.187 |
| C3 combined | 0.066 | 0.154 | 0.188 |
| C4 motion | 0.010 | 0.047 | 0.089 |
| C5 net displacement | 0.030 | 0.081 | 0.132 |

### Noise sensitivity: is the weak rank-tracking a signal-to-noise limit? (calibration scenarios, parameters frozen from the realistic-noise calibration)

| noise scale | production learned MAE | C1 ρ | C2 ρ | C3 ρ | C4 ρ | C5 ρ |
|---|---|---|---|---|---|---|
| 0 | 38.3 | 0.352 | 0.404 | 0.398 | 0.307 | 0.281 |
| 0.25 | 44.6 | 0.285 | 0.396 | 0.390 | 0.270 | 0.260 |
| 0.5 | 53.9 | 0.198 | 0.370 | 0.365 | 0.202 | 0.205 |
| 1 | 76.1 | 0.106 | 0.290 | 0.285 | 0.092 | 0.111 |

## 4. Invariants and rules (C3 combined, validation seeds)

- PASS Lower ≤ learned TDEE ≤ Upper on every day of all 380 validation worlds
- PASS deterministic: two runs give identical bands
- PASS S12a Calibrating days (frozen 14 d, 14 days) have NO band; the band appears once the status is Updating
- PASS S12b (learn from day 1): Calibrating = no band (2 Calibrating days until the first Updating)
- PASS S18 no valid observations: every day null
- PASS S11 Holding: 11 consecutive Holding days have an UNCHANGED band (no new observation ⇒ no candidate update), state = Holding
- PASS causality (S04): the band at day d computed with data truncated at d equals the band computed with all later data (no future data used)
- PASS S13 Static mode: identical input ⇒ band identical to S01; Flux is a function of the LEARNED estimator only (a static value of 2300 does not enter it)
- PASS S15 program change without TDEE reset: program/goal are not inputs ⇒ band identical to S01
- PASS S16 backdated log (days 40–46 entered late): days before day 40 are untouched; 78 later days are recomputed (a derived rebuild, as production does)
- PASS S14 goal change (intake changes, TDEE does not): band does not blow up because of the intake change alone (false expansion within 25 pts of S01)

INVARIANTS: 11 PASS, 0 FAIL

Per-scenario day-by-day data (seed 101): `Docs\Design\Flux\E_data/<scenario>.csv` (true TDEE, learned TDEE + status, 4 candidate trajectories, Lower/Upper, band state).

## 5. Selected parameters (to be copied into FluxCalculationPolicy.V1 once approved)

```csharp
new FluxCandidateSpec("slow", 0.07m, 21),
new FluxCandidateSpec("fast", 0.14m, 21),
new FluxCandidateSpec("short", 0.1m, 14),
new FluxCandidateSpec("long", 0.1m, 56),
```
