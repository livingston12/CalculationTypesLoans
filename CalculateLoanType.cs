using CalculateLoanType.ViewClass;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateLoanType
{
    public class CalculateLoanType : Base
    {
        private TypeFORMAPG _TypeFORMAPG { get; set; }
        private int DayToadd = 0;
        private decimal montoTotalPrestamo { get; set; }
        private decimal Balance_actual { get; set; }
        private bool isRecalculate { get; set; }
        int day = 0;

        public string CalculateAmortization(string json, bool roundFee = false)
        {
            View_ListFincaciamientos lstFina = new View_ListFincaciamientos();
            lstFina.ListFinanciamientos = new List<View_financiamientos>();
            View_financiamientos Financiamiento = new View_financiamientos();
            lstFina.message = new message() { Is_Success = true };
            bool Is_manual = false;
            try
            {

                Financiamiento = JsonConvert.DeserializeObject<View_financiamientos>(json);
                lstFina.message = FinanciamientoIsvalid(Financiamiento);

                if (lstFina.message.Is_Success == false)
                    return JsonConvert.SerializeObject(lstFina);

                montoTotalPrestamo = Financiamiento.amount;
                Balance_actual = Financiamiento.amount;
                _TypeFORMAPG = (TypeFORMAPG)Enum.Parse(typeof(TypeFORMAPG), Financiamiento.paymentMethod.ToString());

                DayToadd = 1;
                Financiamiento.AdditionalFees = Financiamiento.listAdditionalFees.Count();
                if (TypeFORMAPG.MENSUAL != _TypeFORMAPG)
                {
                    DayToadd = getCurrentDayToadd(_TypeFORMAPG);
                }
                /* Si existen algunas cuotas adiccionales se le resta el capital al monto total 
                 * para que el recalculo se haga en base al total sin las cuotas adiccionales */
                if (Financiamiento.AdditionalFees > 0 && (TypeInteres)Financiamiento.TypeFin != TypeInteres.MANUAL)
                    montoTotalPrestamo -= Financiamiento.listAdditionalFees.Sum(x => x.capital);

                switch ((TypeInteres)Enum.Parse(typeof(TypeInteres), Financiamiento.TypeFin.ToString()))
                {

                    case TypeInteres.FIJO:
                        lstFina.ListFinanciamientos.AddRange(calcularInteresFijo(Financiamiento));
                        break;
                    case TypeInteres.INSOLUTO:
                        lstFina.ListFinanciamientos.AddRange(calcularInteresINSOLUTO(Financiamiento));
                        break;
                    case TypeInteres.SOLOINTERES:
                        lstFina.ListFinanciamientos.AddRange(calcularInteresSOLOINTERES(Financiamiento));
                        break;
                    case TypeInteres.SININTERES:
                        lstFina.ListFinanciamientos.AddRange(calcularInteresSININTERES(Financiamiento));
                        break;
                    case TypeInteres.VINSOLUTO:
                        lstFina.ListFinanciamientos.AddRange(calcularInteresVINSOLUTO(Financiamiento));
                        break;
                    case TypeInteres.MANUAL:
                        if (Financiamiento.listAdditionalFees.Count() < 1)
                        {
                            lstFina.message = new message() { Is_Success = false, Message = "This option is not allowed by the system you need to insert List of Additional Fees" };
                            break;
                        }
                        Balance_actual = Financiamiento.listAdditionalFees.Sum(x => x.capital);
                        lstFina.ListFinanciamientos.AddRange(recalcularCuotas(Financiamiento.listAdditionalFees.OrderBy(x => x.ExpirationDate).ToList()));
                        Is_manual = true;
                        break;

                }

                // Se calcula siempre excepto cuando es un tipo de interes manual
                if (Is_manual == false)
                {
                    // Se recalculan las cuotas si existen cuotas adiccionales
                    if (Financiamiento.AdditionalFees > 0)
                    {
                        lstFina.ListFinanciamientos.AddRange(Financiamiento.listAdditionalFees);
                        Balance_actual = lstFina.ListFinanciamientos.Sum(x => x.capital);
                        lstFina.ListFinanciamientos = recalcularCuotas(lstFina.ListFinanciamientos.OrderBy(x => x.ExpirationDate.Date).ThenByDescending(x => x.FeeType).ToList());
                    }
                    if (roundFee)
                    {
                        // Se redondean siempre las cuotas para que queden en el multiplo de 5 mas mayor
                        lstFina.ListFinanciamientos = redondearCuotas(lstFina.ListFinanciamientos);
                    }
                }

            }
            catch (Exception ex)
            {
                lstFina.message.Is_Success = false;
                lstFina.message.Message = "Unexpected error: The amortization could not be processed";

            }

            if (lstFina.message.Is_Success == false)
                lstFina.ListFinanciamientos = new List<View_financiamientos>();


            return JsonConvert.SerializeObject(lstFina);
        }

        private List<View_financiamientos> calcularInteresFijo(View_financiamientos financiamiento)
        {
            List<View_financiamientos> ListFinanciamientos = new List<View_financiamientos>();
            for (int i = 0; i < financiamiento.normalFees; i++)
            {
                DateTime fecha = ReturFechaFormadePago(_TypeFORMAPG, financiamiento.ExpirationDate, i, ref DayToadd);

                if (i > 0)
                {

                    financiamiento = new View_financiamientos()
                    {
                        capital = financiamiento.capital,
                        normalFees = financiamiento.normalFees,
                        PerInt = financiamiento.PerInt,
                        ExpirationDate = financiamiento.ExpirationDate,
                        date = financiamiento.date

                    };
                }
                financiamiento.ExpirationDate = fecha;
                financiamiento.date = returnDate(fecha);
                financiamiento.numFee = i + 1;
                financiamiento.capital = montoTotalPrestamo / financiamiento.normalFees;
                financiamiento.interest = (financiamiento.PerInt * montoTotalPrestamo) / 100;
                financiamiento.amount = financiamiento.capital + financiamiento.interest;
                Balance_actual = montoTotalPrestamo - (financiamiento.capital * financiamiento.numFee?? 1);
                financiamiento.Balance = Balance_actual;
                financiamiento.FeeType = TypeCuota.NORMAL.ToString();
                ListFinanciamientos.Add(financiamiento);
            }
            return ListFinanciamientos;
        }

        private List<View_financiamientos> recalcularCuotas(List<View_financiamientos> listFinanciamientos)
        {
            isRecalculate = false;
            List<View_financiamientos> listFinanciamientos2 = new List<View_financiamientos>();
            View_financiamientos fincaciamiento = new View_financiamientos();
            int i = 0;
            foreach (View_financiamientos fincaciamiento2 in listFinanciamientos)
            {
              
                fincaciamiento = fincaciamiento2;
                fincaciamiento.FeeType = fincaciamiento.TypeFin == (int)TypeInteres.MANUAL ? TypeCuota.NORMAL.ToString() : fincaciamiento.FeeType;
                if (i > 0)
                {
                    fincaciamiento = new View_financiamientos()
                    {
                        capital = fincaciamiento.capital,
                        normalFees = fincaciamiento.normalFees,
                        PerInt = fincaciamiento.PerInt,
                        ExpirationDate = fincaciamiento.ExpirationDate,
                        date = fincaciamiento.date,
                        FeeType = fincaciamiento.FeeType,
                        interest = fincaciamiento.interest
                    };
                }
                fincaciamiento.numFee = i + 1;

                fincaciamiento.amount = fincaciamiento.capital + fincaciamiento.interest;
                Balance_actual -= fincaciamiento.capital;


                fincaciamiento.Balance = Balance_actual < 0 ? 0 : Balance_actual;
                listFinanciamientos2.Add(fincaciamiento);
                i++;
            }

            return listFinanciamientos2;
        }

        private List<View_financiamientos> calcularInteresVINSOLUTO(View_financiamientos fincaciamiento)
        {
            List<View_financiamientos> Listfincaciamientos = new List<View_financiamientos>();
            decimal totalCal = 0, porint = 0;
            decimal totalAdicionales = fincaciamiento.listAdditionalFees.Sum(x => x.capital);
            montoTotalPrestamo = fincaciamiento.amount - totalAdicionales;
            porint = decimal.Parse(fincaciamiento.PerInt.ToString());



            #region ----------------calculo 1 -----------------
            totalCal = montoTotalPrestamo / fincaciamiento.normalFees;

            #endregion  // Finalizacion calulo 2


            #region ----------------calculo 2 -----------------

            for (int i = 0; i < fincaciamiento.normalFees; i++)
            {
                DateTime fecha = ReturFechaFormadePago(_TypeFORMAPG, fincaciamiento.ExpirationDate, i,ref day);

                if (i == 0)
                    fincaciamiento.interest = fincaciamiento.amount * (porint / 100);
                else
                {
                    fincaciamiento = new View_financiamientos()
                    {
                        capital = fincaciamiento.capital,
                        normalFees = fincaciamiento.normalFees,
                        date = fincaciamiento.date,
                        PerInt = fincaciamiento.PerInt,
                        amount = fincaciamiento.amount,
                        interest = fincaciamiento.interest,
                        Balance = fincaciamiento.Balance,
                        ExpirationDate = fincaciamiento.ExpirationDate
                    };
                    fincaciamiento.interest = montoTotalPrestamo * porint / 100;
                }


                fincaciamiento.ExpirationDate = fecha;
                fincaciamiento.date = returnDate(fecha);
                fincaciamiento.capital = totalCal;
                fincaciamiento.amount = fincaciamiento.capital + fincaciamiento.interest;
                fincaciamiento.numFee = i + 1;
                Balance_actual = montoTotalPrestamo - fincaciamiento.capital;
                Balance_actual = Balance_actual < 0 ? 0 : Balance_actual;
                fincaciamiento.Balance = Balance_actual;
                montoTotalPrestamo = Balance_actual;
                fincaciamiento.FeeType = TypeCuota.NORMAL.ToString();
                Listfincaciamientos.Add(fincaciamiento);

            }


            #endregion  // Finalizacion calulo 3

            return Listfincaciamientos;
        }

        private List<View_financiamientos> calcularInteresSININTERES(View_financiamientos fincaciamiento)
        {
            List<View_financiamientos> Listfincaciamientos = new List<View_financiamientos>();
            decimal totalAdicionales = fincaciamiento.listAdditionalFees.Sum(x => x.capital);
           
            montoTotalPrestamo = fincaciamiento.amount - totalAdicionales;
            for (int i = 0; i < fincaciamiento.normalFees; i++)
            {
                DateTime fecha = ReturFechaFormadePago(_TypeFORMAPG, fincaciamiento.ExpirationDate, i,ref day);

                if (i > 0)
                {
                    fincaciamiento = new View_financiamientos()
                    {
                        amount = montoTotalPrestamo,
                        Balance = fincaciamiento.Balance,
                        capital = fincaciamiento.capital,
                        interest = fincaciamiento.interest,
                        PerInt = fincaciamiento.PerInt,
                        numFee = fincaciamiento.numFee,
                        normalFees = fincaciamiento.normalFees,
                        date = fincaciamiento.date,
                        ExpirationDate = fincaciamiento.ExpirationDate
                    };
                }

                fincaciamiento.ExpirationDate = fecha;
                fincaciamiento.date = returnDate(fecha);
                fincaciamiento.interest = 0;
                fincaciamiento.capital = montoTotalPrestamo / fincaciamiento.normalFees;

                fincaciamiento.amount = fincaciamiento.interest + fincaciamiento.capital;
                fincaciamiento.numFee = i + 1;
                fincaciamiento.Balance = montoTotalPrestamo - (fincaciamiento.capital * fincaciamiento.numFee?? 1);

                fincaciamiento.FeeType = TypeCuota.NORMAL.ToString();
                Listfincaciamientos.Add(fincaciamiento);
            }


            return Listfincaciamientos;
        }

        private List<View_financiamientos> calcularInteresSOLOINTERES(View_financiamientos fincaciamiento)
        {
            List<View_financiamientos> Listfincaciamientos = new List<View_financiamientos>();
            decimal totalAdicionales = fincaciamiento.listAdditionalFees.Sum(x => x.capital);
            int day = 0;
            montoTotalPrestamo = fincaciamiento.amount - totalAdicionales;
            for (int i = 0; i < fincaciamiento.normalFees; i++)
            {
                DateTime fecha = ReturFechaFormadePago(_TypeFORMAPG, fincaciamiento.ExpirationDate, i,ref day);

                if (i > 0)
                {
                    fincaciamiento = new View_financiamientos()
                    {
                        amount = montoTotalPrestamo,
                        Balance = fincaciamiento.Balance,
                        capital = fincaciamiento.capital,
                        interest = fincaciamiento.interest,
                        PerInt = fincaciamiento.PerInt,
                        numFee = fincaciamiento.numFee,
                        normalFees = fincaciamiento.normalFees,
                        date = fincaciamiento.date,
                        ExpirationDate = fincaciamiento.ExpirationDate
                    };
                }

                fincaciamiento.ExpirationDate = fecha;
                fincaciamiento.date = returnDate(fecha);
                fincaciamiento.interest = fincaciamiento.amount * fincaciamiento.PerInt / 100;               
                fincaciamiento.capital = i == fincaciamiento.normalFees - 1 ? montoTotalPrestamo : 0;
                fincaciamiento.amount = fincaciamiento.interest + fincaciamiento.capital;
                fincaciamiento.Balance = montoTotalPrestamo - fincaciamiento.capital;
                fincaciamiento.numFee = i + 1;
                fincaciamiento.FeeType = TypeCuota.NORMAL.ToString();
                Listfincaciamientos.Add(fincaciamiento);
            }


            return Listfincaciamientos;
        }

        private List<View_financiamientos> calcularInteresINSOLUTO(View_financiamientos fincaciamiento)
        {
            List<View_financiamientos> Listfincaciamientos = new List<View_financiamientos>();
            decimal _Base = 0, valor = 0, totalCal = 0, porint = 0;
            decimal totalAdicionales = fincaciamiento.listAdditionalFees.Sum(x => x.capital);
           
            montoTotalPrestamo = fincaciamiento.amount - totalAdicionales;
            porint = decimal.Parse(fincaciamiento.PerInt.ToString());
            
            _Base = (1 + porint / 100);
            #region -----------------calculo 1---------------
            for (int i = 0; i < fincaciamiento.normalFees; i++)
            {
                if (i == 0)
                {
                    valor = _Base;
                    continue;
                }


                valor *= _Base;

            }

            totalCal = 1 / valor;
            #endregion // Finalizacion calulo 1

            #region ----------------calculo 2 -----------------
            totalCal = ((montoTotalPrestamo) / ((1 - totalCal) / (porint / 100)));

            #endregion  // Finalizacion calulo 2


            #region ----------------calculo 3 -----------------

            for (int i = 0; i < fincaciamiento.normalFees; i++)
            {
                DateTime fecha = ReturFechaFormadePago(_TypeFORMAPG, fincaciamiento.ExpirationDate, i,ref day);

                if (i == 0)
                    fincaciamiento.interest = montoTotalPrestamo * (porint / 100);
                else
                {
                    fincaciamiento = new View_financiamientos()
                    {
                        capital = fincaciamiento.capital,
                        normalFees = fincaciamiento.normalFees,
                        date = fincaciamiento.date,
                        PerInt = fincaciamiento.PerInt,
                        amount = fincaciamiento.amount,
                        interest = fincaciamiento.interest,
                        Balance = fincaciamiento.Balance,
                        ExpirationDate = fincaciamiento.ExpirationDate
                    };
                    fincaciamiento.interest = montoTotalPrestamo * porint / 100;
                }

                fincaciamiento.ExpirationDate = fecha;
                fincaciamiento.date = returnDate(fecha);
                fincaciamiento.capital = totalCal - fincaciamiento.interest;

                fincaciamiento.amount = fincaciamiento.capital + fincaciamiento.interest;
                fincaciamiento.numFee = i + 1;
                Balance_actual = montoTotalPrestamo - fincaciamiento.capital;
                Balance_actual = Balance_actual < 0 ? 0 : Balance_actual;
                fincaciamiento.Balance = Balance_actual;
                montoTotalPrestamo = Balance_actual;
                fincaciamiento.FeeType = TypeCuota.NORMAL.ToString();
                Listfincaciamientos.Add(fincaciamiento);

            }


            #endregion  // Finalizacion calulo 3

            return Listfincaciamientos;
        }

    }
}
