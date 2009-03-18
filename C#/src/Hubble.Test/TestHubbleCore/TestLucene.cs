using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using SimpleSearch;
using SimpleSearch.News;

using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis;
using FTAlgorithm;

namespace TestHubbleCore
{
    class TestLucene
    {
        public string NewsXml = @"C:\ApolloWorkFolder\test\laboratory\Opensource\KTDictSeg\V1.4.01\Release\news.xml";

        private void TestFileIndexRebuild(List<XmlNode> documentList, String fileName)
        {
            try
            {
                Stopwatch watch = new Stopwatch();
                Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration = 0;

                DateTime old = DateTime.Now;
                int count = 0;

                long totalChars = 0;
                Index.Rebuild(Index.INDEX_DIR);
                Index.MaxMergeFactor = 100;
                Index.MinMergeDocs = 100;
                long docId = 0;
                foreach (XmlNode node in documentList)
                {
                    String content = node.Attributes["Content"].Value;

                    totalChars += content.Length;

                    watch.Start();

                    if (Global.Cfg.TestShortText)
                    {
                        for (int i = 0; i < content.Length / 100; i++)
                        {
                            Index.IndexString(content.Substring(i * 100, 100));
                            docId++;
                        }
                    }
                    else
                    {
                        Index.IndexString(content);
                        docId++;
                    }


                    watch.Stop();

                    count++;

                    if (count >= Global.Cfg.TestRows)
                    {
                        break;
                    }
                }

                watch.Start();
                Index.Close();

                watch.Stop();

                long indexElapsedMilliseconds = watch.ElapsedMilliseconds - Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration;

                Console.WriteLine(String.Format("Lucene.Net 插入{0}行数据,共{1}字符,用时{2}秒 分词用时{3}秒 索引时间{4}秒",
                    docId, totalChars, watch.ElapsedMilliseconds / 1000 + "." + watch.ElapsedMilliseconds % 1000,
                    Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration / 1000 + "." +
                    Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration % 1000,
                    indexElapsedMilliseconds / 1000 + "." + indexElapsedMilliseconds % 1000
                    ));


            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
            }

        }

        public void Test(List<XmlNode> documentList, String fileName, bool rebuild)
        {
            if (rebuild)
            {
                TestFileIndexRebuild(documentList, fileName);
            }

        }

        public void Test(List<XmlNode> documentList)
        {
            try
            {
                Stopwatch watch = new Stopwatch();
                Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration = 0;

                DateTime old = DateTime.Now;
                int count = 0;

                long totalChars = 0;
                Index.CreateIndex(null);
                Index.MaxMergeFactor = 100;
                Index.MinMergeDocs = 100;
                long docId = 0;
                foreach (XmlNode node in documentList)
                {
                    String content = node.Attributes["Content"].Value;

                    totalChars += content.Length;

                    watch.Start();

                    if (Global.Cfg.TestShortText)
                    {
                        for (int i = 0; i < content.Length / 100; i++)
                        {
                            Index.IndexString(content.Substring(i * 100, 100));
                            docId++;
                        }
                    }
                    else
                    {
                        Index.IndexString(content);
                        docId++;
                    }


                    watch.Stop();

                    count++;

                    if (count >= Global.Cfg.TestRows)
                    {
                        break;
                    }
                }

                watch.Start();
                Index.Close();

                watch.Stop();

                long indexElapsedMilliseconds = watch.ElapsedMilliseconds - Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration;

                Console.WriteLine(String.Format("Lucene.Net 插入{0}行数据,共{1}字符,用时{2}秒 分词用时{3}秒 索引时间{4}秒",
                    docId, totalChars, watch.ElapsedMilliseconds / 1000 + "." + watch.ElapsedMilliseconds % 1000,
                    Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration / 1000 + "." +
                    Lucene.Net.Analysis.KTDictSeg.KTDictSegAnalyzer.Duration % 1000,
                    indexElapsedMilliseconds / 1000 + "." + indexElapsedMilliseconds % 1000
                    ));

                //Begin test performance

                count = 0;
                int loopCount;
                if (Global.Cfg.PerformanceTest)
                {
                    loopCount = 1000;
                }
                else
                {
                    loopCount = 1;
                }

                string queryString = Global.Cfg.QueryString;
                Console.WriteLine("QueryString:" + queryString);

                List<Hubble.Core.Entity.WordInfo> queryWords = new List<Hubble.Core.Entity.WordInfo>();

                KTAnalyzer ktAnalyzer = new KTAnalyzer();

                foreach (Hubble.Core.Entity.WordInfo wordInfo in ktAnalyzer.Tokenize(queryString))
                {
                    queryWords.Add(wordInfo);
                }

                Console.WriteLine("取前100条记录");

                count = 0;
                watch.Reset();
                watch.Start();
                for (int i = 0; i < loopCount; i++)
                {
                    foreach (Hubble.Core.Query.DocumentRank docRank in Index.GetRankEnumerable(queryWords))
                    {
                        count++;

                        if (count >= 100)
                        {
                            break;
                        }
                    }
                }
                watch.Stop();
                Console.WriteLine("单次的查询时间:" + ((double)watch.ElapsedMilliseconds / loopCount).ToString() + "ms");



                StringBuilder report = new StringBuilder();
                count = 0;

                watch.Reset();
                watch.Start();

                for (int i = 0; i < loopCount; i++)
                {
                    foreach (Hubble.Core.Query.DocumentRank docRank in Index.GetRankEnumerable(queryWords, true))
                    {
                        if (!Global.Cfg.PerformanceTest)
                        {
                            string content = documentList[(int)docRank.DocumentId].Attributes["Content"].Value.Replace("\r\n", "");

                            int index = content.IndexOf(queryString);

                            if (index >= 0)
                            {
                                int fst = Math.Max(0, index - 20);
                                int len = Math.Min(content.Length - fst, 100);
                                content = content.Substring(fst, len);
                            }
                            
                            string title = documentList[(int)docRank.DocumentId].Attributes["Title"].Value;
                            KTDictSeg.HighLight.SimpleHTMLFormatter simpleHTMLFormatter =
                               new KTDictSeg.HighLight.SimpleHTMLFormatter("<font color=\"red\">", "</font>");

                            KTDictSeg.HighLight.Highlighter highlighter =
                                new KTDictSeg.HighLight.Highlighter(simpleHTMLFormatter,
                                new Lucene.Net.Analysis.KTDictSeg.KTDictSegTokenizer());

                            highlighter.FragmentSize = 100;

                            Console.WriteLine(docRank);
                            report.AppendLine("Title:" + title);
                            report.AppendLine("</br>");
                            report.AppendLine(highlighter.GetBestFragment(queryString, content));
                            report.AppendLine("</br>");
                            report.AppendLine("</br>");

                            if (count > 25)
                            {
                                using (FileStream fs = new FileStream("lucene.htm", FileMode.Create, FileAccess.ReadWrite))
                                {
                                    TextWriter w = new StreamWriter(fs, Encoding.UTF8);
                                    w.Write(report.ToString());
                                    w.Flush();
                                    w.Close();
                                }

                                break;
                            }

                        }

                        count++;
                    }
                }

                watch.Stop();


                Console.WriteLine("取所有记录");
                Console.WriteLine("单次的查询时间:" + ((double)watch.ElapsedMilliseconds / loopCount).ToString() + "ms");

                Console.WriteLine("查询出来的文章总数:" + (count / loopCount).ToString());

            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
            }
        }
    }
}
