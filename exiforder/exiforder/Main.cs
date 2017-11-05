using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Security.Cryptography;

namespace exiforder
{
	class Statistics
	{
		public int totalFiles = 0;
		public int filesNoExif = 0;
		public int dupFingerprint = 0;
		public override string ToString()
		{
			return "Total files: " + totalFiles;// + " NoEXIF: " + filesNoExif + " Dup: " + dupFingerprint;
		}
	}
	
	class MainClass
	{
		public static void Main (string[] args)
		{
			Statistics stats = new Statistics();

			foreach(string file in getImageFiles(args))
			{
				System.Console.Write("NAME: " + file);
				++stats.totalFiles;
				try
				{
					DateTime dt = getDate(file);
					string newStamp = dt.ToString("yyyy/MM/yyyyMMdd_HHmmss");
					string ext = Path.GetExtension(file);
					if (file.EndsWith(newStamp + ext))
					{
						System.Console.WriteLine("\talready sorted. Skipping...");
						continue;
					}
					if (File.Exists(newStamp + ext))
					{
						if (areFilesIdentical(newStamp + ext, file))
						{
							System.Console.Write("\talready present, deleting duplicate " + file + "::" + newStamp + ext);
							File.Delete(file);
						}
						else
						{
							System.Console.Write("\tname collision. Skipping...");
						}
					}
					else
					{
						System.Console.Write("\trenaming to: " + newStamp + ext);
						createIfNotExists(dt.ToString("yyyy"));
						createIfNotExists(dt.ToString("yyyy/MM"));
						File.Move(file, newStamp + ext);
					}
					System.Console.WriteLine();
				}
				catch(Exception e)
				{
					System.Console.WriteLine("\tError: " + e.Message);
				}
			}
			System.Console.WriteLine("Statistics:");
			System.Console.WriteLine(stats.ToString());
			
//			if (stats.dupFingerprint > 0)
//			{
//				foreach(var kvp in fingerprints)
//				{
//					if (kvp.Value.Count > 1)
//					{
//						System.Console.WriteLine("The hash " + kvp.Key + " has the following instances:");
//						foreach(string f in kvp.Value)
//						{
//							System.Console.WriteLine(f);
//						}
//						System.Console.WriteLine();
//					}
//				}
//			}
		}
		
		static DateTime getDate(string fname)
		{
			try
			{
				DateTime? dtExif = getEXIFDate(fname);
				if (!dtExif.HasValue)
					throw new Exception();
				return dtExif.Value;
			}
			catch(Exception) 
			{
				throw new Exception("No EXIF date. Skipping...");
			}
		}
		
		static IEnumerable<string> getImageFiles(string[] args)
		{
			string filter = "*";
			if (args.Length == 1)
				filter = args[0];
			System.Console.WriteLine("Using filter " + filter);
			string[] files = Directory.GetFiles(".", filter, SearchOption.AllDirectories);
			foreach(string file in files)
			{
				yield return file;
			}
		}
		
		static void createIfNotExists(string path)
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}
		
		static bool areFilesIdentical(string a, string b)
		{
			string fa = getFingerprint(a);
			string fb = getFingerprint(b);
			return fa.Equals(fb);
		}
				
		static string getFingerprint(string fname)
		{
			StringBuilder sbFingerprint = new StringBuilder();
			using(FileStream fs = File.OpenRead(fname))
			{
				SHA256Managed sha = new SHA256Managed();
				byte[] checksum = sha.ComputeHash(fs);
				sbFingerprint.Append(BitConverter.ToString(checksum).Replace("-", String.Empty));
				sbFingerprint.Append("_");
			}
			FileInfo f = new FileInfo(fname);
			sbFingerprint.Append(f.Length);
			return sbFingerprint.ToString();
		}
		
		static DateTime? getEXIFDate(string fname)
		{
			using (FileStream fs = new FileStream(fname,FileMode.Open))
			{
				using (Image img = Image.FromStream(fs,true,false))
				{
					foreach (PropertyItem pi in img.PropertyItems)
					{
						if ((pi.Id == 0x0132) || (pi.Id == 0x9003) || (pi.Id == 0x9004))
						{
							Encoding ascii=Encoding.ASCII;
							string CreateDate = ascii.GetString(pi.Value,0,pi.Len-1);
							return DateTime.ParseExact(CreateDate, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
						}
					}
				}
			}
			return null;
		}
	}
}

