using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;  
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using TrialScout.DAL.Context;
using TrialScout.DAL.Entities;
using TrialScout.Web.ViewModels;
using TrialScout.Web.Classes;
using System.Web;
using System.Configuration;
using Microsoft.AspNet.Identity;

namespace TrialScout.Web
{
    public class SearchController : Controller
    {
        private TrialScoutContext db = new TrialScoutContext();

        [AllowAnonymous]
        //[HttpGet]
        public ActionResult TrialDetail(string nctId,  Guid? aliasId = null, Guid? centerId = null)
        {
            //nctId = "NCT00129129";
            var requestPrefix = Request.Url.Scheme + Uri.SchemeDelimiter + Request.Url.Host + (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port);

            if (nctId == null || nctId.Trim() == "")
                return RedirectToAction("", "Home");

            db.Database.Log = Console.Write;

            SqlParameter aliasIdParameter = new SqlParameter("@aliasId", SqlDbType.UniqueIdentifier);
            SqlParameter centerIdParameter = new SqlParameter("@centerId", SqlDbType.UniqueIdentifier);
            aliasIdParameter.Value = (object) aliasId ?? DBNull.Value;
            centerIdParameter.Value = (object)centerId ?? DBNull.Value;


            TrialStudyDetailViewModel model = db.Database.SqlQuery<TrialStudyDetailViewModel>("EXEC GetTrialStudyDetail @nct_id, @aliasId, @centerId",
                new SqlParameter("@nct_id", nctId), aliasIdParameter, centerIdParameter).FirstOrDefault();

            if (Request.UrlReferrer?.OriginalString != null)
            {
                ViewBag.ReturnUrl = Request.UrlReferrer.OriginalString;
                ViewBag.SearchResults = string.IsNullOrEmpty(Session["SearchResultsUrl"]?.ToString()) ? requestPrefix + "/Search?proximity=50&condition=" + (model.Conditions?.Replace("<br />", string.Empty) ?? "") + "&location=" + (model.City ?? "") + ", " + (model.State ?? "") : Session["SearchResultsUrl"];
            }
            else
            {
                ViewBag.ReturnUrl = requestPrefix + "/ResearchCenter?id=" + model.CenterId + "&alias=" + model.AliasName;

                ViewBag.SearchResults = string.IsNullOrEmpty(Session["SearchResultsUrl"]?.ToString()) ? requestPrefix + "/Search?proximity=50&condition=" + (model.Conditions?.Replace("<br />", string.Empty) ?? "") + "&location=" + (model.City ?? "") + ", " + (model.State ?? "") : Session["SearchResultsUrl"];
            }

            return View(model);
        }

        [AllowAnonymous]
        //[HttpGet]
        public ActionResult Index(SearchTrialsViewModel model)
        {
            try
            {
                if (model != null &&
                        (model.Location != null && model.Location.Trim() != "") &&
                        (model.Condition != null && model.Condition.Trim() != ""))
                {
                    model.PageSize = 10;

                    //model.Results = GetSearchResults(model.Condition, model.Location, model.Proximity);

                    // get a list of nctIds from the clinicaltrials.gov API
                    model.NctIds = Globals.GetNctIdsFromCtg(model);

                    // pass nctIds to our stored proc
                    model.Results = GetSearchResultsFromNctIds(model);

                    Session["SearchResultsUrl"] = Request.Url;
                }

                if (model.Results == null || model.Results.Count() == 0)
                    model.Total = 0;
                else
                    model.Total = model.Results[0].Total;
            }
            catch(Exception ex)
            {
                Globals.LogError(null, ex, ex.Message, Globals.LogLevels.Error);
                return Redirect("~/Error/PageNotFound");
            }


            return View(model);
        }

