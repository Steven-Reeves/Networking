// Responder
// "script" executable for responding to simple surveys on an SDServer
// See Assignment 5, CST 415 Fall 2017

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Responder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // read header variables from input
                Dictionary<string, string> variables = ReadVariables();
                string method = GetVariable(variables, "method", true);
                string target = GetVariable(variables, "target", true);
                
                string response = null;
                if (method == "post")
                {
                    // read form variables
                    Dictionary<string, string> formVariables = ReadVariables();

                    // examine form values
                    bool sayit = GetVariable(formVariables, "sayit", false) != null;
                    string shortresponse = GetVariable(formVariables, "shortresponse", false);

                    // generate response
                    if (sayit)
                    {
                        response = "<html><body>Thank you for your response: " + shortresponse + "</body></html>";
                    }
                    else
                    {
                        response = "<html><body>That's cool, we'll leave you be.</body></html>";
                    }
                }
                else if (method == "get")
                {
                    // generate response
                    response = "<html><body>We have many responses. I can't say how many. It's, umh, confidential.</body></html>";
                }
                else
                {
                    throw new Exception("Unrecognized method " + method);
                }

                // write session variables
                WriteSessionVariables(variables);

                if (response != null)
                {
                    // write content length
                    WriteVariable("length", response.Length.ToString());

                    // write blank line
                    Console.WriteLine();

                    // write content
                    WriteContent(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error");
                Console.WriteLine(ex.Message);
                System.Environment.ExitCode = -1;
            }
        }

        static Dictionary<string, string> ReadVariables()
        {
            Dictionary<string, string> variables = new Dictionary<string, string>();

            string line = null;
            while ((line = Console.ReadLine()) != null && line.Length > 0)
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                    variables[parts[0]] = parts[1];
            }

            return variables; 
        }

        static string GetVariable(Dictionary<string, string> variables, string key, bool required)
        {
            if (variables.ContainsKey(key))
                return variables[key];
            else if (required)
                throw new Exception("Cannot find variable " + key);

            return null;
        }

        static void WriteSessionVariables(Dictionary<string, string> variables)
        {
            foreach (KeyValuePair<string,string> variable in variables)
            {
                if (variable.Key.StartsWith("session-"))
                    WriteVariable(variable.Key, variable.Value);
            }
        }

        static void WriteVariable(string key, string value)
        {
            Console.WriteLine(key + "=" + value);
        }

        static void WriteContent(string response)
        {
            Console.WriteLine(response);
        }
    }
}
