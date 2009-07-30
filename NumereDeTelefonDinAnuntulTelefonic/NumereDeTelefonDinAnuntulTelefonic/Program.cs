using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Cfg;
using RZ.Web;

namespace NumereDeTelefonDinAnuntulTelefonic
{
    public class Anunt
    {
        public int Id { get; set; }
        public string Continut { get; set; }
        public DateTime DataIndroducerii { get; set; }
        public int Pret { get; set; }
    }

    public class Numar
    {
        public int Id { get; set; }
        public Anunt Anunt { get; set; }
        public string Nr { get; set; }
    }

    static class Program
    {
        static IEnumerable<HtmlContentBlock> GetByTagName(this HtmlContentBlock target, string tag)
        {
            foreach (var item in target.GetByTagName(tag, t => true))
            {
                yield return item;
            }
        }
        static IEnumerable<HtmlContentBlock> GetByTagName(this HtmlContentBlock target, string tag, Predicate<HtmlContentBlock> where)
        {
            foreach (var item in target)
            {
                if (item.IsTagName(tag) && where(item))
                    yield return item;
            }
        }

        static void Main(string[] args)
        {
            ISessionFactory factory = new Configuration()
                .AddAssembly(typeof(Anunt).Assembly)
                .BuildSessionFactory();
            ISession session = factory.OpenSession();
            for (int i = 0; i < 23; i++)
            {
                WebRequest r = WebRequest.Create("http://www.anuntul.ro/index.php?menu=anunturi&RubricaID=1&SubrubricaID=5&page=" + i.ToString());
                WebResponse rs = r.GetResponse();
                StreamReader sr = new StreamReader(rs.GetResponseStream());
                string rstr = sr.ReadToEnd();
                HtmlParser parser = new HtmlParser(rstr);
                if (parser.MoveToNextTag())
                {
                    Boolean hasMatch;
                    HtmlContentBlock tagBlock = parser.GrabCurrentTag(out hasMatch);
                    foreach (var item in tagBlock.GetByTagName("tbody", t => t.Attributes["id"] == "anunturi_body"))
                        foreach (var item1 in item.GetByTagName("tr"))
                        {
                            Anunt a = null;
                            foreach (var item2 in item1.GetByTagName("td"))
                                if (a == null)
                                    foreach (var item3 in item2.GetByTagName("span"))
                                    {
                                        foreach (var item4 in item3.GetByTagName("a"))
                                        {
                                            if (item4.Count > 2)
                                            {
                                                var c = item4[1].ToString() + item4[2].ToString();
                                                a = new Anunt()
                                                {
                                                    Continut = c.Length > 499 ? c.Substring(0, 499) : c,
                                                    DataIndroducerii = DateTime.Now
                                                };
                                            }
                                            if (a == null)
                                                continue;
                                            session.Save(a);
                                            string raw = item4.ToString();
                                            var m = Regex.Match(raw, @"[0-9]*\.[0-9]*\.[0-9]*\.[0-9]*");
                                            while (m.Success)
                                            {
                                                var realNr = m.Value.Replace(".", "");
                                                if (realNr.Length == 10)
                                                    session.Save(new Numar() { Anunt = a, Nr = realNr });
                                                m = m.NextMatch();
                                            }
                                            m = Regex.Match(raw, @"[0-9]*\.[0-9]*\.[0-9]*");
                                            while (m.Success)
                                            {
                                                var realNr = m.Value.Replace(".", "");
                                                if (realNr.Length == 10)
                                                    session.Save(new Numar() { Anunt = a, Nr = realNr });
                                                m = m.NextMatch();
                                            }
                                            m = Regex.Match(raw, @"[0-9]*");
                                            while (m.Success)
                                            {
                                                var realNr = m.Value;
                                                if (realNr.Length == 10)
                                                    session.Save(new Numar() { Anunt = a, Nr = realNr });
                                                m = m.NextMatch();
                                            }
                                            break;
                                        }
                                        break;
                                    }
                                else
                                {
                                    foreach (var itemx in item2.GetByTagName("span", t => t.Attributes["class"] == "pret"))
                                    {
                                        var p = 0;
                                        var rm = Regex.Match(itemx[0].ToString(), @"[0-9]*\.[0-9]*");
                                        if (rm.Success)
                                            while (rm.Success)
                                            {
                                                if (int.TryParse(rm.Value.Replace(".", ""), out p))
                                                    break;
                                                rm = rm.NextMatch();
                                            }
                                        else
                                        {
                                            rm = Regex.Match(itemx[0].ToString(), @"[0-9]*");
                                            while (rm.Success)
                                            {
                                                if (int.TryParse(rm.Value, out p))
                                                    break;
                                                rm = rm.NextMatch();
                                            }
                                        }
                                        a.Pret = p;
                                        session.SaveOrUpdate(a);
                                        break;
                                    }
                                }
                        }
                }
            }
            session.Flush();
            Console.WriteLine("GAAAAAAATTTTTTTAAAAAAAAAA");
        }
    }
}