        [AllowAnonymous]
        [Route("Search/_SearchResults")]
        //[HttpGet]
        public PartialViewResult _SearchResults(string location, string condition, int proximity, int pageNumber)
        {
            SearchTrialsViewModel model = new SearchTrialsViewModel();
            
            model.Location =  location;
            model.Condition = condition;
            model.Proximity = proximity;
            model.PageSize = 10;

            try
            {
                // model.Results = GetSearchResults(condition, location, proximity, page: pageNumber);

                // get a list of nctIds from the clinicaltrials.gov API
                model.NctIds = Globals.GetNctIdsFromCtg(model);

                // pass nctIds to our stored proc
                model.Results = GetSearchResultsFromNctIds(model, pageNumber);

            }
            catch (Exception ex)
            {
                Globals.LogError(null, ex, null, Globals.LogLevels.Error);
            }

            return PartialView(model);
        }

        [AllowAnonymous]
        [Route("Search/GetCityStateZip")]
        [HttpGet]
        public JsonResult GetCityStateZip(string term)
        {
            try
            {
                var q =
                    db.ZipcodeLatLongs
                        .Select(r => new TrialLocationSearchViewModel()
                        {
                            Zip = r.ZipCode,
                            City = r.City,
                            State = r.State
                        });

                int idx = term.LastIndexOf(' ');
                if (term.Contains(","))
                {
                    string[] terms = term.Split(',');
                    string t1 = terms[0].Trim();
                    string t2 = terms[1].Trim();

                    q = q.Where(z => z.City.Contains(t1) && (z.Zip.Contains(t2) || z.State.Contains(t2)));
                }
                else if (idx != -1)
                {
                    string t1 = term.Substring(0, idx).Trim();
                    string t2 = term.Substring(idx + 1).Trim();

                    q = q.Where(z => z.City.Contains(term) || (z.City.Contains(t1) && (z.Zip.Contains(t2) || z.State.Contains(t2))));
                }
                else
                {
                    q = q.Where(z => z.Zip.Contains(term) || z.City.Contains(term) || z.State.Contains(term));
                }

                List<TrialLocationSearchViewModel> locations = q
                    .ToList()
                    .GroupBy(x => new { x.City, x.State })
                    .Select(x => x.FirstOrDefault())
                    .OrderBy(x => x.City).ThenBy(x => x.State)
                    .ToList();

                return Json(locations, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Globals.LogError(null, ex, null, Globals.LogLevels.Error);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("Search/GetCondition")]
        [HttpGet]
        public JsonResult GetCondition(string term)
        {
            try
            {

                List<TrialConditionSearchViewModel> conditions =
                db.ConditionLookups.Where(z => z.mesh_term.ToLower().Contains(term.ToLower()))
                .Select(r => new TrialConditionSearchViewModel()
                {
                    Condition = r.mesh_term
                })
                .OrderBy(z => !z.Condition.ToLower().StartsWith(term.ToLower()))
                .ThenBy(z => z.Condition)
                .Take(15)
                .ToList();

                return Json(conditions, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                Globals.LogError(null, ex, null, Globals.LogLevels.Error);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }


        // Contact Form - Post (to get below)
        [Route("Search/ContactUsSubmit/")]
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
                       
                        StudyContactForm formSubmit = new StudyContactForm();
                        Guid? AliasId = model.AliasId; // always set to null
                         string StudyId = model.StudyId; // always set to null
                        string rcBody = "";
                        string returnReceiptMessage = "";

                      //  if (model.AliasName != null)
                      //      AliasId = db.ResearchCenterAliases.FirstOrDefault(r => r.AliasName == model.AliasName && !r.IsDeleted).AliasId;


                        string researchCenterName = db.ResearchCenters.FirstOrDefault(r => r.CenterId == model.ResearchCenterId && !r.IsDeleted).LocationName;

                        formSubmit.ContactId = Guid.NewGuid();
                        formSubmit.ResearchCenterId = model.ResearchCenterId;
                        formSubmit.ContactIP = model.ContactIP;
                        formSubmit.LinkID = model.LinkID;
                        formSubmit.FormName = model.FormName;
                         formSubmit.AliasName = model.AliasName;
                         formSubmit.AliasId = AliasId;
                        formSubmit.CreatedOn = model.CreatedOn;
                         formSubmit.Location = model.Location;
                         formSubmit.Proximity = model.Proximity;
                        formSubmit.Condition = model.Condition;
                        formSubmit.ReqURL = model.ReqURL;
                        formSubmit.ContactName = model.ContactName;
                        formSubmit.EmailAddress = model.EmailAddress;
                        formSubmit.PhoneNumber = model.PhoneNumber;
                         formSubmit.StudyId = model.StudyId;
                        formSubmit.ContactMessage = model.ContactMessage;
                        formSubmit.IPStat = Globals.GetIPStat(model.ContactIP); // JSON return
                        db.StudyContactForms.Add(formSubmit);
                        db.SaveChanges();

                        // REDIRECT TY MESSAGE SET
                        string emailSubjectType = researchCenterName;
                        returnReceiptMessage = "";
                       // redirectURL = redirectURL + "&callback=ContactUsResearchCenter";
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
                          // if (model.StudyId != null)
                            //{ // Study was clicked.\
                            if(!string.IsNullOrEmpty(model.AliasName) )
                        {
                            redirectURL = redirectURL + "?callback=ContactUsStudy";
                            emailSubjectType = "Study";
                        }else if (!string.IsNullOrEmpty(model.Location) && !string.IsNullOrEmpty(model.Condition))
                        {
                            redirectURL = redirectURL + "&callback=ContactUsStudy";
                            emailSubjectType = "Study";
                        }
                        else if (!string.IsNullOrEmpty(model.LinkID))
                        {
                            redirectURL = redirectURL + "&callback=ContactUsStudy";
                            emailSubjectType = "Study";
                        }

                            // }
                            // EMAIL  
                            // CenterID is always here
                            rcBody += "<font size=2><B>Research Center :</b> " + researchCenterName + "</font><br />";

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
                              string StudyOfficialDescription = db.ImportStudies.FirstOrDefault(r => r.nct_id == model.StudyId).official_title;
                              rcBody += "<font size=2><B>Study Description:</b> " + StudyOfficialDescription + "</font><br />";
                               returnReceiptMessage = StudyOfficialDescription;

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
        [Route("Search/ContactUs/{CenterId}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ContactUs(Guid CenterId)
        {
            StudyContactForm viewModel = new StudyContactForm();
            ViewData.Add(new KeyValuePair<string, object>("FormSource", "Search"));

            string fullURL = Request.UrlReferrer.AbsoluteUri;
            Uri reqUrl = new Uri(fullURL);

            if (reqUrl.Segments.Length > 4)
           {
            string location = reqUrl.Segments[5].Replace("%20", " ");
                string condition = reqUrl.Segments[4]?.Replace("/", "").Replace("%20", " ");
                string proximity = HttpUtility.ParseQueryString(reqUrl.Query).Get("proximity");

                viewModel.Location = location;
                viewModel.Proximity = proximity;
                viewModel.Condition = condition;
            }

            string aliasName = reqUrl.Segments[2]?.Replace("/", "").Replace("%20", " ");

            // string lastElement = fullURL.Split('/').Last();

            //  string location = HttpUtility.ParseQueryString(reqUrl.Query).Get("location");
            //  string aliasName = HttpUtility.ParseQueryString(reqUrl.Query).Get("alias"); // this is the searchId 
            //  string proximity = HttpUtility.ParseQueryString(reqUrl.Query).Get("proximity");
            //  string condition = HttpUtility.ParseQueryString(reqUrl.Query).Get("condition");
            string studyId = Request.QueryString["studyId"]; // HttpUtility.ParseQueryString(reqUrl.Query).Get("studyId"); // study ID link

            try
            {
                // TO-DO: IP -> regional tracking data we can write analytics against - https://ipdata.co/pricing.html
                viewModel.ContactIP = Globals.GetExternalIp();
                viewModel.ResearchCenterId = CenterId; // incoming
               // viewModel.AliasId = AliasId;
                  viewModel.FormName = "ContactUsFromStudy";
                    viewModel.StudyId = studyId;

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


                return PartialView("_ContactUs", viewModel);
            }
            catch (Exception e)
            {
                // log e
                return null;
            }


        }



        private decimal[] GetLatLonFromZip(string zip)
        {
            decimal[] latLon = new decimal[] { 0, 0 };

            ZipcodeLatLong ll = db.ZipcodeLatLongs
                .Where(z => z.ZipCode == zip)
                .FirstOrDefault();

            if (ll != null)
            {
                latLon[0] = ll.Latitude;
                latLon[1] = ll.Longitude;
            }
            return latLon;
        }

        private double Distance(double lat1, double lon1, double lat2, double lon2, char unit)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(Deg2Rad(lat1)) * Math.Sin(Deg2Rad(lat2)) + Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * Math.Cos(Deg2Rad(theta));
            dist = Math.Acos(dist);
            dist = Rad2Deg(dist);
            dist = dist * 60 * 1.1515; // miles
            if (unit == 'K') // kilometers
            {
                dist = dist * 1.609344;
            }
            else if (unit == 'N') // nautical miles
            {
                dist = dist * 0.8684;
            }
            return (dist);
        }

        private double Deg2Rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }
        private double Rad2Deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        private List<SearchTrialsResultViewModel> GetSearchResultsFromNctIds(SearchTrialsViewModel model, int page = 1)
        {
            DataTable nctIdTable = Globals.GetDataTableFromNctIds(model.NctIds);
            db.Database.CommandTimeout = 60;
            var latlong = Globals.LongLat(model.Location);

            var latitude = 0.00;
            var longitude = 0.00;
            if (latlong.Contains(','))
            {
                string[] temp = latlong.Split(',');
                 latitude = Double.Parse(temp[0].Trim());
                 longitude = Double.Parse(temp[1].Trim());
            }
            
            var results = db.Database.SqlQuery<SearchTrialsResultViewModel>(
                       "EXEC GetResearchCentersFromCTGSearch @Location, @Latitude, @Longitude,  @Proximity, @pageSize, @pageNumber, @SearchNctIds",
                       new SqlParameter("@Location", model.Location),
                       new SqlParameter("@Latitude",latitude),
                       new SqlParameter("@Longitude",longitude),
                       new SqlParameter("@Proximity", model.Proximity),
                       new SqlParameter("@pageSize", model.PageSize),
                       new SqlParameter("@pageNumber", page),
                       new SqlParameter("@SearchNctIds", SqlDbType.Structured)
                       {
                           TypeName = nctIdTable.TableName,
                           Value = nctIdTable
                       })
                   .ToList();
            return results;
            
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public static string SEOurl(string title)
        {
            if (title == null) return "";

            const int maxlen = 80;
            int len = title.Length;
            bool prevdash = false;
            var sb = new StringBuilder(len);
            char c;

            for (int i = 0; i < len; i++)
            {
                c = title[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    prevdash = false;
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    sb.Append((char)(c | 32));
                    prevdash = false;
                }
                else if (c == ' ' || c == ',' || c == '.' || c == '/' ||
                    c == '\\' || c == '-' || c == '_' || c == '=')
                {
                    if (!prevdash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevdash = true;
                    }
                }
                else if ((int)c >= 128)
                {
                    int prevlen = sb.Length;
                    sb.Append(RemapInternationalCharToAscii(c));
                    if (prevlen != sb.Length) prevdash = false;
                }
                if (i == maxlen) break;
            }

            if (prevdash)
                return sb.ToString().Substring(0, sb.Length - 1);
            else
                return sb.ToString();
        }

        private static char RemapInternationalCharToAscii(char c)
        {
            throw new NotImplementedException();
        }
    }
}
