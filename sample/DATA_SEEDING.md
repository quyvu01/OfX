# Data Seeding Documentation

This document describes the data seeding structure for the three sample services and their relationships.

## Overview

The sample project consists of three services with the following data relationships:

```
Service3 (Foundation)
   └─> Countries (8 countries)
       └─> Provinces (24 total provinces)
           ├─> Service2: Users (54 users reference provinces)
           └─> Service1: MemberAddress (35 addresses reference provinces)

Service1 (Additional data)
   ├─> MemberAdditionalData (20 members - standalone)
   └─> MemberSocial (10 social profiles - MongoDB, standalone)
```

---

## Service 3: Countries & Provinces (Foundation Data)

**File**: `sample/Service3/Data/Service3DataSeeder.cs`

**Database**: PostgreSQL (`OfXTestService3`)

### Data Structure

Service3 provides the foundation geographic data that other services reference.

**Countries (8 total)**:
- US (United States) - 5 provinces
- CA (Canada) - 3 provinces
- UK (United Kingdom) - 3 provinces
- AU (Australia) - 3 provinces
- DE (Germany) - 3 provinces
- FR (France) - 3 provinces
- JP (Japan) - 3 provinces
- VN (Vietnam) - 3 provinces

**Total Provinces**: 24

### Example Data

| Country ID | Country Name | Province ID | Province Name |
|------------|--------------|-------------|---------------|
| US | United States | `01962f9a-f7f8-7f61-941c-6a086fe96cd2` | California |
| US | United States | `01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7` | New York |
| US | United States | `01962f9a-f7f8-7e54-a79d-575a8e882eb8` | Texas |
| CA | Canada | `01962f9a-f7f8-7c3d-8e4f-6a7b8c9d0123` | Ontario |
| UK | United Kingdom | `01962f9a-f7f8-7a7b-9c8d-0e1f23456789` | England |

### Models

```csharp
public class Country
{
    public string Id { get; set; }  // e.g., "US", "CA"
    public string Name { get; set; }
    public List<Province> Provinces { get; set; }
}

public class Province
{
    public Guid Id { get; set; }  // UUID
    public string Name { get; set; }
    public string CountryId { get; set; }  // FK to Country
}
```

### Relationships
- **1 Country → Many Provinces** (one-to-many)
- Provinces are created inline when Country is seeded

---

## Service 2: Users

**File**: `sample/Service2/Data/Service2DataSeeder.cs`

**Database**: PostgreSQL (`OfXTestService2`)

### Data Structure

Service2 contains user data, where each user is associated with a province from Service3.

**Total Users**: 54 users distributed across all 24 provinces

### Distribution
- **California**: 4 users (user-001 to user-004)
- **New York**: 4 users (user-004 to user-007)
- **Texas**: 3 users (user-007 to user-009)
- **Florida**: 2 users (user-009 to user-010)
- **Illinois**: 2 users (user-011 to user-012)
- ... (continues for all provinces)

### Example Data

| User ID | Name | Email | Province ID |
|---------|------|-------|-------------|
| user-001 | John Smith | john.smith@email.com | California |
| user-004 | Emily Davis | emily.davis@email.com | New York |
| user-013 | Daniel Martin | daniel.m@email.com | Ontario |

### Model

```csharp
public class User
{
    public string Id { get; set; }  // e.g., "user-001"
    public string Name { get; set; }
    public string Email { get; set; }
    public string ProvinceId { get; set; }  // FK to Service3.Province.Id (as string)
}
```

### Relationships
- **User → Province** (many-to-one)
- **Foreign Key**: `ProvinceId` references `Service3.Province.Id`
- Users cannot exist without a valid Province reference

---

## Service 1: Member Data

**File**: `sample/Service1/Data/Service1DataSeeder.cs`

**Databases**:
- PostgreSQL: `OfXTestService1` (MemberAdditionalData)
- PostgreSQL: `OfXTestOtherService1` (MemberAddress)
- MongoDB: `Service1MongoDb` (MemberSocial)

### 1. MemberAddress (OtherService1Context)

Member addresses linked to provinces.

**Total Addresses**: 35 addresses distributed across provinces

#### Distribution
- **California**: 4 addresses (addr-001 to addr-004)
- **New York**: 4 addresses (addr-005 to addr-008)
- **Texas**: 3 addresses (addr-009 to addr-011)
- ... (continues for other provinces)

#### Example Data

| Address ID | Province | Street | City | Zip Code |
|------------|----------|--------|------|----------|
| addr-001 | California | 123 Silicon Valley Blvd | San Francisco | 94102 |
| addr-005 | New York | 100 Wall Street | New York | 10005 |
| addr-016 | Ontario | 1200 Bay St | Toronto | M5J 2R8 |

#### Model

```csharp
public class MemberAddress
{
    public string Id { get; set; }  // e.g., "addr-001"
    public string ProvinceId { get; set; }  // FK to Service3.Province.Id (as string)
    public string Street { get; set; }  // e.g., "123 Silicon Valley Blvd"
    public string City { get; set; }  // e.g., "San Francisco"
    public string ZipCode { get; set; }  // e.g., "94102"
}
```

#### Relationships
- **MemberAddress → Province** (many-to-one)
- **Foreign Key**: `ProvinceId` references `Service3.Province.Id`

### 2. MemberAdditionalData (Service1Context)

Standalone member data (no foreign keys).

**Total Members**: 20 members

#### Example Data

| Member ID | Name | Bio |
|-----------|------|-----|
| member-001 | Alice Cooper | Software Engineer specializing in distributed systems |
| member-002 | Bob Dylan | Product Manager with 10 years experience |
| member-005 | Edward Norton | Full Stack Developer \| Open Source Contributor |

