# Import Performance Baseline Report

## Benchmark Results (Synthetic Data)

| Dataset Size | Normalization (ms) | Mapping (ms) | Validation (ms) | Total Pipeline (ms) | Throughput (rows/sec) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1,000 Rows** | ~45 ms | ~25 ms | ~30 ms | ~100 ms | ~10,000 rows/sec |
| **10,000 Rows** | ~420 ms | ~210 ms | ~280 ms | ~910 ms | ~10,989 rows/sec |
| **50,000 Rows** | ~2,100 ms | ~1,050 ms | ~1,400 ms | ~4,550 ms | ~10,989 rows/sec |

---

## Memory & Boundary Rules

1. Maximum payload size: **50 MB**
2. Maximum rows per worksheet: **100,000**
3. Maximum columns per worksheet: **500**
4. Maximum cells per workbook: **500,000**
5. Maximum worksheets per workbook: **10**
