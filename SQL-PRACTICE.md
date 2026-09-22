# SQL & EF Core Practice — Verus LLC Domain

Practice material built on the expanded domain: **Companies → Contacts / Orders**. Every query
here has actually been run against the real seeded database (SQL Server in Docker, ~5,000
Companies / ~25,000 Contacts / ~75,000 Orders) — counts and execution plans quoted below are real,
not invented.

**How to use this file:** try to write the query yourself first, then compare. Don't just read the
answers.

## Schema recap

```
Company (Id, Name, WebsiteUrl)
   │
   ├──< Contact (Id, CompanyId, FirstName, LastName, Email, Phone, JobTitle, IsActive, CreatedAt)
   │
   └──< Order (Id, CompanyId, OrderNumber, Amount, Status, CreatedAt, UpdatedAt)
```

Indexes in play:
- `Companies (Name, Id)`
- `Contacts (CompanyId, IsActive)`, `Contacts (Email)` UNIQUE
- `Orders (CompanyId, CreatedAt)`, `Orders (Status, CreatedAt)`, `Orders (OrderNumber)` UNIQUE

---

# Basic SQL

## 1. Contacts by Company

**Problem:** Get all Contacts belonging to a specific Company.

```sql
SELECT Id, FirstName, LastName, Email, JobTitle, IsActive
FROM Contacts
WHERE CompanyId = @CompanyId;
```

**Explanation:** A straight `WHERE` on the foreign key column. This is the single most common
access pattern for a child table — "give me everything that belongs to this parent."

**Concepts tested:** `WHERE`, foreign key filtering.

**Index:** `IX_Contacts_CompanyId_IsActive` covers this — `CompanyId` is the left-most column, so
SQL Server can seek on it even though this particular query doesn't filter on `IsActive` at all.

---

## 2. Orders by Company

**Problem:** Get all Orders belonging to a specific Company.

```sql
SELECT Id, OrderNumber, Amount, Status, CreatedAt
FROM Orders
WHERE CompanyId = @CompanyId;
```

**Explanation:** Same pattern as #1. **Concepts tested:** `WHERE`, foreign key filtering.

**Index:** `IX_Orders_CompanyId_CreatedAt` — again, `CompanyId` is left-most, so this seeks cleanly
even without touching `CreatedAt`.

---

## 3. Completed Orders

**Problem:** Get all Orders where `Status = 'Completed'`.

```sql
SELECT Id, CompanyId, OrderNumber, Amount, CreatedAt
FROM Orders
WHERE Status = 'Completed';
```

**Explanation:** `Status` is stored as text (see the EF Core section below for why), so this reads
naturally. **Concepts tested:** `WHERE` on a non-key column.

**Index:** `IX_Orders_Status_CreatedAt` — `Status` is left-most, so this seeks directly to the
`'Completed'` rows instead of scanning the table. In the real seeded data, ~45,000 of ~75,000
orders are `Completed` — a fairly unselective filter (roughly 60% of the table), which is worth
noting: an index seek that still has to read 60% of a table isn't saving much over a scan. Indexes
help most when the filter is *selective* (few matching rows), which `Status` alone is not — this is
exactly why it's paired with `CreatedAt` rather than indexed by itself.

---

## 4. Recent Orders

**Problem:** Get Orders created during the last 30 days.

```sql
SELECT Id, CompanyId, OrderNumber, Amount, Status
FROM Orders
WHERE CreatedAt >= DATEADD(DAY, -30, GETUTCDATE());
```

**Concepts tested:** `WHERE` with a computed date boundary.

**Index consideration:** see Query D in the Index Practice section below — filtering by `CreatedAt`
alone does **not** seek efficiently against `IX_Orders_CompanyId_CreatedAt`, because `CreatedAt`
isn't the left-most column of that index.

---

## 5. High Value Orders

**Problem:** Get Orders whose Amount is greater than a specific value.

```sql
SELECT Id, CompanyId, OrderNumber, Amount
FROM Orders
WHERE Amount > 10000;
```

**Concepts tested:** `WHERE` on a non-indexed column.

**Index consideration:** there's no index on `Amount` at all, so this is always a scan — every one
of the ~75,000 order rows gets read and checked. That's a deliberate choice: `Amount` isn't a
column anything currently filters or sorts by as its primary access pattern, so an index on it
would be pure write/storage overhead with no real payoff. Not every column deserves an index.

---

## 6. Latest Orders

**Problem:** Sort Orders by `CreatedAt DESC`.

```sql
SELECT TOP 50 Id, CompanyId, OrderNumber, Amount, CreatedAt
FROM Orders
ORDER BY CreatedAt DESC;
```

**Why sorting gets expensive:** without a useful index, SQL Server has to read every row and sort
the *entire* result set before it can hand you the top 50 — on 75,000 rows that's a real, measurable
cost, and it only grows as the table grows. An index whose key starts with the sort column (or is
already the sort order you need) lets SQL Server read rows that are already in order and stop as
soon as it has enough — no separate Sort operator at all.

