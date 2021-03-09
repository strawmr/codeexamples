using Microsoft.AspNet.Identity;
using MvcSiteMapProvider;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TrialScout.DAL.Context;
using TrialScout.DAL.Entities;
using TrialScout.Web.ViewModels;
using static TrialScout.Web.Localization.LocalizedRoute;
using System.Net; // webclient 

namespace TrialScout.Web.Controllers
{
    [Authorize]
    [RoutePrefix("Ratings/RateResearchCenter")]
    public class RatingsController : Controller
    {
        #region Action Results

        private string getIPStat(string IP)
        {
            try
            {
                string url = ConfigurationManager.AppSettings["IPStatBaseURL"] + IP + "?access_key=" + ConfigurationManager.AppSettings["IPStatAPIKey"];
                string response = (new WebClient()).DownloadString(url);
                return response;
            }
            catch
            {
                return null;
            }
        }


        [AllowAnonymous]
        [Route("")]
        [HttpGet]
        public ActionResult RateResearchCenter(Guid? ratingId, string alias, Guid? centerId, string utm_medium)
         {
            if (utm_medium != "" && utm_medium == "SCRS")
            {
                try 
                { 
                    using (TrialScoutContext db = new TrialScoutContext())
                    {
                        BackLinkAnalytics analytics = new BackLinkAnalytics();
                        analytics.AnalyticsId = Guid.NewGuid();
                        analytics.ReferrerUrl = Request.ServerVariables["HTTP_REFERER"]; 
                        analytics.IPStats = getIPStat(Request.ServerVariables["REMOTE_ADDR"]);
                        analytics.CenterId = centerId; 
                        analytics.AliasName = alias;
                        db.BackLinkAnalytics.Add(analytics);
                        db.SaveChanges();
                    }
                }
                catch (Exception e) {
                    Globals.LogMessage(Guid.NewGuid(), e.Message.ToString(), Globals.LogLevels.Critical);
                }
            }

            RateTrialViewModel viewModel = new RateTrialViewModel();
            using (TrialScoutContext db = new TrialScoutContext())
            {
                if (ratingId.HasValue)
                {
                    ResearchCenterRating rating = db.ResearchCenterRatings.Include("ResearchCenter").Where(r => r.ReviewId == ratingId).FirstOrDefault();
                    viewModel.CenterId = rating.CenterId;
                    viewModel.ResearchCenter = rating.ResearchCenter.LocationName;
                    viewModel.ResearchCenterDisplayName = alias ?? rating.ResearchCenter.LocationName;
                    viewModel.InitialRating = rating.OverallRating;
                    viewModel.RatingId = ratingId;
                    viewModel.AliasName = alias;
                    viewModel.AliasId = rating.AliasId;
                }

                if(centerId != null && centerId != Guid.Empty && !String.IsNullOrEmpty(alias))
                {
                    ResearchCenterAlias researchCenterAlias = db.ResearchCenterAliases.Include("ResearchCenter").Where(rca => rca.CenterId == centerId && rca.AliasName == alias).FirstOrDefault();

                    if(researchCenterAlias != null)
                    {
                        viewModel.ResearchCenter = researchCenterAlias.ResearchCenter.LocationName;
                        viewModel.CenterId = centerId.Value;
                        viewModel.AliasName = alias;
                        viewModel.AliasId = researchCenterAlias.AliasId;
                        viewModel.ResearchCenterDisplayName = alias ?? viewModel.ResearchCenter;
                    }
                }
            }

            return View(viewModel);
        }

