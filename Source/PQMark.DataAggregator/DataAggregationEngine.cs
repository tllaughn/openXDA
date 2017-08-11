﻿//******************************************************************************************************
//  DataAggregationEngine.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  08/11/2017 - Billy Ernest
//       Created class.
//
//******************************************************************************************************

using GSF;
using GSF.Scheduling;
using GSF.Web.Model;
using openHistorian.XDALink;
using openXDA.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace PQMark.DataAggregator
{
    public class DataAggregationEngine : IDisposable
    {
        #region [ Members ]

        // Fields
        private DataContext m_dataContext;
        private bool m_disposed;
        private ScheduleManager m_scheduler;
        private bool m_running = false;

        #endregion

        #region [ Constructors ]

        #endregion

        #region [ Properties ]
        private DataContext DataContext => m_dataContext ?? (m_dataContext = new DataContext("systemSettings"));
        private ScheduleManager Scheduler => m_scheduler ?? (m_scheduler = new ScheduleManager());
        public bool Running => m_running;
        #endregion

        #region [ Static ]

        public static event EventHandler<EventArgs<string>> LogStatusMessageEvent;

        private static void OnLogStatusMessage(string message)
        {
            LogStatusMessageEvent?.Invoke(new object(), new EventArgs<string>(message));
        }

        public static event EventHandler<EventArgs<string>> LogExceptionMessageEvent;

        private static void OnLogExceptionMessage(string message)
        {
            LogExceptionMessageEvent?.Invoke(new object(), new EventArgs<string>(message));
        }

        #endregion

        #region [ Methods ]
        public void Dispose()
        {
            if (!m_disposed)
            {
                try
                {
                    Stop();
                    m_dataContext.Dispose();
                    m_disposed = true;
                }
                catch (Exception ex)
                {
                    OnLogStatusMessage(ex.ToString());
                }
            }
        }


        public void Start()
        {
            if (!Running)
            {
                Scheduler.Initialize();
                Scheduler.Starting += Scheduler_Starting;
                Scheduler.Started += Scheduler_Started;
                Scheduler.ScheduleDue += Scheduler_ScheduleDue;
                Scheduler.Disposed += Scheduler_Disposed;
                string schedule = DataContext.Table<Setting>().QueryRecordWhere("Name = 'PQMarkAggregationFrequency'")?.Value ?? "0 0 * * 0";
                Scheduler.AddSchedule("PQMarkAggregation", schedule);
                Scheduler.Start();
                m_running = true;
            }
        }

        public void Stop()
        {
            if (Running)
            {
                Scheduler.Stop();
                m_running = false;
            }
        }

        public void ReloadSystemSettings()
        {
        }

        private void Scheduler_Started(object sender, EventArgs e)
        {
            OnLogStatusMessage("PQMark Data Aggregator Scheduler has started successfully...");
        }

        private void Scheduler_Starting(object sender, EventArgs e)
        {
            OnLogStatusMessage("PQMark Data Aggregator Scheduler is starting...");
        }

        private void Scheduler_Disposed(object sender, EventArgs e)
        {
            OnLogStatusMessage("PQMark Data Aggregator Scheduler is disposed...");
        }


        private void Scheduler_ScheduleDue(object sender, EventArgs<Schedule> e)
        {
            OnLogStatusMessage(string.Format("PQMark month to date data aggregation is due..."));
            ProcessMonthToDateData();

        }

        private class DisturbanceData
        {
            public int MeterID { get; set; }
            public double PerUnitMagnitude { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public double DurationSeconds { get; set; }
            public double DurationCycles { get; set; }
        }

        public void ProcessAllData()
        {
            List<Tuple<double, double>> iticUpperCurve = new List<Tuple<double, double>>()
            {
                Tuple.Create(2.0,0.001),
                Tuple.Create(1.4,0.003),
                Tuple.Create(1.2,0.003),
                Tuple.Create(1.2,0.5),
                Tuple.Create(1.1,0.5),
            };
            List<Tuple<double, double>> iticLowerCurve = new List<Tuple<double, double>>()
            {
                Tuple.Create(0.0,0.02),
                Tuple.Create(0.7,0.02),
                Tuple.Create(0.7,0.5),
                Tuple.Create(0.8,0.5),
                Tuple.Create(0.8,10.0),
                Tuple.Create(0.9,10.0),
            };
            List<Tuple<double, double>> semiCurve = new List<Tuple<double, double>>()
            {
                Tuple.Create(0.0,0.02),
                Tuple.Create(0.5,0.02),
                Tuple.Create(0.5,0.2),
                Tuple.Create(0.7,0.2),
                Tuple.Create(0.7,0.5),
                Tuple.Create(0.8,0.5),
                Tuple.Create(0.8,10.0),
                Tuple.Create(0.9,10.0),
            };
            string historianServer = DataContext.Connection.ExecuteScalar<string>("SELECT Value FROM Setting WHERE Name = 'Historian.Server'") ?? "127.0.0.1";
            string historianInstance = DataContext.Connection.ExecuteScalar<string>("SELECT Value FROM Setting WHERE Name = 'Historian.Instance'") ?? "XDA";

            IEnumerable<PQMarkCompanyMeter> meters = DataContext.Table<PQMarkCompanyMeter>().QueryRecordsWhere("Enabled = 1");

            if (!meters.Any()) return;

            DataTable table = DataContext.Connection.RetrieveData(
                @"SELECT	Event.MeterID, Disturbance.PerUnitMagnitude, Disturbance.StartTime, Disturbance.EndTime, Disturbance.DurationSeconds, Disturbance.DurationCycles
                  FROM  	Disturbance JOIN
		                    Event ON Event.ID = Disturbance.EventID
                  WHERE	    Disturbance.PhaseID = (SELECT ID FROM Phase WHERE Name = 'Worst') AND
		                    Event.MeterID IN ("+ string.Join(",", meters.Select(x => x.ID)) +")"
            );
            IEnumerable<DisturbanceData> dd = table.Select().Select(row => DataContext.Table<DisturbanceData>().LoadRecord(row));

            var meterGroups = dd.GroupBy(x => x.MeterID);

            foreach(var meterGroup in meterGroups)
            {
                var yearGroups = meterGroup.GroupBy(x => x.StartTime.Year);
                foreach(var yearGroup in yearGroups)
                {
                    var dateGroups = yearGroup.GroupBy(x => x.StartTime.Month);
                    foreach(var dateGroup in dateGroups)
                    {
                        DateTime startDate = new DateTime(yearGroup.Key, dateGroup.Key, 1);
                        DateTime endDate = startDate.AddMonths(1).AddSeconds(-1);

                        // Get Easy Counts for SARFI 90, 70, 50, 10
                        int sarfi90 = dateGroup.Where(x => x.PerUnitMagnitude <= 0.9).Count();
                        int sarfi70 = dateGroup.Where(x => x.PerUnitMagnitude <= 0.7).Count();
                        int sarfi50 = dateGroup.Where(x => x.PerUnitMagnitude <= 0.5).Count();
                        int sarfi10 = dateGroup.Where(x => x.PerUnitMagnitude <= 0.1).Count();

                        // Get Counts for semi
                        int semi = dateGroup.Where(x => {
                            double vc = 0.0D;

                            int point1 = semiCurve.TakeWhile(y => y.Item2 < x.DurationSeconds).Count() - 1;
                            if (point1 == -1) return false;
                            else if (point1 + 1 == semiCurve.Count()) vc = semiCurve[point1].Item1;
                            else if (semiCurve[point1].Item2 == semiCurve[point1 + 1].Item2) vc = semiCurve[point1 + 1].Item1;
                            else
                            {
                                double slope = (semiCurve[point1 + 1].Item1 - semiCurve[point1].Item1) / (semiCurve[point1 + 1].Item2 - semiCurve[point1].Item2);
                                vc = slope * (x.DurationSeconds - semiCurve[point1].Item2) + semiCurve[point1].Item1;
                            }
                            double value = (1.0D - x.PerUnitMagnitude) / (1.0D - vc);
                            return 0.0D < value && value <= 1.0D;
                        }).Count();

                        // Get counts for ITIC
                        int iticLower = dateGroup.Where(x => {
                            double vc = 0.0D;

                            int point1 = iticLowerCurve.TakeWhile(y => y.Item2 < x.DurationSeconds).Count() - 1;
                            if (point1 == -1) return false;
                            else if (point1 + 1 == iticLowerCurve.Count()) vc = iticLowerCurve[point1].Item1;
                            else if (iticLowerCurve[point1].Item2 == iticLowerCurve[point1 + 1].Item2) vc = iticLowerCurve[point1 + 1].Item1;
                            else
                            {
                                double slope = (iticLowerCurve[point1 + 1].Item1 - iticLowerCurve[point1].Item1) / (iticLowerCurve[point1 + 1].Item2 - iticLowerCurve[point1].Item2);
                                vc = slope * (x.DurationSeconds - iticLowerCurve[point1].Item2) + iticLowerCurve[point1].Item1;
                            }
                            double value = (1.0D - x.PerUnitMagnitude) / (1.0D - vc);
                            return 0.0D < value && value <= 1.0D;
                        }).Count();

                        int iticUpper = dateGroup.Where(x => {
                            double vc = 0.0D;

                            int point1 = iticUpperCurve.TakeWhile(y => y.Item2 < x.DurationSeconds).Count() - 1;
                            if (point1 == -1) return false;
                            else if (point1 + 1 == iticUpperCurve.Count()) vc = iticUpperCurve[point1].Item1;
                            else if (iticUpperCurve[point1].Item2 == iticUpperCurve[point1 + 1].Item2) vc = iticUpperCurve[point1 + 1].Item2;
                            else
                            {
                                double slope = (iticUpperCurve[point1 + 1].Item1 - iticUpperCurve[point1].Item1) / (iticUpperCurve[point1 + 1].Item2 - iticUpperCurve[point1].Item2);
                                vc = slope * (x.DurationSeconds - iticUpperCurve[point1].Item2) + iticUpperCurve[point1].Item1;
                            }
                            double value = (1.0D - x.PerUnitMagnitude) / (1.0D - vc);
                            return 0.0D < value && value <= 1.0D;
                        }).Count();

                        List<int> channelIds = DataContext.Table<Channel>().QueryRecordsWhere("MeterID = {0} AND MeasurementCharacteristicID = (SELECT ID FROM MeasurementCharacteristic WHERE Name = 'TotalTHD')", meterGroup.Key).Select(x => x.ID).ToList();
                        List<openHistorian.XDALink.TrendingDataPoint> historianPoints = new List<openHistorian.XDALink.TrendingDataPoint>();
                        using (Historian historian = new Historian(historianServer, historianInstance))
                        {
                            foreach (openHistorian.XDALink.TrendingDataPoint point in historian.Read(channelIds, startDate, endDate))
                                if(point.SeriesID.ToString() == "Average")
                                    historianPoints.Add(point);
                        }

                        string thdjson = "[" + string.Join(";", historianPoints.GroupBy(x => (int)(x.Value / 0.1)).Select(x => $"{x.Key}:{x.Count()}")) + "]";
                    }

                }
            }
        }

        public void ProcessAllEmptyData()
        {

        }

        public void ProcessMonthToDateData()
        {
            DateTime endDate = DateTime.UtcNow;
            DateTime startDate = new DateTime(endDate.Year, endDate.Month, 1);
        }

        public string GetHelpMessage(string command)
        {
            string newString = "";
            if (command == "PQMarkProcessAllData")
                newString = "Creates aggregates for all data.";
            else if (command == "PQMarkProcessEmptyData")
                newString = "Creates aggregates for missing monthly data";
            else
                newString = "Creates aggregates for month to date data";

            StringBuilder helpMessage = new StringBuilder();

            helpMessage.Append(newString);
            helpMessage.AppendLine();
            helpMessage.AppendLine();
            helpMessage.Append("   Usage:");
            helpMessage.AppendLine();
            helpMessage.Append("       " + command);
            helpMessage.AppendLine();
            helpMessage.Append("       " + command +" -?");
            helpMessage.AppendLine();
            helpMessage.AppendLine();
            helpMessage.Append("   Options:");
            helpMessage.AppendLine();
            helpMessage.Append("       -?".PadRight(25));
            helpMessage.Append("Displays this help message");

            return helpMessage.ToString();
        }
    #endregion
}
}