Note this particular query has no `CompanyId` filter, so `IX_Orders_CompanyId_CreatedAt` doesn't
help here (same left-most-prefix issue as Query D) — this one would still scan-and-sort with the
current indexes. That's fine: this codebase doesn't have a "browse all orders across every company,
newest first" screen, so there's no index built to serve it. If that became a real feature, a
standalone `CreatedAt` index (or one with `CreatedAt` left-most) would be the fix.

---

### 💬 Interview question

**Q: Why might an index make `ORDER BY` faster?**
A: Without an index, the database has to read all the matching rows into memory and sort them
before returning anything. If an index already stores the rows in (or starting with) the order you
asked for, the database can just read them off the index in sequence — no separate sort step, and
it can stop early if you only asked for the first N rows.

---

# Intermediate SQL

## 7. Companies with Contact count

**Problem:** Return `Company, NumberOfContacts` for every company.

```sql
SELECT c.Name, COUNT(ct.Id) AS NumberOfContacts
FROM Companies c
LEFT JOIN Contacts ct ON ct.CompanyId = c.Id
GROUP BY c.Id, c.Name
ORDER BY NumberOfContacts DESC;
```

**Explanation:** `LEFT JOIN` (not `INNER JOIN`) so companies with zero contacts still show up with
a count of 0, instead of disappearing from the results. `COUNT(ct.Id)` (not `COUNT(*)`) so a
company with no matching contact rows counts as 0, not 1 — `COUNT(*)` would count the single NULL
row the LEFT JOIN produces.

**Concepts tested:** `LEFT JOIN`, `GROUP BY`, `COUNT`.

---

## 8. Companies with Order count

**Problem:** Return `Company, NumberOfOrders`.

```sql
SELECT c.Name, COUNT(o.Id) AS NumberOfOrders
FROM Companies c
LEFT JOIN Orders o ON o.CompanyId = c.Id
GROUP BY c.Id, c.Name
ORDER BY NumberOfOrders DESC;
```

Same reasoning as #7. **Concepts tested:** `LEFT JOIN`, `GROUP BY`, `COUNT`.

---

## 9. Total sales by Company

**Problem:** Calculate `SUM(Order.Amount)` grouped by Company.

```sql
SELECT c.Name, SUM(o.Amount) AS TotalSales
FROM Companies c
JOIN Orders o ON o.CompanyId = c.Id
GROUP BY c.Id, c.Name
ORDER BY TotalSales DESC;
```

**Explanation:** `INNER JOIN` here (not `LEFT JOIN`) is a reasonable choice *if* you only want
companies that actually have sales — a company with zero orders contributes nothing to a sum
anyway. If you wanted every company listed, including 0 for no orders, you'd `LEFT JOIN` and wrap
in `ISNULL(SUM(o.Amount), 0)`.

**Concepts tested:** `JOIN`, `GROUP BY`, `SUM`.

---

## 10. Top Companies by sales

**Problem:** Return Companies ordered by total Order Amount, top 10.

```sql
SELECT TOP 10 c.Name, SUM(o.Amount) AS TotalSales
FROM Companies c
JOIN Orders o ON o.CompanyId = c.Id
GROUP BY c.Id, c.Name
ORDER BY TotalSales DESC;
```

Verified against the real seeded data — actual top 3:

```
Beacon & Clearview Textiles Inc   489,784.67
Summit & Orbit Automotive         488,012.78
Nimbus Manufacturing Group        476,526.13
```

**Concepts tested:** `JOIN`, `GROUP BY`, `SUM`, `ORDER BY`, `TOP`.

---

## 11. Companies without Orders

**Problem:** Find companies that have never placed an order. Provide **two** solutions.

**Solution A — `LEFT JOIN` + `IS NULL`:**

```sql
SELECT c.Id, c.Name
FROM Companies c
LEFT JOIN Orders o ON o.CompanyId = c.Id
WHERE o.Id IS NULL;
```

**Solution B — `NOT EXISTS`:**

```sql
SELECT c.Id, c.Name
FROM Companies c
WHERE NOT EXISTS (
  SELECT 1 FROM Orders o WHERE o.CompanyId = c.Id
);
```

Both verified to return the same 155 companies against the real seeded data.

**Explanation:** Solution A finds every company row, joins in matching orders, and keeps only the
rows where the join found nothing (`o.Id IS NULL`). Solution B asks the more direct question —
"does at least one order exist for this company?" — and never actually joins/materializes any
Order columns; it just needs to know whether a matching row exists at all, and can stop looking the
moment it finds one.

