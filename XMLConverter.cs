using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Virinco.WATS.Interface;

namespace XMLConverter
{
    public class XMLConverter : IReportConverter_v2
    {
        public void CleanUp(){} //Nothing to cleanup here

        IDictionary<string, string> arguments; //Save a copy of arguments from Converters.xml

        const string DefaultSequenceFile = "XMLSequence";

        public Dictionary<string, string> ConverterParameters { get; } = new Dictionary<string, string>
        {
            { "sequenceFile", DefaultSequenceFile }
        };

        readonly string sequenceFile;

        public XMLConverter(IDictionary<string, string> args)
        {
            arguments = args; //The constructor is called once when client is started
            sequenceFile = args != null && args.TryGetValue("sequenceFile", out var sf) && !string.IsNullOrEmpty(sf) ? sf : DefaultSequenceFile;
        }

        public Report ImportReport(TDM api, Stream file)
        {
            // The stream is owned by the WATS Client and must not be disposed
            XDocument report = XDocument.Load(file);
            {
                XElement testreport = report.Element("TestReport") ?? throw new InvalidDataException("Missing element: TestReport");
                XElement header = testreport.Element("Header") ?? throw new InvalidDataException("Missing element: Header");
                UUTReport uut = api.CreateUUTReport(
                    HeaderValue(header, "Operator"),
                    HeaderValue(header, "PartNumber"),
                    HeaderValue(header, "Revision"),
                    HeaderValue(header, "SerialNumber"),
                    HeaderValue(header, "OperationTypeCode"),
                    sequenceFile, "1.0");
                uut.StartDateTime = DateTime.ParseExact(HeaderValue(header, "StartDate"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                uut.ExecutionTime = double.Parse(HeaderValue(header, "ExecutionTime"), CultureInfo.InvariantCulture);
                foreach (var sequence in testreport.Elements("Sequence"))
                {
                    SequenceCall s = uut.GetRootSequenceCall().AddSequenceCall(sequence.Attribute("Name").Value);
                    foreach (var t in sequence.Elements())
                    {
                        if (t.Name == "NumericTest")
                            s.AddNumericLimitStep(t.Attribute("Name").Value).
                                AddTest(GetValue(t, "Measure"), CompOperatorType.GELE,
                                GetValue(t, "LowLim"),
                                GetValue(t, "HighLim"),
                                t.Attribute("Unit").Value);
                        else if (t.Name == "StringTest")
                            s.AddStringValueStep(t.Attribute("Name").Value).
                                AddTest(CompOperatorType.IGNORECASE, t.Attribute("Measure").Value, t.Attribute("Limit").Value);
                        else if (t.Name == "PassFailTest")
                            s.AddPassFailStep(t.Attribute("Name").Value).
                                AddTest(t.Attribute("Measure").Value.ToLower() == "true" ? true : false);
                    }
                }
                return uut;
            }
        }
        static string HeaderValue(XElement header, string name)
        {
            return header.Element(name)?.Value ?? throw new InvalidDataException("Missing header element: " + name);
        }
        double GetValue(XElement x, string name)
        {
            return double.Parse(x.Attribute(name).Value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }
    }
}