        [AllowAnonymous]
        [Route("")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RateResearchCenter(RateTrialViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    using (TrialScoutContext db = new TrialScoutContext())
                    {
                        ResearchCenterRating researchCenterRating = null;
                        //ResearchCenter researchCenter = null;
                        if (!viewModel.RatingId.HasValue)
                        {                        
                            ResearchCenterAlias researchCenterAlias = db.ResearchCenterAliases.Include("ResearchCenter").Where(r => r.CenterId == viewModel.CenterId && r.AliasId == viewModel.AliasId).FirstOrDefault();
                            researchCenterRating = new ResearchCenterRating();
                            if (researchCenterAlias == null)
                            {
                               if(viewModel.CenterId == null)
                                {
                                    ModelState.AddModelError("ResearchCenter", "Research Center could not be found");
                                }
                               
                                researchCenterRating.CenterId = viewModel.CenterId;
                            }
                            else
                            {
                                //if(researchCenterAlias.ResearchCenter.SubscriptionLevelId == new Guid("05613ED1-6F80-4DF7-8EEF-D14778C3ED2C"))
                                // {
                                //    string researchCenterAdmin = db.ResearchCenters.Where(r => r.CenterId == viewModel.CenterId && !r.IsDeleted).FirstOrDefault().UserId.ToString();

                                //   string researchCenterName = db.ResearchCenters.Where(r => r.CenterId == viewModel.CenterId && !r.IsDeleted).FirstOrDefault().LocationName;

                                //     string adminEmail = db.Database.SqlQuery<string>("Select Email from dbo.AspNetUsers where Id = @userid", new SqlParameter("@userid", researchCenterAdmin)).FirstOrDefault();

                                //    
                                // }

                                Task.Run(() => RatingAlertEmail(viewModel.OverallRating, viewModel.AliasName, null));

                                researchCenterRating.CenterId = (Guid) researchCenterAlias.CenterId;
                                researchCenterRating.AliasId = (researchCenterAlias.AliasId == null) ? (Guid?)null : researchCenterAlias.AliasId;
                            }

                            researchCenterRating.ReviewId = Guid.NewGuid();
                            researchCenterRating.CreatedOn = DateTime.Now;
                            researchCenterRating.IsApproved = true;
                        }
                        else
                            researchCenterRating = db.ResearchCenterRatings.Where(r => r.ReviewId == viewModel.RatingId).FirstOrDefault();

                        researchCenterRating.OverallRating = viewModel.OverallRating.Value;

                        if (!viewModel.RatingId.HasValue)
                            db.ResearchCenterRatings.Add(researchCenterRating);
                        
                        //ResearchCenterAlias rca = db.ResearchCenterAliases.Where(a => a.AliasName == viewModel.AliasName && a.CenterId == viewModel.CenterId && !a.IsDeleted).FirstOrDefault();
                        //Guid? aliasId = rca?.AliasId;

                        //researchCenterRating.AliasId = aliasId;

                        db.SaveChanges();

                        return RedirectToAction("verifiedRating", new { ratingId = researchCenterRating.ReviewId, alias = viewModel.AliasName });
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return View(viewModel);
        }


        [AllowAnonymous]
        [Route("VerifiedRating")]
        [HttpGet]
        public ActionResult VerifiedRating(Guid ratingId, string alias)
        {
            VerifiedRatingViewModel viewModel = new VerifiedRatingViewModel();

            ViewBag.TrialYear = new SelectList(Enumerable.Range(1980, (DateTime.Today.Year-1980) + 1).Reverse().Select(x =>

                new SelectListItem()
                {
                    Text = x.ToString(),
                    Value = x.ToString()
                }), "Value", "Text");

            ViewBag.PastOrPresentPatient = new SelectList(
                new List<SelectListItem>
                {
                 new SelectListItem { Text = Resources.Resources.PastOption, Value = "Past"},
                 new SelectListItem { Text =Resources.Resources.PresentOption, Value = "Present"},
                }, "Value", "Text");

            ViewBag.CaregiverList = new SelectList(
                new List<SelectListItem>
                {
                 new SelectListItem { Text = Resources.Resources.ParticipantOption, Value = "Participant"},
                 new SelectListItem { Text = Resources.Resources.CaregiverOption, Value = "Caregiver"},
                }, "Value", "Text");

            viewModel.RatingId = ratingId;
            

            return View(viewModel);
        }


        [AllowAnonymous]
        [Route("VerifiedRating")]
        [HttpPost]
        public ActionResult VerifiedRating(VerifiedRatingViewModel viewModel)
        {
            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    ResearchCenterRating rating = db.ResearchCenterRatings.Include("ResearchCenter").Where(r => r.ReviewId == viewModel.RatingId).FirstOrDefault();

                    if (viewModel.NameOfTrial != null)
                        rating.TrialName = viewModel.NameOfTrial;
                    if (viewModel.TrialYear != null)
                        rating.TrialYear = Convert.ToInt16(viewModel.TrialYear);
                    if (viewModel.PastOrPresent != null)
                        rating.PastOrPresent = viewModel.PastOrPresent;
                    if (viewModel.Caregiver!= null)
                        rating.Caregiver = viewModel.Caregiver;
                    if (viewModel.ExplainRating.HasValue)
                        rating.ExplanationRating = viewModel.ExplainRating.Value;
                    if (viewModel.ListenRating.HasValue)
                        rating.ListeningRating = viewModel.ListenRating.Value;
                    if (viewModel.OfficeRating.HasValue)
                        rating.OfficeRating = viewModel.OfficeRating.Value;
                    if (viewModel.WaitTimeRating.HasValue)
                        rating.WaitTimeRating = viewModel.WaitTimeRating.Value;

                    db.SaveChanges();

                    return RedirectToAction("authenticatedRating", new { ratingId = rating.ReviewId });
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return View(viewModel);
        }


        [AllowAnonymous]
        [Route("AuthenticatedRating")]
        [HttpGet]
        public ActionResult AuthenticatedRating(Guid ratingId)
        {
            AuthenticatedReviewViewModel viewModel = new AuthenticatedReviewViewModel();
            viewModel.RatingId = ratingId;

            return View(viewModel);
        }


        [HttpGet]
        public ActionResult EditResponse(Guid reviewId)
        {
            using (TrialScoutContext db = new TrialScoutContext())
            {
                ResearchCenterRating rating = db.ResearchCenterRatings.Where(r => r.ReviewId == reviewId).FirstOrDefault();
                return View(new EditResponseViewModel() { ReviewId = reviewId, Comment = rating.Comment, CreatedOn = rating.CreatedOn, Response = rating.Response });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditResponse(EditResponseViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    using (TrialScoutContext db = new TrialScoutContext())
                    {
                        ResearchCenterRating rating = db.ResearchCenterRatings.Where(r => r.ReviewId == viewModel.ReviewId).FirstOrDefault();
                        ResearchCenter researchCenter = db.ResearchCenters.Where(r => r.CenterId == rating.CenterId).FirstOrDefault();
                        if (researchCenter.UserId == new Guid(User.Identity.GetUserId()))
                        {
                            rating.IsResponseApproved = false;
                            rating.ResponseApprovedBy = null;
                            rating.ResponseApprovedOn = null;
                            rating.Response = viewModel.Response;
                            rating.ResponseCreatedBy = new Guid(User.Identity.GetUserId());
                            rating.ResponseCreatedOn = DateTime.Now;

                            db.SaveChanges();
                        }

                        return RedirectToAction("", "MyResearchCenter");
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return View(viewModel);
        }

        #endregion

        #region JSON

        [AllowAnonymous]
        [Route("InitialTrialRating")]
        [HttpPost]
        public JsonResult InitialTrialRating(string researchCenterName, Guid centerId, int overallRating)
        {      
            try
            {
                if (centerId != Guid.Empty && overallRating > 0 && overallRating <= 5)
                {
                    using (TrialScoutContext db = new TrialScoutContext())
                    {

                        ResearchCenterAlias rca = db.ResearchCenterAliases.Where(a => a.AliasName == researchCenterName && centerId == a.CenterId).FirstOrDefault();
                        if (rca == null)
                            return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                        ResearchCenterRating researchCenterRating = new ResearchCenterRating();

                        //automatically approve reviews that don't have comments
                        researchCenterRating.CenterId = (Guid) rca.CenterId;
                        researchCenterRating.OverallRating = overallRating;
                        researchCenterRating.IsApproved = true; 
                        researchCenterRating.ReviewId = Guid.NewGuid();
                        researchCenterRating.CreatedOn = DateTime.Now;
                        researchCenterRating.AliasId = rca.AliasId;


                        // should be activated when business development gives green light on sending automatic emails to side admins
                        //if (rca.ResearchCenter.SubscriptionLevelId == new Guid("05613ED1-6F80-4DF7-8EEF-D14778C3ED2C"))
                        //{
                        //    string researchCenterAdmin = db.ResearchCenters.Where(r => r.CenterId == centerId && !r.IsDeleted).FirstOrDefault().UserId.ToString();

                        //    //string adminEmail = db.Database.SqlQuery<string>("Select Email from dbo.AspNetUsers where Id = @userid", new SqlParameter("@userid", researchCenterAdmin)).FirstOrDefault();

                        //    Task.Run(() => RatingAlertEmail(overallRating, researchCenterName, adminEmail));
                        //}

                        Task.Run(() => RatingAlertEmail(overallRating, researchCenterName, null));

                        db.ResearchCenterRatings.Add(researchCenterRating);

                        db.SaveChanges();

                        return Json(new { success = true, ratingId = researchCenterRating.ReviewId }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("AuthenticatedRating")]
        [HttpPost]
        public JsonResult AuthenticatedRating(AuthenticatedReviewViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    using (TrialScoutContext db = new TrialScoutContext())
                    {
                        ResearchCenterRating rating = db.ResearchCenterRatings.Include("ResearchCenter").Where(r => r.ReviewId == viewModel.RatingId).FirstOrDefault();

                        if (viewModel.EmailAddress != null && viewModel.EmailAddress.Trim() != "" &&  viewModel.FirstName != null && viewModel.FirstName.Trim() != "" &&  viewModel.LastName != null && viewModel.LastName.Trim() != "")
                            rating.Comment = viewModel.Comment;

                        if (rating.Comment != null && rating.Comment.Trim() != "")
                           rating.IsApproved = false;

                        rating.EmailAddress = viewModel.EmailAddress;
                        rating.Phone = viewModel.Phone;
                        rating.FirstName = viewModel.FirstName;
                        rating.LastName = viewModel.LastName;
                        rating.ShouldContact = viewModel.ShouldContact;

                        db.SaveChanges();

                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }
           
            return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }


        [AllowAnonymous]
        [Route("FollowUp")]
        [HttpPost]
        public JsonResult FollowUp(FollowUpRatingViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    using (TrialScoutContext db = new TrialScoutContext())
                    {
                        ResearchCenterRating rating = db.ResearchCenterRatings.Where(r => r.ReviewId == viewModel.RatingId).FirstOrDefault();

                        if (viewModel.TrialScoutRating.HasValue)
                        {
                            rating.HelpfulRating = (Int16)viewModel.TrialScoutRating.Value;
                            db.SaveChanges();
                        }

                    }
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Functions

        public static async void RatingAlertEmail(int? overallRating, string researchCenterName, string adminEmail)
        {
            List<string> recipients = null;
            string templateId = null;
            dynamic replacements = new ExpandoObject();
            string subject = ConfigurationManager.AppSettings["RatingAlertSubject"];


            replacements.name = researchCenterName;

            switch (overallRating)
            {
                case 1:
                   templateId = "d-8e5644fd17304985b3fe35d3012c87f6";
                    break;
                case 2:
                    templateId = "d-b3a16baf1c8b468d86b1a06733acb465";
                    break;
                case 3:
                    templateId = "d-a89a576820b542418aeb95fdc6fd4704";
                    break;
                case 4:
                    templateId = "d-2f67da7d9c6c43659258123c65ceb535";
                    break;
                case 5:
                    templateId = "d-13de623aa3944418823579d0be651ce6";
                    break;
                default:
                    //do nothing;
                    break; 

            }
            
            recipients = ConfigurationManager.AppSettings["RatingAlertRecipients"].Split(';').ToList();

            // recipients = new List<string> { adminEmail, helloEmail }; TODO: When business rules changes to message admins

           // recipients = new List<string> { helloEmail };

            await Globals.SendEmailTemplate(subject, templateId, recipients,(object) replacements);
        }


        #endregion
    }
}