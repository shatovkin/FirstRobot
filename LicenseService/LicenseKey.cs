//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LicenseService
{
    using System;
    using System.Collections.Generic;
    
    public partial class LicenseKey
    {
        public int Id { get; set; }
        public string LicenseKey1 { get; set; }
        public Nullable<bool> LicenseKeyUsed { get; set; }
        public Nullable<System.Guid> UserId { get; set; }
        public string UserCpuId { get; set; }
        public Nullable<System.DateTime> ActivationDate { get; set; }
        public string ApplicationType { get; set; }
    }
}
