using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nuve.Test.Tokenizers;
using Nuve.Morphologic.Structure;
using Nuve.Lang;
using net.zemberek.erisim;
using net.zemberek.tr.yapi;

namespace SentiAnali
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> content = new List<string>();

            // Sozlukteki kelimeler ve degerleri okundu.
            string[] lexicon = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "sentiment.txt", Encoding.GetEncoding("iso-8859-9"));
            List<string> lexemes = new List<string>();
            List<double> negVals = new List<double>();
            List<double> objVals = new List<double>();
            List<double> posVals = new List<double>();
            foreach (string row in lexicon)
            {
                lexemes.Add(row.Split('\t')[0]);
                negVals.Add(Convert.ToDouble(row.Split('\t')[1]));
                objVals.Add(Convert.ToDouble(row.Split('\t')[2]));
                posVals.Add(Convert.ToDouble(row.Split('\t')[3]));
            }

            Zemberek zemberek = new Zemberek(new TurkiyeTurkcesi());

            double negDocsSum = 0, objDocsSum = 0, posDocsSum = 0;
            double docCount = 0;

            double[,] decision = new double[135, 3];
            //double maxTotalNeg = 0, maxTotalObj = 0, maxTotalPos = 0;
            //double minTotalNeg = 100000, minTotalObj = 100000, minTotalPos = 100000;
            // metin islemleri
            foreach (string filePath in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"\metinler"))
            {
                // PREPROCESSING
                // Metin okundu. Harfler kucuk harfe cevrildi.
                string[] lines = File.ReadAllLines(filePath, Encoding.GetEncoding("iso-8859-9"));
                lines = lines.Select(s => s.ToLowerInvariant()).ToArray();
                string document = "";
                foreach (string line in lines)
                    document += line + " ";

                List<string> tokenList = new List<string>();
                List<string> stems = new List<string>();
                List<int> negators = new List<int>();
                List<string> morph = new List<string>();

                    
                    // Satirlardaki kelimeleri birbirinden ayrildi.
                    IList<string> tokens;
                    tokens = (ClassicTokenizerTest.TestClassicTokenizerReturnDelimiterTrue(document));

                    // Turkce kararkterlerle yazilmamis olan kelimler duzeltildi.
                    foreach (string token in tokens)
                    {
                        if (zemberek.asciidenTurkceye(token).Length > 0)
                            tokenList.Add(zemberek.asciidenTurkceye(token)[0]);

                    }
                    tokenList = tokenList.Distinct().ToList<string>();
                    Language tr = LanguageFactory.Create(LanguageType.Turkish);
                    foreach (string token in tokenList)
                    {

                        IList<Word> solutions = tr.Analyze(token);

                        
                        /*if (tokenList.IndexOf(token) != tokenList.Count - 1 && (tokenList[tokenList.IndexOf(token) + 1].Equals("değil")))
                        {
                            Console.WriteLine(tokenList[tokenList.IndexOf(token)] + " " + tokenList[tokenList.IndexOf(token) + 1]);
                            negators.Add(tokenList.IndexOf(token));
                        }*/

                        // Kelimeler kok haline getirildi.
                        if (solutions.Count > 0)
                        {
                            stems.Add(solutions[solutions.Count - 1].GetStem().GetSurface());
                            
                            // Kelimelerin bicim bilgileri saklandi.
                            morph.Add(solutions[solutions.Count - 1].ToString());
                        }
                        else
                        {
                            stems.Add("");
                            morph.Add("");
                           // Console.WriteLine("***" + token);
                        }
                    }
                tokens.Clear();
                //stems = stems.Distinct().ToList<string>();

                // ANALYSIS
                bool flag = false;
                double totalNeg = 0, totalObj = 0, totalPos = 0;
                
                int counter = 0;
                
                for (int i = 0; i < stems.Count; i++)
                {
                    foreach (string lexeme in lexemes)
                    {
                        if (stems[i].Equals(lexeme))
                        {
                            int index = lexemes.IndexOf(lexeme);
                            flag = false;
                            // Bicim bilgilerinde olumsuzluk eki olan kelimeler saptandi.
                            string[] affixes = morph[i].Split('_');
                            foreach (string affix in affixes)
                            {
                                if (affix.Equals("OLUMSUZLUK") || affix.Equals("YETERSIZLIK")) // siz, suz?
                                {
                                    flag = true;
                                }

                            }
                            // Kendisinden sonra 'degil' kelimesi gelen kelimeler saptandi.
                            if (i + 1 != stems.Count)
                            {
                                if (stems[i + 1].Equals("değil"))
                                {
                                    if (flag)
                                        flag = false;
                                    else
                                        flag = true;
                                }
                            }
                            if (flag == false)
                            {
                                totalNeg += negVals[index];
                                totalPos += posVals[index];
                            }
                            else
                            {
                                totalPos += negVals[index];
                                totalNeg += posVals[index];
                            }
                            totalObj += objVals[index];
                            counter++;
                        }
                    }
                }
                /*
                //maxTotalNeg + " " + maxTotalObj + " " + maxTotalPos + " " + minTotalNeg + " " + minTotalObj + " " + minTotalPos
                //44.727 232.577000000002 46.6989999999999 0.18 1.804 0.229
                //Console.WriteLine(totalPos + " " + totalObj + " " + totalNeg);
                double top = 0;
                top += totalObj = (totalObj - 1.804) / (232.577000000002 - 1.804);
                top += totalNeg = (totalNeg - 0.18) / (44.727 - 0.18);
                top += totalPos = (totalPos - 0.229) / (46.6989999999999 - 0.229);
                totalObj /= top;
                totalNeg /= top;
                totalPos /= top;
                double esik=0.1;
                if(Math.Abs(totalPos-totalNeg)>esik)
                { 
                    if (totalNeg > totalPos)
                        Console.WriteLine("-1");
                    else
                        Console.WriteLine("1");
                }
                else
                    Console.WriteLine("0");*/
                //  / counter islemi yerine 0 - 1 normalizasyon yapilacak
                negDocsSum += totalNeg / counter;
                objDocsSum += totalObj / counter;
                posDocsSum += totalPos / counter;
                docCount++;
                content.Add(string.Format("{0}:\t{1:0.000} \t{2:0.000}\t{3:0.000}\t{4}/{5}", Path.GetFileName(filePath), totalNeg / counter, totalObj / counter, totalPos / counter, counter, stems.Count));
            }
            // Siniflara atanma skorlari, metindeki sozlukte bulunabilen kelimlerin sayisi ve metindeki toplam kelime sayisi bilgileri dosyaya yazildi.
            for (int i = 0; i < content.Count - 1; i++)
            {
                for (int j = i + 1; j < content.Count; j++)
                {
                    if (Convert.ToInt32(content[i].Split('.')[0]) > Convert.ToInt32(content[j].Split('.')[0]))
                    {
                        string temp = content[i];
                        content[i] = content[j];
                        content[j] = temp;
                    }
                }
            }
            FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "bilgi.txt", FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            foreach(string line in content)
            {
                sw.WriteLine(line);
            }
            sw.Close();
            fs.Close();

            // Siniflara atama esik degerleri hesaplandi.
            double negDocsMean = negDocsSum / docCount;
            double objDocsMean = objDocsSum / docCount;
            double posDocsMean = posDocsSum / docCount;

            // Metinlerin sinif bilgileri dosyaya yazildi.
            fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "sonuclar.txt", FileMode.Append, FileAccess.Write);
            sw = new StreamWriter(fs);
            string result = "";
            double olumsuz = 0, belirsiz = 0, olumlu = 0;
            foreach (string line in content)
            {
                if (Convert.ToDouble(line.Split('\t')[1]) >= negDocsMean && Convert.ToDouble(line.Split('\t')[3]) >= posDocsMean)
                {
                    if (Convert.ToDouble(line.Split('\t')[1]) >= negDocsMean && Convert.ToDouble(line.Split('\t')[3]) >= posDocsMean)
                    {
                        olumsuz++;
                        result = "Olumsuz";
                    }
                    else if (Convert.ToDouble(line.Split('\t')[1]) < negDocsMean && Convert.ToDouble(line.Split('\t')[3]) >= posDocsMean)
                    {
                        olumlu++;
                        result = "Olumlu";
                    }
                    else
                    {
                        belirsiz++;
                        result = "Belirsiz";
                    }
                }
                if (Convert.ToDouble(line.Split('\t')[1]) >= negDocsMean && Convert.ToDouble(line.Split('\t')[3]) < posDocsMean)
                {
                    olumsuz++;
                    result = "Olumsuz";
                }
                if (Convert.ToDouble(line.Split('\t')[1]) < negDocsMean && Convert.ToDouble(line.Split('\t')[3]) >= posDocsMean)
                {
                    olumlu++;
                    result = "Olumlu";
                }
                if (Convert.ToDouble(line.Split('\t')[1]) < negDocsMean && Convert.ToDouble(line.Split('\t')[3]) < posDocsMean)
                {
                    belirsiz++;
                    result = "Belirsiz";
                }
                sw.WriteLine("{0}\t{1}", line.Split('\t')[0], result);
            }
            sw.Close();
            fs.Close();

            // Kullanicidan kelime alindi.
            Console.WriteLine("Kelime: ");
            string newWord = Console.ReadLine();

            List<int> indexes = new List<int>();
            foreach (string filePath in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"\metinler"))
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.GetEncoding("iso-8859-9"));
                lines = lines.Select(s => s.ToLowerInvariant()).ToArray();

                // Metinlerdeki kelimeler okundu.
                List<string> words = new List<string>();
                foreach (string line in lines)
                {
                    IList<string> tokens;
                    tokens = (ClassicTokenizerTest.TestClassicTokenizerReturnDelimiterTrue(line));
                    
                    foreach (string token in tokens)
                    {
                        words.Add(token);
                    }
                }
                words = words.Distinct().ToList<string>();

                // Girilen kelimeyi iceren metinlerin indisleri kaydedildi.
                foreach (string word in words)
                {
                    if (newWord.Equals(word))
                    {
                        indexes.Add(Convert.ToInt32(Path.GetFileName(filePath).Split('.')[0]) - 1);
                    }
                }
            }

            // Kelimeyi iceren metinlerin siniflarina bakildi.
            List<string> results = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "sonuclar.txt", Encoding.GetEncoding("iso-8859-9")).ToList<string>();
            double negCount = 0, objCount = 0, posCount = 0;
            if (indexes.Count == 0)
            {
                Console.WriteLine("Bulunamadi.");
            }
            else
            {
                foreach (int index in indexes)
                {
                    int n = 0;
                    foreach (string res in results)
                    {
                        if (index == Convert.ToInt32(res.Split('.')[0]))
                        {
                            n = results.IndexOf(res);
                        }
                    }

                    if (results[n].Split('\t')[1].Equals("Olumsuz"))
                    {
                        negCount++;
                    }
                    else if (results[n].Split('\t')[1].Equals("Belirsiz"))
                    {
                        objCount++;
                    }
                    else
                    {
                        posCount++;
                    }
                }

                // Kelimenin siniflara dahil olma olasiliklari hesaplandi.
                double negNB = (negCount / (negCount + objCount + posCount)) * (olumsuz / docCount);
                double objNB = (objCount / (negCount + objCount + posCount)) * (belirsiz / docCount);
                double posNB = (posCount / (negCount + objCount + posCount)) * (olumlu / docCount);

                // Kelimenin dahil oldugu sinif yazdirildi.
                if (negNB > objNB)
                {
                    if (negCount > posNB)
                    {
                        Console.WriteLine("Olumsuz.\t{0:0.00}", negNB);
                    }
                    else
                    {
                        Console.WriteLine("Olumlu.\t{0:0.00}", posNB);
                    }
                }
                else
                {
                    if (objNB > posNB)
                    {
                        Console.WriteLine("Belirsiz.\t{0:0.00}", objNB);
                    }
                    else
                    {
                        Console.WriteLine("Olumlu.\t{0:0.00}", posNB);
                    }
                }
            }
        }
    }
}