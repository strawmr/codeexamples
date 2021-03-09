using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TrialScout.DAL.Context;
using TrialScout.DAL.Entities;
using TrialScout.Web.Classes;
using TrialScout.Web.ViewModels;
using TrialScout.Web.Utility;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace TrialScout.Web.Controllers
{
    [Authorize(Roles = "Administrator, DataImport")]
    public class ResearchCentersMappingController : Controller
    {
        private TrialScoutContext db = new TrialScoutContext();

        // GET: ResearchCentersMapping
        public ActionResult Index()
        {
            return View();
        }

        // GET: ResearchCentersMapping/Details/5
        public async Task<ActionResult> MapResearchCenter(Guid? id, bool showMapped = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ResearchCenter researchCenter = null;
            await Task.Factory.StartNew(() => { researchCenter = db.ResearchCenters.Find(id); });
            if (researchCenter == null || researchCenter.IsDeleted)
            {
                return HttpNotFound();
            }

            var viewModel = new ResearchCenterMappingViewModel
            {
                ResearchCenterId = researchCenter.CenterId,
                ResearchCenterName = researchCenter.LocationName
            };

            return View(viewModel);
        }

        //POST ResearchCentersMapping/MapResearchCenter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MapResearchCenter(ResearchCenterMappingViewModel model)
        {
            if (!model.ResearchCenterId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var viewModel = new ResearchCenterMappingConfirmationViewModel();
            viewModel.ResearchCenterId = model.ResearchCenterId.Value;
            viewModel.ResearchCenterName = model.ResearchCenterName;

            await Task.Factory.StartNew(() =>
            {
                var selected = model.UnmappedAliases.Where(x => x.Selected).Select(x => x.AliasId).ToList();
                var unselected = model.UnmappedAliases.Where(x => !x.Selected).Select(x => x.AliasId).ToList();

                viewModel.AddedAliases = db.ResearchCenterAliases.Where(x => !x.CenterId.HasValue && selected.Contains(x.AliasId)).ToList();

                viewModel.RemovedAliases = db.ResearchCenterAliases
                    .Where(x => x.CenterId.HasValue && x.CenterId.Value == model.ResearchCenterId && unselected.Contains(x.AliasId)).ToList();
            });

            return View("ConfirmResearchCenterMappingChanges", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmResearchCenterMappingChanges(ResearchCenterMappingConfirmationViewModel model)
        {
            //get aliases from db

            await Task.Factory.StartNew(() =>
            {
                var modelAppended = model.AddedAliases.Select(y => y.AliasId).ToList();
                var appended = db.ResearchCenterAliases.Where(x => modelAppended.Contains(x.AliasId)).ToList();
                foreach (var item in appended)
                {
                    item.CenterId = model.ResearchCenterId;
                    item.LastModifiedBy = Guid.Parse(User.Identity.GetUserId());
                    item.LastModifiedOn = DateTime.Now;
                }
                if (model.RemovedAliases == null) model.RemovedAliases = new List<ResearchCenterAlias>();
                var modelRemoved = model.RemovedAliases.Select(y => y.AliasId).ToList();
                var unmapped = db.ResearchCenterAliases.Where(x => modelRemoved.Contains(x.AliasId)).ToList();
                foreach (var item in unmapped)
                {
                    item.CenterId = null;
                    item.LastModifiedBy = Guid.Parse(User.Identity.GetUserId());
                    item.LastModifiedOn = DateTime.Now;
                }
                db.SaveChanges();
            });

            return RedirectToAction("MapAliasesDynamicResearchCenter");
        }

        // GET: ResearchCentersMapping/Create
        public ActionResult Create()
        {
            List<SelectListItem> subscriptionLevels = null;
            using (var ctx = new TrialScoutContext())
            {
                subscriptionLevels = ctx.SubscriptionLevels.Select(x => new SelectListItem()
                {
                    Text = x.Name,
                    Value = x.SubscriptionId.ToString(),
                    Selected = x.Name == "Basic" ? true : false
                }).ToList();
            }
            ViewBag.SubscriptionLevels = subscriptionLevels;
            ViewBag.DefaultSubscription = subscriptionLevels.FirstOrDefault(x => x.Selected).Value;
            return View();
        }

        // POST: ResearchCentersMapping/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "LocationName,StateImported,Country,SubscriptionLevelId,IsPublished")] ResearchCenter researchCenter)
        {
            if (ModelState.IsValid)
            {
                await Task.Factory.StartNew(() =>
                {
                    var now = DateTime.Now;
                    researchCenter.CenterId = Guid.NewGuid();
                    researchCenter.CreationType = 1;
                    researchCenter.CreatedBy = Guid.Parse(User.Identity.GetUserId());
                    researchCenter.CreatedOn = now;
                    researchCenter.LastModifiedBy = Guid.Parse(User.Identity.GetUserId());
                    researchCenter.LastModifiedOn = now;

                    if (!User.IsInRole("Administrator"))
                    {
                        using (var ctx = new TrialScoutContext())
                        {
                            researchCenter.SubscriptionLevelId = ctx.SubscriptionLevels.FirstOrDefault(x => x.Name == "Basic").SubscriptionId;
                            researchCenter.IsPublished = false;
                        }
                    }
                    db.ResearchCenters.Add(researchCenter);
                    db.SaveChanges();
                });
                return RedirectToAction("MapAliasesDynamicResearchCenter");
            }

            List<SelectListItem> subscriptionLevels = null;
            using (var ctx = new TrialScoutContext())
            {
                subscriptionLevels = ctx.SubscriptionLevels.Select(x => new SelectListItem()
                {
                    Text = x.Name,
                    Value = x.SubscriptionId.ToString(),
                    Selected = x.Name == "Basic" ? true : false
                }).ToList();
            }
            ViewBag.SubscriptionLevels = subscriptionLevels;
            ViewBag.DefaultSubscription = subscriptionLevels.FirstOrDefault(x => x.Selected).Value;

            ViewBag.ApprovalId = new SelectList(db.ResearchCenterApprovals, "ApprovalId", "AgentEmailAddress", researchCenter.ApprovalId);
            return View(researchCenter);
        }

        // GET: ResearchCentersMapping/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ResearchCenter researchCenter = null;
            List<SelectListItem> subscriptionLevels = null;
            await Task.Factory.StartNew(() =>
            {
                using (var ctx = new TrialScoutContext())
                {
                    researchCenter = ctx.ResearchCenters.Find(id);

                    if (researchCenter != null && !researchCenter.IsDeleted)
                    {
                        subscriptionLevels = ctx.SubscriptionLevels.Select(x => new SelectListItem()
                        {
                            Text = x.Name,
                            Value = x.SubscriptionId.ToString(),
                            Selected = x.SubscriptionId == researchCenter.SubscriptionLevelId
                        }).ToList();
                    }
                }
            });
            if (researchCenter == null || researchCenter.IsDeleted)
            {
                return HttpNotFound();
            }

            ViewBag.SubscriptionLevels = subscriptionLevels;

            return View(researchCenter);
        }

        // POST: ResearchCentersMapping/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "CenterId,LocationName,StateImported,Country,SubscriptionLevelId,IsPublished")] ResearchCenter researchCenter)
        {
            if (ModelState.IsValid)
            {
                await Task.Factory.StartNew(() =>
                {
                    var rc = db.ResearchCenters.Find(researchCenter.CenterId);

                    rc.LastModifiedBy = Guid.Parse(User.Identity.GetUserId());
                    rc.LastModifiedOn = DateTime.Now;
                    rc.LocationName = researchCenter.LocationName;
                    rc.StateImported = researchCenter.StateImported;
                    rc.Country = researchCenter.Country;

                    if (User.IsInRole("Administrator"))
                    {
                        rc.SubscriptionLevelId = researchCenter.SubscriptionLevelId;
                        rc.IsPublished = researchCenter.IsPublished;

                        if (researchCenter.IsPublished)
                        {
                            rc.PublishedOn = DateTime.Now;
                            rc.PublishedBy = Guid.Parse(User.Identity.GetUserId());
                        }
                    }
                    db.SaveChanges();
                });
                return RedirectToAction("Index");
            }
            return View(researchCenter);
        }

        // GET: ResearchCentersMapping/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ResearchCenter researchCenter = null;
            await Task.Factory.StartNew(() => { researchCenter = db.ResearchCenters.Find(id); });
            if (researchCenter == null || researchCenter.IsDeleted)
            {
                return HttpNotFound();
            }

            await Task.Factory.StartNew(() =>
            {
                using (ApplicationUserManager userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>())
                {
                    var user = userManager.Users.FirstOrDefault(x => x.Id == researchCenter.UserId);
                    ViewBag.UserName = user != null ? user.FirstName + " " + user.LastName : string.Empty;
                }
            });

            return View(researchCenter);
        }

        // POST: ResearchCentersMapping/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            await Task.Factory.StartNew(() =>
            {
                ResearchCenter researchCenter = db.ResearchCenters.Find(id);
                researchCenter.IsDeleted = true;
                researchCenter.DeletedOn = DateTime.Now;
                researchCenter.DeletedBy = Guid.Parse(User.Identity.GetUserId());

                foreach (var item in researchCenter.ResearchCenterAliases)
                {
                    item.CenterId = null;
                }

                db.SaveChanges();
            });

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public async Task<PartialViewResult> MapAliasGrid(Guid? id, bool showMapped = false, string metroCode = null, string state = null, string county = null, string country = null, bool showAliasesWithNoFacilities = false)
        {
            try
            {
                ResearchCenter researchCenter = db.ResearchCenters.Find(id);

                List<AliasViewModel> aliases = new List<AliasViewModel>();
                ResearchCenterMappingViewModel viewModel = null;

                await Task.Factory.StartNew(() =>
                {
                    using (var ctx = new TrialScoutContext())
                    {
                        aliases = ctx.Database.SqlQuery<AliasViewModel>(
                            "sp_GetAliasesForMapping @showMapped, @showAliasesWithoutFacilities, @metroCode, @state, @county, @country, @centerId",
                            new SqlParameter("@showMapped", showMapped),
                            new SqlParameter("@showAliasesWithoutFacilities", showAliasesWithNoFacilities),
                            new SqlParameter("@metroCode", metroCode),
                            new SqlParameter("@state", state),
                            new SqlParameter("@county", county),
                            new SqlParameter("@country", country),
                            new SqlParameter("@centerId", id ?? SqlGuid.Null))
                            .ToList();

                        ViewBag.TotalCount = aliases.Count;
                        ViewBag.CBSASearch = metroCode;
                        ViewBag.StateSearch = state;
                        ViewBag.CountrySearch = country;

                        aliases = aliases.OrderBy(x => x.AliasName).Take(500).ToList();
                        Guid? rcId = null;

                        if (researchCenter != null)
                            rcId = researchCenter.CenterId;

                        viewModel = new ResearchCenterMappingViewModel
                        {
                            ResearchCenterId = rcId,
                            ResearchCenterName = researchCenter != null ? researchCenter.LocationName : string.Empty,
                            ShowMapped = showMapped,
                            UnmappedAliases = aliases
                        };

                    }
                });

                return PartialView("_MapAliasGrid", viewModel);
            }
            catch (Exception ex)
            {
                Globals.LogError(Guid.Parse(User.Identity.GetUserId()), ex, ex.Message, Globals.LogLevels.Error);
                throw;
            }
        }

        [AllowAnonymous]
        [Route("ResearchCentersMapping/GetMetroAreas")]
        [HttpGet]
        public JsonResult GetMetroAreas(string term)
        {
            try
            {
                List<string> metroAreas = db.ResearchCenterAliases.Where(r => r.CBSATitle.Contains(term)).Select(r => r.CBSATitle).Distinct().Take(5).ToList();
                return Json(metroAreas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous, HttpGet, Route("ResearchCentersMapping/GetStates")]
        public JsonResult GetStates(string term)
        {
            try
            {
                List<string> states = db.ResearchCenterAliases
                    .Where(r => (term == null || term.Trim() == string.Empty) ?
                                    (r.State == null || r.State.Trim() == string.Empty) :
                                    r.State != null && r.State.Trim() != string.Empty && r.State.ToLower().Contains(term.ToLower()))
                    .Select(r => r.State)
                    .Distinct()
                    .Take(5)
                    .ToList();
                return Json(states, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ResearchCenterAjaxSearch(DataTableAjaxPostModel model)
        {
            var res = ResearchCenterSearch(model);

            return Json(new
            {
                // this is what datatables wants sending back
                draw = model.draw,
                recordsTotal = res.TotalResultsCount,
                recordsFiltered = res.FilteredResultsCount,
                data = res.ReturnData.Select(rc => new string[]
                {
                    $"<a href=\"/ResearchCenter/{rc.LocationName}\" target=\"_blank\">{rc.LocationName}</a>",
                    rc.StateImported,
                    rc.Country,
                    rc.SubscriptionLevel.Name,
                    rc.IsPublished ? "Yes" : "No",
                     $"<div class=\"btn-group\">" +
                     $"<a href=\"/ResearchCentersMapping/Edit?id={rc.CenterId}\" class=\"btn btn-primary\">Edit</a>" +
                     (User.IsInRole("Administrator") ? $"<a href=\"/ResearchCenter/Edit/{rc.CenterId}?returnController=ResearchCentersMapping&returnAction=Index\" class=\"btn btn-primary\">Edit Profile</a>" : string.Empty) +
                     $"<a href=\"/ResearchCentersMapping/Delete?id={rc.CenterId}\" class=\"btn btn-danger\">Delete</a>" +
                     $"<a href=\"/ResearchCentersMapping/MapResearchCenter?id={rc.CenterId}\" class=\"btn btn-primary\">Map</a>" +
                     "</div>"
                }).ToArray()
            });
        }

        public DataTableReturnData<ResearchCenter> ResearchCenterSearch(DataTableAjaxPostModel model)
        {
            var searchBy = (model.search != null) ? model.search.value : null;
            var take = model.length;
            var skip = model.start;

            string sortBy = "";
            bool sortAsc = true;

            if (model.order != null)
            {
                // in this example we just default sort on the 1st column
                sortBy = GetResearchCenterOrderBy(model.columns[model.order[0].column].data);
                sortAsc = model.order[0].dir.ToLower() == "asc";
            }

            // search the dbase taking into consideration table sorting and paging
            var result = GetResearchCentersFromDb(searchBy, take, skip, sortBy, sortAsc);
            if (result.ReturnData == null)
            {
                // empty collection...
                result.ReturnData = new List<ResearchCenter>();
            }
            return result;
        }

        private string GetResearchCenterOrderBy(string data)
        {
            switch (data)
            {
                case "0":
                    return nameof(ResearchCenter.LocationName);
                case "1":
                    return nameof(ResearchCenter.State);
                case "2":
                    return nameof(ResearchCenter.Country);
                case "3":
                    return "SubscriptionLevel.Name";
                case "4":
                    return nameof(ResearchCenter.IsPublished);
                default:
                    return nameof(ResearchCenter.LocationName);
            }
        }

        public DataTableReturnData<ResearchCenter> GetResearchCentersFromDb(string searchBy, int take, int skip, string sortBy, bool sortAsc)
        {
            // the example datatable used is not supporting multi column ordering
            // so we only need get the column order from the first column passed to us.        
            var whereClause = BuildDynamicWhereClauseForResearchCenterDataTable(searchBy);

            var result = new DataTableReturnData<ResearchCenter>();


            var query = db.ResearchCenters.Include(r => r.SubscriptionLevel)
                .Where(rc => !rc.IsDeleted)
                .Where(whereClause).AsQueryable();

            if (sortAsc)
                query = query.OrderBy(sortBy);
            else
                query = query.OrderByDescending(sortBy);

            result.ReturnData = query
                .Skip(skip)
                .Take(take)
                .ToList();

            // now just get the count of items (without the skip and take) - eg how many could be returned with filtering
            result.FilteredResultsCount = db.ResearchCenters.Count(whereClause);
            result.TotalResultsCount = db.ResearchCenters.Count(rc => !rc.IsDeleted);

            return result;
        }

        private Func<ResearchCenter, bool> BuildDynamicWhereClauseForResearchCenterDataTable(string searchValue)
        {
            var result = new Func<ResearchCenter, bool>(rc => true);
            // simple method to dynamically plugin a where clause
            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                result = new Func<ResearchCenter, bool>(rc =>
                    !rc.IsDeleted &&
                    (rc.LocationName.NullToLower().Contains(string.IsNullOrWhiteSpace(searchValue) ? string.Empty : searchValue.ToLower()) ||
                    rc.State.NullToLower().Contains(string.IsNullOrEmpty(searchValue) ? string.Empty : searchValue.ToLower())));
            }
            return result;
        }

        public ActionResult MapAliasesDynamicResearchCenter()
        {
            return View();
        }

        [AllowAnonymous]
        [Route("ResearchCentersMapping/GetAliasMetroAreas")]
        [HttpGet]
        public JsonResult GetAliasMetroAreas(string term)
        {
            try
            {
                List<string> metroAreas = db.ResearchCenterAliases.Where(r => r.CBSATitle.Contains(term) && !r.IsDeleted).Select(r => r.CBSATitle).Distinct().Take(5).ToList();

                return Json(metroAreas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("ResearchCentersMapping/GetAliasStates")]
        [HttpGet]
        public JsonResult GetAliasStates(string term)
        {
            try
            {
                List<string> metroAreas = db.ResearchCenterAliases.Where(r => r.State.Contains(term) && !r.IsDeleted).Select(r => r.State).Distinct().Take(5).ToList();

                return Json(metroAreas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("ResearchCentersMapping/GetAliasCounties")]
        [HttpGet]
        public JsonResult GetAliasCounties(string term)
        {
            try
            {
                List<string> metroAreas = db.ResearchCenterAliases.Where(r => r.County.Contains(term) && !r.IsDeleted).Select(r => r.County).Distinct().Take(5).ToList();

                return Json(metroAreas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("ResearchCentersMapping/GetAliasCountries")]
        [HttpGet]
        public JsonResult GetAliasCountries(string term)
        {
            try
            {
                List<string> metroAreas = db.ResearchCenterAliases.Where(r => r.Country.Contains(term) && !r.IsDeleted).Select(r => r.Country).Distinct().Take(5).ToList();

                return Json(metroAreas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous, HttpGet, Route("ResearchCentersMapping/GetResearchCenters")]
        public JsonResult GetResearchCenters(string term)
        {
            try
            {
                var aliases = db.ResearchCenters
                                .Where(x => x.LocationName.Contains(term) && !x.IsDeleted)
                                .OrderBy(x => x.LocationName)
                                .Take(20).ToList()
                                .Select(x => new
                                {
                                    id = x.CenterId,
                                    label = $"{x.LocationName} - {(string.IsNullOrWhiteSpace(x.CityImported) ? "No City Imported" : x.CityImported)}, {(string.IsNullOrWhiteSpace(x.StateImported) ? "No State Imported" : x.StateImported)},{(string.IsNullOrWhiteSpace(x.Country) ? "No Country Imported" : x.Country)}",
                                    value = x.LocationName
                                }).ToArray();

                return Json(aliases, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
    }
}