#### Model

```csharp
public class MemberAdditionalData
{
    public string Id { get; set; }  // e.g., "member-001"
    public string Name { get; set; }
    // Note: Bio, JoinDate may not exist in actual model
}
```

#### Relationships
- **Standalone** - no foreign keys

### 3. MemberSocial (MongoDB)

Social media profiles stored in MongoDB.

**Total Profiles**: 10 social media profiles

#### Example Data

| ID | Name | Platform | Handle | Metadata |
|----|------|----------|--------|----------|
| 1 | Tech Influencer Alpha | Twitter | @techinfluencer | Followers: 150000, Verified: True |
| 2 | Code Master Beta | GitHub | codemaster | Stars: 25000, Repositories: 150 |
| 5 | Data Scientist Epsilon | Kaggle | datascientist | Rank: Expert, Competitions: 25 |

#### Model

```csharp
public class MemberSocial
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string OtherValue { get; set; }  // Platform: Handle
    public DateTime CreatedTime { get; set; }
    public List<MemerSocialMetadata> Metadata { get; set; }
}

public class MemerSocialMetadata
{
    public string Key { get; set; }
    public string Value { get; set; }
    public int Order { get; set; }
    public ExternalOfMetadata ExternalOfMetadata { get; set; }
}
```

#### Relationships
- **Standalone** - no foreign keys
- MongoDB document with embedded metadata

---

## Seeding Order

To maintain referential integrity, services must be seeded in this order:

1. **Service3** - Countries and Provinces (foundation data)
2. **Service2** - Users (depends on Provinces)
3. **Service1** - MemberAddress (depends on Provinces), MemberAdditionalData, MemberSocial

### Execution

Each service's `Program.cs` calls its seeder automatically via `UseAsyncSeeding`:

**Service3/Program.cs**:
```csharp
.UseAsyncSeeding(async (context, _, cancellationToken) =>
{
    await Service3Api.Data.Service3DataSeeder.SeedAsync((Service3Context)context, cancellationToken);
});
```

**Service2/Program.cs**:
```csharp
.UseAsyncSeeding(async (context, _, cancellationToken) =>
{
    await Service2.Data.Service2DataSeeder.SeedAsync((Service2Context)context, cancellationToken);
});
```

**Service1/Program.cs**:
```csharp
// EF Core seeding
.UseAsyncSeeding(async (context, _, cancellationToken) =>
{
    await Service1.Data.Service1DataSeeder.SeedMemberAdditionalDataAsync((Service1Context)context, cancellationToken);
});

.UseAsyncSeeding(async (context, _, cancellationToken) =>
{
    await Service1.Data.Service1DataSeeder.SeedServiceMemberAddressAsync((OtherService1Context)context, cancellationToken);
});

// MongoDB seeding (after migrations)
await Service1.Data.Service1DataSeeder.SeedMemberSocialAsync(memberSocialCollection);
```

---

## Data Statistics

| Service | Entity | Count | Database | Relationships |
|---------|--------|-------|----------|---------------|
| Service3 | Countries | 8 | PostgreSQL | 1 → Many Provinces |
| Service3 | Provinces | 24 | PostgreSQL | Parent of Users & Addresses |
| Service2 | Users | 54 | PostgreSQL | Many → 1 Province |
| Service1 | MemberAddress | 35 | PostgreSQL | Many → 1 Province |
| Service1 | MemberAdditionalData | 20 | PostgreSQL | Standalone |
| Service1 | MemberSocial | 10 | MongoDB | Standalone |

**Total Records**: 151 records across 6 entities

---

## Seeder Features

All seeders include:

✅ **Idempotent Operations**: Can be run multiple times without duplication
✅ **Existence Checks**: Checks if data already exists before inserting
✅ **Transaction Support**: Uses SaveChangesAsync for atomicity
✅ **Cancellation Token Support**: Respects cancellation tokens
✅ **Meaningful Data**: Realistic names, emails, addresses
✅ **Comprehensive Coverage**: All provinces have at least 1 user and 1 address

---

## Testing the Seeding

To verify seeding works correctly:

```bash
# Start databases
docker-compose up -d postgres mongodb

# Run services in order
cd sample/Service3 && dotnet run  # Seeds Countries & Provinces
cd sample/Service2 && dotnet run  # Seeds Users
cd sample/Service1 && dotnet run  # Seeds MemberAddress, MemberAdditionalData, MemberSocial
```

Check database:
```sql
-- Service3
SELECT COUNT(*) FROM "Countries";  -- Should be 8
SELECT COUNT(*) FROM "Provinces";  -- Should be 24

-- Service2
SELECT COUNT(*) FROM "Users";  -- Should be 54

-- Service1 (OfXTestOtherService1)
SELECT COUNT(*) FROM "MemberAddress";  -- Should be 35

-- Service1 (OfXTestService1)
SELECT COUNT(*) FROM "MemberAdditionalData";  -- Should be 20
```

Check MongoDB:
```javascript
use Service1MongoDb
db.MemberSocials.count()  // Should be 10
```

---

## Notes

1. **Province IDs are UUIDs**: All province references use Guid/UUID format
2. **Consistent FK Format**: ProvinceId is stored as string (Guid.ToString())
3. **No Circular References**: Data flow is one-way: Service3 → Service2/Service1
4. **MongoDB Separation**: MemberSocial is isolated in MongoDB with no FK constraints
5. **Realistic Data**: Uses real city names, streets, and professional titles
6. **Cross-Service Testing**: You can test OfX distributed mapping across services by fetching User → Province → Country data
