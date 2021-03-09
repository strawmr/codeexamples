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
    public class ResearchCenterController : Controller
    {


        #region Action Results


        // Contact Form - Post (to get below)
        [Route("ResearchCenter/ContactUsSubmit/")]
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
                        //Guid? AliasId = null; // always set to null
                        // Guid? StudyId = null; // always set to null
                        string rcBody = "";
                        string returnReceiptMessage = "";

                        //if (model.AliasName != null)
                        //    AliasId = db.ResearchCenterAliases.FirstOrDefault(r => r.AliasName == model.AliasName && !r.IsDeleted).AliasId;


                        string ResearchCenterName = db.ResearchCenters.FirstOrDefault(r => r.CenterId == model.ResearchCenterId && !r.IsDeleted).LocationName;

                        formSubmit.ContactId = Guid.NewGuid();
                        formSubmit.ResearchCenterId = model.ResearchCenterId;
                        formSubmit.ContactIP = model.ContactIP;
                        formSubmit.LinkID = model.LinkID;
                        formSubmit.FormName = model.FormName;
                        // formSubmit.AliasName = model.AliasName;
                        // formSubmit.AliasId = AliasId;
                        formSubmit.CreatedOn = model.CreatedOn;
                        // formSubmit.Location = model.Location;
                        // formSubmit.Proximity = model.Proximity;
                        // formSubmit.Condition = model.Condition;
                        formSubmit.ReqURL = model.ReqURL;
                        formSubmit.ContactName = model.ContactName;
                        formSubmit.EmailAddress = model.EmailAddress;
                        formSubmit.PhoneNumber = model.PhoneNumber;
                        // formSubmit.StudyId = model.StudyId;
                        formSubmit.ContactMessage = model.ContactMessage;
                        formSubmit.IPStat = Globals.GetIPStat(model.ContactIP); // JSON return
                        db.StudyContactForms.Add(formSubmit);
                        db.SaveChanges();

                        // REDIRECT TY MESSAGE SET
                        string emailSubjectType = "Research Center";
                        returnReceiptMessage = ResearchCenterName;
                        redirectURL = redirectURL + "&callback=ContactUsResearchCenter";
                        //  if (model.AliasId == null && model.StudyId == null)
                        // {  // research center (ad or not)
                        //     redirectURL = redirectURL + "&callback=ContactUsResearchCenter";
                        //     returnReceiptMessage = ResearchName; // only place I can set this.
                        //      emailSubjectType = "Research Center";
                        //   }

                        //  if (model.AliasName != null)
                        //   { // Alias
                        //       redirectURL = redirectURL + "&callback=ContactUsAlias";
                        //        emailSubjectType = "Alias";
                        //     }
                        //    if (model.StudyId != null)
                        //    { // Study was clicked.
                        //         redirectURL = redirectURL + "&callback=ContactUsStudy";
                        ///         emailSubjectType = "Study";
                        ///  }
                        // EMAIL  
                        // CenterID is always here
                        rcBody += "<font size=2><B>Research Center :</b> " + ResearchCenterName + "</font><br />";

                        // show ad link that was clicked
                        if (model.LinkID != null) // ad was clicked
                            rcBody += "<b>Ad Click?</b> Yes<br />";

                        // if (model.AliasName != null) // Alias
                        //{
                        //   rcBody += "<font size=2><b>Condition (Search) :</b> " + model.Condition + "</font><hr />";
                        //   rcBody += "<b>Alias Name :</b> " + model.AliasName + "<br />";
                        //   rcBody += "<b>Location (Search) :</b> " + model.Location + "<br />";
                        //   rcBody += "<b>Proximity (Search) :</b> " + model.Proximity + "<br />";
                        //   returnReceiptMessage = model.AliasName;
                        // }

                        // if (model.StudyId != null) // Study was clicked.
                        // {
                        //      string StudyOfficialDescription = db.ImportStudies.FirstOrDefault(r => r.nct_id == model.StudyId).official_title;
                        //      rcBody += "<font size=2><B>Study Description:</b> " + StudyOfficialDescription + "</font><br />";
                        //       returnReceiptMessage = StudyOfficialDescription;

                        //   }


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
        [Route("ResearchCenter/ContactUs/{CenterId}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ContactUs(Guid CenterId)
        {
            StudyContactForm viewModel = new StudyContactForm();
            ViewData.Add(new KeyValuePair<string, object>("FormSource", "ResearchCenter"));

            string fullURL = Request.UrlReferrer.AbsoluteUri;
            Uri reqUrl = new Uri(fullURL);

            // string lastElement = fullURL.Split('/').Last();

            //  string location = HttpUtility.ParseQueryString(reqUrl.Query).Get("location");
            //  string aliasName = HttpUtility.ParseQueryString(reqUrl.Query).Get("alias"); // this is the searchId 
            //  string proximity = HttpUtility.ParseQueryString(reqUrl.Query).Get("proximity");
            //  string condition = HttpUtility.ParseQueryString(reqUrl.Query).Get("condition");
            //string studyId = Request.QueryString["studyId"]; // HttpUtility.ParseQueryString(reqUrl.Query).Get("studyId"); // study ID link

            try
            {
                // TO-DO: IP -> regional tracking data we can write analytics against - https://ipdata.co/pricing.html
                viewModel.ContactIP = Globals.GetExternalIp();
                viewModel.ResearchCenterId = CenterId; // incoming

                viewModel.FormName = "ContactUsFromResearchCenter";
                string linkId = HttpUtility.ParseQueryString(reqUrl.Query).Get("linkId");
                viewModel.LinkID = linkId;

                // FORM HANDLING - Manipulating the ContactUs based on this 
                //if (aliasName != null && studyId == null) // Alias Page
                //{

                //    viewModel.FormName = "ContactUsFromAlias";
                //    viewModel.AliasName = aliasName;

                //}
                //else if (studyId != null) // Study Page
                //{
                //    viewModel.FormName = "ContactUsFromStudy";
                //    viewModel.StudyId = studyId;
                //}
                //else if (aliasName == null && studyId == null) // Research Center page
                //{
                //    viewModel.FormName = "ContactUsFromResearchCenter";
                //    string linkId = HttpUtility.ParseQueryString(reqUrl.Query).Get("linkId");
                //    viewModel.LinkID = linkId;

                //}
                //else // Catch All 
                //{

                //    viewModel.FormName = "ContactUsGenericOrUnknown";
                //}

                // Comes in from Search - Study (?) & Alias
                // viewModel.Location = location;
                //  viewModel.Proximity = proximity;
                // viewModel.Condition = condition;

                // Generic on All Forms
                viewModel.CreatedOn = DateTime.Now;
                viewModel.ReqURL = reqUrl.ToString();


                return PartialView("_ContactUs",viewModel);
            }
            catch (Exception e)
            {
                // log e
                return null;
            }


        }

        [Route("ResearchCenter/ClaimResearchCenter")]
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClaimResearchCenter(ClaimResearchCenterViewModel claimResearchCenterViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    using (TrialScoutContext db = new TrialScoutContext())
                    {

                        ResearchCenter researchCenter = db.ResearchCenters.Where(r => r.CenterId == claimResearchCenterViewModel.ResearchCenterId).FirstOrDefault();

                        if (researchCenter == null)
                        {
                            ModelState.AddModelError("ResearchCenter", "Research Center could not be found");
                            return View(claimResearchCenterViewModel);
                        }

                        ResearchCenterApproval approval = new ResearchCenterApproval();

                        approval.ApprovalId = Guid.NewGuid();
                        approval.AgentEmailAddress = claimResearchCenterViewModel.AgentEmailAddress;
                        approval.AgentName = claimResearchCenterViewModel.AgentName;
                        approval.AgentTitle = claimResearchCenterViewModel.AgentTitle;
                        approval.CenterId = researchCenter.CenterId;
                        approval.CreatedOn = DateTime.Now;

                        db.ResearchCenterApprovals.Add(approval);

                        db.SaveChanges();

                        //generate email
                        string emailBody = String.Format(ConfigurationManager.AppSettings["ClaimResearchCenterNotification"],
                                                          approval.AgentName,
                                                          researchCenter.LocationName,
                                                          approval.AgentEmailAddress);

                        Globals.SendEmail(ConfigurationManager.AppSettings["ClaimResearchCenterNotificationSubject"],
                                            emailBody,
                                            ConfigurationManager.AppSettings["ClaimResearchCenterNotificationRecipients"].Split(';').ToList());

                        return RedirectToAction("ClaimResearchCenterConfirmation");

                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return View(claimResearchCenterViewModel);
        }

        [Route("ResearchCenter/ClaimResearchCenter")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ClaimResearchCenter(string name)
        {
            ClaimResearchCenterViewModel viewModel = new ClaimResearchCenterViewModel();
            if (name != null && name.Trim() != "")
            {
                viewModel.ResearchCenter = name;
            }
            return View();
        }

        [Route("ResearchCenter/ClaimResearchCenterConfirmation")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ClaimResearchCenterConfirmation()
        {
            return View();
        }

        // TODO for later - 
        //[Route("MyResearchCenter")]
        //[AllowAnonymous]
        //[HttpGet]
        //public ActionResult Index()
        //{
        //    ViewBag.ReturnUrl = Request.UrlReferrer;

        //    using (TrialScoutContext db = new TrialScoutContext())
        //    {
        //        if (User.Identity.IsAuthenticated)
        //        {
        //            Guid userId = new Guid(User.Identity.GetUserId());
        //            ResearchCenter researchCenter = db.ResearchCenters.Include("SubscriptionLevel").Where(r => r.UserId == userId && r.IsDeleted == false).FirstOrDefault();

        //            if (researchCenter == null)
        //                return RedirectToAction("Index", "Home");


        //            return View(BuildResearchCenterDetailsViewModel(researchCenter, db, null, null, 0, null));
        //        }
        //        else
        //            return RedirectToAction("Index", "Home");

        //    }
        //}

        //[Route("ResearchCenter/view")]
        //[AllowAnonymous]
        //[HttpGet]
        //public ActionResult Index(Guid id, string alias, string viewId)
        //{
        //    string condition = null;
        //    string location = null;
        //    var proximity = 50;
        //    using (TrialScoutContext db = new TrialScoutContext())
        //    {
        //        ResearchCenterAlias researchCenterAlias = null;
        //        ResearchCenter researchCenter = null;
        //        ResearchCenterDetailsViewModel vm = new ResearchCenterDetailsViewModel();
        //        researchCenterAlias = db.ResearchCenterAliases.Include("SubscriptionLevel").FirstOrDefault(r => r.CenterId == id && r.IsDeleted == false && r.AliasName == alias);

        //        if (researchCenterAlias == null)
        //        {
        //            researchCenter = db.ResearchCenters.Include("SubscriptionLevel").FirstOrDefault(rc => rc.CenterId == id && !rc.IsDeleted);
        //            vm = BuildResearchCenterDetailsViewModel(researchCenter, db, condition, location, proximity, alias);
        //        }
        //        else if (!researchCenterAlias.IsPublished)
        //        {
        //            researchCenter = db.ResearchCenters.Include("SubscriptionLevel").FirstOrDefault(rc => rc.CenterId == id && !rc.IsDeleted);
        //            vm = BuildResearchCenterDetailsViewModel(researchCenter, db, condition, location, proximity, alias);
        //        }
        //        else
        //        {
        //            vm = BuildResearchCenterAliasDetailsViewModel(researchCenterAlias, db, condition, location, proximity);
        //        }

        //        if (researchCenterAlias == null && researchCenter == null)
        //            return RedirectToAction("Index", "Home");

        //        return View(vm);

        //    }
        //}


        [AllowAnonymous]
        [HttpGet]
        public ActionResult Index(Guid? id, string condition, string location, int? proximity, string alias)
        {
            return RedirectPermanent($"ResearchSite?alias={alias}&centerId={id}&condition={condition}&location={location}&proximity={proximity}");
        }

        [Route("ResearchCenter/{name}/{centerId}")]
        [Route("ResearchCenter/{name}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Index(string name, Guid? centerId, string linkId)
        {

            //ViewBag.ReturnUrl = Request.UrlReferrer;
            //if(Session != null)
            //{
            //    Session.Clear();
            //}

            using (TrialScoutContext db = new TrialScoutContext())
            {
                //log if an add was clicked
                if (linkId != null && string.IsNullOrEmpty(linkId?.Trim()))
                {
                    Guid adId = new Guid(linkId);
                    ResearchCenterAd ad = db.ResearchCenterAds.FirstOrDefault(a => a.AdId == adId);
                    if (ad != null && ad.ClickedOn == null)
                    {
                        ad.Clicked = true;
                        ad.ClickedOn = DateTime.Now;
                        db.SaveChanges();
                    }
                }
                ResearchCenter researchCenter = null;
                if (centerId != null)
                    researchCenter = db.ResearchCenters.Include(r => r.ResearchCenterAliases).FirstOrDefault(r => r.CenterId == centerId && r.IsDeleted == false);
                else
                    researchCenter = db.ResearchCenters.Include(r => r.ResearchCenterAliases).FirstOrDefault(r => r.LocationName.Replace(".", "").Replace("&", "") == name && r.IsDeleted == false);

                if (researchCenter == null)
                    return RedirectToAction("Index", "Home");

                if(researchCenter.ResearchCenterAliases.Count == 1 && !User.IsInRole("Administrator"))
                {
                    bool isSame = (researchCenter.ResearchCenterAliases.FirstOrDefault().AliasName.Trim().ToLower() == researchCenter.LocationName.Trim().ToLower());

                    if (isSame)
                    {
                        var researchSite = researchCenter.ResearchCenterAliases.SingleOrDefault();
                        return Redirect($"~/ResearchSite/{researchSite.AliasId}");
                    }
                }
                


                return View(BuildResearchCenterDetailsViewModel(researchCenter, db));

            }
        }

        [Route("ResearchCenter/Edit/{id}")]
        [HttpGet]
        public ActionResult Edit(Guid id, string returnController, string returnAction)
        {
            var factory = new EditResearchCenterViewModelFactory();
            var viewModel = factory.GetResearchCenterViewModel(id, new Guid(User.Identity.GetUserId()), User.IsInRole("Administrator"));

            if (viewModel == null && !User.IsInRole("Administrator"))
                return RedirectToAction("/Home/Index");

            ViewBag.ReturnController = returnController;
            ViewBag.ReturnAction = returnAction;

            return View(viewModel);
        }

        [Route("ResearchCenter/Edit/{id}")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(EditResearchCenterViewModel viewModel, List<Guid> departmentIds,
                 HttpPostedFileBase logoImage, HttpPostedFileBase profileImage, List<HttpPostedFileBase> secondaryImages, string returnController, string returnAction)
        {
            bool changesAffectBillingOptions = false;

            using (TrialScoutContext db = new TrialScoutContext())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        ResearchCenter researchCenter = db.ResearchCenters.FirstOrDefault(r => r.CenterId == viewModel.CenterId && r.IsDeleted == false);

                        changesAffectBillingOptions = researchCenter.IncreaseAdRadius != viewModel.IncreaseAdRadius;

                        researchCenter.Address = viewModel.Address;
                        researchCenter.City = viewModel.City;
                        if (User.IsInRole("Administrators"))
                            researchCenter.LocationName = viewModel.LocationName;
                        researchCenter.Phone = viewModel.Phone;
                        researchCenter.SearchDescription = viewModel.SearchDescription;
                        researchCenter.State = viewModel.State;
                        researchCenter.Country = viewModel.Country;
                        researchCenter.Summary = viewModel.Summary;
                        researchCenter.IncreaseAdRadius = viewModel.IncreaseAdRadius;

                        if (viewModel.Url != null && viewModel.Url != researchCenter.Url)
                        {
                            string url = viewModel.Url;
                            if (!url.Trim().ToLower().StartsWith("http"))
                                url = "https://" + url;

                            researchCenter.Url = url;

                        }

                        researchCenter.Zip = viewModel.Zip;

                        //save images
                        AzureImageStorage imageStore = new AzureImageStorage(ConfigurationManager.AppSettings["TrialScoutBlobUri"], ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                                                     ConfigurationManager.AppSettings["ResearchCenterBlobKey"], ConfigurationManager.AppSettings["ResearchCenterContainerName"]);

                        if (logoImage != null)
                        {
                            try
                            {
                                //delete the old image before uploading a new one
                                //if this fails, we still want to continue
                                if (researchCenter.LogoBlobId.HasValue)
                                    imageStore.DeleteImage(researchCenter.LogoBlobId.Value);
                            }
                            catch (Exception ex)
                            {
                                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
                            }

                            Guid blobId = imageStore.SaveImage(logoImage.InputStream, AzureImageStorage.ImageSizes.ResearchCenterLogo);
                            researchCenter.LogoBlobId = blobId;
                        }
                        if (profileImage != null)
                        {
                            //delete the old image before uploading a new one
                            if (researchCenter.ProfileImageBlobId.HasValue)
                                imageStore.DeleteImage(researchCenter.ProfileImageBlobId.Value);

                            Guid blobId = imageStore.SaveImage(profileImage.InputStream, AzureImageStorage.ImageSizes.ResearchCenterPrimaryImage);
                            researchCenter.ProfileImageBlobId = blobId;
                        }

                        int imageCount = db.ResearchCenterImages.Where(r => r.CenterId == researchCenter.CenterId).Count();

                        if (imageCount < 4)
                        {
                            if (secondaryImages != null && secondaryImages.Count() > 0)
                            {
                                foreach (HttpPostedFileBase secondaryImage in secondaryImages)
                                {
                                    if (secondaryImage != null)
                                    {
                                        Guid blobId = imageStore.SaveImage(secondaryImage.InputStream, AzureImageStorage.ImageSizes.ResearchCenterSecondaryImage);
                                        ResearchCenterImage image = new ResearchCenterImage();

                                        image.BlobId = blobId;
                                        image.CenterId = researchCenter.CenterId;
                                        image.CreatedBy = new Guid(User.Identity.GetUserId());
                                        image.CreatedOn = DateTime.Now;
                                        image.ImageId = Guid.NewGuid();

                                        db.ResearchCenterImages.Add(image);

                                        imageCount++;

                                        if (imageCount >= 4)
                                            break;

                                    }
                                }
                            }
                        }

                        //save departments
                        List<ResearchCenterDepartment> existingDepartments = db.ResearchCenterDepartments.Where(r => r.CenterId == viewModel.CenterId).ToList();

                        if (existingDepartments != null && existingDepartments.Count() > 0)
                            db.ResearchCenterDepartments.RemoveRange(existingDepartments);

                        if (departmentIds != null && departmentIds.Count() > 0)
                        {
                            foreach (Guid departmentId in departmentIds)
                            {
                                ResearchCenterDepartment department = new ResearchCenterDepartment();

                                department.CenterId = viewModel.CenterId;
                                department.CreatedBy = new Guid(User.Identity.GetUserId());
                                department.CreatedOn = DateTime.Now;
                                department.DepartmentId = departmentId;
                                department.ResearchCenterDepartmentId = Guid.NewGuid();

                                db.ResearchCenterDepartments.Add(department);
                            }
                        }

                        //Save Ad City Mapping
                        List<ResearchCenterAdCity> existingCities = db.ResearchCenterAdCities.Where(x => x.CenterId == viewModel.CenterId).ToList();

                        if (!changesAffectBillingOptions)
                        {
                            changesAffectBillingOptions = existingCities.Count() != viewModel.AdCities.Count(c => !string.IsNullOrWhiteSpace(c.City)) ||
                                existingCities.Count(c => c.IncreaseRadius) != viewModel.AdCities.Count(c => !string.IsNullOrWhiteSpace(c.City) && c.IncreaseRadius);
                        }

                        foreach (var city in viewModel.AdCities)
                        {
                            if (!string.IsNullOrWhiteSpace(city.City) && !string.IsNullOrWhiteSpace(city.State))
                            {
                                if (!city.AdCityId.HasValue)
                                {
                                    if (existingCities.Where(c => c.City == city.City && c.State == city.State).Count() == 0)
                                    {
                                        var adCity = new ResearchCenterAdCity
                                        {
                                            AdCityId = Guid.NewGuid(),
                                            CenterId = viewModel.CenterId,
                                            City = city.City,
                                            State = city.State,
                                            IncreaseRadius = city.IncreaseRadius,
                                            CreatedBy = Guid.Parse(User.Identity.GetUserId()),
                                            CreatedOn = DateTime.Now
                                        };

                                        existingCities.Add(adCity);

                                        db.ResearchCenterAdCities.Add(adCity);
                                    }
                                }
                                else
                                {
                                    var orig = db.ResearchCenterAdCities.Find(city.AdCityId);

                                    if (orig != null)
                                        orig.IncreaseRadius = city.IncreaseRadius;
                                }
                            }
                        }

                        db.SaveChanges();

                        if (changesAffectBillingOptions && !User.IsInRole("Administrator"))
                        {
                            SendAlertEmail(researchCenter.LocationName);
                        }

                        if (string.IsNullOrWhiteSpace(returnAction))
                            return RedirectToAction("", "ResearchCenter", new { id = viewModel.CenterId });
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

        #endregion

        #region Partial View Results

        [AllowAnonymous]
        [Route("ResearchCenter/_Ratings/{researchCenterId}")]
        [HttpGet]
        public PartialViewResult _Ratings(Guid researchCenterId, Guid? aliasId, string sort, int pageNumber)
        {
            List<ResearchCenterRatingsViewModel> ratings = new List<ResearchCenterRatingsViewModel>();

            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    int pageSize = 10;
                    string responderName = db.ResearchCenters.Where(r => r.CenterId == researchCenterId).Select(r => r.LocationName).FirstOrDefault();
                    IEnumerable<ResearchCenterRating> ratingQuery = db.ResearchCenterRatings.Include(r => r.ResearchCenterAlias);

                    switch (sort)
                    {
                        case "date":
                            ratingQuery = ratingQuery.OrderByDescending(r => r.CreatedOn).ToList();
                            break;
                        case "reviewer":
                            ratingQuery = ratingQuery.OrderByDescending(r => r.EmailAddress != null && r.FirstName != null && r.LastName != null)
                                                    .ThenByDescending(r => r.ExplanationRating != null && r.ListeningRating != null && r.WaitTimeRating != null && r.OfficeRating != null)
                                                    .ThenByDescending(r => r.CreatedOn).ToList();
                            break;
                        case "ratingsDesc":
                            ratingQuery = ratingQuery.OrderByDescending(r => r.OverallRating).ThenByDescending(r => r.CreatedOn).ToList();
                            break;
                        case "ratingsAsc":
                            ratingQuery = ratingQuery.OrderBy(r => r.OverallRating).ThenByDescending(r => r.CreatedOn).ToList();
                            break;
                    }

                    if (aliasId != null)
                    {
                        ratings = ratingQuery.Where(r => r.CenterId == researchCenterId && r.IsApproved && !r.IsDeleted && r.AliasId == aliasId)
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
                            ReponseDate = r.ResponseCreatedOn,
                            Response = r.Response,
                            HasExplanationRating = r.ExplanationRating.HasValue,
                            HasListeningRating = r.ListeningRating.HasValue,
                            HasWaitTimeRating = r.WaitTimeRating.HasValue,
                            HasOfficeRating = r.OfficeRating.HasValue,
                            ResponderName = responderName
                        })
                        .Skip(pageNumber * pageSize)
                        .Take(pageSize)
                        .ToList();
                    }
                    else
                    {
                        ratings = ratingQuery.Where(r => r.CenterId == researchCenterId && r.IsApproved && !r.IsDeleted)
                            .Select(r => new ResearchCenterRatingsViewModel()
                            {
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
                                ReponseDate = r.ResponseCreatedOn,
                                Response = r.Response,
                                HasExplanationRating = r.ExplanationRating.HasValue,
                                HasListeningRating = r.ListeningRating.HasValue,
                                HasWaitTimeRating = r.WaitTimeRating.HasValue,
                                HasOfficeRating = r.OfficeRating.HasValue,
                                AliasName = (r.ResearchCenterAlias != null) ? r.ResearchCenterAlias.AliasName : "",
                                ResponderName = responderName
                            })
                            .Skip(pageNumber * pageSize)
                            .Take(pageSize)
                            .ToList();
                    }




                    Guid? userId = db.ResearchCenters.Where(r => r.CenterId == researchCenterId && r.ApprovalId != null).Select(r => r.UserId).FirstOrDefault();
                    if (userId.HasValue && User.Identity.GetUserId() != null && userId.Value == new Guid(User.Identity.GetUserId()))
                        ViewData["IsSiteOwner"] = true;
                    else
                        ViewData["IsSiteOwner"] = false;
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(null, ex, null, Globals.LogLevels.Error);
            }

            return PartialView(ratings);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("ResearchCenter/_ScoreRatings")]
        public PartialViewResult _Ratings(Guid centerId, Guid? aliasId, int score, string sort, int pageNumber)
        {
            List<ResearchCenterRatingsViewModel> ratings = new List<ResearchCenterRatingsViewModel>();

            using (TrialScoutContext db = new TrialScoutContext())
            {
                IEnumerable<ResearchCenterRating> rateQuery = db.ResearchCenterRatings.Include(r => r.ResearchCenterAlias);

                string responderName = db.ResearchCenters.Where(r => r.CenterId == centerId).Select(r => r.LocationName).FirstOrDefault();
                int pageSize = 10;

                switch (sort)
                {
                    case "Rater Comments":
                        rateQuery = rateQuery.OrderByDescending(r => r.EmailAddress != null && r.FirstName != null && r.LastName != null)
                            .ThenByDescending(r => r.ListeningRating != null && r.OfficeRating != null && r.WaitTimeRating != null && r.ExplanationRating != null)
                            .ThenByDescending(r => r.CreatedOn).ToList();
                        break;
                    default:
                        rateQuery = rateQuery.OrderByDescending(r => r.CreatedOn).ToList();
                        break;
                }

                if (aliasId == null)
                {
                    ratings = rateQuery.Where(r => r.CenterId == centerId && r.IsApproved && r.OverallRating == score).Skip(pageNumber * pageSize).Take(pageSize)
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
                        ReponseDate = r.ResponseCreatedOn,
                        Response = r.Response,
                        HasExplanationRating = r.ExplanationRating.HasValue,
                        HasListeningRating = r.ListeningRating.HasValue,
                        HasWaitTimeRating = r.WaitTimeRating.HasValue,
                        HasOfficeRating = r.OfficeRating.HasValue,
                        AliasName = (r.ResearchCenterAlias != null) ? r.ResearchCenterAlias.AliasName : "",
                        ResponderName = responderName
                    }).ToList();
                }
                else
                {
                    ratings = rateQuery.Where(r => r.CenterId == centerId && r.IsApproved && r.OverallRating == score && r.AliasId == aliasId).Skip(pageNumber * pageSize).Take(pageSize)
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
                        ReponseDate = r.ResponseCreatedOn,
                        Response = r.Response,
                        HasExplanationRating = r.ExplanationRating.HasValue,
                        HasListeningRating = r.ListeningRating.HasValue,
                        HasWaitTimeRating = r.WaitTimeRating.HasValue,
                        HasOfficeRating = r.OfficeRating.HasValue,
                        //AliasName = (r.ResearchCenterAlias != null) ? r.ResearchCenterAlias.AliasName : "",
                        ResponderName = responderName
                    }).ToList();
                }



                Guid? userId = db.ResearchCenters.Where(r => r.CenterId == centerId && r.ApprovalId != null).Select(r => r.UserId).FirstOrDefault();
                if (userId.HasValue && User.Identity.GetUserId() != null && userId.Value == new Guid(User.Identity.GetUserId()))
                    ViewData["IsSiteOwner"] = true;
                else
                    ViewData["IsSiteOwner"] = false;
            }

            return PartialView(ratings);
        }

        [AllowAnonymous]
        [Route("ResearchCenter/_Trials/{researchCenterId}")]
        [HttpGet]
        public JsonResult _ClinicalTrials(Guid researchCenterId, int pageNumber, int? age, string sex, int? phase, string condition, string location, int? proximity, string alias)
        {
            List<ResearchCenterTrialViewModel> trials = new List<ResearchCenterTrialViewModel>();

            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    Guid? aliasId = null;

                    if (!String.IsNullOrEmpty(alias))
                    {
                        //retrieve alias 
                        ResearchCenterAlias researchcenteralias = db.ResearchCenterAliases.FirstOrDefault(r => (r.CenterId == researchCenterId && r.AliasName == alias && !r.IsDeleted));
                        aliasId = researchcenteralias.AliasId;
                    }

                    if (String.IsNullOrEmpty(location))
                    {
                        location = GetAliasLocation(db, researchCenterId);
                    }

                    int pageSize = 5;
                    //location = "Rochester, NY, USA";
                    if ((condition != null && condition.Trim() != "") &&
                        (location != null && location.Trim() != ""))
                    {
                        SearchTrialsViewModel trialsVM = new SearchTrialsViewModel();
                        trialsVM.PageSize = pageSize;
                        trialsVM.Location = location;
                        trialsVM.Condition = condition;
                        trialsVM.Age = age;
                        trialsVM.Sex = sex;
                        trialsVM.Phase = phase;

                        if (proximity == null || proximity == 0)
                            proximity = 50;

                        trialsVM.Proximity = proximity.Value;

                        // get a list of nctIds from the clinicaltrials.gov API
                        trialsVM.NctIds = Globals.GetNctIdsFromCtg(trialsVM);

                        DataTable nctIdTable = Globals.GetDataTableFromNctIds(trialsVM.NctIds);


                        if (alias == null || alias.Trim() == "" || aliasId == null)
                        {

                            trials = db.Database.SqlQuery<ResearchCenterTrialViewModel>("exec GetResearchLocationTrials " +
                               "@centerId, @condition, @pageSize, @pageNumber, @SearchNctIds",
                                               new SqlParameter("centerId", researchCenterId),
                                               new SqlParameter("condition", condition),
                                               new SqlParameter("pageSize", pageSize),
                                               new SqlParameter("pageNumber", pageNumber),
                                               new SqlParameter("@SearchNctIds", SqlDbType.Structured)
                                               {
                                                   TypeName = nctIdTable.TableName,
                                                   Value = nctIdTable
                                               })
                                           .ToList();
                        }
                        else
                        {

                            // pass nctIds to our stored proc
                            trials = db.Database.SqlQuery<ResearchCenterTrialViewModel>("exec GetResearchCenterTrials " +
                                    "@aliasId, @centerId, @condition, @pageSize, @pageNumber, @SearchNctIds",
                                                    new SqlParameter("aliasId", aliasId),
                                                    new SqlParameter("centerId", researchCenterId),
                                                    new SqlParameter("condition", condition),
                                                    new SqlParameter("pageSize", pageSize),
                                                    new SqlParameter("pageNumber", pageNumber),
                                                    new SqlParameter("@SearchNctIds", SqlDbType.Structured)
                                                    {
                                                        TypeName = nctIdTable.TableName,
                                                        Value = nctIdTable
                                                    })
                                                .ToList();
                        }



                    }


                }
            }
            catch (Exception ex)
            {
                Globals.LogError(null, ex, null, Globals.LogLevels.Error);
            }


            return Json(trials, JsonRequestBehavior.AllowGet);
        }

        [Route("ResearchCenter/_ResearchCenterAdCity")]
        [HttpGet]
        public PartialViewResult _ResearchCenterAdCity()
        {
            var viewModel = new AdCitiesViewModel()
            {
                IncreaseRadius = false
            };

            return PartialView(viewModel);
        }

        #endregion

        #region JSON

        [Route("ResearchCenter/RemoveLogo/{centerId}/{aliasId}")]
        [HttpPost]
        public JsonResult RemoveLogo(Guid centerId, Guid? aliasId)
        {
            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    AzureImageStorage imageStore = new AzureImageStorage(ConfigurationManager.AppSettings["TrialScoutBlobUri"], ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                        ConfigurationManager.AppSettings["ResearchCenterBlobKey"], ConfigurationManager.AppSettings["ResearchCenterContainerName"]);



                    if (aliasId != null)
                    {
                        ResearchCenterAlias researchCenterAlias =
                            db.ResearchCenterAliases.FirstOrDefault(r => r.AliasId == aliasId && r.CenterId == centerId && !r.IsDeleted);
                        if (researchCenterAlias.LogoBlobId.HasValue)
                        {
                            imageStore.DeleteImage(researchCenterAlias.LogoBlobId.Value);

                            researchCenterAlias.LogoBlobId = null;

                            db.SaveChanges();
                        }


                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {
                        ResearchCenter researchCenter = db.ResearchCenters.Where(r => r.CenterId == centerId && r.IsDeleted == false).FirstOrDefault();
                        if (researchCenter.LogoBlobId.HasValue)
                        {
                            imageStore.DeleteImage(researchCenter.LogoBlobId.Value);

                            researchCenter.LogoBlobId = null;

                            db.SaveChanges();
                        }


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

        [Route("ResearchCenter/RemovePrimaryImage/{centerId}/{aliasId}")]
        [HttpPost]
        public JsonResult RemovePrimaryImage(Guid centerId, Guid? aliasId)
        {
            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    AzureImageStorage imageStore = new AzureImageStorage(ConfigurationManager.AppSettings["TrialScoutBlobUri"], ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                        ConfigurationManager.AppSettings["ResearchCenterBlobKey"], ConfigurationManager.AppSettings["ResearchCenterContainerName"]);



                    if (aliasId != null)
                    {
                        ResearchCenterAlias researchCenterAlias =
                            db.ResearchCenterAliases.FirstOrDefault(r => r.AliasId == aliasId && r.CenterId == centerId && !r.IsDeleted);
                        if (researchCenterAlias.ProfileImageBlobId.HasValue)
                        {
                            imageStore.DeleteImage(researchCenterAlias.ProfileImageBlobId.Value);

                            researchCenterAlias.ProfileImageBlobId = null;

                            db.SaveChanges();
                        }


                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {
                        ResearchCenter researchCenter = db.ResearchCenters.Where(r => r.CenterId == centerId && r.IsDeleted == false).FirstOrDefault();
                        if (researchCenter.ProfileImageBlobId.HasValue)
                        {
                            imageStore.DeleteImage(researchCenter.ProfileImageBlobId.Value);

                            researchCenter.ProfileImageBlobId = null;

                            db.SaveChanges();
                        }


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

        [Route("ResearchCenter/RemoveSecondaryImage/{imageId}")]
        [HttpPost]
        public JsonResult RemoveSecondaryImage(Guid imageId, bool isAlias = false)
        {
            try
            {

                using (TrialScoutContext db = new TrialScoutContext())
                {
                    AzureImageStorage imageStore = new AzureImageStorage(ConfigurationManager.AppSettings["TrialScoutBlobUri"], ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                         ConfigurationManager.AppSettings["ResearchCenterBlobKey"], ConfigurationManager.AppSettings["ResearchCenterContainerName"]);

                    if (isAlias)
                    {
                        ResearchCenterAliasImages image = db.ResearchCenterAliasImages.FirstOrDefault(im => im.AliasImageId == imageId);

                        imageStore.DeleteImage(image.BlobId);

                        db.ResearchCenterAliasImages.Remove(image);

                    }
                    else
                    {
                        ResearchCenterImage image = db.ResearchCenterImages.Where(r => r.ImageId == imageId).FirstOrDefault();

                        imageStore.DeleteImage(image.BlobId);

                        db.ResearchCenterImages.Remove(image);
                    }

                    db.SaveChanges();

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);

                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("ResearchCenter/GetUnassignedAliases")]
        [HttpGet]
        public JsonResult GetUnassignedAliases(string term)
        {
            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    List<ResearchCenterSearchViewModel> researchCenters = db.ResearchCenterAliases.Where(r => r.CenterId != null && !r.ResearchCenter.IsDeleted && r.AliasName.Contains(term))
                                                                                                  .OrderBy(r => r.AliasName)
                                                                                                  .Select(r => new ResearchCenterSearchViewModel()
                                                                                                  {
                                                                                                      LocationName = r.AliasName,
                                                                                                      City = r.City,
                                                                                                      State = r.State,
                                                                                                      CenterId = r.CenterId.Value
                                                                                                  })
                                                                                                  .Take(5)
                                                                                                  .ToList();
                    return Json(researchCenters, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [Route("ResearchCenter/DeleteCity")]
        [HttpPost]
        public JsonResult DeleteCity(Guid? id)
        {
            try
            {
                if (id.HasValue)
                {
                    using (var ctx = new TrialScoutContext())
                    {
                        var rcName = ctx.ResearchCenterAdCities.Include(rac => rac.ResearchCenter).FirstOrDefault(rac => rac.AdCityId == id.Value).ResearchCenter.LocationName;

                        ctx.ResearchCenterAdCities.Remove(ctx.ResearchCenterAdCities.Find(id.Value));
                        ctx.SaveChanges();

                        SendAlertEmail(rcName);
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Globals.LogError(null, ex, null, Globals.LogLevels.Error);
                return Json(new { success = false });
            }
        }

        [AllowAnonymous]
        [Route("ResearchCenter/ResearchCenterRanking")]
        [HttpGet]
        public JsonResult ResearchCenterRanking(Guid id, string type)
        {
            // Id = research center or alias ID 
            // type = specifies whether alias or center
            object data = null;
            using (TrialScoutContext db = new TrialScoutContext())
            {
                ResearchCenterAliasRanking aRankingTemp = null;
                ResearchCenterRanking rankingTemp = null;
                try
                {
                    if (!string.IsNullOrEmpty(type) && id != null)
                    {
                        switch (type)
                        {
                            case "alias":
                                aRankingTemp = db.ResearchCenterAliasRankings.Where(rk => rk.AliasId == id).Include(r => r.ResearchCenterAlias).FirstOrDefault();
                                data = new { CenterId = aRankingTemp.ResearchCenterAlias.CenterId, Name = aRankingTemp.AliasName, Tenth = aRankingTemp.RankingRoundedToTenth, Half = aRankingTemp.RankingRoundedToHalf };
                                break;
                            default:
                                rankingTemp = db.ResearchCenterRankings.Where(rk => rk.CenterId == id).FirstOrDefault();
                                data = new { CenterId = id, Name = rankingTemp.Name, Tenth = rankingTemp.RankingRoundedToTenth, Half = rankingTemp.RankingRoundedToHalf };
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Globals.LogError(null, ex, ex.Message, Globals.LogLevels.Debug);
                    return Json(null);
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);

        }



        private void SendAlertEmail(string rcName)
        {
            if (!User.IsInRole("Administrator"))
            {
                Globals.SendEmail("Invoice Review Required",
                        $"A change has been made to the profile for {rcName} that requires a review of their invoice.",
                        new List<string>() { ConfigurationManager.AppSettings["ResearchCenterBillingChangeAlertRecipient"] });

            }
        }

        #endregion

        #region Functions

        private ResearchCenterDetailsViewModel BuildResearchCenterDetailsViewModel(ResearchCenter researchCenter, TrialScoutContext db)
        {
            ResearchCenterDetailsViewModel viewModel = new ResearchCenterDetailsViewModel();

            viewModel.LocationName = researchCenter.LocationName;
            viewModel.SearchDescription = researchCenter.SearchDescription;
            viewModel.IsPublished = researchCenter.IsPublished;
            viewModel.CenterId = researchCenter.CenterId;
            viewModel.IsClaimed = researchCenter.UserId.HasValue;
            viewModel.UserId = researchCenter.UserId ?? Guid.Empty;

            viewModel.ProfileImageBlobUri = "/Content/images/overlay@2x.png";
            viewModel.LogoBlobUri = "/Content/images/TS-Logomark-FullColor.png";
            //viewModel.SubscriptionLevel = researchCenter.SubscriptionLevel;

            //Process Advanced Trial Search

            if (Request.IsAuthenticated)
                viewModel.LoggedInUserId = new Guid(User.Identity.GetUserId());

            if (researchCenter.ResearchCenterAliases.Count > 0)
            {
                viewModel.Aliases = db.ResearchCenterAliases
                    .Where(rc => !rc.IsDeleted && rc.CenterId == viewModel.CenterId).Select(a => a.AliasName).ToList();
            }


            viewModel.ReviewSortOptions = new Dictionary<string, string>() {
                                                        { "date", "Date(default)"},
                                                        { "reviewer", "Rater Comments" },
                                                        { "ratingsDesc", "High Ratings" },
                                                        { "ratingsAsc", "Low Ratings" } };

            viewModel.Ratings = db.ResearchCenterRatings.Include(ra => ra.ResearchCenterAlias).Where(r => r.CenterId == viewModel.CenterId && r.IsApproved && !r.IsDeleted)
                                      .Select(r => new ResearchCenterRatingsViewModel()
                                      {
                                              //AliasId = r.AliasId, 
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
                                          HasOfficeRating = r.OfficeRating.HasValue,
                                          AliasName = r.ResearchCenterAlias.AliasName
                                      })
                                      //.OrderByDescending(r => r.HasEmail && r.HasExplanationRating == true && r.HasListeningRating == true && r.HasWaitTimeRating == true && r.HasOfficeRating == true)
                                      //.ThenByDescending(r => r.HasExplanationRating == true && r.HasListeningRating == true && r.HasWaitTimeRating == true && r.HasOfficeRating == true)
                                      //.ThenByDescending(r => r.CreatedDate)
                                      .OrderByDescending(r => r.CreatedDate)
                                      .Take(viewModel.ReviewPageSize).ToList();

            ResearchCenterRanking rcr = db.ResearchCenterRankings.Where(r => r.CenterId == viewModel.CenterId).FirstOrDefault();
            viewModel.Rating = rcr?.RankingRoundedToHalf ?? 3;
            viewModel.RatingTenths = rcr?.RankingRoundedToTenth ?? 3;


            viewModel.TotalRatingCount = db.ResearchCenterRatings.Where(r => r.CenterId == viewModel.CenterId && r.IsApproved).Count();

            //Process Advanced Trial Search
            AdvancedTrialSearchViewModel advancedTrialSearchVm = new AdvancedTrialSearchViewModel()
            {
                Condition = viewModel.Condition ?? "",
                Age = null
            };

            viewModel.TrialSearch = advancedTrialSearchVm;

            var ScoreCard = new RatingScoreCardViewModel();

            //scorecard 
            ScoreCard = db.Database.SqlQuery<RatingScoreCardViewModel>(
                "exec GetRatingScoreCard @aliasId, @centerId",
                new SqlParameter("aliasId", DBNull.Value),
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
                    RankingRoundedToHalf = rcr?.RankingRoundedToHalf,
                    RankingRoundedToTenth = rcr?.RankingRoundedToTenth,
                    AliasId = viewModel.AliasId,
                    CenterId = viewModel.CenterId,
                    AliasName = viewModel.Alias,
                    LocationName = viewModel.LocationName
                };
            }

            viewModel.Top10Conditions = db.Database.SqlQuery<Top10ConditionsViewModel>("Exec Top_10_Conditions @alias_ID, @center_ID", new SqlParameter("@alias_ID", DBNull.Value), new SqlParameter("@center_ID", viewModel.CenterId)).ToList();


            if (researchCenter.IsPublished || viewModel.IsSiteOwner || User.IsInRole("Administrator"))
            {
                viewModel.Address = researchCenter.Address;
                viewModel.City = researchCenter.City;
                viewModel.State = researchCenter.State;
                viewModel.Zip = researchCenter.Zip;
                viewModel.Country = researchCenter.Country;
                viewModel.Phone = researchCenter.Phone;
                viewModel.PrintPublishedOn = researchCenter.PublishedOn == null ? "" : researchCenter.PublishedOn.Value.ToString("MMMM yyyy");
                viewModel.Url = researchCenter.Url;
                viewModel.Summary = "<p>" + (researchCenter.Summary ?? string.Empty).Replace("\r\n", "</p><p>") + "</p>";


                AzureImageStorage imageStore = new AzureImageStorage(ConfigurationManager.AppSettings["TrialScoutBlobUri"], ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                                                                     ConfigurationManager.AppSettings["ResearchCenterBlobKey"], ConfigurationManager.AppSettings["ResearchCenterContainerName"]);

                viewModel.ResearchCenterDepartments = db.ResearchCenterDepartments.Include("Department").Where(r => r.CenterId == viewModel.CenterId)
                                                        .Select(r => new ResearchCenterDepartmentViewModel()
                                                        {
                                                            DepartmentName = r.Department.DepartmentName,
                                                            ImageName = r.Department.ImageName
                                                        }).ToList();


                viewModel.Images = db.ResearchCenterImages.Where(r => r.CenterId == viewModel.CenterId)
                                                          .Select(r => new ResearchCenterImageViewModel()
                                                          {
                                                              ImageId = r.ImageId,
                                                              BlobId = r.BlobId
                                                          }).ToList();

                if (viewModel.Images != null)
                    viewModel.Images.ForEach(i => i.ImageUri = imageStore.GetImageUri(i.BlobId.ToString()));

                if (researchCenter.LogoBlobId.HasValue)
                    viewModel.LogoBlobUri = imageStore.GetImageUri(researchCenter.LogoBlobId.ToString());
                if (researchCenter.ProfileImageBlobId.HasValue)
                    viewModel.ProfileImageBlobUri = imageStore.GetImageUri(researchCenter.ProfileImageBlobId.ToString());
            }
            else
            {
                viewModel.Images = new List<ResearchCenterImageViewModel>();
            }

            return viewModel;
        }

        private string GetAliasLocation(TrialScoutContext db, Guid centerId)
        {
            string returnLocation = "";

            ResearchCenterAlias researchCenterAlias = db.ResearchCenterAliases.FirstOrDefault(ra => ra.CenterId == centerId && !ra.IsDeleted);
            if (researchCenterAlias != null)
            {
                returnLocation = $"{researchCenterAlias.State}, USA";
            }

            return returnLocation;
        }


        #endregion

    }
}



