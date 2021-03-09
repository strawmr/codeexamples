using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TrialScout.DAL.Context;
using TrialScout.DAL.Entities;
using TrialScout.Web.Classes;
using TrialScout.Web.Factory;
using TrialScout.Web.ViewModels;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Web.UI.WebControls.Expressions;
using Microsoft.SqlServer.Server;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Data.Entity.Validation;
using MvcSiteMapProvider;

namespace TrialScout.Web.Controllers
{
    [Authorize]
    public class ResearchSiteController : Controller
    {
        // GET: ResearchCenterAlias
        
        [Route("ResearchSite/{aliasId}/{alias}/{condition}/{location}")]
        [Route("ResearchSite/{aliasId}/{condition}/{location}")]
        [Route("ResearchSite/{aliasId}/{alias}")]
        [Route("ResearchSite/{aliasId}")]
        [Route("ResearchSite")]
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index(string condition, string location, int? proximity, string alias, Guid? aliasId, Guid? centerId)
        {
            using (TrialScoutContext db = new TrialScoutContext())
            {
                ResearchCenterAlias researchCenterAlias = null;

                if (aliasId != null)
                    researchCenterAlias = db.ResearchCenterAliases.FirstOrDefault(r => r.AliasId == aliasId && r.IsDeleted == false);

                if (centerId != null && !string.IsNullOrEmpty(alias))
                {
                    researchCenterAlias = db.ResearchCenterAliases.FirstOrDefault(r => r.CenterId == centerId && r.IsDeleted == false && r.AliasName == alias);
                }

                if (researchCenterAlias == null)
                    return RedirectToAction("Index", "Home");

                return View(BuildResearchCenterAliasDetailsViewModel(researchCenterAlias, db, condition, location, proximity));
            }
        }

        [Route("ResearchSite/EditAlias/{id}")]
        [HttpGet]
        public ActionResult EditAlias(Guid id, string returnController, string returnAction)
        {
            var factory = new EditResearchCenterViewModelFactory();
            var viewModel = factory.GetResearchCenterAliasViewModel(id, new Guid(User.Identity.GetUserId()), User.IsInRole("Administrator"));

            if (viewModel == null && !User.IsInRole("Administrator"))
                return RedirectToAction("/Home/Index");

            ViewBag.ReturnController = returnController;
            ViewBag.ReturnAction = returnAction;

            return View(viewModel);
        }

