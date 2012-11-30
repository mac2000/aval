using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;
using System.IO;

class aval
{
    const string re = @"https?:\/\/(?<username>[^:]+):(?<password>[^@]+)@";
    static string aval_username = string.Empty;
    static string aval_password = string.Empty;
    static string gmail_username = string.Empty;
    static string gmail_password = string.Empty;
    static bool verbose = false;
    static bool sendmail = false;
    static string log_txt = "log.txt";
    static string log_html = "log.html";
    static string prev_txt = "aval.txt";
    static string prev = string.Empty;

    static void Main(string[] args)
    {
        #region Clear log files
        log("[*] Clearing log files");
        try
        {
            File.Delete(log_txt);
            File.Delete(log_html);
        }
        catch (Exception) { }
        #endregion

        #region Arguments
        #region Read arguments
        log("[*] Reading arguments");
        string aval = args.SingleOrDefault(arg => arg.Contains("vipiska.aval.ua"));
        log(string.Format("aval: {0}", aval));
        string gmail = args.SingleOrDefault(arg => arg.Contains("gmail.com"));
        log(string.Format("gmail: {0}", gmail));
        verbose = !string.IsNullOrEmpty(args.SingleOrDefault(arg => arg.ToLower().Trim() == "verbose"));
        log(string.Format("verbose: {0}", verbose.ToString()));
        sendmail = !string.IsNullOrEmpty(args.SingleOrDefault(arg => arg.ToLower().Trim() == "sendmail"));
        log(string.Format("sendmail: {0}", sendmail.ToString()));
        if (string.IsNullOrEmpty(aval) || string.IsNullOrEmpty(gmail)) badArguments("Wrong number of arguments");
        #endregion

        #region Parse arguments
        log("[*] Parsing arguments");
        Match match = Regex.Match(aval, re, RegexOptions.IgnoreCase);
        aval_username = match.Groups["username"].Value;
        log(string.Format("aval username: {0}", aval_username));
        aval_password = match.Groups["password"].Value;
        log(string.Format("aval password: {0}", aval_password));
        match = Regex.Match(gmail, re, RegexOptions.IgnoreCase);
        gmail_username = match.Groups["username"].Value;
        log(string.Format("gmail username: {0}", gmail_username));
        gmail_password = match.Groups["password"].Value;
        log(string.Format("gmail password: {0}", gmail_password));
        if (string.IsNullOrEmpty(aval_username) || string.IsNullOrEmpty(aval_password) || string.IsNullOrEmpty(aval_username) || string.IsNullOrEmpty(aval_password)) badArguments("Wrong arguments");
        #endregion
        #endregion

        #region Get prev value
        try
        {
            prev = File.ReadAllText(prev_txt).Trim();
            log(string.Format("prev: {0}", prev));
        }
        catch (Exception) { }
        #endregion

        log("[*] Navigating to https://vipiska.aval.ua");
        runBrowserThread(new Uri("https://vipiska.aval.ua"));
    }

    static void badArguments(string message)
    {
        log(message);
        if (!verbose) Console.WriteLine(message);
        Console.WriteLine("Usage example:");
        Console.WriteLine("aval.exe https://<username>:<password>@vipiska.aval.ua http://<username>:<password>@gmail.com [verbose] [sendmail]");
        Environment.Exit(1);
    }

    static void runBrowserThread(Uri url)
    {
        var th = new Thread(() =>
        {
            var br = new WebBrowser();
            br.DocumentCompleted += browser_DocumentCompleted;
            br.Navigate(url);
            Application.Run();
        });
        th.SetApartmentState(ApartmentState.STA);
        th.Start();
    }

    static void log(string message)
    {
        if (verbose) Console.WriteLine(message);
        File.AppendAllText(log_txt, string.Format("{0}{1}", message, Environment.NewLine), Encoding.UTF8);
    }

    static void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
    {
        try
        {
            var browser = sender as WebBrowser;
            if (browser.Url == e.Url && browser.DocumentTitle.ToLower().Contains("виписка он-лайн"))
            {
                File.WriteAllText(log_html, browser.Document.Body.InnerHtml, Encoding.UTF8);
                HtmlElementCollection u_n = browser.Document.GetElementsByTagName("INPUT").GetElementsByName("u_n");
                HtmlElementCollection u_p = browser.Document.GetElementsByTagName("INPUT").GetElementsByName("u_p");
                HtmlElementCollection ENTER = browser.Document.GetElementsByTagName("INPUT").GetElementsByName("ENTER");

                if (browser.Document.Body.InnerText.Contains("Недiйсний логiн або пароль")) badArguments("Wrong aval login or password");
                if (browser.Document.Body.InnerText.Contains("Вибачте, програмна помилка")) badArguments("Page is broken");

                if (u_n.Count == 1 && u_p.Count == 1 && ENTER.Count == 1 && ENTER[0].GetAttribute("value") == "Ok")
                {
                    log("[*] Signin in");
                    u_n[0].SetAttribute("value", aval_username);
                    u_p[0].SetAttribute("value", aval_password);
                    ENTER[0].InvokeMember("click");
                    return;
                }

                foreach (HtmlElement item in ENTER)
                {
                    if (item.GetAttribute("value") == "Доступна сума")
                    {
                        log("[*] Navigating to report");
                        item.InvokeMember("click");
                        return;
                    }
                    if (item.GetAttribute("value") == "Повернутися до меню")
                    {
                        log("[*] Parsing report report");
                        string available = browser.Document.GetElementsByTagName("TH")[1].InnerText.Split('.')[0].Trim();
                        log(string.Format("available: {0}", available));
                        string report = browser.Document.GetElementsByTagName("TABLE")[3].GetElementsByTagName("TABLE")[2].GetElementsByTagName("FONT")[5].InnerHtml;
                        report = string.Join("<br>", report.Split(new string[] { "<BR>" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().TrimStart('(')).OrderBy(x => x).ToArray());
                        log(string.Format("report: {0}", report));

                        /*string[] lines = report.Split(new string[] { "<BR>" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().TrimStart('(')).ToArray();
                        foreach (string line in lines)
                        {
                            Match m = Regex.Match(line, @"(?<date>\d+\.\d+\.\d+ \d+:\d+:\d+) (?<amount>\d+\.\d+) (?<currency>\w+) (?<company>.*)");
                            DateTime d = DateTime.ParseExact(m.Groups["date"].Value, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            Decimal amount = Decimal.Parse(m.Groups["amount"].Value, NumberStyles.Currency, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            string currency = m.Groups["currency"].Value;
                            string company = m.Groups["company"].Value;
                        }*/

                        File.WriteAllText(prev_txt, available);

                        #region SendMail
                        if (prev != available || sendmail == true)
                        {
                            log("[*] Sending mail");
                            string subject = string.Format("[aval{1}] {0}", available, sendmail == true ? ":daily" : string.Empty);
                            string body = string.Format("<h3>{0}</h3>{1}", available, report);
                            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                            smtp.EnableSsl = true;
                            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtp.UseDefaultCredentials = false;
                            smtp.Credentials = new NetworkCredential(gmail_username, gmail_password);
                            using (MailMessage message = new MailMessage(string.Format("{0}@gmail.com", gmail_username), string.Format("{0}@gmail.com", gmail_username), subject, body))
                            {
                                message.IsBodyHtml = true;
                                smtp.Send(message);
                            }
                        }
                        else
                        {
                            log("[!] Nothing changed, mail wont be send");
                        }
                        #endregion

                        log("[+] Done");
                        Application.ExitThread();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log(string.Format("Error: {0}", ex.Message));
            Environment.Exit(1);
        }
    }
}
