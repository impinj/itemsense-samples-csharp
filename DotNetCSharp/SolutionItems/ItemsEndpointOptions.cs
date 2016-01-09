using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemSense
{
    class ItemsEndpointOptions
    {
        private const string startOfFilter = @"?";
        private const string filterDelimiter = @"&";

        private string _epcPrefix;
        private string _zoneNames;
        private string _presenceConfidence;
        private DateTime? _fromTime = null;
        private string _epcFormat;
        private string _facility;
        private string _pageMarker;
        private int _pageSize;


        public ItemsEndpointOptions()
        {
            //Set default values
            _pageSize = 1000;
        }

        public void SetEpcPrefix(string epcPrefix) { _epcPrefix = epcPrefix; }

        public void SetZoneNames(string zoneNames) { _zoneNames = zoneNames; }

        public void SetPresenceConfidence(string presenceConfidence) { _presenceConfidence = presenceConfidence; }

        public DateTime? FromTime
        {
            get { return _fromTime; }
            set { _fromTime = value; }
        }

        public void SetFromTime(string fromTime)
        {
            if (null != fromTime)
            {
                _fromTime = DateTime.Parse(
                    fromTime,
                    null, 
                    System.Globalization.DateTimeStyles.RoundtripKind
                    );
            }
        }

        public void SetEpcFormat(string epcFormat) { _epcFormat = epcFormat; }

        public void SetFacility(string facility) { _facility = facility; }

        public void SetPageMarker(string pageMarker)
        {
            _pageMarker =
                (null == pageMarker) ?
                pageMarker :
                System.Web.HttpUtility.UrlEncode(pageMarker);
                // For .NET frameworks higher than 4.0, use
                //System.Net.WebUtility.UrlEncode(pageMarker);
        }

        public void SetPageSize(int size) { _pageSize = size; }

        public void SetFullOptionsFromString(string options)
        {
            if (false == string.IsNullOrEmpty(options))
            {
                try
                {
                    // Format of string is <option1>=<value1>:<option2>=<value2>:..<optionn>=<valuen>
                    // Break strings up into options
                    List<string> optionValues = new List<string>(options.Split(':'));
                    string timeValue = string.Empty;

                    // Iterate through list of options
                    foreach (string optionValuePair in optionValues)
                    {
                        // TODO - fix logic so that a *Time option as the last parameter is handled

                        // Check to see whether this is a time filter. If so,
                        // then there may be ':' characters in the time value, so we
                        // need to construct the full time value, ':' characters
                        // and all
                        if (optionValuePair.Contains("Time"))
                        {
                            timeValue = optionValuePair;
                        }
                        else if (false == string.IsNullOrEmpty(timeValue))
                        {
                            if (false == optionValuePair.Contains('='))
                            {
                                timeValue += ":" + optionValuePair;

                                if (optionValuePair == optionValues.Last())
                                {
                                    setFilterFromOptionValuePair(timeValue);
                                    timeValue = string.Empty;
                                }
                            }
                            else
                            {
                                setFilterFromOptionValuePair(timeValue);
                                timeValue = string.Empty;
                                setFilterFromOptionValuePair(optionValuePair);
                            }
                        }
                        else
                        {
                            setFilterFromOptionValuePair(optionValuePair);
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        private void setFilterFromOptionValuePair(string optionValuePair)
        {
            if (string.IsNullOrEmpty(optionValuePair))
            {
                throw (new ArgumentNullException());
            }
            else
            {
                try
                {
                    string[] optionValue = optionValuePair.Split('=');

                    switch (optionValue[0])
                    {
                        case "epc":
                            SetEpcPrefix(optionValue[1]);
                            break;
                        case "zoneNames":
                            SetZoneNames(optionValue[1]);
                            break;
                        case "presenceConfidence":
                            SetPresenceConfidence(optionValue[1]);
                            break;
                        case "epcFormat":
                            SetEpcFormat(optionValue[1]);
                            break;
                        case "fromTime":
                            SetFromTime(optionValue[1]);
                            break;
                        case "facility":
                            SetFacility(optionValue[1]);
                            break;
                        default:
                            break;
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        public string GetFullOptionsString()
        {
            StringBuilder options = new StringBuilder();
            options.Append(startOfFilter);
            options.Append("pageSize=");
            options.Append(_pageSize.ToString());

            if (false == string.IsNullOrEmpty(_epcPrefix))
            {
                options.Append(filterDelimiter);
                options.Append("epcPrefix=");
                options.Append(_epcPrefix);
            }

            if (false == string.IsNullOrEmpty(_zoneNames))
            {
                options.Append(filterDelimiter);
                options.Append("zoneNames=");
                options.Append(_zoneNames);
            }

            if (false == string.IsNullOrEmpty(_presenceConfidence))
            {
                options.Append(filterDelimiter);
                options.Append("presenceConfidence=");
                options.Append(_presenceConfidence);
            }

            if (false == string.IsNullOrEmpty(_epcFormat))
            {
                options.Append(filterDelimiter);
                options.Append("epcFormat=");
                options.Append(_epcFormat);
            }

            if (false == string.IsNullOrEmpty(_facility))
            {
                options.Append(filterDelimiter);
                options.Append("toTime=");
                options.Append(_facility);
            }

            // No command ine filter at this time; objective achieved via
            // post-processing.
            //if (false == string.IsNullOrEmpty(_fromTime))
            //{
            //}

            if (false == string.IsNullOrEmpty(_pageMarker))
            {
                options.Append(filterDelimiter);
                options.Append("pageMarker=");
                options.Append(_pageMarker);
            }

            return options.ToString();
        }

    }
}
