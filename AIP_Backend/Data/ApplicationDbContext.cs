using AIPBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions options)
            : base(options)
        {
        }

        // Define your DbSets here
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ActionCalendar> ActionCalendars { get; set; }
        public DbSet<ActionCalendarStatusUpdate> ActionCalendarStatusUpdates { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<UserCustomerAssignment> UserCustomerAssignments { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<LookupTable> LookupTables { get; set; }
        public DbSet<PageAccess> PageAccesses { get; set; }
        public DbSet<RolePageAccess> RolePageAccesses { get; set; }
        public DbSet<PageAccessSettings> PageAccessSettings { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<StolenItem> StolenItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CustomerPageAccess> CustomerPageAccesses { get; set; }
        public DbSet<DailyActivityReport> DailyActivityReports { get; set; }
        public DbSet<DailyActivityReportActivity> DailyActivityReportActivities { get; set; }
        public DbSet<DailyActivityReportIncident> DailyActivityReportIncidents { get; set; }
        public DbSet<DailyActivityReportSecurityCheck> DailyActivityReportSecurityChecks { get; set; }
        public DbSet<DailyActivityReportVisitorEntry> DailyActivityReportVisitorEntries { get; set; }
        public DbSet<DailyOccurrenceBook> DailyOccurrenceBooks { get; set; }
        public DbSet<HolidayRequest> HolidayRequests { get; set; }
        public DbSet<BankHoliday> BankHolidays { get; set; }
        public DbSet<AlertRule> AlertRules { get; set; }
        public DbSet<AlertInstance> AlertInstances { get; set; }
        public DbSet<EvidenceItem> EvidenceItems { get; set; }
        public DbSet<EvidenceCustodyEvent> EvidenceCustodyEvents { get; set; }
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }
        public DbSet<StoreRiskScore> StoreRiskScores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ApplicationUser relationships

            // Configure ApplicationUser self-referencing relationships for audit fields
            modelBuilder.Entity<ApplicationUser>()
                .HasOne<ApplicationUser>()
                .WithMany(u => u.CreatedUsers)
                .HasForeignKey(u => u.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne<ApplicationUser>()
                .WithMany(u => u.UpdatedUsers)
                .HasForeignKey(u => u.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ApplicationUser to Customer relationship (for Customer users)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Customer)
                .WithMany()
                .HasForeignKey(u => u.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Configure RolePermission relationships
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique constraint for RoleId and PermissionId combination
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            // Configure ActionCalendar relationships
            modelBuilder.Entity<ActionCalendar>()
                .HasOne(a => a.AssignedUser)
                .WithMany()
                .HasForeignKey(a => a.AssignTo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActionCalendar>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActionCalendar>()
                .HasOne(a => a.ModifiedByUser)
                .WithMany()
                .HasForeignKey(a => a.ModifiedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActionCalendarStatusUpdate>()
                .HasOne(asu => asu.ActionCalendar)
                .WithMany()
                .HasForeignKey(asu => asu.ActionCalendarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActionCalendarStatusUpdate>()
                .HasOne(asu => asu.UpdatedByUser)
                .WithMany()
                .HasForeignKey(asu => asu.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure UserCustomerAssignment relationships
            modelBuilder.Entity<UserCustomerAssignment>()
                .HasOne(uca => uca.User)
                .WithMany(u => u.UserCustomerAssignments)
                .HasForeignKey(uca => uca.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCustomerAssignment>()
                .HasOne(uca => uca.Customer)
                .WithMany(c => c.UserCustomerAssignments)
                .HasForeignKey(uca => uca.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique constraint for UserId and CustomerId combination
            modelBuilder.Entity<UserCustomerAssignment>()
                .HasIndex(uca => new { uca.UserId, uca.CustomerId })
                .IsUnique();

            // Configure Region, CustomerRegion, and Site relationships
            // Region relationships
            modelBuilder.Entity<Region>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Regions)
                .HasForeignKey(r => r.fkCustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            // Site relationships
            modelBuilder.Entity<Site>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.Sites)
                .HasForeignKey(s => s.fkCustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Site>()
                .HasOne(s => s.Region)
                .WithMany(r => r.Sites)
                .HasForeignKey(s => s.fkRegionID)
                .OnDelete(DeleteBehavior.Cascade);



            // Configure indexes for better performance

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CompanyNumber)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.VatNumber)
                .IsUnique();

            // Configure computed columns

            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.FullName)
                .HasComputedColumnSql("CONCAT([FirstName], ' ', [LastName])");

            // LookupTable relationships
            modelBuilder.Entity<LookupTable>()
                .HasOne(lt => lt.CreatedByUser)
                .WithMany()
                .HasForeignKey(lt => lt.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LookupTable>()
                .HasOne(lt => lt.UpdatedByUser)
                .WithMany()
                .HasForeignKey(lt => lt.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // LookupTable indexes
            modelBuilder.Entity<LookupTable>()
                .HasIndex(lt => lt.Category);

            modelBuilder.Entity<LookupTable>()
                .HasIndex(lt => new { lt.Category, lt.Value })
                .IsUnique();

            // Configure PageAccess relationships
            modelBuilder.Entity<PageAccess>()
                .HasIndex(p => p.PageId)
                .IsUnique();

            modelBuilder.Entity<PageAccess>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PageAccess>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.UpdatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure RolePageAccess relationships
            modelBuilder.Entity<RolePageAccess>()
                .HasOne(rpa => rpa.PageAccess)
                .WithMany(p => p.RolePageAccesses)
                .HasForeignKey(rpa => rpa.PageAccessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePageAccess>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(rpa => rpa.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePageAccess>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(rpa => rpa.UpdatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Add unique constraint for RoleName and PageAccessId combination
            modelBuilder.Entity<RolePageAccess>()
                .HasIndex(rpa => new { rpa.RoleName, rpa.PageAccessId })
                .IsUnique();

            // Configure CustomerPageAccess relationships
            modelBuilder.Entity<CustomerPageAccess>()
                .HasOne(cpa => cpa.Customer)
                .WithMany(c => c.CustomerPageAccesses)
                .HasForeignKey(cpa => cpa.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomerPageAccess>()
                .HasOne(cpa => cpa.PageAccess)
                .WithMany(p => p.CustomerPageAccesses)
                .HasForeignKey(cpa => cpa.PageAccessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomerPageAccess>()
                .HasIndex(cpa => new { cpa.CustomerId, cpa.PageAccessId })
                .IsUnique();

            // Configure PageAccessSettings relationships
            modelBuilder.Entity<PageAccessSettings>()
                .HasIndex(ps => ps.SettingKey)
                .IsUnique();

            modelBuilder.Entity<PageAccessSettings>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ps => ps.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PageAccessSettings>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ps => ps.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Incident relationships
            modelBuilder.Entity<Incident>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Incident>()
                .HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Incident>()
                .HasOne(i => i.UpdatedByUser)
                .WithMany()
                .HasForeignKey(i => i.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Incident>()
                .HasMany(i => i.StolenItems)
                .WithOne(si => si.Incident)
                .HasForeignKey(si => si.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure StolenItem relationships
            modelBuilder.Entity<StolenItem>()
                .HasOne(si => si.Incident)
                .WithMany(i => i.StolenItems)
                .HasForeignKey(si => si.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes for better performance
            modelBuilder.Entity<Incident>()
                .HasIndex(i => i.CustomerId);

            modelBuilder.Entity<Incident>()
                .HasIndex(i => i.SiteId);

            modelBuilder.Entity<Incident>()
                .HasIndex(i => i.DateOfIncident);

            modelBuilder.Entity<Incident>()
                .HasIndex(i => i.IncidentType);

            modelBuilder.Entity<Incident>()
                .HasIndex(i => i.Status);

            modelBuilder.Entity<StolenItem>()
                .HasIndex(si => si.IncidentId);

            // Configure Product indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.EAN)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);

            // Configure DailyActivityReport relationships
            modelBuilder.Entity<DailyActivityReport>()
                .HasOne(dar => dar.Customer)
                .WithMany()
                .HasForeignKey(dar => dar.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyActivityReport>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(dar => dar.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyActivityReport>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(dar => dar.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DailyActivityReport one-to-many relationships
            modelBuilder.Entity<DailyActivityReport>()
                .HasMany(dar => dar.Activities)
                .WithOne(a => a.DailyActivityReport)
                .HasForeignKey(a => a.DailyActivityReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailyActivityReport>()
                .HasMany(dar => dar.Incidents)
                .WithOne(i => i.DailyActivityReport)
                .HasForeignKey(i => i.DailyActivityReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailyActivityReport>()
                .HasMany(dar => dar.SecurityChecks)
                .WithOne(sc => sc.DailyActivityReport)
                .HasForeignKey(sc => sc.DailyActivityReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailyActivityReport>()
                .HasMany(dar => dar.VisitorLog)
                .WithOne(ve => ve.DailyActivityReport)
                .HasForeignKey(ve => ve.DailyActivityReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes for better performance
            modelBuilder.Entity<DailyActivityReport>()
                .HasIndex(dar => dar.CustomerId);

            modelBuilder.Entity<DailyActivityReport>()
                .HasIndex(dar => dar.SiteId);

            modelBuilder.Entity<DailyActivityReport>()
                .HasIndex(dar => dar.ReportDate);

            modelBuilder.Entity<DailyActivityReport>()
                .HasIndex(dar => dar.OfficerName);

            // Configure indexes for child tables
            modelBuilder.Entity<DailyActivityReportActivity>()
                .HasIndex(a => a.DailyActivityReportId);

            modelBuilder.Entity<DailyActivityReportIncident>()
                .HasIndex(i => i.DailyActivityReportId);

            modelBuilder.Entity<DailyActivityReportSecurityCheck>()
                .HasIndex(sc => sc.DailyActivityReportId);

            modelBuilder.Entity<DailyActivityReportVisitorEntry>()
                .HasIndex(ve => ve.DailyActivityReportId);

            // Configure DailyOccurrenceBook relationships
            modelBuilder.Entity<DailyOccurrenceBook>()
                .HasOne(dob => dob.Customer)
                .WithMany()
                .HasForeignKey(dob => dob.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyOccurrenceBook>()
                .HasOne(dob => dob.ReportedByUser)
                .WithMany()
                .HasForeignKey(dob => dob.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyOccurrenceBook>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(dob => dob.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyOccurrenceBook>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(dob => dob.UpdatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for better performance
            modelBuilder.Entity<DailyOccurrenceBook>()
                .HasIndex(dob => dob.CustomerId);

            modelBuilder.Entity<DailyOccurrenceBook>()
                .HasIndex(dob => new { dob.CustomerId, dob.SiteId });

            modelBuilder.Entity<DailyOccurrenceBook>()
                .HasIndex(dob => dob.OccurrenceDate);

            modelBuilder.Entity<DailyOccurrenceBook>()
				.HasIndex(dob => dob.OccurrenceCode);

            modelBuilder.Entity<DailyOccurrenceBook>()
				.HasIndex(dob => dob.StoreNumber);

            // Configure HolidayRequest relationships

            modelBuilder.Entity<HolidayRequest>()
                .HasOne(hr => hr.AuthorisedByUser)
                .WithMany()
                .HasForeignKey(hr => hr.AuthorisedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HolidayRequest>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(hr => hr.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HolidayRequest>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(hr => hr.UpdatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for better performance
            modelBuilder.Entity<HolidayRequest>()
                .HasIndex(hr => hr.Status);

            modelBuilder.Entity<HolidayRequest>()
                .HasIndex(hr => hr.StartDate);

            modelBuilder.Entity<HolidayRequest>()
                .HasIndex(hr => hr.Archived);

            // Configure BankHoliday relationships (Officer navigation removed - using OfficerId directly)


            modelBuilder.Entity<BankHoliday>()
                .HasOne(bh => bh.CreatedByUser)
                .WithMany()
                .HasForeignKey(bh => bh.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BankHoliday>()
                .HasOne(bh => bh.UpdatedByUser)
                .WithMany()
                .HasForeignKey(bh => bh.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure BankHoliday indexes
            modelBuilder.Entity<BankHoliday>()
                .HasIndex(bh => bh.OfficerId);

            modelBuilder.Entity<BankHoliday>()
                .HasIndex(bh => bh.HolidayDate);

            modelBuilder.Entity<BankHoliday>()
                .HasIndex(bh => bh.Status);

            modelBuilder.Entity<BankHoliday>()
                .HasIndex(bh => bh.Archived);

            // Configure Employee relationships
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ModifiedByUser)
                .WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Employee indexes
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL AND [RecordIsDeletedYN] = 0");

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            // Configure Employee computed columns
            modelBuilder.Entity<Employee>()
                .Property(e => e.FullName)
                .HasComputedColumnSql("CONCAT([FirstName], ' ', [Surname])");

            // Configure EvidenceItem relationships
            modelBuilder.Entity<EvidenceItem>()
                .HasOne(e => e.Incident)
                .WithMany()
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvidenceItem>()
                .HasMany(e => e.CustodyEvents)
                .WithOne(c => c.EvidenceItem)
                .HasForeignKey(c => c.EvidenceItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EvidenceItem>()
                .HasIndex(e => e.Barcode);

            modelBuilder.Entity<EvidenceItem>()
                .HasIndex(e => e.IncidentId);

            modelBuilder.Entity<EvidenceCustodyEvent>()
                .HasIndex(c => c.EvidenceItemId);

            // Configure FaceEmbedding indexes to support efficient similarity lookup pivots
            modelBuilder.Entity<FaceEmbedding>()
                .HasIndex(f => f.OffenderId);

            modelBuilder.Entity<FaceEmbedding>()
                .HasIndex(f => f.IncidentId);

            modelBuilder.Entity<FaceEmbedding>()
                .HasIndex(f => f.FileName);

            // Configure StoreRiskScore indexes for fast dashboard lookups
            modelBuilder.Entity<StoreRiskScore>()
                .HasIndex(s => new { s.CustomerId, s.ForDate });

            modelBuilder.Entity<StoreRiskScore>()
                .HasIndex(s => new { s.CustomerId, s.SiteId, s.ForDate })
                .IsUnique();

            // Configure AlertInstance relationships
            modelBuilder.Entity<AlertInstance>()
                .HasOne(a => a.AlertRule)
                .WithMany()
                .HasForeignKey(a => a.AlertRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlertInstance>()
                .HasOne(a => a.Incident)
                .WithMany()
                .HasForeignKey(a => a.IncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlertInstance>()
                .HasIndex(a => a.Status);

            modelBuilder.Entity<AlertInstance>()
                .HasIndex(a => a.AlertRuleId);

            modelBuilder.Entity<AlertInstance>()
                .HasIndex(a => a.CreatedAt);

        }
    }
}