        [Route("ResearchSite/EditAlias/{id}")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditAlias(EditResearchCenterViewModel viewModel, List<Guid> departmentIds,
                 HttpPostedFileBase logoImage, HttpPostedFileBase profileImage, List<HttpPostedFileBase> secondaryImages, string returnController, string returnAction)
        {

            using (TrialScoutContext db = new TrialScoutContext())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        ResearchCenterAlias researchCenterAlias = db.ResearchCenterAliases.FirstOrDefault(r => r.AliasId == viewModel.AliasId && r.IsDeleted == false && r.CenterId != null);
                        // changesAffectBillingOptions = researchCenter.IncreaseAdRadius != viewModel.IncreaseAdRadius;

                        if (researchCenterAlias != null)
                        {
                            researchCenterAlias.Address = viewModel.Address;
                            researchCenterAlias.City = viewModel.City;
                            researchCenterAlias.Phone = viewModel.Phone;
                            researchCenterAlias.SearchDescription = viewModel.SearchDescription;
                            researchCenterAlias.State = viewModel.State;
                            researchCenterAlias.Country = viewModel.Country;
                            researchCenterAlias.Summary = viewModel.Summary;
                            researchCenterAlias.IncreaseAdRadius = viewModel.IncreaseAdRadius;

                            if (User.IsInRole("Administrators"))
                                researchCenterAlias.IsTop50 = viewModel.IsTop50;
                            researchCenterAlias.IsSCRS = viewModel.IsSCRS;


                            if (viewModel.Url != null && viewModel.Url != researchCenterAlias.Url)
                            {
                                string url = viewModel.Url;
                                if (!url.Trim().ToLower().StartsWith("http"))
                                    url = "https://" + url;

                                researchCenterAlias.Url = url;
                            }

                            researchCenterAlias.Zip = viewModel.Zip;

                            //save images
                            AzureImageStorage imageStore = new AzureImageStorage(
                                ConfigurationManager.AppSettings["TrialScoutBlobUri"],
                                ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                                ConfigurationManager.AppSettings["ResearchCenterBlobKey"],
                                ConfigurationManager.AppSettings["ResearchCenterContainerName"]);

                            if (logoImage != null)
                            {
                                try
                                {
                                    //delete the old image before uploading a new one
                                    //if this fails, we still want to continue
                                    if (researchCenterAlias.LogoBlobId.HasValue)
                                        imageStore.DeleteImage(researchCenterAlias.LogoBlobId.Value);
                                }
                                catch (Exception ex)
                                {
                                    Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null,
                                        Globals.LogLevels.Error);
                                }

                                Guid blobId = imageStore.SaveImage(logoImage.InputStream,
                                    AzureImageStorage.ImageSizes.ResearchCenterLogo);
                                researchCenterAlias.LogoBlobId = blobId;
                            }

                            if (profileImage != null)
                            {
                                //delete the old image before uploading a new one
                                if (researchCenterAlias.ProfileImageBlobId.HasValue)
                                    imageStore.DeleteImage(researchCenterAlias.ProfileImageBlobId.Value);

                                Guid blobId = imageStore.SaveImage(profileImage.InputStream,
                                    AzureImageStorage.ImageSizes.ResearchCenterPrimaryImage);
                                researchCenterAlias.ProfileImageBlobId = blobId;
                            }

                            int imageCount =
                                db.ResearchCenterAliasImages.Count(r => r.AliasId == researchCenterAlias.CenterId);

                            if (imageCount < 4)
                            {
                                if (secondaryImages != null && secondaryImages.Count() > 0)
                                {
                                    foreach (HttpPostedFileBase secondaryImage in secondaryImages)
                                    {
                                        if (secondaryImage != null)
                                        {
                                            Guid blobId = imageStore.SaveImage(secondaryImage.InputStream,
                                                AzureImageStorage.ImageSizes.ResearchCenterSecondaryImage);
                                            ResearchCenterAliasImages image = new ResearchCenterAliasImages();

                                            image.BlobId = blobId;
                                            image.AliasId = researchCenterAlias.AliasId;
                                            image.CreatedBy = new Guid(User.Identity.GetUserId());
                                            image.CreatedOn = DateTime.Now;
                                            image.AliasImageId = Guid.NewGuid();

                                            db.ResearchCenterAliasImages.Add(image);

                                            imageCount++;

                                            if (imageCount >= 4)
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        //save departments
                        List<ResearchCenterAliasDepartments> existingDepartments = db.ResearchCenterAliasDepartments.Where(r => r.AliasId == viewModel.AliasId).ToList();

                        if (existingDepartments != null && existingDepartments.Count() > 0)
                            db.ResearchCenterAliasDepartments.RemoveRange(existingDepartments);

                        if (departmentIds != null && departmentIds.Count() > 0)
                        {
                            foreach (Guid departmentId in departmentIds)
                            {
                                ResearchCenterAliasDepartments department = new ResearchCenterAliasDepartments();

                                department.AliasId = viewModel.AliasId;
                                department.CreatedBy = new Guid(User.Identity.GetUserId());
                                department.CreatedOn = DateTime.Now;
                                department.DepartmentId = departmentId;
                                department.ResearchCenterAliasDepartmentId = Guid.NewGuid();

                                db.ResearchCenterAliasDepartments.Add(department);
                            }
                        }

                        db.SaveChanges();

                        if (string.IsNullOrWhiteSpace(returnAction))
                            return RedirectToAction("", "ResearchCenter", new { id = viewModel.CenterId, alias = viewModel.AliasName });
                        else
                            return RedirectToAction(returnAction, returnController);
                    }
                }
                catch (Exception ex)
                {
                    Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
                }

                viewModel.States = new List<State>();
                viewModel.States = db.States.OrderBy(s => s.StateName).ToList();

                return View(viewModel);
            }
        }


        // Contact Form - Post (to get below)
        [Route("ResearchSite/ContactUsSubmit/")]
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ContactUsSubmit(StudyContactForm model)
        {
            CaptchaResponse response = Globals.ValidateCaptcha(Request["g-recaptcha-response"]);

            String redirectURL = model.ReqURL;
            if (response.Success && ModelState.IsValid)
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {

                    try
                    {
                        //DB Setup
                        StudyContactForm formSubmit = new StudyContactForm();
                        Guid? aliasId = model.AliasId; // always set to null
                        // Guid? StudyId = null; // always set to null
                        string rcBody = "";
                        string returnReceiptMessage = "";


                        formSubmit.ContactId = Guid.NewGuid();

                        formSubmit.ContactIP = model.ContactIP;
                        formSubmit.LinkID = model.LinkID;
                        formSubmit.FormName = model.FormName;
                        formSubmit.AliasName = model.AliasName;
                         formSubmit.AliasId = aliasId;
                        formSubmit.CreatedOn = model.CreatedOn;
                         formSubmit.Location = model.Location;
                         formSubmit.Proximity = model.Proximity;
                         formSubmit.Condition = model.Condition;
                        formSubmit.ReqURL = model.ReqURL;
                        formSubmit.ContactName = model.ContactName;
                        formSubmit.EmailAddress = model.EmailAddress;
                        formSubmit.PhoneNumber = model.PhoneNumber;

                        formSubmit.ContactMessage = model.ContactMessage;
                        formSubmit.IPStat = Globals.GetIPStat(model.ContactIP); // JSON return
                        db.StudyContactForms.Add(formSubmit);
                        db.SaveChanges();

                        // REDIRECT TY MESSAGE SET
                        string emailSubjectType = "Alias";
                        returnReceiptMessage = model.AliasName;
                        if(!string.IsNullOrEmpty(model.Location) && !string.IsNullOrEmpty(model.Condition))
                        {
                            redirectURL = redirectURL = redirectURL + "&callback=ContactUsAlias";
                        }
                        else
                        {
                            redirectURL = redirectURL = redirectURL + "?callback=ContactUsAlias";

                        }

                        rcBody += "<font size=2><B>Research Center :</b> " + model.AliasName + "</font><br />";

                        // show ad link that was clicked
                        if (model.LinkID != null) // ad was clicked
                            rcBody += "<b>Ad Click?</b> Yes<br />";

                           rcBody += "<font size=2><b>Condition (Search) :</b> " + model.Condition + "</font><hr />";
                          rcBody += "<b>Alias Name :</b> " + model.AliasName + "<br />";
                           rcBody += "<b>Location (Search) :</b> " + model.Location + "<br />";
                           rcBody += "<b>Proximity (Search) :</b> " + model.Proximity + "<br />";



                        rcBody += "<b>Date/Time :</b> " + model.CreatedOn + "<br />";
                        rcBody += "<b>Origin URL :</b> " + model.ReqURL + "<br />";

                        string emailSubject = ConfigurationManager.AppSettings["ResearchCenterContactUsSubject"] + emailSubjectType;
                        string receipient = "<hr /><h2>" + model.ContactName + "<br /><a href=\"mailto:" + model.EmailAddress + "?subject=" + emailSubject + "\">" + model.EmailAddress + "</a><br/>" + model.PhoneNumber + "</h2><br />";
                        receipient += "<b>Message</b>: " + model.ContactMessage + "<br />";
                        string receipients = ConfigurationManager.AppSettings["ResearchCenterContactUsRecipients"];
                        string salutation = "<h3>Thanks," + "<br />" + "<b>TrialScout</b>" + "</h3><br />";

                        string emailBody = string.Format(ConfigurationManager.AppSettings["ResearchCenterContactUsMessage"],
                                                          receipient, rcBody, salutation);

                        if (ConfigurationManager.AppSettings["RCContactUsSendConfirmationToAdmin"] == "True")
                        {
                            var cId = new SqlParameter("@clientId", model.ResearchCenterId.ToString());
                            string SiteAdminEmail = db.Database.SqlQuery<string>("EXEC GetAdminEmail @clientId", cId).SingleOrDefault<string>();
                            if (SiteAdminEmail != null)
                            {
                                receipients += SiteAdminEmail + ";"; // this sets up another email to go to the admin
                            }
                        }

                        Globals.SendEmail(emailSubject, emailBody, receipients.Split(';').ToList()); // hello kitty @ trialscout

                        // End User Message - ConfigurationManager.AppSettings["RCContactUsSendConfirmation"]  will trigger email
                        if (model.EmailAddress != null && ConfigurationManager.AppSettings["RCContactUsSendConfirmation"] == "True")
                        {
                            string emailConfirmSubject = ConfigurationManager.AppSettings["RCContactUsConfirmationSubject"];

                            string rcConfirmBody = "<b>" + returnReceiptMessage + "</b>";
                            string rcConfirmSalutation = "<p><b>Thanks,<br>The TrialScout Team</p>";

                            // because a list is required 
                            model.EmailAddress += ";";

                            // merge the soups 
                            string emailConfirmationBody = String.Format(ConfigurationManager.AppSettings["RCContactUsConfirmationMessage"],
                                                         "<p>" + model.ContactName + "</p>", returnReceiptMessage, rcConfirmSalutation);

                            Globals.SendEmail(emailConfirmSubject, emailConfirmationBody, model.EmailAddress.Split(';').ToList()); // end user @ trialscout

                        }
                        // send email to Alias Admin - if they exist and we allow it.
                        return Redirect(redirectURL);
                    }
                    catch (Exception e)
                    {
                        string error = "Error: " + response.ErrorMessage + Environment.NewLine;
                        error += "User IP: " + model.ContactIP + Environment.NewLine;
                        error += "Request URL: " + redirectURL + Environment.NewLine;

                        Globals.LogError(new Guid(User.Identity.GetUserId()), e, error, Globals.LogLevels.Error);
                        return Redirect(redirectURL);

                    }
                }
            }
            else if (response.Success == false)
            {

                // SOFT ERROR, e.message is read only and LogError requires an object, you get this
                Exception e = new Exception();


                string error = "Google Recaptcha Error: " + response.ErrorMessage + Environment.NewLine;
                error += "User IP: " + model.ContactIP + Environment.NewLine;
                error += "Request URL: " + redirectURL + Environment.NewLine;

                Globals.LogError(new Guid(User.Identity.GetUserId()), e, error, Globals.LogLevels.Error);

            }

            return RedirectToAction("Index", "Home");
        }
        [Route("ResearchSite/ContactUs/{AliasId}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ContactUs(Guid? aliasId)
        {
            StudyContactForm viewModel = new StudyContactForm();
            ViewData.Add(new KeyValuePair<string, object>("FormSource", "ResearchSite"));
            string fullURL = Request.UrlReferrer.AbsoluteUri;
            Uri reqUrl = new Uri(fullURL);

            try
            {
                if (reqUrl.Segments.Length > 4)
            {
                string location = reqUrl.Segments[5].Replace("%20", " ");
                string condition = reqUrl.Segments[4]?.Replace("/","").Replace("%20", " ");
                string proximity = HttpUtility.ParseQueryString(reqUrl.Query).Get("proximity");

                viewModel.Location = location;
                viewModel.Proximity = proximity;
                viewModel.Condition = condition;
            }

            string aliasName = reqUrl.Segments[2]?.Replace("/", "").Replace("%20", " ");
           


                // TO-DO: IP -> regional tracking data we can write analytics against - https://ipdata.co/pricing.html
                viewModel.ContactIP = Globals.GetExternalIp();

                    viewModel.FormName = "ContactUsFromAlias";
                    viewModel.AliasName = aliasName;
                viewModel.AliasId = aliasId;

                // Generic on All Forms
                viewModel.CreatedOn = DateTime.Now;
                viewModel.ReqURL = reqUrl.ToString();


                return PartialView("_ContactUs", viewModel);
            }
            catch (Exception e)
            {
                // log e
                return null;
            }
        }


        [AllowAnonymous]
        [Route("ResearchSite/GetAliases")]
        [HttpGet]
        public JsonResult GetAliases(string term)
        {
            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    var researchCenters = db.Database.SqlQuery<ResearchCenterSearchViewModel>(
                        "EXEC GetResearchCenterAliasSearch @input",
                        new SqlParameter("@input", term)).ToList();

                    return Json(researchCenters, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Globals.LogError(null, ex, null, Globals.LogLevels.Error);

            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("ResearchSite/GetAliasesByCenter")]
        [HttpGet]
        public JsonResult GetAliasesByCenter(Guid centerId)
        {
            List<string> aliases = new List<string>();

            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    if (centerId != null)
                    {
                        aliases = db.ResearchCenterAliases.Where(r => !r.IsDeleted && r.CenterId == centerId).Select(r => r.AliasName).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(null, ex, ex.Message, Globals.LogLevels.Debug);
                return Json(null);
            }

            return Json(aliases, JsonRequestBehavior.AllowGet);
        }


        private ResearchCenterAliasDetailsViewModel BuildResearchCenterAliasDetailsViewModel(ResearchCenterAlias researchCenterAlias, TrialScoutContext db, string condition, string location, int? proximity)
        {
            ResearchCenterAliasDetailsViewModel viewModel = new ResearchCenterAliasDetailsViewModel();

            viewModel.Alias = researchCenterAlias.AliasName;
            viewModel.Condition = condition;
            if (researchCenterAlias.CenterId != null) viewModel.CenterId = (Guid)researchCenterAlias.CenterId;

            ResearchCenter researchCenter = db.ResearchCenters.FirstOrDefault(a => a.CenterId == viewModel.CenterId && !a.IsDeleted);

            viewModel.SearchDescription = researchCenterAlias.SearchDescription;
            viewModel.IsPublished = researchCenterAlias.IsPublished;

            viewModel.AliasId = researchCenterAlias.AliasId;
            viewModel.IsAliasTop50 = researchCenterAlias.IsTop50;
            viewModel.IsAliasSCRS = researchCenterAlias.IsSCRS;

            viewModel.ProfileImageBlobUri = "/Content/images/overlay@2x.png";
            viewModel.LogoBlobUri = "/Content/images/TS-Logomark-FullColor.png";
            //viewModel.SubscriptionLevel = researchCenterAlias.SubscriptionLevel;



            if (Request.IsAuthenticated)
                viewModel.LoggedInUserId = new Guid(User.Identity.GetUserId());

            viewModel.ReviewSortOptions = new Dictionary<string, string>() {
                { "date", "Date(default)" },
                { "reviewer", "Rater Comments" },
                { "ratingsDesc", "High Ratings" },
                { "ratingsAsc", "Low Ratings"} };


            //Process Advanced Trial Search
            AdvancedTrialSearchViewModel advancedTrialSearchVm = new AdvancedTrialSearchViewModel()
            {
                Condition = viewModel.Condition ?? "",
                Age = null
            };

            viewModel.TrialSearch = advancedTrialSearchVm;

            if (researchCenter != null)
            {
                viewModel.IsClaimed = researchCenter.UserId.HasValue;
                viewModel.UserId = researchCenter.UserId ?? Guid.Empty;
                viewModel.LocationName = researchCenter.LocationName;
            }

            viewModel.Ratings = db.ResearchCenterRatings.Where(r => r.AliasId == viewModel.AliasId && r.IsApproved && !r.IsDeleted)
                                      .Select(r => new ResearchCenterRatingsViewModel()
                                      {
                                          AliasId = r.AliasId,
                                          CenterId = r.CenterId,
                                          Comment = r.Comment,
                                          CreatedDate = r.CreatedOn,
                                          ReviewId = r.ReviewId,
                                          HasEmail = (r.EmailAddress != null && r.EmailAddress.Trim() != ""),
                                          HasFirstName = (r.FirstName != null && r.FirstName.Trim() != ""),
                                          HasLastName = (r.LastName != null && r.LastName.Trim() != ""),
                                          HasTrialName = (r.TrialName != null && r.TrialName.Trim() != ""),
                                          HasTrialYear = (r.TrialYear != null),
                                          Rating = r.OverallRating,
                                          ReponseDate = r.IsResponseApproved ? r.ResponseCreatedOn : null,
                                          Response = r.IsResponseApproved ? r.Response : null,
                                          ResponderName = r.IsResponseApproved ? viewModel.LocationName : null,
                                          HasExplanationRating = r.ExplanationRating.HasValue,
                                          HasListeningRating = r.ListeningRating.HasValue,
                                          HasWaitTimeRating = r.WaitTimeRating.HasValue,
                                          HasOfficeRating = r.OfficeRating.HasValue
                                      })
                                      //.OrderByDescending(r => r.HasEmail && r.HasExplanationRating == true && r.HasListeningRating == true && r.HasWaitTimeRating == true && r.HasOfficeRating == true)
                                      //.ThenByDescending(r => r.HasExplanationRating == true && r.HasListeningRating == true && r.HasWaitTimeRating == true && r.HasOfficeRating == true)
                                      //.ThenByDescending(r => r.CreatedDate)
                                      .OrderByDescending(r => r.CreatedDate)
                                      .Take(viewModel.ReviewPageSize).ToList();


            //Alias Ranking
            ResearchCenterAliasRanking rcar = db.ResearchCenterAliasRankings.Where(ra => ra.AliasId == viewModel.AliasId).FirstOrDefault();
            viewModel.AliasRating = rcar?.RankingRoundedToHalf ?? 3;
            viewModel.AliasRatingTenths = rcar?.RankingRoundedToTenth ?? 3;

            viewModel.TotalRatingCount = db.ResearchCenterRatings.Count(r => r.CenterId == viewModel.CenterId && r.IsApproved);

            viewModel.Top10Conditions = db.Database.SqlQuery<Top10ConditionsViewModel>("Exec Top_10_Conditions @alias_ID, @center_ID", new SqlParameter("@alias_ID", viewModel.AliasId), new SqlParameter("@center_ID", viewModel.CenterId)).ToList();

            viewModel.Address = researchCenterAlias.Address;
            viewModel.City = researchCenterAlias.City;
            viewModel.State = researchCenterAlias.State;
            viewModel.Zip = researchCenterAlias.Zip;
            viewModel.Phone = researchCenterAlias.Phone;


            var ScoreCard = new RatingScoreCardViewModel();

            //scorecard 
            ScoreCard = db.Database.SqlQuery<RatingScoreCardViewModel>(
                "exec GetRatingScoreCard @aliasId, @centerId",
                new SqlParameter("aliasId", viewModel.AliasId),
                new SqlParameter("centerId", viewModel.CenterId)).FirstOrDefault();

            if (ScoreCard != null)
            {
                viewModel.ScoreCard = new RatingScoreCardViewModel()
                {
                    Ones = ScoreCard.Ones,
                    Twos = ScoreCard.Twos,
                    Threes = ScoreCard.Threes,
                    Fours = ScoreCard.Fours,
                    Fives = ScoreCard.Fives,
                    TotalRatings = ScoreCard.TotalRatings,
                    RankingRoundedToHalf = rcar?.RankingRoundedToHalf,
                    RankingRoundedToTenth = rcar?.RankingRoundedToTenth,
                    AliasId = viewModel.AliasId,
                    CenterId = viewModel.CenterId,
                    AliasName = viewModel.Alias,
                    LocationName = viewModel.LocationName
                };
            }

            if ((condition != null && condition.Trim() != "") &&
                (location != null && location.Trim() != ""))
            {

                viewModel.SearchLocation = location;
                viewModel.SearchProximity = proximity ?? 50;

                //create view model for shared use of call to CTG API
                SearchTrialsViewModel trialsVM = new SearchTrialsViewModel();
                trialsVM.PageSize = viewModel.TrialPageSize;
                trialsVM.Location = location;
                trialsVM.Condition = condition;
                trialsVM.Proximity = proximity ?? 50;

                // get a list of nctIds from the clinicaltrials.gov API
                trialsVM.NctIds = Globals.GetNctIdsFromCtg(trialsVM);

                DataTable nctIdTable = Globals.GetDataTableFromNctIds(trialsVM.NctIds);

                // pass nctIds to our stored proc
                viewModel.Trials = db.Database.SqlQuery<ResearchCenterTrialViewModel>("exec GetResearchCenterTrials @aliasId, @centerId, @condition, @pageSize, @pageNumber, @SearchNctIds",
                                                                                   new SqlParameter("aliasId", viewModel.AliasId),
                                                                                   new SqlParameter("centerId", viewModel.CenterId),
                                                                                   new SqlParameter("condition", viewModel.Condition),
                                                                                   new SqlParameter("pageSize", viewModel.TrialPageSize),
                                                                                   new SqlParameter("pageNumber", 1),
                                                                                   new SqlParameter("@SearchNctIds", SqlDbType.Structured)
                                                                                   {
                                                                                       TypeName = nctIdTable.TableName,
                                                                                       Value = nctIdTable
                                                                                   })
                                                                               .ToList();
            }
            else
                viewModel.Trials = null;

            if (viewModel.Trials != null && viewModel.Trials.Count() > 0)
                viewModel.TotalTrialCount = viewModel.Trials.FirstOrDefault().Total;
            else
                viewModel.TotalTrialCount = 0;


            if (viewModel.IsAliasTop50)
            {
                viewModel.Address = researchCenterAlias.Address;
                viewModel.City = researchCenterAlias.City;
                viewModel.State = researchCenterAlias.State;
                viewModel.Zip = researchCenterAlias.Zip;
            }

            if (viewModel.IsPublished || viewModel.IsSiteOwner || User.IsInRole("Administrator"))
            {
                viewModel.PrintPublishedOn = researchCenterAlias.PublishedOn == null ? "" : researchCenterAlias.PublishedOn.Value.ToString("MMMM yyyy");
                viewModel.Url = researchCenterAlias.Url;
                viewModel.Summary = "<p>" + (researchCenterAlias.Summary ?? string.Empty).Replace("\r\n", "</p><p>") + "</p>";

                AzureImageStorage imageStore = new AzureImageStorage(ConfigurationManager.AppSettings["TrialScoutBlobUri"], ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                                                                     ConfigurationManager.AppSettings["ResearchCenterBlobKey"], ConfigurationManager.AppSettings["ResearchCenterContainerName"]);

                viewModel.ResearchCenterDepartments = db.ResearchCenterAliasDepartments.Include("Department").Where(r => r.AliasId == viewModel.AliasId)
                                                                                  .Select(r => new ResearchCenterDepartmentViewModel()
                                                                                  {
                                                                                      DepartmentName = r.Department.DepartmentName,
                                                                                      ImageName = r.Department.ImageName
                                                                                  }).ToList();


                viewModel.Images = db.ResearchCenterAliasImages.Where(r => r.AliasId == viewModel.AliasId)
                                                          .Select(r => new ResearchCenterImageViewModel()
                                                          {
                                                              ImageId = r.AliasImageId,
                                                              BlobId = r.BlobId
                                                          }).ToList();

                viewModel.Images?.ForEach(i => i.ImageUri = imageStore.GetImageUri(i.BlobId.ToString()));

                if (researchCenterAlias.LogoBlobId.HasValue)
                    viewModel.LogoBlobUri = imageStore.GetImageUri(researchCenterAlias.LogoBlobId.ToString());
                if (researchCenterAlias.ProfileImageBlobId.HasValue)
                    viewModel.ProfileImageBlobUri = imageStore.GetImageUri(researchCenterAlias.ProfileImageBlobId.ToString());
            }
            else
            {
                viewModel.Images = new List<ResearchCenterImageViewModel>();
            }

            return viewModel;
        }
    }
}