using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using TrialScout.DAL;
using TrialScout.DAL.Context;
using TrialScout.DAL.Entities;
using TrialScout.Web.Classes;
using TrialScout.Web.ViewModels;
using static TrialScout.Web.Localization.LocalizedRoute;

namespace TrialScout.Web.Controllers
{
    public class HomeController : Controller
    {
        #region Action Results

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ContactUs()
        {
            return View(new ContactUsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ContactUs(ContactUsViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //check for our tripwire
                    if (viewModel.ContactReason != null)
                        return View(viewModel);

                    using (TrialScoutContext db = new TrialScoutContext())
                    {
                        ContactUsForm contactUsForm = new ContactUsForm();

                        contactUsForm.ContactId = Guid.NewGuid();
                        contactUsForm.CreatedOn = DateTime.Now;
                        contactUsForm.EmailAddress = viewModel.EmailAddress;
                        contactUsForm.FirstName = viewModel.FirstName;
                        contactUsForm.LastName = viewModel.LastName;
                        contactUsForm.Message = viewModel.Message;
                        contactUsForm.Subject = viewModel.Subject;

                        db.ContactUsForms.Add(contactUsForm);

                        db.SaveChanges();

                        //send confirmation email
                        Globals.SendEmail(ConfigurationManager.AppSettings["ContactUsSubject"].ToString(), BuildContactUsEmailBody(viewModel),
                                            new List<string>() { ConfigurationManager.AppSettings["ContactUsEmailRecipient"].ToString() });

                        return RedirectToAction("ContactConfirmation");
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), ex, null, Globals.LogLevels.Error);
            }

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult ContactConfirmation()
        {
            return View();
        }

        [HttpGet]
        public ActionResult LookUpResearchCenter()
        {
            return View();
        }

        [HttpGet]
        public ActionResult FindClinicalTrial()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Education()
        {
            return View();
        }

        public ActionResult Transparency()
        {
            return View();
        }

        public ActionResult PrivacyPolicy()
        {
            return View();
        }

        public ActionResult UserAgreement()
        {
            return View();
        }

        [Route("OurStory")]
        [Route("Home/OurStory")]
        public ActionResult OurStory()
        {
            return View();
        }

        [Route("Resources")]
        [Route("Home/Resources")]
        public ActionResult Resources()
        {
            return View();
        }

        public ActionResult SearchSection()
        {
            return View();
        }

        [Route("Partners")]
        [Route("Home/Partners")]
        public ActionResult Partners()
        {
            return View();
        }

        [Route("Top50Hospitals")]
        [Route("Home/Top50Hospitals")]
        public ActionResult Top50Hospitals()
        {
            List<Top50AwardsViewModel> viewModel = new List<Top50AwardsViewModel>();
            try
            {
                using (TrialScoutContext db = new TrialScoutContext())
                {
                    viewModel = db.ResearchCenterAliases.Where(h => h.IsTop50)
                        .GroupBy(h => h.State).Select(t => new Top50AwardsViewModel()
                        {
                            State = t.Key,
                            NameCityCenterId = t.Select(y => y.AliasName+"+"+y.City+", "+y.State+"+"+y.CenterId)
                        })
                        .OrderBy(h => h.State)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Globals.LogError(new Guid(User.Identity.GetUserId()), e, null, Globals.LogLevels.Error);
            }


            return View(viewModel);
        }

        [Route("Top50FAQ")]
        [Route("Home/Top50FAQ")]
        public ActionResult Top50FAQ()
        {
            return View();
        }

        //Home/XMLData?state=NY
        [HttpGet]
        public FileResult XMLData(string state)
        {

            List<OptiJobTempViewModel> returnData = new List<OptiJobTempViewModel>();
            using (TrialScoutContext db = new TrialScoutContext())
            {
                returnData = db.Database.SqlQuery<OptiJobTempViewModel>("Exec OptiJobTemp @state", new SqlParameter("@state", state)).ToList();
            }

            XElement xmlElements = new XElement("root", returnData.Select(i => new XElement("Value", new XElement("HealthSystemName", i.HealthSystemName), new XElement("HealthSystemId", i.HealthSystemId), new XElement("HealthSystemURL", i.HealthSystemURL), new XElement("ResearchSiteName", i.ResearchSiteName), new XElement("ResearchSiteId", i.ResearchSiteId), new XElement("ResearchSiteUrl", i.ResearchSiteUrl), new XElement("RateUrl", i.RateUrl), new XElement("State", i.State), new XElement("City", i.City), new XElement("Zip", i.Zip), new XElement("NCTID", i.NCTID), new XElement("NCTIDAsSiteUrl", i.NCTIDAsSiteUrl), new XElement("BriefTitle", i.BriefTitle), new XElement("OfficialTitle", i.OfficialTitle), new XElement("BriefDescription", i.BriefDescription), new XElement("DetailedDescription", i.DetailedDescription))));
            string fileName = String.Format(state + "_data.xml");
            var contentDis = new ContentDisposition
            {
                FileName = fileName,
                Inline = false
            };
            Response.AppendHeader("Content-Disposition", contentDis.ToString());
            return File(Encoding.UTF8.GetBytes(xmlElements.ToString()), "application/xml");
        }

        #endregion

        #region Partial Views
        [HttpGet]
        public async Task<PartialViewResult> _ResearchCenterAds(decimal? latitude, decimal? longitude, bool vertical)
        {
            using (TrialScoutContext db = new TrialScoutContext())
            {
                ResearchCenterAdsViewModel viewModel = new ResearchCenterAdsViewModel() { Vertical = vertical };
                await Task.Factory.StartNew(() =>
                {
                    // fails on localhost 
                    // TO-DO: https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator
                    //      Document process to use local emulator
                    AzureImageStorage imageStore = new AzureImageStorage(ConfigurationManager.AppSettings["TrialScoutBlobUri"], ConfigurationManager.AppSettings["ResearchCenterBlobName"],
                                                                         ConfigurationManager.AppSettings["ResearchCenterBlobKey"], ConfigurationManager.AppSettings["ResearchCenterContainerName"]);

                         viewModel.Centers = db.Database.SqlQuery<ResearchCenterAdViewModel>("exec GetAds @Latitude, @Longitude, @Radius, @ClientIP",
                                                                    new SqlParameter("@Latitude", latitude ?? 0.0m),
                                                                    new SqlParameter("@Longitude", longitude ?? 0.0m),
                                                                    new SqlParameter("@Radius", Convert.ToInt32(ConfigurationManager.AppSettings["ResearchCenterAdRadius"])),
                                                                    new SqlParameter("@ClientIP", Request.UserHostAddress))
                                                                    .ToList();

                });

                return PartialView("_ResearchCenterAds", viewModel);
            }
        }

        #endregion

        #region Functons

        public string BuildContactUsEmailBody(ContactUsViewModel viewModel)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append($"<p>Submitted at {DateTime.Now.ToString()}</p>");
            stringBuilder.Append($"<p>First Name:  {viewModel.FirstName}</p>");
            stringBuilder.Append($"<p>Last Name:  {viewModel.LastName}</p>");
            stringBuilder.Append($"<p>Email:  {viewModel.EmailAddress}</p>");
            stringBuilder.Append($"<p>Subject:  {viewModel.Subject}</p>");
            stringBuilder.Append($"<p>Message:  {viewModel.Message}</p>");

            return stringBuilder.ToString();
        }

        #endregion


    }
}