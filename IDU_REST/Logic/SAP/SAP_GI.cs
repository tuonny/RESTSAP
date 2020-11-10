﻿using IDU_REST.Models;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace IDU_REST.Logic.SAP
{
    public class SAP_GI
    {
        public static GI_HEADER GetData(SAPbobsCOM.Company oCompany, int id)
        {
            Utils control = new Utils();
            List<GI_DETAIL> details = new List<GI_DETAIL>();
            XDocument xDoc = new XDocument();
            XDocument xDocDetail = new XDocument();

            string sql = "select U_PrimaryNumberingPOS, U_DocumentNumberingPOS, docentry, docnum, DocDate, DocDueDate, TaxDate from oige where docentry = " + id;
            Recordset rs = control._IDU_Recordset(oCompany, sql);
            xDoc = XDocument.Parse(rs.GetAsXML());

            sql = "select t1.LineNum, ItemCode, dscription, UomEntry, UomCode, Quantity, Price, t2.WhsCode, t2.WhsName from ige1 t1 " +
                "join owhs t2 on t1.WhsCode = t2.WhsCode where t1.docentry = " + id;
            rs = control._IDU_Recordset(oCompany, sql);
            xDocDetail = XDocument.Parse(rs.GetAsXML());

            XElement xEle = xDoc.Element("BOM").Element("BO").Element("oige").Element("row");
            XElement xEleDetail = xDocDetail.Element("BOM").Element("BO").Element("ige1");

            foreach (var item in xEleDetail.Elements("row"))
            {
                GI_DETAIL model_detail = new GI_DETAIL()
                {
                    LineNum = Convert.ToInt32(item.Element("LineNum").Value),
                    ItemCode = item.Element("ItemCode").Value,
                    Description = item.Element("dscription").Value,
                    UomEntry = Convert.ToInt32(item.Element("UomEntry").Value),
                    UomCode = item.Element("UomCode").Value,
                    Quantity = Convert.ToDouble(item.Element("Quantity").Value),
                    Price = Convert.ToDouble(item.Element("Price").Value),
                    WarehouseCode = item.Element("WhsCode").Value,
                    WarehouseName = item.Element("WhsName").Value
                };

                details.Add(model_detail);
            }

            GI_HEADER model = new GI_HEADER()
            {
           
                DocEntry = xEle.Element("docentry").Value,
                DocNum = xEle.Element("docnum").Value,
                PostingDate  = DateTime.ParseExact(xEle.Element("DocDate").Value, "yyyyMMdd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None),
                DocDueDate = DateTime.ParseExact(xEle.Element("DocDueDate").Value, "yyyyMMdd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None),
                TaxDate = DateTime.ParseExact(xEle.Element("TaxDate").Value, "yyyyMMdd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None),
                lines = details
            };

            return model;
        }
        public static string AddData(Company oCompany, GI_HEADER model)
        {
            Documents oObject = null;
            RTNMANVAL rtn = new RTNMANVAL();
            int errCode; string errMessage, strResult = "";

            try
            {
                oCompany.StartTransaction();

                oObject = oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenExit);

                oObject.DocDate = model.PostingDate;
                oObject.DocDueDate = model.DocDueDate;
                oObject.TaxDate = model.TaxDate;
                if (model.Reference2.Length > 11)
                {
                    oObject.Reference2 = model.Reference2.Substring(0, 11);
                }
                else
                {
                    oObject.Reference2 = model.Reference2;
                }
                oObject.Comments = model.Remarks;
                oObject.DocType = BoDocumentTypes.dDocument_Items;
            

                for (int i = 0; i < model.lines.Count; i++)
                {
                    oObject.Lines.ItemCode = model.lines[i].ItemCode;
                    oObject.Lines.WarehouseCode = model.lines[i].WarehouseCode;
                    oObject.Lines.Quantity = model.lines[i].Quantity;
                    oObject.Lines.UnitPrice = model.lines[i].Price;
                    oObject.Lines.ProjectCode = model.lines[i].ProjectCode;
                    oObject.Lines.CostingCode = model.lines[i].CostCenter;
                    oObject.Lines.AccountCode = model.lines[i].AccountCode;
                    oObject.Lines.Add();
                }

                int addStatus = oObject.Add();

                if (addStatus == 0)
                {
                    if (strResult == "")
                    {
                        strResult = oCompany.GetNewObjectKey();
                    }
                    else
                    {
                        strResult = strResult + " | " + oCompany.GetNewObjectKey();
                    }
                }
                else
                {
                    if (oCompany.InTransaction)
                    {
                        oCompany.EndTransaction(BoWfTransOpt.wf_RollBack);
                    }

                    oCompany.GetLastError(out errCode, out errMessage);
                    throw new Exception("Error Code : " + errCode + " | Error Message : " + errMessage);
                }
                oCompany.EndTransaction(BoWfTransOpt.wf_Commit);
            }
            catch (Exception)
            {
                if (oCompany.InTransaction)
                {
                    oCompany.EndTransaction(BoWfTransOpt.wf_RollBack);
                };
                throw;
            }

            return strResult;
        }
    }
}