using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstRobot
{
    public class LicenseServiceController
    {
        public int getLincenceRequest(string licenseNumber)
        {
            int numberOne = int.Parse(licenseNumber.Substring(3, 1));
            int numberTwo = int.Parse(licenseNumber.Substring(8, 1));
            int numberThree = int.Parse(licenseNumber.Substring(11, 1));
            int numberFour = int.Parse(licenseNumber.Substring(17, 1));

            return numberOne + numberTwo - numberThree + numberFour + 19804;
        }

    }
}
