using DataRetrieval.Commands;
using DataRetrieval.Models;
using DataRetrieval.Views;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;

namespace DataRetrieval.ViewModels
{
    public class DataRetrievalViewModel : INotifyPropertyChanged
    {
        private string isin = string.Empty;
        private string bbgIdGbl = string.Empty;
        private string bbgIdUnq = string.Empty;
        private string ticker = string.Empty;
        private string sedol = string.Empty;
        private string identifier = string.Empty;
        private string identifierValue = string.Empty;
        private string UserID = string.Empty;
        private string Password = string.Empty;
        private string Source = string.Empty;
        private string exportPath = string.Empty;
        private ICommand onSubmit;
        private ICommand onReset;
        private ICommand onBack;
        private ICommand onExport;
        private OracleConnection dBConnction;
        private ExcelExport exlexp;
        private Visibility isGridDataVisible;
        private Visibility isControlVisible;
        private DataTable dataRetrieved;
        private DataTable tableResult; 
        private int _sFilePathIndex;
        private FilePathModel _sFilePath;
        private ObservableCollection<FilePathModel> _filePaths;
        public event PropertyChangedEventHandler PropertyChanged;

        public DataRetrievalViewModel()
        {
            isControlVisible = Visibility.Visible;
            IsGridDataVisible = Visibility.Collapsed;
            tableResult = new DataTable();
            GetDbDetails();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand OnExport
        {
            get
            {
                return onExport = new CommandHandler(() => Export(), true);
            }
        }

        public ICommand OnReset
        {
            get
            {
                return onReset = new CommandHandler(() => Reset(), true);
            }
        }

        public ICommand OnBack
        {
            get
            {
                return onBack = new CommandHandler(() => Back(), true);
            }
        }

        public ICommand OnSubmit
        {
            get
            {
                return onSubmit ?? (onSubmit = new CommandHandler(() => Connect(), true));
            }
        }

        public Visibility IsControlVisible
        {
            get
            {
                return isControlVisible;
            }
            set
            {
                isControlVisible = value;
                this.OnPropertyChanged("IsControlVisible");
            }
        }

        public Visibility IsGridDataVisible
        {
            get
            {
                return isGridDataVisible;
            }
            set
            {
                isGridDataVisible = value;
                this.OnPropertyChanged("IsGridDataVisible");
            }
        }

        public string Isin
        {
            get
            {
                return isin;
            }
            set
            {
                isin = value;
                this.OnPropertyChanged("isin");
            }
        }

        public string BbgIdGbl
        {
            get
            {
                return bbgIdGbl;
            }
            set
            {
                bbgIdGbl = value;
                this.OnPropertyChanged("bbgIdGbl");
            }
        }
        
        public string BbgIdUnq
        {
            get
            {
                return bbgIdUnq;
            }
            set
            {
                bbgIdUnq = value;
                this.OnPropertyChanged("bbgIdUnq");
            }
        }
        
        public string Ticker
        {
            get
            {
                return ticker;
            }
            set
            {
                ticker = value;
                this.OnPropertyChanged("ticker");
            }
        }
        
        public string Sedol
        {
            get
            {
                return sedol;
            }
            set
            {
                sedol = value;
                this.OnPropertyChanged("sedol");
            }
        }

        public ObservableCollection<FilePathModel> FilePaths
        {
            get { return _filePaths; }
            set
            {
                _filePaths = value;
                this.OnPropertyChanged("FilePaths");
            }
        }
                
        public FilePathModel SFilePath
        {
            get { return _sFilePath; }
            set
            {
                if (SFilePath != null && value != null && value.Name != SFilePath.Name)
                {
                    LoadData(value.Name);
                }
                _sFilePath = value;
                this.OnPropertyChanged("SFilePath");
            }
        }
              
        public int SFilePathIndex
        {
            get { return _sFilePathIndex; }
            set
            {
                _sFilePathIndex = value;
                this.OnPropertyChanged("SFilePathIndex");
            }
        }

        public DataTable TableResult
        {
            get { return tableResult; }
            set { tableResult = value; this.OnPropertyChanged("TableResult"); }
        }

        public void Export()
        {
            if (TableResult != null && TableResult.Rows.Count > 0)
            {
                SaveFileDialog exportDialog = new SaveFileDialog();
                exportDialog.FileName = string.Format("StagingAreaData_{0}.xls", DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                if (exportDialog.ShowDialog() == DialogResult.OK)
                {
                    exportPath = exportDialog.FileName;
                    exlexp = new ExcelExport();
                    exlexp.CreateExcel(TableResult, exportPath);
                }
            }
        }

        public void GetDbDetails()
        {
            XmlDocument DbConfigsXml = new XmlDocument();
            DbConfigsXml.Load(@"..\..\Configurations\DBConfigurations.xml");
            XmlNodeList xmlNodeList = DbConfigsXml.SelectNodes("/DatabaseConfiguration");

            foreach (XmlNode xmlNode in xmlNodeList)
            {
                UserID = xmlNode["UserId"].InnerText;
                Password = xmlNode["Password"].InnerText;
                Source = xmlNode["Source"].InnerText;
            }
        }

        public void Connect()
        {
            try
            {
                if (IsValid())
                {
                    identifier = string.Empty;
                    if (!string.IsNullOrEmpty(Isin))
                    {
                        identifier = "sbitm.Id_ISIN";
                        identifierValue = Isin;
                    }
                    else if (!string.IsNullOrEmpty(BbgIdGbl))
                    {
                        identifier = "sbitm.ID_BB_GLOBAL";
                        identifierValue = BbgIdGbl;
                    }
                    else if (!string.IsNullOrEmpty(BbgIdUnq))
                    {
                        identifier = "sbitm.ID_BB_UNIQUE";
                        identifierValue = BbgIdUnq;
                    }
                    else if (!string.IsNullOrEmpty(Ticker))
                    {
                        identifier = "sbitm.TICKER";
                        identifierValue = Ticker;
                    }
                    else if (!string.IsNullOrEmpty(Sedol))
                    {
                        identifier = "sbitm.ID_SEDOL1";
                        identifierValue = Sedol;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Please enter atleast one Identifier value to proceed.", "Information");
                        return;
                    }


                    dBConnction = new OracleConnection();
                    dBConnction.ConnectionString = string.Format("User Id={0};Password={1};Data Source={2}", UserID, Password, Source);
                    dBConnction.Open();
                    dataRetrieved = new DataTable("");

                    if (dBConnction.State == ConnectionState.Open)
                    {
                        OracleCommand pullRecord = dBConnction.CreateCommand();
                        string Stmnt = "select sbitm.TICKER, sbitm.Id_ISIN, sbitm.ID_BB_UNIQUE, sbitm.ID_SEDOL1, sbitm.ID_BB_GLOBAL, sbitm.yellow_key, sbres.filepath, sbreq.usedfieldsline, sbreq.usedfieldsline2, sbitm.serializeddataobjectpart1, sbitm.serializeddataobjectfallback from SA_BBBO_ITEM sbitm inner join sa_bbbo_response sbres on sbres.id = sbitm.FKRESPONSEID inner join sa_bbbo_request sbreq on sbreq.id = sbres.FKREQUESTID";
                        string query = string.Format("{0} Where {1} = '{2}'", Stmnt, identifier, identifierValue);
                        pullRecord.CommandText = query;
                        OracleDataReader reader = pullRecord.ExecuteReader();
                        dataRetrieved.Load(reader);
                    }

                    if (dataRetrieved.Rows.Count == 0)
                    {
                        System.Windows.MessageBox.Show("No data found in staging area corresponding to provided identifier value.", "Information");
                        return;
                    }
                    InitialComboLoad(dataRetrieved);
                    LoadData("ALL FILES");
                }
            }

            finally
            {
                CloseResources();
                Reset();
            }

        }

        public void LoadData(string FileName)
        {
            DataTable Newdt;

            if (FileName.ToUpper() == "ALL FILES")
            {
                Newdt = Transpose(dataRetrieved);
            }
            else
            {
                DataTable FilteredData = dataRetrieved.AsEnumerable().Where(x => x.ItemArray[6].ToString().ToUpper().Contains(FileName)).CopyToDataTable();
                Newdt = Transpose(FilteredData);
            }
            OpenDataGridView(Newdt);
        }

        public void OpenDataGridView(DataTable dt)
        {
            IsControlVisible = Visibility.Collapsed;
            IsGridDataVisible = Visibility.Visible;
            TableResult = dt;
        }

        public void Reset()
        {
            Isin = string.Empty;
            BbgIdGbl = string.Empty;
            BbgIdUnq = string.Empty;
            Ticker = string.Empty;
            Sedol = string.Empty;
        }

        public void Back()
        {
            IsControlVisible = Visibility.Visible;
            IsGridDataVisible = Visibility.Collapsed;
        }

        public void InitialComboLoad(DataTable dt)
        {
            FilePaths = new ObservableCollection<FilePathModel>() { new FilePathModel { Id = 0, Name = "All FILES" } };

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int j = dt.Columns.IndexOf("FILEPATH");
                int k = Convert.ToString(dt.Rows[i].ItemArray[j]).IndexOf('.');
                FilePaths.Add(new FilePathModel { Id = i + 1, Name = Convert.ToString(dt.Rows[i].ItemArray[j]).ToUpper() });
            }

            SFilePathIndex = 0;
        }

        private DataTable Transpose(DataTable dt)
        {
            DataTable dtNew = new DataTable();
            DataRow r1 = dtNew.NewRow();

            dtNew.Columns.Add("Fields");
            int cnt = 0;
            for (int q = 0; q < dt.Rows.Count; q++)
            {
                if (dt.Rows.Count == 1)
                {
                    dtNew.Columns.Add(string.Format("Values"));
                }
                else
                {
                    dtNew.Columns.Add(string.Format("File {0} - Values", q+1));
                }
                cnt = q + 1;
            }

            List<string> ltFields = new List<string>();
            List<string[]> ltValues = new List<string[]>();
            List<string> ltValues1 = new List<string>();
            string idents = "USEDFIELDSLINE, USEDFIELDSLINE2, SERIALIZEDDATAOBJECTPART1, SERIALIZEDDATAOBJECTFALLBACK";

            for (int k = 0; k < dt.Columns.Count; k++)
            {
                DataRow r = dtNew.NewRow();
                if (!idents.Contains(dt.Columns[k].ToString()))
                {
                    r[0] = dt.Columns[k].ToString();
                }

                for (int q = 0; q < dt.Rows.Count; q++)
                {
                    if (!idents.Contains(dt.Columns[k].ToString()))
                    {
                        r[q + 1] = dt.Rows[q][k].ToString();
                    }
                    else if (dt.Columns[k].ToString() == "USEDFIELDSLINE" && !string.IsNullOrEmpty(dt.Columns[k].ToString()))
                    {
                        ltFields.AddRange((dt.Rows[q]["USEDFIELDSLINE"].ToString() + dt.Rows[q]["USEDFIELDSLINE2"].ToString()).Split('|'));
                    }
                    else if (dt.Columns[k].ToString() == "SERIALIZEDDATAOBJECTPART1" && !string.IsNullOrEmpty(dt.Columns[k].ToString()))
                    {
                        ltValues1.AddRange((dt.Rows[q]["SERIALIZEDDATAOBJECTPART1"].ToString() + dt.Rows[q]["SERIALIZEDDATAOBJECTFALLBACK"].ToString()).Split('|').Skip(3));
                        ltValues1.RemoveAt(ltValues1.Count - 1);
                    }
                }

                if (!idents.Contains(dt.Columns[k].ToString()))
                {
                    dtNew.Rows.Add(r);
                }
            }

            for (int i = 0; i < ltFields.Count; i++)
            {
                dtNew.Rows.Add(ltFields[i], ltValues1[i]);
            }

            return dtNew;
        }

        public bool IsValid()
        {
            return true;
        }

        void CloseResources()
        {
            dBConnction?.Close();
            dBConnction?.Dispose();
        }
    }

}
