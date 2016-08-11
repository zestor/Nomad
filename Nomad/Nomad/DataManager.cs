using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace Nomad
{
    public class DataManager
    {
        private const string JOBSTATUS_NEW = "NEW";
        private const string JOBSTATUS_SAME= "SAME";
        private const string JOBSTATUS_OLD = "OLD";

        public string File1FileName = string.Empty;
        public string File2FileName = string.Empty;

        private JSONJobs File1Jobs;
        private JSONJobs File2Jobs;

        internal bool HideNotInterested = true;
        private Dictionary<string, string> NotInterestedDict;
        internal string NOT_INTERESTED_FILE_NAME = @".\notinterested.txt";

        public DataManager()
        {
            if (File.Exists(NOT_INTERESTED_FILE_NAME))
            {
                NotInterestedDict = File.ReadAllText(NOT_INTERESTED_FILE_NAME)
                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .ToDictionary(split => split, split => split);
            }
        }

        public string GetFileLength1
        {
            get
            {
                return File1Jobs != null ? File1Jobs.jobs.Length.ToString() : string.Empty;
            }
        }

        public string GetFileLength2
        {
            get
            {
                return File2Jobs != null ? File2Jobs.jobs.Length.ToString() : string.Empty;
            }
        }

        public void NotInterested(DataGridView dataGridView1)
        {
            string slug = (string)dataGridView1.CurrentRow.Cells[GRID_POS_SLUG].Value;

            if (slug != null)
            {
                if (NotInterestedDict == null)
                {
                    NotInterestedDict = new Dictionary<string, string>();
                }

                if (!NotInterestedDict.ContainsKey(slug))
                {
                    NotInterestedDict.Add(slug, slug);
                }
                else
                {
                    NotInterestedDict.Remove(slug);
                }

                string outdata = string.Empty;
                foreach (string key in NotInterestedDict.Keys)
                {
                    if (outdata == string.Empty)
                    {
                        outdata += key;
                    }
                    else
                    {
                        outdata += ";" + key;
                    }
                }
                File.WriteAllText(NOT_INTERESTED_FILE_NAME, outdata);
            }

            HideNotInterested = true;
            ToggleNotInterestedRows(dataGridView1);
        }

        public void startupLoadFiles(DataGridView dataGridView1)
        {
            // get all JSON job files
            string[] files = Directory.GetFiles(@".\", "*.json");

            if (files.Length >= 2)
            {
                File1FileName = files[files.Length - 2];
                string data = File.ReadAllText(File1FileName);
                File1Jobs = JsonConvert.DeserializeObject<JSONJobs>(data);

                File2FileName = files[files.Length - 1];
                string data2 = File.ReadAllText(File2FileName);
                File2Jobs = JsonConvert.DeserializeObject<JSONJobs>(data2);
            }
            else if (files.Length == 1)
            {
                File2FileName = files[files.Length - 1];
                string data2 = File.ReadAllText(File2FileName);
                File2Jobs = JsonConvert.DeserializeObject<JSONJobs>(data2);

                File1Jobs = File2Jobs;
                File1FileName = File2FileName;
            }
            else
            {
                //test change
                return;
            }

            CompareFiles(dataGridView1);
            HideNotInterested = true;
            ToggleNotInterestedRows(dataGridView1);

        }

        public void CreateNewFile(DataGridView dataGridView1)
        {
            DateTime dt = DateTime.Now;

            // delete all json files for same day
            string[] deletefiles = Directory.GetFiles(@".\", string.Format("{0:MMddyy}*.json", dt));
            foreach (string deletefile in deletefiles)
            {
                File.Delete(deletefile);
            }

            // download 1,000 jobs from avanade careers right now
            string localFilename = @".\" + string.Format("{0:MMddyy HHmmss}.json", dt);
            
            List<JSONJob> totalJobs = null;

            int joboffset = 0;
            while (true)
            {
                string url = @"https://careers.avanade.com/api/jobs?brand=experienced,both&limit=100&offset={0}&page=1";
                url = string.Format(url, joboffset);

                WebClient client = new WebClient();
                client.DownloadFile(url, localFilename);
                File2FileName = localFilename;

                // load the downloaded jobs into File2Jobs
                string data = File.ReadAllText(File2FileName);
                File2Jobs = JsonConvert.DeserializeObject<JSONJobs>(data);

                if (File2Jobs.jobs.Count<JSONJob>() == 0)
                {
                    break;
                }

                if (totalJobs == null)
                {
                    totalJobs = File2Jobs.jobs.ToList<JSONJob>();
                }
                else
                {
                    foreach (var job in File2Jobs.jobs)
                    {
                        totalJobs.Add(job);
                    }
                }

                joboffset += 100;
            }

            File2Jobs = new JSONJobs();
            File2Jobs.jobs = totalJobs.ToArray<JSONJob>();
            File.WriteAllText(localFilename,JsonConvert.SerializeObject(File2Jobs));
                        
            // get all JSON job files
            string[] files = Directory.GetFiles(@".\", "*.json");

            if (files.Length >= 2)
            {
                File1FileName = files[files.Length-2];
                string data2 = File.ReadAllText(File1FileName);
                File1Jobs = JsonConvert.DeserializeObject<JSONJobs>(data2);
            }
            else
            {
                File1Jobs = File2Jobs;
                File1FileName = File2FileName;
            }
            
            CompareFiles(dataGridView1);
        }

        public void LoadGrid1(string filename, DataGridView dataGridView1)
        {
            string data = File.ReadAllText(filename);
            JsonSerializer ser = new JsonSerializer();
            File1Jobs = JsonConvert.DeserializeObject<JSONJobs>(data);

            configureGridColumns(dataGridView1);

            foreach (JSONJob j in File1Jobs.jobs)
            {
                string[] row = new string[] { j.RECORD_STATUS, j.slug, j.title, j.city, j.state, j.country, j.location, j.description };
                dataGridView1.Rows.Add(row);
            }

            if(File1Jobs != null && File2Jobs != null)
            {
                CompareFiles(dataGridView1);
            }
        }

        public void LoadGrid2(string filename, DataGridView dataGridView1)
        {
            string data = File.ReadAllText(filename);
            JsonSerializer ser = new JsonSerializer();
            File2Jobs = JsonConvert.DeserializeObject<JSONJobs>(data);

            configureGridColumns(dataGridView1);

            foreach (JSONJob j in File1Jobs.jobs)
            {
                string[] row = new string[] { j.RECORD_STATUS, j.slug, j.title, j.city, j.state, j.country, j.location, j.description };
                dataGridView1.Rows.Add(row);
            }

            if (File1Jobs != null && File2Jobs != null)
            {
                CompareFiles(dataGridView1);
            }
        }

        internal int GRID_POS_TYPE = 0;
        internal int GRID_POS_SLUG = 1;
        internal int GRID_POS_TITLE = 2;
        internal int GRID_POS_CITY = 3;
        internal int GRID_POS_STATE = 4;
        internal int GRID_POS_COUNTRY = 5;
        internal int GRID_POS_LOCATION = 6;
        internal int GRID_POS_DESCRIPTION = 7;

        private void configureGridColumns(DataGridView dataGridView1)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            Application.DoEvents();

            dataGridView1.ColumnCount = 8;
            dataGridView1.Columns[GRID_POS_TYPE].Name = "Status";
            dataGridView1.Columns[GRID_POS_TYPE].Width = 40;
            dataGridView1.Columns[GRID_POS_SLUG].Name = "Slug";
            dataGridView1.Columns[GRID_POS_SLUG].Width = 100;
            dataGridView1.Columns[GRID_POS_TITLE].Name = "Title";
            dataGridView1.Columns[GRID_POS_TITLE].Width = 375;
            dataGridView1.Columns[GRID_POS_CITY].Name = "City";
            dataGridView1.Columns[GRID_POS_CITY].Width = 150;
            dataGridView1.Columns[GRID_POS_STATE].Name = "State";
            dataGridView1.Columns[GRID_POS_STATE].Width = 150;
            dataGridView1.Columns[GRID_POS_COUNTRY].Name = "Country";
            dataGridView1.Columns[GRID_POS_COUNTRY].Width = 150;
            dataGridView1.Columns[GRID_POS_COUNTRY].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns[GRID_POS_LOCATION].Name = "Location";
            dataGridView1.Columns[GRID_POS_LOCATION].Width = 150;
            dataGridView1.Columns[GRID_POS_DESCRIPTION].Name = "Description";
            dataGridView1.Columns[GRID_POS_DESCRIPTION].Width = 150;
            dataGridView1.Columns[GRID_POS_DESCRIPTION].Visible = false;
        }


        public void CompareFiles(DataGridView dataGridView1)
        {
            if (File1Jobs != null && File2Jobs != null)
            {
                configureGridColumns(dataGridView1);

                Compare(File1Jobs, File2Jobs, JOBSTATUS_OLD);
                Compare(File2Jobs, File1Jobs, JOBSTATUS_NEW);

                foreach (JSONJob j in File2Jobs.jobs)
                {
                    if (j.RECORD_STATUS == JOBSTATUS_NEW)
                    {
                        string[] row = new string[] { j.RECORD_STATUS, j.slug, j.title, j.city, j.state, j.country, j.location, j.description };
                        dataGridView1.Rows.Add(row);
                    }
                }
                foreach (JSONJob j in File1Jobs.jobs)
                {
                    if (j.RECORD_STATUS == JOBSTATUS_SAME)
                    {
                        string[] row = new string[] { j.RECORD_STATUS, j.slug, j.title, j.city, j.state, j.country, j.location, j.description };
                        dataGridView1.Rows.Add(row);
                    }
                }
                foreach (JSONJob j in File1Jobs.jobs)
                {
                    if (j.RECORD_STATUS == JOBSTATUS_OLD)
                    {
                        string[] row = new string[] { j.RECORD_STATUS, j.slug, j.title, j.city, j.state, j.country, j.location, j.description };
                        dataGridView1.Rows.Add(row);
                    }
                }
                foreach (DataGridViewRow r in dataGridView1.Rows)
                {
                    string status = (string)r.Cells[GRID_POS_TYPE].Value;

                    if (status == JOBSTATUS_OLD)
                    {
                        r.DefaultCellStyle.BackColor = Color.LightSalmon;
                    }
                    if (status == JOBSTATUS_NEW)
                    {
                        r.DefaultCellStyle.BackColor = Color.LightGreen;
                    }
                    if (status == JOBSTATUS_SAME)
                    {
                        r.DefaultCellStyle.BackColor = Color.LightYellow;
                    }
                }
                ColorNotInterestedRows(dataGridView1);
                HideNotInterested = true;
                ToggleNotInterestedRows(dataGridView1);
            }
        }

        private void ColorNotInterestedRows(DataGridView dataGridView1)
        {
            NotInterestedAllNonUsJobs(dataGridView1);

            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                if (NotInterestedDict != null)
                {
                    foreach (string key in NotInterestedDict.Keys)
                    {
                        if ((string)r.Cells[GRID_POS_SLUG].Value == key)
                        {
                            r.DefaultCellStyle.BackColor = Color.DimGray;
                        }
                    }
                }
            }
        }

        private void Compare(JSONJobs a, JSONJobs b, string statustext)
        {
            if (a != null && b != null)
            {
                foreach (JSONJob j1 in a.jobs)
                {
                    string slug = j1.slug;

                    bool foundit = false;
                    foreach (JSONJob j2 in b.jobs)
                    {
                        if (j2.slug.Trim() == j1.slug.Trim())
                        {
                            foundit = true;
                            break;
                        }
                    }

                    if (!foundit)
                    {
                        j1.RECORD_STATUS = statustext;
                    }
                    else
                    {
                        j1.RECORD_STATUS = JOBSTATUS_SAME;
                    }
                }
            }
        }

        internal void ToggleNotInterestedRows(DataGridView dataGridView1)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                string status = (string)r.Cells[GRID_POS_TYPE].Value;

                if (status == JOBSTATUS_OLD)
                {
                    r.DefaultCellStyle.BackColor = Color.LightSalmon;
                }
                if (status == JOBSTATUS_NEW)
                {
                    r.DefaultCellStyle.BackColor = Color.LightGreen;
                }
                if (status == JOBSTATUS_SAME)
                {
                    r.DefaultCellStyle.BackColor = Color.LightYellow;
                }

                if (NotInterestedDict != null)
                {
                    foreach (string key in NotInterestedDict.Keys)
                    {
                        if ((string)r.Cells[GRID_POS_SLUG].Value == key)
                        {
                            //r.DefaultCellStyle.BackColor = Color.DimGray;

                            if (HideNotInterested)
                            {
                                if (status != JOBSTATUS_OLD) // if it's not a removed job
                                {
                                    r.Visible = false;
                                }
                            }
                            else
                            {
                                r.Visible = true;
                            }
                        }
                    }
                }
            }
            HideNotInterested = !HideNotInterested;
        }

        private void NotInterestedAllNonUsJobs(DataGridView dataGridView1)
        {

            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                string slug = (string)r.Cells[GRID_POS_SLUG].Value;
                string country = (string)r.Cells[GRID_POS_COUNTRY].Value;

                if (country != "United States" && slug != null)
                {
                    if (NotInterestedDict == null)
                    {
                        NotInterestedDict = new Dictionary<string, string>();
                    }

                    if (!NotInterestedDict.ContainsKey(slug))
                    {
                        NotInterestedDict.Add(slug, slug);
                    }
                }
            }

            string outdata = string.Empty;
            foreach (string key in NotInterestedDict.Keys)
            {
                if (outdata == string.Empty)
                {
                    outdata += key;
                }
                else
                {
                    outdata += ";" + key;
                }
            }

            File.WriteAllText(NOT_INTERESTED_FILE_NAME, outdata);
        }

        public void InterestedAllUsOpenLocationJobs(DataGridView dataGridView1)
        {
            bool checkForDict = true;

            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                string slug = (string)r.Cells[GRID_POS_SLUG].Value;
                string country = (string)r.Cells[GRID_POS_COUNTRY].Value;
                string state = (string)r.Cells[GRID_POS_STATE].Value;
                string city = (string)r.Cells[GRID_POS_CITY].Value;

                if (
                    (country == "United States" && slug != null && state == null) ||
                    (country == "United States" && slug != null && state == "North Carolina" && city == "Charlotte")
                    )
                {
                    if (checkForDict && NotInterestedDict == null)
                    {
                        checkForDict = false;
                        NotInterestedDict = new Dictionary<string, string>();
                    }

                    if (NotInterestedDict.ContainsKey(slug))
                    {
                        NotInterestedDict.Remove(slug);
                    }
                }
            }

            string outdata = string.Empty;
            foreach (string key in NotInterestedDict.Keys)
            {
                if (outdata == string.Empty)
                {
                    outdata += key;
                }
                else
                {
                    outdata += ";" + key;
                }
            }

            File.WriteAllText(NOT_INTERESTED_FILE_NAME, outdata);
        }

    }
}
