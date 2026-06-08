
# SQL Database Setup Guide for NandanLabRawData Application

## 📋 Overview

This guide provides step-by-step instructions for setting up the SQL Server database for the Lab Data File Monitor application.

---

## 🗂️ SQL Script Files Included

| File | Purpose | Run Order |
|------|---------|-----------|
| `00_QuickSetup.sql` | All-in-one setup (tables + sample data) | **START HERE** |
| `01_CreateTables.sql` | Create tables, indexes, views, stored procedures | Alternative (manual) |
| `02_InsertSampleData.sql` | Insert sample test data | Optional |
| `03_QueryExamples.sql` | Example queries for analysis | Reference |
| `README.md` | Detailed documentation | Reference |

---

## 🚀 Quick Setup (5 Minutes)

### Option A: Fastest Setup (Recommended)

1. **Open SQL Server Management Studio (SSMS)**
2. **Connect to Server**
   - Server name: `103.191.208.18`
   - Authentication: SQL Server Authentication
   - Login: `developer`
   - Password: `p@wd`

3. **Run QuickSetup Script**
   - Open file: `SQL_SCRIPTS/00_QuickSetup.sql`
   - Press `F5` or click "Execute"
   - Wait for completion message

4. **Verify Success**
   - You should see:
     - ✅ AnalyzerReports table created
     - ✅ AnalyzerResults table created  
     - ✅ Sample data inserted
     - ✅ 3 reports with 7 results

5. **Start Application**
   - Run: `D:\Ankit\NandanLabRawData\dist\NandanLabRawData.exe`
   - Select folder to monitor
   - Click "Start Monitoring"

### Option B: Step-by-Step Setup

1. **Create Tables Only**
   - Open: `SQL_SCRIPTS/01_CreateTables.sql`
   - Execute
   - Wait for "Database Schema Created Successfully!" message

2. **(Optional) Add Sample Data**
   - Open: `SQL_SCRIPTS/02_InsertSampleData.sql`
   - Execute
   - Useful for testing

3. **Start Application**
   - Application will use existing tables
   - New data will be saved automatically

---

## 🔍 Verification Steps

### Step 1: Verify Tables Exist
```sql
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('AnalyzerReports', 'AnalyzerResults');
```
**Expected Result**: Two rows showing both tables

### Step 2: Verify Sample Data (if inserted)
```sql
SELECT COUNT(*) AS ReportCount FROM dbo.AnalyzerReports;
SELECT COUNT(*) AS ResultCount FROM dbo.AnalyzerResults;
```
**Expected Result**: 3 reports, 7 results

### Step 3: Test Stored Procedure
```sql
EXEC sp_GetDatabaseStatistics;
```
**Expected Result**: Statistics showing total reports and results

### Step 4: Check Indexes
```sql
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN ('AnalyzerReports', 'AnalyzerResults')
ORDER BY TableName, IndexName;
```
**Expected Result**: 7 indexes listed

---

## 📊 Database Schema

### Table 1: AnalyzerReports
Stores analyzer report headers and raw data

```
┌─────────────────────────────────────────────┐
│         AnalyzerReports                     │
├─────────────────────────────────────────────┤
│ Id (PK)            INT                      │
│ SampleId           NVARCHAR(100)            │
│ AnalysisDate       DATETIME2                │
│ ProcessedDate      DATETIME2 (DEFAULT NOW)  │
│ SourceFileName     NVARCHAR(255)            │
│ RawData            NVARCHAR(MAX)            │
│ CreatedDate        DATETIME2 (DEFAULT NOW)  │
│ UpdatedDate        DATETIME2                │
└─────────────────────────────────────────────┘
```

**Indexes**:
- IX_AnalyzerReports_SampleId
- IX_AnalyzerReports_ProcessedDate

### Table 2: AnalyzerResults
Stores individual test results (child of AnalyzerReports)

```
┌─────────────────────────────────────────────┐
│         AnalyzerResults                     │
├─────────────────────────────────────────────┤
│ Id (PK)            INT                      │
│ AnalyzerReportId (FK) INT → AnalyzerReports│
│ ParameterName      NVARCHAR(100)            │
│ Value              NVARCHAR(50)             │
│ Unit               NVARCHAR(50)             │
│ ReferenceRange     NVARCHAR(100)            │
│ Flag               NVARCHAR(10)             │
│ CreatedDate        DATETIME2 (DEFAULT NOW)  │
└─────────────────────────────────────────────┘
```

**Relationship**: One AnalyzerReport → Many AnalyzerResults (CASCADE DELETE)

**Indexes**:
- IX_AnalyzerResults_AnalyzerReportId
- IX_AnalyzerResults_ParameterName

---

## 🔧 Useful Queries

### Check Database Size
```sql
SELECT 
    SUM(size) * 8 / 1024 AS SizeInMB
FROM sys.database_files;
```

### List All Reports
```sql
SELECT ar.Id, ar.SampleId, ar.ProcessedDate, ar.SourceFileName
FROM dbo.AnalyzerReports ar
ORDER BY ar.ProcessedDate DESC;
```

### Find Abnormal Results
```sql
SELECT ar.SampleId, res.ParameterName, res.Value, res.Flag
FROM dbo.AnalyzerReports ar
INNER JOIN dbo.AnalyzerResults res ON ar.Id = res.AnalyzerReportId
WHERE res.Flag IN ('H', 'L')
ORDER BY ar.ProcessedDate DESC;
```

