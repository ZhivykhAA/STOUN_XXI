using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MySql.Data.MySqlClient;

namespace STOUN_XXI_test
{
    class Program
    {
        static void Main(string[] args)
        {
            // the parameters for connecting to database currency
            string connstring = "server=localhost;userid=valute;database=currency;password=''";

            // filling out table guide and creating table rate
            //fillingOutTableGuide(connstring);

            DateTime today = DateTime.Today;
            checkDateInDatabase(connstring, today);

            // filling out table rate 
            //fillingOutTableRate(connstring, today);

            today = new DateTime(2021, 2, 10);
            string currency = "Болгарский лев";
            Console.WriteLine("The exchange rate of the required currency on " + today.ToShortDateString() + " is " + findOutRate(currency, today));
            Console.ReadLine();
        }


        // find out the currency rate
        static string findOutRate(string name, DateTime date)
        {
            if (date > DateTime.Today)
            {
                return "The required day has not yet arrived!";
            }
            else
            {
                string connstring = "server=localhost;userid=valute;database=currency;password=''";

                checkDateInDatabase(connstring, date);

                // find out ID of currency by name
                string idCurrency = "";
                using (MySqlConnection mConnection = new MySqlConnection(connstring))
                {
                    mConnection.Open();

                    using (MySqlCommand myCmd = new MySqlCommand("SELECT ID FROM guide " +
                                                                "WHERE Name = '" + name + "';", mConnection))
                    {
                        MySqlDataReader reader = myCmd.ExecuteReader();
                        while (reader.Read())
                        {
                            idCurrency = reader[0].ToString();
                        }
                        reader.Close();
                    }
                }

                string rate = "";
                // find out the currency rate by ID
                using (MySqlConnection mConnection = new MySqlConnection(connstring))
                {
                    mConnection.Open();

                    // insert new information into table rate
                    using (MySqlCommand myCmd = new MySqlCommand("SELECT " + idCurrency + " FROM rate " +
                                                                "WHERE date = '" + date.Year + "-" + date.Month + "-" + date.Day + "';", mConnection))
                    {
                        MySqlDataReader reader = myCmd.ExecuteReader();
                        while (reader.Read())
                        {
                            rate = reader[0].ToString();
                        }
                        reader.Close();
                    }
                }

                if (rate == "")
                {
                    return "There is no information on the required currency!";
                }
                else
                {
                    return rate;
                }
            }
        }


        // Checks the date in the rate table. If the date is not in the table, it enters new information in the table rate.
        static void checkDateInDatabase(string connstring, DateTime date)
        {
            string checkDate = "";

            using (MySqlConnection mConnection = new MySqlConnection(connstring))
            {
                mConnection.Open();

                using (MySqlCommand myCmd = new MySqlCommand("SELECT COUNT(*) FROM rate " +
                                                            "WHERE date = '" + date.Year + "-" + date.Month + "-" + date.Day + "';", mConnection))
                {
                    MySqlDataReader reader = myCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        checkDate = reader[0].ToString();
                    }
                    reader.Close();
                }
            }

            if (checkDate == "0")
            {
                fillingOutTableRate(connstring, date);
            }
        }


