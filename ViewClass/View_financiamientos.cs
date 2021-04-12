using System;
using System.Collections.Generic;

namespace CalculateLoanType.ViewClass
{
   public class View_financiamientos
    {
        public int? numFee { get; set; }
        public int TypeFin { get; set; }
        public decimal amount { get; set; }
        public decimal Balance { get; set; }
        public decimal interest { get; set; }
        public decimal capital { get; set; }
        public string date { get; set; }
        public int paymentMethod { get; set; }
        public int interestRate { get; set; }
        public decimal PerInt { get; set; }
        public int normalFees { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int AdditionalFees { get; set; }
        public string FeeType { get; set; }
        public List<View_financiamientos> listAdditionalFees { get; set; }
    }
}