### Get Report Summary
```sql
SELECT 
    ar.SampleId,
    ar.ProcessedDate,
    COUNT(res.Id) AS ResultCount
FROM dbo.AnalyzerReports ar
LEFT JOIN dbo.AnalyzerResults res ON ar.Id = res.AnalyzerReportId
GROUP BY ar.SampleId, ar.ProcessedDate
ORDER BY ar.ProcessedDate DESC;
```

---

## ⚠️ Common Issues & Solutions

### Issue 1: "Login Failed"
**Cause**: Wrong credentials
**Solution**:
- Username: `developer`
- Password: `p@wd`
- Server: `103.191.208.18`

### Issue 2: "Cannot Connect to Server"
**Cause**: Network connectivity
**Solution**:
1. Test connection: `ping 103.191.208.18`
2. Check firewall (port 1433)
3. Verify VPN if needed

### Issue 3: "Database NandanLabDbDev Does Not Exist"
**Cause**: Database not created yet
**Solution**:
1. In SSMS Object Explorer, right-click "Databases"
2. Select "New Database"
3. Name: `NandanLabDbDev`
4. Click OK
5. Then run the setup scripts

### Issue 4: "Tables Already Exist"
**Cause**: Running script second time
**Solution**:
- Scripts include `DROP IF EXISTS`
- Safe to run multiple times
- Will replace existing tables (deletes data!)

### Issue 5: "Foreign Key Constraint Violated"
**Cause**: Wrong execution order
**Solution**:
- Always run AnalyzerReports before AnalyzerResults
- QuickSetup handles this automatically

---

## 🔐 Security Notes

### Current Setup (Development)
```
Server: 103.191.208.18
Database: NandanLabDbDev
User: developer
Password: p@wd (stored in code)
```

### For Production
⚠️ **Do NOT use in production as-is**

**Recommended changes**:
1. Create dedicated SQL user with limited permissions
2. Use Azure Key Vault for secrets
3. Enable encryption at rest
4. Enable SSL/TLS for network
5. Regular backups with encryption
6. Audit all data access

---

## 📈 Performance Tips

### 1. Regular Maintenance
```sql
-- Rebuild fragmented indexes (monthly)
DBCC DBREINDEX (AnalyzerReports);
DBCC DBREINDEX (AnalyzerResults);
```

### 2. Archive Old Data
```sql
-- Archive reports older than 1 year
-- (To archive table, not delete)
INSERT INTO AnalyzerReports_Archive
SELECT * FROM AnalyzerReports
WHERE ProcessedDate < DATEADD(YEAR, -1, GETDATE());

DELETE FROM AnalyzerReports
WHERE ProcessedDate < DATEADD(YEAR, -1, GETDATE());
```

### 3. Monitor Growth
```sql
-- Check table sizes
SELECT 
    t.NAME,
    SUM(u.total_pages) * 8 AS SizeKB
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats u ON t.object_id = u.object_id
GROUP BY t.NAME;
```

---

## 🔄 Application Integration

The application automatically:
1. ✅ Connects to database on startup
2. ✅ Creates tables if missing (EF Core migrations)
3. ✅ Saves parsed data to database
4. ✅ Updates existing records
5. ✅ Handles connection errors gracefully

**Configuration File**: `Configuration/DatabaseConfiguration.cs`

```csharp
public const string ConnectionString = 
    "Data Source=103.191.208.18; Initial Catalog=NandanLabDbDev; User ID=developer;Password=p@wd;";
```

---

## 📞 Support Checklist

- [ ] Can connect to SQL Server in SSMS
- [ ] Database `NandanLabDbDev` exists
- [ ] Tables created successfully
- [ ] Sample data inserted (if desired)
- [ ] Application connection string is correct
- [ ] Application shows "Database initialized" message
- [ ] First file processes and saves to database
- [ ] Can query results in SQL Server

---

## 🎯 Next Steps

1. ✅ Run setup script (00_QuickSetup.sql)
2. ✅ Verify tables and data
3. ✅ Start the application
4. ✅ Select monitoring folder
5. ✅ Add lab data files
6. ✅ Query results in SQL Server

---

## 📚 Additional Resources

- **SQL Server Documentation**: https://docs.microsoft.com/sql
- **T-SQL Reference**: https://docs.microsoft.com/en-us/sql/t-sql/language-reference
- **Query Optimization**: https://docs.microsoft.com/sql/relational-databases/query-processing-architecture-guide

---

## 📝 Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-06-07 | 1.0 | Initial setup scripts created |

---

**Created**: June 7, 2026
**Database Name**: NandanLabDbDev
**Compatibility**: SQL Server 2016+, Azure SQL Database

---

## 🎓 Example Workflow

```
1. Run 00_QuickSetup.sql
   ↓
2. Verify tables created
   ↓
3. Run application
   ↓
4. Add lab files to monitor folder
   ↓
5. Application automatically:
   - Detects new files
   - Parses lab data
   - Saves to database
   - Moves file to Processed folder
   ↓
6. Query results in SQL Server
   SELECT * FROM dbo.AnalyzerReports
```

---

**Ready to set up? Start with `00_QuickSetup.sql`! 🚀**