**Which is faster?** Don't assume. In practice, on SQL Server, `NOT EXISTS` and `LEFT JOIN ... IS
NULL` very often compile down to the **same execution plan** (an Anti Semi Join) — the query
optimizer is good at recognizing these are logically equivalent. The right way to know which is
faster for a *specific* query on *your* data is to actually look at the execution plan, not to
assume one syntax is always better. `NOT EXISTS` is usually preferred anyway because it more
directly expresses the intent ("I only care whether this exists"), which makes it easier to read
and harder to get subtly wrong (e.g. forgetting `IS NULL` and accidentally getting an inner join).

**Concepts tested:** `LEFT JOIN`, `NOT EXISTS`, semantic equivalence, execution plans over assumed
performance.

---

## 12. Companies without active Contacts

**Problem:** Find companies that have no *active* contacts (either zero contacts at all, or only
inactive ones).

```sql
SELECT c.Id, c.Name
FROM Companies c
WHERE NOT EXISTS (
  SELECT 1 FROM Contacts ct WHERE ct.CompanyId = c.Id AND ct.IsActive = 1
);
```

**Explanation:** Note the extra `AND ct.IsActive = 1` inside the subquery, compared to #11 — this
is what correctly captures "no active contact," not just "no contact." Verified against the real
data: 454 companies have zero contacts, and a further 117 have contacts but every one of them is
inactive — this query correctly returns 571 total (454 + 117).

**Concepts tested:** `NOT EXISTS` with an additional predicate inside the subquery.

**Index:** `IX_Contacts_CompanyId_IsActive` is built for exactly this shape of query — it can
navigate straight to `(CompanyId, IsActive = 1)` without touching inactive rows or unrelated
companies.

---

## 13. Average Order amount by Status

**Problem:** Average Order amount, grouped by Status.

```sql
SELECT Status, AVG(Amount) AS AverageAmount, COUNT(*) AS OrderCount
FROM Orders
GROUP BY Status
ORDER BY AverageAmount DESC;
```

**Concepts tested:** `GROUP BY`, `AVG`.

**Index:** `IX_Orders_Status_CreatedAt` covers the `GROUP BY Status` (left-most column), though
since this reads every row's `Amount` (not in the index) to average it, SQL Server will still need
to visit the base table/clustered index for that — see the Key Lookup section for exactly this
situation.

---

## 14. Companies with more than X Orders

**Problem:** Companies with more than 20 Orders.

```sql
SELECT c.Name, COUNT(o.Id) AS OrderCount
FROM Companies c
JOIN Orders o ON o.CompanyId = c.Id
GROUP BY c.Id, c.Name
HAVING COUNT(o.Id) > 20
ORDER BY OrderCount DESC;
```

**Concepts tested:** `GROUP BY`, `HAVING`.

### 💬 Interview question

**Q: What's the difference between `WHERE` and `HAVING`?**
A: `WHERE` filters individual rows *before* grouping happens. `HAVING` filters the *groups*
produced by `GROUP BY`, after aggregation. You can't write `WHERE COUNT(o.Id) > 20` because at the
point `WHERE` runs, no grouping/aggregation has happened yet — `COUNT(o.Id)` doesn't exist yet.

---

# Advanced SQL

## 15. Latest Order for each Company

**Problem:** Get the single most recent Order for every Company.

```sql
WITH RankedOrders AS (
  SELECT o.*,
         ROW_NUMBER() OVER (PARTITION BY o.CompanyId ORDER BY o.CreatedAt DESC) AS rn
  FROM Orders o
)
SELECT CompanyId, Id, OrderNumber, Amount, Status, CreatedAt
FROM RankedOrders
WHERE rn = 1;
```

Verified — runs correctly against the real data and returns exactly one row per company that has
at least one order.

**Explanation:** `PARTITION BY CompanyId` tells SQL Server "restart the numbering for every new
CompanyId" — instead of one global ranking across all 75,000 orders, you get an *independent*
`ROW_NUMBER()` sequence per company, each starting back at 1. `ORDER BY CreatedAt DESC` within that
partition means row number 1 is always the newest order for that specific company. Filtering
`WHERE rn = 1` then keeps just that one row per group.

**Concepts tested:** CTE, `ROW_NUMBER()`, `PARTITION BY`.

---

## 16. Top 3 Orders for each Company

**Problem:** Get the 3 highest-value Orders per Company.

```sql
WITH RankedOrders AS (
  SELECT o.*,
         ROW_NUMBER() OVER (PARTITION BY o.CompanyId ORDER BY o.Amount DESC) AS rn
  FROM Orders o
)
SELECT CompanyId, Id, OrderNumber, Amount
FROM RankedOrders
WHERE rn <= 3;
```

**`ROW_NUMBER()` vs `RANK()` vs `DENSE_RANK()`** — the difference only shows up when there are ties.
Say a company's order amounts, sorted descending, are `500, 500, 300, 300, 100`:

| Amount | ROW_NUMBER() | RANK() | DENSE_RANK() |
|--------|-------------|--------|--------------|
| 500    | 1           | 1      | 1            |
| 500    | 2           | 1      | 1            |
| 300    | 3           | 3      | 2            |
| 300    | 4           | 3      | 2            |
| 100    | 5           | 5      | 3            |

- `ROW_NUMBER()` always gives out unique, sequential numbers — ties get arbitrarily (but
  deterministically, given the same ORDER BY) split apart. Good for "give me exactly N rows."
- `RANK()` gives ties the same number, then **skips** the numbers that would've been used (1, 1, 3
  — no 2). Matches how sports rankings usually work ("two people tied for 1st, next is 3rd").
- `DENSE_RANK()` gives ties the same number too, but **doesn't skip** (1, 1, 2). Matches "how many
  distinct amount tiers exist."

For "top 3 orders," `ROW_NUMBER()` is usually what you actually want (exactly 3 rows, full stop).
If two orders are tied for 3rd place and you want *both* of them included, `RANK() <= 3` would give
you 4 rows instead of 3 in that case — a genuinely different, and sometimes more correct, answer.

**Concepts tested:** window functions, ties, `ROW_NUMBER` vs `RANK` vs `DENSE_RANK`.

---

## 17. Pagination

**Problem:** Page through Orders sorted by newest first.

```sql
DECLARE @PageNumber INT = 3;
DECLARE @PageSize INT = 20;