        // filling out table rate 
        static void fillingOutTableRate(string connstring, DateTime today)
        {
            string urlInfoDay = "http://www.cbr.ru/scripts/XML_daily.asp?date_req="+today.ToShortDateString();
            XmlTextReader reader = new XmlTextReader(urlInfoDay);

            List<string> ids = new List<string>();
            List<string> vals = new List<string>();
            string name = "";
            string value = "";

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        name = reader.Name;

                        while (reader.MoveToNextAttribute()) // Read the attributes. 
                        {
                            name = reader.Name;
                            if (name == "ID")
                            {
                                value = reader.Value;
                                ids.Add(value);
                            }
                        }
                        break;
                    case XmlNodeType.Text: // the text in each element.
                        if (name == "Value")
                        {
                            value = reader.Value;
                            vals.Add(value.Replace(',', '.'));
                        }
                        break;
                }
            }

            using (MySqlConnection mConnection = new MySqlConnection(connstring))
            {
                mConnection.Open();

                // insert new information into table rate
                using (MySqlCommand myCmd = new MySqlCommand("INSERT INTO rate " + 
                                                            "(date, " + String.Join(", ", ids) + ") VALUES " +
                                                            "('" + today.Year + "-" + today.Month + "-" + today.Day + "', " + 
                                                            String.Join(", ", vals) + ");", mConnection))
                {
                    myCmd.ExecuteNonQuery();
                }
            }
        }


        // check that str is one of the columns in the table guide
        static int findStr(string str)
        {
            string[] headers = { "ID", "Name", "EngName", "Nominal", "ParentCode", "ISO_Num_Code", "ISO_Char_Code" };

            for (int i = 0; i < headers.Length; ++i)
            {
                if (headers[i] == str)
                {
                    return i;
                }
            }
            return -1;
        }


        // filling out table guide and creating table rate
        static void fillingOutTableGuide(string connstring)
        {
            StringBuilder forQueryCreate = new StringBuilder(""); // string for create query
            List<string> forQueryInsert = new List<string>(); // array of rows for insert query

            string[] input = { "null", "null", "null", "null", "null", "null", "null" };
            List<string> currencyInfo = new List<string>(input);  // list of one currency parameters
            
            string name = "";
            string value = "";

            String urlInfoVal = "http://www.cbr.ru/scripts/XML_valFull.asp";
            XmlTextReader reader = new XmlTextReader(urlInfoVal);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        name = reader.Name;

                        while (reader.MoveToNextAttribute()) // Read the attributes. 
                        {
                            name = reader.Name;
                            if (findStr(name) == 0)
                            {
                                value = reader.Value;
                                forQueryCreate.Append(value + " FLOAT(10,4), "); //create 1 column for the currency
                                currencyInfo[0] = "'" + value + "'";
                            }
                        }
                        break;
                    case XmlNodeType.Text: // the text in each element.
                        value = reader.Value;
                        try
                        {
                            int index = findStr(name);
                            if (index == 3 || index == 5) // these elements are INT
                            {
                                currencyInfo[index] = value;
                            }
                            else
                            {
                                currencyInfo[index] = "'" + value + "'";
                            }
                        }
                        catch (Exception e)
                        {

                        }
                        break;
                    case XmlNodeType.EndElement: // the end of the element.
                        if (reader.Name == "Item")
                        {
                            forQueryInsert.Add("(" + String.Join(", ", currencyInfo) + ")");

                            for (int i = 0; i < currencyInfo.Count; ++i)
                            {
                                currencyInfo[i] = "null";
                            }
                        }
                        break;
                }
            }

            using (MySqlConnection mConnection = new MySqlConnection(connstring))
            {
                mConnection.Open();

                // insert into guide
                using (MySqlCommand myCmd = new MySqlCommand("INSERT INTO guide (ID, Name, EngName, Nominal, ParentCode, ISO_Num_Code, ISO_Char_Code) VALUES " +
                                                            String.Join(", ", forQueryInsert) + ";", mConnection))
                {
                    myCmd.ExecuteNonQuery();
                }

                // create table rate
                using (MySqlCommand myCmd = new MySqlCommand("CREATE TABLE rate (date DATE NOT NULL, " +
                                                            forQueryCreate.ToString() +
                                                            "PRIMARY KEY (date)) ENGINE InnoDB DEFAULT CHARSET = utf8;", mConnection))
                {
                    myCmd.ExecuteNonQuery();
                }
            }
        }

        
        static void printArr(List<string> arr)
        {
            for (int i = 0; i < arr.Count(); ++i)
            {
                Console.Write(arr[i] + " ");
            }
            Console.Write("\n");
        }
    }
}
