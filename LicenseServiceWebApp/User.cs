//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LicenseServiceWebApp
{
    using System;
    using System.Collections.Generic;
    
    public partial class User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public User()
        {
            this.LicenseKeys = new HashSet<LicenseKey>();
        }
    
        public System.Guid Id { get; set; }
        public string Name { get; set; }
        public string Firstname { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public Nullable<System.Guid> CpuId { get; set; }
        public Nullable<System.DateTime> ActivationDate { get; set; }
        public Nullable<bool> Deleted { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<LicenseKey> LicenseKeys { get; set; }
    }
}
