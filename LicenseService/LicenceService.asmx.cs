using LicenseService;
using System;
using System.Linq;
using System.Web.Services;


namespace LicenceService
{
    /// <summary>
    /// Summary description for WebService
    /// </summary>
    [WebService(Namespace = "http://yournewskills.ru/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {

        [WebMethod]
        public LicenseRequest getLincenseAndCPURequest(string licenseNumber, string cpuId)
        {
            LicenseRequest request = new LicenseRequest();
            request.ExcerciseNumber = getExcersiceRequest(licenseNumber);
            request.LicenseExistence = checkLicenseExistenz(licenseNumber, cpuId);
            return request;
        }


        private int getExcersiceRequest(string licenseNumber)
        {
            int numberOne = int.Parse(licenseNumber.Substring(3, 1));
            int numberTwo = int.Parse(licenseNumber.Substring(8, 1));
            int numberThree = int.Parse(licenseNumber.Substring(11, 1));
            int numberFour = int.Parse(licenseNumber.Substring(17, 1));

            return numberOne + numberTwo - numberThree + numberFour + 19804;
        }

        //Check if license exist
        private bool checkLicenseExistenz(string licenseNumber, string cpuId)
        {

            EmaCalculatorEntities context = new EmaCalculatorEntities ();

            var licenseFromDb = context.LicenseKeys.ToList().Where(x => x.LicenseKey1 == licenseNumber).FirstOrDefault();

            if (licenseFromDb != null)
            {
                //Если ключ и cpuId совпадают с ключом и cpuId в базе => true
                if (licenseFromDb.LicenseKey1 == licenseNumber && licenseFromDb.UserCpuId == cpuId)
                {
                    return true;
                }
                //Если ключ совпдадате с ключом в базе и cpuId пустое => treu
                else if (licenseFromDb.LicenseKey1 == licenseNumber && (licenseFromDb.UserCpuId == null || licenseFromDb.UserCpuId == string.Empty))
                {
                    licenseFromDb.LicenseKeyUsed = true;
                    licenseFromDb.ActivationDate = DateTime.Now;
                    licenseFromDb.ApplicationType = "DesktopApplication";
                    licenseFromDb.UserCpuId = cpuId;
                    context.SaveChanges();
                    //write cpuId to the table
                    return true;
                }
                //во всех других случаях false
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public class LicenseRequest
        {
            public int ExcerciseNumber { get; set; }
            public bool LicenseExistence { get; set; }
        }
    }
}
