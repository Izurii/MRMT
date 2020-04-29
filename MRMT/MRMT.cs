using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Devart.Data.SQLite;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace MRMT
{
    public partial class MRMT : Form
    {

        String csvPath, path;
        Process p = new Process();
        String currentDirectory = Directory.GetCurrentDirectory().ToString();
        String curDirCor;
        SQLiteDataReader reader, reader2;
        SQLiteConnection dbConnection = new SQLiteConnection();
        SQLiteCommand cmd, cmd2;
        List<String> mangasN = new List<string>();
        List<String> links = new List<string>();


        public MRMT()
        {
            InitializeComponent();
        }

        private void MRMT_Load(object sender, EventArgs e)
        {
            checkBox1.Enabled = false; button2.Enabled = false; button4.Enabled = false;
            cmd = dbConnection.CreateCommand();
            cmd2 = dbConnection.CreateCommand();

        }

        /////////////////////////////////////////////////////////BUTTONS///////////////////////////////////////////////////////////////////////////////

        private void button1_Click(object sender, EventArgs e)
        {

            dbPath.ShowDialog();
            textBox1.Text = dbPath.FileName.ToString();
            path = textBox1.Text.ToString();
            checkBox1.Enabled = true; button2.Enabled = true; button4.Enabled = true;

        }

        private void button2_Click(object sender, EventArgs e)
        {

            convertCSV();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (File.Exists(@"mangarock.db") == true)
            {
                File.Delete(@"mangarock.db");
            }
            else
            {
                try
                {
                    getDB();
                    Decompress();
                    ExtractTar();
                    cpdlFiles();
                }
                catch (Exception ex) { }
                finally
                {
                    MessageBox.Show("DB Extracted.", "Yeee!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {

            dbConnection.ConnectionString = @"Data Source=" + path + ";FailIfMissing=False;";
            dbConnection.Open();

            try
            {
                createJson();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            finally
            {
                MessageBox.Show("It's done bro.", "Yeee!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
       
        ////////////////////////////////////////////////////////////////ExtractDB//////////////////////////////////////////////////////////////////////

        void getDB()
        {
            try
            {
                p.StartInfo.FileName = addQuotes(currentDirectory + @"\adb.exe");
                p.StartInfo.Arguments = "backup -noapk com.notabasement.mangarock.android.lotus";
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        void Decompress()
        {
            try
            {
                p.StartInfo.FileName = "java";
                p.StartInfo.Arguments = "-jar " + addQuotes(currentDirectory) + "\abe.jar unpack backup.ab manga.tar";
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }

        }

        void ExtractTar()
        {
            try
            {
                MessageBox.Show(addQuotes(currentDirectory + @"\mangatar"));
                if (!Directory.Exists(addQuotes(currentDirectory + @"\mangatar")))
                {
                    Directory.CreateDirectory(addQuotes(currentDirectory + @"\mangatar"));
                }

                p.StartInfo.FileName = addQuotes(currentDirectory + @"\tar.exe");
                p.StartInfo.Arguments = "xvf manga.tar -C mangatar";
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        void cpdlFiles()
        {
            try
            {
                File.Copy(addQuotes(currentDirectory + @"\mangatar\apps\com.notabasement.mangarock.android.lotus\db\mangarock.db"), addQuotes(currentDirectory + "\\mangarock.db"));
                if (Directory.Exists(addQuotes(currentDirectory + @"\mangatar")))
                {
                    Directory.Delete(addQuotes(currentDirectory + @"\mangatar"), true);
                }
                File.Delete(addQuotes(currentDirectory + @"\backup.ab"));
                File.Delete(addQuotes(currentDirectory + @"\manga.tar"));
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////exportCSV///////////////////////////////////////////////////////////////////

        void convertCSV()
        {

            dbConnection.ConnectionString = @"Data Source=" + path + ";FailIfMissing=False;";

            try
            {

                dbConnection.Open();
                cmd.CommandText = "SELECT manga_name, author, last_read FROM Favorites";
                reader = cmd.ExecuteReader();

                using (StreamWriter sw = File.CreateText(@"mangalist.csv"))
                {
                    try
                    {

                        if (checkBox1.Checked)
                        {
                            sw.WriteLine(String.Format("{0};{1}", "Manga name", "Author's name") + "\n");
                            while (reader.Read())
                            {
                                byte[] bytes = Encoding.Default.GetBytes(String.Format("{0};{1}", reader.GetString("manga_name"), reader.GetString("author")));
                                string mangainf = Encoding.UTF8.GetString(bytes);
                                sw.WriteLine(mangainf);
                            }
                        }

                        if (!checkBox1.Checked)
                        {

                            sw.WriteLine(String.Format("{0}", "Manga name") + "\n");
                            while (reader.Read())
                            {
                                byte[] bytes = Encoding.Default.GetBytes(String.Format("{0}", reader.GetString("manga_name")));
                                string mangainf = Encoding.UTF8.GetString(bytes);
                                sw.WriteLine(mangainf);
                            }

                        }

                    }
                    catch (Exception ex) { MessageBox.Show(ex.ToString()); }
                    finally
                    {
                        MessageBox.Show("It's done bro.", "Yeee!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
                dbConnection.Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////exportJSON///////////////////////////////////////////////////////////////////

        class Parent
        {
            public String version { get; set; }
            public List<object> mangas { get; set; }
        }

        class Manga
        {

            public List<object> manga { get; set; }
            public List<Chapter> chapters { get; set; }

        }

        class Chapter
        {

            public string u { get; set; }
            public int r { get; set; }

        }

        void createJson()
        {

            var json = new Parent()
            {
                version = "2",
                mangas = Mangaa(),
            };

            var sjson = JsonConvert.SerializeObject(json, Formatting.Indented);
            File.WriteAllText("tachiyomi.json", sjson);
            links.Clear();

        }

        List<object> Mangaa()
        {
            List<object> manga = new List<object>();
            cmd.CommandText = "SELECT * FROM Favorites";
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString("source_id") == "1") // MangaEden
                { mangasN.Add(reader.GetString("manga_id")); }
                else

                if (reader.GetString("source_id") == "4") //MangaReader
                { mangasN.Add(reader.GetString("manga_id")); }
                else

                if (reader.GetString("source_id") == "71") //MangaRock
                { mangasN.Add(reader.GetString("manga_id")); }

            }

            for (int i = 0; i < mangasN.Count; i++)
            {

                cmd2.CommandText = "SELECT * FROM Favorites WHERE manga_id = '" + mangasN[i] + "'";
                reader2 = cmd2.ExecuteReader();
                while (reader2.Read())
                {

                    String nameEdenReader = reader2.GetString("manga_name").Replace(' ', '-').ToLower();

                    if (reader2.GetString("source_id") == "1")
                    {
                        //MangaEden
                        manga.Add(
                        new Manga()
                        {
                            manga = new List<object> { "/en/en-manga/" + nameEdenReader, reader2.GetString("manga_name"), 6894303465364688269, 0, 0 },
                            chapters = Chapters(reader2.GetString("manga_id"), reader2.GetString("source_id"), nameEdenReader)

                        });
                    }

                    if (reader2.GetString("source_id") == "4")
                    {
                        //MangaReader
                        manga.Add(
                        new Manga()
                        {
                            manga = new List<object> { "/en/en-manga/" + nameEdenReader, reader2.GetString("manga_name"), 789561949979941461, 0, 0 },
                            chapters = Chapters(reader2.GetString("manga_id"), reader2.GetString("source_id"), nameEdenReader)

                        });

                    }

                    if (reader2.GetString("source_id") == "71")
                    {
                        //MangaRock
                        manga.Add(
                        new Manga()
                        {
                            manga = new List<object> { "/manga/" + reader2.GetString("manga_oid"), reader2.GetString("manga_name"), 1554176584893433663, 0, 0 },
                            chapters = Chapters(reader2.GetString("manga_id"), reader2.GetString("source_id"), null)

                        });

                    }


                }
            }

            return manga;
        }

        String correctedChapter(String chapter)
        {

            int index = chapter.IndexOf(':');
            string correct;

            if (index >= 0)
            {
                correct = Regex.Replace(chapter.Substring(0, index), "[^0-9]", "");
                correct.Replace(" ", "");

            }
            else
            { correct = chapter; }

            return correct;

        }

        List<Chapter> Chapters(String mId, String sId, String correctedName)
        {
            links.Clear();
            List<Chapter> chapters = new List<Chapter>();
            chapters.Clear();
            cmd.CommandText = "SELECT * FROM MangaChapter WHERE manga_id = '" + mId + "'";
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {

                if (sId == "1") //MangaEden
                { links.Add(correctedChapter(reader.GetString("title"))); }

                if (sId == "4") //MangaReader
                    links.Add(correctedChapter(reader.GetString("title")));

                if (sId == "71") //MangaRock
                    links.Add(reader.GetString("oid"));

            }
            for (int i = 0; i < links.Count; i++)
            {
                if (sId == "1") //MangaEden
                {
                    chapters.Add(new Chapter()
                    {
                        u = "/en/en-manga/" + correctedName + "/" + links[i] + "/1/",
                        r = 1
                    });
                }
                if (sId == "4") //MangaReader
                {
                    chapters.Add(new Chapter()
                    {
                        u = "/en/en-manga/" + correctedName + "/" + links[i] + "/1/",
                        r = 1
                    });
                }
                if (sId == "71") //MangaRock
                {
                    chapters.Add(new Chapter()
                    {
                        u = "/pagesv2?oid=" + links[i],
                        r = 1
                    });
                }

            }
            return chapters;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public string addQuotes(string path)
        {
            return !string.IsNullOrWhiteSpace(path) ?
                path.Contains(" ") && (!path.StartsWith("\"") && !path.EndsWith("\"")) ?
                    "\"" + path + "\"" : path :
                    string.Empty;
        }

    }

}
