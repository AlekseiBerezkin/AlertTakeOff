using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlertTakeOff
{
    static class FileProvider
    {
        static public IEnumerable<string> ReadFile()
        {

            try
            {
                List<string> data = new List<string>();
                using (StreamReader reader = new StreamReader("pairs.txt"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        data.Add(line);
                    }

                }
                return data;
            }
            catch (Exception)
            {
                return null;
            }

        }
    }
}
