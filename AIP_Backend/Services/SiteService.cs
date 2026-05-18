#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Services
{
    public class SiteService : ISiteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserContextService _userContext;

        public SiteService(ApplicationDbContext context, IUserContextService userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task<List<SiteDto>> GetSitesAsync(int page = 1, int pageSize = 10, string? search = null, int? customerId = null, int? regionId = null)
        {
            var context = _userContext.GetCurrentContext();
            var customerFilter = _userContext.ResolveCustomerFilter(customerId);
            var query = _context.Sites.AsQueryable().ApplyTenantScope(customerFilter, context);

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.LocationName.Contains(search) || 
                                        s.BuildingName!.Contains(search) || 
                                        s.Town!.Contains(search) || 
                                        s.County!.Contains(search));
            }

            if (regionId.HasValue)
            {
                query = query.Where(s => s.fkRegionID == regionId.Value);
            }

            // Only return non-deleted sites
            query = query.Where(s => !s.RecordIsDeletedYN);

            var sites = await query
                .OrderBy(s => s.LocationName) // Add default ordering to fix EF warning
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SiteDto
                {
                    SiteID = s.SiteID,
                    fkCustomerID = s.fkCustomerID,
                    fkRegionID = s.fkRegionID,
                    CoreSiteYN = s.CoreSiteYN,
                    LocationName = s.LocationName,
                    SINNumber = s.SINNumber,
                    LocationType = s.LocationType,
                    BuildingName = s.BuildingName,
                    NumberandStreet = s.NumberandStreet,
                    VillageOrSuburb = s.VillageOrSuburb,
                    Town = s.Town,
                    County = s.County,
                    Postcode = s.Postcode,
                    TelephoneNumber = s.TelephoneNumber,
                    ContractStartDate = s.ContractStartDate,
                    ContractEndDate = s.ContractEndDate,
                    Details = s.Details,
                    SiteSurveyComplete = s.SiteSurveyComplete,
                    AssignmentInstructionsIssued = s.AssignmentInstructionsIssued,
                    RiskAssessmentIssued = s.RiskAssessmentIssued,
                    RecordIsDeletedYN = s.RecordIsDeletedYN,
                    DateCreated = s.DateCreated,
                    CreatedBy = s.CreatedBy,
                    DateModified = s.DateModified,
                    ModifiedBy = s.ModifiedBy
                })
                .ToListAsync();

            return sites;
        }

        public async Task<SiteDto?> GetSiteByIdAsync(int id)
        {
            var site = await _context.Sites
                .FirstOrDefaultAsync(s => s.SiteID == id && !s.RecordIsDeletedYN);

            if (site == null)
                return null;

            _userContext.EnsureCanAccessCustomer(site.fkCustomerID);
            var userContext = _userContext.GetCurrentContext();
            if (userContext.AccessibleSiteIds.Count > 0 &&
                !userContext.AccessibleSiteIds.Contains(site.SiteID.ToString()))
            {
                throw new AIPBackend.Exceptions.ForbiddenAccessException("You do not have permission to access this site.");
            }

            return new SiteDto
            {
                SiteID = site.SiteID,
                fkCustomerID = site.fkCustomerID,
                fkRegionID = site.fkRegionID,
                CoreSiteYN = site.CoreSiteYN,
                LocationName = site.LocationName,
                SINNumber = site.SINNumber,
                LocationType = site.LocationType,
                BuildingName = site.BuildingName,
                NumberandStreet = site.NumberandStreet,
                VillageOrSuburb = site.VillageOrSuburb,
                Town = site.Town,
                County = site.County,
                Postcode = site.Postcode,
                TelephoneNumber = site.TelephoneNumber,
                ContractStartDate = site.ContractStartDate,
                ContractEndDate = site.ContractEndDate,
                Details = site.Details,
                SiteSurveyComplete = site.SiteSurveyComplete,
                AssignmentInstructionsIssued = site.AssignmentInstructionsIssued,
                RiskAssessmentIssued = site.RiskAssessmentIssued,
                RecordIsDeletedYN = site.RecordIsDeletedYN,
                DateCreated = site.DateCreated,
                CreatedBy = site.CreatedBy,
                DateModified = site.DateModified,
                ModifiedBy = site.ModifiedBy
            };
        }

        public async Task<List<SiteDto>> GetSitesByCustomerAsync(int customerId)
        {
            var context = _userContext.GetCurrentContext();
            var customerFilter = _userContext.ResolveCustomerFilter(customerId);
            var sites = await _context.Sites
                .Where(s => !s.RecordIsDeletedYN)
                .ApplyTenantScope(customerFilter, context)
                .Select(s => new SiteDto
                {
                    SiteID = s.SiteID,
                    fkCustomerID = s.fkCustomerID,
                    fkRegionID = s.fkRegionID,
                    CoreSiteYN = s.CoreSiteYN,
                    LocationName = s.LocationName,
                    SINNumber = s.SINNumber,
                    LocationType = s.LocationType,
                    BuildingName = s.BuildingName,
                    NumberandStreet = s.NumberandStreet,
                    VillageOrSuburb = s.VillageOrSuburb,
                    Town = s.Town,
                    County = s.County,
                    Postcode = s.Postcode,
                    TelephoneNumber = s.TelephoneNumber,
                    ContractStartDate = s.ContractStartDate,
                    ContractEndDate = s.ContractEndDate,
                    Details = s.Details,
                    SiteSurveyComplete = s.SiteSurveyComplete,
                    AssignmentInstructionsIssued = s.AssignmentInstructionsIssued,
                    RiskAssessmentIssued = s.RiskAssessmentIssued,
                    RecordIsDeletedYN = s.RecordIsDeletedYN,
                    DateCreated = s.DateCreated,
                    CreatedBy = s.CreatedBy,
                    DateModified = s.DateModified,
                    ModifiedBy = s.ModifiedBy
                })
                .ToListAsync();

            return sites;
        }

        public async Task<List<SiteDto>> GetSitesByRegionAsync(int regionId)
        {
            var region = await _context.Regions
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RegionID == regionId && !r.RecordIsDeletedYN);
            if (region == null)
            {
                return new List<SiteDto>();
            }

            _userContext.EnsureCanAccessCustomer(region.fkCustomerID);
            var context = _userContext.GetCurrentContext();
            var customerFilter = _userContext.ResolveCustomerFilter(region.fkCustomerID);
            var sites = await _context.Sites
                .Where(s => s.fkRegionID == regionId && !s.RecordIsDeletedYN)
                .ApplyTenantScope(customerFilter, context)
                .Select(s => new SiteDto
                {
                    SiteID = s.SiteID,
                    fkCustomerID = s.fkCustomerID,
                    fkRegionID = s.fkRegionID,
                    CoreSiteYN = s.CoreSiteYN,
                    LocationName = s.LocationName,
                    SINNumber = s.SINNumber,
                    LocationType = s.LocationType,
                    BuildingName = s.BuildingName,
                    NumberandStreet = s.NumberandStreet,
                    VillageOrSuburb = s.VillageOrSuburb,
                    Town = s.Town,
                    County = s.County,
                    Postcode = s.Postcode,
                    TelephoneNumber = s.TelephoneNumber,
                    ContractStartDate = s.ContractStartDate,
                    ContractEndDate = s.ContractEndDate,
                    Details = s.Details,
                    SiteSurveyComplete = s.SiteSurveyComplete,
                    AssignmentInstructionsIssued = s.AssignmentInstructionsIssued,
                    RiskAssessmentIssued = s.RiskAssessmentIssued,
                    RecordIsDeletedYN = s.RecordIsDeletedYN,
                    DateCreated = s.DateCreated,
                    CreatedBy = s.CreatedBy,
                    DateModified = s.DateModified,
                    ModifiedBy = s.ModifiedBy
                })
                .ToListAsync();

            return sites;
        }

        public async Task<SiteDto> CreateSiteAsync(SiteCreateRequestDto createSiteDto, string createdBy)
        {
            _userContext.EnsureCanAccessCustomer(createSiteDto.fkCustomerID);
            var site = new Site
            {
                fkCustomerID = createSiteDto.fkCustomerID,
                fkRegionID = createSiteDto.fkRegionID,
                CoreSiteYN = createSiteDto.CoreSiteYN,
                LocationName = createSiteDto.LocationName,
                SINNumber = createSiteDto.SINNumber,
                LocationType = createSiteDto.LocationType,
                BuildingName = createSiteDto.BuildingName,
                NumberandStreet = createSiteDto.NumberandStreet,
                VillageOrSuburb = createSiteDto.VillageOrSuburb,
                Town = createSiteDto.Town,
                County = createSiteDto.County,
                Postcode = createSiteDto.Postcode,
                TelephoneNumber = createSiteDto.TelephoneNumber,
                ContractStartDate = createSiteDto.ContractStartDate,
                ContractEndDate = createSiteDto.ContractEndDate,
                Details = createSiteDto.Details,
                SiteSurveyComplete = createSiteDto.SiteSurveyComplete,
                AssignmentInstructionsIssued = createSiteDto.AssignmentInstructionsIssued,
                RiskAssessmentIssued = createSiteDto.RiskAssessmentIssued,
                RecordIsDeletedYN = false,
                DateCreated = DateTime.UtcNow,
                CreatedBy = createdBy,
                DateModified = null,
                ModifiedBy = null
            };

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            return new SiteDto
            {
                SiteID = site.SiteID,
                fkCustomerID = site.fkCustomerID,
                fkRegionID = site.fkRegionID,
                CoreSiteYN = site.CoreSiteYN,
                LocationName = site.LocationName,
                SINNumber = site.SINNumber,
                LocationType = site.LocationType,
                BuildingName = site.BuildingName,
                NumberandStreet = site.NumberandStreet,
                VillageOrSuburb = site.VillageOrSuburb,
                Town = site.Town,
                County = site.County,
                Postcode = site.Postcode,
                TelephoneNumber = site.TelephoneNumber,
                ContractStartDate = site.ContractStartDate,
                ContractEndDate = site.ContractEndDate,
                Details = site.Details,
                SiteSurveyComplete = site.SiteSurveyComplete,
                AssignmentInstructionsIssued = site.AssignmentInstructionsIssued,
                RiskAssessmentIssued = site.RiskAssessmentIssued,
                RecordIsDeletedYN = site.RecordIsDeletedYN,
                DateCreated = site.DateCreated,
                CreatedBy = site.CreatedBy,
                DateModified = site.DateModified,
                ModifiedBy = site.ModifiedBy
            };
        }

        public async Task<SiteDto?> UpdateSiteAsync(int id, SiteUpdateRequestDto updateSiteDto, string modifiedBy)
        {
            var site = await _context.Sites
                .FirstOrDefaultAsync(s => s.SiteID == id && !s.RecordIsDeletedYN);

            if (site == null)
                return null;

            _userContext.EnsureCanAccessCustomer(site.fkCustomerID);
            site.CoreSiteYN = updateSiteDto.CoreSiteYN;
            site.LocationName = updateSiteDto.LocationName ?? site.LocationName;
            site.SINNumber = updateSiteDto.SINNumber;
            site.LocationType = updateSiteDto.LocationType;
            site.BuildingName = updateSiteDto.BuildingName;
            site.NumberandStreet = updateSiteDto.NumberandStreet;
            site.VillageOrSuburb = updateSiteDto.VillageOrSuburb;
            site.Town = updateSiteDto.Town;
            site.County = updateSiteDto.County;
            site.Postcode = updateSiteDto.Postcode;
            site.TelephoneNumber = updateSiteDto.TelephoneNumber;
            site.ContractStartDate = updateSiteDto.ContractStartDate;
            site.ContractEndDate = updateSiteDto.ContractEndDate;
            site.Details = updateSiteDto.Details;
            site.SiteSurveyComplete = updateSiteDto.SiteSurveyComplete;
            site.AssignmentInstructionsIssued = updateSiteDto.AssignmentInstructionsIssued;
            site.RiskAssessmentIssued = updateSiteDto.RiskAssessmentIssued;
            site.DateModified = DateTime.UtcNow;
            site.ModifiedBy = modifiedBy ?? "system";

            await _context.SaveChangesAsync();

            return new SiteDto
            {
                SiteID = site.SiteID,
                fkCustomerID = site.fkCustomerID,
                fkRegionID = site.fkRegionID,
                CoreSiteYN = site.CoreSiteYN,
                LocationName = site.LocationName,
                SINNumber = site.SINNumber,
                LocationType = site.LocationType,
                BuildingName = site.BuildingName,
                NumberandStreet = site.NumberandStreet,
                VillageOrSuburb = site.VillageOrSuburb,
                Town = site.Town,
                County = site.County,
                Postcode = site.Postcode,
                TelephoneNumber = site.TelephoneNumber,
                ContractStartDate = site.ContractStartDate,
                ContractEndDate = site.ContractEndDate,
                Details = site.Details,
                SiteSurveyComplete = site.SiteSurveyComplete,
                AssignmentInstructionsIssued = site.AssignmentInstructionsIssued,
                RiskAssessmentIssued = site.RiskAssessmentIssued,
                RecordIsDeletedYN = site.RecordIsDeletedYN,
                DateCreated = site.DateCreated,
                CreatedBy = site.CreatedBy,
                DateModified = site.DateModified,
                ModifiedBy = site.ModifiedBy
            };
        }

        public async Task<bool> DeleteSiteAsync(int id)
        {
            var site = await _context.Sites
                .FirstOrDefaultAsync(s => s.SiteID == id && !s.RecordIsDeletedYN);

            if (site == null)
                return false;

            _userContext.EnsureCanAccessCustomer(site.fkCustomerID);
            // Soft delete
            site.RecordIsDeletedYN = true;
            site.DateModified = DateTime.UtcNow;
            site.ModifiedBy = "system"; // TODO: Get from current user

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