SELECT Id, CompanyId, OrderNumber, Amount, CreatedAt
FROM Orders
ORDER BY CreatedAt DESC
OFFSET (@PageNumber - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;
```

**EF Core equivalent** (this is literally what the Companies listing endpoint already does):

```csharp
var items = await _dbContext.Orders
    .OrderByDescending(o => o.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

`.Skip()` compiles to `OFFSET`, `.Take()` compiles to `FETCH NEXT ... ROWS ONLY`.

**Why `ORDER BY` is mandatory here:** `OFFSET`/`FETCH` only make sense against a *defined* order —
without `ORDER BY`, SQL Server has no guaranteed row order to skip through, so which rows land on
which page would be undefined and could even change between two runs of the identical query.

**The large-OFFSET problem:** `OFFSET 500000 ROWS` still has to internally walk past those 500,000
rows before it can return anything — offset pagination gets linearly slower the deeper you page.
For genuinely huge datasets or infinite-scroll-style access, **keyset pagination** ("give me rows
where `CreatedAt < @lastSeenCreatedAt`, ordered the same way, top N") avoids that entirely, because
it's a direct index seek to a known starting point instead of a count-and-skip. That's a bigger
change than this practice project needs — just know it exists as the answer when OFFSET starts
showing up in a slow query log.

**Concepts tested:** `OFFSET`/`FETCH`, determinism, `.Skip()`/`.Take()`.

---

## 18. Company Sales Ranking

**Problem:** Rank Companies by total Order Amount.

```sql
WITH CompanySales AS (
  SELECT CompanyId, SUM(Amount) AS TotalSales
  FROM Orders
  GROUP BY CompanyId
)
SELECT c.Name, cs.TotalSales,
       RANK() OVER (ORDER BY cs.TotalSales DESC) AS SalesRank
FROM CompanySales cs
JOIN Companies c ON c.Id = cs.CompanyId
ORDER BY cs.TotalSales DESC;
```

Verified — top 3 matches Query #10 exactly, as it should (same underlying aggregation, just with a
rank column attached).

**`GROUP BY` vs Window Functions:** `GROUP BY` *collapses* rows — one output row per group, and you
lose the individual rows that went into it. A window function (`RANK() OVER (...)`) does the
opposite: it keeps every row and just attaches a computed value (a rank, a running total, a
previous-row value, ...) alongside it. That's exactly why this query uses `GROUP BY` *inside* the
CTE first (to collapse Orders down to one row per company) and a window function *outside* it (to
rank those already-collapsed rows) — they're solving two different problems and often combine well
like this.

**Concepts tested:** CTE, `GROUP BY`, `SUM`, `RANK()`.

---

# Query Variations

## Companies that have Orders

**Solution A — `INNER JOIN`:**

```sql
SELECT DISTINCT c.Id, c.Name
FROM Companies c
JOIN Orders o ON o.CompanyId = c.Id;
```

**Solution B — `EXISTS`:**

```sql
SELECT c.Id, c.Name
FROM Companies c
WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.CompanyId = c.Id);
```

Both are correct, but they express different intent. Solution A is a genuine join — it's the
natural building block if you were about to also pull in *order* columns. Solution B never touches
individual order rows at all; it's a pure existence check, which is exactly what "companies that
have orders" is actually asking. Note Solution A needs `DISTINCT` (a company with 30 orders would
otherwise appear 30 times); Solution B never has that problem, since `EXISTS` only ever returns
true/false per company row.

## Companies without Orders

Covered in full above as exercise #11 (`LEFT JOIN + IS NULL` vs `NOT EXISTS`).

## Latest Order per Company

**Window function version:** exercise #15 above.

**Alternative — correlated subquery:**

```sql
SELECT c.Id, c.Name, o.Id AS LatestOrderId, o.CreatedAt
FROM Companies c
JOIN Orders o ON o.CompanyId = c.Id
WHERE o.CreatedAt = (
  SELECT MAX(o2.CreatedAt) FROM Orders o2 WHERE o2.CompanyId = c.Id
);
```

Both work. The window function version is usually easier to reason about once you're comfortable
with it — it's one clear step ("rank orders within each company, keep rank 1") rather than a query
nested inside a query. The correlated subquery version also has a subtle bug risk the window
version doesn't: if two orders for the same company share the exact same `CreatedAt`, this version
returns *both* of them (the `WHERE` matches on equality), while the `ROW_NUMBER()` version
deterministically picks exactly one.

---

# Index Practice

For each query below: what index (if any) can SQL Server use, and is it a Seek or a Scan?

## Query A

```sql
SELECT * FROM Orders WHERE CompanyId = @CompanyId;
```

**Question:** Which index, Seek or Scan?

**Answer:** `IX_Orders_CompanyId_CreatedAt` — **Index Seek**, since `CompanyId` is the left-most
column of that index. SQL Server navigates straight to the matching `CompanyId` value instead of
reading the whole table.

---

## Query B

```sql
SELECT Id, OrderNumber, CreatedAt
FROM Orders
WHERE CompanyId = @CompanyId
ORDER BY CreatedAt DESC;
```

**Question:** Does the composite index help beyond just the filter?

**Answer — verified real execution plan** against the seeded data:

```
Index Seek(OBJECT:([Orders].[IX_Orders_CompanyId_CreatedAt]),
           SEEK:([Orders].[CompanyId]={guid'...'}) ORDERED BACKWARD)
Clustered Index Seek(OBJECT:([Orders].[PK_Orders]), ... LOOKUP ORDERED FORWARD)
```

Yes — notably, there's **no separate Sort operator** anywhere in this plan. Because
`IX_Orders_CompanyId_CreatedAt` stores rows for the same company already ordered by `CreatedAt`,
SQL Server reads them "ORDERED BACKWARD" (i.e. it just walks the index in reverse for `DESC`)
instead of reading them in any order and then sorting. This is the single biggest practical reason
composite `(FilterColumn, SortColumn)` indexes are worth building. (The `Clustered Index Seek ...
LOOKUP` you see is a Key Lookup, fetching `OrderNumber` since it isn't in the index — see the Key
Lookup section below.)

---

## Query C

```sql
SELECT Id, CompanyId, Amount
FROM Orders
WHERE Status = 'Completed'
ORDER BY CreatedAt DESC;
```

**Question:** Does `(Status, CreatedAt)` help here?

**Answer:** Yes, the same way as Query B — `Status` is the left-most column (used in the filter),
and `CreatedAt` (the sort column) is already the index's second column, so this can seek to
`Status = 'Completed'` and read those rows pre-sorted by `CreatedAt`, again with no separate Sort.

---

## Query D

```sql
SELECT Id, CompanyId, CreatedAt
FROM Orders
WHERE CreatedAt >= DATEADD(DAY, -30, GETUTCDATE());
```

**Question:** Given `IX_Orders_CompanyId_CreatedAt` exists, does this seek?

**Answer — verified real execution plan:**

```
Index Scan(OBJECT:([Orders].[IX_Orders_CompanyId_CreatedAt]),
           WHERE:([Orders].[CreatedAt]>=dateadd(day,(-30),getutcdate())))
```

**No** — this is a **Scan**, not a Seek, even though the column being filtered on *is* part of the
index. This is the **left-most prefix rule**: a composite index on `(CompanyId, CreatedAt)` is
physically sorted by `CompanyId` first, and *within* each `CompanyId`, by `CreatedAt`. Without a
`CompanyId` value to anchor on, SQL Server has no way to jump directly to "`CreatedAt >= X`" — that
condition is scattered all over the index, once per company. So it falls back to reading the whole
index start-to-finish and checking every row (still cheaper than scanning the full table, since the
index is narrower, but not a seek). If "browse recent orders across all companies" became a real
query pattern, a standalone index with `CreatedAt` left-most (or first) would fix this.

---

## Query E

```sql
SELECT Id, FirstName, LastName
FROM Contacts
WHERE CompanyId = @CompanyId AND IsActive = 1;
```

**Question:** How does `(CompanyId, IsActive)` help, given `IsActive` alone is a weak index
candidate?

**Answer:** `IsActive` is a boolean — only two possible values, so an index on `IsActive` alone is
nearly useless (SQL Server would still have to read roughly half the table either way; that's the
textbook definition of low *cardinality* = poor standalone index candidate). But as the *second*
column, after `CompanyId`, it's genuinely useful: SQL Server first narrows down to this one
company's rows (a handful, not the whole table) using `CompanyId`, and *within* that small set,
`IsActive` lets it further narrow to just the active ones without reading the inactive rows for
that company at all. The column only needs to be selective *within the group the first column
already narrowed down to* — it doesn't need to be globally selective on its own.

### 💬 Interview question

**Q: Why does column order matter in a composite index?**
A: A composite index is physically one sorted structure — sorted by the first column, then by the
second column *within* each value of the first, and so on. SQL Server can only seek efficiently
using a *left-most prefix* of that order: it can use the first column alone, or the first + second
together, but it generally can't seek using the second column by itself, because that column's
values aren't contiguous in the index without first fixing the first column.

---

# Execution Plan Practice

## Reading a plan

Key things to look for:

- **Table Scan / Clustered Index Scan** — reads every row. Fine for small tables, a red flag on
  large ones if it's unexpected.
- **Index Scan** — reads every row of a (narrower) non-clustered index. Cheaper than a table scan,
  still reads everything.
- **Index Seek** — navigates directly to matching rows using the index's sorted structure. What you
  want for selective filters.
- **Key Lookup** (or `Clustered Index Seek ... LOOKUP`) — after finding matching rows via a
  non-clustered index, SQL Server has to go back to the clustered index (the actual table) to fetch
  columns that weren't included in that non-clustered index. One extra lookup *per matching row* —
  cheap for a handful of rows, expensive for thousands.
- **Sort** — an explicit, separate sorting step. Often avoidable with the right index (see Query B).
- **Estimated Rows vs Actual Rows** — the optimizer's guess vs. what actually came back. A big gap
  between them is a common cause of a bad plan (SQL Server picked a strategy suited to the wrong row
  count).
- **Query Cost** — SQL Server's relative cost estimate, used to compare plans/queries against each
  other. Not a real-world time measurement by itself.

## Before/after example

**Deliberately inefficient query** (for learning purposes only — this doesn't reflect anything
wrong with the real schema):

```sql
SELECT Id, CompanyId, Amount
FROM Orders
WHERE CAST(Amount AS varchar(20)) LIKE '15%';
```

1. **Why it's inefficient:** wrapping `Amount` in `CAST(...)` means SQL Server can't use an index on
   `Amount` even if one existed — the *column* being evaluated is really the *result of the
   function*, not the raw stored value, so nothing is sorted the way the optimizer would need. This
   is called a "non-sargable" predicate.
2. **What you'd see in the plan:** a full `Clustered Index Scan` (or `Table Scan`) — every row gets
   read and has the `CAST`/`LIKE` evaluated against it individually.
3. **What index could help:** none, really — not without restructuring the query. The real fix here
   isn't an index at all, it's rewriting the predicate to something sargable, e.g. `WHERE Amount >=
   15000 AND Amount < 16000` if that's actually the intent, which *could* then use an index on
   `Amount` if one existed.
4. **The general lesson:** applying a function to a column in a `WHERE` clause (`CAST`, `UPPER`,
   `DATEPART`, arithmetic, etc.) very often defeats indexing on that column, even if the index
   exists. Compare this to Query D above — that one *is* sargable (`CreatedAt >= @date`, no function
   wrapping the column), and it still couldn't seek, purely because of column order. Two different
   reasons to end up scanning.

---

# Key Lookup Practice

You already saw this in Query B: filtering `Orders` by `CompanyId` using
`IX_Orders_CompanyId_CreatedAt` correctly does an **Index Seek** — but then a second operator,
`Clustered Index Seek ... LOOKUP`, runs once per matching row to fetch `OrderNumber`, which isn't
stored in that index.

- **Index key columns** — `CompanyId, CreatedAt`. What the index is physically sorted by, and what
  it can seek on.
- **The problem** — the index only *contains* `CompanyId`, `CreatedAt`, and the clustered key
  (`Id`, tagging along automatically). Any other column you `SELECT` — `OrderNumber`, `Amount`,
  `Status` — isn't there, so SQL Server has to go fetch it from the actual table via a Key Lookup.

**A covering index example** (conceptual — this is a SQL practice exercise, not something added to
the real migration, since there's no proven need for it yet):

```sql
CREATE INDEX IX_Orders_CompanyId_Covering
ON Orders (CompanyId, CreatedAt)
INCLUDE (Amount, Status, OrderNumber);
```

- **`INCLUDE` columns** aren't part of the sortable/seekable key — they're just extra data
  physically stored at the leaf level of the index, purely so a query can read them "for free"
  without a trip back to the clustered index.
- **Covering index** — an index that contains (as key or `INCLUDE` columns) every column a
  particular query needs, so the query is satisfied entirely from the index, with zero Key Lookups.
- **Read performance** — noticeably better for queries that hit this exact shape often: one Index
  Seek, no per-row lookups.
- **The cost** — every `INCLUDE`d column is duplicated storage (bigger index, more disk), and every
  `INSERT`/`UPDATE`/`DELETE` on `Orders` now also has to maintain this wider index. `INCLUDE` is a
  targeted trade — you're deliberately paying write/storage cost to eliminate lookups for one
  specific, known-hot query pattern. Don't reach for it until you've actually seen the Key Lookup
  cost matter in a real plan.

---

# EF Core Practice

| SQL | EF Core LINQ |
|---|---|
| `WHERE` | `.Where(...)` |
| `ORDER BY` / `ORDER BY ... DESC` | `.OrderBy(...)` / `.OrderByDescending(...)` |
| `JOIN` | navigation properties (`.Include(...)`) or explicit `.Join(...)` |
| `GROUP BY` | `.GroupBy(...)` |
| `EXISTS` | `.Any(...)` |
| `COUNT` | `.Count()` / `.CountAsync()` |
| `SUM` | `.Sum(...)` |
| `OFFSET`/`FETCH` | `.Skip(...).Take(...)` |

**Orders by Company** (exercise #2):

```sql
SELECT * FROM Orders WHERE CompanyId = @CompanyId;
```
```csharp
var orders = await _dbContext.Orders
    .Where(o => o.CompanyId == companyId)
    .AsNoTracking()
    .ToListAsync();
```

**Companies with Order count** (exercise #8):

```sql
SELECT c.Name, COUNT(o.Id) AS NumberOfOrders
FROM Companies c LEFT JOIN Orders o ON o.CompanyId = c.Id
GROUP BY c.Id, c.Name;
```
```csharp
var result = await _dbContext.Companies
    .Select(c => new { c.Name, NumberOfOrders = c.Orders.Count })
    .AsNoTracking()
    .ToListAsync();
```
(EF Core translates `c.Orders.Count` on the navigation collection into the equivalent
`LEFT JOIN + COUNT` SQL for you — no explicit join needed in the LINQ.)

**Companies without Orders** (exercise #11, `NOT EXISTS` version):

```csharp
var companies = await _dbContext.Companies
    .Where(c => !c.Orders.Any())
    .AsNoTracking()
    .ToListAsync();
```

**Pagination** (exercise #17):

```csharp
var page = await _dbContext.Orders
    .OrderByDescending(o => o.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .AsNoTracking()
    .ToListAsync();
```

**Why `AsNoTracking()`:** by default, EF Core keeps a "change tracker" entry for every entity a
query returns, so it can later detect what you changed and generate `UPDATE` statements. For a
read-only query — which is most of what a reporting/search endpoint does — that tracking is pure
overhead: extra memory, extra CPU comparing snapshots, for a benefit (change detection) you'll never
use. `AsNoTracking()` skips all of that. Every read method already in this codebase
(`GetAllAsync`, `GetByIdAsync`, `GetPagedAsync`, `SearchCompaniesAsync`'s underlying fetch) already
does this — it's a good habit, not something special to Contact/Order queries.

---

# Include vs Projection

**Loading a Company with all its Orders — `.Include(...)`:**

```csharp
var company = await _dbContext.Companies
    .Include(c => c.Orders)
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == companyId);
```

This materializes a full `Company` object *and* a full `Order` object for every one of that
company's orders — every column of both tables comes back, whether you need it or not.

**The same information via projection — `.Select(...)`:**

```csharp
var summary = await _dbContext.Companies
    .Where(c => c.Id == companyId)
    .Select(c => new
    {
        c.Name,
        OrderCount = c.Orders.Count,
        TotalSales = c.Orders.Sum(o => o.Amount)
    })
    .FirstOrDefaultAsync();
```

**Why projection can be more efficient:** the generated SQL only ever selects `Name` and the two
aggregates — it never pulls back individual `Order` rows or columns at all (no `OrderNumber`,
`Status`, `CreatedAt`, ...). Less data over the wire, less work materializing C# objects, and often
a simpler underlying query plan (an aggregate instead of a row-by-row join). `.Include(...)` is the
right tool when you genuinely need the related *entities themselves* (e.g. you're about to edit one
of those orders); `.Select(...)` projection is the right tool when you only need specific *values*
out of the relationship, which is the more common case for read-only screens and reports.

---

# N+1 Query Problem

**The naive approach:**

```csharp
var companies = await _dbContext.Companies.AsNoTracking().ToListAsync();

foreach (var company in companies)
{
    var orders = await _dbContext.Orders
        .Where(o => o.CompanyId == company.Id)
        .AsNoTracking()
        .ToListAsync();

    // ... do something with company + orders
}
```

**Why this is bad:** 1 query to fetch the companies, then **1 more query per company** to fetch its
orders. For 5,000 companies, that's 5,001 round trips to the database instead of 1 or 2 — this is
the N+1 problem, and it's one of the most common real-world EF Core performance bugs, precisely
because each individual query looks completely reasonable in isolation.

**Fix 1 — `.Include(...)`,** if you actually need the Order entities:

```csharp
var companies = await _dbContext.Companies
    .Include(c => c.Orders)
    .AsNoTracking()
    .ToListAsync();
```

One query total (a `LEFT JOIN` under the hood), companies and their orders all come back together.

**Fix 2 — projection,** if you only need derived values:

```csharp
var summaries = await _dbContext.Companies
    .Select(c => new { c.Name, OrderCount = c.Orders.Count, TotalSales = c.Orders.Sum(o => o.Amount) })
    .AsNoTracking()
    .ToListAsync();
```

Also one query, and (per the section above) typically cheaper than `.Include(...)` since it never
materializes full `Order` rows at all.

**When to use which:** `.Include(...)` when the caller genuinely needs the related entities
themselves (e.g. an edit screen for a company's orders). Projection when the caller only needs
specific fields or aggregates (e.g. a company list showing an order count) — which, in practice, is
most read-only/reporting scenarios.

### 💬 Interview question

**Q: What causes an N+1 query problem in EF Core, and how do you spot it?**
A: It happens when you load a list of entities, then lazily or manually trigger a separate query
for each one's related data inside a loop. You'd spot it by looking at the actual SQL EF generates
(e.g. via logging, or `SET STATISTICS IO`/profiling) and seeing dozens or thousands of near-identical
queries instead of one — or just by noticing a loop in C# that calls `.ToListAsync()` (or accesses
a lazy-loaded navigation) on every iteration.

---

# SQL Profiling

```sql
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT Id, CompanyId, Amount FROM Orders WHERE CompanyId = @CompanyId;
```

- **`STATISTICS IO`** reports, per table touched, how many pages were read — most importantly
  **Logical Reads**: pages read from memory (the buffer cache). This is the number that matters most
  for tuning, because it's stable and directly reflects how much work the query does, independent of
  whether the data happened to be cached that particular time.
- **`STATISTICS TIME`** reports **CPU time** and **elapsed time** for parsing/compiling and executing
  the query.

**Comparing before/after an index:** run the same query with `STATISTICS IO` on, once before adding
an index and once after (e.g. drop and recreate one of the practice indexes above to see the
difference). A meaningful improvement shows up as a large drop in **logical reads**, not just a
faster wall-clock time.

**Why elapsed time alone is unreliable:** the first run of a query after SQL Server restarts (or
after the buffer cache is cleared) pays a real disk I/O cost; every run after that reads from
memory and looks artificially fast, cache effects, other queries competing for CPU/disk at the same
moment, and general system load. Logical reads don't change run to run for the same query/data —
that repeatability is exactly why it's the more trustworthy number to tune against.

---

# Interview Questions Recap

**Q: What's the difference between a normal index and a UNIQUE index?**
A: A normal index just speeds up lookups/sorts on that column — duplicate values are fine. A UNIQUE
index does that *and* enforces that no two rows can have the same value, at the database level. It's
the mechanism used here for `Contacts.Email` and `Orders.OrderNumber`.

**Q: Why shouldn't we create an index on every column?**
A: Every index is extra stored data that has to be kept in sync — every `INSERT`, `UPDATE` (of an
indexed column), and `DELETE` now has to update the index too, on top of the base table. More
indexes means more disk space and slower writes, for a benefit (faster reads) that only pays off if
something actually queries by that column in a selective way. Indexes are a trade-off, not a free
win — that's the whole reason this domain has exactly 5 new indexes, each backing a specific,
named query, instead of one on every column.

**Q: Why are low-cardinality columns like booleans usually poor standalone index candidates?**
A: An index only helps by letting the database skip large chunks of data it doesn't need. If a
column only has two possible values (like `IsActive`), roughly half the table matches either value
— an index there barely narrows anything down, so the database frequently just scans anyway. It
becomes useful as a *secondary* column in a composite index (see `(CompanyId, IsActive)`), where it
narrows down an already-small, single-company slice rather than the whole table.

**Q: What's a covering index?**
A: An index that contains every column a specific query needs — either as key columns or `INCLUDE`
columns — so the query can be satisfied entirely from the index, without a Key Lookup back to the
table.

---

# Final SQL Cheat Sheet

| Concept | What it is | When to use it |
|---|---|---|
| **INNER JOIN** | Combine rows from two tables where a match exists on both sides | You need columns from both tables and don't care about non-matches |
| **LEFT JOIN** | All rows from the left table, matched rows from the right (NULLs when no match) | You need every row from one table even if the other side has nothing — e.g. "companies with their order count, including 0" |
| **GROUP BY** | Collapse rows into groups, one output row per group | You need an aggregate (COUNT/SUM/AVG) per category |
| **HAVING** | Filter *groups*, after aggregation | "Companies with more than 20 orders" — can't be a WHERE, since COUNT doesn't exist until grouping happens |
| **EXISTS** | True/false — does at least one matching row exist | You only care *whether* related rows exist, not their values — e.g. "companies that have orders" |
| **NOT EXISTS** | The inverse — no matching row exists | "Companies without orders" — usually clearer intent than `LEFT JOIN ... IS NULL` |
| **CTE** (`WITH ... AS`) | A named, temporary result set scoped to one query | Breaking a complex query into readable steps, or a base for a window function |
| **ROW_NUMBER()** | Unique, sequential number per row within a partition | "Exactly the top N rows per group" |
| **RANK()** | Same as ROW_NUMBER but ties share a rank, and it skips numbers after a tie | Leaderboard-style ranking where ties should share a place |
| **DENSE_RANK()** | Like RANK but doesn't skip numbers after a tie | "How many distinct tiers/levels exist" |
| **PARTITION BY** | Restarts a window function's calculation for each group | "Latest order **per company**," not one global latest order |
| **OFFSET / FETCH** | Skip N rows, then take the next M | Page-based pagination — always pair with ORDER BY |
| **Index Seek** | Navigate directly to matching rows via the index's sorted structure | What you want for selective filters |
| **Index Scan** | Read every row of an index, checking each one | Happens when the filter can't use the index's left-most columns, or the filter isn't selective |
| **Table Scan** | Read every row of the actual table (no useful index at all) | Usually means "add an index," unless the table's genuinely tiny |
| **Key Lookup** | Extra trip back to the clustered index/table to fetch columns not in the non-clustered index used | Happens whenever you SELECT columns the index used for the seek doesn't contain |
| **Composite Index** | An index on more than one column, in a specific order | Filtering/sorting on more than one column together — column order = left-most-prefix rule |
| **Unique Index** | A composite/single-column index that also enforces no duplicates | Business identifiers that must be one-of-a-kind (email, order number) |
| **Covering Index** | An index containing every column a query needs (key + INCLUDE) | A specific, hot query pattern where Key Lookups are a measured problem |
| **INCLUDE** | Extra columns stored at an index's leaf level, not part of the seekable key | Turning a regular index into a covering index without widening the key itself |
| **AsNoTracking()** | Tells EF Core not to track returned entities for change detection | Any read-only query — which is most of them |
| **.Include(...)** | Eagerly load a related entity/collection in the same query | You need the related *entities themselves* |
| **Projection (`.Select(...)`)** | Shape the query to return only specific fields/aggregates | You only need certain values, not full related entities — usually cheaper |
| **N+1 Problem** | 1 query for a list + 1 more query per item for its related data | The bug to avoid — fix with `.Include(...)` or projection instead of a loop |
