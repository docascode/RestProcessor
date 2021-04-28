namespace RedirectSourceMapping
{
    using RedirectSourceMapping.Model;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;

    public class PublishFileExtractor : AbstractExtractor<PublishFile>
    {
        private int Counter=1;
        public PublishFileExtractor(string dirPath)
        {
            this.dirPath = dirPath;
        }
        protected override void Extract()
        {
            LoadFile();
        }

        private void LoadFile()
        {
            Console.WriteLine(dirPath+ "Start Extract");
            if (string.IsNullOrEmpty(dirPath))
            {
                var erroeMsg = "Error:PuslichFile Path is Empty";
                Console.WriteLine(erroeMsg);
                throw new Exception(erroeMsg);
            }

            if (!Directory.Exists(dirPath))
            {
                var erroeMsg = string.Format("Error:PuslichFile Path {0} destn't exist", dirPath);
                Console.WriteLine(erroeMsg);
                throw new Exception(erroeMsg);
            }

            var files = Directory.GetFiles(dirPath);
            if (files.Length !=1)
            {
                var erroeMsg = "Error:PuslichFile can only have one file, not empty or multiple files";
                Console.WriteLine(erroeMsg);
                throw new Exception(erroeMsg);
            }

            OpenCSV(files[0]);
            Console.WriteLine(dirPath + "End Extract");
        }

        public void OpenCSV(string filePath)
        {
            var encoding = GetType(filePath);
            var fs = new FileStream(filePath, FileMode.Open,FileAccess.Read);

            var sr = new StreamReader(fs, encoding);

            var headDic = new Dictionary<string, int>();

            string strLine = "";
            string[] aryLine = null;
            string[] tableHead = null;
            int columnCount = 0;
            bool IsFirst = true;
            while ((strLine = sr.ReadLine()) != null)
            {
                
                if (IsFirst == true)
                {
                    tableHead = strLine.Split(',');
                    IsFirst = false;
                    columnCount = tableHead.Length;
                    for (int i = 0; i < columnCount; i++)
                    {
                        headDic.Add(tableHead[i],i);
                    }
                }
                else
                {
                    aryLine = strLine.Split(',');
                    var obj=GeneratePublishFile(headDic, aryLine);
                    if (!string.IsNullOrEmpty(obj.ContentGitUrl))
                    {
                        var index = obj.ContentGitUrl.IndexOf(Constants.SplitFolder);
                        if (index == -1)
                        {
                            Console.WriteLine("Error:"+obj.ToString());
                            continue;
                        }
                        var key = obj.ContentGitUrl.Substring(index+ Constants.SplitFolder.Length+1);
                        //key = key.Replace("-", "");
                        if (!string.IsNullOrEmpty(key))
                        {
                            keyValuePairs.Add(key, obj);
                        }
                    }
                   
                }
            }

            sr.Close();
            fs.Close();
        }

        public PublishFile GeneratePublishFile(Dictionary<string, int> headDic,string[] aryLine)
        {
            var publishFile=new PublishFile();
            var t = publishFile.GetType();
            var bl = false;
            var sb = new StringBuilder();
            var properties = t.GetProperties();
            foreach (var property in properties)
            {
                if (!property.IsDefined(typeof(FieldAttribute), false)) continue;
                var attributes = property.GetCustomAttributes();
                foreach (var attribute in attributes)
                {
                    var name = (string)attribute.GetType().GetProperty("Name").GetValue(attribute);
                    if (headDic.TryGetValue(name, out var value))
                    {
                        sb.Append(name + ":" + aryLine[value] + "|");
                        bl = true;
                        property.SetValue(publishFile, aryLine[value]);
                    }
                }
            }

            if (bl)
            {
                Console.WriteLine(Counter+" Line:");
                Console.WriteLine(sb.ToString());
                Counter++;
            }

            return publishFile;
        }
         

        public static Encoding GetType(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var r = GetType(fs);
            fs.Close();
            return r;
        }

        public static Encoding GetType(FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //BOM
            var reVal = Encoding.Default;

            var r = new BinaryReader(fs, Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = System.Text.Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = System.Text.Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = System.Text.Encoding.Unicode;
            }
            r.Close();
            return reVal;
        }

        /// check BOM  UTF8 format
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1;  //Calculates the number of bytes that should remain in the character currently being analyzed
            byte curByte; //analysis current byte.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //If the first digit of the tag is not 0, it should start with at least two one, such as 110xxxxx..... 1111110x　
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //If UTF-8, the first bit must be 1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("Unexpected byte format");
            }
            return true;

        }
    }
}
