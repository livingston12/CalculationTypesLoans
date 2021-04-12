using CalculateLoanType.ViewClass;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateLoanType
{
    public enum TypeInteres
    {
        FIJO = 1,
        INSOLUTO = 2,
        SOLOINTERES = 3,
        SININTERES = 4,
        VINSOLUTO = 5,
        MANUAL = 6
    }
    public enum TypeFORMAPG
    {
        MENSUAL = 1,
        QUINCENAL = 2,
        SEMANAL = 3,
        DIARIO = 4
    }
    public enum TypeCuota
    {
        NORMAL = 1,
        ADDITIONAL = 2
    }
    public class Base
    {
        public message FinanciamientoIsvalid(View_financiamientos fincaciamientos, bool optional = true)
        {
            message message = new message() { Message = "", Is_Success = true };
            string incorrecto = " Incorrect";

            if (string.IsNullOrWhiteSpace(fincaciamientos.amount.ToString()) || fincaciamientos.amount < 1)
                message.Message += Environment.NewLine + "- amount" + incorrecto;
            if (string.IsNullOrWhiteSpace(fincaciamientos.interestRate.ToString()) || fincaciamientos.interestRate < 1)
                message.Message += Environment.NewLine + "- Interest rate" + incorrecto;
            if (string.IsNullOrWhiteSpace(fincaciamientos.PerInt.ToString()) || fincaciamientos.PerInt < 1)
                message.Message += Environment.NewLine + "- Percent interest" + incorrecto;
            if (string.IsNullOrWhiteSpace(fincaciamientos.normalFees.ToString()) || fincaciamientos.normalFees < 1)
                message.Message += Environment.NewLine + "- Normal fees" + incorrecto ;

            if (message.Message.Count() > 0)
                message.Is_Success = false;

            return message;

        }

        public DateTime ReturFechaFormadePago(TypeFORMAPG typeFORMAPG, DateTime fechaVencimiento, int index,ref int DayToadd)
        {
            DateTime fechaReturn = DateTime.Now;
            bool dayIncurrent = false;
            if (fechaVencimiento.Day == DateTime.DaysInMonth(fechaVencimiento.Year, fechaVencimiento.Month))
                dayIncurrent = true;
          

            if (typeFORMAPG != TypeFORMAPG.MENSUAL)
            {
                if (DayToadd != 0)
                {
                    DayToadd = getCurrentDayToadd(typeFORMAPG);
                }
               
            }

            switch (typeFORMAPG)
            {
                case TypeFORMAPG.MENSUAL:
                    fechaReturn = dayIncurrent ? fechaVencimiento.AddDays(1).AddMonths(1).AddDays(-1) : fechaVencimiento.AddMonths(1);
                    break;
                case TypeFORMAPG.DIARIO:
                case TypeFORMAPG.SEMANAL:
                case TypeFORMAPG.QUINCENAL:
                    fechaReturn = fechaVencimiento.AddDays(DayToadd);
                    break;

            }


            return index != 0 ? fechaReturn : fechaVencimiento;
        }

        public int getCurrentDayToadd(TypeFORMAPG _TypeFORMAPG)
        {
            int DayToadd = 0;
            if (TypeFORMAPG.MENSUAL != _TypeFORMAPG)
            {
                switch (_TypeFORMAPG)
                {
                    case TypeFORMAPG.QUINCENAL:
                        DayToadd = 15;
                        break;
                    case TypeFORMAPG.SEMANAL:
                        DayToadd = 7;
                        break;
                    case TypeFORMAPG.DIARIO:
                        DayToadd = 1;
                        break;
                }
            }
            return DayToadd;
        }

        public string returnDate(DateTime fecha)
        {
            string fechaReturn = string.Format("{0}-{1}-{2}", fecha.Day, returMonthName(fecha.Month), fecha.Year);

            return fechaReturn;
        }
        public static string returMonthName(int month)
        {
            string[] str = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
            return str[month - 1];
        }

        public List<View_financiamientos> redondearCuotas(List<View_financiamientos> listFinanciamientos)
        {

            int redondeado = 0;
            decimal totalResiduo = 0;
            foreach (var fin in listFinanciamientos)
            {
                if (fin.amount % 5 == 0)
                    continue;
                else
                {
                    redondeado = decimal.ToInt32((fin.amount / 5));
                    redondeado = (redondeado + 1) * 5;
                    totalResiduo = redondeado - fin.amount;
                    fin.interest += totalResiduo;                    
                    fin.amount += totalResiduo;
                }


                listFinanciamientos.Where(x => x.numFee == fin.numFee)
                    .Select(x => new { fin }
                    );

            }
            return listFinanciamientos;
        }

    }


}
