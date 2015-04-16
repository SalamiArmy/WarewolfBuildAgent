
/*
*  Warewolf - The Easy Service Bus
*  Copyright 2014 by Warewolf Ltd <alpha@warewolf.io>
*  Licensed under GNU Affero General Public License 3.0 or later. 
*  Some rights reserved.
*  Visit our website for more information <http://warewolf.io/>
*  AUTHORS <http://warewolf.io/authors.php> , CONTRIBUTORS <http://warewolf.io/contributors.php>
*  @license GNU Affero General Public License <http://www.gnu.org/licenses/agpl-3.0.html>
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;

namespace Dev2.ScheduleExecutor
{
    public class Program
    {
        private static readonly string OutputPath = string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "warewolf");
        private static readonly string SchedulerLogDirectory = OutputPath +  "SchedulerLogs";

        private static void Main(string[] args)
        {
            try
            {
                SetupForLogging();

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                Log("Info", "Task Started");
                var paramters = new Dictionary<string, string>();
                for(int i = 0; i < args.Count(); i++)
                {
                    string[] singleParameters = args[i].Split(':');

                    paramters.Add(singleParameters[0],
                                  singleParameters.Skip(1).Aggregate((a, b) => String.Format("{0}:{1}", a, b)));
                }
                Log("Info", string.Format("Start execution of {0}", paramters["Workflow"]));
                Console.WriteLine(PostDataToWebserverAsRemoteAgent(paramters["Workflow"], paramters.FirstOrDefault(param => param.Key == "Hostname").Value, paramters.FirstOrDefault(param => param.Key == "Port").Value));
            }
            catch(Exception e)
            {
                Log("Error", string.Format("Error from execution: {0}{1}", e.Message, e.StackTrace));
                Environment.Exit(1);
            }
        }

        public static string PostDataToWebserverAsRemoteAgent(string workflowName, string hostname, string port)
        {
            workflowName = FormatForWebCall(workflowName);
            string postUrl = string.Format("http://" + (hostname ?? "localhost") + ":" + (port ?? "3142") + "/services/{0}", workflowName);
            Log("Info", string.Format("Executing as {0}", CredentialCache.DefaultNetworkCredentials.UserName));
            string result = string.Empty;

            WebRequest req = WebRequest.Create(postUrl);
            req.Credentials = CredentialCache.DefaultNetworkCredentials;
            req.Method = "GET";
            req.Headers.Add(HttpRequestHeader.Cookie, "RemoteWarewolfServer");
            req.Timeout = Timeout.Infinite;

            try
            {
                using(var response = req.GetResponse() as HttpWebResponse)
                {
                    if(response != null)
                    {
                        // ReSharper disable AssignNullToNotNullAttribute
                        using(var reader = new StreamReader(response.GetResponseStream()))
                        // ReSharper restore AssignNullToNotNullAttribute
                        {
                            result = reader.ReadToEnd();
                            var logIndex=workflowName.IndexOf("LogFile=", StringComparison.Ordinal);
                            if (logIndex>0)
                            {
                                var logFile = workflowName.Substring(logIndex + "LogFile=".Length, workflowName.IndexOf(".log", StringComparison.Ordinal) - logIndex - "LogFile=".Length + ".log".Length);
                                File.AppendAllLines(logFile, new[]{string.Empty, result});
                            }
                        }

                        if(response.StatusCode != HttpStatusCode.OK)
                        {
                            Log("Error", string.Format("Error from execution: {0}", result));
                        }
                        else
                        {
                            Log("Info", string.Format("Completed execution. Output: {0}", result));
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.Write(e.Message);
                Console.WriteLine(e.StackTrace);
                Log("Error",
                    string.Format(
                        "Error executing request. Exception: {0}" + Environment.NewLine + "StackTrace: {1}",
                        e.Message, e.StackTrace));
                Environment.Exit(1);
            }
            return result;
        }

        static string FormatForWebCall(string workflowName)
        {
            return workflowName.Replace(" ", "%20").Replace('\\', '/');
        }

        private static void Log(string logType, string logMessage)
        {
            try
            {
                using(
                    TextWriter tsw =
                        new StreamWriter(new FileStream(SchedulerLogDirectory + "/" + DateTime.Now.ToString("yyyy-MM-dd"),
                                                        FileMode.Append)))
                {
                    tsw.WriteLine();
                    tsw.Write(logType);
                    tsw.Write("----");
                    tsw.WriteLine(logMessage);
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {


            }
        }

        private static void SetupForLogging()
        {
            bool hasSchedulerLogDirectory = Directory.Exists(SchedulerLogDirectory);
            if(hasSchedulerLogDirectory)
            {
                var directoryInfo = new DirectoryInfo(SchedulerLogDirectory);
                FileInfo[] logFiles = directoryInfo.GetFiles();
                if(logFiles.Count() > 20)
                {
                    try
                    {
                        FileInfo fileInfo = logFiles.OrderByDescending(f => f.LastWriteTime).First();
                        fileInfo.Delete();
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch
                    // ReSharper restore EmptyGeneralCatchClause
                    {
                    }

                }
            }
            else
            {
                Directory.CreateDirectory(SchedulerLogDirectory);
            }
        }
    }
}
