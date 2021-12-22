using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Nito.AsyncEx;

namespace WebCrawler
{
    class Program
    {
        //https://based.cooking/
        //https://orf.at
        //https://de.wikipedia.org/wiki/Wikipedia:Hauptseite

        static readonly short tiefe = -1; //alles über 3 dauer seeeeeeeeehr lange
        static readonly string url = null;
        static int linkNR = 0;
        static List<Exception> exList = new List<Exception>();

        static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            var init = new Program();

            stopwatch.Start();

            AsyncContext.Run(() => init.Crawlerasync(0, url)); //wait

            stopwatch.Stop();

            finalOutput(stopwatch);
        }

        //bekommt tiefe und start URL durch user input
        static Program()
        {
            while (tiefe < 1 || tiefe > 32767)
            {
                Console.Write("Tiefe (alles über 3 ist abzuraten): ");
                tiefe = Convert.ToInt16(Console.ReadLine());

                if(tiefe < 1 || tiefe > 32767)
                {
                    Console.WriteLine("Ungueltiger Zahlenbereich, Zahl muss zwischen 0 und 32768 liegen");
                }
            }

            while (url == null || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                Console.Write("Url: ");
                url = Console.ReadLine();

                if(!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    Console.WriteLine("Ungueltige URL, es muss ein Absoluter Pfad sein, z.B. https://orf.at");
                }
            }
        }

        //liest alle Links aus und wenn die gegebene tiefe noch nicht erreicht wurde ruft es für jeden Link eine Task mit dieser Funktion auf
        //gibt zusätzlich jeden Link aus bevor er aufgerufen wird
        private async Task Crawlerasync(short i, string uri)
        {
            var taskList = new List<Task>();
            i++;

            if (i <= tiefe)
            {
                try
                {
                    var httpClient = new HttpClient(); //HTTP request und response
                    var html = await httpClient.GetStringAsync(uri); //GET request
                    var htmlDocument = new HtmlDocument(); //top-level programmatischer zugriff auf HTML dokument
                    htmlDocument.LoadHtml(html);
                    var links = htmlDocument.DocumentNode.Descendants("a") //holt alle links welche uri.absolute konform sind 
                        .Select(n => n.ChildAttributes("href"))
                        .Where(l => l.FirstOrDefault() != null && Uri.IsWellFormedUriString(l.FirstOrDefault().Value, UriKind.Absolute));

                    foreach (var l in links)
                    {
                        if (l.FirstOrDefault() != null)
                        {
                            lock (this)
                            {
                                linkNR++;
                                Console.WriteLine($"{linkNR}: {l.FirstOrDefault().Value} in der {i}. Ebene");
                            }

                            taskList.Add(Task.Run(() => Crawlerasync(i, l.FirstOrDefault().Value)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (this)
                    {
                        exList.Add(ex);
                        linkNR++;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{linkNR}: hatte einen Fehler bei {uri} in der {i}. Ebene");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                Task.WaitAll(taskList.ToArray()); //await
            }
        }
        
        //übernimmt die finale Ausgabe, gibt aus wie viele Threads ohne Fehler beendet wurden, wie lange es gedauert hat, welche Fehler auftratten und wie viele Fehler es gab
        private static void finalOutput(Stopwatch stopwatch)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nFertig - Zeit aufgewendet: {stopwatch.Elapsed} und {linkNR - exList.Count()} Tasks wurden ohne Fehler beendet");

            if (exList != null && exList.Count > 0)
            {
                var exListSorted = exList.GroupBy(ex => ex.InnerException).FirstOrDefault().GroupBy(ex => ex.Message);

                Console.WriteLine($"Es kam zu {exList.Count()} Fehler und folgende Fehler traten auf:");
                foreach (var innerEx in exListSorted)
                    Console.WriteLine("  " + innerEx.Key);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
